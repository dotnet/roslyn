// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    public static class TestOptions
    {
        // Disable documentation comments by default so that we don't need to
        // document every public member of every test input.
        public static readonly CSharpParseOptions Script = new CSharpParseOptions(kind: SourceCodeKind.Script, documentationMode: DocumentationMode.None);
        public static readonly CSharpParseOptions Interactive = new CSharpParseOptions(kind: SourceCodeKind.Interactive, documentationMode: DocumentationMode.None);
        public static readonly CSharpParseOptions Regular = new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.None);
        public static readonly CSharpParseOptions RegularWithDocumentationComments = new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.Diagnose);

        public static readonly CSharpParseOptions ExperimentalParseOptions = 
            new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.None, languageVersion: LanguageVersion.Experimental);
        
        public static readonly CSharpCompilationOptions Dll = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        public static readonly CSharpCompilationOptions Exe = new CSharpCompilationOptions(OutputKind.ConsoleApplication);
        public static readonly CSharpCompilationOptions WinExe = new CSharpCompilationOptions(OutputKind.WindowsApplication);
        public static readonly CSharpCompilationOptions NetModule = new CSharpCompilationOptions(OutputKind.NetModule);
        public static readonly CSharpCompilationOptions WinMDObj = new CSharpCompilationOptions(OutputKind.WindowsRuntimeMetadata);
        public static readonly CSharpCompilationOptions WinRtExe = new CSharpCompilationOptions(OutputKind.WindowsRuntimeApplication);

        public static readonly CSharpCompilationOptions UnsafeDll = Dll.WithAllowUnsafe(true).WithOptimizations(true);
        public static readonly CSharpCompilationOptions UnsafeExe = Exe.WithAllowUnsafe(true).WithOptimizations(true);

        public static readonly CSharpCompilationOptions DllAlwaysImportInternals = Dll.WithMetadataImportOptions(MetadataImportOptions.Internal);
        public static readonly CSharpCompilationOptions ExeAlwaysImportInternals = Exe.WithMetadataImportOptions(MetadataImportOptions.Internal);

        public static readonly CSharpCompilationOptions OptimizedDll = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimize: true);
        public static readonly CSharpCompilationOptions OptimizedExe = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimize: true);

        public static readonly CSharpCompilationOptions UnoptimizedDll = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimize: false);
        public static readonly CSharpCompilationOptions UnoptimizedExe = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimize: false);

        public static readonly CSharpCompilationOptions DebugDll = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimize: false, debugInformationKind: DebugInformationKind.Full);
        public static readonly CSharpCompilationOptions DebugExe = new CSharpCompilationOptions(OutputKind.ConsoleApplication, allowUnsafe: true, optimize: false, debugInformationKind: DebugInformationKind.Full);
    }
}
