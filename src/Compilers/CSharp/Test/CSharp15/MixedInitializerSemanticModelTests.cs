// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

// SemanticModel and IOperation tests for the mixed object/collection initializer feature
// (dotnet/csharplang#10185). Pins that the public `GetCollectionInitializerSymbolInfo` API
// recognizes element-shape children of a mixed `ObjectInitializerExpression` wrapper, and that
// the operation tree falls out as a single `IObjectOrCollectionInitializerOperation` with mixed
// per-child operation kinds.
public sealed class MixedInitializerSemanticModelTests : CSharpTestBase
{
    private const string MixedContainerSource = """
        using System.Collections;
        using System.Collections.Generic;

        class C : IEnumerable<int>
        {
            public int X { get; set; }
            public void Add(int item) { }
            public void Add(string item) { }
            public IEnumerator<int> GetEnumerator() { yield break; }
            IEnumerator IEnumerable.GetEnumerator() => null;
        }
        """;

    #region GetCollectionInitializerSymbolInfo

    [Fact]
    public void GetCollectionInitializerSymbolInfo_ElementShapeInMixedWrapper_ResolvesToAdd()
    {
        var source = MixedContainerSource + """

            class Program
            {
                static void Main()
                {
                    var c = new C { X = 1, "hello", 42 };
                }
            }
            """;

        var compilation = CreateCompilation(source);
        compilation.VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var model = compilation.GetSemanticModel(tree);

        // The wrapper is `ObjectInitializerExpression` (mixed): it contains `X = 1` plus two
        // bare element initializers.
        var initializer = tree.GetRoot().DescendantNodes()
            .OfType<InitializerExpressionSyntax>()
            .Single(i => i.IsKind(SyntaxKind.ObjectInitializerExpression));

        var memberInit = initializer.Expressions[0];
        var stringElement = initializer.Expressions[1];
        var intElement = initializer.Expressions[2];

        // Member-shape child: no `Add` resolution to return.
        Assert.Null(model.GetCollectionInitializerSymbolInfo(memberInit).Symbol);

        // Element-shape children: pick the right `Add` overload by argument type.
        var stringInfo = model.GetCollectionInitializerSymbolInfo(stringElement);
        Assert.NotNull(stringInfo.Symbol);
        Assert.Equal("void C.Add(System.String item)", stringInfo.Symbol.ToTestDisplayString());

        var intInfo = model.GetCollectionInitializerSymbolInfo(intElement);
        Assert.NotNull(intInfo.Symbol);
        Assert.Equal("void C.Add(System.Int32 item)", intInfo.Symbol.ToTestDisplayString());
    }

    [Fact]
    public void GetCollectionInitializerSymbolInfo_PureCollectionWrapper_Unchanged()
    {
        var source = MixedContainerSource + """

            class Program
            {
                static void Main()
                {
                    var c = new C { "hello", 42 };
                }
            }
            """;

        var compilation = CreateCompilation(source);
        compilation.VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var model = compilation.GetSemanticModel(tree);

        // Pure-collection wrapper continues to work exactly as before this PR.
        var initializer = tree.GetRoot().DescendantNodes()
            .OfType<InitializerExpressionSyntax>()
            .Single(i => i.IsKind(SyntaxKind.CollectionInitializerExpression));

        var stringInfo = model.GetCollectionInitializerSymbolInfo(initializer.Expressions[0]);
        Assert.Equal("void C.Add(System.String item)", stringInfo.Symbol.ToTestDisplayString());

        var intInfo = model.GetCollectionInitializerSymbolInfo(initializer.Expressions[1]);
        Assert.Equal("void C.Add(System.Int32 item)", intInfo.Symbol.ToTestDisplayString());
    }

    [Fact]
    public void GetCollectionInitializerSymbolInfo_NestedMixedWrapper_ResolvesToAdd()
    {
        // A mixed wrapper nested inside a `Prop = { mixed }` first-form member initializer must
        // still resolve element-shape children's `Add` symbols correctly. The "skip containing
        // object initializers" loop in `GetCollectionInitializerSymbolInfo` walks up through the
        // `=` parent and finds the outer `BaseObjectCreationExpressionSyntax`; the worker is
        // called on the inner initializer (where the child syntax actually lives).
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class Outer
            {
                public C Items { get; } = new C();
            }

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var o = new Outer { Items = { X = 1, 10, 20 } };
                }
            }
            """;

        var compilation = CreateCompilation(source);
        compilation.VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var model = compilation.GetSemanticModel(tree);

        // Find the inner mixed wrapper (`{ X = 1, 10, 20 }`), not the outer object initializer.
        var inner = tree.GetRoot().DescendantNodes()
            .OfType<InitializerExpressionSyntax>()
            .Single(i => i.IsKind(SyntaxKind.ObjectInitializerExpression) && i.Expressions.Count == 3);

        var intElement = inner.Expressions[1];
        var info = model.GetCollectionInitializerSymbolInfo(intElement);
        Assert.NotNull(info.Symbol);
        Assert.Equal("void C.Add(System.Int32 item)", info.Symbol.ToTestDisplayString());
    }

    [Fact]
    public void GetCollectionInitializerSymbolInfo_ComplexElementInMixedWrapper_ResolvesMultiArgAdd()
    {
        // The brace-list `{ a, b }` complex-element shape — multi-arg `Add(a, b)` — appears inside
        // mixed wrappers too. `GetCollectionInitializerSymbolInfo` on the complex element resolves
        // to the multi-arg `Add` overload.
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int a, int b) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { X = 1, { 2, 3 } };
                }
            }
            """;

        var compilation = CreateCompilation(source);
        compilation.VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var model = compilation.GetSemanticModel(tree);

        var initializer = tree.GetRoot().DescendantNodes()
            .OfType<InitializerExpressionSyntax>()
            .Single(i => i.IsKind(SyntaxKind.ObjectInitializerExpression));

        var complexElement = (InitializerExpressionSyntax)initializer.Expressions[1];
        Assert.True(complexElement.IsKind(SyntaxKind.ComplexElementInitializerExpression));

        var info = model.GetCollectionInitializerSymbolInfo(complexElement);
        Assert.NotNull(info.Symbol);
        Assert.Equal("void C.Add(System.Int32 a, System.Int32 b)", info.Symbol.ToTestDisplayString());
    }

    [Fact]
    public void GetCollectionInitializerSymbolInfo_MemberShapeInMixedWrapper_ReturnsNone()
    {
        // Member-shape children of a mixed wrapper are not `Add`-target expressions; the API
        // returns `SymbolInfo.None` for them rather than picking a coincidental overload.
        var source = MixedContainerSource + """

            class Program
            {
                static void Main()
                {
                    var c = new C { 10, X += 5, 20 };
                }
            }
            """;

        var compilation = CreateCompilation(source);
        compilation.VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var model = compilation.GetSemanticModel(tree);

        var initializer = tree.GetRoot().DescendantNodes()
            .OfType<InitializerExpressionSyntax>()
            .Single(i => i.IsKind(SyntaxKind.ObjectInitializerExpression));

        var compoundMember = initializer.Expressions[1];
        Assert.True(compoundMember.IsKind(SyntaxKind.AddAssignmentExpression));
        Assert.Null(model.GetCollectionInitializerSymbolInfo(compoundMember).Symbol);
    }

    #endregion

    #region IOperation tree shape

    [Fact]
    public void IOperation_MixedWrapper_ProducesUnifiedInitializerOperationWithMixedChildren()
    {
        // Mixed wrappers continue to produce a single `IObjectOrCollectionInitializerOperation`
        // whose `Initializers` is a mix of `IAssignmentOperation` (the member-init children) and
        // `IInvocationOperation` (the `Add` calls for the element-init children).
        var source = MixedContainerSource + """

            class Program
            {
                static C Make() => new C { X = 1, "hello", 42 };
            }
            """;

        var compilation = CreateCompilation(source);
        compilation.VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var model = compilation.GetSemanticModel(tree);

        var creation = tree.GetRoot().DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Single();
        var op = (IObjectCreationOperation)model.GetOperation(creation)!;

        Assert.NotNull(op.Initializer);
        var children = op.Initializer!.Initializers;
        Assert.Equal(3, children.Length);

        var memberOp = Assert.IsAssignableFrom<ISimpleAssignmentOperation>(children[0]);
        Assert.Equal("X", ((IPropertyReferenceOperation)memberOp.Target).Property.Name);

        var stringAdd = Assert.IsAssignableFrom<IInvocationOperation>(children[1]);
        Assert.Equal("Add", stringAdd.TargetMethod.Name);
        Assert.Equal("System.String", stringAdd.TargetMethod.Parameters[0].Type.ToTestDisplayString());

        var intAdd = Assert.IsAssignableFrom<IInvocationOperation>(children[2]);
        Assert.Equal("Add", intAdd.TargetMethod.Name);
        Assert.Equal("System.Int32", intAdd.TargetMethod.Parameters[0].Type.ToTestDisplayString());
    }

    [Fact]
    public void IOperation_MixedWrapper_DynamicAddArgument_ProducesDynamicInvocation()
    {
        // A `dynamic` argument in an element-shape child of a mixed wrapper produces an
        // `IDynamicInvocationOperation`, mirroring the pure-collection behavior. Pinned so any
        // future operation-tree change preserves the dynamic vs static split.
        var source = MixedContainerSource + """

            class Program
            {
                static void M(dynamic d)
                {
                    var c = new C { X = 1, d + 0 };
                }
            }
            """;

        var compilation = CreateCompilation(source, references: new[] { CSharpRef });
        compilation.VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var model = compilation.GetSemanticModel(tree);

        var creation = tree.GetRoot().DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Single();
        var op = (IObjectCreationOperation)model.GetOperation(creation)!;

        Assert.NotNull(op.Initializer);
        var children = op.Initializer!.Initializers;
        Assert.Equal(2, children.Length);
        Assert.IsAssignableFrom<ISimpleAssignmentOperation>(children[0]);
        Assert.Equal(OperationKind.DynamicInvocation, children[1].Kind);
    }

    [Fact]
    public void IOperation_MixedWrapper_PreservesLexicalChildOrder()
    {
        // Children must appear in source order — the unified initializer operation's `Initializers`
        // array matches the source-textual order regardless of which kind each child is.
        var source = MixedContainerSource + """

            class Program
            {
                static C Make() => new C { 1, X = 2, 3, X += 4, 5 };
            }
            """;

        var compilation = CreateCompilation(source);
        compilation.VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var model = compilation.GetSemanticModel(tree);

        var creation = tree.GetRoot().DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Single();
        var op = (IObjectCreationOperation)model.GetOperation(creation)!;

        Assert.NotNull(op.Initializer);
        var kinds = op.Initializer!.Initializers.Select(o => o.Kind).ToArray();
        Assert.Equal(
            [
                OperationKind.Invocation,         // Add(1)
                OperationKind.SimpleAssignment,   // X = 2
                OperationKind.Invocation,         // Add(3)
                OperationKind.CompoundAssignment, // X += 4
                OperationKind.Invocation,         // Add(5)
            ],
            kinds);
    }

    #endregion
}
