using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHomeStudio.Utilities
{
    public static class UIHooks
    {
        public static Action<string>? OnSplashUpdate;

        public static void SplashLog(string msg)
        {
            Console.WriteLine($"[Init] {msg}");
            OnSplashUpdate?.Invoke(msg);
        }

    }
}
