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

        public static readonly CSharpCompilationOptions ReleaseDll = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimize: true, debugInformationKind: DebugInformationKind.PdbOnly);
        public static readonly CSharpCompilationOptions ReleaseExe = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimize: true, debugInformationKind: DebugInformationKind.PdbOnly);

        public static readonly CSharpCompilationOptions DebuggableReleaseDll = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimize: true, debugInformationKind: DebugInformationKind.Full);
        public static readonly CSharpCompilationOptions DebuggableReleaseExe = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimize: true, debugInformationKind: DebugInformationKind.Full);

        public static readonly CSharpCompilationOptions DebugDll = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimize: false, debugInformationKind: DebugInformationKind.Full);
        public static readonly CSharpCompilationOptions DebugExe = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimize: false, debugInformationKind: DebugInformationKind.Full);

        public static readonly CSharpCompilationOptions ReleaseWinMD = new CSharpCompilationOptions(OutputKind.WindowsRuntimeMetadata, optimize: true, debugInformationKind: DebugInformationKind.PdbOnly);
        public static readonly CSharpCompilationOptions ReleaseModule = new CSharpCompilationOptions(OutputKind.NetModule, optimize: true, debugInformationKind: DebugInformationKind.PdbOnly);

        public static readonly CSharpCompilationOptions UnsafeReleaseDll = ReleaseDll.WithAllowUnsafe(true);
        public static readonly CSharpCompilationOptions UnsafeReleaseExe = ReleaseExe.WithAllowUnsafe(true);

        public static readonly CSharpCompilationOptions UnsafeDebugDll = DebugDll.WithAllowUnsafe(true);
        public static readonly CSharpCompilationOptions UnsafeDebugExe = DebugExe.WithAllowUnsafe(true);

        // TODO: remove
        public static readonly CSharpCompilationOptions UnoptimizedDll = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimize: false);
        public static readonly CSharpCompilationOptions UnoptimizedExe = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimize: false);
    }
}
