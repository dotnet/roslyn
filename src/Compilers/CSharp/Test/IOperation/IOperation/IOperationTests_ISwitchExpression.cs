// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_ISwitchExpression : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_NonExhaustive()
        {
            //null case is not handled -> not exhaustive
            string source = @"
namespace Tests
{
    public class Prog
    {
        static int EvalPoint((int, int)? point) => /*<bind>*/point switch
        {
            (0, int t) => t > 2 ? 3 : 4,
            var (_, _) => 2
        }/*</bind>*/;

        static void Main()
        {
			EvalPoint(null);
        }
    }
}
";
            string expectedOperationTree = @"ISwitchExpressionOperation (2 arms, IsExhaustive: False) (OperationKind.SwitchExpression, Type: System.Int32) (Syntax: 'point switc ... }')
  Value: 
    IParameterReferenceOperation: point (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)?) (Syntax: 'point')
  Arms(2):
      ISwitchExpressionArmOperation (1 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '(0, int t)  ... > 2 ? 3 : 4')
        Pattern: 
          IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null) (Syntax: '(0, int t)') (InputType: (System.Int32, System.Int32)?, NarrowedType: (System.Int32, System.Int32), DeclaredSymbol: null, MatchedType: (System.Int32, System.Int32), DeconstructSymbol: null)
            DeconstructionSubpatterns (2):
                IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '0') (InputType: System.Int32, NarrowedType: System.Int32)
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int t') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 t, MatchesNull: False)
            PropertySubpatterns (0)
        Value: 
          IConditionalOperation (OperationKind.Conditional, Type: System.Int32) (Syntax: 't > 2 ? 3 : 4')
            Condition: 
              IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 't > 2')
                Left: 
                  ILocalReferenceOperation: t (OperationKind.LocalReference, Type: System.Int32) (Syntax: 't')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            WhenTrue: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            WhenFalse: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
        Locals: Local_1: System.Int32 t
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: 'var (_, _) => 2')
        Pattern: 
          IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null) (Syntax: '(_, _)') (InputType: (System.Int32, System.Int32)?, NarrowedType: (System.Int32, System.Int32), DeclaredSymbol: null, MatchedType: (System.Int32, System.Int32), DeconstructSymbol: null)
            DeconstructionSubpatterns (2):
                IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32, NarrowedType: System.Int32)
                IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32, NarrowedType: System.Int32)
            PropertySubpatterns (0)
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_Basic()
        {
            string source = @"
using System;
class X
{
    void M(int? x, object y)
    {
        y = /*<bind>*/x switch { 1 => 2, 3 => 4, _ => 5 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISwitchExpressionOperation (3 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Int32) (Syntax: 'x switch {  ... 4, _ => 5 }')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Arms(3):
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '1 => 2')
        Pattern: 
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32?, NarrowedType: System.Int32)
            Value: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '3 => 4')
        Pattern: 
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '3') (InputType: System.Int32?, NarrowedType: System.Int32)
            Value: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '_ => 5')
        Pattern: 
          IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32?, NarrowedType: System.Int32?)
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_NoArms()
        {
            string source = @"
using System;
class X
{
    void M(int? x, object y)
    {
        y = /*<bind>*/x switch { }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISwitchExpressionOperation (0 arms, IsExhaustive: False) (OperationKind.SwitchExpression, Type: System.Object) (Syntax: 'x switch { }')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Arms(0)
";
            var expectedDiagnostics = new[] {
                // file.cs(7,25): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //         y = /*<bind>*/x switch { }/*</bind>*/;
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(7, 25)
            };
            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_MissingPattern()
        {
            string source = @"
using System;
class X
{
    void M(int? x, object y)
    {
        y = /*<bind>*/x switch { => 5 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISwitchExpressionOperation (1 arms, IsExhaustive: False) (OperationKind.SwitchExpression, Type: System.Int32, IsInvalid) (Syntax: 'x switch { => 5 }')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Arms(1):
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '=> 5')
        Pattern: 
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid) (Syntax: '') (InputType: System.Int32?, NarrowedType: System.Int32)
            Value: 
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            var expectedDiagnostics = new[] {
                // file.cs(7,34): error CS8504: Pattern missing
                //         y = /*<bind>*/x switch { => 5 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MissingPattern, "=>").WithLocation(7, 34)
            };
            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_BadInput_01()
        {
            string source = @"
using System;
class X
{
    void M(object y)
    {
        y = /*<bind>*/x switch { 1 => 2, 3 => 4, _ => 5 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISwitchExpressionOperation (3 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Int32, IsInvalid) (Syntax: 'x switch {  ... 4, _ => 5 }')
  Value: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x')
      Children(0)
  Arms(3):
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '1 => 2')
        Pattern: 
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: ?, NarrowedType: System.Int32)
            Value: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '3 => 4')
        Pattern: 
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '3') (InputType: ?, NarrowedType: System.Int32)
            Value: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '_ => 5')
        Pattern: 
          IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: ?, NarrowedType: ?)
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            var expectedDiagnostics = new[] {
                // file.cs(7,23): error CS0103: The name 'x' does not exist in the current context
                //         y = /*<bind>*/x switch { 1 => 2, 3 => 4, _ => 5 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(7, 23)
            };
            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_NoCommonType_01()
        {
            string source = @"
using System;
class X
{
    void M(int? x, object y)
    {
        y = /*<bind>*/x switch { 1 => 2, _ => ""Z"" }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISwitchExpressionOperation (2 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Object) (Syntax: 'x switch {  ...  _ => ""Z"" }')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Arms(2):
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '1 => 2')
        Pattern: 
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32?, NarrowedType: System.Int32)
            Value: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Value: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '2')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '_ => ""Z""')
        Pattern: 
          IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32?, NarrowedType: System.Int32?)
        Value: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '""Z""')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Z"") (Syntax: '""Z""')
";
            var expectedDiagnostics = new DiagnosticDescription[] { };
            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_NoCommonType_02()
        {
            string source = @"
using System;
class X
{
    void M(int? x, object y)
    {
        var z = /*<bind>*/x switch { 1 => 2, _ => ""Z"" }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
    ISwitchExpressionOperation (2 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: ?, IsInvalid) (Syntax: 'x switch {  ...  _ => ""Z"" }')
      Value: 
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
      Arms(2):
          ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '1 => 2')
            Pattern: 
              IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32?, NarrowedType: System.Int32)
                Value: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Value: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, IsImplicit) (Syntax: '2')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
          ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '_ => ""Z""')
            Pattern: 
              IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32?, NarrowedType: System.Int32?)
            Value: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, IsImplicit) (Syntax: '""Z""')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Z"") (Syntax: '""Z""')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,29): error CS8506: No best type was found for the switch expression.
                //         var z = /*<bind>*/x switch { 1 => 2, _ => "Z" }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(7, 29)
            };
            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_MissingArrow()
        {
            string source = @"
using System;
class X
{
    void M(int? x, object y)
    {
        y = /*<bind>*/x switch { _ /*=>*/ 5 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISwitchExpressionOperation (1 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Int32, IsInvalid) (Syntax: 'x switch { _ /*=>*/ 5 }')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Arms(1):
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '_ /*=>*/ 5')
        Pattern: 
          IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32?, NarrowedType: System.Int32?)
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5, IsInvalid) (Syntax: '5')
";
            var expectedDiagnostics = new[] {
                // file.cs(7,43): error CS1003: Syntax error, '=>' expected
                //         y = /*<bind>*/x switch { _ /*=>*/ 5 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SyntaxError, "5").WithArguments("=>").WithLocation(7, 43)
            };
            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_MissingExpression()
        {
            string source = @"
using System;
class X
{
    void M(int? x, object y)
    {
        y = /*<bind>*/x switch { _ => /*5*/ }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISwitchExpressionOperation (1 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: ?, IsInvalid) (Syntax: 'x switch { _ => /*5*/ }')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Arms(1):
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '_ => /*5*/ ')
        Pattern: 
          IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32?, NarrowedType: System.Int32?)
        Value: 
          IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
            Children(0)
";
            var expectedDiagnostics = new[] {
                // file.cs(7,45): error CS1525: Invalid expression term '}'
                //         y = /*<bind>*/x switch { _ => /*5*/ }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "}").WithArguments("}").WithLocation(7, 45)
            };
            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_BadPattern()
        {
            string source = @"
using System;
class X
{
    void M(int? x, object y)
    {
        y = /*<bind>*/x switch { NotFound => 5 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISwitchExpressionOperation (1 arms, IsExhaustive: False) (OperationKind.SwitchExpression, Type: System.Int32, IsInvalid) (Syntax: 'x switch {  ... ound => 5 }')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Arms(1):
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: 'NotFound => 5')
        Pattern: 
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid) (Syntax: 'NotFound') (InputType: System.Int32?, NarrowedType: System.Int32)
            Value: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'NotFound')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'NotFound')
                    Children(0)
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            var expectedDiagnostics = new[] {
                // file.cs(7,34): error CS0103: The name 'NotFound' does not exist in the current context
                //         y = /*<bind>*/x switch { NotFound => 5 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "NotFound").WithArguments("NotFound").WithLocation(7, 34)
            };
            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_BadArmExpression()
        {
            string source = @"
using System;
class X
{
    void M(int? x, object y)
    {
        y = /*<bind>*/x switch { _ => NotFound }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISwitchExpressionOperation (1 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: ?, IsInvalid) (Syntax: 'x switch {  ...  NotFound }')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Arms(1):
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '_ => NotFound')
        Pattern: 
          IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32?, NarrowedType: System.Int32?)
        Value: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'NotFound')
            Children(0)
";
            var expectedDiagnostics = new[] {
                // file.cs(7,39): error CS0103: The name 'NotFound' does not exist in the current context
                //         y = /*<bind>*/x switch { _ => NotFound }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "NotFound").WithArguments("NotFound").WithLocation(7, 39)
            };
            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_SubsumedArm()
        {
            string source = @"
using System;
class X
{
    void M(int? x, object y)
    {
        y = /*<bind>*/x switch { _ => 5, 1 => 2 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISwitchExpressionOperation (2 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Int32, IsInvalid) (Syntax: 'x switch {  ... 5, 1 => 2 }')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Arms(2):
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '_ => 5')
        Pattern: 
          IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32?, NarrowedType: System.Int32?)
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '1 => 2')
        Pattern: 
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid) (Syntax: '1') (InputType: System.Int32?, NarrowedType: System.Int32)
            Value: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new[] {
                // file.cs(7,42): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //         y = /*<bind>*/x switch { _ => 5, 1 => 2 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "1").WithLocation(7, 42)
            };
            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_BasicGuard()
        {
            string source = @"
using System;
class X
{
    void M(int? x, bool b, object y)
    {
        y = /*<bind>*/x switch { 1 when b => 2, _ => 5 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISwitchExpressionOperation (2 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Int32) (Syntax: 'x switch {  ... 2, _ => 5 }')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Arms(2):
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '1 when b => 2')
        Pattern: 
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32?, NarrowedType: System.Int32)
            Value: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Guard: 
          IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '_ => 5')
        Pattern: 
          IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32?, NarrowedType: System.Int32?)
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_FalseGuard()
        {
            string source = @"
using System;
class X
{
    void M(int? x, object y)
    {
        y = /*<bind>*/x switch { 1 => 2, _ when false => 5 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISwitchExpressionOperation (2 arms, IsExhaustive: False) (OperationKind.SwitchExpression, Type: System.Int32) (Syntax: 'x switch {  ... alse => 5 }')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Arms(2):
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '1 => 2')
        Pattern: 
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32?, NarrowedType: System.Int32)
            Value: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '_ when false => 5')
        Pattern: 
          IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32?, NarrowedType: System.Int32?)
        Guard: 
          ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            var expectedDiagnostics = new[] {
                // file.cs(7,25): warning CS8846: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '0' is not covered. However, a pattern with a 'when' clause might successfully match this value.
                //         y = /*<bind>*/x switch { 1 => 2, _ when false => 5 }/*</bind>*/;
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveWithWhen, "switch").WithArguments("0").WithLocation(7, 25)
            };
            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_TrueGuard()
        {
            string source = @"
using System;
class X
{
    void M(int? x, object y)
    {
        y = /*<bind>*/x switch { 1 => 2, _ when true => 5 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISwitchExpressionOperation (2 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Int32) (Syntax: 'x switch {  ... true => 5 }')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Arms(2):
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '1 => 2')
        Pattern: 
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32?, NarrowedType: System.Int32)
            Value: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '_ when true => 5')
        Pattern: 
          IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32?, NarrowedType: System.Int32?)
        Guard: 
          ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_BadGuard()
        {
            string source = @"
using System;
class X
{
    void M(int? x, object y)
    {
        y = /*<bind>*/x switch { _ when NotFound => 5 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISwitchExpressionOperation (1 arms, IsExhaustive: False) (OperationKind.SwitchExpression, Type: System.Int32, IsInvalid) (Syntax: 'x switch {  ... ound => 5 }')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Arms(1):
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '_ when NotFound => 5')
        Pattern: 
          IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32?, NarrowedType: System.Int32?)
        Guard: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'NotFound')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'NotFound')
                Children(0)
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            var expectedDiagnostics = new[] {
                // file.cs(7,41): error CS0103: The name 'NotFound' does not exist in the current context
                //         y = /*<bind>*/x switch { _ when NotFound => 5 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "NotFound").WithArguments("NotFound").WithLocation(7, 41)
            };
            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_LocalsClash()
        {
            string source = @"
using System;
class X
{
    void M(int? x, object y)
    {
        y = /*<bind>*/x switch { int z when x is int z => 5 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISwitchExpressionOperation (1 arms, IsExhaustive: False) (OperationKind.SwitchExpression, Type: System.Int32, IsInvalid) (Syntax: 'x switch {  ... nt z => 5 }')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Arms(1):
      ISwitchExpressionArmOperation (2 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: 'int z when  ...  int z => 5')
        Pattern: 
          IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int z') (InputType: System.Int32?, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 z, MatchesNull: False)
        Guard: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x is int z')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'x is int z')
                Value: 
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
                Pattern: 
                  IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'int z') (InputType: System.Int32?, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 z, MatchesNull: False)
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
        Locals: Local_1: System.Int32 z
          Local_2: System.Int32 z
";
            var expectedDiagnostics = new[] {
                // file.cs(7,54): error CS0128: A local variable or function named 'z' is already defined in this scope
                //         y = /*<bind>*/x switch { int z when x is int z => 5 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "z").WithArguments("z").WithLocation(7, 54)
            };
            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TargetTypedSwitchExpression_ImplicitCast()
        {
            var source = @"
using System;
bool b = true;
/*<bind>*/Action<string> a = b switch { true => arg => {}, false => arg => {} };/*</bind>*/";

            var expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Action<stri ... rg => {} };')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Action<stri ... arg => {} }')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action<System.String> a) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a = b switc ... arg => {} }')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= b switch  ... arg => {} }')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Action<System.String>, IsImplicit) (Syntax: 'b switch {  ... arg => {} }')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ISwitchExpressionOperation (2 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Action<System.String>) (Syntax: 'b switch {  ... arg => {} }')
                    Value: 
                      ILocalReferenceOperation: b (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'b')
                    Arms(2):
                        ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: 'true => arg => {}')
                          Pattern: 
                            IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'true') (InputType: System.Boolean, NarrowedType: System.Boolean)
                              Value: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                          Value: 
                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action<System.String>, IsImplicit) (Syntax: 'arg => {}')
                              Target: 
                                IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'arg => {}')
                                  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{}')
                                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{}')
                                      ReturnedValue: 
                                        null
                        ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: 'false => arg => {}')
                          Pattern: 
                            IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'false') (InputType: System.Boolean, NarrowedType: System.Boolean)
                              Value: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
                          Value: 
                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action<System.String>, IsImplicit) (Syntax: 'arg => {}')
                              Target: 
                                IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'arg => {}')
                                  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{}')
                                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{}')
                                      ReturnedValue: 
                                        null
    Initializer: 
      null
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TargetTypedSwitchExpression_ExplicitCast()
        {
            var source = @"
using System;
bool b = true;
/*<bind>*/var a = (Action<string>)(b switch { true => arg => {}, false => arg => {} });/*</bind>*/";

            var expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var a = (Ac ... g => {} });')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var a = (Ac ... rg => {} })')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action<System.String> a) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a = (Action ... rg => {} })')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (Action<s ... rg => {} })')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Action<System.String>) (Syntax: '(Action<str ... rg => {} })')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ISwitchExpressionOperation (2 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Action<System.String>) (Syntax: 'b switch {  ... arg => {} }')
                    Value: 
                      ILocalReferenceOperation: b (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'b')
                    Arms(2):
                        ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: 'true => arg => {}')
                          Pattern: 
                            IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'true') (InputType: System.Boolean, NarrowedType: System.Boolean)
                              Value: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                          Value: 
                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action<System.String>, IsImplicit) (Syntax: 'arg => {}')
                              Target: 
                                IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'arg => {}')
                                  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{}')
                                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{}')
                                      ReturnedValue: 
                                        null
                        ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: 'false => arg => {}')
                          Pattern: 
                            IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'false') (InputType: System.Boolean, NarrowedType: System.Boolean)
                              Value: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
                          Value: 
                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action<System.String>, IsImplicit) (Syntax: 'arg => {}')
                              Target: 
                                IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'arg => {}')
                                  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{}')
                                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{}')
                                      ReturnedValue: 
                                        null
    Initializer: 
      null
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchExpression_BasicFlow()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int input)
    /*<bind>*/{
        result = input switch
            {
                1 => false,
                _ => true
            };
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

            Jump if False (Regular) to Block[B4]
                IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: '1 => false')
                  Value: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
                  Pattern: 
                    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32, NarrowedType: System.Int32)
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'false')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

            Next (Regular) Block[B7]
                Leaving: {R2}
        Block[B4] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: '_ => true')
                  Value: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
                  Pattern: 
                    IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32, NarrowedType: System.Int32)
                Leaving: {R2}

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'true')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B7]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B4]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.InvalidOperationException..ctor()) (OperationKind.ObjectCreation, Type: System.InvalidOperationException, IsImplicit) (Syntax: 'input switc ... }')
              Arguments(0)
              Initializer: 
                null
    Block[B7] - Block
        Predecessors: [B3] [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = in ... }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'input switc ... }')

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(32216, "https://github.com/dotnet/roslyn/issues/32216")]
        public void SwitchExpression_CompoundGuard()
        {
            string source = @"
#pragma warning disable CS8509
public sealed class MyClass
{
    void M(bool result, int input, bool a, bool b)
    /*<bind>*/{
        result = input switch
            {
                1 when a && b => false
            };
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

            Jump if False (Regular) to Block[B6]
                IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: '1 when a && b => false')
                  Value: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
                  Pattern: 
                    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32, NarrowedType: System.Int32)
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')
                Leaving: {R2}

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'false')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

            Next (Regular) Block[B7]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B2] [B3] [B4]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.InvalidOperationException..ctor()) (OperationKind.ObjectCreation, Type: System.InvalidOperationException, IsImplicit) (Syntax: 'input switc ... }')
              Arguments(0)
              Initializer: 
                null
    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = in ... }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'input switc ... }')

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(32216, "https://github.com/dotnet/roslyn/issues/32216")]
        public void SwitchExpression_CompoundInput()
        {
            string source = @"
#pragma warning disable CS8509
public sealed class MyClass
{
    void M(bool result, bool a, int input1, int input2)
    /*<bind>*/{
        result = (a ? input1 : input2) switch
            {
                1 => false
            };
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                  Value: 
                    IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input1')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
                  Value: 
                    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: '1 => false')
                  Value: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ? input1 : input2')
                  Pattern: 
                    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32, NarrowedType: System.Int32)
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                Leaving: {R2}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'false')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

            Next (Regular) Block[B8]
                Leaving: {R2}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.InvalidOperationException..ctor()) (OperationKind.ObjectCreation, Type: System.InvalidOperationException, IsImplicit) (Syntax: '(a ? input1 ... }')
              Arguments(0)
              Initializer: 
                null
    Block[B8] - Block
        Predecessors: [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (a ... };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = (a ... }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: '(a ? input1 ... }')

        Next (Regular) Block[B9]
            Leaving: {R1}
}

Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(32216, "https://github.com/dotnet/roslyn/issues/32216")]
        public void SwitchExpression_BadInput_02()
        {
            string source = @"
#pragma warning disable CS8509
public sealed class MyClass
{
    void M(bool result)
    /*<bind>*/{
        result = NotFound switch
            {
                1 => false
            };
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // file.cs(7,18): error CS0103: The name 'NotFound' does not exist in the current context
                //         result = NotFound switch
                Diagnostic(ErrorCode.ERR_NameNotInContext, "NotFound").WithArguments("NotFound").WithLocation(7, 18)
                };
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'NotFound')
                  Value: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'NotFound')
                      Children(0)

            Jump if False (Regular) to Block[B4]
                IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: '1 => false')
                  Value: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'NotFound')
                  Pattern: 
                    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: ?, NarrowedType: System.Int32)
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'false')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.InvalidOperationException..ctor()) (OperationKind.ObjectCreation, Type: System.InvalidOperationException, IsInvalid, IsImplicit) (Syntax: 'NotFound sw ... }')
              Arguments(0)
              Initializer: 
                null
    Block[B5] - Block
        Predecessors: [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'result = No ... };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsInvalid) (Syntax: 'result = No ... }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'NotFound sw ... }')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(32216, "https://github.com/dotnet/roslyn/issues/32216")]
        public void SwitchExpression_CompoundConsequence()
        {
            string source = @"
#pragma warning disable CS8509
public sealed class MyClass
{
    void M(bool result, bool a, int input, bool input1, bool input2)
    /*<bind>*/{
        result = input switch
            {
                1 => (a ? input1 : input2)
            };
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

            Jump if False (Regular) to Block[B6]
                IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: '1 => (a ? i ... 1 : input2)')
                  Value: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
                  Pattern: 
                    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32, NarrowedType: System.Int32)
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if False (Regular) to Block[B5]
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                  Value: 
                    IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input1')

            Next (Regular) Block[B7]
                Leaving: {R2}
        Block[B5] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
                  Value: 
                    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input2')

            Next (Regular) Block[B7]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B2]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.InvalidOperationException..ctor()) (OperationKind.ObjectCreation, Type: System.InvalidOperationException, IsImplicit) (Syntax: 'input switc ... }')
              Arguments(0)
              Initializer: 
                null
    Block[B7] - Block
        Predecessors: [B4] [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = in ... }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'input switc ... }')

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(32216, "https://github.com/dotnet/roslyn/issues/32216")]
        public void SwitchExpression_CompoundPattern()
        {
            string source = @"
#pragma warning disable CS8509
public sealed class MyClass
{
    void M(bool result, bool a, int input, int input1, int input2)
    /*<bind>*/{
        result = input switch
            {
                (a ? input1 : input2) => true
            };
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // (9,18): error CS9133: A constant value of type 'int' is expected
                //                 (a ? input1 : input2) => true
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "a ? input1 : input2").WithArguments("int").WithLocation(9, 18)
            };
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

            Next (Regular) Block[B3]
                Entering: {R3}

        .locals {R3}
        {
            CaptureIds: [3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (0)
                Jump if False (Regular) to Block[B5]
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'a')

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input1')
                      Value: 
                        IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'input1')

                Next (Regular) Block[B6]
            Block[B5] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input2')
                      Value: 
                        IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'input2')

                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B4] [B5]
                Statements (0)
                Jump if False (Regular) to Block[B8]
                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: '(a ? input1 ... t2) => true')
                      Value: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
                      Pattern: 
                        IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid) (Syntax: '(a ? input1 : input2)') (InputType: System.Int32, NarrowedType: System.Int32)
                          Value: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'a ? input1 : input2')
                    Leaving: {R3} {R2}

                Next (Regular) Block[B7]
                    Leaving: {R3}
        }

        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'true')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B9]
                Leaving: {R2}
    }

    Block[B8] - Block
        Predecessors: [B6]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.InvalidOperationException..ctor()) (OperationKind.ObjectCreation, Type: System.InvalidOperationException, IsInvalid, IsImplicit) (Syntax: 'input switc ... }')
              Arguments(0)
              Initializer: 
                null
    Block[B9] - Block
        Predecessors: [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'result = in ... };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsInvalid) (Syntax: 'result = in ... }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'input switc ... }')

        Next (Regular) Block[B10]
            Leaving: {R1}
}

Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchExpression_Combinators()
        {
            string source = @"
#pragma warning disable CS8509
public sealed class MyClass
{
    bool M(char input)
    /*<bind>*/{
        return input switch
        {
            >= 'A' and <= 'Z' or >= 'a' and <= 'z' => true,
            '_' => false,
            not (>= '0' and <= '9') => true,
            _ => false,
        };
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                };
            string expectedFlowGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1} {R2}
    .locals {R1}
    {
        CaptureIds: [0]
        .locals {R2}
        {
            CaptureIds: [1]
            Block[B1] - Block
                Predecessors: [B0]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                      Value: 
                        IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Char) (Syntax: 'input')
                Jump if False (Regular) to Block[B3]
                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: '>= 'A' and  ... 'z' => true')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Char, IsImplicit) (Syntax: 'input')
                      Pattern: 
                        IBinaryPatternOperation (BinaryOperatorKind.Or) (OperationKind.BinaryPattern, Type: null) (Syntax: '>= 'A' and  ...  and <= 'z'') (InputType: System.Char, NarrowedType: System.Char)
                          LeftPattern: 
                            IBinaryPatternOperation (BinaryOperatorKind.And) (OperationKind.BinaryPattern, Type: null) (Syntax: '>= 'A' and <= 'Z'') (InputType: System.Char, NarrowedType: System.Char)
                              LeftPattern: 
                                IRelationalPatternOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.RelationalPattern, Type: null) (Syntax: '>= 'A'') (InputType: System.Char, NarrowedType: System.Char)
                                  Value: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: A) (Syntax: ''A'')
                              RightPattern: 
                                IRelationalPatternOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.RelationalPattern, Type: null) (Syntax: '<= 'Z'') (InputType: System.Char, NarrowedType: System.Char)
                                  Value: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: Z) (Syntax: ''Z'')
                          RightPattern: 
                            IBinaryPatternOperation (BinaryOperatorKind.And) (OperationKind.BinaryPattern, Type: null) (Syntax: '>= 'a' and <= 'z'') (InputType: System.Char, NarrowedType: System.Char)
                              LeftPattern: 
                                IRelationalPatternOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.RelationalPattern, Type: null) (Syntax: '>= 'a'') (InputType: System.Char, NarrowedType: System.Char)
                                  Value: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: a) (Syntax: ''a'')
                              RightPattern: 
                                IRelationalPatternOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.RelationalPattern, Type: null) (Syntax: '<= 'z'') (InputType: System.Char, NarrowedType: System.Char)
                                  Value: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: z) (Syntax: ''z'')
                Next (Regular) Block[B2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'true')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                Next (Regular) Block[B10]
                    Leaving: {R2}
            Block[B3] - Block
                Predecessors: [B1]
                Statements (0)
                Jump if False (Regular) to Block[B5]
                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: ''_' => false')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Char, IsImplicit) (Syntax: 'input')
                      Pattern: 
                        IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: ''_'') (InputType: System.Char, NarrowedType: System.Char)
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: _) (Syntax: ''_'')
                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'false')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
                Next (Regular) Block[B10]
                    Leaving: {R2}
            Block[B5] - Block
                Predecessors: [B3]
                Statements (0)
                Jump if False (Regular) to Block[B7]
                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'not (>= '0' ... 9') => true')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Char, IsImplicit) (Syntax: 'input')
                      Pattern: 
                        INegatedPatternOperation (OperationKind.NegatedPattern, Type: null) (Syntax: 'not (>= '0' and <= '9')') (InputType: System.Char, NarrowedType: System.Char)
                          Pattern: 
                            IBinaryPatternOperation (BinaryOperatorKind.And) (OperationKind.BinaryPattern, Type: null) (Syntax: '>= '0' and <= '9'') (InputType: System.Char, NarrowedType: System.Char)
                              LeftPattern: 
                                IRelationalPatternOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.RelationalPattern, Type: null) (Syntax: '>= '0'') (InputType: System.Char, NarrowedType: System.Char)
                                  Value: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: 0) (Syntax: ''0'')
                              RightPattern: 
                                IRelationalPatternOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.RelationalPattern, Type: null) (Syntax: '<= '9'') (InputType: System.Char, NarrowedType: System.Char)
                                  Value: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: 9) (Syntax: ''9'')
                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'true')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                Next (Regular) Block[B10]
                    Leaving: {R2}
            Block[B7] - Block
                Predecessors: [B5]
                Statements (0)
                Jump if False (Regular) to Block[B9]
                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: '_ => false')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Char, IsImplicit) (Syntax: 'input')
                      Pattern: 
                        IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Char, NarrowedType: System.Char)
                    Leaving: {R2}
                Next (Regular) Block[B8]
            Block[B8] - Block
                Predecessors: [B7]
                Statements (1)
                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'false')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
                Next (Regular) Block[B10]
                    Leaving: {R2}
        }
        Block[B9] - Block
            Predecessors: [B7]
            Statements (0)
            Next (Throw) Block[null]
                IObjectCreationOperation (Constructor: System.InvalidOperationException..ctor()) (OperationKind.ObjectCreation, Type: System.InvalidOperationException, IsImplicit) (Syntax: 'input switc ... }')
                  Arguments(0)
                  Initializer: 
                    null
        Block[B10] - Block
            Predecessors: [B2] [B4] [B6] [B8]
            Statements (0)
            Next (Return) Block[B11]
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'input switc ... }')
                Leaving: {R1}
    }
    Block[B11] - Exit
        Predecessors: [B10]
        Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularWithPatternCombinators);
        }
    }
}
