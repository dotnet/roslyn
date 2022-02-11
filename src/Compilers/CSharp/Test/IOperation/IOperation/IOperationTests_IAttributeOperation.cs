// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.IOperation)]
    public class IOperationTests_IAttributeOperation : SemanticModelTestBase
    {
        [Fact]
        public void TestCallerInfoImplicitCall()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class MyAttribute : Attribute
{
    public MyAttribute([CallerLineNumber] int lineNumber = -1)
    {
        Console.WriteLine(lineNumber);
    }
}

[/*<bind>*/My/*</bind>*/]
class Test { }
";
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: MyAttribute) (Syntax: 'My')
  Arguments(1):
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: lineNumber) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'My')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 13, IsImplicit) (Syntax: 'My')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NamedArguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestNonExistingAttribute()
        {
            string source = @"
using System;

[/*<bind>*/My/*</bind>*/]
class C
{
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'My')
  Children(0)
";
            var expectedDiagnostics = new[]
            {
                // (4,12): error CS0246: The type or namespace name 'MyAttribute' could not be found (are you missing a using directive or an assembly reference?)
                // [/*<bind>*/My/*</bind>*/]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "My").WithArguments("MyAttribute").WithLocation(4, 12),
                // (4,12): error CS0246: The type or namespace name 'My' could not be found (are you missing a using directive or an assembly reference?)
                // [/*<bind>*/My/*</bind>*/]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "My").WithArguments("My").WithLocation(4, 12),
            };

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestAttributeWithoutArguments()
        {
            string source = @"
using System;

class MyAttribute : Attribute { }

[/*<bind>*/My/*</bind>*/]
class C
{
}
";
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: MyAttribute) (Syntax: 'My')
  Arguments(0)
  NamedArguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestAttributeWithExplicitArgument()
        {
            string source = @"
using System;

class MyAttribute : Attribute
{
    public MyAttribute(string value) { }
}

[/*<bind>*/My(""Value"")/*</bind>*/]
class C
{
}
";
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: MyAttribute) (Syntax: 'My(""Value"")')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '""Value""')
        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Value"") (Syntax: '""Value""')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NamedArguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestAttributeWithExplicitArgumentOptionalParameter()
        {
            string source = @"
using System;

class MyAttribute : Attribute
{
    public MyAttribute(string value = """") { }
}

[/*<bind>*/My(""Value"")/*</bind>*/]
class C
{
}
";
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: MyAttribute) (Syntax: 'My(""Value"")')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '""Value""')
        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Value"") (Syntax: '""Value""')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NamedArguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Theory]
        [CombinatorialData]
        public void TestAttributeWithOptionalParameterNotPassed(bool withParentheses)
        {
            string attribute = withParentheses switch
            {
                true => "My()",
                false => "My",
            };

            string attributeListSyntax = $"[/*<bind>*/{attribute}/*</bind>*/]";

            string source = @"
using System;

class MyAttribute : Attribute
{
    public MyAttribute(string value = """") { }
}
" + attributeListSyntax + @"
class C
{
}
";
            string expectedOperationTree = $@"
IAttributeOperation (OperationKind.Attribute, Type: MyAttribute) (Syntax: '{attribute}')
  Arguments(1):
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: value) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{attribute}')
        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """", IsImplicit) (Syntax: '{attribute}')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NamedArguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestAttributeWithUnorderedArguments()
        {
            string source = @"
using System;

class MyAttribute : Attribute
{
    public MyAttribute(int a, int b) { }
}

[/*<bind>*/My(b: 1, a: 0)/*</bind>*/]
class C
{
}
";
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: MyAttribute) (Syntax: 'My(b: 1, a: 0)')
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'b: 1')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null) (Syntax: 'a: 0')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NamedArguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestAttributeWithUnorderedArgumentsAndOptionalParameters()
        {
            string source = @"
using System;

class MyAttribute : Attribute
{
    public MyAttribute(int a, int b, int c = 2, int d = 3) { }
}

[/*<bind>*/My(b: 1, a: 0, d: 5)/*</bind>*/]
class C
{
}
";
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: MyAttribute) (Syntax: 'My(b: 1, a: 0, d: 5)')
  Arguments(4):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'b: 1')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null) (Syntax: 'a: 0')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: d) (OperationKind.Argument, Type: null) (Syntax: 'd: 5')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'My(b: 1, a: 0, d: 5)')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'My(b: 1, a: 0, d: 5)')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NamedArguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestConversion()
        {
            string source = @"
using System;


[/*<bind>*/My(0.0f)/*</bind>*/]
class MyAttribute : Attribute
{
    public MyAttribute(double x) { }
}
";
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: MyAttribute) (Syntax: 'My(0.0f)')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '0.0f')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, Constant: 0, IsImplicit) (Syntax: '0.0f')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand:
            ILiteralOperation (OperationKind.Literal, Type: System.Single, Constant: 0) (Syntax: '0.0f')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NamedArguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TargetTypedSwitch_Attribute()
        {
            string source = @"
using System;
class Program
{
    [/*<bind>*/My(1 switch { 1 => 1, _ => 2 })/*</bind>*/]
    public static void M1() { }
}
public class MyAttribute : Attribute
{
    public MyAttribute(int Value) { }
}
public class A
{
    public static implicit operator int(A a) => 4;
}
public class B
{
    public static implicit operator int(B b) => 2;
}
";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // (5,19): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [/*<bind>*/My(1 switch { 1 => 1, _ => 2 })/*</bind>*/]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "1 switch { 1 => 1, _ => 2 }").WithLocation(5, 19),
            };
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: MyAttribute, IsInvalid) (Syntax: 'My(1 switch ... , _ => 2 })')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Value) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '1 switch {  ... 1, _ => 2 }')
        ISwitchExpressionOperation (2 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Int32, IsInvalid) (Syntax: '1 switch {  ... 1, _ => 2 }')
          Value:
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
          Arms(2):
              ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '1 => 1')
                Pattern:
                  IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid) (Syntax: '1') (InputType: System.Int32, NarrowedType: System.Int32)
                    Value:
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                Value:
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
              ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '_ => 2')
                Pattern:
                  IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null, IsInvalid) (Syntax: '_') (InputType: System.Int32, NarrowedType: System.Int32)
                Value:
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NamedArguments(0)
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
