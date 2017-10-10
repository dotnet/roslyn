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
IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return;')
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
IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return true;')
  ReturnedValue: 
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
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
IReturnStatement (OperationKind.YieldReturnStatement) (Syntax: 'yield return 0;')
  ReturnedValue: 
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
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
IReturnStatement (OperationKind.YieldBreakStatement) (Syntax: 'yield break;')
  ReturnedValue: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<YieldStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
