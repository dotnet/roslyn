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
IBlockStatement (7 statements, 7 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: F x1
    Local_2: F x2
    Local_3: F x3
    Local_4: F x4
    Local_5: F x5
    Local_6: F e1
    Local_7: F e2
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var x1 = new F();')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var x1 = new F();')
      Variables: Local_1: F x1
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F) (Syntax: 'new F()')
          Arguments(0)
          Initializer: null
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var x2 = ne ... ield = 2 };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var x2 = ne ... ield = 2 };')
      Variables: Local_1: F x2
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F) (Syntax: 'new F() { Field = 2 }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: F) (Syntax: '{ Field = 2 }')
              Initializers(1):
                  ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'Field = 2')
                    Left: IFieldReferenceExpression: System.Int32 F.Field (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'Field')
                        Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: F) (Syntax: 'Field')
                    Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var x3 = ne ... ty1 = """" };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var x3 = ne ... ty1 = """" };')
      Variables: Local_1: F x3
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F) (Syntax: 'new F() { P ... rty1 = """" }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: F) (Syntax: '{ Property1 = """" }')
              Initializers(1):
                  ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.String) (Syntax: 'Property1 = """"')
                    Left: IPropertyReferenceExpression: System.String F.Property1 { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'Property1')
                        Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: F) (Syntax: 'Property1')
                    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: """") (Syntax: '""""')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var x4 = ne ... ield = 2 };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var x4 = ne ... ield = 2 };')
      Variables: Local_1: F x4
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F) (Syntax: 'new F() { P ... Field = 2 }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: F) (Syntax: '{ Property1 ... Field = 2 }')
              Initializers(2):
                  ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.String) (Syntax: 'Property1 = """"')
                    Left: IPropertyReferenceExpression: System.String F.Property1 { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'Property1')
                        Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: F) (Syntax: 'Property1')
                    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: """") (Syntax: '""""')
                  ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'Field = 2')
                    Left: IFieldReferenceExpression: System.Int32 F.Field (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'Field')
                        Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: F) (Syntax: 'Field')
                    Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var x5 = ne ... = true } };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var x5 = ne ... = true } };')
      Variables: Local_1: F x5
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F) (Syntax: 'new F() { P ...  = true } }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: F) (Syntax: '{ Property2 ...  = true } }')
              Initializers(1):
                  ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B) (Syntax: 'Property2 = ... ld = true }')
                    Left: IPropertyReferenceExpression: B F.Property2 { get; set; } (OperationKind.PropertyReferenceExpression, Type: B) (Syntax: 'Property2')
                        Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: F) (Syntax: 'Property2')
                    Right: IObjectCreationExpression (Constructor: B..ctor()) (OperationKind.ObjectCreationExpression, Type: B) (Syntax: 'new B { Field = true }')
                        Arguments(0)
                        Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: B) (Syntax: '{ Field = true }')
                            Initializers(1):
                                ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean) (Syntax: 'Field = true')
                                  Left: IFieldReferenceExpression: System.Boolean B.Field (OperationKind.FieldReferenceExpression, Type: System.Boolean) (Syntax: 'Field')
                                      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: B) (Syntax: 'Field')
                                  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'var e1 = ne ... rty2 = 1 };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'var e1 = ne ... rty2 = 1 };')
      Variables: Local_1: F e1
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F, IsInvalid) (Syntax: 'new F() { P ... erty2 = 1 }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: F, IsInvalid) (Syntax: '{ Property2 = 1 }')
              Initializers(1):
                  ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B, IsInvalid) (Syntax: 'Property2 = 1')
                    Left: IPropertyReferenceExpression: B F.Property2 { get; set; } (OperationKind.PropertyReferenceExpression, Type: B) (Syntax: 'Property2')
                        Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: F) (Syntax: 'Property2')
                    Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: B, IsInvalid) (Syntax: '1')
                        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'var e2 = new F() { """" };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'var e2 = new F() { """" };')
      Variables: Local_1: F e2
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F, IsInvalid) (Syntax: 'new F() { """" }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: F, IsInvalid) (Syntax: '{ """" }')
              Initializers(1):
                  IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '""""')
                    Children(1):
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: """", IsInvalid) (Syntax: '""""')
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
IObjectCreationExpression (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new List<in ...  y, field }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ x, y, field }')
      Initializers(3):
          ICollectionElementInitializerExpression (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: 'x')
            Arguments(1):
                IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
          ICollectionElementInitializerExpression (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: 'y')
            Arguments(1):
                ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
          ICollectionElementInitializerExpression (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: 'field')
            Arguments(1):
                IFieldReferenceExpression: System.Int32 C.field (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'field')
                  Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'field')
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
IObjectCreationExpression (Constructor: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>) (Syntax: 'new List<Li ... }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>) (Syntax: '{ ... }')
      Initializers(2):
          ICollectionElementInitializerExpression (AddMethod: void System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>.Add(System.Collections.Generic.List<System.Int32> item)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: 'new[] { x, y }.ToList()')
            Arguments(1):
                IInvocationExpression (System.Collections.Generic.List<System.Int32> System.Linq.Enumerable.ToList<System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new[] { x, y }.ToList()')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument) (Syntax: 'new[] { x, y }')
                        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'new[] { x, y }')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                          Operand: IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: 'new[] { x, y }')
                              Dimension Sizes(1):
                                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'new[] { x, y }')
                              Initializer: IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{ x, y }')
                                  Element Values(2):
                                      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
                        InConversion: null
                        OutConversion: null
          ICollectionElementInitializerExpression (AddMethod: void System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>.Add(System.Collections.Generic.List<System.Int32> item)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: 'new List<int> { field }')
            Arguments(1):
                IObjectCreationExpression (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new List<int> { field }')
                  Arguments(0)
                  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ field }')
                      Initializers(1):
                          ICollectionElementInitializerExpression (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: 'field')
                            Arguments(1):
                                IFieldReferenceExpression: System.Int32 C.field (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'field')
                                  Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'field')
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
IObjectCreationExpression (Constructor: Class..ctor()) (OperationKind.ObjectCreationExpression, Type: Class) (Syntax: 'new Class() ... }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: Class) (Syntax: '{ ... }')
      Initializers(4):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'X = x')
            Left: IPropertyReferenceExpression: System.Int32 Class.X { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'X')
            Right: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
          IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'Y = { x, y, 3 }')
            InitializedMember: IPropertyReferenceExpression: System.Collections.Generic.List<System.Int32> Class.Y { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'Y')
                Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'Y')
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ x, y, 3 }')
                Initializers(3):
                    ICollectionElementInitializerExpression (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: 'x')
                      Arguments(1):
                          IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                    ICollectionElementInitializerExpression (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: 'y')
                      Arguments(1):
                          ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
                    ICollectionElementInitializerExpression (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '3')
                      Arguments(1):
                          ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
          IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: 'Z = { { x, y } }')
            InitializedMember: IPropertyReferenceExpression: System.Collections.Generic.Dictionary<System.Int32, System.Int32> Class.Z { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: 'Z')
                Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'Z')
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: '{ { x, y } }')
                Initializers(1):
                    ICollectionElementInitializerExpression (AddMethod: void System.Collections.Generic.Dictionary<System.Int32, System.Int32>.Add(System.Int32 key, System.Int32 value)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '{ x, y }')
                      Arguments(2):
                          IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                          ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
          IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: Class) (Syntax: 'C = { X = field }')
            InitializedMember: IPropertyReferenceExpression: Class Class.C { get; set; } (OperationKind.PropertyReferenceExpression, Type: Class) (Syntax: 'C')
                Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'C')
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: Class) (Syntax: '{ X = field }')
                Initializers(1):
                    ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'X = field')
                      Left: IPropertyReferenceExpression: System.Int32 Class.X { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'X')
                          Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'X')
                      Right: IFieldReferenceExpression: System.Int32 Class.field (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'field')
                          Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'field')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
