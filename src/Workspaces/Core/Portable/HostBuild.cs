using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis
{
    internal sealed class HostBuildOptions
    {
        public string ProjectDirectory { get; set; }
        public string OutputDirectory { get; set; }
        public string DefineConstants { get; set; }
        public string DocumentationFile { get; set; }
        public string LanguageVersion { get; set; }
        public string PlatformWith32BitPreference { get; set; }
        public string ApplicationConfiguration { get; set; }
        public string KeyContainer { get; set; }
        public string KeyFile { get; set; }
        public string MainEntryPoint { get; set; }
        public string ModuleAssemblyName { get; set; }
        public string Platform { get; set; }
        public string RuleSetFile { get; set; }
        public bool? AllowUnsafeBlocks { get; set; }
        public bool? CheckForOverflowUnderflow { get; set; }
        public bool? Optimize { get; set; }
        public bool? WarningsAsErrors { get; set; }
        public int? WarningLevel { get; set; }
        public OutputKind? OutputKind { get; set; }
        public Tuple<bool, bool> DelaySign { get; set; }
        public Dictionary<string, ReportDiagnostic> Warnings { get; set; }

        internal HostBuildOptions()
        {
            Warnings = new Dictionary<string, ReportDiagnostic>();
        }
    }

    internal sealed class HostBuildData
    {
        internal readonly ParseOptions ParseOptions;

        internal readonly CompilationOptions CompilationOptions;

        internal HostBuildData(ParseOptions parseOptions, CompilationOptions compilationOptions)
        {
            ParseOptions = parseOptions;
            CompilationOptions = compilationOptions;
        }
    }

    internal interface IHostBuildDataFactory : ILanguageService
    {
        HostBuildData Create(HostBuildOptions options);
    }
}
