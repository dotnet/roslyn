// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_Empty()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        Console.WriteLine(/*<bind>*/$""""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""""')
  Parts(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_OnlyTextPart()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        Console.WriteLine(/*<bind>*/$""Only text part""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""Only text part""')
  Parts(1):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'Only text part')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Only text part"", IsImplicit) (Syntax: 'Only text part')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_OnlyInterpolationPart()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        Console.WriteLine(/*<bind>*/$""{1}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{1}""')
  Parts(1):
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{1}')
        Expression: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: 
          null
        FormatString: 
          null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_EmptyInterpolationPart()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        Console.WriteLine(/*<bind>*/$""{}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$""{}""')
  Parts(1):
      IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{}')
        Expression: 
          IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
            Children(0)
        Alignment: 
          null
        FormatString: 
          null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1733: Expected expression
                //         Console.WriteLine(/*<bind>*/$"{}"/*</bind>*/);
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(8, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_TextAndInterpolationParts()
        {
            string source = @"
using System;

internal class Class
{
    public void M(int x)
    {
        Console.WriteLine(/*<bind>*/$""String {x} and constant {1}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String {x ... nstant {1}""')
  Parts(4):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{x}')
        Expression: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
        Alignment: 
          null
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and constant ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and constant "", IsImplicit) (Syntax: ' and constant ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{1}')
        Expression: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: 
          null
        FormatString: 
          null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_FormatAndAlignment()
        {
            string source = @"
using System;

internal class Class
{
    private string x = string.Empty;
    private int y = 0;

    public void M()
    {
        Console.WriteLine(/*<bind>*/$""String {x,20} and {y:D3} and constant {1}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String {x ... nstant {1}""')
  Parts(6):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{x,20}')
        Expression: 
          IFieldReferenceOperation: System.String Class.x (OperationKind.FieldReference, Type: System.String) (Syntax: 'x')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'x')
        Alignment: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and "", IsImplicit) (Syntax: ' and ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{y:D3}')
        Expression: 
          IFieldReferenceOperation: System.Int32 Class.y (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'y')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'y')
        Alignment: 
          null
        FormatString: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""D3"") (Syntax: ':D3')
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and constant ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and constant "", IsImplicit) (Syntax: ' and constant ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{1}')
        Expression: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: 
          null
        FormatString: 
          null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_InterpolationAndFormatAndAlignment()
        {
            string source = @"
using System;

internal class Class
{
    private string x = string.Empty;
    private const int y = 0;

    public void M()
    {
        Console.WriteLine(/*<bind>*/$""String {x,y:D3}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String {x,y:D3}""')
  Parts(2):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{x,y:D3}')
        Expression: 
          IFieldReferenceOperation: System.String Class.x (OperationKind.FieldReference, Type: System.String) (Syntax: 'x')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'x')
        Alignment: 
          IFieldReferenceOperation: System.Int32 Class.y (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 0) (Syntax: 'y')
            Instance Receiver: 
              null
        FormatString: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""D3"") (Syntax: ':D3')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_InvocationInInterpolation()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        string x = string.Empty;
        int y = 0;
        Console.WriteLine(/*<bind>*/$""String {x} and {M2(y)} and constant {1}""/*</bind>*/);
    }

    private string M2(int z) => z.ToString();
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String {x ... nstant {1}""')
  Parts(6):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{x}')
        Expression: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.String) (Syntax: 'x')
        Alignment: 
          null
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and "", IsImplicit) (Syntax: ' and ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{M2(y)}')
        Expression: 
          IInvocationOperation ( System.String Class.M2(System.Int32 z)) (OperationKind.Invocation, Type: System.String) (Syntax: 'M2(y)')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'M2')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null) (Syntax: 'y')
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Alignment: 
          null
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and constant ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and constant "", IsImplicit) (Syntax: ' and constant ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{1}')
        Expression: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: 
          null
        FormatString: 
          null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_NestedInterpolation()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        string x = string.Empty;
        int y = 0;
        Console.WriteLine(/*<bind>*/$""String {M2($""{y}"")}""/*</bind>*/);
    }

    private int M2(string z) => Int32.Parse(z);
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String {M2($""{y}"")}""')
  Parts(2):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{M2($""{y}"")}')
        Expression: 
          IInvocationOperation ( System.Int32 Class.M2(System.String z)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M2($""{y}"")')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'M2')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null) (Syntax: '$""{y}""')
                  IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{y}""')
                    Parts(1):
                        IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{y}')
                          Expression: 
                            ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                          Alignment: 
                            null
                          FormatString: 
                            null
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Alignment: 
          null
        FormatString: 
          null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_InvalidExpressionInInterpolation()
        {
            string source = @"
using System;

internal class Class
{
    public void M(int x)
    {
        Console.WriteLine(/*<bind>*/$""String {x1} and constant {Class}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$""String {x ... nt {Class}""')
  Parts(4):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{x1}')
        Expression: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x1')
            Children(0)
        Alignment: 
          null
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and constant ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and constant "", IsImplicit) (Syntax: ' and constant ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{Class}')
        Expression: 
          IInvalidOperation (OperationKind.Invalid, Type: Class, IsInvalid, IsImplicit) (Syntax: 'Class')
            Children(1):
                IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Class')
        Alignment: 
          null
        FormatString: 
          null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'x1' does not exist in the current context
                //         Console.WriteLine(/*<bind>*/$"String {x1} and constant {Class}"/*</bind>*/);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(8, 47),
                // CS0119: 'Class' is a type, which is not valid in the given context
                //         Console.WriteLine(/*<bind>*/$"String {x1} and constant {Class}"/*</bind>*/);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Class").WithArguments("Class", "type").WithLocation(8, 65)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_Empty_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    /*<bind>*/{
        Console.WriteLine($"""");
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine($"""");')
          Expression: 
            IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine($"""")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$""""')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""""')
                      Parts(0)
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_OnlyTextPart_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    /*<bind>*/{
        Console.WriteLine($""Only text part"");
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... ext part"");')
          Expression: 
            IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... text part"")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$""Only text part""')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""Only text part""')
                      Parts(1):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'Only text part')
                            Text: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Only text part"", IsImplicit) (Syntax: 'Only text part')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_OnlyInterpolationPart_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, string c)
    /*<bind>*/{
        Console.WriteLine($""{(a ? b : c)}"");
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
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ...  b : c)}"");')
          Expression: 
            IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ? b : c)}"")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$""{(a ? b : c)}""')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{(a ? b : c)}""')
                      Parts(1):
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{(a ? b : c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a ? b : c')
                            Alignment: 
                              null
                            FormatString: 
                              null
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_TextAndInterpolationParts_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, string c)
    /*<bind>*/{
        Console.WriteLine($""String1 {(b = a ? b : c)} and String2 {(a ? b : c)}"");
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'String1 ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String1 "", IsImplicit) (Syntax: 'String1 ')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (2)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b = a ? b : c')
          Value: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'b = a ? b : c')
              Left: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a ? b : c')

        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: ' and String2 ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and String2 "", IsImplicit) (Syntax: ' and String2 ')

    Jump if False (Regular) to Block[B6]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ...  b : c)}"");')
          Expression: 
            IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ? b : c)}"")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$""String1 { ...  ? b : c)}""')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String1 { ...  ? b : c)}""')
                      Parts(4):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String1 ')
                            Text: 
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, Constant: ""String1 "", IsImplicit) (Syntax: 'String1 ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{(b = a ? b : c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b = a ? b : c')
                            Alignment: 
                              null
                            FormatString: 
                              null
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and String2 ')
                            Text: 
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.String, Constant: "" and String2 "", IsImplicit) (Syntax: ' and String2 ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{(a ? b : c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a ? b : c')
                            Alignment: 
                              null
                            FormatString: 
                              null
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B8]
Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_FormatAndAlignment_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, string c, int? d, int e)
    /*<bind>*/{
        Console.WriteLine($""String {(a ? b : c),20} and {(d ?? e):D3} and constant {1}"");
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
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'String ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')

    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (3)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '20')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')

        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: ' and ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and "", IsImplicit) (Syntax: ' and ')

        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'd')

    Jump if True (Regular) to Block[B6]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd')
          Operand: 
            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'd')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'd')
              Arguments(0)

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
          Value: 
            IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'e')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... tant {1}"");')
          Expression: 
            IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... stant {1}"")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$""String {( ... nstant {1}""')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String {( ... nstant {1}""')
                      Parts(6):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
                            Text: 
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{(a ? b : c),20}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a ? b : c')
                            Alignment: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 20, IsImplicit) (Syntax: '20')
                            FormatString: 
                              null
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
                            Text: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, Constant: "" and "", IsImplicit) (Syntax: ' and ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{(d ?? e):D3}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'd ?? e')
                            Alignment: 
                              null
                            FormatString: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""D3"") (Syntax: ':D3')
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and constant ')
                            Text: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and constant "", IsImplicit) (Syntax: ' and constant ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{1}')
                            Expression: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            Alignment: 
                              null
                            FormatString: 
                              null
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B8]
Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_InterpolationAndFormatAndAlignment_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, string c, int? d, int e)
    /*<bind>*/{
        Console.WriteLine($""String {(a ? b : c),20:D3} and {(d ?? e),21:D4}"");
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
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'String ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')

    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (4)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '20')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')

        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: ':D3')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""D3"") (Syntax: ':D3')

        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: ' and ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and "", IsImplicit) (Syntax: ' and ')

        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'd')

    Jump if True (Regular) to Block[B6]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd')
          Operand: 
            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'd')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'd')
              Arguments(0)

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
          Value: 
            IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'e')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... ),21:D4}"");')
          Expression: 
            IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... e),21:D4}"")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$""String {( ...  e),21:D4}""')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String {( ...  e),21:D4}""')
                      Parts(4):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
                            Text: 
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{(a ? b : c),20:D3}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a ? b : c')
                            Alignment: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 20, IsImplicit) (Syntax: '20')
                            FormatString: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, Constant: ""D3"", IsImplicit) (Syntax: ':D3')
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
                            Text: 
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.String, Constant: "" and "", IsImplicit) (Syntax: ' and ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{(d ?? e),21:D4}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'd ?? e')
                            Alignment: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 21) (Syntax: '21')
                            FormatString: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""D4"") (Syntax: ':D4')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B8]
Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_InvocationInInterpolation_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M(int y)
    /*<bind>*/{
        Console.WriteLine($""String {M2(ref y)} and {y}"");
    }/*</bind>*/

    private string M2(ref int z) => z.ToString();
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ...  and {y}"");')
          Expression: 
            IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... } and {y}"")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$""String {M ... )} and {y}""')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String {M ... )} and {y}""')
                      Parts(4):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
                            Text: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{M2(ref y)}')
                            Expression: 
                              IInvocationOperation ( System.String Class.M2(ref System.Int32 z)) (OperationKind.Invocation, Type: System.String) (Syntax: 'M2(ref y)')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'M2')
                                Arguments(1):
                                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null) (Syntax: 'ref y')
                                      IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Alignment: 
                              null
                            FormatString: 
                              null
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
                            Text: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and "", IsImplicit) (Syntax: ' and ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{y}')
                            Expression: 
                              IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                            Alignment: 
                              null
                            FormatString: 
                              null
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_NestedInterpolation_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, int b, int? c, int? d, int? e)
    /*<bind>*/{
        Console.WriteLine($""String {(e = a ? b : c)} and {M2($""{d ?? e}"")}"");
    }/*</bind>*/

    private int M2(string z) => Int32.Parse(z);
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'String ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
          Value: 
            IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'e')

    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsImplicit) (Syntax: 'b')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitNullable)
              Operand: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (4)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e = a ? b : c')
          Value: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?) (Syntax: 'e = a ? b : c')
              Left: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'e')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a ? b : c')

        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: ' and ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and "", IsImplicit) (Syntax: ' and ')

        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2')
          Value: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'M2')

        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'd')

    Jump if True (Regular) to Block[B6]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd')
          Operand: 
            IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'd')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'd')

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
          Value: 
            IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'e')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... ?? e}"")}"");')
          Expression: 
            IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ...  ?? e}"")}"")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$""String {( ... d ?? e}"")}""')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String {( ... d ?? e}"")}""')
                      Parts(4):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
                            Text: 
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{(e = a ? b : c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'e = a ? b : c')
                            Alignment: 
                              null
                            FormatString: 
                              null
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
                            Text: 
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.String, Constant: "" and "", IsImplicit) (Syntax: ' and ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{M2($""{d ?? e}"")}')
                            Expression: 
                              IInvocationOperation ( System.Int32 Class.M2(System.String z)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M2($""{d ?? e}"")')
                                Instance Receiver: 
                                  IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'M2')
                                Arguments(1):
                                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null) (Syntax: '$""{d ?? e}""')
                                      IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{d ?? e}""')
                                        Parts(1):
                                            IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{d ?? e}')
                                              Expression: 
                                                IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'd ?? e')
                                              Alignment: 
                                                null
                                              FormatString: 
                                                null
                                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Alignment: 
                              null
                            FormatString: 
                              null
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B8]
Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_InvalidExpressionInInterpolation_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M(int x)
    /*<bind>*/{
        Console.WriteLine($""String {x1} and constant {Class}"");
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.Wri ...  {Class}"");')
          Expression: 
            IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.Wri ... t {Class}"")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '$""String {x ... nt {Class}""')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$""String {x ... nt {Class}""')
                      Parts(4):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
                            Text: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{x1}')
                            Expression: 
                              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x1')
                                Children(0)
                            Alignment: 
                              null
                            FormatString: 
                              null
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and constant ')
                            Text: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and constant "", IsImplicit) (Syntax: ' and constant ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{Class}')
                            Expression: 
                              IInvalidOperation (OperationKind.Invalid, Type: Class, IsInvalid, IsImplicit) (Syntax: 'Class')
                                Children(1):
                                    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Class')
                            Alignment: 
                              null
                            FormatString: 
                              null
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,37): error CS0103: The name 'x1' does not exist in the current context
                //         Console.WriteLine($"String {x1} and constant {Class}");
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(8, 37),
                // file.cs(8,55): error CS0119: 'Class' is a type, which is not valid in the given context
                //         Console.WriteLine($"String {x1} and constant {Class}");
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Class").WithArguments("Class", "type").WithLocation(8, 55)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_NonConstantExpressionInFormatAndAlignment_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, string c, int? d, int e)
    /*<bind>*/{
        Console.WriteLine($""String {(a ? b : c),e:c} and {(d ?? e),e:c}"");
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
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'String ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')

    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (4)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'e')
          Value: 
            IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'e')

        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: ':c')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""c"", IsInvalid) (Syntax: ':c')

        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: ' and ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and "", IsImplicit) (Syntax: ' and ')

        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'd')

    Jump if True (Regular) to Block[B6]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd')
          Operand: 
            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'd')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'd')
              Arguments(0)

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
          Value: 
            IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'e')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.Wri ...  e),e:c}"");')
          Expression: 
            IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.Wri ... ? e),e:c}"")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '$""String {( ... ?? e),e:c}""')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$""String {( ... ?? e),e:c}""')
                      Parts(4):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
                            Text: 
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{(a ? b : c),e:c}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a ? b : c')
                            Alignment: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'e')
                            FormatString: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, Constant: ""c"", IsInvalid, IsImplicit) (Syntax: ':c')
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
                            Text: 
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.String, Constant: "" and "", IsImplicit) (Syntax: ' and ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{(d ?? e),e:c}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'd ?? e')
                            Alignment: 
                              IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'e')
                            FormatString: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""c"", IsInvalid) (Syntax: ':c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B8]
Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,49): error CS0150: A constant value is expected
                //         Console.WriteLine($"String {(a ? b : c),e:c} and {(d ?? e),e:c}");
                Diagnostic(ErrorCode.ERR_ConstantExpected, "e").WithLocation(8, 49),
                // file.cs(8,68): error CS0150: A constant value is expected
                //         Console.WriteLine($"String {(a ? b : c),e:c} and {(d ?? e),e:c}");
                Diagnostic(ErrorCode.ERR_ConstantExpected, "e").WithLocation(8, 68)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
