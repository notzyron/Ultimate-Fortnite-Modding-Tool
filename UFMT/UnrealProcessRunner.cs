using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UFMT
{
    internal static class UnrealProcessRunner
    {
        private static string PythonScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", "UE_Import.py").Replace("\\", "/");
        internal static async Task LaunchUnreal(UnrealExportData unrealData, string ueProjectPath, string ueExecutablePath)
        {
            string jsonString = System.Text.Json.JsonSerializer.Serialize(unrealData, AppJsonContext.Default.UnrealExportData);
            string tempJsonPath = Path.Combine(Path.GetTempPath(), "ue_import_data.json");
            File.WriteAllText(tempJsonPath, jsonString, new System.Text.UTF8Encoding(false));

            string arguments = $"\"{ueProjectPath}\" -run=PythonScriptCommandlet -script=\"{PythonScriptPath}\" -NullRHI -NoWindow -Silent";

            Console.WriteLine($"Launching UE with args: {arguments}");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ueExecutablePath,
                Arguments = arguments,
                UseShellExecute = false, //This must be false for EnvironmentVariables to work
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            };

            startInfo.EnvironmentVariables["UFMT_JSON_PATH"] = tempJsonPath;
            Console.WriteLine("Launching unreal engine...");
            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                Task<string> stderrTask = process.StandardError.ReadToEndAsync();
                await Task.WhenAll(stdoutTask, stderrTask);
                await process.WaitForExitAsync();
                Console.WriteLine(stdoutTask.Result);
            }
        }
        internal static async Task CookFiles(string ueProjectPath, string ueExecutablePath)
        {
            Console.WriteLine("Cooking the newly created assets...");
            string arguments = $"\"{ueProjectPath}\" -run=Cook -TargetPlatform=WindowsNoEditor -unversioned -iterate -NullRHI -NoWindow -Silent";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ueExecutablePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                Task<string> stderrTask = process.StandardError.ReadToEndAsync();
                await Task.WhenAll(stdoutTask, stderrTask);
                await process.WaitForExitAsync();
                Console.WriteLine(stdoutTask.Result);
            }
            Console.WriteLine("Cook done!");
        }
    }
}
