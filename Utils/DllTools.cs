using System;
using System.Runtime.InteropServices;

namespace BonelabMultiplayerMockup.Utils
{
    public static class DllTools
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("Kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("Kernel32.dll")]
        public static extern uint GetLastError();

        public static T GetFunction<T>(string signature, IntPtr hModule) where T : Delegate
        {
            if (hModule == IntPtr.Zero) throw new ArgumentException("hModule was a nullptr!");

            var procAddress = GetProcAddress(hModule, signature);
            return Marshal.GetDelegateForFunctionPointer<T>(procAddress);
        }
    }
}