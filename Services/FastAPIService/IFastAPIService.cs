using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHomeStudio.Services
{
    public interface IFastAPIService
    {

        Task StartAsync(int port);
        Task<bool> WaitForServerAsync(int port, int timeoutSeconds = 15);


    }
}
