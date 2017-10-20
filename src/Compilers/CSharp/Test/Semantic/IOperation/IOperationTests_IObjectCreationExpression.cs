// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17588, "https://github.com/dotnet/roslyn/issues/17588")]
        public void ObjectCreationWithMemberInitializers()
        {
            string source = @"
struct B
{
    public bool Field;
}

class F
{
    public int Field;
    public string Property1 { set; get; }
    public B Property2 { set; get; }
}

class C
{
    public void M1()
    /*<bind>*/{
        var x1 = new F();
        var x2 = new F() { Field = 2 };
        var x3 = new F() { Property1 = """" };
        var x4 = new F() { Property1 = """", Field = 2 };
        var x5 = new F() { Property2 = new B { Field = true } };

        var e1 = new F() { Property2 = 1 };
        var e2 = new F() { """" };
    }/*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockOperation (7 statements, 7 locals) (OperationKind.Block, IsStatement, Type: null, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: F x1
    Local_2: F x2
    Local_3: F x3
    Local_4: F x4
    Local_5: F x5
    Local_6: F e1
    Local_7: F e2
  IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null) (Syntax: 'var x1 = new F();')
    IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x1 = new F()')
      Variables: Local_1: F x1
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new F()')
          IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: F) (Syntax: 'new F()')
            Arguments(0)
            Initializer: 
              null
  IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null) (Syntax: 'var x2 = ne ... ield = 2 };')
    IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x2 = new F( ... Field = 2 }')
      Variables: Local_1: F x2
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new F() { Field = 2 }')
          IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: F) (Syntax: 'new F() { Field = 2 }')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: F) (Syntax: '{ Field = 2 }')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Int32) (Syntax: 'Field = 2')
                      Left: 
                        IFieldReferenceOperation: System.Int32 F.Field (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'Field')
                          Instance Receiver: 
                            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: F) (Syntax: 'Field')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null) (Syntax: 'var x3 = ne ... ty1 = """" };')
    IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x3 = new F( ... rty1 = """" }')
      Variables: Local_1: F x3
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new F() { ... rty1 = """" }')
          IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: F) (Syntax: 'new F() { P ... rty1 = """" }')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: F) (Syntax: '{ Property1 = """" }')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.String) (Syntax: 'Property1 = """"')
                      Left: 
                        IPropertyReferenceOperation: System.String F.Property1 { get; set; } (OperationKind.PropertyReference, IsExpression, Type: System.String) (Syntax: 'Property1')
                          Instance Receiver: 
                            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: F) (Syntax: 'Property1')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.String, Constant: """") (Syntax: '""""')
  IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null) (Syntax: 'var x4 = ne ... ield = 2 };')
    IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x4 = new F( ... Field = 2 }')
      Variables: Local_1: F x4
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new F() { ... Field = 2 }')
          IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: F) (Syntax: 'new F() { P ... Field = 2 }')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: F) (Syntax: '{ Property1 ... Field = 2 }')
                Initializers(2):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.String) (Syntax: 'Property1 = """"')
                      Left: 
                        IPropertyReferenceOperation: System.String F.Property1 { get; set; } (OperationKind.PropertyReference, IsExpression, Type: System.String) (Syntax: 'Property1')
                          Instance Receiver: 
                            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: F) (Syntax: 'Property1')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.String, Constant: """") (Syntax: '""""')
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Int32) (Syntax: 'Field = 2')
                      Left: 
                        IFieldReferenceOperation: System.Int32 F.Field (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'Field')
                          Instance Receiver: 
                            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: F) (Syntax: 'Field')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null) (Syntax: 'var x5 = ne ... = true } };')
    IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x5 = new F( ...  = true } }')
      Variables: Local_1: F x5
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new F() { ...  = true } }')
          IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: F) (Syntax: 'new F() { P ...  = true } }')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: F) (Syntax: '{ Property2 ...  = true } }')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: B) (Syntax: 'Property2 = ... ld = true }')
                      Left: 
                        IPropertyReferenceOperation: B F.Property2 { get; set; } (OperationKind.PropertyReference, IsExpression, Type: B) (Syntax: 'Property2')
                          Instance Receiver: 
                            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: F) (Syntax: 'Property2')
                      Right: 
                        IObjectCreationOperation (Constructor: B..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: B) (Syntax: 'new B { Field = true }')
                          Arguments(0)
                          Initializer: 
                            IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: B) (Syntax: '{ Field = true }')
                              Initializers(1):
                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Boolean) (Syntax: 'Field = true')
                                    Left: 
                                      IFieldReferenceOperation: System.Boolean B.Field (OperationKind.FieldReference, IsExpression, Type: System.Boolean) (Syntax: 'Field')
                                        Instance Receiver: 
                                          IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: B) (Syntax: 'Field')
                                    Right: 
                                      ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null, IsInvalid) (Syntax: 'var e1 = ne ... rty2 = 1 };')
    IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'e1 = new F( ... erty2 = 1 }')
      Variables: Local_1: F e1
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new F() { ... erty2 = 1 }')
          IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: F, IsInvalid) (Syntax: 'new F() { P ... erty2 = 1 }')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: F, IsInvalid) (Syntax: '{ Property2 = 1 }')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: B, IsInvalid) (Syntax: 'Property2 = 1')
                      Left: 
                        IPropertyReferenceOperation: B F.Property2 { get; set; } (OperationKind.PropertyReference, IsExpression, Type: B) (Syntax: 'Property2')
                          Instance Receiver: 
                            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: F) (Syntax: 'Property2')
                      Right: 
                        IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: B, IsInvalid, IsImplicit) (Syntax: '1')
                          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
  IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null, IsInvalid) (Syntax: 'var e2 = new F() { """" };')
    IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'e2 = new F() { """" }')
      Variables: Local_1: F e2
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new F() { """" }')
          IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: F, IsInvalid) (Syntax: 'new F() { """" }')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: F, IsInvalid) (Syntax: '{ """" }')
                Initializers(1):
                    IInvalidOperation (OperationKind.Invalid, IsExpression, Type: ?, IsInvalid, IsImplicit) (Syntax: '""""')
                      Children(1):
                          ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.String, Constant: """", IsInvalid) (Syntax: '""""')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int' to 'B'
                //         var e1 = new F() { Property2 = 1 };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "B").WithLocation(24, 40),
                // CS1922: Cannot initialize type 'F' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         var e2 = new F() { "" };
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, @"{ """" }").WithArguments("F").WithLocation(25, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17588, "https://github.com/dotnet/roslyn/issues/17588")]
        public void ObjectCreationWithCollectionInitializer()
        {
            string source = @"
using System.Collections.Generic;

class C
{
	private readonly int field;
	public void M1(int x)
	{
		int y = 0;
		var x1 = /*<bind>*/new List<int> { x, y, field }/*</bind>*/;
	}
}
";
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new List<in ...  y, field }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ x, y, field }')
      Initializers(3):
          ICollectionElementInitializerOperation (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: 'x')
            Arguments(1):
                IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
          ICollectionElementInitializerOperation (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: 'y')
            Arguments(1):
                ILocalReferenceOperation: y (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'y')
          ICollectionElementInitializerOperation (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: 'field')
            Arguments(1):
                IFieldReferenceOperation: System.Int32 C.field (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'field')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: C, IsImplicit) (Syntax: 'field')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0649: Field 'C.field' is never assigned to, and will always have its default value 0
                // 	private readonly int field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("C.field", "0").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17588, "https://github.com/dotnet/roslyn/issues/17588")]
        public void ObjectCreationWithNestedCollectionInitializer()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    private readonly int field = 0;
    public void M1(int x)
    {
        int y = 0;
        var x1 = /*<bind>*/new List<List<int>> {
            new[] { x, y }.ToList(),
            new List<int> { field }
        }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>) (Syntax: 'new List<Li ... }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>) (Syntax: '{ ... }')
      Initializers(2):
          ICollectionElementInitializerOperation (AddMethod: void System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>.Add(System.Collections.Generic.List<System.Int32> item)) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: 'new[] { x, y }.ToList()')
            Arguments(1):
                IInvocationOperation (System.Collections.Generic.List<System.Int32> System.Linq.Enumerable.ToList<System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source)) (OperationKind.Invocation, IsExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new[] { x, y }.ToList()')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'new[] { x, y }')
                        IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'new[] { x, y }')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            IArrayCreationOperation (OperationKind.ArrayCreation, IsExpression, Type: System.Int32[]) (Syntax: 'new[] { x, y }')
                              Dimension Sizes(1):
                                  ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'new[] { x, y }')
                              Initializer: 
                                IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ x, y }')
                                  Element Values(2):
                                      IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
                                      ILocalReferenceOperation: y (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'y')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          ICollectionElementInitializerOperation (AddMethod: void System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>.Add(System.Collections.Generic.List<System.Int32> item)) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: 'new List<int> { field }')
            Arguments(1):
                IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new List<int> { field }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ field }')
                      Initializers(1):
                          ICollectionElementInitializerOperation (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: 'field')
                            Arguments(1):
                                IFieldReferenceOperation: System.Int32 C.field (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'field')
                                  Instance Receiver: 
                                    IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: C, IsImplicit) (Syntax: 'field')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17588, "https://github.com/dotnet/roslyn/issues/17588")]
        public void ObjectCreationWithMemberAndCollectionInitializers()
        {
            string source = @"
using System.Collections.Generic;

internal class Class
{
    public int X { get; set; }
    public List<int> Y { get; set; }
    public Dictionary<int, int> Z { get; set; }
    public Class C { get; set; }

    private readonly int field = 0;

    public void M(int x)
    {
        int y = 0;
        var c = /*<bind>*/new Class() {
            X = x,
            Y = { x, y, 3 },
            Z = { { x, y } },
            C = { X = field }
        }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: Class) (Syntax: 'new Class() ... }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: Class) (Syntax: '{ ... }')
      Initializers(4):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Int32) (Syntax: 'X = x')
            Left: 
              IPropertyReferenceOperation: System.Int32 Class.X { get; set; } (OperationKind.PropertyReference, IsExpression, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: Class) (Syntax: 'X')
            Right: 
              IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
          IMemberInitializerOperation (OperationKind.MemberInitializer, IsExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'Y = { x, y, 3 }')
            InitializedMember: 
              IPropertyReferenceOperation: System.Collections.Generic.List<System.Int32> Class.Y { get; set; } (OperationKind.PropertyReference, IsExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'Y')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: Class) (Syntax: 'Y')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ x, y, 3 }')
                Initializers(3):
                    ICollectionElementInitializerOperation (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: 'x')
                      Arguments(1):
                          IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
                    ICollectionElementInitializerOperation (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: 'y')
                      Arguments(1):
                          ILocalReferenceOperation: y (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'y')
                    ICollectionElementInitializerOperation (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: '3')
                      Arguments(1):
                          ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
          IMemberInitializerOperation (OperationKind.MemberInitializer, IsExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: 'Z = { { x, y } }')
            InitializedMember: 
              IPropertyReferenceOperation: System.Collections.Generic.Dictionary<System.Int32, System.Int32> Class.Z { get; set; } (OperationKind.PropertyReference, IsExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: 'Z')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: Class) (Syntax: 'Z')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: '{ { x, y } }')
                Initializers(1):
                    ICollectionElementInitializerOperation (AddMethod: void System.Collections.Generic.Dictionary<System.Int32, System.Int32>.Add(System.Int32 key, System.Int32 value)) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: '{ x, y }')
                      Arguments(2):
                          IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
                          ILocalReferenceOperation: y (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'y')
          IMemberInitializerOperation (OperationKind.MemberInitializer, IsExpression, Type: Class) (Syntax: 'C = { X = field }')
            InitializedMember: 
              IPropertyReferenceOperation: Class Class.C { get; set; } (OperationKind.PropertyReference, IsExpression, Type: Class) (Syntax: 'C')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: Class) (Syntax: 'C')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: Class) (Syntax: '{ X = field }')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Int32) (Syntax: 'X = field')
                      Left: 
                        IPropertyReferenceOperation: System.Int32 Class.X { get; set; } (OperationKind.PropertyReference, IsExpression, Type: System.Int32) (Syntax: 'X')
                          Instance Receiver: 
                            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: Class) (Syntax: 'X')
                      Right: 
                        IFieldReferenceOperation: System.Int32 Class.field (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'field')
                          Instance Receiver: 
                            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: Class, IsImplicit) (Syntax: 'field')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17588, "https://github.com/dotnet/roslyn/issues/17588")]
        public void ObjectCreationWithArrayInitializer()
        {
            string source = @"
class C
{
    int[] a;

    static void Main()
    {
        var a = /*<bind>*/new C { a = { [0] = 1, [1] = 2 } }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: C) (Syntax: 'new C { a = ... [1] = 2 } }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: C) (Syntax: '{ a = { [0] ... [1] = 2 } }')
      Initializers(1):
          IMemberInitializerOperation (OperationKind.MemberInitializer, IsExpression, Type: System.Int32[]) (Syntax: 'a = { [0] = 1, [1] = 2 }')
            InitializedMember: 
              IFieldReferenceOperation: System.Int32[] C.a (OperationKind.FieldReference, IsExpression, Type: System.Int32[]) (Syntax: 'a')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: C) (Syntax: 'a')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: System.Int32[]) (Syntax: '{ [0] = 1, [1] = 2 }')
                Initializers(2):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Int32) (Syntax: '[0] = 1')
                      Left: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, IsExpression, Type: System.Int32) (Syntax: '[0]')
                          Array reference: 
                            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: System.Int32[], IsImplicit) (Syntax: 'a')
                          Indices(1):
                              ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Int32) (Syntax: '[1] = 2')
                      Left: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, IsExpression, Type: System.Int32) (Syntax: '[1]')
                          Array reference: 
                            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: System.Int32[], IsImplicit) (Syntax: 'a')
                          Indices(1):
                              ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // warning CS0414: The field 'C.a' is assigned but its value is never used
                //     int[] a;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "a").WithArguments("C.a").WithLocation(4, 11)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

    }
}
