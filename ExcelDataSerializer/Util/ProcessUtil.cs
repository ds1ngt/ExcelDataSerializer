using System.Diagnostics;
using System.Text;
using Cysharp.Threading.Tasks;

namespace ExcelDataSerializer.Util;

public static class ProcessUtil
{
#region Process
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
            };

            info = SetRedirect(requestInfo, info);

            if (!string.IsNullOrWhiteSpace(requestInfo.WorkingDirectory))
                info.WorkingDirectory = requestInfo.WorkingDirectory;
            
            var process = Process.Start(info);
            if (process == null)
                return;

            while (!process.HasExited)
            {
                await ProcessRedirectAsync(process, info, requestInfo);
                await UniTask.Yield();
            }

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
#endregion // Process

#region Redirect
    private static ProcessStartInfo SetRedirect(RequestInfo requestInfo, ProcessStartInfo info)
    {
        var redirectError = requestInfo.ErrorDataReceived != null;
        var redirectOutput = requestInfo.OutputDataReceived != null;
        if (redirectError)
        {
            info.RedirectStandardError = true;
            info.StandardErrorEncoding = Encoding.UTF8;
        }

        if (redirectOutput)
        {
            info.RedirectStandardOutput = true;
            info.StandardOutputEncoding = Encoding.UTF8;
        }

        return info;
    }

    private static async UniTask ProcessRedirectAsync(Process process, ProcessStartInfo info, RequestInfo requestInfo)
    {
        if (info.RedirectStandardError)
        {
            var error = await process.StandardError.ReadToEndAsync();
            requestInfo.ErrorDataReceived?.Invoke(error);
        }

        if (info.RedirectStandardOutput)
        {
            var output = await process.StandardOutput.ReadToEndAsync();
            requestInfo.OutputDataReceived?.Invoke(output);
        }
    }
#endregion // Redirect

#region Request
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
#endregion // Request
}