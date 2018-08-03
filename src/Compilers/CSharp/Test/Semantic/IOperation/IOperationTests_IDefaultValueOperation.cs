// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    }
}
