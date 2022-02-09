// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
    }
}
