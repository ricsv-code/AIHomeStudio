using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHomeStudio.Utilities
{
    public static class ConfigHelper
    {
        private static IServiceProvider _serviceProvider;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static AppConfiguration GetConfig()
        {
            return _serviceProvider.GetRequiredService<AppConfiguration>();
        }

        public static string GetApiKeyStorageKey()
        {
            return GetConfig().MauiStorageKey;
        }
    }
}
