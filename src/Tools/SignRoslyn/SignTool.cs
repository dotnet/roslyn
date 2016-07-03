using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static SignRoslyn.PathUtil;

namespace SignRoslyn
{
    internal sealed class SignTool : ISignTool
    {
        private readonly string _msbuildPath;
        private readonly string _binariesPath;
        private readonly string _sourcePath;
        private readonly string _buildFilePath;
        private readonly string _runPath;
        private readonly bool _ignoreFailures;

        internal SignTool(string runPath, string binariesPath, string sourcePath, bool ignoreFailures)
        {
            _binariesPath = binariesPath;
            _sourcePath = sourcePath;
            _buildFilePath = Path.Combine(runPath, "build.proj");
            _runPath = runPath;
            _ignoreFailures = ignoreFailures;

            var path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            _msbuildPath = Path.Combine(path, @"MSBuild\14.0\Bin\MSBuild.exe");
            if (!File.Exists(_msbuildPath))
            {
                throw new Exception(@"Unable to locate MSBuild at the path {_msbuildPath}");
            }
        }

        private void Sign(IEnumerable<string> filePaths)
        {
            var commandLine = new StringBuilder();
            commandLine.Append(@"/v:m /target:RoslynSign ");
            commandLine.Append($@"""{_buildFilePath}"" ");
            Console.WriteLine($"msbuild.exe {commandLine.ToString()}");

            var content = GenerateBuildFileContent(filePaths);
            File.WriteAllText(_buildFilePath, content);
            Console.WriteLine("Generated project file");
            Console.WriteLine(content);

            var startInfo = new ProcessStartInfo()
            {
                FileName = _msbuildPath,
                Arguments = commandLine.ToString(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WorkingDirectory = _runPath,
            };

            var process = Process.Start(startInfo);
            process.OutputDataReceived += (sender, e) =>
            {
                Console.WriteLine(e.Data);
            };
            process.BeginOutputReadLine();
            process.WaitForExit();

            File.Delete(_buildFilePath);

            if (process.ExitCode != 0 && !_ignoreFailures)
            {
                Console.WriteLine("MSBuild failed!!!");
                throw new Exception("Sign failed");
            }
        }

        private string GenerateBuildFileContent(IEnumerable<string> filesToSign)
        {
            var builder = new StringBuilder();
            AppendLine(builder, depth: 0, text: @"<?xml version=""1.0"" encoding=""utf-8""?>");
            AppendLine(builder, depth: 0, text: @"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">");

            AppendLine(builder, depth: 1, text: $@"<Import Project=""{Path.Combine(_sourcePath, @"build\Targets\VSL.Settings.targets")}"" />");

            AppendLine(builder, depth: 1, text: $@"<Import Project=""$(NuGetPackageRoot)\MicroBuild.Core\0.2.0\build\MicroBuild.Core.props"" />");
            AppendLine(builder, depth: 1, text: $@"<Import Project=""$(NuGetPackageRoot)\MicroBuild.Core\0.2.0\build\MicroBuild.Core.targets"" />");

            AppendLine(builder, depth: 1, text: $@"<ItemGroup>");

            foreach (var fileToSign in filesToSign)
            {
                AppendLine(builder, depth: 2, text: $@"<FilesToSign Include=""{fileToSign}"">");

                if (IsAssembly(fileToSign))
                {
                    AppendLine(builder, depth: 3, text: @"<Authenticode>$(AuthenticodeCertificateName)</Authenticode>");
                    AppendLine(builder, depth: 3, text: @"<StrongName>MsSharedLib72</StrongName>");
                }
                else if (IsVsix(fileToSign))
                {
                    AppendLine(builder, depth: 3, text: @"<Authenticode>VsixSHA2</Authenticode>");
                }
                else
                {
                    Console.WriteLine($@"Unrecognized file type: {fileToSign}");
                }

                AppendLine(builder, depth: 2, text: @"</FilesToSign>");
            }

            AppendLine(builder, depth: 1, text: $@"</ItemGroup>");

            var intermediatesPath = Path.Combine(Path.GetDirectoryName(_binariesPath), "Obj");
            AppendLine(builder, depth: 1, text: @"<Target Name=""RoslynSign"">");
            AppendLine(builder, depth: 2, text: $@"<SignFiles Files=""@(FilesToSign)"" BinariesDirectory=""{_binariesPath}"" IntermediatesDirectory=""{intermediatesPath}"" Type=""real"" />");
            AppendLine(builder, depth: 1, text: @"</Target>");
            AppendLine(builder, depth: 0, text: @"</Project>");

            return builder.ToString();
        }

        private static void AppendLine(StringBuilder builder, int depth, string text)
        {
            for (int i = 0; i < depth; i++)
            {
                builder.Append("    ");
            }

            builder.AppendLine(text);
        }

        void ISignTool.Sign(IEnumerable<string> filePaths)
        {
            Sign(filePaths);
        }
    }
}
