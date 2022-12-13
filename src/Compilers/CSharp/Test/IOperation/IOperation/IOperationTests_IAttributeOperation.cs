// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My')
  IObjectCreationOperation (Constructor: MyAttribute..ctor([System.Int32 lineNumber = -1])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: lineNumber) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'My')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 13, IsImplicit) (Syntax: 'My')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My')
          IObjectCreationOperation (Constructor: MyAttribute..ctor([System.Int32 lineNumber = -1])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My')
            Arguments(1):
                IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: lineNumber) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'My')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 13, IsImplicit) (Syntax: 'My')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Initializer:
              null
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
            VerifyFlowGraphAndDiagnosticsForTest<AttributeSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void TestCallerMemberName_Class()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class MyAttribute : Attribute
{
    public MyAttribute([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(callerName);
    }
}

[/*<bind>*/My/*</bind>*/]
class Test
{
}
";
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My')
  IObjectCreationOperation (Constructor: MyAttribute..ctor([System.String callerName = """"])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: callerName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'My')
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """", IsImplicit) (Syntax: 'My')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestCallerMemberName_Method()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class MyAttribute : Attribute
{
    public MyAttribute([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(callerName);
    }
}

class Test
{
    [/*<bind>*/My/*</bind>*/]
    public void M() { }
}
";
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My')
  IObjectCreationOperation (Constructor: MyAttribute..ctor([System.String callerName = """"])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: callerName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'My')
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""M"", IsImplicit) (Syntax: 'My')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestCallerMemberName_Parameter()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public void M([/*<bind>*/My/*</bind>*/] int x)
    {
    }
}

internal class MyAttribute : Attribute
{
    public MyAttribute([CallerMemberName] string x = null) {}
}
";
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My')
  IObjectCreationOperation (Constructor: MyAttribute..ctor([System.String x = null])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'My')
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""M"", IsImplicit) (Syntax: 'My')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
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
IAttributeOperation (OperationKind.Attribute, Type: null, IsInvalid) (Syntax: 'My')
  IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'My')
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
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My')
  IObjectCreationOperation (Constructor: MyAttribute..ctor()) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My')
    Arguments(0)
    Initializer:
      null
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
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(""Value"")')
  IObjectCreationOperation (Constructor: MyAttribute..ctor(System.String value)) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(""Value"")')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '""Value""')
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Value"") (Syntax: '""Value""')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestAttributeWithExplicitArgument_IncorrectTypePassed()
        {
            string source = @"
using System;

class MyAttribute : Attribute
{
    public MyAttribute(string value) { }
}

[/*<bind>*/My(0)/*</bind>*/]
class C
{
}
";
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null, IsInvalid) (Syntax: 'My(0)')
  IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'My(0)')
    Children(1):
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
";
            var expectedDiagnostics = new[]
            {
                // (9,15): error CS1503: Argument 1: cannot convert from 'int' to 'string'
                // [/*<bind>*/My(0)/*</bind>*/]
                Diagnostic(ErrorCode.ERR_BadArgType, "0").WithArguments("1", "int", "string").WithLocation(9, 15)
            };

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
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(""Value"")')
  IObjectCreationOperation (Constructor: MyAttribute..ctor([System.String value = """"])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(""Value"")')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '""Value""')
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Value"") (Syntax: '""Value""')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Theory]
        [CombinatorialData]
        public void TestAttributeWithOptionalParameterNotPassed(bool withParentheses)
        {
            string attribute = withParentheses ? "My()" : "My";

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
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: '{attribute}')
  IObjectCreationOperation (Constructor: MyAttribute..ctor([System.String value = """"])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: '{attribute}')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: value) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{attribute}')
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """", IsImplicit) (Syntax: '{attribute}')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
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
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(b: 1, a: 0)')
  IObjectCreationOperation (Constructor: MyAttribute..ctor(System.Int32 a, System.Int32 b)) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(b: 1, a: 0)')
    Arguments(2):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'b: 1')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null) (Syntax: 'a: 0')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
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
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(b: 1, a: 0, d: 5)')
  IObjectCreationOperation (Constructor: MyAttribute..ctor(System.Int32 a, System.Int32 b, [System.Int32 c = 2], [System.Int32 d = 3])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(b: 1, a: 0, d: 5)')
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
    Initializer:
      null
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
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(0.0f)')
  IObjectCreationOperation (Constructor: MyAttribute..ctor(System.Double x)) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(0.0f)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '0.0f')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, Constant: 0, IsImplicit) (Syntax: '0.0f')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand:
              ILiteralOperation (OperationKind.Literal, Type: System.Single, Constant: 0) (Syntax: '0.0f')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void SwitchExpression_Attribute()
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
IAttributeOperation (OperationKind.Attribute, Type: null, IsInvalid) (Syntax: 'My(1 switch ... , _ => 2 })')
  IObjectCreationOperation (Constructor: MyAttribute..ctor(System.Int32 Value)) (OperationKind.ObjectCreation, Type: MyAttribute, IsInvalid, IsImplicit) (Syntax: 'My(1 switch ... , _ => 2 })')
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
    Initializer:
      null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void BadAttributeParameterType()
        {
            string source = @"
[/*<bind>*/Boom/*</bind>*/]
class Boom : System.Attribute
{
    public Boom(int? x = 0) { }

    static void Main()
    {
        typeof(Boom).GetCustomAttributes(true);
    }
}";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // (2,2): error CS0181: Attribute constructor parameter 'x' has type 'int?', which is not a valid attribute parameter type
                // [/*<bind>*/Boom/*</bind>*/]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Boom").WithArguments("x", "int?").WithLocation(2, 12)
            };
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null, IsInvalid) (Syntax: 'Boom')
  IObjectCreationOperation (Constructor: Boom..ctor([System.Int32? x = 0])) (OperationKind.ObjectCreation, Type: Boom, IsInvalid, IsImplicit) (Syntax: 'Boom')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: x) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'Boom')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'Boom')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand:
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: 'Boom')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void BadAttributeParameterType2()
        {
            string source = @"
[/*<bind>*/Boom(null)/*</bind>*/]
class Boom : System.Attribute
{
    public Boom(int? x = 0) { }

    static void Main()
    {
        typeof(Boom).GetCustomAttributes(true);
    }
}";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // (2,2): error CS0181: Attribute constructor parameter 'x' has type 'int?', which is not a valid attribute parameter type
                // [/*<bind>*/Boom/*</bind>*/]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Boom").WithArguments("x", "int?").WithLocation(2, 12)
            };
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null, IsInvalid) (Syntax: 'Boom(null)')
  IObjectCreationOperation (Constructor: Boom..ctor([System.Int32? x = 0])) (OperationKind.ObjectCreation, Type: Boom, IsInvalid, IsImplicit) (Syntax: 'Boom(null)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'null')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, Constant: null, IsImplicit) (Syntax: 'null')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand:
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void AttributeWithExplicitNullArgument()
        {
            string source = @"
using System;

[/*<bind>*/My(null)/*</bind>*/]
class MyAttribute : Attribute
{
    public MyAttribute(Type opt = null)
    {
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(null)')
  IObjectCreationOperation (Constructor: MyAttribute..ctor([System.Type opt = null])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(null)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: opt) (OperationKind.Argument, Type: null) (Syntax: 'null')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Type, Constant: null, IsImplicit) (Syntax: 'null')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand:
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void AttributeWithDefaultNullArgument()
        {
            string source = @"
using System;

[/*<bind>*/My/*</bind>*/]
class MyAttribute : Attribute
{
    public MyAttribute(Type opt = null)
    {
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My')
  IObjectCreationOperation (Constructor: MyAttribute..ctor([System.Type opt = null])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: opt) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'My')
          IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Type, Constant: null, IsImplicit) (Syntax: 'My')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void AttributeWithTypeOfArgument()
        {
            string source = @"
using System;

[/*<bind>*/My(typeof(MyAttribute))/*</bind>*/]
class MyAttribute : Attribute
{
    public MyAttribute(Type opt = null)
    {
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(typeof(MyAttribute))')
  IObjectCreationOperation (Constructor: MyAttribute..ctor([System.Type opt = null])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(typeof(MyAttribute))')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: opt) (OperationKind.Argument, Type: null) (Syntax: 'typeof(MyAttribute)')
          ITypeOfOperation (OperationKind.TypeOf, Type: System.Type) (Syntax: 'typeof(MyAttribute)')
            TypeOperand: MyAttribute
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void InvalidValue()
        {
            string source = @"
using System.Security.Permissions;

[/*<bind>*/A/*</bind>*/]
class A : CodeAccessSecurityAttribute
{
    public A(SecurityAction a = 0) : base(a)
    {
    }

}
";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // (4,12): error CS7049: Security attribute 'A' has an invalid SecurityAction value '0'
                // [/*<bind>*/A/*</bind>*/]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidAction, "A").WithArguments("A", "0").WithLocation(4, 12),
                // (5,7): error CS0534: 'A' does not implement inherited abstract member 'SecurityAttribute.CreatePermission()'
                // class A : CodeAccessSecurityAttribute
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "A").WithArguments("A", "System.Security.Permissions.SecurityAttribute.CreatePermission()").WithLocation(5, 7)
            };
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null, IsInvalid) (Syntax: 'A')
  IObjectCreationOperation (Constructor: A..ctor([System.Security.Permissions.SecurityAction a = (System.Security.Permissions.SecurityAction)0])) (OperationKind.ObjectCreation, Type: A, IsInvalid, IsImplicit) (Syntax: 'A')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'A')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Security.Permissions.SecurityAction, Constant: 0, IsInvalid, IsImplicit) (Syntax: 'A')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand:
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: 'A')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void InvalidAttributeParameterType()
        {
            string source = @"
using System;

[/*<bind>*/My/*</bind>*/]
class MyAttribute : Attribute
{
    public MyAttribute(params int[][,] x) { }
}
";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // (4,12): error CS0181: Attribute constructor parameter 'x' has type 'int[][*,*]', which is not a valid attribute parameter type
                // [/*<bind>*/My/*</bind>*/]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "My").WithArguments("x", "int[][*,*]").WithLocation(4, 12)
            };
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null, IsInvalid) (Syntax: 'My')
  IObjectCreationOperation (Constructor: MyAttribute..ctor(params System.Int32[][,] x)) (OperationKind.ObjectCreation, Type: MyAttribute, IsInvalid, IsImplicit) (Syntax: 'My')
    Arguments(1):
        IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: x) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'My')
          IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[][,], IsInvalid, IsImplicit) (Syntax: 'My')
            Dimension Sizes(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: 'My')
            Initializer:
              IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid, IsImplicit) (Syntax: 'My')
                Element Values(0)
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Theory]
        [InlineData("assembly")]
        [InlineData("module")]
        public void AssemblyAndModuleAttributeTargets(string attributeTarget)
        {
            string source = $"""
                using System;

                [{attributeTarget}: /*<bind>*/CLSCompliant(true)/*</bind>*/]
                """;

            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'CLSCompliant(true)')
  IObjectCreationOperation (Constructor: System.CLSCompliantAttribute..ctor(System.Boolean isCompliant)) (OperationKind.ObjectCreation, Type: System.CLSCompliantAttribute, IsImplicit) (Syntax: 'CLSCompliant(true)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: isCompliant) (OperationKind.Argument, Type: null) (Syntax: 'true')
          ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            var expectedDiagnostics = attributeTarget switch
            {
                "assembly" => DiagnosticDescription.None,
                "module" => new DiagnosticDescription[]
                {
                    // (3,20): warning CS3012: You must specify the CLSCompliant attribute on the assembly, not the module, to enable CLS compliance checking
                    // [module: /*<bind>*/CLSCompliant(true)/*</bind>*/]
                    Diagnostic(ErrorCode.WRN_CLS_NotOnModules, "CLSCompliant(true)").WithLocation(3, 20),
                },
                _ => throw TestExceptionUtilities.UnexpectedValue(attributeTarget),
            };

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ReturnAttributeTarget()
        {
            string source = """
                using System;

                class MyAttribute : Attribute
                {
                    public MyAttribute(int i) 
                    {
                    }
                }

                public class C
                {
                    [return: /*<bind>*/My(10)/*</bind>*/]
                    public string M() => null;
                }
                """;

            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(10)')
  IObjectCreationOperation (Constructor: MyAttribute..ctor(System.Int32 i)) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(10)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '10')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ComplexAttribute()
        {
            string source = @"
using System;

[/*<bind>*/My(i: 1, b: true, o: 2)/*</bind>*/]
class MyAttribute : Attribute
{
    public MyAttribute(bool b, int i, params object[] o) 
    {
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(i: 1, b: true, o: 2)')
    IObjectCreationOperation (Constructor: MyAttribute..ctor(System.Boolean b, System.Int32 i, params System.Object[] o)) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(i: 1, b: true, o: 2)')
    Arguments(3):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i: 1')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'b: true')
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'My(i: 1, b: true, o: 2)')
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Object[], IsImplicit) (Syntax: 'My(i: 1, b: true, o: 2)')
            Dimension Sizes(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'My(i: 1, b: true, o: 2)')
            Initializer:
                IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'My(i: 1, b: true, o: 2)')
                Element Values(1):
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '2')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
        null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ComplexAttributeWithNamedArgument()
        {
            string source = @"
using System;

[My(i: 1, b: true, o: 2)]
[/*<bind>*/My(i: 1, b: true, o: 2, B = 10, D = 5)/*</bind>*/]
[My(i: 1, b: true, o: 2)]
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class MyAttribute : Attribute
{
    public MyAttribute(bool b, int i, params object[] o) 
    {
    }

    public int B { get; set; }
    public int C { get; set; }
    public int D { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(i: 1, b: ...  10, D = 5)')
    IObjectCreationOperation (Constructor: MyAttribute..ctor(System.Boolean b, System.Int32 i, params System.Object[] o)) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(i: 1, b: ...  10, D = 5)')
    Arguments(3):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i: 1')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'b: true')
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'My(i: 1, b: ...  10, D = 5)')
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Object[], IsImplicit) (Syntax: 'My(i: 1, b: ...  10, D = 5)')
            Dimension Sizes(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'My(i: 1, b: ...  10, D = 5)')
            Initializer:
                IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'My(i: 1, b: ...  10, D = 5)')
                Element Values(1):
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '2')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MyAttribute, IsImplicit) (Syntax: 'My(i: 1, b: ...  10, D = 5)')
        Initializers(2):
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'B = 10')
                Left:
                IPropertyReferenceOperation: System.Int32 MyAttribute.B { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'B')
                    Instance Receiver:
                    null
                Right:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'D = 5')
                Left:
                IPropertyReferenceOperation: System.Int32 MyAttribute.D { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'D')
                    Instance Receiver:
                    null
                Right:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'My(i: 1, b: ...  10, D = 5)')
              Value:
                IObjectCreationOperation (Constructor: MyAttribute..ctor(System.Boolean b, System.Int32 i, params System.Object[] o)) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(i: 1, b: ...  10, D = 5)')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i: 1')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'b: true')
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'My(i: 1, b: ...  10, D = 5)')
                        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Object[], IsImplicit) (Syntax: 'My(i: 1, b: ...  10, D = 5)')
                          Dimension Sizes(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'My(i: 1, b: ...  10, D = 5)')
                          Initializer:
                            IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'My(i: 1, b: ...  10, D = 5)')
                              Element Values(1):
                                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '2')
                                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      (Boxing)
                                    Operand:
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'B = 10')
              Left:
                IPropertyReferenceOperation: System.Int32 MyAttribute.B { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'B')
                  Instance Receiver:
                    null
              Right:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'D = 5')
              Left:
                IPropertyReferenceOperation: System.Int32 MyAttribute.D { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'D')
                  Instance Receiver:
                    null
              Right:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
            IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(i: 1, b: ...  10, D = 5)')
              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyAttribute, IsImplicit) (Syntax: 'My(i: 1, b: ...  10, D = 5)')
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
            VerifyFlowGraphAndDiagnosticsForTest<AttributeSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void AttributeOnLocalFunction()
        {
            string source = @"
using System;

class MyAttribute : Attribute
{
    public MyAttribute(int i) 
    {
        local();

        [/*<bind>*/My(10)/*</bind>*/]
        void local() { }
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(10)')
    IObjectCreationOperation (Constructor: MyAttribute..ctor(System.Int32 i)) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(10)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '10')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
        null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void AttributeOnBackingField()
        {
            string source = @"
using System;

class MyAttribute : Attribute
{
    public MyAttribute(int i) 
    {
    }

    [field: /*<bind>*/My(10)/*</bind>*/]
    public string S { get; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(10)')
    IObjectCreationOperation (Constructor: MyAttribute..ctor(System.Int32 i)) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(10)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '10')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
        null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void PropertyAttributeTargetOnRecordPositionalParameter()
        {
            string source = @"
using System;

class MyAttribute : Attribute
{
    public MyAttribute(int i)
    {
    }
}

record R([property: /*<bind>*/My(10)/*</bind>*/] string S);
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(10)')
  IObjectCreationOperation (Constructor: MyAttribute..ctor(System.Int32 i)) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(10)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '10')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
        null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics, targetFramework: TargetFramework.NetCoreApp);
        }

        [Fact]
        public void AttributeOnInvalidLocation()
        {
            string source = @"
using System;

class MyAttribute : Attribute
{
    public MyAttribute(int i)
    {
    }
}

class C
{
    void M()
    {
        [/*<bind>*/My(10)/*</bind>*/]
        int x = 5;
        _ = x;
    }
}
";
            var expectedDiagnostics = new[]
            {
                // (15,9): error CS7014: Attributes are not valid in this context.
                //         [/*<bind>*/My(10)/*</bind>*/]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[/*<bind>*/My(10)/*</bind>*/]").WithLocation(15, 9),
            };

            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null, IsInvalid) (Syntax: 'My(10)')
  IObjectCreationOperation (Constructor: MyAttribute..ctor(System.Int32 i)) (OperationKind.ObjectCreation, Type: MyAttribute, IsInvalid, IsImplicit) (Syntax: 'My(10)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '10')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10, IsInvalid) (Syntax: '10')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void AttributeOnEnumMember()
        {
            string source = @"
using System;

class MyAttribute : Attribute
{
    public MyAttribute(int i)
    {
    }
}

enum E
{
        [/*<bind>*/My(10)/*</bind>*/]
        A,
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(10)')
  IObjectCreationOperation (Constructor: MyAttribute..ctor(System.Int32 i)) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(10)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '10')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void AttributeOnTypeParameter()
        {
            string source = @"
using System;

class MyAttribute : Attribute
{
    public MyAttribute(int i)
    {
    }
}

class C <[/*<bind>*/My(10)/*</bind>*/] T>
{
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(10)')
  IObjectCreationOperation (Constructor: MyAttribute..ctor(System.Int32 i)) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(10)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '10')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void AttributeOnTypeParameterWithCallerMemberName_Method()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class MyAttribute : Attribute
{
    public MyAttribute([CallerMemberName] string s = ""default"")
    {
    }
}

class C
{
    void M<[/*<bind>*/My/*</bind>*/] T>() { }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My')
  IObjectCreationOperation (Constructor: MyAttribute..ctor([System.String s = ""default""])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'My')
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""M"", IsImplicit) (Syntax: 'My')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void AttributeOnTypeParameterWithCallerMemberName_Class()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class MyAttribute : Attribute
{
    public MyAttribute([CallerMemberName] string s = ""default"")
    {
    }
}

class C<[/*<bind>*/My/*</bind>*/] T>
{
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My')
  IObjectCreationOperation (Constructor: MyAttribute..ctor([System.String s = ""default""])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'My')
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""default"", IsImplicit) (Syntax: 'My')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
