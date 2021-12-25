using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateYM2413 : ChannelState
    {
        protected int  channelIdx = 0;
        protected byte Ym2413Instrument = 0;
        protected byte prevPeriodHi;

        public ChannelStateYM2413(IPlayerInterface player, int apuIdx, int channelType) : base(player, apuIdx, channelType, false)
        {
            channelIdx = channelType - ChannelType.YM2413Fm1;
            customRelease = true;
        }

        private void WriteYM2413Register(int reg, int data)
        {
            WriteRegister(NesApu.YM2413_REG_SEL,   reg);
            WriteRegister(NesApu.YM2413_REG_WRITE, data);
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            if (instrument != null)
            {
                Debug.Assert(instrument.IsYM2413Instrument);

                if (instrument.IsYM2413Instrument)
                {
                    if (instrument.YM2413Patch == 0)
                    {
                        // Tell other channels using custom patches that they will need 
                        // to reload their instruments.
                        player.NotifyInstrumentLoaded(
                            instrument,
                            (1 << ChannelType.YM2413Fm1) |
                            (1 << ChannelType.YM2413Fm2) |
                            (1 << ChannelType.YM2413Fm3) |
                            (1 << ChannelType.YM2413Fm4) |
                            (1 << ChannelType.YM2413Fm5) |
                            (1 << ChannelType.YM2413Fm6) |
                            (1 << ChannelType.YM2413Fm7) |
                            (1 << ChannelType.YM2413Fm8) |
                            (1 << ChannelType.YM2413Fm9));

                        for (byte i = 0; i < 8; i++)
                            WriteYM2413Register(i, instrument.YM2413PatchRegs[i]);
                    }

                    Ym2413Instrument = (byte)(instrument.YM2413Patch << 4);

                    if (instrument.YM2413Patch > 16)
                    {
                        WriteYM2413Register(NesApu.YM2413_REG_RHYTHM_MODE, Ym2413Instrument);
                        WriteYM2413Register(NesApu.YM2413_REG_DRUM_BD, Ym2413Instrument);
                        WriteYM2413Register(NesApu.YM2413_REG_DRUM_SD_HH, Ym2413Instrument);
                        WriteYM2413Register(NesApu.YM2413_REG_DRUM_TOM_CYM, Ym2413Instrument);
                    }
            
                }
            }

        }

        public override void IntrumentLoadedNotify(Instrument instrument)
        {
            Debug.Assert(instrument.IsYM2413Instrument && instrument.YM2413Patch == 0);

            // This will be called when another channel loads a custom patch.
            if (note.Instrument != null && 
                note.Instrument != instrument &&
                note.Instrument.YM2413Patch == 0)
            {
                forceInstrumentReload = true;
            }
        }

        private int GetOctave(ref int period)
        {
            var octave = 0;
            while (period >= 0x200)
            {
                period >>= 1;
                octave++;
            }
            return octave;
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                prevPeriodHi = (byte)(prevPeriodHi & ~(0x30));
                WriteYM2413Register(NesApu.YM2413_REG_HI_1 + channelIdx, prevPeriodHi);
            }
            else if (note.IsRelease)
            {
                prevPeriodHi = (byte)(prevPeriodHi & ~(0x10));
                WriteYM2413Register(NesApu.YM2413_REG_HI_1 + channelIdx, prevPeriodHi);
            }
            else if (note.IsMusical)
            {
                var period  = GetPeriod();
                var octave  = GetOctave(ref period);
                var volume  = 15 - GetVolume();

                var periodLo = (byte)(period & 0xff);
                var periodHi = (byte)(0x30 | ((octave & 0x7) << 1) | ((period >> 8) & 1));

                if (noteTriggered && (prevPeriodHi & 0x10) != 0)
                    WriteYM2413Register(NesApu.YM2413_REG_HI_1 + channelIdx, prevPeriodHi & ~(0x10));

                WriteYM2413Register(NesApu.YM2413_REG_LO_1  + channelIdx, periodLo);
                WriteYM2413Register(NesApu.YM2413_REG_HI_1  + channelIdx, periodHi);
                WriteYM2413Register(NesApu.YM2413_REG_VOL_1 + channelIdx, Ym2413Instrument | volume);

                prevPeriodHi = periodHi;
            }
            

            

            base.UpdateAPU();
        }


    };

}
