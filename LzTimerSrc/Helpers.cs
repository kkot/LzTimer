﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace kkot.LzTimer
{
    class Helpers
    {
        public static string NowShortDateString()
        {
            DateTime nowDate = DateTime.Now;

            // przed 5 rano To jeszcze wczoraj
            if (nowDate.Hour < 5)
            {
                TimeSpan ds = new TimeSpan(24, 0, 0);
                nowDate = nowDate.Subtract(ds);
            }

            String date = nowDate.ToShortDateString();
            return date;
        }

        public static string SecondsToHMS(int allSeconds)
        {
            int hours = allSeconds / (60 * 60);
            int minutes = (allSeconds - (hours * 60 * 60)) / 60;
            int secunds = allSeconds - (hours * 60 * 60) - (minutes * 60);

            string result = String.Format("{0,2} h {1,2} m {2,2} s", new object[] { hours, minutes, secunds });
            return result;
        }

        public static int GetLastInputTicks()
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