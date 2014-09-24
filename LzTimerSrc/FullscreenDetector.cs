﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace kkot.LzTimer
{
    class FullscreenDetector
    {
        private int scrX, scrY;

        public FullscreenDetector()
        {
            scrX = GetSystemMetrics(SM_CXSCREEN);
            scrY = GetSystemMetrics(SM_CYSCREEN);
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int smIndex);

        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out W32RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct W32RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public bool IsForegroundWindowFullScreen()
        {
            IntPtr handle = GetForegroundWindow();
            if (handle == IntPtr.Zero) return false;

            W32RECT wRect;
            if (!GetWindowRect(handle, out wRect)) return false;

            return scrX == (wRect.Right - wRect.Left) && scrY == (wRect.Bottom - wRect.Top);
        }

    }
}
