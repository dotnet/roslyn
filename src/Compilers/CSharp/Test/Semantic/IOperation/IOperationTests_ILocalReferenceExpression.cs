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
        public void ILocalReferenceExpression_OutVar()
        {
            string source = @"
using System;

public class C1
{
    public virtual void M1()
    {
        M2(out /*<bind>*/var i/*</bind>*/);
    }

    public void M2(out int i )
    {
        i = 0;
    }
}
";
            string expectedOperationTree = @"
IArgument (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument) (Syntax: 'var i')
  ILocalReferenceExpression: i (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'var i')
  InConversion: null
  OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<DeclarationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ILocalReferenceExpression_DeconstructionDeclaration()
        {
            string source = @"
using System;

public class C1
{
    public virtual void M1()
    {
        /*<bind>*/(var i1, var i2)/*</bind>*/ = (1, 2);
    }
}
";
            string expectedOperationTree = @"
ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(var i1, var i2)')
  Elements(2):
      ILocalReferenceExpression: i1 (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'var i1')
      ILocalReferenceExpression: i2 (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'var i2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ILocalReferenceExpression_DeconstructionDeclaration_AlternateSyntax()
        {
            string source = @"
using System;

public class C1
{
    public virtual void M1()
    {
        /*<bind>*/var (i1, i2)/*</bind>*/ = (1, 2);
    }
}
";
            string expectedOperationTree = @"
ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: 'var (i1, i2)')
  Elements(2):
      ILocalReferenceExpression: i1 (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
      ILocalReferenceExpression: i2 (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<DeclarationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
