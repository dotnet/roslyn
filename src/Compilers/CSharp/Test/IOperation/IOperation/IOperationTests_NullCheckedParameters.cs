// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_NullCheckedParameters : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NullCheckedMethodDeclarationIOp()
        {
            var source = @"
public class C
{
    public void M(string input!!) { }
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'public void ... nput!!) { }')
  BlockBody: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
  ExpressionBody: 
    null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Exit
    Predecessors: [B0]
    Statements (0)");
        }
    }
}
