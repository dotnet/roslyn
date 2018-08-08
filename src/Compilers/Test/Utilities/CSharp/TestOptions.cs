﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    public static class TestOptions
    {
        // Disable diagnosing documentation comments by default so that we don't need to
        // document every public member of every test input.
        // PROTOTYPE(NullableReferenceTypes): Use default for LanguageVersion rather than LanguageVersion.CSharp7.3.
        public static readonly CSharpParseOptions RegularDefault = new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.Parse);
        public static readonly CSharpParseOptions Regular = RegularDefault.WithLanguageVersion(LanguageVersion.CSharp7_3);
        public static readonly CSharpParseOptions ScriptDefault = new CSharpParseOptions(kind: SourceCodeKind.Script, documentationMode: DocumentationMode.Parse);
        public static readonly CSharpParseOptions Script = ScriptDefault.WithLanguageVersion(LanguageVersion.CSharp7_3);
        public static readonly CSharpParseOptions Regular6 = RegularDefault.WithLanguageVersion(LanguageVersion.CSharp6);
        public static readonly CSharpParseOptions Regular7 = RegularDefault.WithLanguageVersion(LanguageVersion.CSharp7);
        public static readonly CSharpParseOptions Regular7_1 = RegularDefault.WithLanguageVersion(LanguageVersion.CSharp7_1);
        public static readonly CSharpParseOptions Regular7_2 = RegularDefault.WithLanguageVersion(LanguageVersion.CSharp7_2);
        public static readonly CSharpParseOptions Regular7_3 = RegularDefault.WithLanguageVersion(LanguageVersion.CSharp7_3);
        public static readonly CSharpParseOptions Regular8 = RegularDefault.WithLanguageVersion(LanguageVersion.CSharp8);
        public static readonly CSharpParseOptions RegularWithDocumentationComments = Regular.WithDocumentationMode(DocumentationMode.Diagnose);
        public static readonly CSharpParseOptions WithoutImprovedOverloadCandidates = Regular.WithLanguageVersion(MessageID.IDS_FeatureImprovedOverloadCandidates.RequiredVersion() - 1);

        private static readonly SmallDictionary<string, string> s_experimentalFeatures = new SmallDictionary<string, string> { };
        public static readonly CSharpParseOptions ExperimentalParseOptions =
            new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.None, languageVersion: LanguageVersion.Latest).WithFeatures(s_experimentalFeatures);

        // Enable pattern-switch translation even for switches that use no new syntax. This is used
        // to help ensure compatibility of the semantics of the new switch binder with the old switch
        // binder, so that we may eliminate the old one in the future.
        public static readonly CSharpParseOptions Regular6WithV7SwitchBinder = Regular6.WithFeatures(new Dictionary<string, string>() { { "testV7SwitchBinder", "true" } });

        public static readonly CSharpParseOptions RegularWithFlowAnalysisFeature = Regular.WithFlowAnalysisFeature();

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

        public static CSharpParseOptions WithFlowAnalysisFeature(this CSharpParseOptions options)
        {
            return options.WithFeatures(options.Features.Concat(new[] { new KeyValuePair<string, string>("flow-analysis", "true") }));
        }
    }
}

