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
    public partial class IOperationTests : SemanticModelTestBase
    {
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
            // PROTOTYPE: Why IArgumentOperation is implicit here? Need to confirm whether this is correct or not.
            string expectedOperationTree = @"
    IAttributeOperation (OperationKind.Attribute, Type: MyAttribute) (Syntax: 'My(""Value"")')
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '""Value""')
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Value"") (Syntax: '""Value""')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      NamedArguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
