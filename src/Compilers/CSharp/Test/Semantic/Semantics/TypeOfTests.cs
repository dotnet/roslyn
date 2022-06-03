// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class TypeofTests : CSharpTestBase
    {
        [Fact, WorkItem(1720, "https://github.com/dotnet/roslyn/issues/1720")]
        public void GetSymbolsOnResultOfTypeof()
        {
            var source = @"
class C
{
    public C(int i)
    {
        typeof(C).GetField("" "").SetValue(null, new C(0));
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var node = (ObjectCreationExpressionSyntax)tree.GetRoot().DescendantNodes().Where(n => n.ToString() == "new C(0)").Last();
            var identifierName = node.Type;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("C..ctor(System.Int32 i)", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("C", typeInfo.Type.ToTestDisplayString());

        }
    }
}
