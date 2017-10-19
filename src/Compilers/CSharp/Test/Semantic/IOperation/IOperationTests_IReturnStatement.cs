// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void SimpleReturnFromRegularMethod()
        {
            string source = @"
class C
{
    static void Method()
    {
        /*<bind>*/return;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return;')
  ReturnedValue: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ReturnWithValueFromRegularMethod()
        {
            string source = @"
class C
{
    static bool Method()
    {
        /*<bind>*/return true;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return true;')
  ReturnedValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void YieldReturnFromRegularMethod()
        {
            string source = @"
using System.Collections.Generic;
class C
{
    static IEnumerable<int> Method()
    {
        /*<bind>*/yield return 0;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.YieldReturn, Type: null) (Syntax: 'yield return 0;')
  ReturnedValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<YieldStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void YieldBreakFromRegularMethod()
        {
            string source = @"
using System.Collections.Generic;
class C
{
    static IEnumerable<int> Method()
    {
        yield return 0;
        /*<bind>*/yield break;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.YieldBreak, Type: null) (Syntax: 'yield break;')
  ReturnedValue: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<YieldStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(7299, "https://github.com/dotnet/roslyn/issues/7299")]
        public void Return_ConstantConversions_01()
        {
            string source = @"
class C
{
    static float Method()
    {
        /*<bind>*/return 0.0;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null, IsInvalid) (Syntax: 'return 0.0;')
  ReturnedValue: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Single, Constant: 0, IsInvalid, IsImplicit) (Syntax: '0.0')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 0, IsInvalid) (Syntax: '0.0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (6,26): error CS0664: Literal of type double cannot be implicitly converted to type 'float'; use an 'F' suffix to create a literal of this type
                //         /*<bind>*/return 0.0;/*</bind>*/
                Diagnostic(ErrorCode.ERR_LiteralDoubleCast, "0.0").WithArguments("F", "float").WithLocation(6, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
