using System;
using System.Drawing;
using BSCustomClevoController.Utility;

namespace BSCustomClevoController.Controls
{
    internal static class KeyboardEffects
    {
        private const string AppRegistryKey = @"HKEY_CURRENT_USER\Software\BSCustomClevoController";

        public static int Duration = 1000;

        public static KeyboardEffect Random = new KeyboardEffect() { Name = "Random", UniqId = 112, Id = 0 };
        public static KeyboardEffect Breath = new KeyboardEffect() { Name = "Breath", UniqId = 16, Id = 2, IteAnimMode = 0x02 };
        public static KeyboardEffect Cycle = new KeyboardEffect() { Name = "Cycle", UniqId = 51, Id = 3 };
        public static KeyboardEffect Wave = new KeyboardEffect() { Name = "Wave", UniqId = 176, Id = 4, IteAnimMode = 0x03, IteBehaviour = 0x01 };
        public static KeyboardEffect Dance = new KeyboardEffect() { Name = "Dance", UniqId = 128, Id = 5 };
        public static KeyboardEffect Tempo = new KeyboardEffect() { Name = "Tempo", UniqId = 144, Id = 6 };
        public static KeyboardEffect Flash = new KeyboardEffect() { Name = "Flash", UniqId = 160, Id = 7 };

        // ITE 8291 multi-directional wave (tuxedo-drivers ite_8291.c)
        public static KeyboardEffect WaveLeftRight = new KeyboardEffect() { Name = "Wave: Left to Right", UniqId = 176, Id = 4, IteAnimMode = 0x03, IteBehaviour = 0x01 };
        public static KeyboardEffect WaveRightLeft = new KeyboardEffect() { Name = "Wave: Right to Left", UniqId = 176, Id = 11, IteAnimMode = 0x03, IteBehaviour = 0x02 };
        public static KeyboardEffect WaveBottomTop = new KeyboardEffect() { Name = "Wave: Bottom to Top", UniqId = 176, Id = 12, IteAnimMode = 0x03, IteBehaviour = 0x03 };
        public static KeyboardEffect WaveTopBottom = new KeyboardEffect() { Name = "Wave: Top to Bottom", UniqId = 176, Id = 13, IteAnimMode = 0x03, IteBehaviour = 0x04 };

        // ITE 8291 interactive and hardware engine modes
        public static KeyboardEffect Reactive = new KeyboardEffect() { Name = "Reactive (Key Press)", UniqId = 0, Id = 14, IteAnimMode = 0x04, IteBehaviour = 0x00 };
        public static KeyboardEffect Ripple = new KeyboardEffect() { Name = "Ripple (Key Waves)", UniqId = 0, Id = 15, IteAnimMode = 0x06, IteBehaviour = 0x00 };
        public static KeyboardEffect Rainbow = new KeyboardEffect() { Name = "Rainbow Wave", UniqId = 51, Id = 16, IteAnimMode = 0x05, IteBehaviour = 0x00 };
        public static KeyboardEffect Marquee = new KeyboardEffect() { Name = "Marquee", UniqId = 128, Id = 17, IteAnimMode = 0x09, IteBehaviour = 0x00 };
        public static KeyboardEffect Raindrop = new KeyboardEffect() { Name = "Raindrop", UniqId = 112, Id = 18, IteAnimMode = 0x0A, IteBehaviour = 0x00 };
        public static KeyboardEffect Aurora = new KeyboardEffect() { Name = "Aurora", UniqId = 144, Id = 19, IteAnimMode = 0x0E, IteBehaviour = 0x00 };
        public static KeyboardEffect Spark = new KeyboardEffect() { Name = "Spark", UniqId = 160, Id = 20, IteAnimMode = 0x11, IteBehaviour = 0x00 };

        public static KeyboardEffect SoftwareBreath = new KeyboardEffect() { Name = "Breath", UniqId = 0, Id = 8, Func = BreathEffect, Milliseconds = 16 };
        public static KeyboardEffect SoftwareCycle = new KeyboardEffect() { Name = "Cycle", UniqId = 0, Id = 9, Func = CycleEffect, Milliseconds = 16 };
        public static KeyboardEffect SoftwareBreathColor = new KeyboardEffect() { Name = "Colorful Breath", UniqId = 0, Id = 10, Func = ColorfulBreathEffect, Milliseconds = 16 };

        public static KeyboardEffect[] Effects = new KeyboardEffect[]
        {
            Random, Breath, Cycle, Wave, Dance, Tempo, Flash,
            WaveLeftRight, WaveRightLeft, WaveBottomTop, WaveTopBottom,
            Reactive, Ripple, Rainbow, Marquee, Raindrop, Aurora, Spark,
            SoftwareBreath, SoftwareCycle, SoftwareBreathColor
        };

        public static KeyboardEffect[] GetAvailableEffects(KeyboardHardwareCapabilities caps, bool allowSoftwareEffects = false)
        {
            if (caps == null) return Effects;

            var list = new System.Collections.Generic.List<KeyboardEffect>();

            if (caps.SupportsHardwareEffects)
            {
                list.Add(Random);
                list.Add(Breath);
                list.Add(Cycle);

                if (caps.SupportsMultiZone)
                {
                    list.Add(WaveLeftRight);
                    list.Add(WaveRightLeft);
                    list.Add(WaveBottomTop);
                    list.Add(WaveTopBottom);
                }

                list.Add(Dance);
                list.Add(Tempo);
                list.Add(Flash);

                if (caps.SupportsIteDirectHid)
                {
                    list.Add(Reactive);
                    list.Add(Ripple);
                    list.Add(Rainbow);
                    list.Add(Marquee);
                    list.Add(Raindrop);
                    list.Add(Aurora);
                    list.Add(Spark);
                }
            }

            if (allowSoftwareEffects && !IsRunningOnBattery && caps.SupportsBacklight)
            {
                if (!caps.SupportsHardwareEffects)
                {
                    list.Add(SoftwareBreath);
                }

                if (caps.SupportsRgb)
                {
                    if (!caps.SupportsHardwareEffects)
                    {
                        list.Add(SoftwareCycle);
                    }
                    list.Add(SoftwareBreathColor);
                }
            }

            return list.ToArray();
        }

        public static bool IsAnySoftwareEffectActive()
        {
            try
            {
                if (KeyboardEffect.IsAnySoftwareEffectRunning())
                {
                    return true;
                }

                object regActive = Microsoft.Win32.Registry.GetValue(AppRegistryKey, "KbEffectActive", null);
                object regSoft = Microsoft.Win32.Registry.GetValue(AppRegistryKey, "KbEffectIsSoftware", null);
                if (regActive is int a && a == 1 && regSoft is int s && s == 1)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log("KeyboardEffects.IsAnySoftwareEffectActive", ex);
            }
            return false;
        }

        public static bool IsRunningOnBattery
        {
            get
            {
                try
                {
                    return System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus != System.Windows.Forms.PowerLineStatus.Online;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static KeyboardEffect SavedBatterySoftwareEffect { get; set; }

        private static void RestoreStaticLightingAndSleepTimer()
        {
            KeyboardStatus status = KeyboardBackLight.GetStatus();
            if (status.LightState)
            {
                KeyboardBackLight.TurnOn();
                byte bri = status.BackLight.A > 0 ? status.BackLight.A : (byte)255;
                MaxBrightness = bri;
                KeyboardBackLight.ApplyCurrentStaticColorsRealtime();
                KeyboardBackLight.SetBrightnessRealtime(bri);
            }
            else
            {
                KeyboardBackLight.TurnOff();
            }

            int sleepSec = 30;
            object regUserSleep = Microsoft.Win32.Registry.GetValue(AppRegistryKey, "KbUserSleepTimeout", null);
            if (regUserSleep is int us && us > 0)
            {
                sleepSec = us;
            }
            KeyboardBackLight.SetSleepTimer(sleepSec);
        }

        public static void DisableSoftwareAnimationForExit()
        {
            try
            {
                KeyboardEffect.StopAllSoftwareEffects();
                RestoreStaticLightingAndSleepTimer();
            }
            catch (Exception ex)
            {
                AppLogger.Log("KeyboardEffects.DisableSoftwareAnimationForExit", ex);
            }
        }

        public static void HandlePowerStatusChanged(bool isAcOnline)
        {
            try
            {
                if (!isAcOnline)
                {
                    if (IsAnySoftwareEffectActive())
                    {
                        KeyboardEffect runningEffect = KeyboardEffect.FindSoftwareEffect();
                        if (runningEffect == null)
                        {
                            object regId = Microsoft.Win32.Registry.GetValue(AppRegistryKey, "KbEffectId", null);
                            if (regId is int idVal)
                            {
                                runningEffect = KeyboardEffect.LookUpEffectById(idVal);
                            }
                        }

                        if (runningEffect != null && runningEffect.IsSoftware)
                        {
                            SavedBatterySoftwareEffect = runningEffect;
                        }

                        KeyboardEffect.StopAllSoftwareEffects();
                        RestoreStaticLightingAndSleepTimer();
                    }
                }
                else
                {
                    KeyboardEffect effectToResume = SavedBatterySoftwareEffect;
                    SavedBatterySoftwareEffect = null;

                    if (effectToResume == null)
                    {
                        object regActive = Microsoft.Win32.Registry.GetValue(AppRegistryKey, "KbEffectActive", null);
                        object regLight = Microsoft.Win32.Registry.GetValue(AppRegistryKey, "KbLightState", null);
                        bool lightOn = !(regLight is int l && l == 0);

                        if (regActive is int a && a == 1 && lightOn)
                        {
                            object regId = Microsoft.Win32.Registry.GetValue(AppRegistryKey, "KbEffectId", null);
                            object regUniq = Microsoft.Win32.Registry.GetValue(AppRegistryKey, "KbEffectUniqId", null);
                            object regName = Microsoft.Win32.Registry.GetValue(AppRegistryKey, "KbEffectName", null);

                            if (regId is int idVal)
                            {
                                byte uniqVal = regUniq is int u ? (byte)u : (byte)0;
                                string nameVal = regName as string;
                                KeyboardEffect savedEffect = KeyboardEffect.LookUpEffect(idVal, uniqVal, nameVal);
                                if (savedEffect != null && savedEffect.IsSoftware)
                                {
                                    effectToResume = savedEffect;
                                }
                            }
                        }
                    }

                    if (effectToResume != null && effectToResume.IsSoftware)
                    {
                        KeyboardBackLight.TurnSleepTimerOff(updateRegistry: false);
                        System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                        {
                            effectToResume.SetEffect();
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log("KeyboardEffects.HandlePowerStatusChanged", ex);
            }
        }

        private static readonly Color[] RainbowColors = new Color[]
        {
            Color.FromArgb(255, 0, 0),
            Color.FromArgb(255, 255, 0),
            Color.FromArgb(0, 255, 0),
            Color.FromArgb(0, 255, 255),
            Color.FromArgb(0, 0, 255),
            Color.FromArgb(255, 0, 255)
        };

        public static byte MaxBrightness = 255;
        private static int _breathColorIndex = -1;
        private static long _transitionStartTicks = MonotonicClock.NowTicks();
        private static int _lastBrightness = -1;
        private static int _lastR = -1;
        private static int _lastG = -1;
        private static int _lastB = -1;

        public static bool IsBreathingSoftwareEffectRunning()
        {
            KeyboardEffect running = KeyboardEffect.FindSoftwareEffect();
            return running != null && running.IsBreathingSoftwareEffect;
        }

        public static void ResetTime()
        {
            _transitionStartTicks = MonotonicClock.NowTicks();
            _breathColorIndex = -1;
            _lastBrightness = -1;
            _lastR = -1;
            _lastG = -1;
            _lastB = -1;
        }

        private static byte ComputeBreathBrightness(double elapsedMs, double cycleDurationMs)
        {
            double phase = (elapsedMs % cycleDurationMs) / cycleDurationMs;
            double progress = phase <= 0.5 ? (phase * 2.0) : ((1.0 - phase) * 2.0);
            double ease = (1.0 - Math.Cos(progress * Math.PI)) / 2.0;
            return (byte)Math.Round(ease * MaxBrightness);
        }

        public static void BreathEffect(object sender, EventArgs e)
        {
            double cycleDuration = Math.Max(200.0, Duration * 2.0);
            double elapsed = MonotonicClock.ElapsedMs(_transitionStartTicks);
            byte targetBrightness = ComputeBreathBrightness(elapsed, cycleDuration);

            if (targetBrightness != _lastBrightness)
            {
                _lastBrightness = targetBrightness;
                KeyboardBackLight.SetBrightnessRealtime(targetBrightness);
            }
        }

        public static void ColorfulBreathEffect(object sender, EventArgs e)
        {
            double cycleDuration = Math.Max(200.0, Duration * 2.0);
            double elapsed = MonotonicClock.ElapsedMs(_transitionStartTicks);
            int colorIdx = (int)(elapsed / cycleDuration) % RainbowColors.Length;
            byte targetBrightness = ComputeBreathBrightness(elapsed, cycleDuration);

            bool colorChanged = colorIdx != _breathColorIndex;
            if (colorChanged)
            {
                _breathColorIndex = colorIdx;
                Color baseColor = RainbowColors[_breathColorIndex];
                KeyboardBackLight.SetColourRealtime(baseColor.R, baseColor.G, baseColor.B);
            }

            if (colorChanged || targetBrightness != _lastBrightness)
            {
                _lastBrightness = targetBrightness;
                KeyboardBackLight.SetBrightnessRealtime(targetBrightness);
            }
        }

        private static Color InterpolateColor(Color start, Color end, float t)
        {
            int r = (int)(start.R + (end.R - start.R) * t);
            int g = (int)(start.G + (end.G - start.G) * t);
            int b = (int)(start.B + (end.B - start.B) * t);
            return Color.FromArgb(255, Clamp(r), Clamp(g), Clamp(b));
        }

        public static void CycleEffect(object sender, EventArgs e)
        {
            double stepDuration = Math.Max(100.0, Duration);
            double totalCycle = stepDuration * RainbowColors.Length;
            double elapsed = MonotonicClock.ElapsedMs(_transitionStartTicks) % totalCycle;

            int fromIndex = (int)(elapsed / stepDuration) % RainbowColors.Length;
            int toIndex = (fromIndex + 1) % RainbowColors.Length;
            float t = (float)((elapsed % stepDuration) / stepDuration);

            Color interpolated = InterpolateColor(RainbowColors[fromIndex], RainbowColors[toIndex], t);
            bool colorChanged = interpolated.R != _lastR || interpolated.G != _lastG || interpolated.B != _lastB;
            if (colorChanged)
            {
                _lastR = interpolated.R;
                _lastG = interpolated.G;
                _lastB = interpolated.B;
                KeyboardBackLight.SetColourRealtime(interpolated.R, interpolated.G, interpolated.B);
            }

            if (colorChanged || _lastBrightness != MaxBrightness)
            {
                _lastBrightness = MaxBrightness;
                KeyboardBackLight.SetBrightnessRealtime(MaxBrightness);
            }
        }

        private static int Clamp(int value) => Math.Max(0, Math.Min(255, value));
    }
}
