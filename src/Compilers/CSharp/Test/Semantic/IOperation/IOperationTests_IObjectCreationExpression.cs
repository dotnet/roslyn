﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
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
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var x2 = ne ... ield = 2 };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var x2 = ne ... ield = 2 };')
      Variables: Local_1: F x2
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F) (Syntax: 'new F() { Field = 2 }')
          Initializers(1): IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32) (Syntax: 'Field = 2')
              Left: IOperation:  (OperationKind.None) (Syntax: 'Field')
              Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var x3 = ne ... ty1 = """" };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var x3 = ne ... ty1 = """" };')
      Variables: Local_1: F x3
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F) (Syntax: 'new F() { P ... rty1 = """" }')
          Initializers(1): IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.String) (Syntax: 'Property1 = """"')
              Left: IOperation:  (OperationKind.None) (Syntax: 'Property1')
              Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: """") (Syntax: '""""')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var x4 = ne ... ield = 2 };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var x4 = ne ... ield = 2 };')
      Variables: Local_1: F x4
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F) (Syntax: 'new F() { P ... Field = 2 }')
          Initializers(2): IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.String) (Syntax: 'Property1 = """"')
              Left: IOperation:  (OperationKind.None) (Syntax: 'Property1')
              Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: """") (Syntax: '""""')
            IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32) (Syntax: 'Field = 2')
              Left: IOperation:  (OperationKind.None) (Syntax: 'Field')
              Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var x5 = ne ... = true } };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var x5 = ne ... = true } };')
      Variables: Local_1: F x5
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F) (Syntax: 'new F() { P ...  = true } }')
          Initializers(1): IAssignmentExpression (OperationKind.AssignmentExpression, Type: B) (Syntax: 'Property2 = ... ld = true }')
              Left: IOperation:  (OperationKind.None) (Syntax: 'Property2')
              Right: IObjectCreationExpression (Constructor: B..ctor()) (OperationKind.ObjectCreationExpression, Type: B) (Syntax: 'new B { Field = true }')
                  Initializers(1): IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Boolean) (Syntax: 'Field = true')
                      Left: IOperation:  (OperationKind.None) (Syntax: 'Field')
                      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'var e1 = ne ... rty2 = 1 };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'var e1 = ne ... rty2 = 1 };')
      Variables: Local_1: F e1
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F, IsInvalid) (Syntax: 'new F() { P ... erty2 = 1 }')
          Initializers(1): IAssignmentExpression (OperationKind.AssignmentExpression, Type: B, IsInvalid) (Syntax: 'Property2 = 1')
              Left: IOperation:  (OperationKind.None) (Syntax: 'Property2')
              Right: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: B, IsInvalid) (Syntax: '1')
                  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'var e2 = new F() { """" };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'var e2 = new F() { """" };')
      Variables: Local_1: F e2
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F, IsInvalid) (Syntax: 'new F() { """" }')
          Initializers(1): IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '""""')
              Children(1): ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: """") (Syntax: '""""')
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
  Initializers(3): IOperation:  (OperationKind.None) (Syntax: 'x')
      Children(1): IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
    IOperation:  (OperationKind.None) (Syntax: 'y')
      Children(1): ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
    IOperation:  (OperationKind.None) (Syntax: 'field')
      Children(1): IFieldReferenceExpression: System.Int32 C.field (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'field')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'field')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0649: Field 'C.field' is never assigned to, and will always have its default value 0
                // 	private readonly int field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("C.field", "0").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
  Initializers(2): IOperation:  (OperationKind.None) (Syntax: 'new[] { x, y }.ToList()')
      Children(1): IInvocationExpression (static System.Collections.Generic.List<System.Int32> System.Linq.Enumerable.ToList<System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new[] { x, y }.ToList()')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument) (Syntax: 'new[] { x, y }')
              IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'new[] { x, y }')
                IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: 'new[] { x, y }')
                  Dimension Sizes(1): ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'new[] { x, y }')
                  Initializer: IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{ x, y }')
                      Element Values(2): IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                        ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
    IOperation:  (OperationKind.None) (Syntax: 'new List<int> { field }')
      Children(1): IObjectCreationExpression (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new List<int> { field }')
          Initializers(1): IOperation:  (OperationKind.None) (Syntax: 'field')
              Children(1): IFieldReferenceExpression: System.Int32 C.field (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'field')
                  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'field')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
  Initializers(4): IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32) (Syntax: 'X = x')
      Left: IOperation:  (OperationKind.None) (Syntax: 'X')
      Right: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
    IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'Y = { x, y, 3 }')
      Left: IOperation:  (OperationKind.None) (Syntax: 'Y')
      Right: IOperation:  (OperationKind.None) (Syntax: '{ x, y, 3 }')
          Children(3): IOperation:  (OperationKind.None) (Syntax: 'x')
              Children(1): IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
            IOperation:  (OperationKind.None) (Syntax: 'y')
              Children(1): ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
            IOperation:  (OperationKind.None) (Syntax: '3')
              Children(1): ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
    IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: 'Z = { { x, y } }')
      Left: IOperation:  (OperationKind.None) (Syntax: 'Z')
      Right: IOperation:  (OperationKind.None) (Syntax: '{ { x, y } }')
          Children(1): IOperation:  (OperationKind.None) (Syntax: '{ x, y }')
              Children(2): IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
    IAssignmentExpression (OperationKind.AssignmentExpression, Type: Class) (Syntax: 'C = { X = field }')
      Left: IOperation:  (OperationKind.None) (Syntax: 'C')
      Right: IOperation:  (OperationKind.None) (Syntax: '{ X = field }')
          Children(1): IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32) (Syntax: 'X = field')
              Left: IOperation:  (OperationKind.None) (Syntax: 'X')
              Right: IFieldReferenceExpression: System.Int32 Class.field (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'field')
                  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'field')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
