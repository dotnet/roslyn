// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to binding object and collection initializer expressions.
    /// </summary>
    public class ObjectAndCollectionInitializerTests : CompilingTestBase
    {
        #region "Functionality tests"

        #region "Object Initializer"

        [Fact]
        public void ObjectInitializerTest_ClassType()
        {
            string source = @"
class MemberInitializerTest
{
    public int x;
    public int y { get; set; }

    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest() { x = 0, y = 0 }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest) (Syntax: 'new MemberI ...  0, y = 0 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest) (Syntax: '{ x = 0, y = 0 }')
      Initializers(2):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'x = 0')
            Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'x')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'y = 0')
            Left: IPropertyReferenceExpression: System.Int32 MemberInitializerTest.y { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'y')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'y')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);

            CompileAndVerify(source, expectedOutput: "");
        }

        [Fact]
        public void ObjectInitializerTest_StructType()
        {
            string source = @"
struct MemberInitializerTest
{
    public int x;
    public int y { get; set; }

    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest() { x = 0, y = 0 }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest) (Syntax: 'new MemberI ...  0, y = 0 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest) (Syntax: '{ x = 0, y = 0 }')
      Initializers(2):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'x = 0')
            Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'x')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'y = 0')
            Left: IPropertyReferenceExpression: System.Int32 MemberInitializerTest.y { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'y')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'y')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);

            CompileAndVerify(source, expectedOutput: "");
        }

        [Fact]
        public void ObjectInitializerTest_TypeParameterType()
        {
            string source = @"
class Base
{
    public Base() { }
    public int x;
    public int y { get; set; }
    public static void Main()
    {
        MemberInitializerTest<Base>.Goo();
    }
}

class MemberInitializerTest<T> where T : Base, new()
{
    public static void Goo()
    {
        var i = /*<bind>*/new T() { x = 0, y = 0 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: T) (Syntax: 'new T() { x = 0, y = 0 }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
            CompileAndVerify(source, expectedOutput: "");
        }

        [Fact]
        public void ObjectInitializerTest_EnumType()
        {
            string source = @"
enum X { x = 0 }

class MemberInitializerTest
{
    public static void Main()
    {
        var i = /*<bind>*/new X() { }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: X..ctor()) (OperationKind.ObjectCreationExpression, Type: X) (Syntax: 'new X() { }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: X) (Syntax: '{ }')
      Initializers(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
            CompileAndVerify(source, expectedOutput: "");
        }

        [Fact]
        public void ObjectInitializerTest_PrimitiveType()
        {
            string source = @"
class MemberInitializerTest
{
    public static void Main()
    {
        var i = /*<bind>*/new int() { }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'new int() { }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Int32) (Syntax: '{ }')
      Initializers(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);

            CompileAndVerify(source, expectedOutput: "");
        }

        [Fact]
        public void ObjectInitializerTest_MemberAccess_DynamicType()
        {
            string source = @"
class MemberInitializerTest
{
    public dynamic X;
    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest { X = 0 }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest) (Syntax: 'new MemberI ... t { X = 0 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest) (Syntax: '{ X = 0 }')
      Initializers(1):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: dynamic) (Syntax: 'X = 0')
            Left: IFieldReferenceExpression: dynamic MemberInitializerTest.X (OperationKind.FieldReferenceExpression, Type: dynamic) (Syntax: 'X')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'X')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: dynamic) (Syntax: '0')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);

            // TODO: This should produce no diagnostics.
            CreateStandardCompilation(source, references: new MetadataReference[] { SystemCoreRef, CSharpRef }).VerifyDiagnostics();
        }

        [Fact]
        public void ObjectInitializerTest_DefAssignment()
        {
            var source = @"
using System.Collections.Generic;


class O<T> where T : new()
{
    public T list = new T();
}

class Test
{
    static int Main(string[] args)
    {
        int a, b, c, d;

        var list = new MyList(a=1){};
        new MyList(b=2){};

        var o = new O<MyList> { list=new MyList(c=3){}};
        new O<MyList> { list=new MyList(d=4){}};
       
        int i = a;
        i = b;
        i = c;
        i = d;

        return 0;
    }

}

class MyList : List<int>
{
    public MyList(){}

    public MyList(int i){}

    public MyList(List<int> list){}    
}
";
            CompileAndVerify(source, expectedOutput: "");
        }

        #endregion

        #region "Collection Initializer"

        [Fact]
        public void CollectionInitializerTest_Empty()
        {
            string source = @"
using System.Collections.Generic;

class MemberInitializerTest
{
    public List<int> x;
    public static void Main()
    /*<bind>*/{
        var i = new List<int>() { };
        var j = new MemberInitializerTest() { x = { } };
        var k = new MemberInitializerTest() { };
    }/*</bind>*/
}
";
string expectedOperationTree = @"
IBlockStatement (3 statements, 3 locals) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Locals: Local_1: System.Collections.Generic.List<System.Int32> i
    Local_2: MemberInitializerTest j
    Local_3: MemberInitializerTest k
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var i = new ... int>() { };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var i = new ... int>() { };')
      Variables: Local_1: System.Collections.Generic.List<System.Int32> i
      Initializer: IObjectCreationExpression (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new List<int>() { }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ }')
              Initializers(0)
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var j = new ...  x = { } };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var j = new ...  x = { } };')
      Variables: Local_1: MemberInitializerTest j
      Initializer: IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest) (Syntax: 'new MemberI ... { x = { } }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest) (Syntax: '{ x = { } }')
              Initializers(1):
                  IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'x = { }')
                    InitializedMember: IFieldReferenceExpression: System.Collections.Generic.List<System.Int32> MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'x')
                        Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'x')
                    Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ }')
                        Initializers(0)
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var k = new ... Test() { };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var k = new ... Test() { };')
      Variables: Local_1: MemberInitializerTest k
      Initializer: IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest) (Syntax: 'new MemberI ... rTest() { }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest) (Syntax: '{ }')
              Initializers(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);

            CompileAndVerify(source, expectedOutput: "");
        }

        [Fact]
        public void CollectionInitializerTest_DynamicType()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public dynamic list = new List<int>();

    public static int Main()
    {
        var t = /*<bind>*/new Test() { list = { 1 } }/*</bind>*/;
        DisplayCollection((List<int>)t.list);
        return 0;
    }

    public static void DisplayCollection<T>(IEnumerable<T> collection)
    {
        foreach (var i in collection)
        {
            Console.WriteLine(i);
        }
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: Test..ctor()) (OperationKind.ObjectCreationExpression, Type: Test) (Syntax: 'new Test()  ... t = { 1 } }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: Test) (Syntax: '{ list = { 1 } }')
      Initializers(1):
          IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: dynamic) (Syntax: 'list = { 1 }')
            InitializedMember: IFieldReferenceExpression: dynamic Test.list (OperationKind.FieldReferenceExpression, Type: dynamic) (Syntax: 'list')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Test) (Syntax: 'list')
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: dynamic) (Syntax: '{ 1 }')
                Initializers(1):
                    ICollectionElementInitializerExpression (IsDynamic: True) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '1')
                      Arguments(1):
                          ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);

            // TODO: This should produce no diagnostics.
            // The 'info' message is ONLY used for IDE (NOT show up in console)
            CompileAndVerify(source, additionalRefs: new MetadataReference[] { SystemCoreRef, CSharpRef }).
                VerifyDiagnostics(
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Collections;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections;"));
        }

        [Fact]
        public void CollectionInitializerTest_ExplicitInterfaceImplementation_IEnumerable()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        B coll = /*<bind>*/new B { 1, 2, 3, 4, 5 }/*</bind>*/;
        coll.Display();
        return 0;
    }
}

class B : IEnumerable
{
    List<object> list = new List<object>();

    public void Add(long i)
    {
        list.Add(i);
    }

    public void Display()
    {
        foreach (var item in list)
        {
            Console.WriteLine(item);
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: B..ctor()) (OperationKind.ObjectCreationExpression, Type: B) (Syntax: 'new B { 1, 2, 3, 4, 5 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: B) (Syntax: '{ 1, 2, 3, 4, 5 }')
      Initializers(5):
          ICollectionElementInitializerExpression (AddMethod: void B.Add(System.Int64 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '1')
            Arguments(1):
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64, Constant: 1) (Syntax: '1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          ICollectionElementInitializerExpression (AddMethod: void B.Add(System.Int64 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '2')
            Arguments(1):
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64, Constant: 2) (Syntax: '2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
          ICollectionElementInitializerExpression (AddMethod: void B.Add(System.Int64 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '3')
            Arguments(1):
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64, Constant: 3) (Syntax: '3')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
          ICollectionElementInitializerExpression (AddMethod: void B.Add(System.Int64 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '4')
            Arguments(1):
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64, Constant: 4) (Syntax: '4')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
          ICollectionElementInitializerExpression (AddMethod: void B.Add(System.Int64 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '5')
            Arguments(1):
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64, Constant: 5) (Syntax: '5')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);

            string expectedOutput = @"1
2
3
4
5";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void CollectionInitializerTest_ExplicitInterfaceImplementation_IEnumerable_Of_T()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        var coll = /*<bind>*/new B<long> { 1, 2, 3, 4, 5 }/*</bind>*/;
        coll.Display();
        return 0;
    }
}

class B<T> : IEnumerable<T>
{
    List<T> list = new List<T>();

    public void Add(T i)
    {
        list.Add(i);
    }

    public void Display()
    {
        foreach (var item in list)
        {
            Console.WriteLine(item);
        }
    }

    System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: B<System.Int64>..ctor()) (OperationKind.ObjectCreationExpression, Type: B<System.Int64>) (Syntax: 'new B<long> ... , 3, 4, 5 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: B<System.Int64>) (Syntax: '{ 1, 2, 3, 4, 5 }')
      Initializers(5):
          ICollectionElementInitializerExpression (AddMethod: void B<System.Int64>.Add(System.Int64 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '1')
            Arguments(1):
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64, Constant: 1) (Syntax: '1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          ICollectionElementInitializerExpression (AddMethod: void B<System.Int64>.Add(System.Int64 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '2')
            Arguments(1):
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64, Constant: 2) (Syntax: '2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
          ICollectionElementInitializerExpression (AddMethod: void B<System.Int64>.Add(System.Int64 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '3')
            Arguments(1):
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64, Constant: 3) (Syntax: '3')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
          ICollectionElementInitializerExpression (AddMethod: void B<System.Int64>.Add(System.Int64 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '4')
            Arguments(1):
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64, Constant: 4) (Syntax: '4')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
          ICollectionElementInitializerExpression (AddMethod: void B<System.Int64>.Add(System.Int64 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '5')
            Arguments(1):
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64, Constant: 5) (Syntax: '5')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);

            string expectedOutput = @"1
2
3
4
5";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void CollectionInitializerTest_ExplicitImplOfAdd_And_ImplicitImplOfAdd()
        {
            // Explicit interface member implementation of Add(T) is ignored and implicit implementation is called if both are defined.

            string source = @"
using System;
using System.Collections;
using System.Collections.Generic;
class MemberInitializerTest
{
    public static int Main()
    {
        var coll = /*<bind>*/new MyList<string> { ""str"" }/*</bind>*/;
        Console.WriteLine(coll.added);
        return 0;
    }
}

class MyList<T> : ICollection<T>
{
    public T added;

    void ICollection<T>.Add(T item)
    {
        added = item;
    }

    public void Add(T item)
    {
        added = item;
    }

    #region Other ICollection<T> Members

    public void Clear()
    {
        throw new System.Exception(""The method or operation is not implemented."");
    }

    public bool Contains(T item)
    {
        throw new System.Exception(""The method or operation is not implemented."");
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        throw new System.Exception(""The method or operation is not implemented."");
    }

    public int Count
    {
        get { throw new System.Exception(""The method or operation is not implemented.""); }
    }

    public bool IsReadOnly
    {
        get { throw new System.Exception(""The method or operation is not implemented.""); }
    }

    public bool Remove(T item)
    {
        throw new System.Exception(""The method or operation is not implemented."");
    }

    #endregion

    #region IEnumerable<T> Members

    public IEnumerator<T> GetEnumerator()
    {
        throw new System.Exception(""The method or operation is not implemented."");
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        throw new System.Exception(""The method or operation is not implemented."");
    }

    #endregion
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MyList<System.String>..ctor()) (OperationKind.ObjectCreationExpression, Type: MyList<System.String>) (Syntax: 'new MyList< ... > { ""str"" }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MyList<System.String>) (Syntax: '{ ""str"" }')
      Initializers(1):
          ICollectionElementInitializerExpression (AddMethod: void MyList<System.String>.Add(System.String item)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '""str""')
            Arguments(1):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""str"") (Syntax: '""str""')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);

            CompileAndVerify(source, expectedOutput: "str");
        }

        [Fact]
        public void CollectionInitializerTest_ExplicitImplOfAdd_NoImplicitImpl()
        {
            // Explicit interface member implementation of Add(T) will cause a compile-time error.

            string source = @"
using System.Collections.Generic;
class MemberInitializerTest
{
    public static int Main()
    {
        var coll = /*<bind>*/new MyList<string> { ""str"" }/*</bind>*/;
        return 0;
    }
}

class MyList<T> : ICollection<T>
{
    public T added;

    void ICollection<T>.Add(T item)
    {
        added = item;
    }

    #region Other ICollection<T> Members

    public void Clear()
    {
        throw new System.Exception(""The method or operation is not implemented."");
    }

    public bool Contains(T item)
    {
        throw new System.Exception(""The method or operation is not implemented."");
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        throw new System.Exception(""The method or operation is not implemented."");
    }

    public int Count
    {
        get { throw new System.Exception(""The method or operation is not implemented.""); }
    }

    public bool IsReadOnly
    {
        get { throw new System.Exception(""The method or operation is not implemented.""); }
    }

    public bool Remove(T item)
    {
        throw new System.Exception(""The method or operation is not implemented."");
    }

    #endregion

    #region IEnumerable<T> Members

    public IEnumerator<T> GetEnumerator()
    {
        throw new System.Exception(""The method or operation is not implemented."");
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        throw new System.Exception(""The method or operation is not implemented."");
    }

    #endregion
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MyList<System.String>..ctor()) (OperationKind.ObjectCreationExpression, Type: MyList<System.String>, IsInvalid) (Syntax: 'new MyList< ... > { ""str"" }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MyList<System.String>, IsInvalid) (Syntax: '{ ""str"" }')
      Initializers(1):
          IInvocationExpression ( ? MyList<System.String>.Add()) (OperationKind.InvocationExpression, Type: ?, IsInvalid) (Syntax: '""str""')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MyList<System.String>) (Syntax: 'MyList<string>')
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: '""str""')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""str"", IsInvalid) (Syntax: '""str""')
                  InConversion: null
                  OutConversion: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1061: 'MyList<string>' does not contain a definition for 'Add' and no extension method 'Add' accepting a first argument of type 'MyList<string>' could be found (are you missing a using directive or an assembly reference?)
                //         var coll = /*<bind>*/new MyList<string> { "str" }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, @"""str""").WithArguments("MyList<string>", "Add").WithLocation(7, 51)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        #endregion

        #endregion

        #region "Error Tests"

        [Fact]
        [WorkItem(629368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629368")]
        public void AddFieldUsedLikeMethod()
        {
            string source = @"
using System;
using System.Collections;
using System.Collections.Generic;

class A : IEnumerable<int>
{
    public Action<string> Add;

    static void Main()
    {
        /*<bind>*/new A { """" }/*</bind>*/;
    }

    public IEnumerator<int> GetEnumerator() { return null; }
    IEnumerator IEnumerable.GetEnumerator() { return null; }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: A..ctor()) (OperationKind.ObjectCreationExpression, Type: A, IsInvalid) (Syntax: 'new A { """" }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: A, IsInvalid) (Syntax: '{ """" }')
      Initializers(1):
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '""""')
            Children(2):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: """", IsInvalid) (Syntax: '""""')
                IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'A')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0118: 'Add' is a field but is used like a method
                //         /*<bind>*/new A { "" }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadSKknown, @"""""").WithArguments("Add", "field", "method").WithLocation(12, 27),
                // CS0649: Field 'A.Add' is never assigned to, and will always have its default value null
                //     public Action<string> Add;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Add").WithArguments("A.Add", "null").WithLocation(8, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        [WorkItem(629368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629368")]
        public void AddPropertyUsedLikeMethod()
        {
            string source = @"
using System;
using System.Collections;
using System.Collections.Generic;

class A : IEnumerable<int>
{
    public Action<string> Add { get; set; }

    static void Main()
    {
        /*<bind>*/new A { """" }/*</bind>*/;
    }

    public IEnumerator<int> GetEnumerator() { return null; }
    IEnumerator IEnumerable.GetEnumerator() { return null; }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: A..ctor()) (OperationKind.ObjectCreationExpression, Type: A, IsInvalid) (Syntax: 'new A { """" }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: A, IsInvalid) (Syntax: '{ """" }')
      Initializers(1):
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '""""')
            Children(2):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: """", IsInvalid) (Syntax: '""""')
                IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'A')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0118: 'Add' is a property but is used like a method
                //         /*<bind>*/new A { "" }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadSKknown, @"""""").WithArguments("Add", "property", "method").WithLocation(12, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS0070ERR_BadEventUsage()
        {
            string source = @"
delegate void D();
struct MemberInitializerTest
{
    public event D z;
}
class X
{
    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest() { z = null }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ...  z = null }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ z = null }')
      Initializers(1):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: D, IsInvalid) (Syntax: 'z = null')
            Left: IEventReferenceExpression: event D MemberInitializerTest.z (OperationKind.EventReferenceExpression, Type: D, IsInvalid) (Syntax: 'z')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'z')
            Right: ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0070: The event 'MemberInitializerTest.z' can only appear on the left hand side of += or -= (except when used from within the type 'MemberInitializerTest')
                //         var i = /*<bind>*/new MemberInitializerTest() { z = null }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadEventUsage, "z").WithArguments("MemberInitializerTest.z", "MemberInitializerTest").WithLocation(11, 57),
                // CS0067: The event 'MemberInitializerTest.z' is never used
                //     public event D z;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "z").WithArguments("MemberInitializerTest.z").WithLocation(5, 20)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS0117ERR_NoSuchMember()
        {
            string source = @"
class MemberInitializerTest
{
    public static void Main()
    {
        var i = /*<bind>*/new int() { x = 0 }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32, IsInvalid) (Syntax: 'new int() { x = 0 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Int32, IsInvalid) (Syntax: '{ x = 0 }')
      Initializers(1):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?, IsInvalid) (Syntax: 'x = 0')
            Left: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'x')
                Children(1):
                    IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'x')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0117: 'int' does not contain a definition for 'x'
                //         var i = /*<bind>*/new int() { x = 0 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMember, "x").WithArguments("int", "x").WithLocation(6, 39)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS0120_ERR_ObjectRequired()
        {
            string source = @"
class MemberInitializerTest
{
    public int x;
    public int y { get; set; }
    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest() { x = 1, y = x }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ...  1, y = x }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = 1, y = x }')
      Initializers(2):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'x = 1')
            Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'x')
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsInvalid) (Syntax: 'y = x')
            Left: IPropertyReferenceExpression: System.Int32 MemberInitializerTest.y { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'y')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'y')
            Right: IFieldReferenceExpression: System.Int32 MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'x')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0120: An object reference is required for the non-static field, method, or property 'MemberInitializerTest.x'
                //         var i = /*<bind>*/new MemberInitializerTest() { x = 1, y = x }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x").WithArguments("MemberInitializerTest.x").WithLocation(8, 68)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS0122_ERR_BadAccess()
        {
            string source = @"
class MemberInitializerTest
{
    protected int x;
    private int y { get; set; }
    internal int z;
}

class Test
{
    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest() { x = 1, y = 2, z = 3 }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ...  2, z = 3 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = 1, y = 2, z = 3 }')
      Initializers(3):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsInvalid) (Syntax: 'x = 1')
            Left: IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32, IsInvalid) (Syntax: 'x')
                Children(1):
                    IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'MemberInitializerTest')
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsInvalid) (Syntax: 'y = 2')
            Left: IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32, IsInvalid) (Syntax: 'y')
                Children(1):
                    IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'MemberInitializerTest')
            Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'z = 3')
            Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest.z (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'z')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'z')
            Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0122: 'MemberInitializerTest.x' is inaccessible due to its protection level
                //         var i = /*<bind>*/new MemberInitializerTest() { x = 1, y = 2, z = 3 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadAccess, "x").WithArguments("MemberInitializerTest.x").WithLocation(13, 57),
                // CS0122: 'MemberInitializerTest.y' is inaccessible due to its protection level
                //         var i = /*<bind>*/new MemberInitializerTest() { x = 1, y = 2, z = 3 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadAccess, "y").WithArguments("MemberInitializerTest.y").WithLocation(13, 64),
                // CS0649: Field 'MemberInitializerTest.x' is never assigned to, and will always have its default value 0
                //     protected int x;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "x").WithArguments("MemberInitializerTest.x", "0").WithLocation(4, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS0144_ERR_NoNewAbstract()
        {
            string source = @"
interface I { }
class MemberInitializerTest
{
    public static void Main()
    {
        var i = /*<bind>*/new I() { }/*</bind>*/; // CS0144
    }
}
";
string expectedOperationTree = @"
IInvalidExpression (OperationKind.InvalidExpression, Type: I, IsInvalid) (Syntax: 'new I() { }')
  Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0144: Cannot create an instance of the abstract class or interface 'I'
                //         var i = /*<bind>*/new I() { }/*</bind>*/; // CS0144
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new I() { }").WithArguments("I").WithLocation(7, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS0154_ERR_PropertyLacksGet()
        {
            string source = @"
class MemberInitializerTest
{
    public int x;
    public int y { get; set; }
}

class Test
{
    public MemberInitializerTest m;
    public MemberInitializerTest Prop { set { m = value; } }

    public static void Main()
    {
        var i = /*<bind>*/new Test() { Prop = { x = 1, y = 2 } }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: Test..ctor()) (OperationKind.ObjectCreationExpression, Type: Test, IsInvalid) (Syntax: 'new Test()  ... , y = 2 } }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: Test, IsInvalid) (Syntax: '{ Prop = {  ... , y = 2 } }')
      Initializers(1):
          IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Prop = { x = 1, y = 2 }')
            InitializedMember: IPropertyReferenceExpression: MemberInitializerTest Test.Prop { set; } (OperationKind.PropertyReferenceExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Prop')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Test, IsInvalid) (Syntax: 'Prop')
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest) (Syntax: '{ x = 1, y = 2 }')
                Initializers(2):
                    ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'x = 1')
                      Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'x')
                          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'x')
                      Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'y = 2')
                      Left: IPropertyReferenceExpression: System.Int32 MemberInitializerTest.y { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'y')
                          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'y')
                      Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0154: The property or indexer 'Test.Prop' cannot be used in this context because it lacks the get accessor
                //         var i = /*<bind>*/new Test() { Prop = { x = 1, y = 2 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "Prop").WithArguments("Test.Prop").WithLocation(15, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS0165_ERR_UseDefViolation()
        {
            string source = @"
class MemberInitializerTest
{
    public int x;
    public static void Main()
    {
        MemberInitializerTest m = /*<bind>*/new MemberInitializerTest() { x = m.x }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... { x = m.x }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = m.x }')
      Initializers(1):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsInvalid) (Syntax: 'x = m.x')
            Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'x')
            Right: IFieldReferenceExpression: System.Int32 MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'm.x')
                Instance Receiver: ILocalReferenceExpression: m (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'm')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0165: Use of unassigned local variable 'm'
                //         MemberInitializerTest m = /*<bind>*/new MemberInitializerTest() { x = m.x }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "m").WithArguments("m").WithLocation(7, 79)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS0191_ERR_AssgReadonly()
        {
            string source = @"
struct MemberInitializerTest
{
    public readonly int x;
    public int y { get { return 0; } }
}

struct Test
{
    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest() { x = 1 }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... ) { x = 1 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = 1 }')
      Initializers(1):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsInvalid) (Syntax: 'x = 1')
            Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'x')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'x')
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0191: A readonly field cannot be assigned to (except in a constructor or a variable initializer)
                //         var i = /*<bind>*/new MemberInitializerTest() { x = 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_AssgReadonly, "x").WithLocation(12, 57)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS0200_ERR_AssgReadonlyProp()
        {
            string source = @"
struct MemberInitializerTest
{
    public readonly int x;
    public int y { get { return 0; } }
}

struct Test
{
    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest() { y = 2 }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... ) { y = 2 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ y = 2 }')
      Initializers(1):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsInvalid) (Syntax: 'y = 2')
            Left: IPropertyReferenceExpression: System.Int32 MemberInitializerTest.y { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'y')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'y')
            Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0200: Property or indexer 'MemberInitializerTest.y' cannot be assigned to -- it is read only
                //         var i = /*<bind>*/new MemberInitializerTest() { y = 2 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "y").WithArguments("MemberInitializerTest.y").WithLocation(12, 57),
                // CS0649: Field 'MemberInitializerTest.x' is never assigned to, and will always have its default value 0
                //     public readonly int x;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "x").WithArguments("MemberInitializerTest.x", "0").WithLocation(4, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS0246_ERR_SingleTypeNameNotFound()
        {
            string source = @"
class MemberInitializerTest
{
    public static void Main()
    {
        var i = /*<bind>*/new X() { x = 0 }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IInvalidExpression (OperationKind.InvalidExpression, Type: X, IsInvalid) (Syntax: 'new X() { x = 0 }')
  Children(1):
      IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: X) (Syntax: '{ x = 0 }')
        Initializers(1):
            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?) (Syntax: 'x = 0')
              Left: IInvalidExpression (OperationKind.InvalidExpression, Type: ?) (Syntax: 'x')
                  Children(1):
                      IOperation:  (OperationKind.None) (Syntax: 'x')
              Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0246: The type or namespace name 'X' could not be found (are you missing a using directive or an assembly reference?)
                //         var i = /*<bind>*/new X() { x = 0 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "X").WithArguments("X").WithLocation(6, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(543936, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543936")]
        [Fact]
        public void CS0246_ERR_SingleTypeNameNotFound_02()
        {
            string source = @"
static class Ext
{
    static int Width(this Goo f) { return 0; }
}

class Goo
{
    void M()
    {
        var x = /*<bind>*/new Bar() { Width = 16 }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IInvalidExpression (OperationKind.InvalidExpression, Type: Bar, IsInvalid) (Syntax: 'new Bar() { Width = 16 }')
  Children(1):
      IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: Bar) (Syntax: '{ Width = 16 }')
        Initializers(1):
            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?) (Syntax: 'Width = 16')
              Left: IInvalidExpression (OperationKind.InvalidExpression, Type: ?) (Syntax: 'Width')
                  Children(1):
                      IOperation:  (OperationKind.None) (Syntax: 'Width')
              Right: ILiteralExpression (Text: 16) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 16) (Syntax: '16')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0246: The type or namespace name 'Bar' could not be found (are you missing a using directive or an assembly reference?)
                //         var x = /*<bind>*/new Bar() { Width = 16 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Bar").WithArguments("Bar").WithLocation(11, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS0304_ERR_NoNewTyvar()
        {
            string source = @"
class MemberInitializerTest<T>
{
    public static void Main()
    {
        var i = /*<bind>*/new T() { x = 0 }/*</bind>*/; // CS0304
    }
}
";
string expectedOperationTree = @"
IInvalidExpression (OperationKind.InvalidExpression, Type: T, IsInvalid) (Syntax: 'new T() { x = 0 }')
  Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0117: 'T' does not contain a definition for 'x'
                //         var i = /*<bind>*/new T() { x = 0 }/*</bind>*/; // CS0304
                Diagnostic(ErrorCode.ERR_NoSuchMember, "x").WithArguments("T", "x").WithLocation(6, 37),
                // CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                //         var i = /*<bind>*/new T() { x = 0 }/*</bind>*/; // CS0304
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new T() { x = 0 }").WithArguments("T").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS0411_ERR_CantInferMethTypeArgs()
        {
            string source = @"
using System.Collections.Generic;
using System.Collections;

class Gen<T> : IEnumerable
{
    public static void Add<U>(T i) { }

    List<object> list = new List<object>();
    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}

class Test
{
    public static void Main()
    {
        var coll = /*<bind>*/new Gen<int> { 1 }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: Gen<System.Int32>..ctor()) (OperationKind.ObjectCreationExpression, Type: Gen<System.Int32>, IsInvalid) (Syntax: 'new Gen<int> { 1 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: Gen<System.Int32>, IsInvalid) (Syntax: '{ 1 }')
      Initializers(1):
          IInvocationExpression ( void Gen<System.Int32>.Add()) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: '1')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Gen<System.Int32>) (Syntax: 'Gen<int>')
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: '1')
                  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                  InConversion: null
                  OutConversion: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0411: The type arguments for method 'Gen<int>.Add<U>(int)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var coll = /*<bind>*/new Gen<int> { 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "1").WithArguments("Gen<int>.Add<U>(int)").WithLocation(21, 45)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS0747_ERR_InvalidInitializerElementInitializer()
        {
            string source = @"
class MemberInitializerTest
{
    public int x, y;
    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest { x = 0, y++ }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ...  = 0, y++ }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = 0, y++ }')
      Initializers(2):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'x = 0')
            Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'x')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          IIncrementExpression (UnaryOperandKind.Invalid) (OperationKind.IncrementExpression, Type: System.Object, IsInvalid) (Syntax: 'y++')
            Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest.y (OperationKind.FieldReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'y')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'y')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0120: An object reference is required for the non-static field, method, or property 'MemberInitializerTest.y'
                //         var i = /*<bind>*/new MemberInitializerTest { x = 0, y++ }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "y").WithArguments("MemberInitializerTest.y").WithLocation(7, 62),
                // CS0747: Invalid initializer member declarator
                //         var i = /*<bind>*/new MemberInitializerTest { x = 0, y++ }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "y++").WithLocation(7, 62)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS0747_ERR_InvalidInitializerElementInitializer_MethodCall()
        {
            string source = @"
class MemberInitializerTest
{
    public int x;
    public MemberInitializerTest Goo() { return new MemberInitializerTest(); }
    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest() { x = 0, Goo() = new MemberInitializerTest() }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... zerTest() }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = 0, Go ... zerTest() }')
      Initializers(2):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'x = 0')
            Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'x')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Goo() = new ... lizerTest()')
            Left: IInvocationExpression ( MemberInitializerTest MemberInitializerTest.Goo()) (OperationKind.InvocationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Goo()')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Goo')
                Arguments(0)
            Right: IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... lizerTest()')
                Arguments(0)
                Initializer: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0120: An object reference is required for the non-static field, method, or property 'MemberInitializerTest.Goo()'
                //         var i = /*<bind>*/new MemberInitializerTest() { x = 0, Goo() = new MemberInitializerTest() }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Goo").WithArguments("MemberInitializerTest.Goo()").WithLocation(8, 64),
                // CS0747: Invalid initializer member declarator
                //         var i = /*<bind>*/new MemberInitializerTest() { x = 0, Goo() = new MemberInitializerTest() }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "Goo() = new MemberInitializerTest()").WithLocation(8, 64)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS0747_ERR_InvalidInitializerElementInitializer_AssignmentExpression()
        {
            string source = @"
using System.Collections.Generic;
class MemberInitializerTest
{
    public int x;
    static MemberInitializerTest Goo() { return new MemberInitializerTest(); }

    public static void Main()
    {
        var i = /*<bind>*/new List<int> { 1, Goo().x = 1 }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Collections.Generic.List<System.Int32>, IsInvalid) (Syntax: 'new List<in ... o().x = 1 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Collections.Generic.List<System.Int32>, IsInvalid) (Syntax: '{ 1, Goo().x = 1 }')
      Initializers(2):
          ICollectionElementInitializerExpression (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '1')
            Arguments(1):
                ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          ICollectionElementInitializerExpression (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void, IsInvalid) (Syntax: 'Goo().x = 1')
            Arguments(1):
                ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsInvalid) (Syntax: 'Goo().x = 1')
                  Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'Goo().x')
                      Instance Receiver: IInvocationExpression (MemberInitializerTest MemberInitializerTest.Goo()) (OperationKind.InvocationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Goo()')
                          Instance Receiver: null
                          Arguments(0)
                  Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0747: Invalid initializer member declarator
                //         var i = /*<bind>*/new List<int> { 1, Goo().x = 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "Goo().x = 1").WithLocation(10, 46)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1912ERR_MemberAlreadyInitialized()
        {
            string source = @"
class MemberInitializerTest
{
    public int x;
    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest() { x = 1, x = 2 }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ...  1, x = 2 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = 1, x = 2 }')
      Initializers(2):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'x = 1')
            Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'x')
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsInvalid) (Syntax: 'x = 2')
            Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'x')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'x')
            Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1912: Duplicate initialization of member 'x'
                //         var i = /*<bind>*/new MemberInitializerTest() { x = 1, x = 2 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "x").WithArguments("x").WithLocation(7, 64)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1913ERR_MemberCannotBeInitialized()
        {
            string source = @"
class MemberInitializerTest
{
    public MemberInitializerTest Goo() { return new MemberInitializerTest(); }
    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest() { Goo = new MemberInitializerTest() }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... zerTest() }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ Goo = new ... zerTest() }')
      Initializers(1):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?, IsInvalid) (Syntax: 'Goo = new M ... lizerTest()')
            Left: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'Goo')
                Children(1):
                    IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'Goo')
            Right: IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest) (Syntax: 'new MemberI ... lizerTest()')
                Arguments(0)
                Initializer: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1913: Member 'Goo' cannot be initialized. It is not a field or property.
                //         var i = /*<bind>*/new MemberInitializerTest() { Goo = new MemberInitializerTest() }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MemberCannotBeInitialized, "Goo").WithArguments("Goo").WithLocation(7, 57)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1914ERR_StaticMemberInObjectInitializer_EnumTypeMember()
        {
            string source = @"
enum X { x = 0 }

class MemberInitializerTest
{
    public static void Main()
    {
        var i = /*<bind>*/new X() { x = 0 }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: X..ctor()) (OperationKind.ObjectCreationExpression, Type: X, IsInvalid) (Syntax: 'new X() { x = 0 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: X, IsInvalid) (Syntax: '{ x = 0 }')
      Initializers(1):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: X, IsInvalid) (Syntax: 'x = 0')
            Left: IFieldReferenceExpression: X.x (OperationKind.FieldReferenceExpression, Type: X, IsInvalid) (Syntax: 'x')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: X, IsInvalid) (Syntax: 'x')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1914: Static field or property 'X.x' cannot be assigned in an object initializer
                //         var i = /*<bind>*/new X() { x = 0 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_StaticMemberInObjectInitializer, "x").WithArguments("X.x").WithLocation(8, 37)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1914ERR_StaticMemberInObjectInitializer()
        {
            string source = @"
class MemberInitializerTest
{
    public static int x;
    public static int Prop { get; set; }

    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest() { x = 1, Prop = 1 }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ...  Prop = 1 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = 1, Prop = 1 }')
      Initializers(2):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsInvalid) (Syntax: 'x = 1')
            Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'x')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'x')
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsInvalid) (Syntax: 'Prop = 1')
            Left: IPropertyReferenceExpression: System.Int32 MemberInitializerTest.Prop { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'Prop')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Prop')
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1914: Static field or property 'MemberInitializerTest.x' cannot be assigned in an object initializer
                //         var i = /*<bind>*/new MemberInitializerTest() { x = 1, Prop = 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_StaticMemberInObjectInitializer, "x").WithArguments("MemberInitializerTest.x").WithLocation(9, 57),
                // CS1914: Static field or property 'MemberInitializerTest.Prop' cannot be assigned in an object initializer
                //         var i = /*<bind>*/new MemberInitializerTest() { x = 1, Prop = 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_StaticMemberInObjectInitializer, "Prop").WithArguments("MemberInitializerTest.Prop").WithLocation(9, 64)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1917ERR_ReadonlyValueTypeInObjectInitializer()
        {
            string source = @"
class MemberInitializerTest
{
    public readonly MemberInitializerTest2 x;

    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest() { x = { y = 1 } }/*</bind>*/;
    }
}

struct MemberInitializerTest2
{
    public int y;
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... { y = 1 } }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = { y = 1 } }')
      Initializers(1):
          IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: MemberInitializerTest2, IsInvalid) (Syntax: 'x = { y = 1 }')
            InitializedMember: IFieldReferenceExpression: MemberInitializerTest2 MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: MemberInitializerTest2, IsInvalid) (Syntax: 'x')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'x')
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest2) (Syntax: '{ y = 1 }')
                Initializers(1):
                    ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'y = 1')
                      Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest2.y (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'y')
                          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest2) (Syntax: 'y')
                      Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1917: Members of readonly field 'MemberInitializerTest.x' of type 'MemberInitializerTest2' cannot be assigned with an object initializer because it is of a value type
                //         var i = /*<bind>*/new MemberInitializerTest() { x = { y = 1 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ReadonlyValueTypeInObjectInitializer, "x").WithArguments("MemberInitializerTest.x", "MemberInitializerTest2").WithLocation(8, 57)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1918ERR_ValueTypePropertyInObjectInitializer()
        {
            string source = @"
class MemberInitializerTest
{
    public int x;
    public MemberInitializerTest2 Prop { get; set; }

    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest() { x = 1, Prop = { x = 1 } }/*</bind>*/;
    }
}

struct MemberInitializerTest2
{
    public int x;
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... { x = 1 } }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = 1, Pr ... { x = 1 } }')
      Initializers(2):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'x = 1')
            Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest.x (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'x')
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: MemberInitializerTest2, IsInvalid) (Syntax: 'Prop = { x = 1 }')
            InitializedMember: IPropertyReferenceExpression: MemberInitializerTest2 MemberInitializerTest.Prop { get; set; } (OperationKind.PropertyReferenceExpression, Type: MemberInitializerTest2, IsInvalid) (Syntax: 'Prop')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Prop')
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest2) (Syntax: '{ x = 1 }')
                Initializers(1):
                    ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'x = 1')
                      Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest2.x (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'x')
                          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest2) (Syntax: 'x')
                      Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1918: Members of property 'MemberInitializerTest.Prop' of type 'MemberInitializerTest2' cannot be assigned with an object initializer because it is of a value type
                //         var i = /*<bind>*/new MemberInitializerTest() { x = 1, Prop = { x = 1 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "Prop").WithArguments("MemberInitializerTest.Prop", "MemberInitializerTest2").WithLocation(9, 64)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1920ERR_EmptyElementInitializer()
        {
            string source = @"
using System.Collections.Generic;

class MemberInitializerTest
{
    public List<int> y;
    public static void Main()
    /*<bind>*/{
        var i = new MemberInitializerTest { y = { } };  // No CS1920
        i = new MemberInitializerTest { y = new List<int> { } };    // No CS1920
        i = new MemberInitializerTest { y = { { } } };  // CS1920
        List<List<int>> collection = new List<List<int>>() { { } }; // CS1920
    }/*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockStatement (4 statements, 2 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: MemberInitializerTest i
    Local_2: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>> collection
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var i = new ...  y = { } };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var i = new ...  y = { } };')
      Variables: Local_1: MemberInitializerTest i
      Initializer: IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest) (Syntax: 'new MemberI ... { y = { } }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest) (Syntax: '{ y = { } }')
              Initializers(1):
                  IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'y = { }')
                    InitializedMember: IFieldReferenceExpression: System.Collections.Generic.List<System.Int32> MemberInitializerTest.y (OperationKind.FieldReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'y')
                        Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'y')
                    Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ }')
                        Initializers(0)
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = new Mem ... int> { } };')
    Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: MemberInitializerTest) (Syntax: 'i = new Mem ... <int> { } }')
        Left: ILocalReferenceExpression: i (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: MemberInitializerTest) (Syntax: 'i')
        Right: IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest) (Syntax: 'new MemberI ... <int> { } }')
            Arguments(0)
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest) (Syntax: '{ y = new L ... <int> { } }')
                Initializers(1):
                    ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'y = new List<int> { }')
                      Left: IFieldReferenceExpression: System.Collections.Generic.List<System.Int32> MemberInitializerTest.y (OperationKind.FieldReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'y')
                          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'y')
                      Right: IObjectCreationExpression (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new List<int> { }')
                          Arguments(0)
                          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ }')
                              Initializers(0)
  IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'i = new Mem ...  { { } } };')
    Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'i = new Mem ... = { { } } }')
        Left: ILocalReferenceExpression: i (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: MemberInitializerTest) (Syntax: 'i')
        Right: IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... = { { } } }')
            Arguments(0)
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ y = { { } } }')
                Initializers(1):
                    IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: System.Collections.Generic.List<System.Int32>, IsInvalid) (Syntax: 'y = { { } }')
                      InitializedMember: IFieldReferenceExpression: System.Collections.Generic.List<System.Int32> MemberInitializerTest.y (OperationKind.FieldReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'y')
                          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest) (Syntax: 'y')
                      Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Collections.Generic.List<System.Int32>, IsInvalid) (Syntax: '{ { } }')
                          Initializers(1):
                              IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '{ }')
                                Children(0)
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'List<List<i ... () { { } };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'List<List<i ... () { { } };')
      Variables: Local_1: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>> collection
      Initializer: IObjectCreationExpression (Constructor: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsInvalid) (Syntax: 'new List<Li ... >() { { } }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsInvalid) (Syntax: '{ { } }')
              Initializers(1):
                  IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '{ }')
                    Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1920: Element initializer cannot be empty
                //         i = new MemberInitializerTest { y = { { } } };  // CS1920
                Diagnostic(ErrorCode.ERR_EmptyElementInitializer, "{ }").WithLocation(11, 47),
                // CS1920: Element initializer cannot be empty
                //         List<List<int>> collection = new List<List<int>>() { { } }; // CS1920
                Diagnostic(ErrorCode.ERR_EmptyElementInitializer, "{ }").WithLocation(12, 62)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1921ERR_InitializerAddHasWrongSignature()
        {
            string source = @"
using System.Collections.Generic;
using System.Collections;

class Test : IEnumerable
{
    public static void Add(int i) { }

    public static void Main()
    {
        var coll = /*<bind>*/new Test() { 1 }/*</bind>*/;
    }

    List<object> list = new List<object>();
    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: Test..ctor()) (OperationKind.ObjectCreationExpression, Type: Test, IsInvalid) (Syntax: 'new Test() { 1 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: Test, IsInvalid) (Syntax: '{ 1 }')
      Initializers(1):
          ICollectionElementInitializerExpression (AddMethod: void Test.Add(System.Int32 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void, IsInvalid) (Syntax: '1')
            Arguments(1):
                ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1921: The best overloaded method match for 'Test.Add(int)' has wrong signature for the initializer element. The initializable Add must be an accessible instance method.
                //         var coll = /*<bind>*/new Test() { 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InitializerAddHasWrongSignature, "1").WithArguments("Test.Add(int)").WithLocation(11, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1922ERR_CollectionInitRequiresIEnumerable()
        {
            string source = @"
class MemberInitializerTest
{
    public static int Main()
    /*<bind>*/{
        B coll = new B { 1 };           // CS1922
        var tc = new A { 1, ""hello"" }; // CS1922
        return 0;
    }/*</bind>*/
}

class B
{
    public B() { }
    public B(int i) { }
}

class A
{
    public int Prop1 { get; set; }
    public string Prop2 { get; set; }
}
";
string expectedOperationTree = @"
IBlockStatement (3 statements, 2 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: B coll
    Local_2: A tc
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'B coll = new B { 1 };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'B coll = new B { 1 };')
      Variables: Local_1: B coll
      Initializer: IObjectCreationExpression (Constructor: B..ctor()) (OperationKind.ObjectCreationExpression, Type: B, IsInvalid) (Syntax: 'new B { 1 }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: B, IsInvalid) (Syntax: '{ 1 }')
              Initializers(1):
                  IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '1')
                    Children(1):
                        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'var tc = ne ...  ""hello"" };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'var tc = ne ...  ""hello"" };')
      Variables: Local_1: A tc
      Initializer: IObjectCreationExpression (Constructor: A..ctor()) (OperationKind.ObjectCreationExpression, Type: A, IsInvalid) (Syntax: 'new A { 1, ""hello"" }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: A, IsInvalid) (Syntax: '{ 1, ""hello"" }')
              Initializers(2):
                  IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '1')
                    Children(1):
                        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                  IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '""hello""')
                    Children(1):
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""hello"", IsInvalid) (Syntax: '""hello""')
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return 0;')
    ReturnedValue: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1922: Cannot initialize type 'B' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         B coll = new B { 1 };           // CS1922
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ 1 }").WithArguments("B").WithLocation(6, 24),
                // CS1922: Cannot initialize type 'A' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         var tc = new A { 1, "hello" }; // CS1922
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, @"{ 1, ""hello"" }").WithArguments("A").WithLocation(7, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1922ERR_CollectionInitRequiresIEnumerable_02()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;
class MemberInitializerTest
{
    public static int Main()
    {
        B coll = /*<bind>*/new B { 1 }/*</bind>*/;
        return 0;
    }
}

class B
{
    List<object> list = new List<object>();

    public void Add(long i)
    {
        list.Add(i);
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: B..ctor()) (OperationKind.ObjectCreationExpression, Type: B, IsInvalid) (Syntax: 'new B { 1 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: B, IsInvalid) (Syntax: '{ 1 }')
      Initializers(1):
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '1')
            Children(1):
                ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1922: Cannot initialize type 'B' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         B coll = /*<bind>*/new B { 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ 1 }").WithArguments("B").WithLocation(8, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1922ERR_CollectionInitRequiresIEnumerable_InvalidInitializer()
        {
            string source = @"
class MemberInitializerTest
{
    public int y;
    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest { y++ }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... est { y++ }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ y++ }')
      Initializers(1):
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'y++')
            Children(1):
                IIncrementExpression (UnaryOperandKind.Invalid) (OperationKind.IncrementExpression, Type: System.Object, IsInvalid) (Syntax: 'y++')
                  Left: IFieldReferenceExpression: System.Int32 MemberInitializerTest.y (OperationKind.FieldReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'y')
                      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'y')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1922: Cannot initialize type 'MemberInitializerTest' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         var i = /*<bind>*/new MemberInitializerTest { y++ }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ y++ }").WithArguments("MemberInitializerTest").WithLocation(7, 53),
                // CS0120: An object reference is required for the non-static field, method, or property 'MemberInitializerTest.y'
                //         var i = /*<bind>*/new MemberInitializerTest { y++ }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "y").WithArguments("MemberInitializerTest.y").WithLocation(7, 55)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1922ERR_CollectionInitRequiresIEnumerable_MethodCall()
        {
            string source = @"
class MemberInitializerTest
{
    public MemberInitializerTest Goo() { return new MemberInitializerTest(); }
    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest() { Goo() = new MemberInitializerTest() }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... zerTest() }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ Goo() = n ... zerTest() }')
      Initializers(1):
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'Goo() = new ... lizerTest()')
            Children(1):
                ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Goo() = new ... lizerTest()')
                  Left: IInvocationExpression ( MemberInitializerTest MemberInitializerTest.Goo()) (OperationKind.InvocationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Goo()')
                      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Goo')
                      Arguments(0)
                  Right: IObjectCreationExpression (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreationExpression, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... lizerTest()')
                      Arguments(0)
                      Initializer: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1922: Cannot initialize type 'MemberInitializerTest' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         var i = /*<bind>*/new MemberInitializerTest() { Goo() = new MemberInitializerTest() }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ Goo() = new MemberInitializerTest() }").WithArguments("MemberInitializerTest").WithLocation(7, 55),
                // CS0747: Invalid initializer member declarator
                //         var i = /*<bind>*/new MemberInitializerTest() { Goo() = new MemberInitializerTest() }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "Goo() = new MemberInitializerTest()").WithLocation(7, 57),
                // CS0120: An object reference is required for the non-static field, method, or property 'MemberInitializerTest.Goo()'
                //         var i = /*<bind>*/new MemberInitializerTest() { Goo() = new MemberInitializerTest() }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Goo").WithArguments("MemberInitializerTest.Goo()").WithLocation(7, 57)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1950ERR_BadArgTypesForCollectionAdd()
        {
            string source = @"
using System.Collections;
class TestClass : CollectionBase
{
    public void Add(int c)
    {
    }
}

class Test
{
    static void Main()
    {
        TestClass t = /*<bind>*/new TestClass { ""hi"" }/*</bind>*/; // CS1950
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: TestClass..ctor()) (OperationKind.ObjectCreationExpression, Type: TestClass, IsInvalid) (Syntax: 'new TestClass { ""hi"" }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: TestClass, IsInvalid) (Syntax: '{ ""hi"" }')
      Initializers(1):
          IInvocationExpression ( void TestClass.Add(System.Int32 c)) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: '""hi""')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'TestClass')
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: '""hi""')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""hi"", IsInvalid) (Syntax: '""hi""')
                  InConversion: null
                  OutConversion: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1950: The best overloaded Add method 'TestClass.Add(int)' for the collection initializer has some invalid arguments
                //         TestClass t = /*<bind>*/new TestClass { "hi" }/*</bind>*/; // CS1950
                Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, @"""hi""").WithArguments("TestClass.Add(int)").WithLocation(14, 49),
                // CS1503: Argument 1: cannot convert from 'string' to 'int'
                //         TestClass t = /*<bind>*/new TestClass { "hi" }/*</bind>*/; // CS1950
                Diagnostic(ErrorCode.ERR_BadArgType, @"""hi""").WithArguments("1", "string", "int").WithLocation(14, 49)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1954ERR_InitializerAddHasParamModifiers()
        {
            string source = @"
using System.Collections.Generic;
using System.Collections;

class Test : IEnumerable
{
    public void Add(ref int i) { }

    public static void Main()
    {
        var coll = /*<bind>*/new Test() { 1 }/*</bind>*/;
    }

    List<object> list = new List<object>();
    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: Test..ctor()) (OperationKind.ObjectCreationExpression, Type: Test, IsInvalid) (Syntax: 'new Test() { 1 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: Test, IsInvalid) (Syntax: '{ 1 }')
      Initializers(1):
          IInvocationExpression ( void Test.Add(ref System.Int32 i)) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: '1')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Test) (Syntax: 'Test')
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: '1')
                  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                  InConversion: null
                  OutConversion: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1954: The best overloaded method match 'Test.Add(ref int)' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
                //         var coll = /*<bind>*/new Test() { 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InitializerAddHasParamModifiers, "1").WithArguments("Test.Add(ref int)").WithLocation(11, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1954ERR_InitializerAddHasParamModifiers02()
        {
            string source = @"
using System.Collections.Generic;
class MyList<T> : IEnumerable<T>
{
    List<T> _list;
    public void Add(ref T item)
    {
        _list.Add(item);
    }

    public System.Collections.Generic.IEnumerator<T> GetEnumerator()
    {
        int index = 0;
        T current = _list[index];
        while (current != null)
        {
            yield return _list[index++];
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

class MyClass
{
    public string tree { get; set; }
}
class Program
{
    static void Main(string[] args)
    {
        MyList<MyClass> myList = /*<bind>*/new MyList<MyClass> { new MyClass { tree = ""maple"" } }/*</bind>*/; // CS1954
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: MyList<MyClass>..ctor()) (OperationKind.ObjectCreationExpression, Type: MyList<MyClass>, IsInvalid) (Syntax: 'new MyList< ... ""maple"" } }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MyList<MyClass>, IsInvalid) (Syntax: '{ new MyCla ... ""maple"" } }')
      Initializers(1):
          IInvocationExpression ( void MyList<MyClass>.Add(ref MyClass item)) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'new MyClass ... = ""maple"" }')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MyList<MyClass>) (Syntax: 'MyList<MyClass>')
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: 'new MyClass ... = ""maple"" }')
                  IObjectCreationExpression (Constructor: MyClass..ctor()) (OperationKind.ObjectCreationExpression, Type: MyClass, IsInvalid) (Syntax: 'new MyClass ... = ""maple"" }')
                    Arguments(0)
                    Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: MyClass, IsInvalid) (Syntax: '{ tree = ""maple"" }')
                        Initializers(1):
                            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.String, IsInvalid) (Syntax: 'tree = ""maple""')
                              Left: IPropertyReferenceExpression: System.String MyClass.tree { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'tree')
                                  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MyClass, IsInvalid) (Syntax: 'tree')
                              Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""maple"", IsInvalid) (Syntax: '""maple""')
                  InConversion: null
                  OutConversion: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1954: The best overloaded method match 'MyList<MyClass>.Add(ref MyClass)' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
                //         MyList<MyClass> myList = /*<bind>*/new MyList<MyClass> { new MyClass { tree = "maple" } }/*</bind>*/; // CS1954
                Diagnostic(ErrorCode.ERR_InitializerAddHasParamModifiers, @"new MyClass { tree = ""maple"" }").WithArguments("MyList<MyClass>.Add(ref MyClass)").WithLocation(35, 66),
                // CS0649: Field 'MyList<T>._list' is never assigned to, and will always have its default value null
                //     List<T> _list;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "_list").WithArguments("MyList<T>._list", "null").WithLocation(5, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1958ERR_ObjectOrCollectionInitializerWithDelegateCreation()
        {
            string source = @"
class MemberInitializerTest
{
    delegate void D<T>();
    public static void GenericMethod<T>() { }
    public static void Main()
    {
        D<int> genD = /*<bind>*/new D<int>(GenericMethod<int>) { }/*</bind>*/; // CS1958
    }
}
";
string expectedOperationTree = @"
IInvalidExpression (OperationKind.InvalidExpression, Type: MemberInitializerTest.D<System.Int32>, IsInvalid) (Syntax: 'new D<int>( ... d<int>) { }')
  Children(1):
      IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'GenericMethod<int>')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1958: Object and collection initializer expressions may not be applied to a delegate creation expression
                //         D<int> genD = /*<bind>*/new D<int>(GenericMethod<int>) { }/*</bind>*/; // CS1958
                Diagnostic(ErrorCode.ERR_ObjectOrCollectionInitializerWithDelegateCreation, "new D<int>(GenericMethod<int>) { }").WithLocation(8, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CollectionInitializerTest_AddMethod_OverloadResolutionFailures()
        {
            string source = @"
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    /*<bind>*/{
        B coll1 = new B { 1 };
        C coll2 = new C { 1 };
        D coll3 = new D { { 1, 2 } };
        E coll4 = new E { 1 };
        return 0;
    }/*</bind>*/
}

class B : IEnumerable
{
    List<object> list = new List<object>();

    public B() { }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}

class C : IEnumerable
{
    List<object> list = new List<object>();

    public C() { }

    public void Add(string i)
    {
        list.Add(i);
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}

class D : IEnumerable
{
    List<object> list = new List<object>();

    public D() { }

    public void Add(int i, float j)
    {
        list.Add(i);
    }

    public void Add(float i, int j)
    {
        list.Add(i);
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}

class E : IEnumerable
{
    List<object> list = new List<object>();

    public E() { }

    private void Add(int i)
    {
        list.Add(i);
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
string expectedOperationTree = @"
IBlockStatement (5 statements, 4 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: B coll1
    Local_2: C coll2
    Local_3: D coll3
    Local_4: E coll4
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'B coll1 = new B { 1 };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'B coll1 = new B { 1 };')
      Variables: Local_1: B coll1
      Initializer: IObjectCreationExpression (Constructor: B..ctor()) (OperationKind.ObjectCreationExpression, Type: B, IsInvalid) (Syntax: 'new B { 1 }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: B, IsInvalid) (Syntax: '{ 1 }')
              Initializers(1):
                  IInvocationExpression ( ? B.Add()) (OperationKind.InvocationExpression, Type: ?, IsInvalid) (Syntax: '1')
                    Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: B) (Syntax: 'B')
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: '1')
                          ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                          InConversion: null
                          OutConversion: null
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C coll2 = new C { 1 };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C coll2 = new C { 1 };')
      Variables: Local_1: C coll2
      Initializer: IObjectCreationExpression (Constructor: C..ctor()) (OperationKind.ObjectCreationExpression, Type: C, IsInvalid) (Syntax: 'new C { 1 }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: C, IsInvalid) (Syntax: '{ 1 }')
              Initializers(1):
                  IInvocationExpression ( void C.Add(System.String i)) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: '1')
                    Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'C')
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: '1')
                          ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                          InConversion: null
                          OutConversion: null
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'D coll3 = n ... { 1, 2 } };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'D coll3 = n ... { 1, 2 } };')
      Variables: Local_1: D coll3
      Initializer: IObjectCreationExpression (Constructor: D..ctor()) (OperationKind.ObjectCreationExpression, Type: D, IsInvalid) (Syntax: 'new D { { 1, 2 } }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: D, IsInvalid) (Syntax: '{ { 1, 2 } }')
              Initializers(1):
                  IInvocationExpression ( void D.Add()) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: '{ 1, 2 }')
                    Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: D) (Syntax: 'D')
                    Arguments(2):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: '1')
                          ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                          InConversion: null
                          OutConversion: null
                        IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: '2')
                          ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
                          InConversion: null
                          OutConversion: null
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'E coll4 = new E { 1 };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'E coll4 = new E { 1 };')
      Variables: Local_1: E coll4
      Initializer: IObjectCreationExpression (Constructor: E..ctor()) (OperationKind.ObjectCreationExpression, Type: E, IsInvalid) (Syntax: 'new E { 1 }')
          Arguments(0)
          Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: E, IsInvalid) (Syntax: '{ 1 }')
              Initializers(1):
                  IInvocationExpression ( void E.Add(System.Int32 i)) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: '1')
                    Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: E) (Syntax: 'E')
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: '1')
                          ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                          InConversion: null
                          OutConversion: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return 0;')
    ReturnedValue: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1061: 'B' does not contain a definition for 'Add' and no extension method 'Add' accepting a first argument of type 'B' could be found (are you missing a using directive or an assembly reference?)
                //         B coll1 = new B { 1 };
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "1").WithArguments("B", "Add").WithLocation(9, 27),
                // CS1950: The best overloaded Add method 'C.Add(string)' for the collection initializer has some invalid arguments
                //         C coll2 = new C { 1 };
                Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "1").WithArguments("C.Add(string)").WithLocation(10, 27),
                // CS1503: Argument 1: cannot convert from 'int' to 'string'
                //         C coll2 = new C { 1 };
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "string").WithLocation(10, 27),
                // CS0121: The call is ambiguous between the following methods or properties: 'D.Add(int, float)' and 'D.Add(float, int)'
                //         D coll3 = new D { { 1, 2 } };
                Diagnostic(ErrorCode.ERR_AmbigCall, "{ 1, 2 }").WithArguments("D.Add(int, float)", "D.Add(float, int)").WithLocation(11, 27),
                // CS0122: 'E.Add(int)' is inaccessible due to its protection level
                //         E coll4 = new E { 1 };
                Diagnostic(ErrorCode.ERR_BadAccess, "1").WithArguments("E.Add(int)").WithLocation(12, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(543933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543933")]
        [Fact]
        public void ObjectInitializerTest_InvalidComplexElementInitializerExpression()
        {
            string source = @"
class Test
{
    public int x;
}
class Program
{
    static void Main()
    {
        var p = /*<bind>*/new Test() { x = 1, { 1 }, { x = 1 } }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: Test..ctor()) (OperationKind.ObjectCreationExpression, Type: Test, IsInvalid) (Syntax: 'new Test()  ... { x = 1 } }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: Test, IsInvalid) (Syntax: '{ x = 1, {  ... { x = 1 } }')
      Initializers(3):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'x = 1')
            Left: IFieldReferenceExpression: System.Int32 Test.x (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Test) (Syntax: 'x')
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '{ 1 }')
            Children(1):
                ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '{ x = 1 }')
            Children(1):
                ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?, IsInvalid) (Syntax: 'x = 1')
                  Left: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'x')
                      Children(0)
                  Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0747: Invalid initializer member declarator
                //         var p = /*<bind>*/new Test() { x = 1, { 1 }, { x = 1 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "{ 1 }").WithLocation(10, 47),
                // CS0103: The name 'x' does not exist in the current context
                //         var p = /*<bind>*/new Test() { x = 1, { 1 }, { x = 1 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(10, 56),
                // CS0747: Invalid initializer member declarator
                //         var p = /*<bind>*/new Test() { x = 1, { 1 }, { x = 1 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "{ x = 1 }").WithLocation(10, 54)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(543933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543933")]
        [Fact]
        public void ObjectInitializerTest_IncompleteComplexElementInitializerExpression()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        var d = /*<bind>*/new Dictionary<object, object>()
        {
            {""s"", 1 },
        var x = 1/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IInvalidExpression (OperationKind.InvalidExpression, Type: Dictionary<System.Object, System.Object>, IsInvalid) (Syntax: 'new Diction ... /*</bind>*/')
  Children(1):
      IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: Dictionary<System.Object, System.Object>, IsInvalid) (Syntax: '{ ... /*</bind>*/')
        Initializers(3):
            IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '{""s"", 1 }')
              Children(2):
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""s"", IsInvalid) (Syntax: '""s""')
                  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
            IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'var')
              Children(0)
            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?, IsInvalid) (Syntax: 'x = 1')
              Left: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'x')
                  Children(1):
                      IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'x')
              Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1003: Syntax error, ',' expected
                //         var x = 1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",", "").WithLocation(9, 13),
                // CS1513: } expected
                //         var x = 1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(9, 29),
                // CS0246: The type or namespace name 'Dictionary<,>' could not be found (are you missing a using directive or an assembly reference?)
                //         var d = /*<bind>*/new Dictionary<object, object>()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Dictionary<object, object>").WithArguments("Dictionary<,>").WithLocation(6, 31),
                // CS0747: Invalid initializer member declarator
                //             {"s", 1 },
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, @"{""s"", 1 }").WithLocation(8, 13),
                // CS0103: The name 'var' does not exist in the current context
                //         var x = 1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(9, 9),
                // CS0747: Invalid initializer member declarator
                //         var x = 1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "var").WithLocation(9, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(543961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543961")]
        [Fact]
        public void CollectionInitializerTest_InvalidComplexElementInitializerSyntax()
        {
            string source = @"
class Test
{
    public static void Main()
    {
        /*<bind>*/new List<int>() { { { 1 } }/*</bind>*/ };
}
}
";
string expectedOperationTree = @"
IInvalidExpression (OperationKind.InvalidExpression, Type: List<System.Int32>, IsInvalid) (Syntax: 'new List<in ... { { { 1 } }')
  Children(1):
      IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: List<System.Int32>, IsInvalid) (Syntax: '{ { { 1 } }')
        Initializers(2):
            IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '{ ')
              Children(0)
            IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '{ 1 }')
              Children(1):
                  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1513: } expected
                //         /*<bind>*/new List<int>() { { { 1 } }/*</bind>*/ };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "{").WithLocation(6, 39),
                // CS1003: Syntax error, ',' expected
                //         /*<bind>*/new List<int>() { { { 1 } }/*</bind>*/ };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",", "{").WithLocation(6, 39),
                // CS1002: ; expected
                //         /*<bind>*/new List<int>() { { { 1 } }/*</bind>*/ };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(6, 58),
                // CS1597: Semicolon after method or accessor block is not valid
                //         /*<bind>*/new List<int>() { { { 1 } }/*</bind>*/ };
                Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";").WithLocation(6, 59),
                // CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1),
                // CS0246: The type or namespace name 'List<>' could not be found (are you missing a using directive or an assembly reference?)
                //         /*<bind>*/new List<int>() { { { 1 } }/*</bind>*/ };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "List<int>").WithArguments("List<>").WithLocation(6, 23),
                // CS1920: Element initializer cannot be empty
                //         /*<bind>*/new List<int>() { { { 1 } }/*</bind>*/ };
                Diagnostic(ErrorCode.ERR_EmptyElementInitializer, "{ ").WithLocation(6, 37)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(544484, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544484")]
        [Fact]
        public void EmptyCollectionInitPredefinedType()
        {
            string source = @"
class Program
{
    const int value = /*<bind>*/new int { }/*</bind>*/;
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32, IsInvalid) (Syntax: 'new int { }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Int32, IsInvalid) (Syntax: '{ }')
      Initializers(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0133: The expression being assigned to 'Program.value' must be constant
                //     const int value = /*<bind>*/new int { }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "new int { }").WithArguments("Program.value").WithLocation(4, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(544349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544349")]
        [Fact]
        public void CollectionInitializerTest_Bug_12635()
        {
            string source = @"
using System.Collections.Generic;

class A
{
    static void Main()
    {
        var x = /*<bind>*/new List<int> { Count = { } }/*</bind>*/;      // CS1918
    }

";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Collections.Generic.List<System.Int32>, IsInvalid) (Syntax: 'new List<in ... unt = { } }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Collections.Generic.List<System.Int32>, IsInvalid) (Syntax: '{ Count = { } }')
      Initializers(1):
          IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: System.Int32, IsInvalid) (Syntax: 'Count = { }')
            InitializedMember: IPropertyReferenceExpression: System.Int32 System.Collections.Generic.List<System.Int32>.Count { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'Count')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: System.Collections.Generic.List<System.Int32>, IsInvalid) (Syntax: 'Count')
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Int32) (Syntax: '{ }')
                Initializers(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1513: } expected
                //     }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(9, 6),
                // CS1918: Members of property 'List<int>.Count' of type 'int' cannot be assigned with an object initializer because it is of a value type
                //         var x = /*<bind>*/new List<int> { Count = { } }/*</bind>*/;      // CS1918
                Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "Count").WithArguments("System.Collections.Generic.List<int>.Count", "int").WithLocation(8, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(544349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544349")]
        [Fact]
        public void CollectionInitializerTest_Bug_12635_02()
        {
            string source = @"
namespace N
{
    struct Struct { public int x; }
    class Class { public int x; }

    class C
    {
        public readonly Struct StructField;
        public Struct StructProp { get { return new Struct(); } }

        public readonly Class ClassField;
        public Class ClassProp { get { return new Class(); } }

        public static void Main()
        {
            var y = /*<bind>*/new C()
            {
                StructField = { },      // CS1917
                StructProp = { },       // CS1918
                ClassField = { },       // No error
                ClassProp = { }         // No error
            }/*</bind>*/;
        }
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: N.C..ctor()) (OperationKind.ObjectCreationExpression, Type: N.C, IsInvalid) (Syntax: 'new C() ... }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: N.C, IsInvalid) (Syntax: '{ ... }')
      Initializers(4):
          IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: N.Struct, IsInvalid) (Syntax: 'StructField = { }')
            InitializedMember: IFieldReferenceExpression: N.Struct N.C.StructField (OperationKind.FieldReferenceExpression, Type: N.Struct, IsInvalid) (Syntax: 'StructField')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: N.C, IsInvalid) (Syntax: 'StructField')
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: N.Struct) (Syntax: '{ }')
                Initializers(0)
          IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: N.Struct, IsInvalid) (Syntax: 'StructProp = { }')
            InitializedMember: IPropertyReferenceExpression: N.Struct N.C.StructProp { get; } (OperationKind.PropertyReferenceExpression, Type: N.Struct, IsInvalid) (Syntax: 'StructProp')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: N.C, IsInvalid) (Syntax: 'StructProp')
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: N.Struct) (Syntax: '{ }')
                Initializers(0)
          IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: N.Class) (Syntax: 'ClassField = { }')
            InitializedMember: IFieldReferenceExpression: N.Class N.C.ClassField (OperationKind.FieldReferenceExpression, Type: N.Class) (Syntax: 'ClassField')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: N.C) (Syntax: 'ClassField')
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: N.Class) (Syntax: '{ }')
                Initializers(0)
          IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: N.Class) (Syntax: 'ClassProp = { }')
            InitializedMember: IPropertyReferenceExpression: N.Class N.C.ClassProp { get; } (OperationKind.PropertyReferenceExpression, Type: N.Class) (Syntax: 'ClassProp')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: N.C) (Syntax: 'ClassProp')
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: N.Class) (Syntax: '{ }')
                Initializers(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1917: Members of readonly field 'C.StructField' of type 'Struct' cannot be assigned with an object initializer because it is of a value type
                //                 StructField = { },      // CS1917
                Diagnostic(ErrorCode.ERR_ReadonlyValueTypeInObjectInitializer, "StructField").WithArguments("N.C.StructField", "N.Struct").WithLocation(19, 17),
                // CS1918: Members of property 'C.StructProp' of type 'Struct' cannot be assigned with an object initializer because it is of a value type
                //                 StructProp = { },       // CS1918
                Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "StructProp").WithArguments("N.C.StructProp", "N.Struct").WithLocation(20, 17),
                // CS0649: Field 'Struct.x' is never assigned to, and will always have its default value 0
                //     struct Struct { public int x; }
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "x").WithArguments("N.Struct.x", "0").WithLocation(4, 32),
                // CS0649: Field 'Class.x' is never assigned to, and will always have its default value 0
                //     class Class { public int x; }
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "x").WithArguments("N.Class.x", "0").WithLocation(5, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(544570, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544570")]
        [Fact]
        public void CollectionInitializerTest_Bug_12977()
        {
            string source = @"
using System.Collections.Generic;
using System.Collections;

static class Test
{
    static void Main()
    {
        var a = /*<bind>*/new A { 5, { 1, 2, { 1, 2 } }/*</bind>*/, 3 };
    }
}

class A : IEnumerable
{
    List<object> list = new List<object>();
    public A() { }
    public A(int i) { }
    public void Add(int i) { list.Add(i); }
    public void Add(int i, int j)
    {
        list.Add(i);
        list.Add(j);
    }
    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: A..ctor()) (OperationKind.ObjectCreationExpression, Type: A, IsInvalid) (Syntax: 'new A { 5,  ...  { 1, 2 } }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: A, IsInvalid) (Syntax: '{ 5, { 1, 2, { 1, 2 } }')
      Initializers(3):
          ICollectionElementInitializerExpression (AddMethod: void A.Add(System.Int32 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '5')
            Arguments(1):
                ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
          IInvocationExpression ( void A.Add()) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: '{ 1, 2, ')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'A')
            Arguments(3):
                IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument) (Syntax: '1')
                  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: null
                  OutConversion: null
                IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument) (Syntax: '2')
                  ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                  InConversion: null
                  OutConversion: null
                IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: '')
                  IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
                    Children(0)
                  InConversion: null
                  OutConversion: null
          ICollectionElementInitializerExpression (AddMethod: void A.Add(System.Int32 i, System.Int32 j)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void, IsInvalid) (Syntax: '{ 1, 2 }')
            Arguments(2):
                ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term '{'
                //         var a = /*<bind>*/new A { 5, { 1, 2, { 1, 2 } }/*</bind>*/, 3 };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(9, 46),
                // CS1513: } expected
                //         var a = /*<bind>*/new A { 5, { 1, 2, { 1, 2 } }/*</bind>*/, 3 };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "{").WithLocation(9, 46),
                // CS1003: Syntax error, ',' expected
                //         var a = /*<bind>*/new A { 5, { 1, 2, { 1, 2 } }/*</bind>*/, 3 };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",", "{").WithLocation(9, 46),
                // CS1001: Identifier expected
                //         var a = /*<bind>*/new A { 5, { 1, 2, { 1, 2 } }/*</bind>*/, 3 };
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "3").WithLocation(9, 69),
                // CS1002: ; expected
                //         var a = /*<bind>*/new A { 5, { 1, 2, { 1, 2 } }/*</bind>*/, 3 };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "3").WithLocation(9, 69),
                // CS1002: ; expected
                //         var a = /*<bind>*/new A { 5, { 1, 2, { 1, 2 } }/*</bind>*/, 3 };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(9, 71),
                // CS1597: Semicolon after method or accessor block is not valid
                //         var a = /*<bind>*/new A { 5, { 1, 2, { 1, 2 } }/*</bind>*/, 3 };
                Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";").WithLocation(9, 72),
                // CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(11, 1)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(545123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545123")]
        [Fact]
        public void VoidElementType_Bug_13402()
        {
            var source = @"
class C
{
    static void Main()
    {
        var array = new[] { Main() };
        foreach (var element in array)
        {
        }
    }
}";
            CreateStandardCompilation(source).VerifyDiagnostics(
                // (6,21): error CS0826: No best type found for implicitly-typed array
                //         var array = new[] { Main() };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { Main() }"));
        }

        [Fact]
        public void AssignmentInOmittedCollectionElementInitializer()
        {
            string source = @"
using System.Collections;

partial class C : IEnumerable
{
    public IEnumerator GetEnumerator() { return null; }

    partial void M(int i);
    partial void Add(int i);

    static void Main()
    {
        int i, j, k;

        var c = /*<bind>*/new C { (i = 1) }/*</bind>*/; // NOTE: assignment is omitted.
        k = i;

        // Normal call, as reference.
        c.M(j = 3);
        k = j;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationExpression (Constructor: C..ctor()) (OperationKind.ObjectCreationExpression, Type: C) (Syntax: 'new C { (i = 1) }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: C) (Syntax: '{ (i = 1) }')
      Initializers(1):
          ICollectionElementInitializerExpression (AddMethod: void C.Add(System.Int32 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '(i = 1)')
            Arguments(1):
                ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 1')
                  Left: ILocalReferenceExpression: i (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                  Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0165: Use of unassigned local variable 'i'
                //         k = i;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "i").WithArguments("i").WithLocation(16, 13),
                // CS0165: Use of unassigned local variable 'j'
                //         k = j;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "j").WithArguments("j").WithLocation(20, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void OmittedCollectionElementInitializerInExpressionTree()
        {
            string source = @"
using System;
using System.Collections;
using System.Linq.Expressions;

partial class C : IEnumerable
{
    public IEnumerator GetEnumerator() { return null; }

    partial void M(int i);
    partial void Add(int i);

    static void Main()
    {
        Expression<Action> a = () => /*<bind>*/new C { 1 }/*</bind>*/; // Omitted element initializer.
        Expression<Action> b = () => new C().M(1); // Normal call, as reference.
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: C..ctor()) (OperationKind.ObjectCreationExpression, Type: C, IsInvalid) (Syntax: 'new C { 1 }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: C, IsInvalid) (Syntax: '{ 1 }')
      Initializers(1):
          ICollectionElementInitializerExpression (AddMethod: void C.Add(System.Int32 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void, IsInvalid) (Syntax: '1')
            Arguments(1):
                ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0765: Partial methods with only a defining declaration or removed conditional methods cannot be used in expression trees
                //         Expression<Action> a = () => /*<bind>*/new C { 1 }/*</bind>*/; // Omitted element initializer.
                Diagnostic(ErrorCode.ERR_PartialMethodInExpressionTree, "1").WithLocation(15, 56),
                // CS0765: Partial methods with only a defining declaration or removed conditional methods cannot be used in expression trees
                //         Expression<Action> b = () => new C().M(1); // Normal call, as reference.
                Diagnostic(ErrorCode.ERR_PartialMethodInExpressionTree, "new C().M(1)").WithLocation(16, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CS1918ERR_ValueTypePropertyInObjectInitializer_NonSpec_Dev10_Error()
        {
            // SPEC:    Nested object initializers cannot be applied to properties with a value type, or to read-only fields with a value type.

            // NOTE:    Dev10 compiler violates the specification here and applies this restriction to nested collection initializers as well.
            // NOTE:    Roslyn goes even further and requires a reference type (rather than "not a value type").  The rationale is that Add methods
            // NOTE:    nearly always manipulate the state of the collection, and those manipulations would be lost if the "this" argument was
            // NOTE:    just a copy.

            string source = @"
using System.Collections;

struct A : IEnumerable
{
    int count;
    int[] values;
    public A(int dummy)
    {
        count = 0;
        values = new int[3];
    }
    public void Add(int i)
    {
        values[count++] = i;
    }
    public int this[int index]
    {
        get { return values[index]; }
    }
    public int Count
    {
        get { return count; }
        set { count = value; }
    }
    public IEnumerator GetEnumerator()
    {
        yield break;
    }
}

class B
{
    public A a = new A();
    public A A
    {
        get { return a; }
        set { a = value; }
    }
}

class Test
{
    static int Main()
    {
        B b = new B { a = { 1, 2, 3 } };
        b = /*<bind>*/new B { A = { 4, 5, 6 } }/*</bind>*/;
        return -1;
    }
}
";
string expectedOperationTree = @"
IObjectCreationExpression (Constructor: B..ctor()) (OperationKind.ObjectCreationExpression, Type: B, IsInvalid) (Syntax: 'new B { A = ... 4, 5, 6 } }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: B, IsInvalid) (Syntax: '{ A = { 4, 5, 6 } }')
      Initializers(1):
          IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: A, IsInvalid) (Syntax: 'A = { 4, 5, 6 }')
            InitializedMember: IPropertyReferenceExpression: A B.A { get; set; } (OperationKind.PropertyReferenceExpression, Type: A, IsInvalid) (Syntax: 'A')
                Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: B, IsInvalid) (Syntax: 'A')
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: A) (Syntax: '{ 4, 5, 6 }')
                Initializers(3):
                    ICollectionElementInitializerExpression (AddMethod: void A.Add(System.Int32 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '4')
                      Arguments(1):
                          ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
                    ICollectionElementInitializerExpression (AddMethod: void A.Add(System.Int32 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '5')
                      Arguments(1):
                          ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
                    ICollectionElementInitializerExpression (AddMethod: void A.Add(System.Int32 i)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '6')
                      Arguments(1):
                          ILiteralExpression (Text: 6) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 6) (Syntax: '6')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1918: Members of property 'B.A' of type 'A' cannot be assigned with an object initializer because it is of a value type
                //         b = /*<bind>*/new B { A = { 4, 5, 6 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "A").WithArguments("B.A", "A").WithLocation(47, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        #endregion

        [WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")]
        [Fact]
        public void GetCollectionInitializerSymbolInfo_01()
        {
            var source = @"
using System;
using System.Collections.Generic;
 
class X : List<int>
{
    void Add(int x) { }
    void Add(string x) {}
 
    static void Main()
    {
        var z = new X { String.Empty, 12 };
    }
}
";
            var compilation = CreateStandardCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var nodes = (from node in tree.GetRoot().DescendantNodes()
                         where node.IsKind(SyntaxKind.CollectionInitializerExpression)
                         select (InitializerExpressionSyntax)node).Single().Expressions;

            SymbolInfo symbolInfo;

            symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(nodes[0]);

            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal("void X.Add(System.String x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);

            symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(nodes[1]);

            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal("void X.Add(System.Int32 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
        }

        [WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")]
        [Fact]
        public void GetCollectionInitializerSymbolInfo_02()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Base : IEnumerable<int>
{} 

class X : Base
{
    void Add(X x) { }
    void Add(List<byte> x) {}
 
    static void Main()
    {
        var z = new X { String.Empty };
    }
}
";
            var compilation = CreateStandardCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var nodes = (from node in tree.GetRoot().DescendantNodes()
                         where node.IsKind(SyntaxKind.CollectionInitializerExpression)
                         select (InitializerExpressionSyntax)node).Single().Expressions;

            SymbolInfo symbolInfo;

            symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(nodes[0]);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            Assert.Equal(new[] {"void X.Add(System.Collections.Generic.List<System.Byte> x)",
                          "void X.Add(X x)"},
                         symbolInfo.CandidateSymbols.Select(s => s.ToTestDisplayString()).Order().ToArray());
        }

        [WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")]
        [Fact]
        public void GetCollectionInitializerSymbolInfo_03()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Base : IEnumerable<int>
{} 

class X : Base
{
    protected void Add(string x) { }
}

class Y
{
    static void Main()
    {
        var z = new X { String.Empty };
    }
}
";
            var compilation = CreateStandardCompilation(source);

            compilation.VerifyDiagnostics(
    // (5,14): error CS0535: 'Base' does not implement interface member 'IEnumerable<int>.GetEnumerator()'
    // class Base : IEnumerable<int>
    Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IEnumerable<int>").WithArguments("Base", "System.Collections.Generic.IEnumerable<int>.GetEnumerator()").WithLocation(5, 14),
    // (5,14): error CS0535: 'Base' does not implement interface member 'IEnumerable.GetEnumerator()'
    // class Base : IEnumerable<int>
    Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IEnumerable<int>").WithArguments("Base", "System.Collections.IEnumerable.GetEnumerator()").WithLocation(5, 14),
    // (17,32): error CS0122: 'X.Add(string)' is inaccessible due to its protection level
    //         var z = new X { String.Empty };
    Diagnostic(ErrorCode.ERR_BadAccess, "Empty").WithArguments("X.Add(string)").WithLocation(17, 32)
                );

            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var nodes = (from node in tree.GetRoot().DescendantNodes()
                         where node.IsKind(SyntaxKind.CollectionInitializerExpression)
                         select (InitializerExpressionSyntax)node).Single().Expressions;

            SymbolInfo symbolInfo;

            symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(nodes[0]);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, symbolInfo.CandidateReason);
            Assert.Equal("void X.Add(System.String x)", symbolInfo.CandidateSymbols.Single().ToTestDisplayString());
        }

        [WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")]
        [Fact]
        public void GetCollectionInitializerSymbolInfo_04()
        {
            var source = @"
using System;
using System.Collections.Generic;
 
class X : List<int>
{
    void Add(string x, int y) {}
 
    static void Main()
    {
        var z = new X { {String.Empty, 12} };
    }
}
";
            var compilation = CreateStandardCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var nodes = (from node in tree.GetRoot().DescendantNodes()
                         where node.IsKind(SyntaxKind.CollectionInitializerExpression)
                         select (InitializerExpressionSyntax)node).Single().Expressions;

            SymbolInfo symbolInfo;

            symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(nodes[0]);

            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal("void X.Add(System.String x, System.Int32 y)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
        }

        [WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")]
        [Fact]
        public void GetCollectionInitializerSymbolInfo_05()
        {
            var source = @"
using System;
using System.Collections.Generic;
 
class X : List<int>
{
    void Add(string x, int y) {}
 
    static void Main()
    {
        var z = new X { {String.Empty, 12} };
    }
}
";
            var compilation = CreateStandardCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var nodes = (from node in tree.GetRoot().DescendantNodes()
                         where node.IsKind(SyntaxKind.CollectionInitializerExpression)
                         select (InitializerExpressionSyntax)node).Single().Expressions;

            SymbolInfo symbolInfo;

            for (int i = 0; i < 2; i++)
            {
                symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(((InitializerExpressionSyntax)nodes[0]).Expressions[i]);

                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            }
        }

        [WorkItem(1084686, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084686"), WorkItem(390, "CodePlex")]
        [Fact]
        public void GetCollectionInitializerSymbolInfo_CollectionInitializerWithinObjectInitializer_01()
        {
            var source = @"
using System;
using System.Collections.Generic;
class Test 
{
    public List<string> List { get; set; }
    
    static void Main() 
    {
        var x = new Test 
                    { 
                        List = { ""Hello"", ""World"" },
                    };
    };
}
";
            var compilation = CreateStandardCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var root = tree.GetRoot();
            var objectCreation = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
            var listAssignment = (AssignmentExpressionSyntax)objectCreation.Initializer.Expressions[0];

            Assert.Equal(SyntaxKind.SimpleAssignmentExpression, listAssignment.Kind());
            Assert.Equal("List", listAssignment.Left.ToString());

            var listInitializer = (InitializerExpressionSyntax)listAssignment.Right;

            SymbolInfo symbolInfo;

            foreach (var expression in listInitializer.Expressions)
            {
                symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(expression);
                Assert.Equal("void System.Collections.Generic.List<System.String>.Add(System.String item)", symbolInfo.Symbol.ToTestDisplayString());
            }
        }

        [WorkItem(1084686, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084686"), WorkItem(390, "CodePlex")]
        [Fact]
        public void GetCollectionInitializerSymbolInfo_CollectionInitializerWithinObjectInitializer_02()
        {
            var source = @"
using System;
using System.Collections.Generic;
class Test 
{
    public List<string> List { get; set; }
    
    static void Main() 
    {
        var x = new Test2 
                    { 
                        P = { 
                                List = { ""Hello"", ""World"" },
                            }
                    };
    };
}

class Test2 
{
    public Test P { get; set; }
}
";
            var compilation = CreateStandardCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var root = tree.GetRoot();
            var objectCreation = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
            var listAssignment = (AssignmentExpressionSyntax)((InitializerExpressionSyntax)((AssignmentExpressionSyntax)objectCreation.Initializer.Expressions[0]).Right).Expressions[0];

            Assert.Equal(SyntaxKind.SimpleAssignmentExpression, listAssignment.Kind());
            Assert.Equal("List", listAssignment.Left.ToString());

            var listInitializer = (InitializerExpressionSyntax)listAssignment.Right;

            SymbolInfo symbolInfo;

            foreach (var expression in listInitializer.Expressions)
            {
                symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(expression);
                Assert.Equal("void System.Collections.Generic.List<System.String>.Add(System.String item)", symbolInfo.Symbol.ToTestDisplayString());
            }
        }

        [WorkItem(1084686, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084686"), WorkItem(390, "CodePlex")]
        [Fact]
        public void GetCollectionInitializerSymbolInfo_CollectionInitializerWithinObjectInitializer_03()
        {
            var source = @"
using System;
using System.Collections.Generic;

class C : System.Collections.Generic.List<C>
{
    class D
    {
        public C[] arr = new C[1] { new C() };
    }

    static void Main()
    {
        var d = new D { arr = { [0] = { null } } };
    }
}
";
            var compilation = CreateStandardCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var root = tree.GetRoot();
            var objectCreation = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Last();
            var listAssignment = (AssignmentExpressionSyntax)((InitializerExpressionSyntax)((AssignmentExpressionSyntax)objectCreation.Initializer.Expressions[0]).Right).Expressions[0];

            Assert.Equal(SyntaxKind.SimpleAssignmentExpression, listAssignment.Kind());
            Assert.Equal("[0]", listAssignment.Left.ToString());

            var listInitializer = (InitializerExpressionSyntax)listAssignment.Right;

            SymbolInfo symbolInfo;

            foreach (var expression in listInitializer.Expressions)
            {
                symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(expression);
                Assert.Equal("void System.Collections.Generic.List<C>.Add(C item)", symbolInfo.Symbol.ToTestDisplayString());
            }
        }

        [Fact, WorkItem(1073330, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1073330")]
        public void NestedIndexerInitializerArray()
        {
            var source = @"
class C
{
    int[] a;

    static void Main()
    {
        var a = new C { a = { [0] = 1, [1] = 2 } };
    }
}
";

            CreateStandardCompilation(source).VerifyDiagnostics(
                // (4,11): warning CS0414: The field 'C.a' is assigned but its value is never used
                //     int[] a;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "a").WithArguments("C.a").WithLocation(4, 11));
        }

        [Fact, WorkItem(1073330, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1073330")]
        public void NestedIndexerInitializerMDArray()
        {
            var source = @"
class C
{
    int[,] a;

    static void Main()
    {
        var a = new C { a = { [0, 0] = 1, [0, 1] = 2, [1, 0] = 3, [1, 1] = 4} };
    }
}
";

            CreateStandardCompilation(source).VerifyDiagnostics(
                // (4,12): warning CS0414: The field 'C.a' is assigned but its value is never used
                //     int[,] a;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "a").WithArguments("C.a").WithLocation(4, 12));
        }

        [Fact, WorkItem(1073330, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1073330")]
        public void NestedIndexerInitializerArraySemanticInfo()
        {
            var source = @"
class C
{
    int[] a;

    static void Main()
    {
        var a = new C { a = { [0] = 1, [1] = 2 } };
    }
}
";

            var compilation = CreateStandardCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var nodes = tree.GetRoot().DescendantNodes().OfType<InitializerExpressionSyntax>().Skip(1).Single().Expressions;

            SymbolInfo symbolInfo;

            for (int i = 0; i < 2; i++)
            {
                symbolInfo = semanticModel.GetSymbolInfo(((AssignmentExpressionSyntax)nodes[i]).Left);

                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            }
        }

        [Fact, WorkItem(2046, "https://github.com/dotnet/roslyn/issues/2046")]
        public void ObjectInitializerTest_DynamicPassedToConstructor()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        DoesNotWork();
    }

    public static void DoesNotWork()
    {
        var cc = new Cc(1, (dynamic)new object())
        {
            C = ""Initialized""
        };
        Console.WriteLine(cc.C ?? ""Uninitialized !!!"");
    }
}

public class Cc{

    public int A { get; set; }
    public dynamic B { get; set; }

    public string C { get; set; }

    public Cc(int a, dynamic b)
    {
        A = a;
        B = b;
    }
}
";
            CompileAndVerify(source, new[] { CSharpRef, SystemCoreRef }, expectedOutput: "Initialized");
        }

        [WorkItem(12983, "https://github.com/dotnet/roslyn/issues/12983")]
        [Fact]
        public void GetCollectionInitializerSymbolInfo_06()
        {
            var source = @"
using System;
using System.Collections.Generic;
 
class X
{
    public static void Main()
    {
        var list1 = new List<string>;
        var list2 = new List<string>();

        var list3 = new List<string> { Count = 3 };
        var list4 = new List<string>() { Count = 3 };

        var list5 = new List<string> { 1, 2, 3 };
        var list6 = new List<string>() { 1, 2, 3 };
    }
}
";
            var compilation = CreateStandardCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var nodes = tree.GetRoot().DescendantNodes().OfType<GenericNameSyntax>().ToArray();
            Assert.Equal(6, nodes.Length);

            foreach (var name in nodes)
            {
                Assert.Equal("List<string>", name.ToString());
                Assert.Equal("System.Collections.Generic.List<System.String>", semanticModel.GetSymbolInfo(name).Symbol.ToTestDisplayString());
                Assert.Null(semanticModel.GetTypeInfo(name).Type);
            }
        }
    }
}
