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
            if (lastInputTick == null) // || IsAfterWakeUp())
            {
                SaveLastInputTick();
                return;
            }

            var active = (lastInputTick != probe.GetLastInputTick());

            var now = clock.CurrentTime();
            listener.PeriodPassed(Period.Create(active, now - RoundedTimeSpanSinceLastCheck(), now));
            
            SaveLastInputTick();
        }

        private TimeSpan RoundedTimeSpanSinceLastCheck()
        {
            return Round(TimeSpanSinceLastCheck(), 100.ms());
        }

        private bool IsAfterWakeUp()
        {
            return TimeSpanSinceLastCheck().Duration() > 2.s();
        }

        private TimeSpan Round(TimeSpan toRound, TimeSpan round)
        {
            long wholeParts = toRound.Ticks/round.Ticks;
            long remain = toRound.Ticks%round.Ticks;
            if (remain < round.Ticks/2)
                remain = 0;
            else
                remain = round.Ticks;

            TimeSpan rounded = new TimeSpan(wholeParts*round.Ticks + remain);
            return rounded;
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
}