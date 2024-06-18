using System.Diagnostics;
using System.Text;
using Cysharp.Threading.Tasks;

namespace ExcelDataSerializer.Util;

public static class ProcessUtil
{
    public static async UniTask RunAsync(RequestInfo requestInfo)
    {
        var request = new Request(requestInfo);
        try
        {
            var info = new ProcessStartInfo
            {
                FileName = requestInfo.Exec,
                Arguments = requestInfo.Argument,
                UseShellExecute = false,
                CreateNoWindow = true,

                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardInputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
            };

            if (!string.IsNullOrWhiteSpace(requestInfo.WorkingDirectory))
                info.WorkingDirectory = requestInfo.WorkingDirectory;
            
            var process = Process.Start(info);
            if (process == null)
                return;

            while (!process.HasExited)
            {
                await UniTask.Yield();
            }

            var error = await process.StandardError.ReadToEndAsync();
            var output = await process.StandardOutput.ReadToEndAsync();
            request.RequestInfo.ErrorDataReceived?.Invoke(error);
            request.RequestInfo.OutputDataReceived?.Invoke(output);

            await process.WaitForExitAsync();
        }
        catch (Exception e)
        {
            request.RequestInfo.ErrorDataReceived?.Invoke(e.Message);
        }
        finally
        {
            request.RequestInfo.Exited?.Invoke();
        }
    }

    private class Request
    {
        public int Id;
        public RequestInfo RequestInfo;

        private static int _id = 0;
        public Request(RequestInfo requestInfo)
        {
            Id = _id++;
            RequestInfo = requestInfo;
        }
    }
    
    public struct RequestInfo
    {
        public string Exec;
        public string Argument;
        public Action? Exited;
        public Action<string>? ErrorDataReceived;
        public Action<string>? OutputDataReceived;
        public string WorkingDirectory;
        public void Print()
        {
            Console.WriteLine($"[Request] Exec: {Exec}, Argument: {Argument}");
        }
    }
}