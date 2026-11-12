using System;
using System.Drawing;
using System.Windows.Threading;
using BSCustomClevoController.Controls;

namespace BSCustomClevoController.Utility
{
    internal class Structs
    {
        public struct KeyboardStatus
        {
            public Color BackLight;
            public KeyboardEffect Effect;
            public bool LightState;
            public bool BootEffect;
            public int SleepSecond;
            public bool SleepState;
        }

        public class KeyboardHardwareCapabilities
        {
            public bool SupportsBacklight { get; set; }
            public bool SupportsRgb { get; set; }
            public bool SupportsHardwareEffects { get; set; }
            public bool SupportsMultiZone { get; set; }
            public bool SupportsBootEffectOverride { get; set; }
            public bool SupportsIteDirectHid { get; set; }
            public bool SupportsLightBar { get; set; }
            public int MaxBrightnessLevels { get; set; }
            public string KeyboardTypeName { get; set; }
            public byte RawKbType { get; set; }

            public KeyboardHardwareCapabilities()
            {
                SupportsBacklight = true;
                SupportsRgb = true;
                SupportsHardwareEffects = true;
                SupportsMultiZone = true;
                SupportsBootEffectOverride = true;
                SupportsIteDirectHid = false;
                SupportsLightBar = false;
                MaxBrightnessLevels = 4;
                KeyboardTypeName = "RGB Keyboard";
                RawKbType = 2;
            }
        }

        public class KeyboardEffect
        {
            public byte UniqId { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
            public int Milisecons { get; set; }
            public byte IteAnimMode { get; set; }
            public byte IteBehaviour { get; set; }
            public Action<object, EventArgs> Func { get; set; }

            public bool IsIteHardwareEffect => IteAnimMode > 0;

            private DispatcherTimer _Timer;
            public DispatcherTimer Timer
            {
                get
                {
                    if (_Timer == null && Func != null)
                    {
                        _Timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Milisecons > 0 ? Milisecons : 16) };
                        _Timer.Tick += new EventHandler(Func);
                    }
                    return _Timer;
                }
            }

            public static KeyboardEffect LookUpEffect(byte mode)
            {
                if (mode == 8) return FindSoftwareEffect();
                foreach (KeyboardEffect effect in KeyboardEffects.Effects)
                {
                    if (effect.UniqId == mode) return effect;
                }
                return null;
            }

            public static KeyboardEffect LookUpEffect(int id, byte uniqId, string name = null)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    foreach (KeyboardEffect effect in KeyboardEffects.Effects)
                    {
                        if (string.Equals(effect.Name, name, StringComparison.OrdinalIgnoreCase) && effect.Id == id)
                        {
                            return effect;
                        }
                    }
                }
                foreach (KeyboardEffect effect in KeyboardEffects.Effects)
                {
                    if (effect.Id == id && effect.UniqId == uniqId) return effect;
                }
                foreach (KeyboardEffect effect in KeyboardEffects.Effects)
                {
                    if (effect.Id == id) return effect;
                }
                return null;
            }

            public static KeyboardEffect LookUpEffectById(int id)
            {
                foreach (KeyboardEffect effect in KeyboardEffects.Effects)
                {
                    if (effect.Id == id) return effect;
                }
                return null;
            }

            public static KeyboardEffect FindSoftwareEffect()
            {
                foreach (KeyboardEffect effect in KeyboardEffects.Effects)
                {
                    if (effect.UniqId == 0 && effect._Timer != null && effect._Timer.IsEnabled) return effect;
                }
                return null;
            }

            public static void StopAllSoftwareEffects()
            {
                foreach (KeyboardEffect effect in KeyboardEffects.Effects)
                {
                    if (effect.UniqId == 0 && effect._Timer != null && effect._Timer.IsEnabled)
                    {
                        effect._Timer.Stop();
                    }
                }
            }

            public bool IsSoftware => UniqId == 0 && !IsIteHardwareEffect;

            public static bool IsAnySoftwareEffectRunning()
            {
                foreach (KeyboardEffect effect in KeyboardEffects.Effects)
                {
                    if (effect.UniqId == 0 && effect._Timer != null && effect._Timer.IsEnabled)
                    {
                        return true;
                    }
                }
                return false;
            }
            public string TypeLabel => IsSoftware ? "(Software)" : "(Hardware)";
            public string DisplayName => $"{Name} {TypeLabel}";

            public override string ToString()
            {
                return DisplayName;
            }

            public static int CurrentAnimationSpeed = 2; // 1 = Slow, 2 = Normal, 3 = Fast

            public static byte GetIteSpeedByte()
            {
                return CurrentAnimationSpeed == 1 ? (byte)0x08 : (CurrentAnimationSpeed == 3 ? (byte)0x02 : (byte)0x05);
            }

            public void SetEffect()
            {
                StopAllSoftwareEffects();
                if (IsIteHardwareEffect || UniqId != 0)
                {
                    KeyboardBackLight.SetMode(this);
                }
                else
                {
                    KeyboardEffects.ResetTime();
                    Timer?.Start();
                }
            }
        }

        public struct FanStatus
        {
            public Fan.Mode Mode;
            public int Offset;
            public FanInfo Fan_CPU;
            public FanInfo Fan_GPU1;
            public FanInfo Fan_GPU2;
            public bool HasThirdFan;
        }

        public struct FanInfo
        {
            public byte T1;
            public byte T2;
            public byte T3;
            public byte T4;
            public byte T2_Default;
            public byte T3_Default;
            public byte D1;
            public byte D2;
            public byte D3;
            public byte D4;
            public byte D2_Default;
            public byte D3_Default;
            public int R12;
            public int R23;
            public int R34;
            public int rpm;
            public int remote;
            public int percent;

            public FanInfo(int T1, int D1, int T2, int D2, int T3, int D3, int T4 = 100, int D4 = 100)
            {
                this.T1 = (byte)T1;
                this.T2 = (byte)T2;
                this.T3 = (byte)T3;
                this.T4 = (byte)T4;
                this.D1 = (byte)D1;
                this.D2 = (byte)D2;
                this.D3 = (byte)D3;
                this.D4 = (byte)D4;

                T2_Default = 0;
                T3_Default = 0;
                D2_Default = 0;
                D3_Default = 0;
                R12 = 0;
                R23 = 0;
                R34 = 0;
                rpm = 0;
                remote = 0;
                percent = 0;
            }
        }
    }
}
