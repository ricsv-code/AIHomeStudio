using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHomeStudio.Utilities
{
    public static class Logger
    {
        public static Action<string>? OnSplashUpdate;

        public static void Log(string msg, object sender, bool showInSplash = false)
        {

            string name = sender.GetType().Name;

            Console.WriteLine($"[ {name} ] {msg}");
            Debug.WriteLine($"[ {name} ] {msg}");


            if (showInSplash)
                OnSplashUpdate?.Invoke(msg);
        }

        public static void DebugLog(string msg, object sender, bool wdup = false)
        {
            string name = sender.GetType().Name;
            Debug.WriteLine($"[ {name} ] {msg}");
        }

    }
}
