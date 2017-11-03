// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation), WorkItem(21769, "https://github.com/dotnet/roslyn/issues/21769")]
        [Fact]
        public void IPropertyReferenceExpression_PropertyReferenceInDerivedTypeUsesDerivedTypeAsInstanceType()
        {
            string source = @"
class C
{
    void M1()
    {
        C2 c2 = new C2() { /*<bind>*/P1 = 1/*</bind>*/ };
    }
}

class C1
{
    public virtual int P1 { get; set; }
}

class C2 : C1
{
}
";
            string expectedOperationTree = @"
ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P1 = 1')
  Left: 
    IPropertyReferenceOperation: System.Int32 C1.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P1')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C2, IsImplicit) (Syntax: 'P1')
  Right: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IPropertyReference_StaticPropertyWithInstanceReceiver()
        {
            string source = @"
class C
{
    static int I { get; }

    public static void M()
    {
        var c = new C();
        var i1 = /*<bind>*/c.I/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceOperation: System.Int32 C.I { get; } (Static) (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'c.I')
  Instance Receiver: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0176: Member 'C.I' cannot be accessed with an instance reference; qualify it with a type name instead
                //         var i1 = /*<bind>*/c.I/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "c.I").WithArguments("C.I").WithLocation(9, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IPropertyReference_StaticPropertyWithInstanceReceiver_Indexer()
        {
            string source = @"
using System.Collections.Generic;

class C
{
    static List<int> list = new List<int>();

    public static void M()
    {
        var c = new C();
        var i1 = /*<bind>*/c.list[1]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceOperation: System.Int32 System.Collections.Generic.List<System.Int32>.this[System.Int32 index] { get; set; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'c.list[1]')
  Instance Receiver: 
    IFieldReferenceOperation: System.Collections.Generic.List<System.Int32> C.list (Static) (OperationKind.FieldReference, Type: System.Collections.Generic.List<System.Int32>, IsInvalid) (Syntax: 'c.list')
      Instance Receiver: 
        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: index) (OperationKind.Argument, Type: System.Int32) (Syntax: '1')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0176: Member 'C.list' cannot be accessed with an instance reference; qualify it with a type name instead
                //         var i1 = /*<bind>*/c.list[1]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "c.list").WithArguments("C.list").WithLocation(11, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IPropertyReference_StaticPropertyInObjectInitializer_NoInstance()
        {
            string source = @"
class C
{
    static int I1 { get; set; }
    public static void Main()
    {
        var c = new C { /*<bind>*/I1/*</bind>*/ = 1 };
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceOperation: System.Int32 C.I1 { get; set; } (Static) (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'I1')
  Instance Receiver: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1914: Static field or property 'C.I1' cannot be assigned in an object initializer
                //         var c = new C { /*<bind>*/I1/*</bind>*/ = 1 };
                Diagnostic(ErrorCode.ERR_StaticMemberInObjectInitializer, "I1").WithArguments("C.I1").WithLocation(7, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
