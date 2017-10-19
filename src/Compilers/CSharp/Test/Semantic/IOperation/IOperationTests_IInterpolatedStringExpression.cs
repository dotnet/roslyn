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
IInterpolatedStringExpression ([0] OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""""')
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
IInterpolatedStringExpression ([0] OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""Only text part""')
  Parts(1):
      IInterpolatedStringText ([0] OperationKind.InterpolatedStringText) (Syntax: 'Only text part')
        Text: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.String, Constant: ""Only text part"") (Syntax: 'Only text part')
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
IInterpolatedStringExpression ([0] OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""{1}""')
  Parts(1):
      IInterpolation ([0] OperationKind.Interpolation) (Syntax: '{1}')
        Expression: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IInterpolatedStringExpression ([0] OperationKind.InterpolatedStringExpression, Type: System.String, IsInvalid) (Syntax: '$""{}""')
  Parts(1):
      IInterpolation ([0] OperationKind.Interpolation, IsInvalid) (Syntax: '{}')
        Expression: 
          IInvalidExpression ([0] OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
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
IInterpolatedStringExpression ([0] OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""String {x ... nstant {1}""')
  Parts(4):
      IInterpolatedStringText ([0] OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.String, Constant: ""String "") (Syntax: 'String ')
      IInterpolation ([1] OperationKind.Interpolation) (Syntax: '{x}')
        Expression: 
          IParameterReferenceExpression: x ([0] OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Alignment: 
          null
        FormatString: 
          null
      IInterpolatedStringText ([2] OperationKind.InterpolatedStringText) (Syntax: ' and constant ')
        Text: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.String, Constant: "" and constant "") (Syntax: ' and constant ')
      IInterpolation ([3] OperationKind.Interpolation) (Syntax: '{1}')
        Expression: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IInterpolatedStringExpression ([0] OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""String {x ... nstant {1}""')
  Parts(6):
      IInterpolatedStringText ([0] OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.String, Constant: ""String "") (Syntax: 'String ')
      IInterpolation ([1] OperationKind.Interpolation) (Syntax: '{x,20}')
        Expression: 
          IFieldReferenceExpression: System.String Class.x ([0] OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 'x')
            Instance Receiver: 
              IInstanceReferenceExpression ([0] OperationKind.InstanceReferenceExpression, Type: Class, IsImplicit) (Syntax: 'x')
        Alignment: 
          ILiteralExpression ([1] OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
        FormatString: 
          null
      IInterpolatedStringText ([2] OperationKind.InterpolatedStringText) (Syntax: ' and ')
        Text: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.String, Constant: "" and "") (Syntax: ' and ')
      IInterpolation ([3] OperationKind.Interpolation) (Syntax: '{y:D3}')
        Expression: 
          IFieldReferenceExpression: System.Int32 Class.y ([0] OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'y')
            Instance Receiver: 
              IInstanceReferenceExpression ([0] OperationKind.InstanceReferenceExpression, Type: Class, IsImplicit) (Syntax: 'y')
        Alignment: 
          null
        FormatString: 
          ILiteralExpression ([1] OperationKind.LiteralExpression, Type: System.String, Constant: ""D3"") (Syntax: ':D3')
      IInterpolatedStringText ([4] OperationKind.InterpolatedStringText) (Syntax: ' and constant ')
        Text: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.String, Constant: "" and constant "") (Syntax: ' and constant ')
      IInterpolation ([5] OperationKind.Interpolation) (Syntax: '{1}')
        Expression: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IInterpolatedStringExpression ([0] OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""String {x,y:D3}""')
  Parts(2):
      IInterpolatedStringText ([0] OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.String, Constant: ""String "") (Syntax: 'String ')
      IInterpolation ([1] OperationKind.Interpolation) (Syntax: '{x,y:D3}')
        Expression: 
          IFieldReferenceExpression: System.String Class.x ([0] OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 'x')
            Instance Receiver: 
              IInstanceReferenceExpression ([0] OperationKind.InstanceReferenceExpression, Type: Class, IsImplicit) (Syntax: 'x')
        Alignment: 
          IFieldReferenceExpression: System.Int32 Class.y (Static) ([1] OperationKind.FieldReferenceExpression, Type: System.Int32, Constant: 0) (Syntax: 'y')
            Instance Receiver: 
              null
        FormatString: 
          ILiteralExpression ([2] OperationKind.LiteralExpression, Type: System.String, Constant: ""D3"") (Syntax: ':D3')
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
IInterpolatedStringExpression ([0] OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""String {x ... nstant {1}""')
  Parts(6):
      IInterpolatedStringText ([0] OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.String, Constant: ""String "") (Syntax: 'String ')
      IInterpolation ([1] OperationKind.Interpolation) (Syntax: '{x}')
        Expression: 
          ILocalReferenceExpression: x ([0] OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'x')
        Alignment: 
          null
        FormatString: 
          null
      IInterpolatedStringText ([2] OperationKind.InterpolatedStringText) (Syntax: ' and ')
        Text: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.String, Constant: "" and "") (Syntax: ' and ')
      IInterpolation ([3] OperationKind.Interpolation) (Syntax: '{M2(y)}')
        Expression: 
          IInvocationExpression ( System.String Class.M2(System.Int32 z)) ([0] OperationKind.InvocationExpression, Type: System.String) (Syntax: 'M2(y)')
            Instance Receiver: 
              IInstanceReferenceExpression ([0] OperationKind.InstanceReferenceExpression, Type: Class, IsImplicit) (Syntax: 'M2')
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: z) ([1] OperationKind.Argument) (Syntax: 'y')
                  ILocalReferenceExpression: y ([0] OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Alignment: 
          null
        FormatString: 
          null
      IInterpolatedStringText ([4] OperationKind.InterpolatedStringText) (Syntax: ' and constant ')
        Text: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.String, Constant: "" and constant "") (Syntax: ' and constant ')
      IInterpolation ([5] OperationKind.Interpolation) (Syntax: '{1}')
        Expression: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IInterpolatedStringExpression ([0] OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""String {M2($""{y}"")}""')
  Parts(2):
      IInterpolatedStringText ([0] OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.String, Constant: ""String "") (Syntax: 'String ')
      IInterpolation ([1] OperationKind.Interpolation) (Syntax: '{M2($""{y}"")}')
        Expression: 
          IInvocationExpression ( System.Int32 Class.M2(System.String z)) ([0] OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'M2($""{y}"")')
            Instance Receiver: 
              IInstanceReferenceExpression ([0] OperationKind.InstanceReferenceExpression, Type: Class, IsImplicit) (Syntax: 'M2')
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: z) ([1] OperationKind.Argument) (Syntax: '$""{y}""')
                  IInterpolatedStringExpression ([0] OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""{y}""')
                    Parts(1):
                        IInterpolation ([0] OperationKind.Interpolation) (Syntax: '{y}')
                          Expression: 
                            ILocalReferenceExpression: y ([0] OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
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
IInterpolatedStringExpression ([0] OperationKind.InterpolatedStringExpression, Type: System.String, IsInvalid) (Syntax: '$""String {x ... nt {Class}""')
  Parts(4):
      IInterpolatedStringText ([0] OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.String, Constant: ""String "") (Syntax: 'String ')
      IInterpolation ([1] OperationKind.Interpolation, IsInvalid) (Syntax: '{x1}')
        Expression: 
          IInvalidExpression ([0] OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'x1')
            Children(0)
        Alignment: 
          null
        FormatString: 
          null
      IInterpolatedStringText ([2] OperationKind.InterpolatedStringText) (Syntax: ' and constant ')
        Text: 
          ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.String, Constant: "" and constant "") (Syntax: ' and constant ')
      IInterpolation ([3] OperationKind.Interpolation, IsInvalid) (Syntax: '{Class}')
        Expression: 
          IInvalidExpression ([0] OperationKind.InvalidExpression, Type: Class, IsInvalid, IsImplicit) (Syntax: 'Class')
            Children(1):
                IOperation:  ([0] OperationKind.None, IsInvalid) (Syntax: 'Class')
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
    }
}
