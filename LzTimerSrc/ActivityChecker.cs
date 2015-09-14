using System;
using System.Runtime.InteropServices;

namespace kkot.LzTimer
{
    public interface Clock
    {
        DateTime CurrentTime();
    }

    public class SystemClock : Clock
    {
        public DateTime CurrentTime()
        {
            return DateTime.Now;
        }
    }

    public class ActivityChecker
    {
        private readonly LastActivityProbe probe;
        private readonly Clock clock;

        private ActivityPeriodsListener listener;
        private int? lastInputTick;
        private DateTime lastCheckTime;

        public ActivityChecker(LastActivityProbe probe, Clock clock)
        {
            this.clock = clock;
            this.probe = probe;
        }

        public void SetActivityListner(ActivityPeriodsListener listener)
        {
            this.listener = listener;
        }

        public void Check()
        {
            if (lastInputTick == null || IsAfterWakeUp())
            {
                SaveLastInputTick();
                return;
            }

            var active = (lastInputTick != probe.GetLastInputTick());

            var now = clock.CurrentTime();
            listener.PeriodPassed(ActivityPeriod.Create(active, now - TimeSpanSinceLastCheck(), now));
            
            SaveLastInputTick();
        }

        private bool IsAfterWakeUp()
        {
            return TimeSpanSinceLastCheck().Duration() > 2.s();
        }

        private TimeSpan TimeSpanSinceLastCheck()
        {
            return clock.CurrentTime() - lastCheckTime;
        }

        private void SaveLastInputTick()
        {
            lastInputTick = probe.GetLastInputTick();
            lastCheckTime = clock.CurrentTime();
        }
    }   

    public interface LastActivityProbe
    {
        int GetLastInputTick();
    }

    public class Win32LastActivityProbe : LastActivityProbe
    {
        public int GetLastInputTick()
        {
            int lastInputTicks = 0;

            PInvoke.LASTINPUTINFO lastInputInfo = new PInvoke.LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            if (PInvoke.GetLastInputInfo(ref lastInputInfo))
            {
                lastInputTicks = (int)lastInputInfo.dwTime;
            }
            else
            {
                throw new Exception();
            }
            return lastInputTicks;
        }
    }

    public class PInvoke
    {
        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        public struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool DestroyIcon(IntPtr handle);
    }
}