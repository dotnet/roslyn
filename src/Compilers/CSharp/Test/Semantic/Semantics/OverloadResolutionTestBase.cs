// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Roslyn.Test.Utilities;
using Basic.Reference.Assemblies;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public abstract class OverloadResolutionTestBase : CompilingTestBase
    {
        internal void TestOverloadResolutionWithDiff(string source, MetadataReference[] additionalRefs = null)
        {
            // The mechanism of this test is: we build the bound tree for the code passed in and then extract
            // from it the nodes that describe the method symbols. We then compare the description of
            // the symbols given to the comment that follows the call.

            var mscorlibRef = AssemblyMetadata.CreateFromImage(Net461.Resources.mscorlib).GetReference(display: "mscorlib");
            var references = new[] { mscorlibRef }.Concat(additionalRefs ?? Array.Empty<MetadataReference>());

            var compilation = CreateEmptyCompilation(source, references, TestOptions.ReleaseDll);

            var method = (SourceMemberMethodSymbol)compilation.GlobalNamespace.GetTypeMembers("C").Single().GetMembers("M").Single();
            var diagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
            var block = MethodCompiler.BindSynthesizedMethodBody(method, new TypeCompilationState(method.ContainingType, compilation, null), diagnostics);
            diagnostics.Free();
            var tree = BoundTreeDumperNodeProducer.MakeTree(block);
            var results = string.Join("\n", tree.PreorderTraversal().Select(edge => edge.Value)
                .Where(x => x.Text == "method" && x.Value != null)
                .Select(x => x.Value)
                .ToArray());

            // var r = string.Join("\n", tree.PreorderTraversal().Select(edge => edge.Value).ToArray();

            var expected = string.Join("\n", source
                .Split(new[] { Environment.NewLine }, System.StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Contains("//-"))
                .Select(x => x.Substring(x.IndexOf("//-", StringComparison.Ordinal) + 3))
                .ToArray());

            AssertEx.EqualOrDiff(expected, results);
        }
    }
}
