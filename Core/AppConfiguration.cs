using AIHomeStudio.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHomeStudio
{
    public class AppConfiguration
    {

        public AppConfiguration()
        {
            Logger.Log("Initializing..", this, true);
        }



        public string MauiStorageKey { get; set; }


        public string LocalAPIBaseUrl { get; set; } = "http://localhost:8000"; 
        public string CloudAPIBaseUrl { get; set; } = "http://your-cloud-vm.com:8000";
        public string CurrentAPIBaseUrl { get; set; }


    }
}
