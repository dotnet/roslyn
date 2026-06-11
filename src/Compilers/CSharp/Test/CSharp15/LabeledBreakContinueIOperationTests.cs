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
                    /*<bind>*/outer: while (true)
                    {
                        break outer;
                    }/*</bind>*/
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IWhileLoopOperation>(corresponding);

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: whil ... }')
              Statement:
                IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'while (true ... }')
                  Condition:
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'break outer;')
                  IgnoredCondition:
                    null
            """, DiagnosticDescription.None);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledContinue_ImmediateWhile()
    {
        var source = """
            class C
            {
                void F()
                {
                    /*<bind>*/outer: while (true)
                    {
                        continue outer;
                    }/*</bind>*/
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<ContinueStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IWhileLoopOperation>(corresponding);

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: whil ... }')
              Statement:
                IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'while (true ... }')
                  Condition:
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      IBranchOperation (BranchKind.Continue, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'continue outer;')
                  IgnoredCondition:
                    null
            """, DiagnosticDescription.None);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledBreak_ImmediateFor()
    {
        var source = """
            class C
            {
                void F()
                {
                    /*<bind>*/outer: for (int i = 0; i < 10; i++)
                    {
                        break outer;
                    }/*</bind>*/
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IForLoopOperation>(corresponding);

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: for  ... }')
              Statement:
                IForLoopOperation (LoopKind.For, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
                  Locals: Local_1: System.Int32 i
                  Condition:
                    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i < 10')
                      Left:
                        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                      Right:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                  Before:
                      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
                        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
                          Declarators:
                              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                                Initializer:
                                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                          Initializer:
                            null
                  AtLoopBottom:
                      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i++')
                        Expression:
                          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'i++')
                            Target:
                              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'break outer;')
            """, new[]
            {
                // (5,50): warning CS0162: Unreachable code detected
                //         /*<bind>*/outer: for (int i = 0; i < 10; i++)
                Diagnostic(ErrorCode.WRN_UnreachableCode, "i").WithLocation(5, 50),
            });
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledContinue_ImmediateForEach()
    {
        var source = """
            class C
            {
                void F()
                {
                    /*<bind>*/outer: foreach (var x in new[] { 1 })
                    {
                        continue outer;
                    }/*</bind>*/
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<ContinueStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IForEachLoopOperation>(corresponding);

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: fore ... }')
              Statement:
                IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
                  Locals: Local_1: System.Int32 x
                  LoopControlVariable:
                    IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
                      Initializer:
                        null
                  Collection:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'new[] { 1 }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand:
                        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new[] { 1 }')
                          Dimension Sizes(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[] { 1 }')
                          Initializer:
                            IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ 1 }')
                              Element Values(1):
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      IBranchOperation (BranchKind.Continue, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'continue outer;')
                  NextVariables(0)
            """, DiagnosticDescription.None);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledBreak_ImmediateDoWhile()
    {
        var source = """
            class C
            {
                void F()
                {
                    /*<bind>*/outer: do
                    {
                        break outer;
                    } while (true);/*</bind>*/
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IWhileLoopOperation>(corresponding);

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: do ... ile (true);')
              Statement:
                IWhileLoopOperation (ConditionIsTop: False, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'do ... ile (true);')
                  Condition:
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'break outer;')
                  IgnoredCondition:
                    null
            """, DiagnosticDescription.None);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledBreak_ImmediateSwitch()
    {
        var source = """
            class C
            {
                void F(int x)
                {
                    /*<bind>*/outer: switch (x)
                    {
                        case 0:
                            break outer;
                    }/*</bind>*/
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<ISwitchOperation>(corresponding);

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: swit ... }')
              Statement:
                ISwitchOperation (1 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null) (Syntax: 'switch (x) ... }')
                  Switch expression:
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Sections:
                      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case 0: ... reak outer;')
                          Clauses:
                              ISingleValueCaseClauseOperation (Label Id: 1) (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: 'case 0:')
                                Value:
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                          Body:
                              IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'break outer;')
            """, DiagnosticDescription.None);
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
                    /*<bind>*/outer: while (true)
                    {
                        while (true)
                        {
                            break outer;
                        }
                    }/*</bind>*/
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IWhileLoopOperation>(corresponding);
        var whileOp = (IWhileLoopOperation)corresponding;
        Assert.Equal("while (true)", whileOp.Syntax.ToString().Split('\n')[0].Trim());

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: whil ... }')
              Statement:
                IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'while (true ... }')
                  Condition:
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'while (true ... }')
                        Condition:
                          ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                        Body:
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                            IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'break outer;')
                        IgnoredCondition:
                          null
                  IgnoredCondition:
                    null
            """, DiagnosticDescription.None);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledContinue_OuterFor_SkipsInnerWhile()
    {
        var source = """
            class C
            {
                void F()
                {
                    /*<bind>*/outer: for (int i = 0; i < 10; i++)
                    {
                        while (true)
                        {
                            continue outer;
                        }
                    }/*</bind>*/
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<ContinueStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IForLoopOperation>(corresponding);

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: for  ... }')
              Statement:
                IForLoopOperation (LoopKind.For, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
                  Locals: Local_1: System.Int32 i
                  Condition:
                    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i < 10')
                      Left:
                        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                      Right:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                  Before:
                      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
                        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
                          Declarators:
                              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                                Initializer:
                                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                          Initializer:
                            null
                  AtLoopBottom:
                      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i++')
                        Expression:
                          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'i++')
                            Target:
                              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'while (true ... }')
                        Condition:
                          ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                        Body:
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                            IBranchOperation (BranchKind.Continue, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'continue outer;')
                        IgnoredCondition:
                          null
            """, DiagnosticDescription.None);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledBreak_OuterWhile_SkipsInnerSwitch()
    {
        var source = """
            class C
            {
                void F(int x)
                {
                    /*<bind>*/outer: while (true)
                    {
                        switch (x)
                        {
                            case 0:
                                break outer;
                        }
                    }/*</bind>*/
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IWhileLoopOperation>(corresponding);

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: whil ... }')
              Statement:
                IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'while (true ... }')
                  Condition:
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      ISwitchOperation (1 cases, Exit Label Id: 2) (OperationKind.Switch, Type: null) (Syntax: 'switch (x) ... }')
                        Switch expression:
                          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                        Sections:
                            ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case 0: ... reak outer;')
                                Clauses:
                                    ISingleValueCaseClauseOperation (Label Id: 3) (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: 'case 0:')
                                      Value:
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                Body:
                                    IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'break outer;')
                  IgnoredCondition:
                    null
            """, DiagnosticDescription.None);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledBreak_OuterSwitch_SkipsInnerSwitch()
    {
        var source = """
            class C
            {
                void F(int x)
                {
                    /*<bind>*/outer: switch (x)
                    {
                        case 0:
                            switch (x)
                            {
                                case 1:
                                    break outer;
                            }
                            break;
                    }/*</bind>*/
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<ISwitchOperation>(corresponding);
        var switchOp = (ISwitchOperation)corresponding;
        Assert.Equal("x", switchOp.Value.Syntax.ToString());

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: swit ... }')
              Statement:
                ISwitchOperation (1 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null) (Syntax: 'switch (x) ... }')
                  Switch expression:
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Sections:
                      ISwitchCaseOperation (1 case clauses, 2 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case 0: ... break;')
                          Clauses:
                              ISingleValueCaseClauseOperation (Label Id: 1) (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: 'case 0:')
                                Value:
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                          Body:
                              ISwitchOperation (1 cases, Exit Label Id: 2) (OperationKind.Switch, Type: null) (Syntax: 'switch (x) ... }')
                                Switch expression:
                                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                                Sections:
                                    ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case 1: ... reak outer;')
                                        Clauses:
                                            ISingleValueCaseClauseOperation (Label Id: 3) (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: 'case 1:')
                                              Value:
                                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                        Body:
                                            IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'break outer;')
                              IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'break;')
            """, DiagnosticDescription.None);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledContinue_OuterForEach_SkipsInnerFor()
    {
        var source = """
            class C
            {
                void F()
                {
                    /*<bind>*/outer: foreach (var x in new[] { 1 })
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            continue outer;
                        }
                    }/*</bind>*/
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<ContinueStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IForEachLoopOperation>(corresponding);

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: fore ... }')
              Statement:
                IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
                  Locals: Local_1: System.Int32 x
                  LoopControlVariable:
                    IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
                      Initializer:
                        null
                  Collection:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'new[] { 1 }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand:
                        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new[] { 1 }')
                          Dimension Sizes(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[] { 1 }')
                          Initializer:
                            IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ 1 }')
                              Element Values(1):
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      IForLoopOperation (LoopKind.For, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
                        Locals: Local_1: System.Int32 i
                        Condition:
                          IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i < 10')
                            Left:
                              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                            Right:
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                        Before:
                            IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
                              IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
                                Declarators:
                                    IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                                      Initializer:
                                        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                Initializer:
                                  null
                        AtLoopBottom:
                            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i++')
                              Expression:
                                IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'i++')
                                  Target:
                                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        Body:
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                            IBranchOperation (BranchKind.Continue, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'continue outer;')
                  NextVariables(0)
            """, new[]
            {
                // (7,37): warning CS0162: Unreachable code detected
                //             for (int i = 0; i < 10; i++)
                Diagnostic(ErrorCode.WRN_UnreachableCode, "i").WithLocation(7, 37),
            });
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledBreak_OuterDoWhile_SkipsInnerForEach()
    {
        var source = """
            class C
            {
                void F()
                {
                    /*<bind>*/outer: do
                    {
                        foreach (var x in new[] { 1 })
                        {
                            break outer;
                        }
                    } while (true);/*</bind>*/
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IWhileLoopOperation>(corresponding);

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: do ... ile (true);')
              Statement:
                IWhileLoopOperation (ConditionIsTop: False, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'do ... ile (true);')
                  Condition:
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
                        Locals: Local_1: System.Int32 x
                        LoopControlVariable:
                          IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
                            Initializer:
                              null
                        Collection:
                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'new[] { 1 }')
                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            Operand:
                              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new[] { 1 }')
                                Dimension Sizes(1):
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[] { 1 }')
                                Initializer:
                                  IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ 1 }')
                                    Element Values(1):
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        Body:
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                            IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'break outer;')
                        NextVariables(0)
                  IgnoredCondition:
                    null
            """, DiagnosticDescription.None);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledBreak_SkipsTwoLevels()
    {
        var source = """
            class C
            {
                void F()
                {
                    /*<bind>*/outer: while (true)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            foreach (var x in new[] { 1 })
                            {
                                break outer;
                            }
                        }
                    }/*</bind>*/
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<BreakStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IWhileLoopOperation>(corresponding);

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: whil ... }')
              Statement:
                IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'while (true ... }')
                  Condition:
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      IForLoopOperation (LoopKind.For, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
                        Locals: Local_1: System.Int32 i
                        Condition:
                          IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i < 10')
                            Left:
                              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                            Right:
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                        Before:
                            IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
                              IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
                                Declarators:
                                    IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                                      Initializer:
                                        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                Initializer:
                                  null
                        AtLoopBottom:
                            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i++')
                              Expression:
                                IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'i++')
                                  Target:
                                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        Body:
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                            IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 4, Exit Label Id: 5) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
                              Locals: Local_1: System.Int32 x
                              LoopControlVariable:
                                IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
                                  Initializer:
                                    null
                              Collection:
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'new[] { 1 }')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                  Operand:
                                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new[] { 1 }')
                                      Dimension Sizes(1):
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[] { 1 }')
                                      Initializer:
                                        IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ 1 }')
                                          Element Values(1):
                                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                              Body:
                                IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                                  IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'break outer;')
                              NextVariables(0)
                  IgnoredCondition:
                    null
            """, DiagnosticDescription.None);
    }

    [Fact]
    public void GetCorrespondingOperation_LabeledContinue_SkipsSwitchAndInnerLoop()
    {
        var source = """
            class C
            {
                void F(int x)
                {
                    /*<bind>*/outer: for (int i = 0; i < 10; i++)
                    {
                        switch (x)
                        {
                            case 0:
                                while (true)
                                {
                                    continue outer;
                                }
                                break;
                        }
                    }/*</bind>*/
                }
            }
            """;
        var corresponding = GetCorrespondingOperation<ContinueStatementSyntax>(source);
        Assert.NotNull(corresponding);
        Assert.IsAssignableFrom<IForLoopOperation>(corresponding);

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: for  ... }')
              Statement:
                IForLoopOperation (LoopKind.For, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
                  Locals: Local_1: System.Int32 i
                  Condition:
                    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i < 10')
                      Left:
                        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                      Right:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                  Before:
                      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
                        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
                          Declarators:
                              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                                Initializer:
                                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                          Initializer:
                            null
                  AtLoopBottom:
                      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i++')
                        Expression:
                          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'i++')
                            Target:
                              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      ISwitchOperation (1 cases, Exit Label Id: 2) (OperationKind.Switch, Type: null) (Syntax: 'switch (x) ... }')
                        Switch expression:
                          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                        Sections:
                            ISwitchCaseOperation (1 case clauses, 2 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case 0: ... break;')
                                Clauses:
                                    ISingleValueCaseClauseOperation (Label Id: 3) (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: 'case 0:')
                                      Value:
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                Body:
                                    IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 4, Exit Label Id: 5) (OperationKind.Loop, Type: null) (Syntax: 'while (true ... }')
                                      Condition:
                                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                                      Body:
                                        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                                          IBranchOperation (BranchKind.Continue, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'continue outer;')
                                      IgnoredCondition:
                                        null
                                    IBranchOperation (BranchKind.Break, Label Id: 2) (OperationKind.Branch, Type: null) (Syntax: 'break;')
            """, new[]
            {
                // (14,21): warning CS0162: Unreachable code detected
                //                     break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(14, 21),
            });
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

        var breakSyntax = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var operation = model.GetOperation(breakSyntax) as IBranchOperation;

        Assert.NotNull(operation);
        Assert.Equal(BranchKind.Break, operation.BranchKind);
        Assert.NotNull(operation.Target);

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: whil ... }')
              Statement:
                IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'while (true ... }')
                  Condition:
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'break outer;')
                  IgnoredCondition:
                    null
            """, DiagnosticDescription.None);
    }

    [Fact]
    public void IBranchOperation_LabeledContinue_HasContinueKind()
    {
        var source = """
            class C
            {
                void F()
                {
                    /*<bind>*/outer: while (true)
                    {
                        continue outer;
                    }/*</bind>*/
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

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: whil ... }')
              Statement:
                IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'while (true ... }')
                  Condition:
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      IBranchOperation (BranchKind.Continue, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'continue outer;')
                  IgnoredCondition:
                    null
            """, DiagnosticDescription.None);
    }

    [Fact]
    public void IBranchOperation_LabeledBreak_NestedLoop_TargetDiffersFromUnlabeled()
    {
        var source = """
            class C
            {
                void F()
                {
                    /*<bind>*/outer: while (true)
                    {
                        while (true)
                        {
                            break outer;
                            break;
                        }
                    }/*</bind>*/
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

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: whil ... }')
              Statement:
                IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'while (true ... }')
                  Condition:
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'while (true ... }')
                        Condition:
                          ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                        Body:
                          IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                            IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'break outer;')
                            IBranchOperation (BranchKind.Break, Label Id: 3) (OperationKind.Branch, Type: null) (Syntax: 'break;')
                        IgnoredCondition:
                          null
                  IgnoredCondition:
                    null
            """, new[]
            {
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(10, 17),
            });
    }

    [Fact]
    public void IBranchOperation_LabeledBreak_TargetMatchesOuterExitLabel()
    {
        var source = """
            class C
            {
                void F()
                {
                    /*<bind>*/outer: for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            break outer;
                        }
                    }/*</bind>*/
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

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: for  ... }')
              Statement:
                IForLoopOperation (LoopKind.For, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
                  Locals: Local_1: System.Int32 i
                  Condition:
                    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i < 10')
                      Left:
                        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                      Right:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                  Before:
                      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
                        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
                          Declarators:
                              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                                Initializer:
                                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                          Initializer:
                            null
                  AtLoopBottom:
                      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i++')
                        Expression:
                          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'i++')
                            Target:
                              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      IForLoopOperation (LoopKind.For, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'for (int j  ... }')
                        Locals: Local_1: System.Int32 j
                        Condition:
                          IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'j < 10')
                            Left:
                              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                            Right:
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                        Before:
                            IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int j = 0')
                              IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int j = 0')
                                Declarators:
                                    IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = 0')
                                      Initializer:
                                        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                Initializer:
                                  null
                        AtLoopBottom:
                            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j++')
                              Expression:
                                IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'j++')
                                  Target:
                                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                        Body:
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                            IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'break outer;')
            """, new[]
            {
                // (7,37): warning CS0162: Unreachable code detected
                //             for (int j = 0; j < 10; j++)
                Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(7, 37),
            });
    }

    [Fact]
    public void IBranchOperation_LabeledContinue_TargetMatchesOuterContinueLabel()
    {
        var source = """
            class C
            {
                void F()
                {
                    /*<bind>*/outer: for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            continue outer;
                        }
                    }/*</bind>*/
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

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: for  ... }')
              Statement:
                IForLoopOperation (LoopKind.For, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
                  Locals: Local_1: System.Int32 i
                  Condition:
                    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i < 10')
                      Left:
                        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                      Right:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                  Before:
                      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
                        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
                          Declarators:
                              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                                Initializer:
                                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                          Initializer:
                            null
                  AtLoopBottom:
                      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i++')
                        Expression:
                          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'i++')
                            Target:
                              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      IForLoopOperation (LoopKind.For, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'for (int j  ... }')
                        Locals: Local_1: System.Int32 j
                        Condition:
                          IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'j < 10')
                            Left:
                              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                            Right:
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                        Before:
                            IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int j = 0')
                              IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int j = 0')
                                Declarators:
                                    IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = 0')
                                      Initializer:
                                        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                Initializer:
                                  null
                        AtLoopBottom:
                            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j++')
                              Expression:
                                IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'j++')
                                  Target:
                                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                        Body:
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                            IBranchOperation (BranchKind.Continue, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'continue outer;')
            """, new[]
            {
                // (7,37): warning CS0162: Unreachable code detected
                //             for (int j = 0; j < 10; j++)
                Diagnostic(ErrorCode.WRN_UnreachableCode, "j").WithLocation(7, 37),
            });
    }

    [Fact]
    public void IBranchOperation_LabeledBreak_TargetMatchesForeachExitLabel()
    {
        var source = """
            class C
            {
                void F()
                {
                    /*<bind>*/outer: foreach (var x in new[] { 1 })
                    {
                        foreach (var y in new[] { 2 })
                        {
                            break outer;
                        }
                    }/*</bind>*/
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

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: fore ... }')
              Statement:
                IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
                  Locals: Local_1: System.Int32 x
                  LoopControlVariable:
                    IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
                      Initializer:
                        null
                  Collection:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'new[] { 1 }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand:
                        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new[] { 1 }')
                          Dimension Sizes(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[] { 1 }')
                          Initializer:
                            IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ 1 }')
                              Element Values(1):
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  Body:
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                      IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
                        Locals: Local_1: System.Int32 y
                        LoopControlVariable:
                          IVariableDeclaratorOperation (Symbol: System.Int32 y) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
                            Initializer:
                              null
                        Collection:
                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'new[] { 2 }')
                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            Operand:
                              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new[] { 2 }')
                                Dimension Sizes(1):
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[] { 2 }')
                                Initializer:
                                  IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ 2 }')
                                    Element Values(1):
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                        Body:
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                            IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'break outer;')
                        NextVariables(0)
                  NextVariables(0)
            """, DiagnosticDescription.None);
    }

    [Fact]
    public void IBranchOperation_LabeledBreak_TargetMatchesSwitchExitLabel()
    {
        var source = """
            class C
            {
                void F(int x)
                {
                    /*<bind>*/outer: switch (x)
                    {
                        case 0:
                            while (true)
                            {
                                break outer;
                            }
                    }/*</bind>*/
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

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: outer) (OperationKind.Labeled, Type: null) (Syntax: 'outer: swit ... }')
              Statement:
                ISwitchOperation (1 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null) (Syntax: 'switch (x) ... }')
                  Switch expression:
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Sections:
                      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case 0: ... }')
                          Clauses:
                              ISingleValueCaseClauseOperation (Label Id: 1) (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: 'case 0:')
                                Value:
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                          Body:
                              IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'while (true ... }')
                                Condition:
                                  ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                                Body:
                                  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                                    IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'break outer;')
                                IgnoredCondition:
                                  null
            """, DiagnosticDescription.None);
    }

    [Fact]
    public void IBranchOperation_InvalidLabeledBreak_LabelNotOnLoop()
    {
        var source = """
            class C
            {
                void F()
                {
                    /*<bind>*/L: { while (true) { break L; } }/*</bind>*/
                }
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (5,45): error CS9391: No enclosing loop or switch statement with the label 'L' out of which to break
            //         /*<bind>*/L: { while (true) { break L; } }/*</bind>*/
            Diagnostic(ErrorCode.ERR_NoBreakId, "L").WithArguments("L").WithLocation(5, 45),
            // (5,19): warning CS0164: This label has not been referenced
            //         /*<bind>*/L: { while (true) { break L; } }/*</bind>*/
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L").WithLocation(5, 19));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var breakSyntax = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var op = model.GetOperation(breakSyntax);

        Assert.NotNull(op);
        Assert.Equal(OperationKind.Invalid, op!.Kind);

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: L) (OperationKind.Labeled, Type: null, IsInvalid) (Syntax: 'L: { while  ... reak L; } }')
              Statement:
                IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ while (tr ... reak L; } }')
                  IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'while (true ...  break L; }')
                    Condition:
                      ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                    Body:
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ break L; }')
                        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'break L;')
                          Children(0)
                    IgnoredCondition:
                      null
            """, new[]
            {
                // (5,45): error CS9391: No enclosing loop or switch statement with the label 'L' out of which to break
                //         /*<bind>*/L: { while (true) { break L; } }/*</bind>*/
                Diagnostic(ErrorCode.ERR_NoBreakId, "L").WithArguments("L").WithLocation(5, 45),
                // (5,19): warning CS0164: This label has not been referenced
                //         /*<bind>*/L: { while (true) { break L; } }/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L").WithLocation(5, 19),
            });
    }

    #endregion

    #region Helpers

    private static IOperation? GetCorrespondingOperation<TSyntax>(string source) where TSyntax : SyntaxNode
    {
        var compilation = CreateCompilation(source);
        var tree = compilation.SyntaxTrees.Single();
        var node = tree.GetRoot().DescendantNodes().OfType<TSyntax>()
            .Single(n => n is BreakStatementSyntax { Name: not null }
                      || n is ContinueStatementSyntax { Name: not null });
        var op = compilation.GetSemanticModel(tree).GetOperation(node) as IBranchOperation;
        return op?.GetCorrespondingOperation();
    }

    #endregion
}
