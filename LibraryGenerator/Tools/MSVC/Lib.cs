using System.Diagnostics;

namespace Amethyst.LibraryGenerator.Tools.MSVC
{
    public static class Lib
    {
        public static readonly string ToolName = "lib.exe";
        public static string VCDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Visual Studio/2022/Community/VC");

        public static Process GenerateLib(string defFile, string libPath)
        {
            if (!Directory.Exists(VCDirectory))
                throw new DirectoryNotFoundException($"Could not find Visual Studio VC directory at '{VCDirectory}'. Please ensure Visual Studio 2022 Community Edition is installed.");

            string vcToolsVersionFile = Path.Combine(VCDirectory, "Auxiliary/Build/Microsoft.VCToolsVersion.default.txt");
            if (!File.Exists(vcToolsVersionFile))
                throw new DirectoryNotFoundException($"Could not find Microsoft.VCToolsVersion.default.txt file at '{vcToolsVersionFile}'. Please ensure Visual Studio 2022 Community Edition is installed with the C++ toolchain.");

            string vcToolsVersion = File.ReadAllText(vcToolsVersionFile).Trim();
            string lib = Path.Combine(VCDirectory, "Tools", "MSVC", vcToolsVersion, "bin", "Hostx64", "x64", ToolName);

            ProcessStartInfo info = new()
            {
                FileName = lib,
                Arguments = @$"/def:""{defFile}"" /out:""{libPath}"" /machine:x64",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process? process = Process.Start(info) ?? 
                throw new Exception($"Failed to start lib.exe at '{lib}' with '{info.Arguments}'.");

            process.BeginOutputReadLine();
            process.OutputDataReceived += (object sender, DataReceivedEventArgs data) =>
            {
                if (data.Data is null)
                    return;
                Console.WriteLine(data.Data);
            };
            return process;
        }
    }
}
