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
        public void IPropertyReference_StaticPropertyAccessOnClass()
        {
            string source = @"
class C
{
    static int I { get; }

    public static void M()
    {
        var i1 = /*<bind>*/C.I/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceOperation: System.Int32 C.I { get; } (Static) (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'C.I')
  Instance Receiver: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IPropertyReference_InstancePropertyAccessOnClass()
        {
            string source = @"
class C
{
    int I { get; }

    public static void M()
    {
        var i1 = /*<bind>*/C.I/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceOperation: System.Int32 C.I { get; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'C.I')
  Instance Receiver: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0120: An object reference is required for the non-static field, method, or property 'C.I'
                //         var i1 = /*<bind>*/C.I/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.I").WithArguments("C.I").WithLocation(8, 28)
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
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: index) (OperationKind.Argument, Type: null) (Syntax: '1')
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
        public void IPropertyReference_StaticPropertyAccessOnClass_IndexerOnProperty()
        {
            string source = @"
using System.Collections.Generic;

class C
{
    static List<int> list = new List<int>();

    public static void M()
    {
        var i1 = /*<bind>*/C.list[1]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceOperation: System.Int32 System.Collections.Generic.List<System.Int32>.this[System.Int32 index] { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'C.list[1]')
  Instance Receiver: 
    IFieldReferenceOperation: System.Collections.Generic.List<System.Int32> C.list (Static) (OperationKind.FieldReference, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'C.list')
      Instance Receiver: 
        null
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: index) (OperationKind.Argument, Type: null) (Syntax: '1')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IPropertyReference_InstancePropertyAccessOnClass_IndexerOnProperty()
        {
            string source = @"
using System.Collections.Generic;

class C
{
    List<int> list = new List<int>();

    public static void M()
    {
        var i1 = /*<bind>*/C.list[1]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceOperation: System.Int32 System.Collections.Generic.List<System.Int32>.this[System.Int32 index] { get; set; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'C.list[1]')
  Instance Receiver: 
    IFieldReferenceOperation: System.Collections.Generic.List<System.Int32> C.list (OperationKind.FieldReference, Type: System.Collections.Generic.List<System.Int32>, IsInvalid) (Syntax: 'C.list')
      Instance Receiver: 
        null
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: index) (OperationKind.Argument, Type: null) (Syntax: '1')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0120: An object reference is required for the non-static field, method, or property 'C.list'
                //         var i1 = /*<bind>*/C.list[1]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.list").WithArguments("C.list").WithLocation(10, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IPropertyReference_InstancePropertyAccessOnClass_IndexerAccessOnType()
        {
            string source = @"
class C
{
    public C this[int i]
    {
        get => null;
        set { }
    }

    public static void M()
    {
        var c1 = /*<bind>*/C[1]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceOperation: C C.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C, IsInvalid) (Syntax: 'C[1]')
  Instance Receiver: 
    IInvalidOperation (OperationKind.Invalid, Type: C, IsInvalid, IsImplicit) (Syntax: 'C')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'C')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0119: 'C' is a type, which is not valid in the given context
                //         var c1 = /*<bind>*/C[1]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "C").WithArguments("C", "type").WithLocation(12, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IPropertyReference_InstancePropertyAccessOnClass_StaticIndexerAccessOnType()
        {
            string source = @"
class C
{
    public static C this[int i]
    {
        get => null;
        set { }
    }

    public static void M()
    {
        var c1 = /*<bind>*/C[1]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceOperation: C C.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C, IsInvalid) (Syntax: 'C[1]')
  Instance Receiver: 
    IInvalidOperation (OperationKind.Invalid, Type: C, IsInvalid, IsImplicit) (Syntax: 'C')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'C')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0106: The modifier 'static' is not valid for this item
                //     public static C this[int i]
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(4, 21),
                // CS0119: 'C' is a type, which is not valid in the given context
                //         var c1 = /*<bind>*/C[1]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "C").WithArguments("C", "type").WithLocation(12, 28)
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
