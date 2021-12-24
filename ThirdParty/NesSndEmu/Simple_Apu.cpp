
// Nes_Snd_Emu 0.1.7. http://www.slack.net/~ant/libs/

#include "Simple_Apu.h"

/* Copyright (C) 2003-2005 Shay Green. This module is free software; you
can redistribute it and/or modify it under the terms of the GNU Lesser
General Public License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version. This
module is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for
more details. You should have received a copy of the GNU Lesser General
Public License along with this module; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA */

static int null_dmc_reader( void*, cpu_addr_t )
{
	return 0x55; // causes dmc sample to be flat
}

Simple_Apu::Simple_Apu()
{
	pal_mode = false;
	seeking = false; 
	separate_tnd_mode = tnd_mode_single;
	time = 0;
	frame_length = 29780;
	tnd_volume = 1.0;
	expansions = expansion_mask_none;
	apu.dmc_reader( null_dmc_reader, NULL );
}

Simple_Apu::~Simple_Apu()
{
}

void Simple_Apu::dmc_reader( int (*f)( void* user_data, cpu_addr_t ), void* p )
{
	assert( f );
	apu.dmc_reader( f, p );
}

blargg_err_t Simple_Apu::sample_rate( long rate, bool pal, int tnd_mode )
{
	pal_mode = pal;
	separate_tnd_mode = tnd_mode;
	separate_tnd_channel_enabled[0] = true;
	separate_tnd_channel_enabled[1] = true;
	separate_tnd_channel_enabled[2] = true;
	frame_length = pal ? 33247 : 29780;

	if (separate_tnd_mode)
	{
		apu.osc_output(2, &tnd[0]);
		apu.osc_output(3, &tnd[1]);
		apu.osc_output(4, &tnd[2]);
	}
	else
	{
		apu.output(&buf, &tnd[0]);
	}

	vrc6.output(&buf);
	vrc7.output(&buf);
	fds.output(&buf);
	mmc5.output(&buf);
	namco.output(&buf);
	sunsoft.output(&buf);
	YM2413.output(&buf);
	
	tnd[0].clock_rate(pal ? 1662607 : 1789773);
	tnd[1].clock_rate(pal ? 1662607 : 1789773);
	tnd[2].clock_rate(pal ? 1662607 : 1789773);
	buf.clock_rate(pal ? 1662607 : 1789773);

	tnd[0].sample_rate(rate);
	tnd[1].sample_rate(rate);
	tnd[2].sample_rate(rate);
	return buf.sample_rate( rate );
}

void Simple_Apu::enable_channel(int expansion, int idx, bool enable)
{
	if (expansion == 0)
	{
		if (idx < 2)
		{
			apu.osc_output(idx, enable ? &buf : NULL);
		}
		else
		{
			if (separate_tnd_mode)
			{
				separate_tnd_channel_enabled[idx - 2] = enable;
			}
			else
			{
				apu.osc_output(idx, enable ? &tnd[0] : NULL);
			}
		}
	}
	else
	{
		switch (expansion)
		{
		case expansion_vrc6: vrc6.osc_output(idx, enable ? &buf : NULL); break;
		case expansion_vrc7: vrc7.enable_channel(idx, enable); break;
		case expansion_fds: fds.output(enable ? &buf : NULL); break;
		case expansion_mmc5: mmc5.osc_output(idx, enable ? &buf : NULL); break;
		case expansion_namco: namco.osc_output(idx, enable ? &buf : NULL); break;
		case expansion_sunsoft: sunsoft.osc_output(idx, enable ? &buf : NULL); break;
		case expansion_YM2413: YM2413.enable_channel(idx, enable ? &buf : NULL); break;
		}
	}
}

void Simple_Apu::treble_eq(int exp, double treble, int sample_rate)
{
	blip_eq_t eq(treble, 0, sample_rate);

	switch (exp)
	{
		case expansion_none: apu.treble_eq(eq); break;
		case expansion_vrc6: vrc6.treble_eq(eq); break;
		case expansion_vrc7: vrc7.treble_eq(eq); break;
		case expansion_fds: fds.treble_eq(eq); break;
		case expansion_mmc5: mmc5.treble_eq(eq); break;
		case expansion_namco: namco.treble_eq(eq); break;
		case expansion_sunsoft: sunsoft.treble_eq(eq); break;
		case expansion_YM2413: YM2413.treble_eq(eq); break;
	}
}

void Simple_Apu::set_expansion_volume(int exp, double volume)
{
	switch (exp)
	{
		case expansion_none: apu.enable_nonlinear(volume); tnd_volume = (float)volume; break;
		case expansion_vrc6: vrc6.volume(volume); break;
		case expansion_vrc7: vrc7.volume(volume); break;
		case expansion_fds: fds.volume(volume); break;
		case expansion_mmc5: mmc5.volume(volume); break;
		case expansion_namco: namco.volume(volume); break;
		case expansion_sunsoft: sunsoft.volume(volume); break;
		case expansion_YM2413: YM2413.volume(volume); break;
	}
}

void Simple_Apu::write_register(cpu_addr_t addr, int data)
{
	if (seeking)
	{
		if (addr >= Nes_Apu::start_addr && addr <= Nes_Apu::end_addr)
		{
			apu.write_shadow_register(addr, data);
		}
		else
		{
			if (expansions & expansion_mask_vrc6) vrc6.write_shadow_register(addr, data);
			if (expansions & expansion_mask_vrc7) vrc7.write_shadow_register(addr, data);
			if (expansions & expansion_mask_fds) fds.write_shadow_register(addr, data);
			if (expansions & expansion_mask_mmc5) mmc5.write_shadow_register(addr, data);
			if (expansions & expansion_mask_namco) namco.write_shadow_register(addr, data);
			if (expansions & expansion_mask_sunsoft) sunsoft.write_shadow_register(addr, data);
			if (expansions & expansion_mask_YM2413) YM2413.write_shadow_register(addr, data);
		}
	}
	else
	{
		if (addr >= Nes_Apu::start_addr && addr <= Nes_Apu::end_addr)
		{
			apu.write_register(clock(), addr, data);
		}
		else
		{
			if (expansions & expansion_mask_vrc6) vrc6.write_register(clock(), addr, data);
			if (expansions & expansion_mask_vrc7) vrc7.write_register(clock(), addr, data);
			if (expansions & expansion_mask_fds) fds.write_register(clock(), addr, data);
			if (expansions & expansion_mask_mmc5) mmc5.write_register(clock(), addr, data);
			if (expansions & expansion_mask_namco) namco.write_register(clock(), addr, data);
			if (expansions & expansion_mask_sunsoft) sunsoft.write_register(clock(), addr, data);
			if (expansions & expansion_mask_YM2413) YM2413.write_register(clock(), addr, data);
		}
	}
}

void Simple_Apu::start_seeking()
{
	seeking = true;
	apu.start_seeking();

	if (expansions & expansion_mask_vrc6) vrc6.start_seeking();
	if (expansions & expansion_mask_vrc7) vrc7.start_seeking();
	if (expansions & expansion_mask_fds) fds.start_seeking();
	if (expansions & expansion_mask_mmc5) mmc5.start_seeking();
	if (expansions & expansion_mask_namco) namco.start_seeking();
	if (expansions & expansion_mask_sunsoft) sunsoft.start_seeking();
	if (expansions & expansion_mask_YM2413) YM2413.start_seeking();
}

void Simple_Apu::stop_seeking()
{
	apu.stop_seeking(time);

	if (expansions & expansion_mask_vrc6) vrc6.stop_seeking(time);
	if (expansions & expansion_mask_vrc7) vrc7.stop_seeking(time);
	if (expansions & expansion_mask_fds) fds.stop_seeking(time);
	if (expansions & expansion_mask_mmc5) mmc5.stop_seeking(time);
	if (expansions & expansion_mask_namco) namco.stop_seeking(time);
	if (expansions & expansion_mask_sunsoft) sunsoft.stop_seeking(time);
	if (expansions & expansion_mask_YM2413) YM2413.stop_seeking(time);

	seeking = false;
}

int Simple_Apu::read_status()
{
	return apu.read_status( clock() );
}

void Simple_Apu::skip_cycles(long cycles)
{
	if (!seeking)
	{
		time += cycles;
		apu.run_until(time);
	}
}

void Simple_Apu::end_frame()
{
	time = 0;
	frame_length ^= 1;

	apu.end_frame( frame_length );

	if (expansions & expansion_mask_vrc6) vrc6.end_frame(frame_length);
	if (expansions & expansion_mask_vrc7) vrc7.end_frame(frame_length);
	if (expansions & expansion_mask_fds) fds.end_frame(frame_length); 
	if (expansions & expansion_mask_mmc5) mmc5.end_frame(frame_length); 
	if (expansions & expansion_mask_namco) namco.end_frame(frame_length); 
	if (expansions & expansion_mask_sunsoft) sunsoft.end_frame(frame_length);
	if (expansions & expansion_mask_YM2413) YM2413.end_frame(frame_length);

	buf.end_frame( frame_length );
	tnd[0].end_frame( frame_length );

	if (separate_tnd_mode)
	{
		tnd[1].end_frame(frame_length);
		tnd[2].end_frame(frame_length);
	}
}

void Simple_Apu::reset()
{
	apu.enable_nonlinear(1.0);
	seeking = false;
	prev_nonlinear_tnd = 0;
	tnd_accum[0] = 0;
	tnd_accum[1] = 0;
	tnd_accum[2] = 0;
	apu.reset(pal_mode);
	vrc6.reset();
	vrc7.reset();
	fds.reset();
	mmc5.reset();
	namco.reset();
	sunsoft.reset();
	YM2413.reset();
}

void Simple_Apu::set_audio_expansions(long exp)
{
	expansions = exp;
}

long Simple_Apu::samples_avail() const
{
	assert(buf.samples_avail() == tnd[0].samples_avail());
	assert(buf.samples_avail() == tnd[1].samples_avail() && separate_tnd_mode || tnd[1].samples_avail() == 0 && !separate_tnd_mode);
	assert(buf.samples_avail() == tnd[2].samples_avail() && separate_tnd_mode || tnd[2].samples_avail() == 0 && !separate_tnd_mode);

	return buf.samples_avail();
}

const int    sample_shift     = blip_sample_bits - 16;
const double sample_scale_inv = (1 << sample_shift) * 65535.0;
const double sample_scale     = 1.0 / sample_scale_inv;

// Using the 3 * tri (15) + 2 * noise (15) + dmc (127) approximation = maximum value is 202.
const float tnd_scale = 202.0f;

inline float unpack_sample(long raw_sample)
{
	float sample_float = (float)(raw_sample * sample_scale);
	// TODO : Investigate this. We sometimes have values that dips every so slightly in the negative range.
	// It never goes below -0.01f so they are essentially zero, but not quite. Worrying.
	// assert(sample_float >= 0.0f); 
	return max(0.00001f, sample_float);
}

inline long pack_sample(float sample_float)
{
	// Same as above, but rescale to the fixed point, blip buffer format.
	return (long)(sample_float * sample_scale_inv);
}

inline float nonlinearize(float sample_float)
{
	return 163.67f / (24329.0f / (sample_float * tnd_scale) + 100.0f);
}

long Simple_Apu::read_samples( sample_t* out, long count )
{
	assert(buf.samples_avail() == tnd[0].samples_avail());
	assert(buf.samples_avail() == tnd[1].samples_avail() && separate_tnd_mode || tnd[1].samples_avail() == 0 && !separate_tnd_mode);
	assert(buf.samples_avail() == tnd[2].samples_avail() && separate_tnd_mode || tnd[2].samples_avail() == 0 && !separate_tnd_mode);

	if (count)
	{
		if (separate_tnd_mode)
		{
			Blip_Buffer::buf_t_* p[3];
			p[0] = tnd[0].buffer_;
			p[1] = tnd[1].buffer_;
			p[2] = tnd[2].buffer_;

			for (unsigned n = count; n--; )
			{
				// Sum all 3 channels, apply non-linear mixing.
				tnd_accum[0] += (long)*p[0];
				tnd_accum[1] += (long)*p[1];
				tnd_accum[2] += (long)*p[2];
				
				float samples_float[3];
				samples_float[0] = unpack_sample(tnd_accum[0]);
				samples_float[1] = unpack_sample(tnd_accum[1]);
				samples_float[2] = unpack_sample(tnd_accum[2]);

				// When running in "TN only" mode and exporting only the DPCM channel
				// ignore the contribution from the others. This is not correct but avoid
				// having the triangle bleed in the DPCM channel.
				if (separate_tnd_mode == tnd_mode_separate_tn_only &&
					separate_tnd_channel_enabled[0] == false &&
					separate_tnd_channel_enabled[1] == false &&
					separate_tnd_channel_enabled[2] == true)
				{
					samples_float[0] = 0.0f;
					samples_float[1] = 0.0f;
				}

				float samples_sum = samples_float[0] + samples_float[1] + samples_float[2];
				float all_channels_nonlinear_mix = nonlinearize(samples_sum);

				// Make sure the channels will sum up to the expected value.
				float ratio = all_channels_nonlinear_mix / samples_sum;

				float enabled_channels_non_linear_mix = 0.0f;
				if (separate_tnd_channel_enabled[0]) enabled_channels_non_linear_mix += samples_float[0] * ratio;
				if (separate_tnd_channel_enabled[1]) enabled_channels_non_linear_mix += samples_float[1] * ratio;
				if (separate_tnd_channel_enabled[2]) enabled_channels_non_linear_mix += samples_float[2] * ratio;

				long nonlinear_tnd = pack_sample(enabled_channels_non_linear_mix * tnd_volume);

				// Write final result in tnd[0] so that the remaining code can proceed as usual.
				*p[0]++ = (nonlinear_tnd - prev_nonlinear_tnd);
				 p[1]++;
				 p[2]++;

				prev_nonlinear_tnd = nonlinear_tnd;
			}
		}
		else
		{
			// Apply non-linear mixing to the TND buffer.
			Blip_Buffer::buf_t_* p = tnd[0].buffer_;

			for (unsigned n = count; n--; )
			{
				tnd_accum[0] += (long)*p;
				long nonlinear_tnd = pack_sample(nonlinearize(unpack_sample(tnd_accum[0])) * tnd_volume);
				*p++ = (nonlinear_tnd - prev_nonlinear_tnd);
				prev_nonlinear_tnd = nonlinear_tnd;
			}
		}

		// Then mix both blip buffers.
		Blip_Reader lin;
		Blip_Reader nonlin;

		int lin_bass = lin.begin(buf);
		int nonlin_bass = nonlin.begin(tnd[0]);

		for (int n = count; n--; )
		{
			int s = lin.read() + nonlin.read();
			lin.next(lin_bass);
			nonlin.next(nonlin_bass);
			*out++ = s;

			if ((BOOST::int16_t)s != s)
				out[-1] = 0x7FFF - (s >> 24);
		}

		lin.end(buf);
		nonlin.end(tnd[0]);

		buf.remove_samples(count);
		tnd[0].remove_samples(count);

		if (separate_tnd_mode)
		{
			tnd[1].remove_samples(count);
			tnd[2].remove_samples(count);
		}
	}

	return count;
}

void Simple_Apu::remove_samples(long s)
{
	buf.remove_samples(s);

	tnd[0].remove_samples(s);
	if (separate_tnd_mode)
	{
		tnd[1].remove_samples(s);
		tnd[2].remove_samples(s);
	}
}

void Simple_Apu::save_snapshot( apu_snapshot_t* out ) const
{
	apu.save_snapshot( out );
}

void Simple_Apu::load_snapshot( apu_snapshot_t const& in )
{
	apu.load_snapshot( in );
}

