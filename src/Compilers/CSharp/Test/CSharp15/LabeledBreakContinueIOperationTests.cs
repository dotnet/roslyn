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
            Diagnostic(ErrorCode.ERR_NoBreakId, "L").WithArguments("L").WithLocation(5, 45));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var breakSyntax = tree.GetRoot().DescendantNodes().OfType<BreakStatementSyntax>().Single();
        var op = model.GetOperation(breakSyntax);

        Assert.NotNull(op);
        Assert.Equal(OperationKind.Branch, op!.Kind);

        VerifyOperationTreeAndDiagnosticsForTest<LabeledStatementSyntax>(source, """
            ILabeledOperation (Label: L) (OperationKind.Labeled, Type: null, IsInvalid) (Syntax: 'L: { while  ... reak L; } }')
              Statement:
                IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ while (tr ... reak L; } }')
                  IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'while (true ...  break L; }')
                    Condition:
                      ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                    Body:
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ break L; }')
                        IBranchOperation (BranchKind.Break, Label: L) (OperationKind.Branch, Type: null, IsInvalid) (Syntax: 'break L;')
                    IgnoredCondition:
                      null
            """, new[]
            {
                // (5,45): error CS9391: No enclosing loop or switch statement with the label 'L' out of which to break
                //         /*<bind>*/L: { while (true) { break L; } }/*</bind>*/
                Diagnostic(ErrorCode.ERR_NoBreakId, "L").WithArguments("L").WithLocation(5, 45),
            });
    }

    #endregion

    #region ControlFlowGraph

    [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
    [Fact]
    public void ControlFlowGraph_LabeledBreak_OutOfNestedLoops()
    {
        string source = """
            class C
            {
                void M(bool b)
                /*<bind>*/{
                    outer: while (b)
                    {
                        while (b)
                        {
                            if (b)
                                break outer;
                        }
                    }
                }/*</bind>*/
            }
            """;
        string expectedFlowGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
            Block[B1] - Block
                Predecessors: [B0] [B2]
                Statements (0)
                Jump if False (Regular) to Block[B4]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                Next (Regular) Block[B2]
            Block[B2] - Block
                Predecessors: [B1] [B3]
                Statements (0)
                Jump if False (Regular) to Block[B1]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (0)
                Jump if False (Regular) to Block[B2]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                Next (Regular) Block[B4]
            Block[B4] - Exit
                Predecessors: [B1] [B3]
                Statements (0)
            """;
        var expectedDiagnostics = DiagnosticDescription.None;
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
    }

    [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
    [Fact]
    public void ControlFlowGraph_LabeledContinue_ToOuterLoop()
    {
        string source = """
            class C
            {
                void M(bool b)
                /*<bind>*/{
                    outer: while (b)
                    {
                        while (b)
                        {
                            if (b)
                                continue outer;
                        }
                    }
                }/*</bind>*/
            }
            """;
        string expectedFlowGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
            Block[B1] - Block
                Predecessors: [B0] [B2] [B3]
                Statements (0)
                Jump if False (Regular) to Block[B4]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                Next (Regular) Block[B2]
            Block[B2] - Block
                Predecessors: [B1] [B3]
                Statements (0)
                Jump if False (Regular) to Block[B1]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (0)
                Jump if False (Regular) to Block[B2]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                Next (Regular) Block[B1]
            Block[B4] - Exit
                Predecessors: [B1]
                Statements (0)
            """;
        var expectedDiagnostics = DiagnosticDescription.None;
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
    }

    [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
    public void ControlFlowGraph_LabeledBreak_OutOfNestedSwitches()
    {
        string source = """
            class C
            {
                void M(int x)
                /*<bind>*/{
                    outer: switch (x)
                    {
                        case 0:
                            switch (x)
                            {
                                case 1:
                                    break outer;
                            }
                            break;
                    }
                }/*</bind>*/
            }
            """;
        string expectedFlowGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                CaptureIds: [0]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                          Value:
                            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                    Jump if False (Regular) to Block[B3]
                        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '0')
                          Left:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                        Leaving: {R1}
                    Next (Regular) Block[B2]
                        Entering: {R2}
                .locals {R2}
                {
                    CaptureIds: [1]
                    Block[B2] - Block
                        Predecessors: [B1]
                        Statements (1)
                            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                              Value:
                                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                        Jump if False (Regular) to Block[B3]
                            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
                              Left:
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                              Right:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            Leaving: {R2} {R1}
                        Next (Regular) Block[B3]
                            Leaving: {R2} {R1}
                }
            }
            Block[B3] - Exit
                Predecessors: [B1] [B2*2]
                Statements (0)
            """;
        var expectedDiagnostics = DiagnosticDescription.None;
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
    }

    [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
    public void ControlFlowGraph_LabeledBreak_OutOfTryFinallyInLoops()
    {
        string source = """
            class C
            {
                void M(bool b)
                /*<bind>*/{
                    outer: while (b)
                    {
                        while (b)
                        {
                            try
                            {
                                break outer;
                            }
                            finally
                            {
                                b = false;
                            }
                        }
                    }
                }/*</bind>*/
            }
            """;
        string expectedFlowGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
            Block[B1] - Block
                Predecessors: [B0] [B2]
                Statements (0)
                Jump if False (Regular) to Block[B5]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                Next (Regular) Block[B2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (0)
                Jump if False (Regular) to Block[B1]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                Next (Regular) Block[B3]
                    Entering: {R1} {R2}
            .try {R1, R2}
            {
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (0)
                    Next (Regular) Block[B5]
                        Finalizing: {R3}
                        Leaving: {R2} {R1}
            }
            .finally {R3}
            {
                Block[B4] - Block
                    Predecessors (0)
                    Statements (1)
                        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = false;')
                          Expression:
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = false')
                              Left:
                                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                              Right:
                                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
                    Next (StructuredExceptionHandling) Block[null]
            }
            Block[B5] - Exit
                Predecessors: [B1] [B3]
                Statements (0)
            """;
        var expectedDiagnostics = DiagnosticDescription.None;
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
    }

    [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
    public void ControlFlowGraph_LabeledContinue_OutOfTryFinallyInLoops()
    {
        string source = """
            class C
            {
                void M(bool b)
                /*<bind>*/{
                    outer: while (b)
                    {
                        while (b)
                        {
                            try
                            {
                                continue outer;
                            }
                            finally
                            {
                                b = false;
                            }
                        }
                    }
                }/*</bind>*/
            }
            """;
        string expectedFlowGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
            Block[B1] - Block
                Predecessors: [B0] [B2] [B3]
                Statements (0)
                Jump if False (Regular) to Block[B5]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                Next (Regular) Block[B2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (0)
                Jump if False (Regular) to Block[B1]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                Next (Regular) Block[B3]
                    Entering: {R1} {R2}
            .try {R1, R2}
            {
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (0)
                    Next (Regular) Block[B1]
                        Finalizing: {R3}
                        Leaving: {R2} {R1}
            }
            .finally {R3}
            {
                Block[B4] - Block
                    Predecessors (0)
                    Statements (1)
                        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = false;')
                          Expression:
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = false')
                              Left:
                                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                              Right:
                                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
                    Next (StructuredExceptionHandling) Block[null]
            }
            Block[B5] - Exit
                Predecessors: [B1]
                Statements (0)
            """;
        var expectedDiagnostics = DiagnosticDescription.None;
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
    }

    [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
    public void ControlFlowGraph_LabeledBreak_OutOfUsingInLoops()
    {
        string source = """
            class C
            {
                void M(bool b, System.IDisposable d)
                /*<bind>*/{
                    outer: while (b)
                    {
                        while (b)
                        {
                            using (d)
                            {
                                break outer;
                            }
                        }
                    }
                }/*</bind>*/
            }
            """;
        string expectedFlowGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
            Block[B1] - Block
                Predecessors: [B0] [B2]
                Statements (0)
                Jump if False (Regular) to Block[B8]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                Next (Regular) Block[B2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (0)
                Jump if False (Regular) to Block[B1]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                Next (Regular) Block[B3]
                    Entering: {R1}
            .locals {R1}
            {
                CaptureIds: [0]
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
                          Value:
                            IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.IDisposable) (Syntax: 'd')
                    Next (Regular) Block[B4]
                        Entering: {R2} {R3}
                .try {R2, R3}
                {
                    Block[B4] - Block
                        Predecessors: [B3]
                        Statements (0)
                        Next (Regular) Block[B8]
                            Finalizing: {R4}
                            Leaving: {R3} {R2} {R1}
                }
                .finally {R4}
                {
                    Block[B5] - Block
                        Predecessors (0)
                        Statements (0)
                        Jump if True (Regular) to Block[B7]
                            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd')
                              Operand:
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'd')
                        Next (Regular) Block[B6]
                    Block[B6] - Block
                        Predecessors: [B5]
                        Statements (1)
                            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'd')
                              Instance Receiver:
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'd')
                              Arguments(0)
                        Next (Regular) Block[B7]
                    Block[B7] - Block
                        Predecessors: [B5] [B6]
                        Statements (0)
                        Next (StructuredExceptionHandling) Block[null]
                }
            }
            Block[B8] - Exit
                Predecessors: [B1] [B4]
                Statements (0)
            """;
        var expectedDiagnostics = DiagnosticDescription.None;
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
    }

    [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
    public void ControlFlowGraph_LabeledBreak_OutOfTryFinallyInSwitch()
    {
        string source = """
            class C
            {
                void M(int x)
                /*<bind>*/{
                    outer: switch (x)
                    {
                        case 0:
                            try
                            {
                                break outer;
                            }
                            finally
                            {
                                x = 0;
                            }
                    }
                }/*</bind>*/
            }
            """;
        string expectedFlowGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                CaptureIds: [0]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                          Value:
                            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                    Jump if False (Regular) to Block[B4]
                        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '0')
                          Left:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                        Leaving: {R1}
                    Next (Regular) Block[B2]
                        Entering: {R2} {R3}
                .try {R2, R3}
                {
                    Block[B2] - Block
                        Predecessors: [B1]
                        Statements (0)
                        Next (Regular) Block[B4]
                            Finalizing: {R4}
                            Leaving: {R3} {R2} {R1}
                }
                .finally {R4}
                {
                    Block[B3] - Block
                        Predecessors (0)
                        Statements (1)
                            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 0;')
                              Expression:
                                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 0')
                                  Left:
                                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                                  Right:
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                        Next (StructuredExceptionHandling) Block[null]
                }
            }
            Block[B4] - Exit
                Predecessors: [B1] [B2]
                Statements (0)
            """;
        var expectedDiagnostics = DiagnosticDescription.None;
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
    }

    [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
    public void ControlFlowGraph_InvalidLabeledBreak_LabelNotOnLoop()
    {
        string source = """
            class C
            {
                void F()
                /*<bind>*/{
                    L: { while (true) { break L; } }
                }/*</bind>*/
            }
            """;
        string expectedFlowGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
            Block[B1] - Block
                Predecessors: [B0] [B1]
                Statements (0)
                Jump if False (Regular) to Block[B2]
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                Next (Regular) Block[B1]
            Block[B2] - Exit [UnReachable]
                Predecessors: [B1]
                Statements (0)
            """;
        var expectedDiagnostics = new[]
        {
            Diagnostic(ErrorCode.ERR_NoBreakId, "L").WithArguments("L").WithLocation(5, 35),
        };
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
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
