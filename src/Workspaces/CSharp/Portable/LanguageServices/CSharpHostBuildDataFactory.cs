// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(ICommandLineArgumentsFactoryService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpCommandLineParserFactory : ICommandLineArgumentsFactoryService
    {
        public CommandLineArguments CreateCommandLineArguments(IEnumerable<string> arguments, string baseDirectory, bool isInteractive, string sdkDirectory)
        {
#if SCRIPTING
            var parser = isInteractive ? CSharpCommandLineParser.Interactive : CSharpCommandLineParser.Default;
#else
            var parser = CSharpCommandLineParser.Default;
#endif
            return parser.Parse(arguments, baseDirectory, sdkDirectory);
        }
    }

    [ExportLanguageService(typeof(IHostBuildDataFactory), LanguageNames.CSharp), Shared]
    internal sealed class CSharpHostBuildDataFactory : IHostBuildDataFactory
    {
        public HostBuildData Create(IHostBuildOptions options)
        {
            var parseOptions = new CSharpParseOptions(languageVersion: LanguageVersion.CSharp6, documentationMode: DocumentationMode.Parse);
            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                xmlReferenceResolver: new XmlFileResolver(options.ProjectDirectory),
                sourceReferenceResolver: new SourceFileResolver(ImmutableArray<string>.Empty, options.ProjectDirectory),
                metadataReferenceResolver: new AssemblyReferenceResolver(
                    new MetadataFileReferenceResolver(ImmutableArray<string>.Empty, options.ProjectDirectory),
                    MetadataFileReferenceProvider.Default),
                strongNameProvider: new DesktopStrongNameProvider(ImmutableArray.Create(options.ProjectDirectory, options.OutputDirectory)),
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
            var warnings = new List<KeyValuePair<string, ReportDiagnostic>>(options.Warnings);

            if (options.OutputKind.HasValue)
            {
                var kind = options.OutputKind.Value;
                compilationOptions = compilationOptions.WithOutputKind(kind);
                if (compilationOptions.Platform == Platform.AnyCpu32BitPreferred &&
                    (kind == OutputKind.DynamicallyLinkedLibrary || kind == OutputKind.NetModule || kind == OutputKind.WindowsRuntimeMetadata))
                {
                    compilationOptions = compilationOptions.WithPlatform(Platform.AnyCpu);
                }
            }

            if (!string.IsNullOrEmpty(options.DefineConstants))
            {
                IEnumerable<Diagnostic> diagnostics;
                parseOptions = parseOptions.WithPreprocessorSymbols(CSharpCommandLineParser.ParseConditionalCompilationSymbols(options.DefineConstants, out diagnostics));
            }

            if (options.DocumentationFile != null)
            {
                parseOptions = parseOptions.WithDocumentationMode(!string.IsNullOrEmpty(options.DocumentationFile) ? DocumentationMode.Diagnose : DocumentationMode.Parse);
            }

            if (options.LanguageVersion != null)
            {
                var languageVersion = CompilationOptionsConversion.GetLanguageVersion(options.LanguageVersion);
                if (languageVersion.HasValue)
                {
                    parseOptions = parseOptions.WithLanguageVersion(languageVersion.Value);
                }
            }

            if (!string.IsNullOrEmpty(options.PlatformWith32BitPreference))
            {
                Platform platform;
                if (Enum.TryParse<Platform>(options.PlatformWith32BitPreference, true, out platform))
                {
                    if (platform == Platform.AnyCpu &&
                        compilationOptions.OutputKind != OutputKind.DynamicallyLinkedLibrary &&
                        compilationOptions.OutputKind != OutputKind.NetModule &&
                        compilationOptions.OutputKind != OutputKind.WindowsRuntimeMetadata)
                    {
                        platform = Platform.AnyCpu32BitPreferred;
                    }

                    compilationOptions = compilationOptions.WithPlatform(platform);
                }
            }

            if (options.AllowUnsafeBlocks.HasValue)
            {
                compilationOptions = compilationOptions.WithAllowUnsafe(options.AllowUnsafeBlocks.Value);
            }

            if (options.CheckForOverflowUnderflow.HasValue)
            {
                compilationOptions = compilationOptions.WithOverflowChecks(options.CheckForOverflowUnderflow.Value);
            }

            if (options.DelaySign != null)
            {
                bool delaySignExplicitlySet = options.DelaySign.Item1;
                bool delaySign = options.DelaySign.Item2;
                compilationOptions = compilationOptions.WithDelaySign(delaySignExplicitlySet ? delaySign : (bool?)null);
            }

            if (!string.IsNullOrEmpty(options.ApplicationConfiguration))
            {
                var appConfigPath = FileUtilities.ResolveRelativePath(options.ApplicationConfiguration, options.ProjectDirectory);
                try
                {
                    using (var appConfigStream = PortableShim.FileStream.Create(appConfigPath, PortableShim.FileMode.Open, PortableShim.FileAccess.Read))
                    {
                        compilationOptions = compilationOptions.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.LoadFromXml(appConfigStream));
                    }
                }
                catch (Exception)
                {
                }
            }

            if (!string.IsNullOrEmpty(options.KeyContainer))
            {
                compilationOptions = compilationOptions.WithCryptoKeyContainer(options.KeyContainer);
            }

            if (!string.IsNullOrEmpty(options.KeyFile))
            {
                var fullPath = FileUtilities.ResolveRelativePath(options.KeyFile, options.ProjectDirectory);
                compilationOptions = compilationOptions.WithCryptoKeyFile(fullPath);
            }

            if (!string.IsNullOrEmpty(options.MainEntryPoint))
            {
                compilationOptions = compilationOptions.WithMainTypeName(options.MainEntryPoint);
            }

            if (!string.IsNullOrEmpty(options.ModuleAssemblyName))
            {
                compilationOptions = compilationOptions.WithModuleName(options.ModuleAssemblyName);
            }

            if (options.Optimize.HasValue)
            {
                compilationOptions = compilationOptions.WithOptimizationLevel(options.Optimize.Value ? OptimizationLevel.Release : OptimizationLevel.Debug);
            }

            if (!string.IsNullOrEmpty(options.Platform))
            {
                Platform plat;
                if (Enum.TryParse<Platform>(options.Platform, ignoreCase: true, result: out plat))
                {
                    compilationOptions = compilationOptions.WithPlatform(plat);
                }
            }

            // Get options from the ruleset file, if any.
            if (!string.IsNullOrEmpty(options.RuleSetFile))
            {
                var fullPath = FileUtilities.ResolveRelativePath(options.RuleSetFile, options.ProjectDirectory);

                Dictionary<string, ReportDiagnostic> specificDiagnosticOptions;
                var generalDiagnosticOption = RuleSet.GetDiagnosticOptionsFromRulesetFile(fullPath, out specificDiagnosticOptions);
                compilationOptions = compilationOptions.WithGeneralDiagnosticOption(generalDiagnosticOption);
                warnings.AddRange(specificDiagnosticOptions);
            }

            if (options.WarningsAsErrors.HasValue)
            {
                compilationOptions = compilationOptions.WithGeneralDiagnosticOption(options.WarningsAsErrors.Value ? ReportDiagnostic.Error : ReportDiagnostic.Default);
            }

            if (options.WarningLevel.HasValue)
            {
                compilationOptions = compilationOptions.WithWarningLevel(options.WarningLevel.Value);
            }

            compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(warnings);
            return new HostBuildData(
                parseOptions,
                compilationOptions);
        }
    }
}
