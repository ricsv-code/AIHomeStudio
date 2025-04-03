using AIHomeStudio.Models;
using Plugin.Maui.Audio;
using System.Diagnostics;
using System.Net.Http;

namespace AIHomeStudio.Utilities
{
    public class Initializer
    {
        private Process? _fastApiProcess;

        public async Task InitializeAllAsync(AIViewModel ai, STTViewModel stt, TTSViewModel tts, IAudioManager audioManager, int port)
        {
            KillProcessUsingPort(port);

            try
            {
                UIHooks.SplashLog("Preparing to start FastAPI server...");
                StartFastApiServer(port);

                UIHooks.SplashLog("Waiting for FastAPI to respond...");
                bool serverReady = await WaitForFastApiAsync(port);
                if (!serverReady)
                {
                    UIHooks.SplashLog("[ERROR] FastAPI did not respond. Aborting initialization.");
                    return;
                }

                InitializeServiceManager(audioManager, port);

                await LoadModelListsAsync(ai, stt, tts);
            }
            catch (Exception ex)
            {
                UIHooks.SplashLog($"[FATAL] Initialization failed: {ex.Message}");
            }
        }

        public void StartFastApiServer(int port)
        {


            try
            {
                string workingDir = Path.Combine(AppContext.BaseDirectory, "Python");
                string pythonExe = Path.Combine(workingDir, ".venv", "Scripts", "python.exe");

                var startInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"-m uvicorn APIServer:app --host 127.0.0.1 --port {port}",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false
                };

                _fastApiProcess = Process.Start(startInfo);

                _fastApiProcess.OutputDataReceived += (s, e) => { if (e.Data != null) UIHooks.SplashLog("[PY OUT] " + e.Data); };
                _fastApiProcess.ErrorDataReceived += (s, e) => { if (e.Data != null) UIHooks.SplashLog("[PY ERR] " + e.Data); };

                _fastApiProcess.BeginOutputReadLine();
                _fastApiProcess.BeginErrorReadLine();

                UIHooks.SplashLog("FastAPI-server started.");
            }
            catch (Exception ex)
            {
                UIHooks.SplashLog($"Failed to start FastAPI server: {ex.Message}");
            }
        }

        private async Task<bool> WaitForFastApiAsync(int port, int timeoutSeconds = 15)
        {
            var httpClient = new HttpClient();
            string url = $"http://localhost:{port}/health";

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < timeoutSeconds)
            {
                try
                {
                    var response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                        return true;
                }
                catch { }

                await Task.Delay(500);
            }

            return false;
        }

        public void InitializeServiceManager(IAudioManager audioManager, int port)
        {
            UIHooks.SplashLog("Initializing service manager...");
            ServiceManager.Initialize(audioManager, port);
        }

        public async Task LoadModelListsAsync(AIViewModel ai, STTViewModel stt, TTSViewModel tts)
        {
            try
            {
                UIHooks.SplashLog("Loading AI models...");
                var aiPath = Path.Combine(AppContext.BaseDirectory, "Python", "ai_models");
                ai.AvailableModels = FileManager.GetAllFolderNamesFrom(aiPath);

                UIHooks.SplashLog("Loading STT models...");
                stt.AvailableModels = await ServiceManager.STTService.GetAvailableModelsAsync();

                UIHooks.SplashLog("Loading TTS models...");
                tts.AvailableModels = await ServiceManager.TTSService.GetAvailableModelsAsync();

                UIHooks.SplashLog("All models loaded.");
            }
            catch (Exception ex)
            {
                UIHooks.SplashLog($"[ERROR] Failed to load model lists: {ex.Message}");
            }
        }

        public void Cleanup()
        {
            try
            {
                if (_fastApiProcess != null && !_fastApiProcess.HasExited)
                {
                    _fastApiProcess.Kill(true);
                    _fastApiProcess.Dispose();
                    UIHooks.SplashLog("FastAPI-server killed.");
                }
            }
            catch (Exception ex)
            {
                UIHooks.SplashLog($"Failed to kill FastAPI server: {ex.Message}");
            }
        }

        private void KillProcessUsingPort(int port)
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
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

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
                            var toKill = Process.GetProcessById(pid);
                            toKill.Kill(true);
                            UIHooks.SplashLog($"Killed process on port {port} (PID: {pid})");
                        }
                        catch (Exception ex)
                        {
                            UIHooks.SplashLog($"Failed to kill PID {pid}: {ex.Message}");
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                UIHooks.SplashLog($"No process is using port {port}.");
            }
        }


    }
}
