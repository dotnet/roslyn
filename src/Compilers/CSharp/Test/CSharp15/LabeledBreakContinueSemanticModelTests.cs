// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public sealed class LabeledBreakContinueSemanticModelTests : CSharpTestBase
{
    #region GetSymbolInfo

    [Fact]
    public void GetSymbolInfo_Break_ResolvesToLabelSymbol()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        break outer;
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var labelDecl = tree.GetRoot().DescendantNodes().OfType<LabeledStatementSyntax>().Single();
        var declaredSymbol = model.GetDeclaredSymbol(labelDecl);
        Assert.NotNull(declaredSymbol);
        Assert.Equal("outer", declaredSymbol.Name);
        Assert.Equal(SymbolKind.Label, declaredSymbol.Kind);

        var breakStmt = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var labelRef = breakStmt.Name;
        Assert.NotNull(labelRef);

        var symbolInfo = model.GetSymbolInfo(labelRef);
        Assert.Same(declaredSymbol, symbolInfo.Symbol);
    }

    [Fact]
    public void GetSymbolInfo_Continue_ResolvesToLabelSymbol()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        continue outer;
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var labelDecl = tree.GetRoot().DescendantNodes().OfType<LabeledStatementSyntax>().Single();
        var declaredSymbol = model.GetDeclaredSymbol(labelDecl);

        var continueStmt = tree.GetRoot().DescendantNodes().OfType<ContinueStatementSyntax>().Single();
        var labelRef = continueStmt.Name;
        Assert.NotNull(labelRef);

        var symbolInfo = model.GetSymbolInfo(labelRef);
        Assert.Same(declaredSymbol, symbolInfo.Symbol);
    }

    [Fact]
    public void GetSymbolInfo_Break_NestedLoop_ResolvesToOuterLabel()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        inner: while (true)
                        {
                            break outer;
                        }
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var labels = tree.GetRoot().DescendantNodes().OfType<LabeledStatementSyntax>().ToArray();
        var outerLabel = model.GetDeclaredSymbol(labels.Single(l => l.Identifier.ValueText == "outer"));

        var breakStmt = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var symbolInfo = model.GetSymbolInfo(breakStmt.Name!);
        Assert.Same(outerLabel, symbolInfo.Symbol);
    }

    [Fact]
    public void GetSymbolInfo_UnlabeledBreak_NoName()
    {
        var source = """
            class C
            {
                void M()
                {
                    while (true)
                    {
                        break;
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();

        var breakStmt = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        Assert.Null(breakStmt.Name);
    }

    [Fact]
    public void GetSymbolInfo_InvalidBreak_DoesNotResolveToLabelSymbol()
    {
        var source = """
            class C
            {
                void M()
                {
                    while (true)
                    {
                    outer:
                        break outer;
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var labelDecl = tree.GetRoot().DescendantNodes().OfType<LabeledStatementSyntax>().Single();
        var declaredSymbol = model.GetDeclaredSymbol(labelDecl);
        Assert.NotNull(declaredSymbol);
        Assert.Equal("outer", declaredSymbol.Name);
        Assert.Equal(SymbolKind.Label, declaredSymbol.Kind);

        var breakStmt = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var labelRef = breakStmt.Name;
        Assert.NotNull(labelRef);

        var symbolInfo = model.GetSymbolInfo(labelRef);
        Assert.Null(symbolInfo.Symbol);
    }

    [Fact]
    public void GetSymbolInfo_InvalidContinue_DoesNotResolveToLabelSymbol()
    {
        var source = """
            class C
            {
                void M()
                {
                    while (true)
                    {
                    outer:
                        continue outer;
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var labelDecl = tree.GetRoot().DescendantNodes().OfType<LabeledStatementSyntax>().Single();
        var declaredSymbol = model.GetDeclaredSymbol(labelDecl);
        Assert.NotNull(declaredSymbol);
        Assert.Equal("outer", declaredSymbol.Name);
        Assert.Equal(SymbolKind.Label, declaredSymbol.Kind);

        var continueStmt = tree.GetRoot().DescendantNodes().OfType<ContinueStatementSyntax>().Single();
        var labelRef = continueStmt.Name;
        Assert.NotNull(labelRef);

        var symbolInfo = model.GetSymbolInfo(labelRef);
        Assert.Null(symbolInfo.Symbol);
    }

    #endregion

    #region GetTypeInfo / GetConversion / GetMemberGroup / GetConstantValue / GetAliasInfo

    [Fact]
    public void GetTypeInfo_OnLabel_ReturnsNothing()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        break outer;
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var breakStmt = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var typeInfo = model.GetTypeInfo(breakStmt.Name!);
        Assert.Null(typeInfo.Type);
        Assert.Null(typeInfo.ConvertedType);
    }

    [Fact]
    public void GetConversion_OnLabel_ReturnsNoConversion()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        break outer;
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var breakStmt = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var conversion = model.GetConversion(breakStmt.Name!);
        Assert.True(conversion.IsIdentity);
    }

    [Fact]
    public void GetMemberGroup_OnLabel_ReturnsEmpty()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        break outer;
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var breakStmt = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var memberGroup = model.GetMemberGroup(breakStmt.Name!);
        Assert.Empty(memberGroup);
    }

    [Fact]
    public void GetConstantValue_OnLabel_ReturnsNothing()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        break outer;
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var breakStmt = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var constantValue = model.GetConstantValue(breakStmt.Name!);
        Assert.False(constantValue.HasValue);
    }

    [Fact]
    public void GetAliasInfo_OnLabel_ReturnsNull()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        break outer;
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var breakStmt = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var aliasInfo = model.GetAliasInfo(breakStmt.Name!);
        Assert.Null(aliasInfo);
    }

    #endregion

    #region GetDeclaredSymbol

    [Fact]
    public void GetDeclaredSymbol_OnBreakStatement_ReturnsNull()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        break outer;
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var breakStmt = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        Assert.Null(model.GetDeclaredSymbol(breakStmt));
    }

    [Fact]
    public void GetDeclaredSymbol_OnContinueStatement_ReturnsNull()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        continue outer;
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var continueStmt = tree.GetRoot().DescendantNodes().OfType<ContinueStatementSyntax>().Single();
        Assert.Null(model.GetDeclaredSymbol(continueStmt));
    }

    [Fact]
    public void GetDeclaredSymbol_OnLabeledStatement_ReturnsLabelSymbol()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        break outer;
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var labelDecl = tree.GetRoot().DescendantNodes().OfType<LabeledStatementSyntax>().Single();
        var symbol = model.GetDeclaredSymbol(labelDecl);
        Assert.NotNull(symbol);
        Assert.Equal("outer", symbol.Name);
        Assert.Equal(SymbolKind.Label, symbol.Kind);
    }

    #endregion

    #region AnalyzeControlFlow

    [Fact]
    public void ControlFlow_LabeledBreak_IsExitPoint_WhenTargetOutsideRegion()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        while (true)
                        {
                            /*<bind>*/break outer;/*</bind>*/
                        }
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var breakStmt = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var controlFlow = model.AnalyzeControlFlow(breakStmt);

        Assert.True(controlFlow.Succeeded);
        Assert.Single(controlFlow.ExitPoints);
        Assert.IsType<BreakStatementSyntax>(controlFlow.ExitPoints.Single());
        Assert.True(controlFlow.StartPointIsReachable);
        Assert.False(controlFlow.EndPointIsReachable);
    }

    [Fact]
    public void ControlFlow_LabeledContinue_IsExitPoint_WhenTargetOutsideRegion()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        while (true)
                        {
                            /*<bind>*/continue outer;/*</bind>*/
                        }
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var continueStmt = tree.GetRoot().DescendantNodes().OfType<ContinueStatementSyntax>().Single();
        var controlFlow = model.AnalyzeControlFlow(continueStmt);

        Assert.True(controlFlow.Succeeded);
        Assert.Single(controlFlow.ExitPoints);
        Assert.IsType<ContinueStatementSyntax>(controlFlow.ExitPoints.Single());
        Assert.True(controlFlow.StartPointIsReachable);
        Assert.False(controlFlow.EndPointIsReachable);
    }

    [Fact]
    public void ControlFlow_LabeledBreak_NotExitPoint_WhenTargetInsideRegion()
    {
        var source = """
            class C
            {
                void M()
                {
                    /*<bind>*/outer: while (true)
                    {
                        break outer;
                    }/*</bind>*/
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var labelStmt = tree.GetRoot().DescendantNodes().OfType<LabeledStatementSyntax>().Single();
        var controlFlow = model.AnalyzeControlFlow(labelStmt);

        Assert.True(controlFlow.Succeeded);
        Assert.Empty(controlFlow.ExitPoints);
    }

    [Fact]
    public void ControlFlow_UnlabeledBreak_IsExitPoint_FromInnerLoop()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        /*<bind>*/break;/*</bind>*/
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var breakStmt = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var controlFlow = model.AnalyzeControlFlow(breakStmt);

        Assert.True(controlFlow.Succeeded);
        Assert.Single(controlFlow.ExitPoints);
    }

    #endregion

    #region AnalyzeDataFlow

    [Fact]
    public void DataFlow_LabeledBreak_VariableFlowsIn()
    {
        var source = """
            class C
            {
                void M(bool b)
                {
                    int x = 0;
                    outer: while (true)
                    {
                        while (true)
                        {
                            /*<bind>*/
                            x++;
                            if (b) break outer;
                            /*</bind>*/
                        }
                    }
                    System.Console.Write(x);
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var text = tree.GetText().ToString();
        int start = text.IndexOf("/*<bind>*/") + "/*<bind>*/".Length;
        int end = text.IndexOf("/*</bind>*/");

        var stmts = tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
            .Where(s => s.SpanStart >= start && s.Span.End <= end && s.Parent is BlockSyntax)
            .ToArray();

        var dataFlow = model.AnalyzeDataFlow(stmts.First(), stmts.Last());

        Assert.True(dataFlow.Succeeded);
        Assert.Contains(dataFlow.DataFlowsIn, s => s.Name == "x");
        Assert.Contains(dataFlow.DataFlowsOut, s => s.Name == "x");
        Assert.Contains(dataFlow.ReadInside, s => s.Name == "x");
        Assert.Contains(dataFlow.WrittenInside, s => s.Name == "x");
    }

    [Fact]
    public void DataFlow_LabeledContinue_VariableFlowsIn()
    {
        var source = """
            class C
            {
                void M(bool b)
                {
                    int x = 0;
                    outer: for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            /*<bind>*/
                            x++;
                            if (b) continue outer;
                            /*</bind>*/
                        }
                    }
                    System.Console.Write(x);
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var text = tree.GetText().ToString();
        int start = text.IndexOf("/*<bind>*/") + "/*<bind>*/".Length;
        int end = text.IndexOf("/*</bind>*/");

        var stmts = tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
            .Where(s => s.SpanStart >= start && s.Span.End <= end && s.Parent is BlockSyntax)
            .ToArray();

        var dataFlow = model.AnalyzeDataFlow(stmts.First(), stmts.Last());

        Assert.True(dataFlow.Succeeded);
        Assert.Contains(dataFlow.DataFlowsIn, s => s.Name == "x");
        Assert.Contains(dataFlow.DataFlowsOut, s => s.Name == "x");
    }

    #endregion

    #region LookupSymbols / GetEnclosingSymbol / speculative model

    [Fact]
    public void LookupLabels_AtLabeledBreak_FindsEnclosingLabel()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        break outer;
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var breakStatement = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var labels = model.LookupLabels(breakStatement.SpanStart);
        Assert.Contains(labels, l => l.Name == "outer");
    }

    [Fact]
    public void GetEnclosingSymbol_AtLabeledBreak_ReturnsContainingMethod()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        break outer;
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var breakStatement = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var enclosing = model.GetEnclosingSymbol(breakStatement.SpanStart);
        Assert.Equal("M", enclosing!.Name);
    }

    [Fact]
    public void SpeculativeSemanticModel_LabeledBreak_ResolvesToLabel()
    {
        var source = """
            class C
            {
                void M()
                {
                    outer: while (true)
                    {
                        System.Console.WriteLine();
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var existing = tree.GetRoot().DescendantNodes().OfType<ExpressionStatementSyntax>().Single();
        var speculative = (BreakStatementSyntax)SyntaxFactory.ParseStatement("break outer;");

        Assert.True(model.TryGetSpeculativeSemanticModel(existing.SpanStart, speculative, out var speculativeModel));

        var symbol = speculativeModel!.GetSymbolInfo(speculative.Name!).Symbol;
        Assert.Equal(SymbolKind.Label, symbol!.Kind);
        Assert.Equal("outer", symbol.Name);
    }

    #endregion
}
