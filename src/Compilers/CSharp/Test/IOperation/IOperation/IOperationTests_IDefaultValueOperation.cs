// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DefaultValueFlow_01()
        {
            string source = @"
class C
{
    void M(int i)
    /*<bind>*/{
        i = default(int);
    }/*</bind>*/
}
";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = default(int);')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = default(int)')
        Left: 
          IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
        Right: 
          IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Int32, Constant: 0) (Syntax: 'default(int)')
";

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DefaultValueFlow_02()
        {
            string source = @"
class C
{
    void M(string s)
    /*<bind>*/{
        s = default;
    }/*</bind>*/
}
";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 's = default;')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 's = default')
        Left: 
          IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String) (Syntax: 's')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'default')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null) (Syntax: 'default')";

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DefaultValueFlow_03()
        {
            string source = @"
class C
{
    void M(string s)
    /*<bind>*/{
        M2(default);
    }/*</bind>*/

    static void M2(int x) { }
    static void M2(string x) { }
}
";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M2(int)' and 'C.M2(string)'
                //         M2(default);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M2").WithArguments("C.M2(int)", "C.M2(string)").WithLocation(6, 9)
            };

            string expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'M2(default);')
    Expression: 
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'M2(default)')
        Children(1):
            IDefaultValueOperation (OperationKind.DefaultValue, Type: ?) (Syntax: 'default')";

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
