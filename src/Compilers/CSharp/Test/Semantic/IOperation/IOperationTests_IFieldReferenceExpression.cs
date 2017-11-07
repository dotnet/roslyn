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
      IFieldReferenceOperation: System.String C.field (Static) (OperationKind.FieldReference, Type: System.String, Constant: ""field"") (Syntax: 'field')
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
IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int i')
  IFieldReferenceOperation: System.Int32 Script.i (IsDeclaration: True) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
    Instance Receiver: 
      IInstanceReferenceOperation (OperationKind.InstanceReference, Type: Script, IsImplicit) (Syntax: 'i')
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
ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(int i1, int i2)')
  Elements(2):
      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int i1')
        IFieldReferenceOperation: System.Int32 Script.i1 (IsDeclaration: True) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i1')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: Script, IsImplicit) (Syntax: 'i1')
      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int i2')
        IFieldReferenceOperation: System.Int32 Script.i2 (IsDeclaration: True) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i2')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: Script, IsImplicit) (Syntax: 'i2')
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
IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
  IFieldReferenceOperation: System.Int32 Script.i (IsDeclaration: True) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
    Instance Receiver: 
      IInstanceReferenceOperation (OperationKind.InstanceReference, Type: Script, IsImplicit) (Syntax: 'i')
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
ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(var i1, var i2)')
  Elements(2):
      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i1')
        IFieldReferenceOperation: System.Int32 Script.i1 (IsDeclaration: True) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i1')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: Script, IsImplicit) (Syntax: 'i1')
      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i2')
        IFieldReferenceOperation: System.Int32 Script.i2 (IsDeclaration: True) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i2')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: Script, IsImplicit) (Syntax: 'i2')
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
IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: 'var (i1, i2)')
  ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(i1, i2)')
    Elements(2):
        IFieldReferenceOperation: System.Int32 Script.i1 (IsDeclaration: True) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i1')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: Script, IsImplicit) (Syntax: 'i1')
        IFieldReferenceOperation: System.Int32 Script.i2 (IsDeclaration: True) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i2')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: Script, IsImplicit) (Syntax: 'i2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<DeclarationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics,
                parseOptions: TestOptions.Script);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(7582, "https://github.com/dotnet/roslyn/issues/7582")]
        [Fact]
        public void IFieldReferenceExpression_ImplicitThis ()
        {
            string source = @"
class C
{
    int i;
    void M()
    {
        /*<bind>*/i/*</bind>*/ = 1;
        i++;
    }
}
";
            string expectedOperationTree = @"
IFieldReferenceOperation: System.Int32 C.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
  Instance Receiver: 
    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(7582, "https://github.com/dotnet/roslyn/issues/7582")]
        [Fact]
        public void IFieldReferenceExpression_ExplicitThis()
        {
            string source = @"
class C
{
    int i;
    void M()
    {
        /*<bind>*/this.i/*</bind>*/ = 1;
        i++;
    }
}
";
            string expectedOperationTree = @"
IFieldReferenceOperation: System.Int32 C.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'this.i')
  Instance Receiver: 
    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(7582, "https://github.com/dotnet/roslyn/issues/7582")]
        [Fact]
        public void IFieldReferenceExpression_base()
        {
            string source = @"
class C
{
    protected int i;
}
class B : C
{
    void M()
    {
        /*<bind>*/base.i/*</bind>*/ = 1;
        i++;
    }
}
";
            string expectedOperationTree = @"
IFieldReferenceOperation: System.Int32 C.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'base.i')
  Instance Receiver: 
    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C) (Syntax: 'base')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReference_StaticFieldWithInstanceReceiver()
        {
            string source = @"
class C
{
    static int i;

    public static void M()
    {
        var c = new C();
        var i1 = /*<bind>*/c.i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IFieldReferenceOperation: System.Int32 C.i (Static) (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'c.i')
  Instance Receiver: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0176: Member 'C.i' cannot be accessed with an instance reference; qualify it with a type name instead
                //         var i1 = /*<bind>*/c.i/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "c.i").WithArguments("C.i").WithLocation(9, 28),
                // CS0649: Field 'C.i' is never assigned to, and will always have its default value 0
                //     static int i;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i").WithArguments("C.i", "0").WithLocation(4, 16)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReference_StaticFieldInObjectInitializer_NoInstance()
        {
            string source = @"
class C
{
    static int i1;
    public static void Main()
    {
        var c = new C { /*<bind>*/i1/*</bind>*/ = 1 };
    }
}
";
            string expectedOperationTree = @"
IFieldReferenceOperation: System.Int32 C.i1 (Static) (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'i1')
  Instance Receiver: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1914: Static field or property 'C.i1' cannot be assigned in an object initializer
                //         var c = new C { /*<bind>*/i1/*</bind>*/ = 1 };
                Diagnostic(ErrorCode.ERR_StaticMemberInObjectInitializer, "i1").WithArguments("C.i1").WithLocation(7, 35),
                // CS0414: The field 'C.i1' is assigned but its value is never used
                //     static int i1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i1").WithArguments("C.i1").WithLocation(4, 16)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReference_StaticField()
        {
            string source = @"
class C
{
    static int i;

    public static void M()
    {
        var i1 = /*<bind>*/C.i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IFieldReferenceOperation: System.Int32 C.i (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'C.i')
  Instance Receiver: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0649: Field 'C.i' is never assigned to, and will always have its default value 0
                //     static int i;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i").WithArguments("C.i", "0").WithLocation(4, 16)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReference_InstanceField_InvalidAccessOffOfClass()
        {
            string source = @"
class C
{
    int i;

    public static void M()
    {
        var i1 = /*<bind>*/C.i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IFieldReferenceOperation: System.Int32 C.i (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'C.i')
  Instance Receiver: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0120: An object reference is required for the non-static field, method, or property 'C.i'
                //         var i1 = /*<bind>*/C.i/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.i").WithArguments("C.i").WithLocation(8, 28),
                // CS0649: Field 'C.i' is never assigned to, and will always have its default value 0
                //     int i;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i").WithArguments("C.i", "0").WithLocation(4, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }


    }
}
