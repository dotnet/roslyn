// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis
{
    internal interface IHostBuildOptions
    {
        string ProjectDirectory { get; set; }
        string OutputDirectory { get; set; }
        string DefineConstants { get; set; }
        string DocumentationFile { get; set; }
        string LanguageVersion { get; set; }
        string PlatformWith32BitPreference { get; set; }
        string ApplicationConfiguration { get; set; }
        string KeyContainer { get; set; }
        string KeyFile { get; set; }
        string MainEntryPoint { get; set; }
        string ModuleAssemblyName { get; set; }
        string Platform { get; set; }
        string RuleSetFile { get; set; }
        string OptionCompare { get; set; }
        string OptionStrict { get; set; }
        string RootNamespace { get; set; }
        string VBRuntime { get; set; }
        bool? AllowUnsafeBlocks { get; set; }
        bool? CheckForOverflowUnderflow { get; set; }
        bool? Optimize { get; set; }
        bool? WarningsAsErrors { get; set; }
        bool? NoWarnings { get; set; }
        bool? OptionExplicit { get; set; }
        bool? OptionInfer { get; set; }
        int? WarningLevel { get; set; }
        OutputKind? OutputKind { get; set; }
        Tuple<bool, bool> DelaySign { get; set; }
        List<string> GlobalImports { get; set; }
        Dictionary<string, ReportDiagnostic> Warnings { get; set; }
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
        HostBuildData Create(IHostBuildOptions options);
    }
}
