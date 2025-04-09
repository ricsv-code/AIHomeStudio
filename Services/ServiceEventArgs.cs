using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHomeStudio.Services
{
    public class ServiceEventArgs : EventArgs
    {


        public ServiceEventType EventType { get; }
        public string Message { get; }


        public ServiceEventArgs(ServiceEventType eventType, string message)
        {
            EventType = eventType;
            Message = message;
        }
    }
    
    public enum ServiceType
    {
        None,
        AI,
        STT,
        TTS,
        API,
        Cloud
    }

    public enum ServiceEventType
    {
        RequestSent,
        TokenReceived,
        LoadProgress,
        Error,
        Info
    }
}
