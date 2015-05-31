// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Host
{
    internal interface ICompilationFactoryService : ILanguageService
    {
        Compilation CreateCompilation(string assemblyName, CompilationOptions options);
        Compilation CreateSubmissionCompilation(string assemblyName, CompilationOptions options, Type hostObjectType);
        Compilation GetCompilationFromCompilationReference(MetadataReference reference);
        bool IsCompilationReference(MetadataReference reference);
        CompilationOptions GetDefaultCompilationOptions();
    }
}
