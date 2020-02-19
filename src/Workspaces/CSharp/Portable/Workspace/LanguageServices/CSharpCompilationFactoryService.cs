// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(ICompilationFactoryService), LanguageNames.CSharp), Shared]
    internal class CSharpCompilationFactoryService : ICompilationFactoryService
    {
        private static readonly CSharpCompilationOptions s_defaultOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication, concurrentBuild: false);

        [ImportingConstructor]
        public CSharpCompilationFactoryService()
        {
        }

        Compilation ICompilationFactoryService.CreateCompilation(string assemblyName, CompilationOptions options)
        {
            return CSharpCompilation.Create(
                assemblyName,
                options: (CSharpCompilationOptions)options ?? s_defaultOptions);
        }

        Compilation ICompilationFactoryService.CreateSubmissionCompilation(string assemblyName, CompilationOptions options, Type hostObjectType)
        {
            return CSharpCompilation.CreateScriptCompilation(
                assemblyName,
                options: (CSharpCompilationOptions)options,
                previousScriptCompilation: null,
                globalsType: hostObjectType);
        }

        Compilation ICompilationFactoryService.GetCompilationFromCompilationReference(MetadataReference reference)
        {
            var compilationRef = reference as CompilationReference;
            return compilationRef?.Compilation;
        }

        bool ICompilationFactoryService.IsCompilationReference(MetadataReference reference)
        {
            return reference is CompilationReference;
        }

        CompilationOptions ICompilationFactoryService.GetDefaultCompilationOptions()
        {
            return s_defaultOptions;
        }
    }
}
