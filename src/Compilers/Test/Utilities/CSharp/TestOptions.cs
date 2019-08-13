// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    public static class TestOptions
    {
        // Disable diagnosing documentation comments by default so that we don't need to
        // document every public member of every test input.
        // https://github.com/dotnet/roslyn/issues/29819 revert explicit C# 8 langversion
        public static readonly CSharpParseOptions Regular = new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.Parse, languageVersion: LanguageVersion.CSharp8);
        public static readonly CSharpParseOptions Script = Regular.WithKind(SourceCodeKind.Script);
        public static readonly CSharpParseOptions Regular6 = Regular.WithLanguageVersion(LanguageVersion.CSharp6);
        public static readonly CSharpParseOptions Regular7 = Regular.WithLanguageVersion(LanguageVersion.CSharp7);
        public static readonly CSharpParseOptions Regular7_1 = Regular.WithLanguageVersion(LanguageVersion.CSharp7_1);
        public static readonly CSharpParseOptions Regular7_2 = Regular.WithLanguageVersion(LanguageVersion.CSharp7_2);
        public static readonly CSharpParseOptions Regular7_3 = Regular.WithLanguageVersion(LanguageVersion.CSharp7_3);
        public static readonly CSharpParseOptions RegularDefault = Regular.WithLanguageVersion(LanguageVersion.Default);
        public static readonly CSharpParseOptions RegularPreview = Regular.WithLanguageVersion(LanguageVersion.Preview);
        public static readonly CSharpParseOptions Regular8 = Regular.WithLanguageVersion(LanguageVersion.CSharp8);
        public static readonly CSharpParseOptions Regular8WithNullableAnalysis = Regular8.WithFeature("run-nullable-analysis");
        public static readonly CSharpParseOptions RegularWithDocumentationComments = Regular.WithDocumentationMode(DocumentationMode.Diagnose);
        public static readonly CSharpParseOptions RegularWithLegacyStrongName = Regular.WithFeature("UseLegacyStrongNameProvider");
        public static readonly CSharpParseOptions WithoutImprovedOverloadCandidates = Regular.WithLanguageVersion(MessageID.IDS_FeatureImprovedOverloadCandidates.RequiredVersion() - 1);

        private static readonly SmallDictionary<string, string> s_experimentalFeatures = new SmallDictionary<string, string> { };
        public static readonly CSharpParseOptions ExperimentalParseOptions =
            // https://github.com/dotnet/roslyn/issues/29819 revert explicit C# 8 langversion
            new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.None, languageVersion: LanguageVersion.CSharp8).WithFeatures(s_experimentalFeatures);

        // Enable pattern-switch translation even for switches that use no new syntax. This is used
        // to help ensure compatibility of the semantics of the new switch binder with the old switch
        // binder, so that we may eliminate the old one in the future.
        public static readonly CSharpParseOptions Regular6WithV7SwitchBinder = Regular6.WithFeatures(new Dictionary<string, string>() { { "testV7SwitchBinder", "true" } });

        public static readonly CSharpParseOptions RegularWithoutRecursivePatterns = Regular7_3;
        public static readonly CSharpParseOptions RegularWithRecursivePatterns = Regular8;

        public static readonly CSharpCompilationOptions ReleaseDll = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release);
        public static readonly CSharpCompilationOptions ReleaseExe = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel: OptimizationLevel.Release);

        public static readonly CSharpCompilationOptions ReleaseDebugDll = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release).
            WithDebugPlusMode(true);

        public static readonly CSharpCompilationOptions ReleaseDebugExe = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel: OptimizationLevel.Release).
            WithDebugPlusMode(true);

        public static readonly CSharpCompilationOptions DebugDll = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug);
        public static readonly CSharpCompilationOptions DebugExe = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel: OptimizationLevel.Debug);

        public static readonly CSharpCompilationOptions ReleaseWinMD = new CSharpCompilationOptions(OutputKind.WindowsRuntimeMetadata, optimizationLevel: OptimizationLevel.Release);
        public static readonly CSharpCompilationOptions DebugWinMD = new CSharpCompilationOptions(OutputKind.WindowsRuntimeMetadata, optimizationLevel: OptimizationLevel.Debug);

        public static readonly CSharpCompilationOptions ReleaseModule = new CSharpCompilationOptions(OutputKind.NetModule, optimizationLevel: OptimizationLevel.Release);
        public static readonly CSharpCompilationOptions DebugModule = new CSharpCompilationOptions(OutputKind.NetModule, optimizationLevel: OptimizationLevel.Debug);

        public static readonly CSharpCompilationOptions UnsafeReleaseDll = ReleaseDll.WithAllowUnsafe(true);
        public static readonly CSharpCompilationOptions UnsafeReleaseExe = ReleaseExe.WithAllowUnsafe(true);

        public static readonly CSharpCompilationOptions UnsafeDebugDll = DebugDll.WithAllowUnsafe(true);
        public static readonly CSharpCompilationOptions UnsafeDebugExe = DebugExe.WithAllowUnsafe(true);

        public static readonly CSharpCompilationOptions SigningReleaseDll = ReleaseDll.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider);
        public static readonly CSharpCompilationOptions SigningReleaseExe = ReleaseExe.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider);
        public static readonly CSharpCompilationOptions SigningReleaseModule = ReleaseModule.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider);
        public static readonly CSharpCompilationOptions SigningDebugDll = DebugDll.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider);

        public static readonly EmitOptions NativePdbEmit = EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Pdb);

        public static CSharpParseOptions WithStrictFeature(this CSharpParseOptions options)
        {
            return options.WithFeatures(options.Features.Concat(new[] { new KeyValuePair<string, string>("strict", "true") }));
        }

        public static CSharpParseOptions WithPEVerifyCompatFeature(this CSharpParseOptions options)
        {
            return options.WithFeatures(options.Features.Concat(new[] { new KeyValuePair<string, string>("peverify-compat", "true") }));
        }

        public static CSharpParseOptions WithLocalFunctionsFeature(this CSharpParseOptions options)
        {
            return options;
        }

        public static CSharpParseOptions WithRefsFeature(this CSharpParseOptions options)
        {
            return options;
        }

        public static CSharpParseOptions WithTuplesFeature(this CSharpParseOptions options)
        {
            return options;
        }

        public static CSharpParseOptions WithFeature(this CSharpParseOptions options, string feature, string value = "true")
        {
            return options.WithFeatures(options.Features.Concat(new[] { new KeyValuePair<string, string>(feature, value) }));
        }

        internal static CSharpParseOptions WithExperimental(this CSharpParseOptions options, params MessageID[] features)
        {
            if (features.Length == 0)
            {
                throw new InvalidOperationException("Need at least one feature to enable");
            }

            var list = new List<KeyValuePair<string, string>>();
            foreach (var feature in features)
            {
                var name = feature.RequiredFeature();
                if (name == null)
                {
                    throw new InvalidOperationException($"{feature} is not a valid experimental feature");
                }
                list.Add(new KeyValuePair<string, string>(name, "true"));
            }

            return options.WithFeatures(options.Features.Concat(list));
        }

        public static CSharpCompilationOptions WithSpecificDiagnosticOptions(this CSharpCompilationOptions options, string key, ReportDiagnostic value)
        {
            return options.WithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic>.Empty.Add(key, value));
        }

        public static CSharpCompilationOptions WithSpecificDiagnosticOptions(this CSharpCompilationOptions options, string key1, string key2, ReportDiagnostic value)
        {
            return options.WithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic>.Empty.Add(key1, value).Add(key2, value));
        }
    }
}

