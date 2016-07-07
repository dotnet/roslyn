using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using static SignRoslyn.PathUtil;

namespace SignRoslyn
{
    internal abstract class SignTool 
    {
        internal string MSBuildPath { get; }
        internal string BinariesPath { get; }
        internal string SourcePath { get; }
        internal string AppPath { get; }

        internal SignTool(string appPath, string binariesPath, string sourcePath)
        {
            BinariesPath = binariesPath;
            SourcePath = sourcePath;
            AppPath = appPath;

            var path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            MSBuildPath = Path.Combine(path, @"MSBuild\14.0\Bin\MSBuild.exe");
            if (!File.Exists(MSBuildPath))
            {
                throw new Exception(@"Unable to locate MSBuild at the path {_msbuildPath}");
            }
        }

        internal abstract void RemovePublicSign(string assemblyPath);

        protected abstract int RunMSBuild(ProcessStartInfo startInfo);

        internal void Sign(IEnumerable<string> filePaths)
        {
            var buildFilePath = Path.Combine(AppPath, "build.proj");
            var commandLine = new StringBuilder();
            commandLine.Append(@"/v:m /target:RoslynSign ");
            commandLine.Append($@"""{buildFilePath}"" ");
            Console.WriteLine($"msbuild.exe {commandLine.ToString()}");

            var content = GenerateBuildFileContent(filePaths);
            File.WriteAllText(buildFilePath, content);
            Console.WriteLine("Generated project file");
            Console.WriteLine(content);

            var startInfo = new ProcessStartInfo()
            {
                FileName = MSBuildPath,
                Arguments = commandLine.ToString(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WorkingDirectory = AppPath,
            };

            var exitCode = RunMSBuild(startInfo);

            File.Delete(buildFilePath);

            if (exitCode != 0)
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

            AppendLine(builder, depth: 1, text: $@"<Import Project=""{Path.Combine(SourcePath, @"build\Targets\VSL.Settings.targets")}"" />");

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

            var intermediatesPath = Path.Combine(Path.GetDirectoryName(BinariesPath), "Obj");
            AppendLine(builder, depth: 1, text: @"<Target Name=""RoslynSign"">");
            AppendLine(builder, depth: 2, text: $@"<SignFiles Files=""@(FilesToSign)"" BinariesDirectory=""{BinariesPath}"" IntermediatesDirectory=""{intermediatesPath}"" Type=""real"" />");
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
    }
}
