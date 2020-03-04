// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
