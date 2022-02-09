using System;
using System.Diagnostics;

namespace IGameInstaller.Helper
{
    public class ProcessHelper
    {
        public static void StartProcess(string path, string args, bool useShell = false, bool wait = true, string workDirecotry = "", string verb = "runas")
        {
            using var process = new Process();
            process.StartInfo.FileName = path;
            process.StartInfo.UseShellExecute = useShell;
            process.StartInfo.WorkingDirectory = workDirecotry;
            process.StartInfo.Verb = verb;
            process.StartInfo.Arguments = args;
            if (!useShell && wait)
            {
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    throw new Exception($"输出信息: {stdout}\n错误信息: {stderr}");
                }
            } else
            {
                process.Start();
            }
        }
    }
}
