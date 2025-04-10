using System;
using System.Diagnostics;
using System.Net.Http;
using AIHomeStudio.Utilities;

namespace AIHomeStudio.Services
{
    public class FastAPIService : IFastAPIService, IDisposable
    {

        #region Fields

        private Process? _fastApiProcess;
        private int _port;

        #endregion

        #region Constructor
        public FastAPIService(int port = 8000)
        {
            _port = port;
        }

        #endregion

        #region Methods

        public async Task StartAsync(int port)
        {
            Logger.Log("Starting FastAPI service...", this, true);

            KillProcessUsingPort(port);

            StartFastApiServer(port);

            Logger.Log("Waiting for FastAPI server..", this, true);
            bool serverReady = await WaitForServerAsync(port);
            if (!serverReady)
            {
                Logger.Log("FastAPI server did not respond within the timeout period.", this, true);
            }
            else
            {
                Logger.Log("FastAPI server is up and running.", this, true);
            }
        }

        private void StartFastApiServer(int port)
        {
            try
            {
                string workingDir = Path.Combine(AppContext.BaseDirectory, "Python");
                string pythonExe = Path.Combine(workingDir, ".venv", "Scripts", "python.exe");

                if (!File.Exists(pythonExe))
                {
                    Logger.Log($"Python executable not found: {pythonExe}", this, true);
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"-m uvicorn api_server:app --host 127.0.0.1 --port {port}",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                _fastApiProcess = Process.Start(startInfo);
                if (_fastApiProcess == null)
                {
                    Logger.Log("Failed to start FastAPI process.", this, true);
                    return;
                }

                _fastApiProcess.OutputDataReceived += (s, e) => { if (e.Data != null) Logger.Log("[PY OUT] " + e.Data, this, false); };
                _fastApiProcess.ErrorDataReceived += (s, e) => { if (e.Data != null) Logger.Log("[PY ERR] " + e.Data, this, false); };

                _fastApiProcess.BeginOutputReadLine();
                _fastApiProcess.BeginErrorReadLine();

                Logger.Log("FastAPI server has been started.", this, true);
            }
            catch (Exception ex)
            {
                Logger.Log("Exception occurred while starting FastAPI server: " + ex.Message, this, true);
            }
        }

        public async Task<bool> WaitForServerAsync(int port, int timeoutSeconds = 15)
        {
            var httpClient = new HttpClient();
            string url = $"http://localhost:{port}/health";
            Logger.Log("Checking if FastAPI server is responding at " + url, this, false);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < timeoutSeconds)
            {
                try
                {
                    var response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        Logger.Log("FastAPI server responded with status code " + response.StatusCode, this, false);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Attempting to reach FastAPI server, error: " + ex.Message, this, false);
                }

                await Task.Delay(500);
            }

            Logger.Log("Timeout while trying to reach FastAPI server.", this, true);
            return false;
        }

        private void KillProcessUsingPort(int port)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C netstat -ano | findstr :{port}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    Logger.Log("Failed to start cmd.exe to search for processes on the port.", this, true);
                    return;
                }

                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                if (string.IsNullOrWhiteSpace(output))
                {
                    Logger.Log($"No process found on port {port}.", this, true);
                    return;
                }

                Logger.Log("Process found on port: " + port, this, false);

                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("LISTENING"))
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (int.TryParse(parts.Last(), out int pid))
                        {
                            try
                            {
                                using var toKill = Process.GetProcessById(pid);
                                toKill.Kill(true);
                                Logger.Log($"Process with PID {pid} killed on port {port}.", this, true);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log($"Failed to kill process with PID {pid}: " + ex.Message, this, true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error while attempting to kill processes on port: " + ex.Message, this, true);
            }
        }

        private void Cleanup()
        {
            try
            {
                if (_fastApiProcess != null && !_fastApiProcess.HasExited)
                {
                    try
                    {
                        Logger.Log("Attempting to shut down FastAPI server...", this, true);
                        if (!_fastApiProcess.CloseMainWindow())
                        {
                            _fastApiProcess.Kill(true);
                            Logger.Log("FastAPI server was forcefully killed using Kill(true).", this, true);
                        }
                        if (!_fastApiProcess.WaitForExit(5000))
                        {
                            Logger.Log("FastAPI server did not exit within 5000 ms.", this, true);
                        }
                        else
                        {
                            Logger.Log("FastAPI server shut down properly.", this, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Error during FastAPI server shutdown: " + ex.Message, this, true);
                    }
                    finally
                    {
                        if (_fastApiProcess != null)
                        {
                            string errorOutput = _fastApiProcess.StandardError.ReadToEnd();
                            if (!string.IsNullOrEmpty(errorOutput))
                            {
                                Logger.Log("Error output from FastAPI server during shutdown: " + errorOutput, this, true);
                            }
                            _fastApiProcess.Dispose();
                            _fastApiProcess = null;
                            Logger.Log("FastAPI process resources have been released.", this, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Critical error during cleanup of FastAPI server: " + ex.Message, this, true);
            }
        }

        private async Task RequestShutdownAsync(int port)
        {
            try
            {
                var httpClient = new HttpClient();
                string shutdownUrl = $"http://localhost:{port}/shutdown";
                Logger.Log("Sending shutdown request to FastAPI server...", this, true);
                var response = await httpClient.PostAsync(shutdownUrl, null);
                if (response.IsSuccessStatusCode)
                {
                    Logger.Log("FastAPI server acknowledged shutdown request.", this, true);
                }
                else
                {
                    Logger.Log("FastAPI server did not acknowledge shutdown request.", this, true);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error sending shutdown request: " + ex.Message, this, true);
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Logger.Log("Dispose called with disposing=true.", this, true);

                RequestShutdownAsync(_port).GetAwaiter().GetResult();
            }
            Cleanup();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~FastAPIService()
        {
            Dispose(false);
        }

        #endregion
    }
}
