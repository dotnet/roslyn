// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Basic.Reference.Assemblies;

namespace Microsoft.CodeAnalysis.Scripting
{
    internal static class TestCompilationFactory
    {
        // TODO: we need to clean up and refactor CreateCompilationWithMscorlib in compiler tests 
        // so that it can be used in portable tests.
        internal static Compilation CreateCSharpCompilationWithCorlib(string source, string assemblyName = null)
        {
            return CSharpCompilation.Create(
                assemblyName ?? Guid.NewGuid().ToString(),
                new[] { CSharp.SyntaxFactory.ParseSyntaxTree(SourceText.From(source, encoding: null, SourceHashAlgorithms.Default)) },
                new[] { NetStandard13.SystemRuntime },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        internal static Compilation CreateVisualBasicCompilationWithCorlib(string source, string assemblyName = null)
        {
            return VisualBasicCompilation.Create(
                assemblyName ?? Guid.NewGuid().ToString(),
                new[] { VisualBasic.SyntaxFactory.ParseSyntaxTree(SourceText.From(source, encoding: null, SourceHashAlgorithms.Default)) },
                new[] { NetStandard13.SystemRuntime },
                new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        internal static Compilation CreateCSharpCompilation(string source, IEnumerable<MetadataReference> references, string assemblyName = null, CSharpCompilationOptions options = null)
        {
            return CSharpCompilation.Create(
                assemblyName ?? Guid.NewGuid().ToString(),
                new[] { CSharp.SyntaxFactory.ParseSyntaxTree(SourceText.From(source, encoding: null, SourceHashAlgorithms.Default)) },
                references,
                options ?? new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}
