// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Scripting.Test
{
    internal static class CompilationHelpers
    {
        // TODO: replace with CreateCompilationWithMscorlib once it's portable
        internal static Compilation CreateLibrary(string assemblyName, string source)
        {
            return CSharpCompilation.Create(
                assemblyName,
                new[] { SyntaxFactory.ParseSyntaxTree(source) },
                new[] { TestReferences.NetFx.v4_0_30319.mscorlib },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}
