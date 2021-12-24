﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamiStudio
{
    public class ParamInfo
    {
        public string Name;
        public string ToolTip;
        public int MinValue;
        public int MaxValue;
        public int DefaultValue;
        public int SnapValue;
        public bool IsList;

        public delegate int GetValueDelegate();
        public delegate bool EnabledDelegate();
        public delegate void SetValueDelegate(int value);
        public delegate string GetValueStringDelegate();

        public EnabledDelegate IsEnabled;
        public GetValueDelegate GetValue;
        public SetValueDelegate SetValue;
        public GetValueStringDelegate GetValueString;

        public int SnapAndClampValue(int value)
        {
            if (SnapValue > 1)
            {
                value = (value / SnapValue) * SnapValue;
            }

            return Utils.Clamp(value, MinValue, MaxValue);
        }

        protected ParamInfo(string name, int minVal, int maxVal, int defaultVal, string tooltip, bool list = false, int snap = 1)
        {
            Name = name;
            ToolTip = tooltip;
            MinValue = minVal;
            MaxValue = maxVal;
            DefaultValue = defaultVal;
            IsList = list;
            SnapValue = snap;
            GetValueString = () => GetValue().ToString();
        }
    };

    public class InstrumentParamInfo : ParamInfo
    {
        public InstrumentParamInfo(Instrument inst, string name, int minVal, int maxVal, int defaultVal, string tooltip = null, bool list = false, int snap = 1) :
            base(name, minVal, maxVal, defaultVal, tooltip, list, snap)
        {
        }
    }

    public static class InstrumentParamProvider
    {
        static public bool HasParams(Instrument instrument)
        {
            return
                instrument.IsEnvelopeActive(EnvelopeType.Pitch) ||
                instrument.IsFdsInstrument  ||
                instrument.IsN163Instrument ||
                instrument.IsVrc6Instrument ||
                instrument.IsVrc7Instrument ||
                instrument.IsYM2413Instrument;
        }

        static public ParamInfo[] GetParams(Instrument instrument)
        {
            var paramInfos = new List<ParamInfo>();

            if (instrument.IsEnvelopeActive(EnvelopeType.Pitch))
            {
                paramInfos.Add(new InstrumentParamInfo(instrument, "Pitch Envelope", 0, 1, 0, "Absolute envelopes display the real pitch for a given time\nRelative envelopes adds the pitch to a running sum (FamiTracker-style)", true)
                {
                    GetValue = () => { return instrument.Envelopes[EnvelopeType.Pitch].Relative ? 1 : 0; },
                    GetValueString = () => { return instrument.Envelopes[EnvelopeType.Pitch].Relative ? "Relative" : "Absolute"; },
                    SetValue = (v) =>
                    {
                        var newRelative = v != 0;

                        /*
                         * Intentially not doing this, this is more confusing/frustrating than anything.
                        if (instrument.Envelopes[EnvelopeType.Pitch].Relative != newRelative)
                        {
                            if (newRelative)
                                instrument.Envelopes[EnvelopeType.Pitch].ConvertToRelative();
                            else
                                instrument.Envelopes[EnvelopeType.Pitch].ConvertToAbsolute();
                        }
                        */

                        instrument.Envelopes[EnvelopeType.Pitch].Relative = newRelative;
                    }
                });
            }

            switch (instrument.Expansion)
            {
                case ExpansionType.Fds:
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Master Volume", 0, 3, 0, null, true)
                        { GetValue = () => { return instrument.FdsMasterVolume; }, GetValueString = () => { return FdsMasterVolumeType.Names[instrument.FdsMasterVolume]; }, SetValue = (v) => { instrument.FdsMasterVolume = (byte)v; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Wave Preset", 0, WavePresetType.Count - 1, WavePresetType.Sine, null, true)
                            { GetValue = () => { return instrument.FdsWavePreset; }, GetValueString = () => { return WavePresetType.Names[instrument.FdsWavePreset]; }, SetValue = (v) => { instrument.FdsWavePreset = (byte)v; instrument.UpdateFdsWaveEnvelope(); } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Mod Preset", 0, WavePresetType.Count - 1, WavePresetType.Flat, null, true )
                            { GetValue = () => { return instrument.FdsModPreset; }, GetValueString = () => { return WavePresetType.Names[instrument.FdsModPreset]; }, SetValue = (v) => { instrument.FdsModPreset = (byte)v; instrument.UpdateFdsModulationEnvelope(); } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Mod Speed", 0, 4095, 0)
                            { GetValue = () => { return instrument.FdsModSpeed; }, SetValue = (v) => { instrument.FdsModSpeed = (ushort)v; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Mod Depth", 0, 63, 0)
                            { GetValue = () => { return instrument.FdsModDepth; }, SetValue = (v) => { instrument.FdsModDepth = (byte)v; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Mod Delay", 0, 255, 0)
                            { GetValue = () => { return instrument.FdsModDelay; }, SetValue = (v) => { instrument.FdsModDelay = (byte)v; } });
                    break;

                case ExpansionType.N163:
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Wave Preset", 0, WavePresetType.Count - 1, WavePresetType.Sine, null, true)
                            { GetValue = () => { return instrument.N163WavePreset; }, GetValueString = () => { return WavePresetType.Names[instrument.N163WavePreset]; }, SetValue = (v) => { instrument.N163WavePreset = (byte)v;} });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Wave Size", 4, 248, 16, null, false, 4)
                            { GetValue = () => { return instrument.N163WaveSize; }, SetValue = (v) => { instrument.N163WaveSize = (byte)v;} });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Wave Position", 0, 244, 0, null, false, 4)
                            { GetValue = () => { return instrument.N163WavePos; }, SetValue = (v) => { instrument.N163WavePos = (byte)v;} });
                    break;

                case ExpansionType.Vrc6:
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Saw Master Volume", 0, 2, 0, null, true)
                            { GetValue = ()  => { return instrument.Vrc6SawMasterVolume; }, GetValueString = () => { return Vrc6SawMasterVolumeType.Names[instrument.Vrc6SawMasterVolume]; }, SetValue = (v) => { instrument.Vrc6SawMasterVolume = (byte)v; } });
                    break;

                case ExpansionType.Vrc7:
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Patch", 0, 15, 1, null, true)
                       { GetValue = () => { return instrument.Vrc7Patch; }, GetValueString = () => { return Instrument.GetVrc7PatchName(instrument.Vrc7Patch); }, SetValue = (v) => { instrument.Vrc7Patch = (byte)v; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier Tremolo", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[1] & 0x80) >> 7)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[1] & 0x80) >> 7; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x80)) | ((v << 7) & 0x80)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier Vibrato", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[1] & 0x40) >> 6)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[1] & 0x40) >> 6; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x40)) | ((v << 6) & 0x40)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier Sustained", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[1] & 0x20) >> 5)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[1] & 0x20) >> 5; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x20)) | ((v << 5) & 0x20)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier Wave Rectified", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[3] & 0x10) >> 4)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[3] & 0x10) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[3] = (byte)((instrument.Vrc7PatchRegs[3] & (~0x10)) | ((v << 4) & 0x10)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier KeyScaling", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[1] & 0x10) >> 4)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[1] & 0x10) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x10)) | ((v << 4) & 0x10)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier KeyScaling Level", 0, 3, (Vrc7InstrumentPatch.Infos[1].data[3] & 0xc0) >> 6)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[3] & 0xc0) >> 6; }, SetValue = (v) => { instrument.Vrc7PatchRegs[3] = (byte)((instrument.Vrc7PatchRegs[3] & (~0xc0)) | ((v << 6) & 0xc0)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier FreqMultiplier", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[1] & 0x0f) >> 0)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[1] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier Attack", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[5] & 0xf0) >> 4)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[5] & 0xf0) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[5] = (byte)((instrument.Vrc7PatchRegs[5] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier Decay", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[5] & 0x0f) >> 0)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[5] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[5] = (byte)((instrument.Vrc7PatchRegs[5] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier Sustain", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[7] & 0xf0) >> 4)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[7] & 0xf0) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[7] = (byte)((instrument.Vrc7PatchRegs[7] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier Release", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[7] & 0x0f) >> 0)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[7] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[7] = (byte)((instrument.Vrc7PatchRegs[7] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Tremolo", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[0] & 0x80) >> 7)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[0] & 0x80) >> 7; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x80)) | ((v << 7) & 0x80)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Vibrato", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[0] & 0x40) >> 6)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[0] & 0x40) >> 6; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x40)) | ((v << 6) & 0x40)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Sustained", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[0] & 0x20) >> 5)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[0] & 0x20) >> 5; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x20)) | ((v << 5) & 0x20)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Wave Rectified", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[3] & 0x08) >> 3)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[3] & 0x08) >> 3; }, SetValue = (v) => { instrument.Vrc7PatchRegs[3] = (byte)((instrument.Vrc7PatchRegs[3] & (~0x08)) | ((v << 3) & 0x08)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator KeyScaling", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[0] & 0x10) >> 4)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[0] & 0x10) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x10)) | ((v << 4) & 0x10)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator KeyScaling Level", 0, 3, (Vrc7InstrumentPatch.Infos[1].data[2] & 0xc0) >> 6)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[2] & 0xc0) >> 6; }, SetValue = (v) => { instrument.Vrc7PatchRegs[2] = (byte)((instrument.Vrc7PatchRegs[2] & (~0xc0)) | ((v << 6) & 0xc0)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator FreqMultiplier", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[0] & 0x0f) >> 0)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[0] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Attack", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[4] & 0xf0) >> 4)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[4] & 0xf0) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[4] = (byte)((instrument.Vrc7PatchRegs[4] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Decay", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[4] & 0x0f) >> 0)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[4] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[4] = (byte)((instrument.Vrc7PatchRegs[4] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Sustain", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[6] & 0xf0) >> 4)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[6] & 0xf0) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[6] = (byte)((instrument.Vrc7PatchRegs[6] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Release", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[6] & 0x0f) >> 0)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[6] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[6] = (byte)((instrument.Vrc7PatchRegs[6] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Level", 0, 63, (Vrc7InstrumentPatch.Infos[1].data[2] & 0x3f) >> 0)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[2] & 0x3f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[2] = (byte)((instrument.Vrc7PatchRegs[2] & (~0x3f)) | ((v << 0) & 0x3f)); instrument.Vrc7Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Feedback", 0, 7, (Vrc7InstrumentPatch.Infos[1].data[3] & 0x07) >> 0)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[3] & 0x07) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[3] = (byte)((instrument.Vrc7PatchRegs[3] & (~0x07)) | ((v << 0) & 0x07)); instrument.Vrc7Patch = 0; } });
                    break;

                case ExpansionType.YM2413:
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Patch", 0, 21, 1, null, true)
                    { GetValue = () => { return instrument.YM2413Patch; }, GetValueString = () => { return Instrument.GetYM2413PatchName(instrument.YM2413Patch); }, SetValue = (v) => { instrument.YM2413Patch = (byte)v; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier Tremolo", 0, 1, (YM2413InstrumentPatch.Infos[1].data[1] & 0x80) >> 7)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[1] & 0x80) >> 7; }, SetValue = (v) => { instrument.YM2413PatchRegs[1] = (byte)((instrument.YM2413PatchRegs[1] & (~0x80)) | ((v << 7) & 0x80)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier Vibrato", 0, 1, (YM2413InstrumentPatch.Infos[1].data[1] & 0x40) >> 6)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[1] & 0x40) >> 6; }, SetValue = (v) => { instrument.YM2413PatchRegs[1] = (byte)((instrument.YM2413PatchRegs[1] & (~0x40)) | ((v << 6) & 0x40)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier Sustained", 0, 1, (YM2413InstrumentPatch.Infos[1].data[1] & 0x20) >> 5)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[1] & 0x20) >> 5; }, SetValue = (v) => { instrument.YM2413PatchRegs[1] = (byte)((instrument.YM2413PatchRegs[1] & (~0x20)) | ((v << 5) & 0x20)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier Wave Rectified", 0, 1, (YM2413InstrumentPatch.Infos[1].data[3] & 0x10) >> 4)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[3] & 0x10) >> 4; }, SetValue = (v) => { instrument.YM2413PatchRegs[3] = (byte)((instrument.YM2413PatchRegs[3] & (~0x10)) | ((v << 4) & 0x10)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier KeyScaling", 0, 1, (YM2413InstrumentPatch.Infos[1].data[1] & 0x10) >> 4)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[1] & 0x10) >> 4; }, SetValue = (v) => { instrument.YM2413PatchRegs[1] = (byte)((instrument.YM2413PatchRegs[1] & (~0x10)) | ((v << 4) & 0x10)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier KeyScaling Level", 0, 3, (YM2413InstrumentPatch.Infos[1].data[3] & 0xc0) >> 6)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[3] & 0xc0) >> 6; }, SetValue = (v) => { instrument.YM2413PatchRegs[3] = (byte)((instrument.YM2413PatchRegs[3] & (~0xc0)) | ((v << 6) & 0xc0)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier FreqMultiplier", 0, 15, (YM2413InstrumentPatch.Infos[1].data[1] & 0x0f) >> 0)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[1] & 0x0f) >> 0; }, SetValue = (v) => { instrument.YM2413PatchRegs[1] = (byte)((instrument.YM2413PatchRegs[1] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier Attack", 0, 15, (YM2413InstrumentPatch.Infos[1].data[5] & 0xf0) >> 4)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[5] & 0xf0) >> 4; }, SetValue = (v) => { instrument.YM2413PatchRegs[5] = (byte)((instrument.YM2413PatchRegs[5] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier Decay", 0, 15, (YM2413InstrumentPatch.Infos[1].data[5] & 0x0f) >> 0)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[5] & 0x0f) >> 0; }, SetValue = (v) => { instrument.YM2413PatchRegs[5] = (byte)((instrument.YM2413PatchRegs[5] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier Sustain", 0, 15, (YM2413InstrumentPatch.Infos[1].data[7] & 0xf0) >> 4)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[7] & 0xf0) >> 4; }, SetValue = (v) => { instrument.YM2413PatchRegs[7] = (byte)((instrument.YM2413PatchRegs[7] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Carrier Release", 0, 15, (YM2413InstrumentPatch.Infos[1].data[7] & 0x0f) >> 0)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[7] & 0x0f) >> 0; }, SetValue = (v) => { instrument.YM2413PatchRegs[7] = (byte)((instrument.YM2413PatchRegs[7] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Tremolo", 0, 1, (YM2413InstrumentPatch.Infos[1].data[0] & 0x80) >> 7)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[0] & 0x80) >> 7; }, SetValue = (v) => { instrument.YM2413PatchRegs[0] = (byte)((instrument.YM2413PatchRegs[0] & (~0x80)) | ((v << 7) & 0x80)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Vibrato", 0, 1, (YM2413InstrumentPatch.Infos[1].data[0] & 0x40) >> 6)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[0] & 0x40) >> 6; }, SetValue = (v) => { instrument.YM2413PatchRegs[0] = (byte)((instrument.YM2413PatchRegs[0] & (~0x40)) | ((v << 6) & 0x40)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Sustained", 0, 1, (YM2413InstrumentPatch.Infos[1].data[0] & 0x20) >> 5)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[0] & 0x20) >> 5; }, SetValue = (v) => { instrument.YM2413PatchRegs[0] = (byte)((instrument.YM2413PatchRegs[0] & (~0x20)) | ((v << 5) & 0x20)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Wave Rectified", 0, 1, (YM2413InstrumentPatch.Infos[1].data[3] & 0x08) >> 3)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[3] & 0x08) >> 3; }, SetValue = (v) => { instrument.YM2413PatchRegs[3] = (byte)((instrument.YM2413PatchRegs[3] & (~0x08)) | ((v << 3) & 0x08)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator KeyScaling", 0, 1, (YM2413InstrumentPatch.Infos[1].data[0] & 0x10) >> 4)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[0] & 0x10) >> 4; }, SetValue = (v) => { instrument.YM2413PatchRegs[0] = (byte)((instrument.YM2413PatchRegs[0] & (~0x10)) | ((v << 4) & 0x10)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator KeyScaling Level", 0, 3, (YM2413InstrumentPatch.Infos[1].data[2] & 0xc0) >> 6)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[2] & 0xc0) >> 6; }, SetValue = (v) => { instrument.YM2413PatchRegs[2] = (byte)((instrument.YM2413PatchRegs[2] & (~0xc0)) | ((v << 6) & 0xc0)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator FreqMultiplier", 0, 15, (YM2413InstrumentPatch.Infos[1].data[0] & 0x0f) >> 0)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[0] & 0x0f) >> 0; }, SetValue = (v) => { instrument.YM2413PatchRegs[0] = (byte)((instrument.YM2413PatchRegs[0] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Attack", 0, 15, (YM2413InstrumentPatch.Infos[1].data[4] & 0xf0) >> 4)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[4] & 0xf0) >> 4; }, SetValue = (v) => { instrument.YM2413PatchRegs[4] = (byte)((instrument.YM2413PatchRegs[4] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Decay", 0, 15, (YM2413InstrumentPatch.Infos[1].data[4] & 0x0f) >> 0)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[4] & 0x0f) >> 0; }, SetValue = (v) => { instrument.YM2413PatchRegs[4] = (byte)((instrument.YM2413PatchRegs[4] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Sustain", 0, 15, (YM2413InstrumentPatch.Infos[1].data[6] & 0xf0) >> 4)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[6] & 0xf0) >> 4; }, SetValue = (v) => { instrument.YM2413PatchRegs[6] = (byte)((instrument.YM2413PatchRegs[6] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Release", 0, 15, (YM2413InstrumentPatch.Infos[1].data[6] & 0x0f) >> 0)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[6] & 0x0f) >> 0; }, SetValue = (v) => { instrument.YM2413PatchRegs[6] = (byte)((instrument.YM2413PatchRegs[6] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Modulator Level", 0, 63, (YM2413InstrumentPatch.Infos[1].data[2] & 0x3f) >> 0)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[2] & 0x3f) >> 0; }, SetValue = (v) => { instrument.YM2413PatchRegs[2] = (byte)((instrument.YM2413PatchRegs[2] & (~0x3f)) | ((v << 0) & 0x3f)); instrument.YM2413Patch = 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "Feedback", 0, 7, (YM2413InstrumentPatch.Infos[1].data[3] & 0x07) >> 0)
                    { GetValue = () => { return (instrument.YM2413PatchRegs[3] & 0x07) >> 0; }, SetValue = (v) => { instrument.YM2413PatchRegs[3] = (byte)((instrument.YM2413PatchRegs[3] & (~0x07)) | ((v << 0) & 0x07)); instrument.YM2413Patch = 0; } });
                    break;
            }

            return paramInfos.Count == 0 ? null : paramInfos.ToArray();
        }
    }

    public class DPCMSampleParamInfo : ParamInfo
    {
        public DPCMSampleParamInfo(DPCMSample sample, string name, int minVal, int maxVal, int defaultVal, string tooltip, bool list = false) :
            base(name, minVal, maxVal, defaultVal, tooltip, list)
        {
        }
    }

    public static class DPCMSampleParamProvider
    {
        static public ParamInfo[] GetParams(DPCMSample sample)
        {
            return new[]
            {
                new DPCMSampleParamInfo(sample, "Preview Rate", 0, 15, 15, "Rate to use when previewing the processed\nDMC data with the play button above", true)
                    { GetValue = () => { return sample.PreviewRate; }, GetValueString = () => { return DPCMSampleRate.GetString(true, FamiStudio.StaticInstance.PalPlayback, true, false, sample.PreviewRate); }, SetValue = (v) => { sample.PreviewRate = (byte)v; } },
                new DPCMSampleParamInfo(sample, "Sample Rate", 0, 15, 15, "Rate at which to resample the source data at", true)
                    { GetValue = () => { return sample.SampleRate; }, GetValueString = () => { return DPCMSampleRate.GetString(true, FamiStudio.StaticInstance.PalPlayback, true, false, sample.SampleRate); }, SetValue = (v) => { sample.SampleRate = (byte)v; sample.Process(); } },
                new DPCMSampleParamInfo(sample, "Padding Mode", 0, 4, DPCMPaddingType.PadTo16Bytes, "Padding method for the processed DMC data", true)
                    { GetValue = () => { return sample.PaddingMode; }, GetValueString = () => { return DPCMPaddingType.Names[sample.PaddingMode]; }, SetValue = (v) => { sample.PaddingMode = v; sample.Process(); } },
                new DPCMSampleParamInfo(sample, "DMC Initial Value", 0, 63, NesApu.DACDefaultValueDiv2, "Initial value of the DMC counter before any volume adjustment.\nThis is actually half of the value used in hardware.")
                    { GetValue = () => { return sample.DmcInitialValueDiv2; }, SetValue = (v) => { sample.DmcInitialValueDiv2 = v; sample.Process(); } },
                new DPCMSampleParamInfo(sample, "Volume Adjust", 0, 200, 100, "Volume adjustment (%)")
                    { GetValue = () => { return sample.VolumeAdjust; }, SetValue = (v) => { sample.VolumeAdjust = v; sample.Process(); } },
                new DPCMSampleParamInfo(sample, "Fine Tuning", 0, 200, 100, "Very fine pitch adjustment to help tune notes")
                    { GetValue = () => { return (int)Math.Round((sample.FinePitch - 0.95f) * 2000); }, SetValue = (v) => { sample.FinePitch = (v / 2000.0f) + 0.95f; sample.Process(); }, GetValueString = () => { return (sample.FinePitch * 100.0f).ToString("N2") + "%"; } },
                new DPCMSampleParamInfo(sample, "Process as PAL", 0, 1, 0, "Use PAL sample rates for all processing\nFor DMC source data, assumes PAL sample rate")
                    { GetValue = () => { return  sample.PalProcessing ? 1 : 0; }, SetValue = (v) => { sample.PalProcessing = v != 0; sample.Process(); } },
                new DPCMSampleParamInfo(sample, "Trim Zero Volume", 0, 1, 0, "Trim parts of the source data that is considered too low to be audible")
                    { GetValue = () => { return sample.TrimZeroVolume ? 1 : 0; }, SetValue = (v) => { sample.TrimZeroVolume = v != 0; sample.Process(); } },
                new DPCMSampleParamInfo(sample, "Reverse Bits", 0, 1, 0, "For DMC source data only, reverse the bits to correct errors in some NES games")
                    { GetValue = () => { return !sample.SourceDataIsWav && sample.ReverseBits ? 1 : 0; }, SetValue = (v) => { if (!sample.SourceDataIsWav) { sample.ReverseBits = v != 0; sample.Process(); } }, IsEnabled = () => { return !sample.SourceDataIsWav; } }
            };
        }
    }
}
