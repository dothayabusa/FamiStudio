using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateYM2413 : ChannelState
    {
        protected int channelIdx = 0;
        protected byte Ym2413Instrument = 0;
        protected byte prevPeriodHi;
        protected byte rhythmMode;



        public ChannelStateYM2413(IPlayerInterface player, int apuIdx, int channelType) : base(player, apuIdx, channelType, false)
        {
            channelIdx = channelType - ChannelType.YM2413Fm1;
            customRelease = true;
        }

        private void WriteYM2413Register(int reg, int data)
        {
            WriteRegister(NesApu.YM2413_REG_SEL, reg);
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
                var period = GetPeriod();
                var octave = GetOctave(ref period);
                var volume = 15 - GetVolume();

                var periodLo = (byte)(period & 0xff);
                var periodHi = (byte)(0x30 | ((octave & 0x7) << 1) | ((period >> 8) & 1));

                
                
                    if (noteTriggered && (prevPeriodHi & 0x10) != 0)
                        WriteYM2413Register(NesApu.YM2413_REG_HI_1 + channelIdx, prevPeriodHi & ~(0x10));

                    WriteYM2413Register(NesApu.YM2413_REG_LO_1 + channelIdx, periodLo);
                    WriteYM2413Register(NesApu.YM2413_REG_HI_1 + channelIdx, periodHi);
                    WriteYM2413Register(NesApu.YM2413_REG_VOL_1 + channelIdx, Ym2413Instrument | volume);
                


                prevPeriodHi = periodHi;


                var noteVal = GetPeriod();
                Console.WriteLine(noteVal);

                if (note.Instrument != null)
                    Debug.Assert(note.Instrument.IsYM2413Instrument);
                rhythmMode = note.Instrument.RhythmMode;

                if (note.HasRhythmMode) rhythmMode = note.RhythmMode;


                if (rhythmMode == 0x00)
                {
                    WriteYM2413Register(NesApu.YM2413_REG_RHYTHM_MODE, 0x00);
                }

                if (rhythmMode == 0x01)

                {
                    if ((channelIdx) >= 6)





                        WriteYM2413Register(NesApu.YM2413_REG_RHYTHM_MODE, 0x20);

                    {
                        switch (noteVal | volume)
                        {





                            //TOM | G#-0

                            case 274 | 15:
                                WriteYM2413Register(0x38, 0xff);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 274 | 14:
                                WriteYM2413Register(0x38, 0xef);
                                WriteYM2413Register(0x18, 0xc0);
                                break;
                            
                            case 274 | 11:
                                WriteYM2413Register(0x38, 0xbf);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 274 | 10:
                                WriteYM2413Register(0x38, 0xaf);
                                WriteYM2413Register(0x18, 0xc0);
                                break;
                          
                            case 274 | 7:
                                WriteYM2413Register(0x38, 0x7f);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 274 | 6:
                                WriteYM2413Register(0x38, 0x6f);
                                WriteYM2413Register(0x18, 0xc0);
                                break;
                           
                            case 274 | 3:
                                WriteYM2413Register(0x38, 0x3f);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 274 | 2:
                                WriteYM2413Register(0x38, 0x2f);
                                WriteYM2413Register(0x18, 0xc0);
                                break;



                            //-----------------------------------------------

                            //-----------------------------------------------





                            //BD + SD | C-3


                            case 1376 | 15:

                                WriteYM2413Register(0x37, 0xff);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x36, 0xff);
                                WriteYM2413Register(0x16, 0x20);
                                break;

                            case 1376 | 14:

                                WriteYM2413Register(0x37, 0xfe);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x36, 0xfe);
                                WriteYM2413Register(0x16, 0x20);
                                break;

                            case 1376 | 13:

                                WriteYM2413Register(0x37, 0xfd);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x36, 0xfd);
                                WriteYM2413Register(0x16, 0x20);
                                break;

                            case 1376 | 12:

                                WriteYM2413Register(0x37, 0xfc);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x36, 0xfc);
                                WriteYM2413Register(0x16, 0x20);
                                break;

                            case 1376 | 11:

                                WriteYM2413Register(0x37, 0xfb);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x36, 0xfb);
                                WriteYM2413Register(0x16, 0x20);
                                break;

                            case 1376 | 10:

                                WriteYM2413Register(0x37, 0xfa);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x36, 0xfa);
                                WriteYM2413Register(0x16, 0x20);
                                break;

                            case 1376 | 9:

                                WriteYM2413Register(0x37, 0xf9);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x36, 0xf9);
                                WriteYM2413Register(0x16, 0x20);
                                break;

                            case 1376 | 8:

                                WriteYM2413Register(0x37, 0xf8);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x36, 0xf8);
                                WriteYM2413Register(0x16, 0x20);
                                break;

                            case 1376 | 7:

                                WriteYM2413Register(0x37, 0xf7);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x36, 0xf7);
                                WriteYM2413Register(0x16, 0x20);
                                break;

                            case 1376 | 6:

                                WriteYM2413Register(0x37, 0xf6);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x36, 0xf6);
                                WriteYM2413Register(0x16, 0x20);
                                break;

                            case 1376 | 5:

                                WriteYM2413Register(0x37, 0xf5);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x36, 0xf5);
                                WriteYM2413Register(0x16, 0x20);
                                break;

                            case 1376 | 4:

                                WriteYM2413Register(0x37, 0xf4);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x36, 0xf4);
                                WriteYM2413Register(0x16, 0x20);
                                break;

                            case 1376 | 3:

                                WriteYM2413Register(0x37, 0xf3);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x36, 0xf3);
                                WriteYM2413Register(0x16, 0x20);
                                break;

                            case 1376 | 2:

                                WriteYM2413Register(0x37, 0xf2);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x36, 0xf2);
                                WriteYM2413Register(0x16, 0x20);
                                break;

                            case 1376 | 1:

                                WriteYM2413Register(0x37, 0xf1);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x36, 0xf1);
                                WriteYM2413Register(0x16, 0x20);
                                break;

                            case 1376 | 0:

                                WriteYM2413Register(0x37, 0xf0);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x36, 0xf0);
                                WriteYM2413Register(0x16, 0x20);
                                break;



                            //-----------------------------------------------

                            //-----------------------------------------------





                            //HH + TCY | F-4


                            case 3680 | 15:
                                WriteYM2413Register(0x37, 0xff);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x38, 0xff);
                                WriteYM2413Register(0x18, 0xc0);
                                break;


                            case 3680 | 14:   
                                WriteYM2413Register(0x37, 0xef);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x38, 0xfe);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 3680 | 13:
                                WriteYM2413Register(0x37, 0xdf);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x38, 0xfd);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 3680 | 12:
                                WriteYM2413Register(0x37, 0xcf);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x38, 0xfc);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 3680 | 11:
                                WriteYM2413Register(0x37, 0xbf);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x38, 0xfb);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 3680 | 10:
                                WriteYM2413Register(0x37, 0xaf);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x38, 0xfa);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 3680 | 9:
                                WriteYM2413Register(0x37, 0x9f);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x38, 0xf9);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 3680 | 8:
                                WriteYM2413Register(0x37, 0x8f);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x38, 0xf8);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 3680 | 7:
                                WriteYM2413Register(0x37, 0x7f);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x38, 0xf7);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 3680 | 6:
                                WriteYM2413Register(0x37, 0x6f);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x38, 0xf6);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 3680 | 5:
                                WriteYM2413Register(0x37, 0x5f);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x38, 0xf5);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 3680 | 4:
                                WriteYM2413Register(0x37, 0x4f);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x38, 0xf4);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 3680 | 3:
                                WriteYM2413Register(0x37, 0x3f);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x38, 0xf3);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 3680 | 2:
                                WriteYM2413Register(0x37, 0x2f);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x38, 0xf2);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 3680 | 1:
                                WriteYM2413Register(0x37, 0x1f);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x38, 0xf1);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                            case 3680 | 0:
                                WriteYM2413Register(0x37, 0x0f);
                                WriteYM2413Register(0x17, 0x50);
                                WriteYM2413Register(0x38, 0xf0);
                                WriteYM2413Register(0x18, 0xc0);
                                break;

                        }



                    }


                }



                base.UpdateAPU();
            }
        }
    }
}



