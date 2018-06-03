// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIf()
        {
            string source = @"
class P
{
    private void M()
    {
        bool condition = false;
        /*<bind>*/if (true)
        {
            condition = true;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (true) ... }')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'condition = true;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'condition = true')
            Left: 
              ILocalReferenceOperation: condition (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'condition')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  WhenFalse: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'condition' is assigned but its value is never used
                //         bool condition = false;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "condition").WithArguments("condition").WithLocation(6, 14)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIfWithElse()
        {
            string source = @"
class P
{
    private void M()
    {
        bool condition = false;
        /*<bind>*/if (true)
        {
            condition = true;
        }
        else
        {
            condition = false;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (true) ... }')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'condition = true;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'condition = true')
            Left: 
              ILocalReferenceOperation: condition (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'condition')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  WhenFalse: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'condition = false;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'condition = false')
            Left: 
              ILocalReferenceOperation: condition (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'condition')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0162: Unreachable code detected
                //             condition = false;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "condition").WithLocation(13, 13),
                // CS0219: The variable 'condition' is assigned but its value is never used
                //         bool condition = false;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "condition").WithArguments("condition").WithLocation(6, 14)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIfWithConditionEvaluationTrue()
        {
            string source = @"
class P
{
    private void M()
    {
        bool condition = false;
        /*<bind>*/if (1 == 1)
        {
            condition = true;
        }/*</bind>*/

    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (1 == 1) ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, Type: System.Boolean, Constant: True) (Syntax: '1 == 1')
      Left: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'condition = true;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'condition = true')
            Left: 
              ILocalReferenceOperation: condition (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'condition')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  WhenFalse: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'condition' is assigned but its value is never used
                //         bool condition = false;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "condition").WithArguments("condition").WithLocation(6, 14)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);

        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIfNested1()
        {
            string source = @"
using System;
class P
{
    private void M()
    {
        int m = 12;
        int n = 18;
        /*<bind>*/if (m > 10)
        {
            if (n > 20)
                Console.WriteLine(m);
        }
        else
        {
            Console.WriteLine(n);
        }/*</bind>*/

    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (m > 10) ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'm > 10')
      Left: 
        ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'm')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (n > 20) ... iteLine(m);')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'n > 20')
            Left: 
              ILocalReferenceOperation: n (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'n')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(m);')
            Expression: 
              IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(m)')
                Instance Receiver: 
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'm')
                      ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'm')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        WhenFalse: 
          null
  WhenFalse: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(n);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(n)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'n')
                  ILocalReferenceOperation: n (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'n')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIfNested2()
        {
            string source = @"
using System;
class P
{
    private void M()
    {
        int m = 9;
        int n = 7;
        /*<bind>*/if (m > 10)
            if (n > 20)
            {
                Console.WriteLine(m);
            }
            else
            {
                Console.WriteLine(n);
            }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (m > 10) ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'm > 10')
      Left: 
        ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'm')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  WhenTrue: 
    IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (n > 20) ... }')
      Condition: 
        IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'n > 20')
          Left: 
            ILocalReferenceOperation: n (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'n')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
      WhenTrue: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(m);')
            Expression: 
              IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(m)')
                Instance Receiver: 
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'm')
                      ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'm')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      WhenFalse: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(n);')
            Expression: 
              IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(n)')
                Instance Receiver: 
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'n')
                      ILocalReferenceOperation: n (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'n')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithMultipleCondition()
        {
            string source = @"
using System;
class P
{
    private void M()
    {
        int m = 9;
        int n = 7;
        int p = 5;
        /*<bind>*/if (m >= n && m >= p)
        {
            Console.WriteLine(""Nothing is larger than m."");
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (m >= n  ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.ConditionalAnd) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'm >= n && m >= p')
      Left: 
        IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'm >= n')
          Left: 
            ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'm')
          Right: 
            ILocalReferenceOperation: n (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'n')
      Right: 
        IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'm >= p')
          Left: 
            ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'm')
          Right: 
            ILocalReferenceOperation: p (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'p')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ...  than m."");')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... r than m."")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '""Nothing is ... er than m.""')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Nothing is larger than m."") (Syntax: '""Nothing is ... er than m.""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithElseIfCondition()
        {
            string source = @"
using System;
class P
{
    private void M()
    {
        int m = 9;
        int n = 7;
        /*<bind>*/if (n > 20)
        {
            Console.WriteLine(""Result1"");
        }
        else if (m > 10)
        {
            Console.WriteLine(""Result2"");
        }
        else
        {
            Console.WriteLine(""Result3"");
        }/*</bind>*/

    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (n > 20) ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'n > 20')
      Left: 
        ILocalReferenceOperation: n (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'n')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... ""Result1"");')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... (""Result1"")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '""Result1""')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Result1"") (Syntax: '""Result1""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (m > 10) ... }')
      Condition: 
        IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'm > 10')
          Left: 
            ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'm')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
      WhenTrue: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... ""Result2"");')
            Expression: 
              IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... (""Result2"")')
                Instance Receiver: 
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '""Result2""')
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Result2"") (Syntax: '""Result2""')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      WhenFalse: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... ""Result3"");')
            Expression: 
              IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... (""Result3"")')
                Instance Receiver: 
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '""Result3""')
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Result3"") (Syntax: '""Result3""')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithElseIfConditionOutVar()
        {
            string source = @"
class P
{
    private void M()
    {
        var s = """";
        /*<bind>*/if (int.TryParse(s, out var i))
            System.Console.WriteLine($""i ={i}, s ={s}"");
        else
            System.Console.WriteLine($""i ={i}, s ={s}"");/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (int.Try ... , s ={s}"");')
  Condition: 
    IInvocationOperation (System.Boolean System.Int32.TryParse(System.String s, out System.Int32 result)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'int.TryPars ...  out var i)')
      Instance Receiver: 
        null
      Arguments(2):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null) (Syntax: 's')
            ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String) (Syntax: 's')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: result) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
            IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
              ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenTrue: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... , s ={s}"");')
      Expression: 
        IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... }, s ={s}"")')
          Instance Receiver: 
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$""i ={i}, s ={s}""')
                IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""i ={i}, s ={s}""')
                  Parts(4):
                      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'i =')
                        Text: 
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""i ="", IsImplicit) (Syntax: 'i =')
                      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{i}')
                        Expression: 
                          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        Alignment: 
                          null
                        FormatString: 
                          null
                      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ', s =')
                        Text: 
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", s ="", IsImplicit) (Syntax: ', s =')
                      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{s}')
                        Expression: 
                          ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String) (Syntax: 's')
                        Alignment: 
                          null
                        FormatString: 
                          null
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... , s ={s}"");')
      Expression: 
        IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... }, s ={s}"")')
          Instance Receiver: 
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$""i ={i}, s ={s}""')
                IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""i ={i}, s ={s}""')
                  Parts(4):
                      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'i =')
                        Text: 
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""i ="", IsImplicit) (Syntax: 'i =')
                      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{i}')
                        Expression: 
                          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        Alignment: 
                          null
                        FormatString: 
                          null
                      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ', s =')
                        Text: 
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", s ="", IsImplicit) (Syntax: ', s =')
                      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{s}')
                        Expression: 
                          ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String) (Syntax: 's')
                        Alignment: 
                          null
                        FormatString: 
                          null
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithOutVar()
        {
            string source = @"
class P
{
    private void M()
    {

        /*<bind>*/if (true)
            System.Console.WriteLine(A());/*</bind>*/
    }
    private int A()
    {
        var s = """";
        if (int.TryParse(s, out var i))
        {
            return i;
        }
        else
        {
            return -1;
        }
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (true) ... eLine(A());')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  WhenTrue: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... eLine(A());')
      Expression: 
        IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... teLine(A())')
          Instance Receiver: 
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'A()')
                IInvocationOperation ( System.Int32 P.A()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'A()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'A')
                  Arguments(0)
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementExplicitEmbeddedOutVar()
        {
            string source = @"
class P
{
    private void M()
    {
        var s = ""data"";
        /*<bind>*/if (true)
        {
            A(int.TryParse(s, out var i));
        }/*</bind>*/
    }
    private void A(bool flag)
    {
        if (flag)
        {
            System.Console.WriteLine(""Result1"");
        }
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (true) ... }')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  WhenTrue: 
    IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      Locals: Local_1: System.Int32 i
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'A(int.TryPa ... ut var i));')
        Expression: 
          IInvocationOperation ( void P.A(System.Boolean flag)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'A(int.TryPa ... out var i))')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'A')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: flag) (OperationKind.Argument, Type: null) (Syntax: 'int.TryPars ...  out var i)')
                  IInvocationOperation (System.Boolean System.Int32.TryParse(System.String s, out System.Int32 result)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'int.TryPars ...  out var i)')
                    Instance Receiver: 
                      null
                    Arguments(2):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null) (Syntax: 's')
                          ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String) (Syntax: 's')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: result) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                            ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementImplicitEmbeddedOutVar()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        object o = 25;
        /*<bind>*/if (true)
            A(o is int i, 1);/*</bind>*/
    }

    private static void A(bool flag, int number)
    {
        if (flag)
        {
            System.Console.WriteLine(new string('*', number));
        }
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (true) ...  int i, 1);')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  WhenTrue: 
    IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'A(o is int i, 1);')
      Locals: Local_1: System.Int32 i
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'A(o is int i, 1);')
        Expression: 
          IInvocationOperation (void Program.A(System.Boolean flag, System.Int32 number)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'A(o is int i, 1)')
            Instance Receiver: 
              null
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: flag) (OperationKind.Argument, Type: null) (Syntax: 'o is int i')
                  IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'o is int i')
                    Expression: 
                      ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
                    Pattern: 
                      IDeclarationPatternOperation (Declared Symbol: System.Int32 i) (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: number) (OperationKind.Argument, Type: null) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithConditionPattern()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        object obj = ""pattern"";

        /*<bind>*/if (obj is string str)
        {
            Console.WriteLine(str);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (obj is  ... }')
  Condition: 
    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'obj is string str')
      Expression: 
        ILocalReferenceOperation: obj (OperationKind.LocalReference, Type: System.Object) (Syntax: 'obj')
      Pattern: 
        IDeclarationPatternOperation (Declared Symbol: System.String str) (OperationKind.DeclarationPattern, Type: null) (Syntax: 'string str')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(str);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(str)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'str')
                  ILocalReferenceOperation: str (OperationKind.LocalReference, Type: System.String) (Syntax: 'str')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithPattern()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/if (true)
            A(25);/*</bind>*/
    }

    private static void A(object o)
    {
        if (o is null) return;
        if (!(o is int i)) return;
        System.Console.WriteLine(new string('*', i));
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (true) ... A(25);')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  WhenTrue: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'A(25);')
      Expression: 
        IInvocationOperation (void Program.A(System.Object o)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'A(25)')
          Instance Receiver: 
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null) (Syntax: '25')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '25')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 25) (Syntax: '25')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithEmbeddedPattern()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        object o = 25;
        /*<bind>*/if (true)
        {
            A(o is int i, 1);
        }/*</bind>*/
    }

    private static void A(bool flag, int number)
    {
        if (flag)
        {
            System.Console.WriteLine(new string('*', number));
        }
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (true) ... }')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  WhenTrue: 
    IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      Locals: Local_1: System.Int32 i
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'A(o is int i, 1);')
        Expression: 
          IInvocationOperation (void Program.A(System.Boolean flag, System.Int32 number)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'A(o is int i, 1)')
            Instance Receiver: 
              null
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: flag) (OperationKind.Argument, Type: null) (Syntax: 'o is int i')
                  IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'o is int i')
                    Expression: 
                      ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
                    Pattern: 
                      IDeclarationPatternOperation (Declared Symbol: System.Int32 i) (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: number) (OperationKind.Argument, Type: null) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithElseMissing()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        object obj = ""pattern"";

        /*<bind>*/if (obj is string str)
        {
            Console.WriteLine(str);
        }
        else
/*</bind>*/    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null, IsInvalid) (Syntax: 'if (obj is  ... else')
  Condition: 
    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'obj is string str')
      Expression: 
        ILocalReferenceOperation: obj (OperationKind.LocalReference, Type: System.Object) (Syntax: 'obj')
      Pattern: 
        IDeclarationPatternOperation (Declared Symbol: System.String str) (OperationKind.DeclarationPattern, Type: null) (Syntax: 'string str')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(str);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(str)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'str')
                  ILocalReferenceOperation: str (OperationKind.LocalReference, Type: System.String) (Syntax: 'str')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '')
      Expression: 
        IInvalidOperation (OperationKind.Invalid, Type: null) (Syntax: '')
          Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term '}'
                //         else
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(14, 13),
                // CS1002: ; expected
                //         else
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(14, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithConditionMissing()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        int a = 1;
        /*<bind>*/if ()
        {
            a = 2;
        }
        else
        {
            a = 3;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null, IsInvalid) (Syntax: 'if () ... }')
  Condition: 
    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = 2;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'a = 2')
            Left: 
              ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'a')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  WhenFalse: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = 3;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'a = 3')
            Left: 
              ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'a')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ')'
                //         /*<bind>*/if ()
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(9, 23),
                // CS0219: The variable 'a' is assigned but its value is never used
                //         int a = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "a").WithArguments("a").WithLocation(8, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithStatementMissing()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        int a = 1;
        
        /*<bind>*/if (a == 1)
        else
/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null, IsInvalid) (Syntax: 'if (a == 1) ... else')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'a == 1')
      Left: 
        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'a')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  WhenTrue: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '')
      Expression: 
        IInvalidOperation (OperationKind.Invalid, Type: null) (Syntax: '')
          Children(0)
  WhenFalse: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '')
      Expression: 
        IInvalidOperation (OperationKind.Invalid, Type: null) (Syntax: '')
          Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term 'else'
                //         /*<bind>*/if (a == 1)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("else").WithLocation(10, 30),
                // CS1002: ; expected
                //         /*<bind>*/if (a == 1)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(10, 30),
                // CS1525: Invalid expression term '}'
                //         else
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(11, 13),
                // CS1002: ; expected
                //         else
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(11, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithFuncCall()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        /*<bind>*/if (true)
            A();
        else
            B();/*</bind>*/
    }
    private void A()
    {
        Console.WriteLine(""A"");
    }
    private void B()
    {
        Console.WriteLine(""B"");
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (true) ... B();')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  WhenTrue: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'A();')
      Expression: 
        IInvocationOperation ( void P.A()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'A()')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'A')
          Arguments(0)
  WhenFalse: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'B();')
      Expression: 
        IInvocationOperation ( void P.B()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'B()')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'B')
          Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0162: Unreachable code detected
                //             B();/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreachableCode, "B").WithLocation(11, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithDynamic()
        {
            string source = @"
using System;

class C
{
    public static int F<T>(dynamic d, Type t, T x) where T : struct
    {
        /*<bind>*/if (d.GetType() == t && ((T)d).Equals(x))
        {
            return 1;
        }/*</bind>*/

        return 2;
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (d.GetTy ... }')
  Condition: 
    IUnaryOperation (UnaryOperatorKind.True) (OperationKind.UnaryOperator, Type: System.Boolean, IsImplicit) (Syntax: 'd.GetType() ... ).Equals(x)')
      Operand: 
        IBinaryOperation (BinaryOperatorKind.ConditionalAnd) (OperationKind.BinaryOperator, Type: dynamic) (Syntax: 'd.GetType() ... ).Equals(x)')
          Left: 
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, Type: dynamic) (Syntax: 'd.GetType() == t')
              Left: 
                IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic) (Syntax: 'd.GetType()')
                  Expression: 
                    IDynamicMemberReferenceOperation (Member Name: ""GetType"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.GetType')
                      Type Arguments(0)
                      Instance Receiver: 
                        IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
                  Arguments(0)
                  ArgumentNames(0)
                  ArgumentRefKinds(0)
              Right: 
                IParameterReferenceOperation: t (OperationKind.ParameterReference, Type: System.Type) (Syntax: 't')
          Right: 
            IInvocationOperation (virtual System.Boolean System.ValueType.Equals(System.Object obj)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: '((T)d).Equals(x)')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: T) (Syntax: '(T)d')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null) (Syntax: 'x')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'x')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T) (Syntax: 'x')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return 1;')
        ReturnedValue: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  WhenFalse: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_01()
        {
            string source = @"
class P
{
    void M()
/*<bind>*/{
        bool condition = false;
        if (true)
        {
            condition = true;
        }
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Boolean condition]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'condition = false')
              Left: 
                ILocalReferenceOperation: condition (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'condition = false')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Jump if False (Regular) to Block[B3]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'condition = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'condition = true')
                  Left: 
                    ILocalReferenceOperation: condition (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'condition')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'condition' is assigned but its value is never used
                //         bool condition = false;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "condition").WithArguments("condition").WithLocation(6, 14)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_02()
        {
            string source = @"
class P
{
    void M()
/*<bind>*/{
        bool condition = false;
        if (true)
        {
            ;
        }
        else
        {
            condition = true;
        }
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Boolean condition]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'condition = false')
              Left: 
                ILocalReferenceOperation: condition (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'condition = false')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Jump if False (Regular) to Block[B2]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B3]
            Leaving: {R1}
    Block[B2] - Block [UnReachable]
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'condition = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'condition = true')
                  Left: 
                    ILocalReferenceOperation: condition (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'condition')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(13,13): warning CS0162: Unreachable code detected
                //             condition = true;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "condition").WithLocation(13, 13),
                // file.cs(6,14): warning CS0219: The variable 'condition' is assigned but its value is never used
                //         bool condition = false;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "condition").WithArguments("condition").WithLocation(6, 14)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_03()
        {
            string source = @"
class P
{
    void M(bool a, bool b)
/*<bind>*/{
        if (a && b)
        {
            a = false;
        }
        else
        {
            b = true;
        }
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'a = false')
              Left: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B5]
Block[B4] - Block
    Predecessors: [B1] [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
              Left: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B3] [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_04()
        {
            string source = @"
class P
{
    void M(bool a, bool b, bool c, bool result)
/*<bind>*/{
        if (a ? b : c)
        {
            result = false;
        }
        else
        {
            result = true;
        }
    }/*</bind>*/
}
";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B5]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B5]
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B6]
Block[B5] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = true')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_05()
        {
            string source = @"
class P
{
    void M(bool? a, bool b, bool result)
/*<bind>*/{
        if (a ?? b)
        {
            result = false;
        }
        else
        {
            result = true;
        }
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B5]
        IInvocationOperation ( System.Boolean System.Boolean?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Instance Receiver: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'a')
          Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B5]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B6]
Block[B5] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = true')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_06()
        {
            string source = @"
class P
{
    void M(bool a, bool b, bool c, bool result)
/*<bind>*/{
        if (!(a ? b : c))
        {
            result = false;
        }
        else
        {
            result = true;
        }
    }/*</bind>*/
}
";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if True (Regular) to Block[B5]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if True (Regular) to Block[B5]
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B6]
Block[B5] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = true')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_07()
        {
            string source = @"
class P
{
    void M(bool a, bool b, bool c, bool d , bool e, bool result)
/*<bind>*/{
        if ((a ? b : c) ? d : e)
        {
            result = false;
        }
    }/*</bind>*/
}
";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B5]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B5]
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B7]
        IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'd')

    Next (Regular) Block[B6]
Block[B5] - Block
    Predecessors: [B2] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B7]
        IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'e')

    Next (Regular) Block[B6]
Block[B6] - Block
    Predecessors: [B4] [B5]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B7]
Block[B7] - Exit
    Predecessors: [B4] [B5] [B6]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_08()
        {
            string source = @"
class P
{
    void M(bool a, bool b, bool c, bool d , bool e, bool result)
/*<bind>*/{
        if (a ? (b ? c : d) : e)
        {
            result = false;
        }
    }/*</bind>*/
}
";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B5]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B2]
    Statements (0)
    Jump if False (Regular) to Block[B7]
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

    Next (Regular) Block[B6]
Block[B4] - Block
    Predecessors: [B2]
    Statements (0)
    Jump if False (Regular) to Block[B7]
        IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'd')

    Next (Regular) Block[B6]
Block[B5] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B7]
        IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'e')

    Next (Regular) Block[B6]
Block[B6] - Block
    Predecessors: [B3] [B4] [B5]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B7]
Block[B7] - Exit
    Predecessors: [B3] [B4] [B5] [B6]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_09()
        {
            string source = @"
class P
{
    void M(bool a, bool b, bool c, bool d , bool e, bool result)
/*<bind>*/{
        if (a ? b : (c ? d : e))
        {
            result = false;
        }
    }/*</bind>*/
}
";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B7]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B6]
Block[B3] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B5]
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B3]
    Statements (0)
    Jump if False (Regular) to Block[B7]
        IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'd')

    Next (Regular) Block[B6]
Block[B5] - Block
    Predecessors: [B3]
    Statements (0)
    Jump if False (Regular) to Block[B7]
        IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'e')

    Next (Regular) Block[B6]
Block[B6] - Block
    Predecessors: [B2] [B4] [B5]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B7]
Block[B7] - Exit
    Predecessors: [B2] [B4] [B5] [B6]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_10()
        {
            string source = @"
class P
{
    void M(object a, object b, object c, object d , object e, object f, bool result)
/*<bind>*/{
        if (GetBool(a ?? b) ? GetBool(c ?? d) : GetBool(e ?? f))
        {
            result = false;
        }
    }/*</bind>*/

    static bool GetBool(object x) => false;
}
";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B9]
        IInvocationOperation (System.Boolean P.GetBool(System.Object x)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'GetBool(a ?? b)')
          Instance Receiver: 
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'a ?? b')
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'a ?? b')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'c')

    Jump if True (Regular) to Block[B7]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c')

    Next (Regular) Block[B6]
Block[B6] - Block
    Predecessors: [B5]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c')

    Next (Regular) Block[B8]
Block[B7] - Block
    Predecessors: [B5]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')

    Next (Regular) Block[B8]
Block[B8] - Block
    Predecessors: [B6] [B7]
    Statements (0)
    Jump if False (Regular) to Block[B14]
        IInvocationOperation (System.Boolean P.GetBool(System.Object x)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'GetBool(c ?? d)')
          Instance Receiver: 
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'c ?? d')
                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c ?? d')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B13]
Block[B9] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
          Value: 
            IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'e')

    Jump if True (Regular) to Block[B11]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'e')
          Operand: 
            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'e')

    Next (Regular) Block[B10]
Block[B10] - Block
    Predecessors: [B9]
    Statements (1)
        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
          Value: 
            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'e')

    Next (Regular) Block[B12]
Block[B11] - Block
    Predecessors: [B9]
    Statements (1)
        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'f')
          Value: 
            IParameterReferenceOperation: f (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'f')

    Next (Regular) Block[B12]
Block[B12] - Block
    Predecessors: [B10] [B11]
    Statements (0)
    Jump if False (Regular) to Block[B14]
        IInvocationOperation (System.Boolean P.GetBool(System.Object x)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'GetBool(e ?? f)')
          Instance Receiver: 
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'e ?? f')
                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'e ?? f')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B13]
Block[B13] - Block
    Predecessors: [B8] [B12]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B14]
Block[B14] - Exit
    Predecessors: [B8] [B12] [B13]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_11()
        {
            string source = @"
class P
{
    void M(bool a, P b, P c, bool result)
/*<bind>*/{
        if (a ? b : c)
        {
            result = false;
        }
    }/*</bind>*/
}
";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: P, IsInvalid) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: P, IsInvalid) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B6]
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'a ? b : c')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            (NoConversion)
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'a ? b : c')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(6,13): error CS0029: Cannot implicitly convert type 'P' to 'bool'
                //         if (a ? b : c)
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "a ? b : c").WithArguments("P", "bool").WithLocation(6, 13)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_12()
        {
            string source = @"
class P
{
    void M(bool? a, bool b, bool result)
/*<bind>*/{
        if (!(a ?? b))
        {
            result = false;
        }
        else
        {
            result = true;
        }
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if True (Regular) to Block[B5]
        IInvocationOperation ( System.Boolean System.Boolean?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Instance Receiver: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'a')
          Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if True (Regular) to Block[B5]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B6]
Block[B5] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = true')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_13()
        {
            string source = @"
class P
{
    void M(bool? a, bool? b, bool c, bool result)
/*<bind>*/{
        if (a ?? (b ?? c))
        {
            result = false;
        }
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B7]
        IInvocationOperation ( System.Boolean System.Boolean?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Instance Receiver: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'a')
          Arguments(0)

    Next (Regular) Block[B6]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean?) (Syntax: 'b')

    Jump if True (Regular) to Block[B5]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B3]
    Statements (0)
    Jump if False (Regular) to Block[B7]
        IInvocationOperation ( System.Boolean System.Boolean?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'b')
          Instance Receiver: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'b')
          Arguments(0)

    Next (Regular) Block[B6]
Block[B5] - Block
    Predecessors: [B3]
    Statements (0)
    Jump if False (Regular) to Block[B7]
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

    Next (Regular) Block[B6]
Block[B6] - Block
    Predecessors: [B2] [B4] [B5]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B7]
Block[B7] - Exit
    Predecessors: [B2] [B4] [B5] [B6]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_14()
        {
            string source = @"
class P
{
    void M(object a, object b, object c, object d, bool result)
/*<bind>*/{
        if (GetNullableBool(a ?? b) ?? GetBool(c ?? d))
        {
            result = false;
        }
    }/*</bind>*/

    static bool GetBool(object x) => false;

    static bool? GetNullableBool(object x) => false;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetNullableBool(a ?? b)')
          Value: 
            IInvocationOperation (System.Boolean? P.GetNullableBool(System.Object x)) (OperationKind.Invocation, Type: System.Boolean?) (Syntax: 'GetNullableBool(a ?? b)')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'a ?? b')
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'a ?? b')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Jump if True (Regular) to Block[B6]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetNullableBool(a ?? b)')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'GetNullableBool(a ?? b)')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (0)
    Jump if False (Regular) to Block[B11]
        IInvocationOperation ( System.Boolean System.Boolean?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'GetNullableBool(a ?? b)')
          Instance Receiver: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'GetNullableBool(a ?? b)')
          Arguments(0)

    Next (Regular) Block[B10]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'c')

    Jump if True (Regular) to Block[B8]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c')
          Operand: 
            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B6]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c')

    Next (Regular) Block[B9]
Block[B8] - Block
    Predecessors: [B6]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')

    Next (Regular) Block[B9]
Block[B9] - Block
    Predecessors: [B7] [B8]
    Statements (0)
    Jump if False (Regular) to Block[B11]
        IInvocationOperation (System.Boolean P.GetBool(System.Object x)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'GetBool(c ?? d)')
          Instance Receiver: 
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'c ?? d')
                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c ?? d')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B10]
Block[B10] - Block
    Predecessors: [B5] [B9]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B11]
Block[B11] - Exit
    Predecessors: [B5] [B9] [B10]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_15()
        {
            string source = @"
class P
{
    void M(P a, P b, bool result)
/*<bind>*/{
        if (a ?? b)
        {
            result = false;
        }
    }/*</bind>*/
}
";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: P, IsInvalid) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'a')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: P, IsInvalid) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B6]
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'a ?? b')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            (NoConversion)
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(6,13): error CS0029: Cannot implicitly convert type 'P' to 'bool'
                //         if (a ?? b)
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "a ?? b").WithArguments("P", "bool").WithLocation(6, 13)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_16()
        {
            string source = @"
class P
{
    void M(bool? a, bool b, bool result)
/*<bind>*/{
        if (a ?? throw null)
        {
            result = false;
        }
        else
        {
            result = true;
        }
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B5]
        IInvocationOperation ( System.Boolean System.Boolean?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Instance Receiver: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'a')
          Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
Block[B4] - Block
    Predecessors: [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B6]
Block[B5] - Block
    Predecessors: [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = true')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_17()
        {
            string source = @"
class P
{
    void M(bool a, bool b, bool result)
/*<bind>*/{
        if (a ? throw null : b)
        {
            result = false;
        }
        else
        {
            result = true;
        }
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
Block[B3] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B5]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B6]
Block[B5] - Block
    Predecessors: [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = true')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_18()
        {
            string source = @"
class P
{
    void M(bool a, bool b, bool result)
/*<bind>*/{
        if (a ? b : throw null)
        {
            result = false;
        }
        else
        {
            result = true;
        }
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B5]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
Block[B4] - Block
    Predecessors: [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B6]
Block[B5] - Block
    Predecessors: [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = true')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_19()
        {
            string source = @"
class P
{
    void M(bool result, System.Exception a, System.Exception b)
/*<bind>*/{
        if (throw a ?? b)
        {
            result = false;
        }
        else
        {
            result = true;
        }
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Exception, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Exception, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (0)
    Next (Throw) Block[null]
        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Exception, IsImplicit) (Syntax: 'a ?? b')
Block[B5] - Block [UnReachable]
    Predecessors (0)
    Statements (0)
    Jump if False (Regular) to Block[B7]
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'throw a ?? b')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            (NoConversion)
          Operand: 
            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'throw a ?? b')
              Children(1):
                  IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw a ?? b')

    Next (Regular) Block[B6]
Block[B6] - Block [UnReachable]
    Predecessors: [B5]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B8]
Block[B7] - Block [UnReachable]
    Predecessors: [B5]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = true')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B8]
Block[B8] - Exit [UnReachable]
    Predecessors: [B6] [B7]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(6,13): error CS8115: A throw expression is not allowed in this context.
                //         if (throw null)
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(6, 13)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_20()
        {
            string source = @"
class P
{
    void M(bool? a, bool b, bool result)
/*<bind>*/{
        if ((a ?? throw null) && b)
        {
            result = false;
        }
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B6]
        IInvocationOperation ( System.Boolean System.Boolean?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Instance Receiver: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'a')
          Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
Block[B4] - Block
    Predecessors: [B2]
    Statements (0)
    Jump if False (Regular) to Block[B6]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B2] [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_21()
        {
            string source = @"
class P
{
    void M(bool a, bool b, bool c, bool d, bool result)
/*<bind>*/{
        if ((a ? b : c) && d)
        {
            result = false;
        }
    }/*</bind>*/
}
";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B6]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B6]
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B6]
        IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'd')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B2] [B3] [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_22()
        {
            string source = @"
class P
{
    void M(bool? a, bool b, bool c, bool result)
/*<bind>*/{
        if ((a ?? b) || c)
        {
            result = false;
        }
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if True (Regular) to Block[B5]
        IInvocationOperation ( System.Boolean System.Boolean?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Instance Receiver: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'a')
          Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if True (Regular) to Block[B5]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B6]
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B2] [B3] [B4]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IfFlow_24()
        {
            string source = @"
class P
{
    void M(dynamic a, bool result)
/*<bind>*/{
        if (a)
        {
            result = false;
        }
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IUnaryOperation (UnaryOperatorKind.True) (OperationKind.UnaryOperator, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B3]
Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
