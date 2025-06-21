// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Test.Utilities
{
    public static class MinimalCoreLibBuilder
    {
        public static Compilation Create(params IEnumerable<string> additionalSources)
        {
            CSharpParseOptions options = new CSharpParseOptions(LanguageVersion.Preview);
            ImmutableArray<SyntaxTree>.Builder trees = ImmutableArray.CreateBuilder<SyntaxTree>();
            trees.Add(CSharpSyntaxTree.ParseText(SourceText.From(TestResources.NetFX.Minimal.mincorlib_cs), options));
            foreach (var additionalSource in additionalSources)
            {
                trees.Add(CSharpSyntaxTree.ParseText(SourceText.From(additionalSource), options));
            }
            return CSharpCompilation.Create("Minimal.CoreLib", trees.DrainToImmutable(), options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}
