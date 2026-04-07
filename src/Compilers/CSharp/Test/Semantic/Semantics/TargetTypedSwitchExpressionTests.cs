// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public class TargetTypedSwitchExpressionTests : CSharpTestBase
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81022")]
    public void ErrorRecovery_Return()
    {
        var source = """
            class C
            {
                C M(int i)
                {
                    return i switch
                    {
                        1 => new(a),
                        _ => default,
                    };
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,22): error CS0103: The name 'a' does not exist in the current context
            //             1 => new(a),
            Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(7, 22));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var switchExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SwitchExpressionSyntax>().Single();
        var typeInfo = model.GetTypeInfo(switchExpression);
        Assert.Null(typeInfo.Type);
        Assert.Equal("C", typeInfo.ConvertedType.ToTestDisplayString());

        var objectCreationExpression = switchExpression.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
        var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
        Assert.Equal("C", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
        Assert.Equal("C", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
        var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
        Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
        Assert.Equal(CandidateReason.OverloadResolutionFailure, objectCreationExpressionSymbolInfo.CandidateReason);
        AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
        var objectCreationExpressionSymbolGroup = model.GetMemberGroup(objectCreationExpression);
        AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionSymbolGroup.ToTestDisplayStrings());

        var defaultLiteralExpression = switchExpression.DescendantNodes().OfType<LiteralExpressionSyntax>().Single(l => l.IsKind(SyntaxKind.DefaultLiteralExpression));
        var defaultLiteralExpressionTypeInfo = model.GetTypeInfo(defaultLiteralExpression);
        Assert.Equal("C", defaultLiteralExpressionTypeInfo.Type.ToTestDisplayString());
        Assert.Equal("C", defaultLiteralExpressionTypeInfo.ConvertedType.ToTestDisplayString());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81022")]
    public void ErrorRecovery_VariableDeclaration()
    {
        var source = """
            class C
            {
                void M(int i)
                {
                    C c = i switch
                    {
                        1 => new(a),
                        _ => default,
                    };
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,22): error CS0103: The name 'a' does not exist in the current context
            //             1 => new(a),
            Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(7, 22));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var switchExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SwitchExpressionSyntax>().Single();
        var typeInfo = model.GetTypeInfo(switchExpression);
        Assert.Null(typeInfo.Type);
        Assert.Equal("C", typeInfo.ConvertedType.ToTestDisplayString());

        var objectCreationExpression = switchExpression.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
        var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
        Assert.Equal("C", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
        Assert.Equal("C", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
        var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
        Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
        Assert.Equal(CandidateReason.OverloadResolutionFailure, objectCreationExpressionSymbolInfo.CandidateReason);
        AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
        var objectCreationExpressionSymbolGroup = model.GetMemberGroup(objectCreationExpression);
        AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionSymbolGroup.ToTestDisplayStrings());

        var defaultLiteralExpression = switchExpression.DescendantNodes().OfType<LiteralExpressionSyntax>().Single(l => l.IsKind(SyntaxKind.DefaultLiteralExpression));
        var defaultLiteralExpressionTypeInfo = model.GetTypeInfo(defaultLiteralExpression);
        Assert.Equal("C", defaultLiteralExpressionTypeInfo.Type.ToTestDisplayString());
        Assert.Equal("C", defaultLiteralExpressionTypeInfo.ConvertedType.ToTestDisplayString());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81022")]
    public void ErrorRecovery_Assignment()
    {
        var source = """
            class C
            {
                void M(int i)
                {
                    C c;
                    c = i switch
                    {
                        1 => new(a),
                        _ => default,
                    };
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (8,22): error CS0103: The name 'a' does not exist in the current context
            //             1 => new(a),
            Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(8, 22));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var switchExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SwitchExpressionSyntax>().Single();
        var typeInfo = model.GetTypeInfo(switchExpression);
        Assert.Null(typeInfo.Type);
        Assert.Equal("C", typeInfo.ConvertedType.ToTestDisplayString());

        var objectCreationExpression = switchExpression.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
        var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
        Assert.Equal("C", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
        Assert.Equal("C", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
        var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
        Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
        Assert.Equal(CandidateReason.OverloadResolutionFailure, objectCreationExpressionSymbolInfo.CandidateReason);
        AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
        var objectCreationExpressionSymbolGroup = model.GetMemberGroup(objectCreationExpression);
        AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionSymbolGroup.ToTestDisplayStrings());

        var defaultLiteralExpression = switchExpression.DescendantNodes().OfType<LiteralExpressionSyntax>().Single(l => l.IsKind(SyntaxKind.DefaultLiteralExpression));
        var defaultLiteralExpressionTypeInfo = model.GetTypeInfo(defaultLiteralExpression);
        Assert.Equal("C", defaultLiteralExpressionTypeInfo.Type.ToTestDisplayString());
        Assert.Equal("C", defaultLiteralExpressionTypeInfo.ConvertedType.ToTestDisplayString());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81022")]
    public void ErrorRecovery_Call()
    {
        var source = """
            class C
            {
                void M(int i)
                {
                    N(i switch
                    {
                        1 => new(a),
                        _ => default,
                    });
                }

                void N(C c) { }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,18): error CS1729: 'C' does not contain a constructor that takes 1 arguments
            //             1 => new(a),
            Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new(a)").WithArguments("C", "1").WithLocation(7, 18),
            // (7,22): error CS0103: The name 'a' does not exist in the current context
            //             1 => new(a),
            Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(7, 22));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var switchExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SwitchExpressionSyntax>().Single();
        var typeInfo = model.GetTypeInfo(switchExpression);
        Assert.Null(typeInfo.Type);
        Assert.Equal("C", typeInfo.ConvertedType.ToTestDisplayString());

        var objectCreationExpression = switchExpression.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
        var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
        Assert.Equal("C", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
        Assert.Equal("C", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
        var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
        Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
        Assert.Equal(CandidateReason.OverloadResolutionFailure, objectCreationExpressionSymbolInfo.CandidateReason);
        AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
        var objectCreationExpressionSymbolGroup = model.GetMemberGroup(objectCreationExpression);
        AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionSymbolGroup.ToTestDisplayStrings());

        var defaultLiteralExpression = switchExpression.DescendantNodes().OfType<LiteralExpressionSyntax>().Single(l => l.IsKind(SyntaxKind.DefaultLiteralExpression));
        var defaultLiteralExpressionTypeInfo = model.GetTypeInfo(defaultLiteralExpression);
        Assert.Equal("C", defaultLiteralExpressionTypeInfo.Type.ToTestDisplayString());
        Assert.Equal("C", defaultLiteralExpressionTypeInfo.ConvertedType.ToTestDisplayString());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81022")]
    public void ErrorRecovery_Cast()
    {
        var source = """
            class C
            {
                void M(int i)
                {
                    var c = (C)(i switch
                    {
                        1 => new(a),
                        _ => default,
                    });
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,18): error CS1729: 'C' does not contain a constructor that takes 1 arguments
            //             1 => new(a),
            Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new(a)").WithArguments("C", "1").WithLocation(7, 18),
            // (7,22): error CS0103: The name 'a' does not exist in the current context
            //             1 => new(a),
            Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(7, 22));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var switchExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SwitchExpressionSyntax>().Single();
        var typeInfo = model.GetTypeInfo(switchExpression);
        Assert.Null(typeInfo.Type);
        Assert.Null(typeInfo.ConvertedType);

        var objectCreationExpression = switchExpression.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
        var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
        Assert.Equal("C", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
        Assert.Equal("C", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
        var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
        Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
        Assert.Equal(CandidateReason.OverloadResolutionFailure, objectCreationExpressionSymbolInfo.CandidateReason);
        AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
        var objectCreationExpressionSymbolGroup = model.GetMemberGroup(objectCreationExpression);
        AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionSymbolGroup.ToTestDisplayStrings());

        var defaultLiteralExpression = switchExpression.DescendantNodes().OfType<LiteralExpressionSyntax>().Single(l => l.IsKind(SyntaxKind.DefaultLiteralExpression));
        var defaultLiteralExpressionTypeInfo = model.GetTypeInfo(defaultLiteralExpression);
        Assert.Equal("C", defaultLiteralExpressionTypeInfo.Type.ToTestDisplayString());
        Assert.Equal("C", defaultLiteralExpressionTypeInfo.ConvertedType.ToTestDisplayString());
    }
}
