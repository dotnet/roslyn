// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.Scripting
{
    internal static class TestCompilationFactory
    {
        // TODO: we need to clean up and refactor CreateCompilationWithMscorlib in compiler tests 
        // so that it can be used in portable tests.
        internal static Compilation CreateCompilationWithMscorlib(string source, string assemblyName)
        {
            return CSharpCompilation.Create(
                assemblyName,
                new[] { CSharp.SyntaxFactory.ParseSyntaxTree(source) },
                new[] { TestReferences.NetFx.v4_0_30319.mscorlib },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        internal static Compilation CreateVisualBasicCompilationWithMscorlib(string source, string assemblyName)
        {
            return VisualBasicCompilation.Create(
                assemblyName,
                new[] { VisualBasic.SyntaxFactory.ParseSyntaxTree(source) },
                new[] { TestReferences.NetFx.v4_0_30319.mscorlib },
                new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        internal static Compilation CreateCompilation(string source, MetadataReference[] references, string assemblyName, CSharpCompilationOptions options = null)
        {
            return CSharpCompilation.Create(
                assemblyName,
                new[] { CSharp.SyntaxFactory.ParseSyntaxTree(source) },
                references,
                options ?? new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}