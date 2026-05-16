using System.Diagnostics;

namespace Amethyst.SymbolGenerator.Linking
{
    public static class Lib
    {
        public static readonly string ToolName = "lib.exe";

        private static string? _cachedVcInstallPath;
        private static string? _cachedLibExePath;

        public static string FindVCInstallPath()
        {
            if (_cachedVcInstallPath is not null) return _cachedVcInstallPath;
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string vswherePath = Path.Combine(programFilesX86, "Microsoft Visual Studio", "Installer", "vswhere.exe");

            if (!File.Exists(vswherePath))
                throw new FileNotFoundException("vswhere.exe not found. Visual Studio Installer is not present.");

            ProcessStartInfo psi = new()
            {
                FileName = vswherePath,
                Arguments = "-latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process proc = Process.Start(psi) ?? throw new Exception("Failed to run vswhere.exe");
            string output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();

            if (string.IsNullOrWhiteSpace(output))
                throw new Exception("No Visual Studio installation with VC++ tools found.");

            _cachedVcInstallPath = Path.Combine(output, "VC");
            return _cachedVcInstallPath;
        }

        private static string GetLibExePath()
        {
            if (_cachedLibExePath is not null) return _cachedLibExePath;

            string vcDirectory = FindVCInstallPath();
            if (!Directory.Exists(vcDirectory))
                throw new DirectoryNotFoundException($"Could not find Visual Studio VC directory at '{vcDirectory}'. Please ensure Visual Studio with C++ tools is installed.");

            string vcToolsVersionFile = Path.Combine(vcDirectory, "Auxiliary", "Build", "Microsoft.VCToolsVersion.default.txt");
            if (!File.Exists(vcToolsVersionFile))
                throw new FileNotFoundException($"Could not find Microsoft.VCToolsVersion.default.txt at '{vcToolsVersionFile}'. Please ensure the C++ toolchain is installed.");

            string vcToolsVersion = File.ReadAllText(vcToolsVersionFile).Trim();
            string lib = Path.Combine(vcDirectory, "Tools", "MSVC", vcToolsVersion, "bin", "Hostx64", "x64", ToolName);
            if (!File.Exists(lib))
                throw new FileNotFoundException($"Could not find '{ToolName}' at '{lib}'.");
            _cachedLibExePath = lib;
            return lib;
        }

        public static Process GenerateLib(string defFile, string libPath)
        {
            string lib = GetLibExePath();

            ProcessStartInfo info = new()
            {
                FileName = lib,
                Arguments = @$"/def:""{defFile}"" /out:""{libPath}"" /machine:x64",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = Process.Start(info) ??
                throw new Exception($"Failed to start lib.exe at '{lib}' with '{info.Arguments}'.");

            process.BeginOutputReadLine();
            process.OutputDataReceived += (sender, data) =>
            {
                if (!string.IsNullOrEmpty(data.Data))
                    Console.WriteLine(data.Data);
            };

            process.BeginErrorReadLine();
            return process;
        }
    }
}
