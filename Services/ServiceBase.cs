using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHomeStudio.Services
{
    public abstract class ServiceBase
    {
        public event EventHandler<ServiceEventArgs>? OnServiceEvent;

        public ServiceType ServiceType { get; }

        protected ServiceBase(ServiceType type)
        {
            ServiceType = type;
        }

        protected void RaiseEvent(ServiceEventType eventType, string message)
        {
            OnServiceEvent?.Invoke(this, new ServiceEventArgs(eventType, message));
        }
    }

}
