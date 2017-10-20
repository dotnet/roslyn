// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void FieldReference_Attribute()
        {
            string source = @"
using System.Diagnostics;

class C
{
    private const string field = nameof(field);

    [/*<bind>*/Conditional(field)/*</bind>*/]
    void M()
    {
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: null) (Syntax: 'Conditional(field)')
  Children(1):
      IFieldReferenceOperation: System.String C.field (Static) (OperationKind.FieldReference, IsExpression, Type: System.String, Constant: ""field"") (Syntax: 'field')
        Instance Receiver: 
          null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReferenceExpression_OutVar_Script()
        {
            string source = @"
public void M2(out int i )
{
    i = 0;
}

M2(out /*<bind>*/int i/*</bind>*/);
";
            string expectedOperationTree = @"
IDeclarationExpressionOperation (OperationKind.DeclarationExpression, IsExpression, Type: System.Int32) (Syntax: 'int i')
  IFieldReferenceOperation: System.Int32 Script.i (IsDeclaration: True) (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'i')
    Instance Receiver: 
      IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: Script, IsImplicit) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<DeclarationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics,
                parseOptions: TestOptions.Script);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReferenceExpression_DeconstructionDeclaration_Script()
        {
            string source = @"
/*<bind>*/(int i1, int i2)/*</bind>*/ = (1, 2);
";
            string expectedOperationTree = @"
ITupleOperation (OperationKind.Tuple, IsExpression, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(int i1, int i2)')
  Elements(2):
      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, IsExpression, Type: System.Int32) (Syntax: 'int i1')
        IFieldReferenceOperation: System.Int32 Script.i1 (IsDeclaration: True) (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'i1')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: Script, IsImplicit) (Syntax: 'i1')
      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, IsExpression, Type: System.Int32) (Syntax: 'int i2')
        IFieldReferenceOperation: System.Int32 Script.i2 (IsDeclaration: True) (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'i2')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: Script, IsImplicit) (Syntax: 'i2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics,
                parseOptions: TestOptions.Script);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReferenceExpression_InferenceOutVar_Script()
        {
            string source = @"
public void M2(out int i )
{
    i = 0;
}

M2(out /*<bind>*/var i/*</bind>*/);
";
            string expectedOperationTree = @"
IDeclarationExpressionOperation (OperationKind.DeclarationExpression, IsExpression, Type: System.Int32) (Syntax: 'var i')
  IFieldReferenceOperation: System.Int32 Script.i (IsDeclaration: True) (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'i')
    Instance Receiver: 
      IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: Script, IsImplicit) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<DeclarationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics,
                parseOptions: TestOptions.Script);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReferenceExpression_InferenceDeconstructionDeclaration_Script()
        {
            string source = @"
/*<bind>*/(var i1, var i2)/*</bind>*/ = (1, 2);
";
            string expectedOperationTree = @"
ITupleOperation (OperationKind.Tuple, IsExpression, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(var i1, var i2)')
  Elements(2):
      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, IsExpression, Type: System.Int32) (Syntax: 'var i1')
        IFieldReferenceOperation: System.Int32 Script.i1 (IsDeclaration: True) (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'i1')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: Script, IsImplicit) (Syntax: 'i1')
      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, IsExpression, Type: System.Int32) (Syntax: 'var i2')
        IFieldReferenceOperation: System.Int32 Script.i2 (IsDeclaration: True) (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'i2')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: Script, IsImplicit) (Syntax: 'i2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics,
                parseOptions: TestOptions.Script);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReferenceExpression_InferenceDeconstructionDeclaration_AlternateSyntax_Script()
        {
            string source = @"
/*<bind>*/var (i1, i2)/*</bind>*/ = (1, 2);
";
            string expectedOperationTree = @"
IDeclarationExpressionOperation (OperationKind.DeclarationExpression, IsExpression, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: 'var (i1, i2)')
  ITupleOperation (OperationKind.Tuple, IsExpression, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(i1, i2)')
    Elements(2):
        IFieldReferenceOperation: System.Int32 Script.i1 (IsDeclaration: True) (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'i1')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: Script, IsImplicit) (Syntax: 'i1')
        IFieldReferenceOperation: System.Int32 Script.i2 (IsDeclaration: True) (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'i2')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: Script, IsImplicit) (Syntax: 'i2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<DeclarationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics,
                parseOptions: TestOptions.Script);
        }
    }
}
