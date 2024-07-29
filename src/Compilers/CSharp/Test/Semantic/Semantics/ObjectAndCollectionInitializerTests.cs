// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest) (Syntax: 'new MemberI ...  0, y = 0 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest) (Syntax: '{ x = 0, y = 0 }')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 0')
            Left: 
              IFieldReferenceOperation: System.Int32 MemberInitializerTest.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'y = 0')
            Left: 
              IPropertyReferenceOperation: System.Int32 MemberInitializerTest.y { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'y')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'y')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);

            CompileAndVerify(source, expectedOutput: "");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest) (Syntax: 'new MemberI ...  0, y = 0 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest) (Syntax: '{ x = 0, y = 0 }')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 0')
            Left: 
              IFieldReferenceOperation: System.Int32 MemberInitializerTest.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'y = 0')
            Left: 
              IPropertyReferenceOperation: System.Int32 MemberInitializerTest.y { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'y')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'y')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);

            CompileAndVerify(source, expectedOutput: "");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'new T() { x = 0, y = 0 }')
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: T) (Syntax: '{ x = 0, y = 0 }')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 0')
            Left: 
              IFieldReferenceOperation: System.Int32 Base.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsImplicit) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'y = 0')
            Left: 
              IPropertyReferenceOperation: System.Int32 Base.y { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'y')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsImplicit) (Syntax: 'y')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
            CompileAndVerify(source, expectedOutput: "");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: X..ctor()) (OperationKind.ObjectCreation, Type: X) (Syntax: 'new X() { }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: X) (Syntax: '{ }')
      Initializers(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
            CompileAndVerify(source, expectedOutput: "");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32) (Syntax: 'new int() { }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Int32) (Syntax: '{ }')
      Initializers(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);

            CompileAndVerify(source, expectedOutput: "");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest) (Syntax: 'new MemberI ... t { X = 0 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest) (Syntax: '{ X = 0 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'X = 0')
            Left: 
              IFieldReferenceOperation: dynamic MemberInitializerTest.X (OperationKind.FieldReference, Type: dynamic) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'X')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: dynamic, IsImplicit) (Syntax: '0')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);

            // TODO: This should produce no diagnostics.
            CreateCompilation(source, references: new MetadataReference[] { CSharpRef }).VerifyDiagnostics();
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IBlockOperation (3 statements, 3 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Locals: Local_1: System.Collections.Generic.List<System.Int32> i
    Local_2: MemberInitializerTest j
    Local_3: MemberInitializerTest k
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var i = new ... int>() { };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var i = new ... <int>() { }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Collections.Generic.List<System.Int32> i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = new List<int>() { }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new List<int>() { }')
                IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new List<int>() { }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ }')
                      Initializers(0)
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var j = new ...  x = { } };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var j = new ... { x = { } }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: MemberInitializerTest j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = new Mem ... { x = { } }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new Membe ... { x = { } }')
                IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest) (Syntax: 'new MemberI ... { x = { } }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest) (Syntax: '{ x = { } }')
                      Initializers(1):
                          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'x = { }')
                            InitializedMember: 
                              IFieldReferenceOperation: System.Collections.Generic.List<System.Int32> MemberInitializerTest.x (OperationKind.FieldReference, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'x')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'x')
                            Initializer: 
                              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ }')
                                Initializers(0)
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var k = new ... Test() { };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var k = new ... rTest() { }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: MemberInitializerTest k) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'k = new Mem ... rTest() { }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new Membe ... rTest() { }')
                IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest) (Syntax: 'new MemberI ... rTest() { }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest) (Syntax: '{ }')
                      Initializers(0)
      Initializer: 
        null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);

            CompileAndVerify(source, expectedOutput: "");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void CollectionInitializerTest_DynamicType()
        {
            string source = @"
using System;
using System.Collections.Generic;

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
IObjectCreationOperation (Constructor: Test..ctor()) (OperationKind.ObjectCreation, Type: Test) (Syntax: 'new Test()  ... t = { 1 } }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Test) (Syntax: '{ list = { 1 } }')
      Initializers(1):
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: dynamic) (Syntax: 'list = { 1 }')
            InitializedMember: 
              IFieldReferenceOperation: dynamic Test.list (OperationKind.FieldReference, Type: dynamic) (Syntax: 'list')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Test, IsImplicit) (Syntax: 'list')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: dynamic) (Syntax: '{ 1 }')
                Initializers(1):
                    IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Void, IsImplicit) (Syntax: '1')
                      Expression: 
                        IDynamicMemberReferenceOperation (Member Name: ""Add"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: null, IsImplicit) (Syntax: 'list')
                          Type Arguments(0)
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: dynamic, IsImplicit) (Syntax: 'list')
                      Arguments(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      ArgumentNames(0)
                      ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);

            CompileAndVerify(source, references: new MetadataReference[] { CSharpRef }).
                VerifyDiagnostics();
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: B..ctor()) (OperationKind.ObjectCreation, Type: B) (Syntax: 'new B { 1, 2, 3, 4, 5 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: B) (Syntax: '{ 1, 2, 3, 4, 5 }')
      Initializers(5):
          IInvocationOperation ( void B.Add(System.Int64 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'B')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 1, IsImplicit) (Syntax: '1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( void B.Add(System.Int64 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'B')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 2, IsImplicit) (Syntax: '2')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( void B.Add(System.Int64 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'B')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 3, IsImplicit) (Syntax: '3')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( void B.Add(System.Int64 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '4')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'B')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '4')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 4, IsImplicit) (Syntax: '4')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( void B.Add(System.Int64 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '5')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'B')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '5')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 5, IsImplicit) (Syntax: '5')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: B<System.Int64>..ctor()) (OperationKind.ObjectCreation, Type: B<System.Int64>) (Syntax: 'new B<long> ... , 3, 4, 5 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: B<System.Int64>) (Syntax: '{ 1, 2, 3, 4, 5 }')
      Initializers(5):
          IInvocationOperation ( void B<System.Int64>.Add(System.Int64 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: B<System.Int64>, IsImplicit) (Syntax: 'B<long>')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 1, IsImplicit) (Syntax: '1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( void B<System.Int64>.Add(System.Int64 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: B<System.Int64>, IsImplicit) (Syntax: 'B<long>')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 2, IsImplicit) (Syntax: '2')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( void B<System.Int64>.Add(System.Int64 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: B<System.Int64>, IsImplicit) (Syntax: 'B<long>')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 3, IsImplicit) (Syntax: '3')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( void B<System.Int64>.Add(System.Int64 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '4')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: B<System.Int64>, IsImplicit) (Syntax: 'B<long>')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '4')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 4, IsImplicit) (Syntax: '4')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( void B<System.Int64>.Add(System.Int64 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '5')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: B<System.Int64>, IsImplicit) (Syntax: 'B<long>')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '5')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 5, IsImplicit) (Syntax: '5')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MyList<System.String>..ctor()) (OperationKind.ObjectCreation, Type: MyList<System.String>) (Syntax: 'new MyList< ... > { ""str"" }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MyList<System.String>) (Syntax: '{ ""str"" }')
      Initializers(1):
          IInvocationOperation ( void MyList<System.String>.Add(System.String item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '""str""')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MyList<System.String>, IsImplicit) (Syntax: 'MyList<string>')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '""str""')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""str"") (Syntax: '""str""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);

            CompileAndVerify(source, expectedOutput: "str");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MyList<System.String>..ctor()) (OperationKind.ObjectCreation, Type: MyList<System.String>, IsInvalid) (Syntax: 'new MyList< ... > { ""str"" }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MyList<System.String>, IsInvalid) (Syntax: '{ ""str"" }')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '""str""')
            Children(1):
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""str"", IsInvalid) (Syntax: '""str""')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: A..ctor()) (OperationKind.ObjectCreation, Type: A, IsInvalid) (Syntax: 'new A { """" }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: A, IsInvalid) (Syntax: '{ """" }')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '""""')
            Children(2):
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """", IsInvalid) (Syntax: '""""')
                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: 'A')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: A..ctor()) (OperationKind.ObjectCreation, Type: A, IsInvalid) (Syntax: 'new A { """" }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: A, IsInvalid) (Syntax: '{ """" }')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '""""')
            Children(2):
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """", IsInvalid) (Syntax: '""""')
                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: 'A')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0118: 'Add' is a property but is used like a method
                //         /*<bind>*/new A { "" }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadSKknown, @"""""").WithArguments("Add", "property", "method").WithLocation(12, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ...  z = null }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ z = null }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: D, IsInvalid) (Syntax: 'z = null')
            Left: 
              IEventReferenceOperation: event D MemberInitializerTest.z (OperationKind.EventReference, Type: D, IsInvalid) (Syntax: 'z')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsInvalid, IsImplicit) (Syntax: 'z')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32, IsInvalid) (Syntax: 'new int() { x = 0 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Int32, IsInvalid) (Syntax: '{ x = 0 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'x = 0')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'x')
                Children(1):
                    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'x')
                      Children(1):
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Int32, IsImplicit) (Syntax: 'int')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0117: 'int' does not contain a definition for 'x'
                //         var i = /*<bind>*/new int() { x = 0 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMember, "x").WithArguments("int", "x").WithLocation(6, 39)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ...  1, y = x }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = 1, y = x }')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
            Left: 
              IFieldReferenceOperation: System.Int32 MemberInitializerTest.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'y = x')
            Left: 
              IPropertyReferenceOperation: System.Int32 MemberInitializerTest.y { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'y')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'y')
            Right: 
              IFieldReferenceOperation: System.Int32 MemberInitializerTest.x (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsInvalid, IsImplicit) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0120: An object reference is required for the non-static field, method, or property 'MemberInitializerTest.x'
                //         var i = /*<bind>*/new MemberInitializerTest() { x = 1, y = x }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x").WithArguments("MemberInitializerTest.x").WithLocation(8, 68)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ...  2, z = 3 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = 1, y = 2, z = 3 }')
      Initializers(3):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'x = 1')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid) (Syntax: 'x')
                Children(1):
                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'MemberInitializerTest')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'y = 2')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid) (Syntax: 'y')
                Children(1):
                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'MemberInitializerTest')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'z = 3')
            Left: 
              IFieldReferenceOperation: System.Int32 MemberInitializerTest.z (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'z')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'z')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
    IInvalidOperation (OperationKind.Invalid, Type: I, IsInvalid) (Syntax: 'new I() { }')
      Children(1):
          IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: I, IsInvalid) (Syntax: '{ }')
            Initializers(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0144: Cannot create an instance of the abstract type or interface 'I'
                //         var i = /*<bind>*/new I() { }/*</bind>*/; // CS0144
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new I() { }").WithArguments("I").WithLocation(7, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: Test..ctor()) (OperationKind.ObjectCreation, Type: Test, IsInvalid) (Syntax: 'new Test()  ... , y = 2 } }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Test, IsInvalid) (Syntax: '{ Prop = {  ... , y = 2 } }')
      Initializers(1):
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Prop = { x = 1, y = 2 }')
            InitializedMember: 
              IPropertyReferenceOperation: MemberInitializerTest Test.Prop { set; } (OperationKind.PropertyReference, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Prop')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Test, IsInvalid, IsImplicit) (Syntax: 'Prop')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest) (Syntax: '{ x = 1, y = 2 }')
                Initializers(2):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
                      Left: 
                        IFieldReferenceOperation: System.Int32 MemberInitializerTest.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'x')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'y = 2')
                      Left: 
                        IPropertyReferenceOperation: System.Int32 MemberInitializerTest.y { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'y')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'y')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0154: The property or indexer 'Test.Prop' cannot be used in this context because it lacks the get accessor
                //         var i = /*<bind>*/new Test() { Prop = { x = 1, y = 2 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "Prop").WithArguments("Test.Prop").WithLocation(15, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... { x = m.x }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = m.x }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'x = m.x')
            Left: 
              IFieldReferenceOperation: System.Int32 MemberInitializerTest.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'x')
            Right: 
              IFieldReferenceOperation: System.Int32 MemberInitializerTest.x (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'm.x')
                Instance Receiver: 
                  ILocalReferenceOperation: m (OperationKind.LocalReference, Type: MemberInitializerTest, IsInvalid) (Syntax: 'm')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0165: Use of unassigned local variable 'm'
                //         MemberInitializerTest m = /*<bind>*/new MemberInitializerTest() { x = m.x }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "m").WithArguments("m").WithLocation(7, 79)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... ) { x = 1 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = 1 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'x = 1')
            Left: 
              IFieldReferenceOperation: System.Int32 MemberInitializerTest.x (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsInvalid, IsImplicit) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0191: A readonly field cannot be assigned to (except in the constructor of the class in which the field is defined or a variable initializer))
                //         var i = /*<bind>*/new MemberInitializerTest() { x = 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_AssgReadonly, "x").WithLocation(12, 57)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... ) { y = 2 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ y = 2 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'y = 2')
            Left: 
              IPropertyReferenceOperation: System.Int32 MemberInitializerTest.y { get; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsInvalid, IsImplicit) (Syntax: 'y')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IInvalidOperation (OperationKind.Invalid, Type: X, IsInvalid) (Syntax: 'new X() { x = 0 }')
  Children(1):
      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: X) (Syntax: '{ x = 0 }')
        Initializers(1):
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?) (Syntax: 'x = 0')
              Left: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: 'x')
                  Children(1):
                      IOperation:  (OperationKind.None, Type: null) (Syntax: 'x')
                        Children(1):
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: X, IsInvalid, IsImplicit) (Syntax: 'X')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0246: The type or namespace name 'X' could not be found (are you missing a using directive or an assembly reference?)
                //         var i = /*<bind>*/new X() { x = 0 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "X").WithArguments("X").WithLocation(6, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IInvalidOperation (OperationKind.Invalid, Type: Bar, IsInvalid) (Syntax: 'new Bar() { Width = 16 }')
  Children(1):
      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Bar) (Syntax: '{ Width = 16 }')
        Initializers(1):
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?) (Syntax: 'Width = 16')
              Left: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: 'Width')
                  Children(1):
                      IOperation:  (OperationKind.None, Type: null) (Syntax: 'Width')
                        Children(1):
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Bar, IsInvalid, IsImplicit) (Syntax: 'Bar')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 16) (Syntax: '16')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0246: The type or namespace name 'Bar' could not be found (are you missing a using directive or an assembly reference?)
                //         var x = /*<bind>*/new Bar() { Width = 16 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Bar").WithArguments("Bar").WithLocation(11, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IInvalidOperation (OperationKind.Invalid, Type: T, IsInvalid) (Syntax: 'new T() { x = 0 }')
  Children(1):
      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: T, IsInvalid) (Syntax: '{ x = 0 }')
        Initializers(1):
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'x = 0')
              Left: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'x')
                  Children(1):
                      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'x')
                        Children(1):
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsInvalid, IsImplicit) (Syntax: 'T')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: Gen<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: Gen<System.Int32>, IsInvalid) (Syntax: 'new Gen<int> { 1 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Gen<System.Int32>, IsInvalid) (Syntax: '{ 1 }')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '1')
            Children(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0411: The type arguments for method 'Gen<int>.Add<U>(int)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var coll = /*<bind>*/new Gen<int> { 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "1").WithArguments("Gen<int>.Add<U>(int)").WithLocation(21, 45)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ...  = 0, y++ }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = 0, y++ }')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 0')
            Left: 
              IFieldReferenceOperation: System.Int32 MemberInitializerTest.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: ?, IsInvalid) (Syntax: 'y++')
            Target: 
              IFieldReferenceOperation: System.Int32 MemberInitializerTest.y (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsInvalid, IsImplicit) (Syntax: 'y')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... zerTest() }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = 0, Go ... zerTest() }')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 0')
            Left: 
              IFieldReferenceOperation: System.Int32 MemberInitializerTest.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Goo() = new ... lizerTest()')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Goo()')
                Children(0)
            Right: 
              IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... lizerTest()')
                Arguments(0)
                Initializer: 
                  null
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Int32>, IsInvalid) (Syntax: 'new List<in ... o().x = 1 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>, IsInvalid) (Syntax: '{ 1, Goo().x = 1 }')
      Initializers(2):
          IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'List<int>')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'Goo().x = 1')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'List<int>')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'Goo().x = 1')
                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'Goo().x = 1')
                    Left: 
                      IFieldReferenceOperation: System.Int32 MemberInitializerTest.x (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'Goo().x')
                        Instance Receiver: 
                          IInvocationOperation (MemberInitializerTest MemberInitializerTest.Goo()) (OperationKind.Invocation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Goo()')
                            Instance Receiver: 
                              null
                            Arguments(0)
                    Right: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0747: Invalid initializer member declarator
                //         var i = /*<bind>*/new List<int> { 1, Goo().x = 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "Goo().x = 1").WithLocation(10, 46)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ...  1, x = 2 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = 1, x = 2 }')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
            Left: 
              IFieldReferenceOperation: System.Int32 MemberInitializerTest.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'x = 2')
            Left: 
              IFieldReferenceOperation: System.Int32 MemberInitializerTest.x (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsInvalid, IsImplicit) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1912: Duplicate initialization of member 'x'
                //         var i = /*<bind>*/new MemberInitializerTest() { x = 1, x = 2 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "x").WithArguments("x").WithLocation(7, 64)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... zerTest() }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ Goo = new ... zerTest() }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'Goo = new M ... lizerTest()')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Goo')
                Children(1):
                    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Goo')
                      Children(1):
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'MemberInitializerTest')
            Right: 
              IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest) (Syntax: 'new MemberI ... lizerTest()')
                Arguments(0)
                Initializer: 
                  null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1913: Member 'Goo' cannot be initialized. It is not a field or property.
                //         var i = /*<bind>*/new MemberInitializerTest() { Goo = new MemberInitializerTest() }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MemberCannotBeInitialized, "Goo").WithArguments("Goo").WithLocation(7, 57)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: X..ctor()) (OperationKind.ObjectCreation, Type: X, IsInvalid) (Syntax: 'new X() { x = 0 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: X, IsInvalid) (Syntax: '{ x = 0 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: X, IsInvalid) (Syntax: 'x = 0')
            Left: 
              IFieldReferenceOperation: X.x (Static) (OperationKind.FieldReference, Type: X, IsInvalid) (Syntax: 'x')
                Instance Receiver: 
                  null
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1914: Static field or property 'X.x' cannot be assigned in an object initializer
                //         var i = /*<bind>*/new X() { x = 0 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_StaticMemberInObjectInitializer, "x").WithArguments("X.x").WithLocation(8, 37)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ...  Prop = 1 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = 1, Prop = 1 }')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'x = 1')
            Left: 
              IFieldReferenceOperation: System.Int32 MemberInitializerTest.x (Static) (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
                Instance Receiver: 
                  null
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'Prop = 1')
            Left: 
              IPropertyReferenceOperation: System.Int32 MemberInitializerTest.Prop { get; set; } (Static) (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'Prop')
                Instance Receiver: 
                  null
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... { y = 1 } }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = { y = 1 } }')
      Initializers(1):
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: MemberInitializerTest2, IsInvalid) (Syntax: 'x = { y = 1 }')
            InitializedMember: 
              IFieldReferenceOperation: MemberInitializerTest2 MemberInitializerTest.x (OperationKind.FieldReference, Type: MemberInitializerTest2, IsInvalid) (Syntax: 'x')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsInvalid, IsImplicit) (Syntax: 'x')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest2) (Syntax: '{ y = 1 }')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'y = 1')
                      Left: 
                        IFieldReferenceOperation: System.Int32 MemberInitializerTest2.y (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'y')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest2, IsImplicit) (Syntax: 'y')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1917: Members of readonly field 'MemberInitializerTest.x' of type 'MemberInitializerTest2' cannot be assigned with an object initializer because it is of a value type
                //         var i = /*<bind>*/new MemberInitializerTest() { x = { y = 1 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ReadonlyValueTypeInObjectInitializer, "x").WithArguments("MemberInitializerTest.x", "MemberInitializerTest2").WithLocation(8, 57)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... { x = 1 } }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ x = 1, Pr ... { x = 1 } }')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
            Left: 
              IFieldReferenceOperation: System.Int32 MemberInitializerTest.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: MemberInitializerTest2, IsInvalid) (Syntax: 'Prop = { x = 1 }')
            InitializedMember: 
              IPropertyReferenceOperation: MemberInitializerTest2 MemberInitializerTest.Prop { get; set; } (OperationKind.PropertyReference, Type: MemberInitializerTest2, IsInvalid) (Syntax: 'Prop')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsInvalid, IsImplicit) (Syntax: 'Prop')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest2) (Syntax: '{ x = 1 }')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
                      Left: 
                        IFieldReferenceOperation: System.Int32 MemberInitializerTest2.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'x')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest2, IsImplicit) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1918: Members of property 'MemberInitializerTest.Prop' of type 'MemberInitializerTest2' cannot be assigned with an object initializer because it is of a value type
                //         var i = /*<bind>*/new MemberInitializerTest() { x = 1, Prop = { x = 1 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "Prop").WithArguments("MemberInitializerTest.Prop", "MemberInitializerTest2").WithLocation(9, 64)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IBlockOperation (4 statements, 2 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: MemberInitializerTest i
    Local_2: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>> collection
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var i = new ...  y = { } };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var i = new ... { y = { } }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: MemberInitializerTest i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = new Mem ... { y = { } }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new Membe ... { y = { } }')
                IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest) (Syntax: 'new MemberI ... { y = { } }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest) (Syntax: '{ y = { } }')
                      Initializers(1):
                          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'y = { }')
                            InitializedMember: 
                              IFieldReferenceOperation: System.Collections.Generic.List<System.Int32> MemberInitializerTest.y (OperationKind.FieldReference, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'y')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'y')
                            Initializer: 
                              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ }')
                                Initializers(0)
      Initializer: 
        null
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = new Mem ... int> { } };')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MemberInitializerTest) (Syntax: 'i = new Mem ... <int> { } }')
        Left: 
          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: MemberInitializerTest) (Syntax: 'i')
        Right: 
          IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest) (Syntax: 'new MemberI ... <int> { } }')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest) (Syntax: '{ y = new L ... <int> { } }')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'y = new List<int> { }')
                      Left: 
                        IFieldReferenceOperation: System.Collections.Generic.List<System.Int32> MemberInitializerTest.y (OperationKind.FieldReference, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'y')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'y')
                      Right: 
                        IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new List<int> { }')
                          Arguments(0)
                          Initializer: 
                            IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ }')
                              Initializers(0)
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'i = new Mem ...  { { } } };')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MemberInitializerTest, IsInvalid) (Syntax: 'i = new Mem ... = { { } } }')
        Left: 
          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: MemberInitializerTest) (Syntax: 'i')
        Right: 
          IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... = { { } } }')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ y = { { } } }')
                Initializers(1):
                    IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Collections.Generic.List<System.Int32>, IsInvalid) (Syntax: 'y = { { } }')
                      InitializedMember: 
                        IFieldReferenceOperation: System.Collections.Generic.List<System.Int32> MemberInitializerTest.y (OperationKind.FieldReference, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'y')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsImplicit) (Syntax: 'y')
                      Initializer: 
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>, IsInvalid) (Syntax: '{ { } }')
                          Initializers(1):
                              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '{ }')
                                Children(0)
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'List<List<i ... () { { } };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'List<List<i ... >() { { } }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>> collection) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'collection  ... >() { { } }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new List< ... >() { { } }')
                IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsInvalid) (Syntax: 'new List<Li ... >() { { } }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsInvalid) (Syntax: '{ { } }')
                      Initializers(1):
                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '{ }')
                            Children(0)
      Initializer: 
        null
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: Test..ctor()) (OperationKind.ObjectCreation, Type: Test, IsInvalid) (Syntax: 'new Test() { 1 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Test, IsInvalid) (Syntax: '{ 1 }')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '1')
            Children(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1921: The best overloaded method match for 'Test.Add(int)' has wrong signature for the initializer element. The initializable Add must be an accessible instance method.
                //         var coll = /*<bind>*/new Test() { 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InitializerAddHasWrongSignature, "1").WithArguments("Test.Add(int)").WithLocation(11, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IBlockOperation (3 statements, 2 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: B coll
    Local_2: A tc
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'B coll = new B { 1 };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'B coll = new B { 1 }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: B coll) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'coll = new B { 1 }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new B { 1 }')
                IObjectCreationOperation (Constructor: B..ctor()) (OperationKind.ObjectCreation, Type: B, IsInvalid) (Syntax: 'new B { 1 }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: B, IsInvalid) (Syntax: '{ 1 }')
                      Initializers(1):
                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '1')
                            Children(1):
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'var tc = ne ...  ""hello"" };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var tc = ne ... , ""hello"" }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: A tc) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'tc = new A  ... , ""hello"" }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new A { 1, ""hello"" }')
                IObjectCreationOperation (Constructor: A..ctor()) (OperationKind.ObjectCreation, Type: A, IsInvalid) (Syntax: 'new A { 1, ""hello"" }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: A, IsInvalid) (Syntax: '{ 1, ""hello"" }')
                      Initializers(2):
                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '1')
                            Children(1):
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '""hello""')
                            Children(1):
                                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""hello"", IsInvalid) (Syntax: '""hello""')
      Initializer: 
        null
  IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return 0;')
    ReturnedValue: 
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: B..ctor()) (OperationKind.ObjectCreation, Type: B, IsInvalid) (Syntax: 'new B { 1 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: B, IsInvalid) (Syntax: '{ 1 }')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '1')
            Children(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1922: Cannot initialize type 'B' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         B coll = /*<bind>*/new B { 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ 1 }").WithArguments("B").WithLocation(8, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... est { y++ }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ y++ }')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'y++')
            Children(1):
                IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: ?, IsInvalid) (Syntax: 'y++')
                  Target: 
                    IFieldReferenceOperation: System.Int32 MemberInitializerTest.y (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsInvalid, IsImplicit) (Syntax: 'y')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... zerTest() }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MemberInitializerTest, IsInvalid) (Syntax: '{ Goo() = n ... zerTest() }')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Goo() = new ... lizerTest()')
            Children(1):
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Goo() = new ... lizerTest()')
                  Left: 
                    IInvalidOperation (OperationKind.Invalid, Type: MemberInitializerTest, IsInvalid) (Syntax: 'Goo()')
                      Children(0)
                  Right: 
                    IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ... lizerTest()')
                      Arguments(0)
                      Initializer: 
                        null
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: TestClass..ctor()) (OperationKind.ObjectCreation, Type: TestClass, IsInvalid) (Syntax: 'new TestClass { ""hi"" }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: TestClass, IsInvalid) (Syntax: '{ ""hi"" }')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '""hi""')
            Children(1):
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""hi"", IsInvalid) (Syntax: '""hi""')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: Test..ctor()) (OperationKind.ObjectCreation, Type: Test, IsInvalid) (Syntax: 'new Test() { 1 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Test, IsInvalid) (Syntax: '{ 1 }')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '1')
            Children(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1954: The best overloaded method match 'Test.Add(ref int)' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
                //         var coll = /*<bind>*/new Test() { 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InitializerAddHasParamModifiers, "1").WithArguments("Test.Add(ref int)").WithLocation(11, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: MyList<MyClass>..ctor()) (OperationKind.ObjectCreation, Type: MyList<MyClass>, IsInvalid) (Syntax: 'new MyList< ... ""maple"" } }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MyList<MyClass>, IsInvalid) (Syntax: '{ new MyCla ... ""maple"" } }')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'new MyClass ... = ""maple"" }')
            Children(1):
                IObjectCreationOperation (Constructor: MyClass..ctor()) (OperationKind.ObjectCreation, Type: MyClass, IsInvalid) (Syntax: 'new MyClass ... = ""maple"" }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: MyClass, IsInvalid) (Syntax: '{ tree = ""maple"" }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid) (Syntax: 'tree = ""maple""')
                            Left: 
                              IPropertyReferenceOperation: System.String MyClass.tree { get; set; } (OperationKind.PropertyReference, Type: System.String, IsInvalid) (Syntax: 'tree')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: MyClass, IsInvalid, IsImplicit) (Syntax: 'tree')
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""maple"", IsInvalid) (Syntax: '""maple""')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IInvalidOperation (OperationKind.Invalid, Type: MemberInitializerTest.D<System.Int32>, IsInvalid) (Syntax: 'new D<int>( ... d<int>) { }')
  Children(1):
      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'GenericMethod<int>')
        Children(1):
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsInvalid, IsImplicit) (Syntax: 'GenericMethod<int>')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1958: Object and collection initializer expressions may not be applied to a delegate creation expression
                //         D<int> genD = /*<bind>*/new D<int>(GenericMethod<int>) { }/*</bind>*/; // CS1958
                Diagnostic(ErrorCode.ERR_ObjectOrCollectionInitializerWithDelegateCreation, "new D<int>(GenericMethod<int>) { }").WithLocation(8, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IBlockOperation (5 statements, 4 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: B coll1
    Local_2: C coll2
    Local_3: D coll3
    Local_4: E coll4
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'B coll1 = new B { 1 };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'B coll1 = new B { 1 }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: B coll1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'coll1 = new B { 1 }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new B { 1 }')
                IObjectCreationOperation (Constructor: B..ctor()) (OperationKind.ObjectCreation, Type: B, IsInvalid) (Syntax: 'new B { 1 }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: B, IsInvalid) (Syntax: '{ 1 }')
                      Initializers(1):
                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '1')
                            Children(1):
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'C coll2 = new C { 1 };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'C coll2 = new C { 1 }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: C coll2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'coll2 = new C { 1 }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new C { 1 }')
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C { 1 }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ 1 }')
                      Initializers(1):
                          IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '1')
                            Children(1):
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'D coll3 = n ... { 1, 2 } };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'D coll3 = n ...  { 1, 2 } }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: D coll3) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'coll3 = new ...  { 1, 2 } }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new D { { 1, 2 } }')
                IObjectCreationOperation (Constructor: D..ctor()) (OperationKind.ObjectCreation, Type: D, IsInvalid) (Syntax: 'new D { { 1, 2 } }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: D, IsInvalid) (Syntax: '{ { 1, 2 } }')
                      Initializers(1):
                          IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '{ 1, 2 }')
                            Children(2):
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'E coll4 = new E { 1 };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'E coll4 = new E { 1 }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: E coll4) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'coll4 = new E { 1 }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new E { 1 }')
                IObjectCreationOperation (Constructor: E..ctor()) (OperationKind.ObjectCreation, Type: E, IsInvalid) (Syntax: 'new E { 1 }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: E, IsInvalid) (Syntax: '{ 1 }')
                      Initializers(1):
                          IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '1')
                            Children(1):
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
      Initializer: 
        null
  IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return 0;')
    ReturnedValue: 
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: Test..ctor()) (OperationKind.ObjectCreation, Type: Test, IsInvalid) (Syntax: 'new Test()  ... { x = 1 } }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Test, IsInvalid) (Syntax: '{ x = 1, {  ... { x = 1 } }')
      Initializers(3):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
            Left: 
              IFieldReferenceOperation: System.Int32 Test.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Test, IsImplicit) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '{ 1 }')
            Children(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '{ x = 1 }')
            Children(1):
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'x = 1')
                  Left: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x')
                      Children(0)
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
        var x = 1;
    }/*</bind>*/
}
";
            string expectedOperationTree = """
                
                IInvalidOperation (OperationKind.Invalid, Type: Dictionary<System.Object, System.Object>, IsInvalid) (Syntax: 'new Diction ... }')
                  Children(1):
                      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Dictionary<System.Object, System.Object>, IsInvalid) (Syntax: '{ ... }')
                        Initializers(3):
                            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '{"s", 1 }')
                              Children(2):
                                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "s", IsInvalid) (Syntax: '"s"')
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'var')
                              Children(0)
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'x = 1')
                              Left:
                                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'x')
                                  Children(1):
                                      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'x')
                                        Children(1):
                                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Dictionary<System.Object, System.Object>, IsInvalid, IsImplicit) (Syntax: 'Dictionary< ... ct, object>')
                              Right:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                """;
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (9,13): error CS1003: Syntax error, ',' expected
                //         var x = 1;
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",").WithLocation(9, 13),
                // (9,18): error CS1003: Syntax error, ',' expected
                //         var x = 1;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(9, 18),
                // (10,6): error CS1002: ; expected
                //     }/*</bind>*/
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(10, 6),
                // (11,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(11, 2),
                // (6,31): error CS0246: The type or namespace name 'Dictionary<,>' could not be found (are you missing a using directive or an assembly reference?)
                //         var d = /*<bind>*/new Dictionary<object, object>()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Dictionary<object, object>").WithArguments("Dictionary<,>").WithLocation(6, 31),
                // (8,13): error CS0747: Invalid initializer member declarator
                //             {"s", 1 },
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, @"{""s"", 1 }").WithLocation(8, 13),
                // (9,9): error CS0747: Invalid initializer member declarator
                //         var x = 1;
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "var").WithLocation(9, 9),
                // (9,9): error CS0103: The name 'var' does not exist in the current context
                //         var x = 1;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(9, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IInvalidOperation (OperationKind.Invalid, Type: List<System.Int32>, IsInvalid) (Syntax: 'new List<in ... { { { 1 } }')
  Children(1):
      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: List<System.Int32>, IsInvalid) (Syntax: '{ { { 1 } }')
        Initializers(2):
            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '{ ')
              Children(0)
            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '{ 1 }')
              Children(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1513: } expected
                //         /*<bind>*/new List<int>() { { { 1 } }/*</bind>*/ };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "{").WithLocation(6, 39),
                // CS1003: Syntax error, ',' expected
                //         /*<bind>*/new List<int>() { { { 1 } }/*</bind>*/ };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(6, 39),
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32, IsInvalid) (Syntax: 'new int { }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Int32, IsInvalid) (Syntax: '{ }')
      Initializers(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0133: The expression being assigned to 'Program.value' must be constant
                //     const int value = /*<bind>*/new int { }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "new int { }").WithArguments("Program.value").WithLocation(4, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Int32>, IsInvalid) (Syntax: 'new List<in ... unt = { } }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>, IsInvalid) (Syntax: '{ Count = { } }')
      Initializers(1):
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Int32, IsInvalid) (Syntax: 'Count = { }')
            InitializedMember: 
              IPropertyReferenceOperation: System.Int32 System.Collections.Generic.List<System.Int32>.Count { get; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'Count')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'Count')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Int32) (Syntax: '{ }')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: N.C..ctor()) (OperationKind.ObjectCreation, Type: N.C, IsInvalid) (Syntax: 'new C() ... }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: N.C, IsInvalid) (Syntax: '{ ... }')
      Initializers(4):
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: N.Struct, IsInvalid) (Syntax: 'StructField = { }')
            InitializedMember: 
              IFieldReferenceOperation: N.Struct N.C.StructField (OperationKind.FieldReference, Type: N.Struct, IsInvalid) (Syntax: 'StructField')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: N.C, IsInvalid, IsImplicit) (Syntax: 'StructField')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: N.Struct) (Syntax: '{ }')
                Initializers(0)
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: N.Struct, IsInvalid) (Syntax: 'StructProp = { }')
            InitializedMember: 
              IPropertyReferenceOperation: N.Struct N.C.StructProp { get; } (OperationKind.PropertyReference, Type: N.Struct, IsInvalid) (Syntax: 'StructProp')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: N.C, IsInvalid, IsImplicit) (Syntax: 'StructProp')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: N.Struct) (Syntax: '{ }')
                Initializers(0)
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: N.Class) (Syntax: 'ClassField = { }')
            InitializedMember: 
              IFieldReferenceOperation: N.Class N.C.ClassField (OperationKind.FieldReference, Type: N.Class) (Syntax: 'ClassField')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: N.C, IsImplicit) (Syntax: 'ClassField')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: N.Class) (Syntax: '{ }')
                Initializers(0)
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: N.Class) (Syntax: 'ClassProp = { }')
            InitializedMember: 
              IPropertyReferenceOperation: N.Class N.C.ClassProp { get; } (OperationKind.PropertyReference, Type: N.Class) (Syntax: 'ClassProp')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: N.C, IsImplicit) (Syntax: 'ClassProp')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: N.Class) (Syntax: '{ }')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: A..ctor()) (OperationKind.ObjectCreation, Type: A, IsInvalid) (Syntax: 'new A { 5,  ...  { 1, 2 } }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: A, IsInvalid) (Syntax: '{ 5, { 1, 2, { 1, 2 } }')
      Initializers(3):
          IInvocationOperation ( void A.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '5')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: 'A')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '5')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '{ 1, 2, ')
            Children(3):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                  Children(0)
          IInvocationOperation ( void A.Add(System.Int32 i, System.Int32 j)) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '{ 1, 2 }')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: 'A')
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: j) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(9, 46),
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
            CreateCompilation(source).VerifyDiagnostics(
                // (6,21): error CS0826: No best type found for implicitly-typed array
                //         var array = new[] { Main() };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { Main() }"));
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C { (i = 1) }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ (i = 1) }')
      Initializers(1):
          IInvocationOperation ( void C.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '(i = 1)')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i = 1')
                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                    Left: 
                      ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                    Right: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C { 1 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ 1 }')
      Initializers(1):
          IInvocationOperation ( void C.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '1')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IObjectCreationOperation (Constructor: B..ctor()) (OperationKind.ObjectCreation, Type: B, IsInvalid) (Syntax: 'new B { A = ... 4, 5, 6 } }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: B, IsInvalid) (Syntax: '{ A = { 4, 5, 6 } }')
      Initializers(1):
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: A, IsInvalid) (Syntax: 'A = { 4, 5, 6 }')
            InitializedMember: 
              IPropertyReferenceOperation: A B.A { get; set; } (OperationKind.PropertyReference, Type: A, IsInvalid) (Syntax: 'A')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: B, IsInvalid, IsImplicit) (Syntax: 'A')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: A) (Syntax: '{ 4, 5, 6 }')
                Initializers(3):
                    IInvocationOperation ( void A.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '4')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: A, IsInvalid, IsImplicit) (Syntax: 'A')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '4')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IInvocationOperation ( void A.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '5')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: A, IsInvalid, IsImplicit) (Syntax: 'A')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '5')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IInvocationOperation ( void A.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '6')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: A, IsInvalid, IsImplicit) (Syntax: 'A')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '6')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
            var compilation = CreateCompilation(source);

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
            var compilation = CreateCompilation(source);

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
            var compilation = CreateCompilation(source);

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
            var compilation = CreateCompilation(source);

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
            var compilation = CreateCompilation(source);

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
            var compilation = CreateCompilation(source);

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
            var compilation = CreateCompilation(source);

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
            var compilation = CreateCompilation(source);

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

        [Fact]
        public void GetComplexCollectionInitializerConversionInfo_NoConversion()
        {
            var source = """
                using System.Collections.Generic;
                _ = new Dictionary<string, string> { { "a", "b" } };
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var syntax = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntax);
            var literals = syntax.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(2, literals.Length);
            foreach (var literal in literals)
            {
                var conversion = model.GetConversion(literal);
                Assert.Equal(ConversionKind.Identity, conversion.Kind);
                var typeInfo = model.GetTypeInfo(literal);
                Assert.Same(typeInfo.Type, typeInfo.ConvertedType);
            }
        }

        [Fact]
        public void GetComplexCollectionInitializerConversionInfo_ImplicitReferenceConversion()
        {
            var source = """
                using System.Collections.Generic;
                _ = new Dictionary<object, object> { { "a", "b" } };
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var syntax = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntax);
            var literals = syntax.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(2, literals.Length);
            foreach (var literal in literals)
            {
                var conversion = model.GetConversion(literal);
                Assert.Equal(ConversionKind.ImplicitReference, conversion.Kind);
                var typeInfo = model.GetTypeInfo(literal);
                Assert.Equal(SpecialType.System_String, typeInfo.Type.SpecialType);
                Assert.Equal(SpecialType.System_Object, typeInfo.ConvertedType.SpecialType);
            }
        }

        [Fact]
        public void GetComplexCollectionInitializerConversionInfo_ImplicitBoxingConversion()
        {
            var source = """
                using System.Collections.Generic;
                _ = new Dictionary<object, object> { { 1, 2 } };
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var syntax = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntax);
            var literals = syntax.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(2, literals.Length);
            foreach (var literal in literals)
            {
                var conversion = model.GetConversion(literal);
                Assert.Equal(ConversionKind.Boxing, conversion.Kind);
                var typeInfo = model.GetTypeInfo(literal);
                Assert.Equal(SpecialType.System_Int32, typeInfo.Type.SpecialType);
                Assert.Equal(SpecialType.System_Object, typeInfo.ConvertedType.SpecialType);
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

            CreateCompilation(source).VerifyDiagnostics(
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

            CreateCompilation(source).VerifyDiagnostics(
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

            var compilation = CreateCompilation(source);

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
            CompileAndVerify(source, new[] { CSharpRef }, expectedOutput: "Initialized");
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
            var compilation = CreateCompilation(source);

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

        [WorkItem(27060, "https://github.com/dotnet/roslyn/issues/27060")]
        [Fact]
        public void GetTypeInfoForBadExpression_01()
        {
            var source = @"
using System.Collections.Generic;

class C
{
    void M()
    {
        I i = new I() { 1, 2 }
    }
}

interface I : IEnumerable<int>
{
    void Add(int i);
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,15): error CS0144: Cannot create an instance of the abstract type or interface 'I'
                //         I i = new I() { 1, 2 }
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new I() { 1, 2 }").WithArguments("I").WithLocation(8, 15),
                // (8,31): error CS1002: ; expected
                //         I i = new I() { 1, 2 }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(8, 31)
                );
            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null, IsInvalid) (Syntax: 'void M() ... }')
      BlockBody: 
        IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
          Locals: Local_1: I i
          IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'I i = new I() { 1, 2 }')
            IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'I i = new I() { 1, 2 }')
              Declarators:
                  IVariableDeclaratorOperation (Symbol: I i) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i = new I() { 1, 2 }')
                    Initializer: 
                      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new I() { 1, 2 }')
                        IInvalidOperation (OperationKind.Invalid, Type: I, IsInvalid) (Syntax: 'new I() { 1, 2 }')
                          Children(1):
                              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: I, IsInvalid) (Syntax: '{ 1, 2 }')
                                Initializers(2):
                                    IInvocationOperation (virtual void I.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '1')
                                      Instance Receiver: 
                                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: I, IsInvalid, IsImplicit) (Syntax: 'I')
                                      Arguments(1):
                                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    IInvocationOperation (virtual void I.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '2')
                                      Instance Receiver: 
                                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: I, IsInvalid, IsImplicit) (Syntax: 'I')
                                      Arguments(1):
                                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '2')
                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
      ExpressionBody: 
        null
");
            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Where(n => n.ToString() == "2");
            var node = nodes.First();
            var typeInfo = semanticModel.GetTypeInfo(node);
            Assert.Equal(SpecialType.System_Int32, typeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, typeInfo.ConvertedType.SpecialType);
        }

        [WorkItem(27060, "https://github.com/dotnet/roslyn/issues/27060")]
        [Fact]
        public void GetTypeInfoForBadExpression_02()
        {
            var source = @"
using System.Collections.Generic;

class C
{
    void M()
    {
        var x = new I[] { 1, 2 }
    }
}

interface I : IEnumerable<int>
{
    void Add(int i);
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,27): error CS0029: Cannot implicitly convert type 'int' to 'I'
                //         var x = new I[] { 1, 2 }
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "I").WithLocation(8, 27),
                // (8,30): error CS0029: Cannot implicitly convert type 'int' to 'I'
                //         var x = new I[] { 1, 2 }
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "2").WithArguments("int", "I").WithLocation(8, 30),
                // (8,33): error CS1002: ; expected
                //         var x = new I[] { 1, 2 }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(8, 33)
                );
            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null, IsInvalid) (Syntax: 'void M() ... }')
      BlockBody: 
        IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
          Locals: Local_1: I[] x
          IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'var x = new I[] { 1, 2 }')
            IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var x = new I[] { 1, 2 }')
              Declarators:
                  IVariableDeclaratorOperation (Symbol: I[] x) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'x = new I[] { 1, 2 }')
                    Initializer: 
                      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new I[] { 1, 2 }')
                        IArrayCreationOperation (OperationKind.ArrayCreation, Type: I[], IsInvalid) (Syntax: 'new I[] { 1, 2 }')
                          Dimension Sizes(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid, IsImplicit) (Syntax: 'new I[] { 1, 2 }')
                          Initializer: 
                            IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{ 1, 2 }')
                              Element Values(2):
                                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I, IsInvalid, IsImplicit) (Syntax: '1')
                                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    Operand: 
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I, IsInvalid, IsImplicit) (Syntax: '2')
                                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    Operand: 
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
              Initializer: 
                null
      ExpressionBody: 
        null
");
            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Where(n => n.ToString() == "2");
            var node = nodes.First();
            var typeInfo = semanticModel.GetTypeInfo(node);
            Assert.Equal(SpecialType.System_Int32, typeInfo.Type.SpecialType);
            Assert.Equal("I", typeInfo.ConvertedType.ToDisplayString());
        }

        [Fact]
        public void DynamicInvocationOnRefStructs()
        {
            var source = """
                using System.Collections;
                using System.Collections.Generic;

                dynamic d = null;
                S s = new S() { d };

                ref struct S : IEnumerable<int>
                {
                    public IEnumerator<int> GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                    public void Add<T>(T t) => throw null;
                }
                """;

            CreateCompilation(source).VerifyDiagnostics(
                // (5,11): error CS9230: Cannot perform a dynamic invocation on an expression with type 'S'.
                // S s = new S() { d };
                Diagnostic(ErrorCode.ERR_CannotDynamicInvokeOnExpression, "S").WithArguments("S").WithLocation(5, 11)
            );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72916")]
        public void RefReturning_Indexer()
        {
            var source = """
                public class C
                {
                    public static void Main()
                    {
                        var c = new C() { [1] = 2 };
                        System.Console.WriteLine(c[1]);        
                    }

                    int _test1 = 0;    
                    ref int this[int x]
                    {
                        get => ref _test1;
                    }
                }
                """;

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            CompileAndVerify(comp, expectedOutput: "2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ImplicitElementAccessSyntax>().Single();
            var symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("ref System.Int32 C.this[System.Int32 x] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.Int32", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72916")]
        public void RefReturning_Property()
        {
            var source = """
                public class C
                {
                    public static void Main()
                    {
                        var c = new C() { P = 2 };
                        System.Console.WriteLine(c.P);        
                    }

                    int _test1 = 0;    
                    ref int P
                    {
                        get => ref _test1;
                    }
                }
                """;

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            CompileAndVerify(comp, expectedOutput: "2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var propertyAccess = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().First().Left;
            var symbolInfo = model.GetSymbolInfo(propertyAccess);
            AssertEx.Equal("ref System.Int32 C.P { get; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(propertyAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(propertyAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)propertyAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.Int32", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());
        }
    }
}
