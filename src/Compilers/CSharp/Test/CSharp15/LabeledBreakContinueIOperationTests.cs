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

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public sealed class LabeledBreakContinueIOperationTests : SemanticModelTestBase
{
    #region GetCorrespondingOperation — labeled break/continue targeting immediate construct

    [Fact]
    public void GetCorrespondingOperation_LabeledBreak_ImmediateWhile()
    {
        var source = """
            class C
            {
                void F()
                {
                    outer: while (true)
                    {
                        /*<bind>*/break outer;/*</bind>*/
                    }
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IWhileLoopOperation>(corresponding);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledContinue_ImmediateWhile()
    {
        var source = """
            class C
            {
                void F()
                {
                    outer: while (true)
                    {
                        /*<bind>*/continue outer;/*</bind>*/
                    }
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<ContinueStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IWhileLoopOperation>(corresponding);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledBreak_ImmediateFor()
    {
        var source = """
            class C
            {
                void F()
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        /*<bind>*/break outer;/*</bind>*/
                    }
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IForLoopOperation>(corresponding);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledContinue_ImmediateForEach()
    {
        var source = """
            class C
            {
                void F()
                {
                    outer: foreach (var x in new[] { 1 })
                    {
                        /*<bind>*/continue outer;/*</bind>*/
                    }
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<ContinueStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IForEachLoopOperation>(corresponding);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledBreak_ImmediateDoWhile()
    {
        var source = """
            class C
            {
                void F()
                {
                    outer: do
                    {
                        /*<bind>*/break outer;/*</bind>*/
                    } while (true);
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IWhileLoopOperation>(corresponding);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledBreak_ImmediateSwitch()
    {
        var source = """
            class C
            {
                void F(int x)
                {
                    outer: switch (x)
                    {
                        case 0:
                            /*<bind>*/break outer;/*</bind>*/
                    }
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<ISwitchOperation>(corresponding);
    }

    #endregion

    #region GetCorrespondingOperation — labeled break/continue targeting outer construct (skipping inner)

    [Fact]
    public void GetCorrespondingOperation_LabeledBreak_OuterWhile_SkipsInnerWhile()
    {
        var source = """
            class C
            {
                void F()
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
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IWhileLoopOperation>(corresponding);
        var whileOp = (IWhileLoopOperation)corresponding;
        Assert.Equal("while (true)", whileOp.Syntax.ToString().Split('\n')[0].Trim());
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledContinue_OuterFor_SkipsInnerWhile()
    {
        var source = """
            class C
            {
                void F()
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        while (true)
                        {
                            /*<bind>*/continue outer;/*</bind>*/
                        }
                    }
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<ContinueStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IForLoopOperation>(corresponding);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledBreak_OuterWhile_SkipsInnerSwitch()
    {
        var source = """
            class C
            {
                void F(int x)
                {
                    outer: while (true)
                    {
                        switch (x)
                        {
                            case 0:
                                /*<bind>*/break outer;/*</bind>*/
                        }
                    }
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IWhileLoopOperation>(corresponding);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledBreak_OuterSwitch_SkipsInnerSwitch()
    {
        var source = """
            class C
            {
                void F(int x)
                {
                    outer: switch (x)
                    {
                        case 0:
                            switch (x)
                            {
                                case 1:
                                    /*<bind>*/break outer;/*</bind>*/
                            }
                            break;
                    }
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<ISwitchOperation>(corresponding);
        var switchOp = (ISwitchOperation)corresponding;
        Assert.Equal("x", switchOp.Value.Syntax.ToString());
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledContinue_OuterForEach_SkipsInnerFor()
    {
        var source = """
            class C
            {
                void F()
                {
                    outer: foreach (var x in new[] { 1 })
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            /*<bind>*/continue outer;/*</bind>*/
                        }
                    }
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<ContinueStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IForEachLoopOperation>(corresponding);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledBreak_OuterDoWhile_SkipsInnerForEach()
    {
        var source = """
            class C
            {
                void F()
                {
                    outer: do
                    {
                        foreach (var x in new[] { 1 })
                        {
                            /*<bind>*/break outer;/*</bind>*/
                        }
                    } while (true);
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IWhileLoopOperation>(corresponding);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledBreak_SkipsTwoLevels()
    {
        var source = """
            class C
            {
                void F()
                {
                    outer: while (true)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            foreach (var x in new[] { 1 })
                            {
                                /*<bind>*/break outer;/*</bind>*/
                            }
                        }
                    }
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IWhileLoopOperation>(corresponding);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledContinue_SkipsSwitchAndInnerLoop()
    {
        var source = """
            class C
            {
                void F(int x)
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        switch (x)
                        {
                            case 0:
                                while (true)
                                {
                                    /*<bind>*/continue outer;/*</bind>*/
                                }
                                break;
                        }
                    }
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<ContinueStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IForLoopOperation>(corresponding);
    }

    #endregion

    #region IBranchOperation properties

    [Fact]
    public void IBranchOperation_LabeledBreak_HasBreakKind()
    {
        var source = """
            class C
            {
                void F()
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

        var breakSyntax = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var operation = model.GetOperation(breakSyntax) as IBranchOperation;

        Assert.NotNull(operation);
        Assert.Equal(BranchKind.Break, operation.BranchKind);
        Assert.NotNull(operation.Target);
    }

    [Fact]
    public void IBranchOperation_LabeledContinue_HasContinueKind()
    {
        var source = """
            class C
            {
                void F()
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

        var continueSyntax = tree.GetRoot().DescendantNodes().OfType<ContinueStatementSyntax>().Single();
        var operation = model.GetOperation(continueSyntax) as IBranchOperation;

        Assert.NotNull(operation);
        Assert.Equal(BranchKind.Continue, operation.BranchKind);
        Assert.NotNull(operation.Target);
    }

    [Fact]
    public void IBranchOperation_LabeledBreak_NestedLoop_TargetDiffersFromUnlabeled()
    {
        var source = """
            class C
            {
                void F()
                {
                    outer: while (true)
                    {
                        while (true)
                        {
                            break outer;
                            break;
                        }
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(10, 17));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var breakStmts = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().ToArray();
        Assert.Equal(2, breakStmts.Length);

        var labeledBreak = model.GetOperation(breakStmts[0]) as IBranchOperation;
        var unlabeledBreak = model.GetOperation(breakStmts[1]) as IBranchOperation;

        Assert.NotNull(labeledBreak);
        Assert.NotNull(unlabeledBreak);
        Assert.Equal(BranchKind.Break, labeledBreak.BranchKind);
        Assert.Equal(BranchKind.Break, unlabeledBreak.BranchKind);
        Assert.NotEqual(labeledBreak.Target, unlabeledBreak.Target);
    }

    [Fact]
    public void IBranchOperation_LabeledBreak_TargetMatchesOuterExitLabel()
    {
        var source = """
            class C
            {
                void F()
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            break outer;
                        }
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,37): warning CS0162: Unreachable code detected
            //             for (int j = 0; j < 10; j++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(7, 37));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var forStmts = tree.GetRoot().DescendantNodes().OfType<ForStatementSyntax>().ToArray();
        Assert.Equal(2, forStmts.Length);

        var outerFor = (IForLoopOperation)model.GetOperation(forStmts[0])!;
        var innerFor = (IForLoopOperation)model.GetOperation(forStmts[1])!;

        var breakSyntax = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var branch = (IBranchOperation)model.GetOperation(breakSyntax)!;

        Assert.Equal(BranchKind.Break, branch.BranchKind);
        Assert.Same(outerFor.ExitLabel, branch.Target);
        Assert.NotSame(innerFor.ExitLabel, branch.Target);
    }

    [Fact]
    public void IBranchOperation_LabeledContinue_TargetMatchesOuterContinueLabel()
    {
        var source = """
            class C
            {
                void F()
                {
                    outer: for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            continue outer;
                        }
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,37): warning CS0162: Unreachable code detected
            //             for (int j = 0; j < 10; j++)
            Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(7, 37));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var forStmts = tree.GetRoot().DescendantNodes().OfType<ForStatementSyntax>().ToArray();
        var outerFor = (IForLoopOperation)model.GetOperation(forStmts[0])!;
        var innerFor = (IForLoopOperation)model.GetOperation(forStmts[1])!;

        var continueSyntax = tree.GetRoot().DescendantNodes().OfType<ContinueStatementSyntax>().Single();
        var branch = (IBranchOperation)model.GetOperation(continueSyntax)!;

        Assert.Equal(BranchKind.Continue, branch.BranchKind);
        Assert.Same(outerFor.ContinueLabel, branch.Target);
        Assert.NotSame(innerFor.ContinueLabel, branch.Target);
    }

    [Fact]
    public void IBranchOperation_LabeledBreak_TargetMatchesForeachExitLabel()
    {
        var source = """
            class C
            {
                void F()
                {
                    outer: foreach (var x in new[] { 1 })
                    {
                        foreach (var y in new[] { 2 })
                        {
                            break outer;
                        }
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var foreachStmts = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().ToArray();
        var outerForeach = (IForEachLoopOperation)model.GetOperation(foreachStmts[0])!;

        var breakSyntax = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var branch = (IBranchOperation)model.GetOperation(breakSyntax)!;

        Assert.Equal(BranchKind.Break, branch.BranchKind);
        Assert.Same(outerForeach.ExitLabel, branch.Target);
    }

    [Fact]
    public void IBranchOperation_LabeledBreak_TargetMatchesSwitchExitLabel()
    {
        var source = """
            class C
            {
                void F(int x)
                {
                    outer: switch (x)
                    {
                        case 0:
                            while (true)
                            {
                                break outer;
                            }
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var switchSyntax = tree.GetRoot().DescendantNodes().OfType<SwitchStatementSyntax>().Single();
        var switchOp = (ISwitchOperation)model.GetOperation(switchSyntax)!;

        var breakSyntax = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var branch = (IBranchOperation)model.GetOperation(breakSyntax)!;

        Assert.Equal(BranchKind.Break, branch.BranchKind);
        Assert.Same(switchOp.ExitLabel, branch.Target);
    }

    [Fact]
    public void IBranchOperation_InvalidLabeledBreak_LabelNotOnLoop()
    {
        var source = """
            class C
            {
                void F()
                {
                    L: { while (true) { break L; } }
                }
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (5,35): error CS9379: No enclosing loop or switch statement with the label 'L' out of which to break or continue
            //         L: { while (true) { break L; } }
            Diagnostic(ErrorCode.ERR_NoBreakOrContId, "L").WithArguments("L").WithLocation(5, 35),
            // (5,9): warning CS0164: This label has not been referenced
            //         L: { while (true) { break L; } }
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L").WithLocation(5, 9));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var breakSyntax = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var op = model.GetOperation(breakSyntax);

        Assert.NotNull(op);
        Assert.Equal(OperationKind.Invalid, op!.Kind);
    }

    #endregion

    #region Helpers

    private IOperation? GetCorrespondingOperation<TSyntax>(string source) where TSyntax : SyntaxNode
    {
        var compilation = CreateCompilation(source);
        var inner = GetOperationAndSyntaxForTest<TSyntax>(compilation).operation as IBranchOperation;
        return inner?.GetCorrespondingOperation();
    }

    #endregion
}
