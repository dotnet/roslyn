using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Win32;
using Roslyn.Test.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public static class ExecutingTestUtils
    {
        public const string CompilerServerExeName = "VBCSCompiler.exe";
        public const string CSharpClientExeName = "csc.exe";
        public const string BasicClientExeName = "vbc.exe";
        public const string BuildTaskDllName = "Microsoft.Build.Tasks.CodeAnalysis.dll";

        public static string s_msbuildDirectory;
        public static string MSBuildDirectory
        {
            get
            {
                if (s_msbuildDirectory == null)
                {
                    var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\MSBuild\ToolsVersions\14.0", false);

                    if (key != null)
                    {
                        var toolsPath = key.GetValue("MSBuildToolsPath");
                        if (toolsPath != null)
                        {
                            s_msbuildDirectory = toolsPath.ToString();
                        }
                    }
                }
                return s_msbuildDirectory;
            }
        }

        public static string MSBuildExecutable { get; } = Path.Combine(MSBuildDirectory, "MSBuild.exe");

        public static readonly string s_workingDirectory = Directory.GetCurrentDirectory();
        public static string ResolveAssemblyPath(string exeName)
        {
            var path = Path.Combine(s_workingDirectory, exeName);
            if (File.Exists(path))
            {
                return path;
            }
            else
            {
                path = Path.Combine(MSBuildDirectory, exeName);
                if (File.Exists(path))
                {
                    var currentAssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
                    var loadedAssemblyVersion = Assembly.LoadFile(path).GetName().Version;
                    if (currentAssemblyVersion == loadedAssemblyVersion)
                    {
                        return path;
                    }
                }
                return null;
            }
        }

        public static readonly string s_compilerServerExecutableSrc = ResolveAssemblyPath(CompilerServerExeName);
        public static readonly string s_buildTaskDllSrc = ResolveAssemblyPath(BuildTaskDllName);
        public static readonly string s_csharpCompilerExecutableSrc = ResolveAssemblyPath("csc.exe");
        public static readonly string s_basicCompilerExecutableSrc = ResolveAssemblyPath("vbc.exe");
        public static readonly string s_microsoftCodeAnalysisDllSrc = ResolveAssemblyPath("Microsoft.CodeAnalysis.dll");
        public static readonly string s_systemCollectionsImmutableDllSrc = ResolveAssemblyPath("System.Collections.Immutable.dll");

        // The native client executables can't be loaded via Assembly.Load, so we just use the
        // compiler server resolved path
        public static readonly string s_clientExecutableBasePath = Path.GetDirectoryName(s_compilerServerExecutableSrc);


        public static readonly string[] s_allCompilerFiles =
        {
            s_csharpCompilerExecutableSrc,
            s_basicCompilerExecutableSrc,
            s_compilerServerExecutableSrc,
            s_microsoftCodeAnalysisDllSrc,
            s_systemCollectionsImmutableDllSrc,
            s_buildTaskDllSrc,
            ResolveAssemblyPath("System.Reflection.Metadata.dll"),
            ResolveAssemblyPath("Microsoft.CodeAnalysis.CSharp.dll"),
            ResolveAssemblyPath("Microsoft.CodeAnalysis.VisualBasic.dll"),
            Path.Combine(s_clientExecutableBasePath, CompilerServerExeName + ".config"),
            Path.Combine(s_clientExecutableBasePath, "csc.rsp"),
            Path.Combine(s_clientExecutableBasePath, "vbc.rsp")
        };

        public class TempExecutionEnvironment {
            public readonly TempDirectory tempDirectory;
            public readonly string compilerDirectory;

            public readonly string csharpCompilerClientExecutable;
            public readonly string basicCompilerClientExecutable;
            public readonly string compilerServerExecutable;
            public readonly string buildTaskDll;

            public TempExecutionEnvironment() {
                var tempRoot = new TempRoot();
                tempDirectory = tempRoot.CreateDirectory();

                // Copy the compiler files to a temporary directory
                compilerDirectory = tempRoot.CreateDirectory().Path;
                foreach (var path in s_allCompilerFiles)
                {
                    var filename = Path.GetFileName(path);
                    File.Copy(path, Path.Combine(compilerDirectory, filename));
                }

                csharpCompilerClientExecutable = Path.Combine(compilerDirectory, CSharpClientExeName);
                basicCompilerClientExecutable = Path.Combine(compilerDirectory, BasicClientExeName);
                compilerServerExecutable = Path.Combine(compilerDirectory, CompilerServerExeName);
                buildTaskDll = Path.Combine(compilerDirectory, BuildTaskDllName);
            }
        }
    }
}
