// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            var source = @"
public class MemberInitializerTest
{   
    public int x;
    public int y { get; set; }

    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 0, y = 0 };
    }
}
";
            CompileAndVerify(source, expectedOutput: "");
        }

        [Fact]
        public void ObjectInitializerTest_StructType()
        {
            var source = @"
public struct MemberInitializerTest
{   
    public int x;
    public int y { get; set; }

    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 0, y = 0 };
    }
}
";
            CompileAndVerify(source, expectedOutput: "");
        }

        [Fact]
        public void ObjectInitializerTest_TypeParameterType()
        {
            var source = @"
public class Base
{
    public Base() {}
    public int x;
    public int y { get; set; }
    public static void Main()
    {
        MemberInitializerTest<Base>.Foo();
    }
}

public class MemberInitializerTest<T> where T: Base, new()
{   
    public static void Foo()
    {
        var i = new T() { x = 0, y = 0 };
    }
}
";
            CompileAndVerify(source, expectedOutput: "");
        }

        [Fact]
        public void ObjectInitializerTest_EnumType()
        {
            var source = @"
public enum X { x = 0 }

public class MemberInitializerTest
{   
    public static void Main()
    {
        var i = new X() { };
    }
}
";
            CompileAndVerify(source, expectedOutput: "");
        }

        [Fact]
        public void ObjectInitializerTest_PrimitiveType()
        {
            var source = @"
public class MemberInitializerTest
{   
    public static void Main()
    {
        var i = new int() { };
    }
}
";
            CompileAndVerify(source, expectedOutput: "");
        }

        [Fact]
        public void ObjectInitializerTest_MemberAccess_DynamicType()
        {
            var source = @"
public class MemberInitializerTest
{   
    public dynamic X;
    public static void Main()
    {
        var i = new MemberInitializerTest { X = 0 };
    }
}
";
            // TODO: This should produce no diagnostics.
            CreateCompilationWithMscorlib(source, references: new MetadataReference[] { SystemCoreRef, CSharpRef }).VerifyDiagnostics();
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
            var source = @"
using System.Collections.Generic;

public class MemberInitializerTest
{   
    public List<int> x;
    public static void Main()
    {
        var i = new List<int>() { };
        var j = new MemberInitializerTest() { x = { } };
        var k = new MemberInitializerTest() { };
    }
}
";
            CompileAndVerify(source, expectedOutput: "");
        }

        [Fact]
        public void CollectionInitializerTest_DynamicType()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
public dynamic list = new List<int>();

    public static int Main()
    {
        var t = new Test() { list = { 1 } };
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
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        B coll = new B { 1, 2, 3, 4, 5 };
        coll.Display();
        return 0;
    }
}

public class B : IEnumerable
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
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        var coll = new B<long> { 1, 2, 3, 4, 5 };
        coll.Display();
        return 0;
    }
}

public class B<T> : IEnumerable<T>
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

            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;
class MemberInitializerTest
{
    public static int Main()
    {
        var coll = new MyList<string> { ""str"" };
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
            CompileAndVerify(source, expectedOutput: "str");
        }

        [Fact]
        public void CollectionInitializerTest_ExplicitImplOfAdd_NoImplicitImpl()
        {
            // Explicit interface member implementation of Add(T) will cause a compile-time error.

            var source = @"
using System.Collections.Generic;
class MemberInitializerTest
{
    public static int Main()
    {
        var coll = new MyList<string> { ""str"" };
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
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,41): error CS1061: 'MyList<string>' does not contain a definition for 'Add' and no extension method 'Add' accepting a first argument of type 'MyList<string>' could be found (are you missing a using directive or an assembly reference?)
                //         var coll = new MyList<string> { "str" };
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, @"""str""").WithArguments("MyList<string>", "Add").WithLocation(7, 41));
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

public class A : IEnumerable<int>
{
    public Action<string> Add;
    
    static void Main()
    {
        new A { """" };
    }
    
    public IEnumerator<int> GetEnumerator() { return null; }
    IEnumerator IEnumerable.GetEnumerator() { return null; }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,17): error CS0118: 'Add' is a field but is used like a method
                Diagnostic(ErrorCode.ERR_BadSKknown, @"""""").WithArguments("Add", "field", "method"));
        }

        [Fact]
        [WorkItem(629368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629368")]
        public void AddPropertyUsedLikeMethod()
        {
            string source = @"
using System;
using System.Collections;
using System.Collections.Generic;

public class A : IEnumerable<int>
{
    public Action<string> Add { get; set; }
    
    static void Main()
    {
        new A { """" };
    }
    
    public IEnumerator<int> GetEnumerator() { return null; }
    IEnumerator IEnumerable.GetEnumerator() { return null; }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,17): error CS0118: 'Add' is a property but is used like a method
                Diagnostic(ErrorCode.ERR_BadSKknown, @"""""").WithArguments("Add", "property", "method"));
        }

        [Fact]
        public void CS0070ERR_BadEventUsage()
        {
            var source = @"
public delegate void D();
public struct MemberInitializerTest
{
    public event D z;
}
public class X
{
    public static void Main()
    {
        var i = new MemberInitializerTest() { z = null };
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (11,47): error CS0070: The event 'MemberInitializerTest.z' can only appear on the left hand side of += or -= (except when used from within the type 'MemberInitializerTest')
                //         var i = new MemberInitializerTest() { z = null };
                Diagnostic(ErrorCode.ERR_BadEventUsage, "z").WithArguments("MemberInitializerTest.z", "MemberInitializerTest").WithLocation(11, 47),
                // (5,20): warning CS0067: The event 'MemberInitializerTest.z' is never used
                //     public event D z;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "z").WithArguments("MemberInitializerTest.z"));
        }

        [Fact]
        public void CS0117ERR_NoSuchMember()
        {
            var source = @"
public class MemberInitializerTest
{   
    public static void Main()
    {
        var i = new int() { x = 0 };
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,29): error CS0117: 'int' does not contain a definition for 'x'
                //         var i = new int() { x = 0 };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "x").WithArguments("int", "x").WithLocation(6, 29));
        }

        [Fact]
        public void CS0120_ERR_ObjectRequired()
        {
            var source = @"
public class MemberInitializerTest
{   
    public int x;
    public int y { get; set; }
    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 1, y = x };
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,58): error CS0120: An object reference is required for the non-static field, method, or property 'MemberInitializerTest.x'
                //         var i = new MemberInitializerTest() { x = 1, y = x };
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x").WithArguments("MemberInitializerTest.x").WithLocation(8, 58));
        }

        [Fact]
        public void CS0122_ERR_BadAccess()
        {
            var source = @"
public class MemberInitializerTest
{   
    protected int x;
    private int y { get; set; }
    internal int z;
}

public class Test
{
    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 1, y = 2, z = 3 };
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (13,47): error CS0122: 'MemberInitializerTest.x' is inaccessible due to its protection level
                //         var i = new MemberInitializerTest() { x = 1, y = 2, z = 3 };
                Diagnostic(ErrorCode.ERR_BadAccess, "x").WithArguments("MemberInitializerTest.x").WithLocation(13, 47),
                // (13,54): error CS0122: 'MemberInitializerTest.y' is inaccessible due to its protection level
                //         var i = new MemberInitializerTest() { x = 1, y = 2, z = 3 };
                Diagnostic(ErrorCode.ERR_BadAccess, "y").WithArguments("MemberInitializerTest.y").WithLocation(13, 54));
        }

        [Fact]
        public void CS0144_ERR_NoNewAbstract()
        {
            var source = @"
public interface I {}
public class MemberInitializerTest
{   
    public static void Main()
    {
        var i = new I() { }; // CS0144
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,17): error CS0144: Cannot create an instance of the abstract class or interface 'I'
                //         var i = new I() { }; // CS0144
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new I() { }").WithArguments("I").WithLocation(7, 17));
        }

        [Fact]
        public void CS0154_ERR_PropertyLacksGet()
        {
            var source = @"
public class MemberInitializerTest
{   
    public int x;
    public int y { get; set; }
}

public class Test
{
    public MemberInitializerTest m;
    public MemberInitializerTest Prop { set { m = value; } }
    
    public static void Main()
    {
        var i = new Test() { Prop = { x = 1, y = 2 } };
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (15,30): error CS0154: The property or indexer 'Test.Prop' cannot be used in this context because it lacks the get accessor
                //         var i = new Test() { Prop = { x = 1, y = 2 } };
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "Prop").WithArguments("Test.Prop").WithLocation(15, 30));
        }

        [Fact]
        public void CS0165_ERR_UseDefViolation()
        {
            var source = @"
public class MemberInitializerTest
{   
    public int x;
    public static void Main()
    {
        MemberInitializerTest m = new MemberInitializerTest() { x = m.x };
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,69): error CS0165: Use of unassigned local variable 'm'
                //         MemberInitializerTest m = new MemberInitializerTest() { x = m.x };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "m").WithArguments("m").WithLocation(7, 69));
        }

        [Fact]
        public void CS0191_ERR_AssgReadonly()
        {
            var source = @"
public struct MemberInitializerTest
{   
    public readonly int x;
    public int y { get { return 0; } }
}

public struct Test
{
    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 1 };
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,47): error CS0191: A readonly field cannot be assigned to (except in a constructor or a variable initializer)
                //         var i = new MemberInitializerTest() { x = 1 };
                Diagnostic(ErrorCode.ERR_AssgReadonly, "x").WithLocation(12, 47));
        }

        [Fact]
        public void CS0200_ERR_AssgReadonlyProp()
        {
            var source = @"
public struct MemberInitializerTest
{   
    public readonly int x;
    public int y { get { return 0; } }
}

public struct Test
{
    public static void Main()
    {
        var i = new MemberInitializerTest() { y = 2 };
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,47): error CS0200: Property or indexer 'MemberInitializerTest.y' cannot be assigned to -- it is read only
                //         var i = new MemberInitializerTest() { y = 2 };
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "y").WithArguments("MemberInitializerTest.y").WithLocation(12, 47));
        }

        [Fact]
        public void CS0246_ERR_SingleTypeNameNotFound()
        {
            var source = @"
public class MemberInitializerTest
{   
    public static void Main()
    {
        var i = new X() { x = 0 };
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,21): error CS0246: The type or namespace name 'X' could not be found (are you missing a using directive or an assembly reference?)
                //         var i = new X() { x = 0 };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "X").WithArguments("X").WithLocation(6, 21));
        }

        [WorkItem(543936, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543936")]
        [Fact]
        public void CS0246_ERR_SingleTypeNameNotFound_02()
        {
            var source = @"
static class Ext
{
    static int Width(this Foo f) { return 0; }
}

class Foo
{
    void M()
    {
        var x = new Bar() { Width = 16 };
    }
}    
";
            CreateCompilationWithMscorlibAndSystemCore(source).VerifyDiagnostics(
                // (11,21): error CS0246: The type or namespace name 'Bar' could not be found (are you missing a using directive or an assembly reference?)
                //         var x = new Bar() { Width = 16 };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Bar").WithArguments("Bar").WithLocation(11, 21));
        }

        [Fact]
        public void CS0304_ERR_NoNewTyvar()
        {
            var source = @"
public class MemberInitializerTest<T>
{   
    public static void Main()
    {
        var i = new T() { x = 0 }; // CS0304
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,17): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                //         var i = new T() { x = 0 }; // CS0304
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new T() { x = 0 }").WithArguments("T").WithLocation(6, 17),
                // (6,27): error CS0117: 'T' does not contain a definition for 'x'
                //         var i = new T() { x = 0 }; // CS0304
                Diagnostic(ErrorCode.ERR_NoSuchMember, "x").WithArguments("T", "x").WithLocation(6, 27));
        }

        [Fact]
        public void CS0411_ERR_CantInferMethTypeArgs()
        {
            var source = @"
using System.Collections.Generic;
using System.Collections;

class Gen<T> : IEnumerable
{
    public static void Add<U>(T i) {}

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
        var coll = new Gen<int> { 1 };
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (21,35): error CS0411: The type arguments for method 'Gen<int>.Add<U>(int)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var coll = new Gen<int> { 1 };
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "1").WithArguments("Gen<int>.Add<U>(int)").WithLocation(21, 35));
        }

        [Fact]
        public void CS0747_ERR_InvalidInitializerElementInitializer()
        {
            var source = @"
public class MemberInitializerTest
{   
    public int x, y;
    public static void Main()
    {
        var i = new MemberInitializerTest { x = 0, y++ };
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,52): error CS0747: Invalid initializer member declarator
                //         var i = new MemberInitializerTest { x = 0, y++ };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "y++").WithLocation(7, 52),
                // (7,52): error CS0120: An object reference is required for the non-static field, method, or property 'MemberInitializerTest.y'
                //         var i = new MemberInitializerTest { x = 0, y++ };
                Diagnostic(ErrorCode.ERR_ObjectRequired, "y").WithArguments("MemberInitializerTest.y").WithLocation(7, 52));
        }

        [Fact]
        public void CS0747_ERR_InvalidInitializerElementInitializer_MethodCall()
        {
            var source = @"
public class MemberInitializerTest
{   
    public int x;
    public MemberInitializerTest Foo() {  return new MemberInitializerTest(); }
    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 0, Foo() = new MemberInitializerTest() };
    }    
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,54): error CS0747: Invalid initializer member declarator
                //         var i = new MemberInitializerTest() { x = 0, Foo() = new MemberInitializerTest() };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "Foo() = new MemberInitializerTest()").WithLocation(8, 54),
                // (8,54): error CS0120: An object reference is required for the non-static field, method, or property 'MemberInitializerTest.Foo()'
                //         var i = new MemberInitializerTest() { x = 0, Foo() = new MemberInitializerTest() };
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Foo").WithArguments("MemberInitializerTest.Foo()").WithLocation(8, 54));
        }

        [Fact]
        public void CS0747_ERR_InvalidInitializerElementInitializer_AssignmentExpression()
        {
            var source = @"
using System.Collections.Generic;
public class MemberInitializerTest
{
    public int x;
    static MemberInitializerTest Foo() { return new MemberInitializerTest(); }

    public static void Main()
    {
        var i = new List<int> { 1, Foo().x = 1};
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,36): error CS0747: Invalid initializer member declarator
                //         var i = new List<int> { 1, Foo().x = 1};
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "Foo().x = 1").WithLocation(10, 36));
        }

        [Fact]
        public void CS1912ERR_MemberAlreadyInitialized()
        {
            var source = @"
public class MemberInitializerTest
{   
    public int x;
    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 1, x = 2 };
    }    
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,54): error CS1912: Duplicate initialization of member 'x'
                //         var i = new MemberInitializerTest() { x = 1, x = 2 };
                Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "x").WithArguments("x").WithLocation(7, 54));
        }

        [Fact]
        public void CS1913ERR_MemberCannotBeInitialized()
        {
            var source = @"
public class MemberInitializerTest
{   
    public MemberInitializerTest Foo() {  return new MemberInitializerTest(); }
    public static void Main()
    {
        var i = new MemberInitializerTest() { Foo = new MemberInitializerTest() };
    }    
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,47): error CS1913: Member 'Foo' cannot be initialized. It is not a field or property.
                //         var i = new MemberInitializerTest() { Foo = new MemberInitializerTest() };
                Diagnostic(ErrorCode.ERR_MemberCannotBeInitialized, "Foo").WithArguments("Foo").WithLocation(7, 47));
        }

        [Fact]
        public void CS1914ERR_StaticMemberInObjectInitializer_EnumTypeMember()
        {
            var source = @"
public enum X { x = 0 }

public class MemberInitializerTest
{   
    public static void Main()
    {
        var i = new X() { x = 0 };
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,27): error CS1914: Static field or property 'X.x' cannot be assigned in an object initializer
                //         var i = new X() { x = 0 };
                Diagnostic(ErrorCode.ERR_StaticMemberInObjectInitializer, "x").WithArguments("X.x").WithLocation(8, 27));
        }

        [Fact]
        public void CS1914ERR_StaticMemberInObjectInitializer()
        {
            var source = @"
public class MemberInitializerTest
{   
    public static int x;
    public static int Prop { get; set; }

    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 1, Prop = 1 };
    }    
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,47): error CS1914: Static field or property 'MemberInitializerTest.x' cannot be assigned in an object initializer
                //         var i = new MemberInitializerTest() { x = 1, Prop = 1 };
                Diagnostic(ErrorCode.ERR_StaticMemberInObjectInitializer, "x").WithArguments("MemberInitializerTest.x").WithLocation(9, 47),
                // (9,54): error CS1914: Static field or property 'MemberInitializerTest.Prop' cannot be assigned in an object initializer
                //         var i = new MemberInitializerTest() { x = 1, Prop = 1 };
                Diagnostic(ErrorCode.ERR_StaticMemberInObjectInitializer, "Prop").WithArguments("MemberInitializerTest.Prop").WithLocation(9, 54));
        }

        [Fact]
        public void CS1917ERR_ReadonlyValueTypeInObjectInitializer()
        {
            var source = @"
public class MemberInitializerTest
{   
    public readonly MemberInitializerTest2 x;

    public static void Main()
    {
        var i = new MemberInitializerTest() { x = { y = 1 } };
    }    
}

public struct MemberInitializerTest2
{
    public int y;
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,47): error CS1917: Members of readonly field 'MemberInitializerTest.x' of type 'MemberInitializerTest2' cannot be assigned with an object initializer because it is of a value type
                //         var i = new MemberInitializerTest() { x = { y = 1 } };
                Diagnostic(ErrorCode.ERR_ReadonlyValueTypeInObjectInitializer, "x").WithArguments("MemberInitializerTest.x", "MemberInitializerTest2").WithLocation(8, 47));
        }

        [Fact]
        public void CS1918ERR_ValueTypePropertyInObjectInitializer()
        {
            var source = @"
public class MemberInitializerTest
{   
    public int x;
    public MemberInitializerTest2 Prop { get; set; }

    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 1, Prop = { x = 1 } };
    }
}

public struct MemberInitializerTest2
{
    public int x;
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,54): error CS1918: Members of property 'MemberInitializerTest.Prop' of type 'MemberInitializerTest2' cannot be assigned with an object initializer because it is of a value type
                //         var i = new MemberInitializerTest() { x = 1, Prop = { x = 1 } };
                Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "Prop").WithArguments("MemberInitializerTest.Prop", "MemberInitializerTest2").WithLocation(9, 54));
        }

        [Fact]
        public void CS1920ERR_EmptyElementInitializer()
        {
            var source = @"
using System.Collections.Generic;

public class MemberInitializerTest
{   
    public List<int> y;
    public static void Main()
    {
        var i = new MemberInitializerTest { y = { } };  // No CS1920
        i = new MemberInitializerTest { y = new List<int> { } };    // No CS1920
        i = new MemberInitializerTest { y = { { } } };  // CS1920
        List<List<int>> collection =  new List<List<int>>() { { } }; // CS1920
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (11,47): error CS1920: Element initializer cannot be empty
                //         i = new MemberInitializerTest { y = { { } } };  // CS1920
                Diagnostic(ErrorCode.ERR_EmptyElementInitializer, "{ }").WithLocation(11, 47),
                Diagnostic(ErrorCode.ERR_EmptyElementInitializer, "{ }"));
        }

        [Fact]
        public void CS1921ERR_InitializerAddHasWrongSignature()
        {
            var source = @"
using System.Collections.Generic;
using System.Collections;

class Test : IEnumerable
{
    public static void Add(int i) {}

    public static void Main()
    {
        var coll = new Test() { 1 };
    }

    List<object> list = new List<object>();
    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (11,33): error CS1921: The best overloaded method match for 'Test.Add(int)' has wrong signature for the initializer element. The initializable Add must be an accessible instance method.
                //         var coll = new Test() { 1 };
                Diagnostic(ErrorCode.ERR_InitializerAddHasWrongSignature, "1").WithArguments("Test.Add(int)").WithLocation(11, 33));
        }

        [Fact]
        public void CS1922ERR_CollectionInitRequiresIEnumerable()
        {
            var source = @"
class MemberInitializerTest
{
    public static int Main()
    {
        B coll = new B { 1 };           // CS1922
        var tc = new A { 1, ""hello"" }; // CS1922
        return 0;
    }
}

class B
{
    public B() { }
    public B(int i) { }
}

public class A
{
    public int Prop1 { get; set; }
    public string Prop2 { get; set; }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,24): error CS1922: Cannot initialize type 'B' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         B coll = new B { 1 };
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ 1 }").WithArguments("B").WithLocation(6, 24),
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, @"{ 1, ""hello"" }").WithArguments("A"));
        }

        [Fact]
        public void CS1922ERR_CollectionInitRequiresIEnumerable_02()
        {
            var source = @"
using System.Collections;
using System.Collections.Generic;
class MemberInitializerTest
{
    public static int Main()
    {
        B coll = new B { 1 };
        return 0;
    }
}

public class B
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
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,24): error CS1922: Cannot initialize type 'B' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         B coll = new B { 1 };
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ 1 }").WithArguments("B").WithLocation(8, 24));
        }

        [Fact]
        public void CS1922ERR_CollectionInitRequiresIEnumerable_InvalidInitializer()
        {
            var source = @"
public class MemberInitializerTest
{   
    public int y;
    public static void Main()
    {
        var i = new MemberInitializerTest { y++ };
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,43): error CS1922: Cannot initialize type 'MemberInitializerTest' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         var i = new MemberInitializerTest { y++ };
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ y++ }").WithArguments("MemberInitializerTest").WithLocation(7, 43),
                // (7,45): error CS0120: An object reference is required for the non-static field, method, or property 'MemberInitializerTest.y'
                //         var i = new MemberInitializerTest { y++ };
                Diagnostic(ErrorCode.ERR_ObjectRequired, "y").WithArguments("MemberInitializerTest.y").WithLocation(7, 45));
        }

        [Fact]
        public void CS1922ERR_CollectionInitRequiresIEnumerable_MethodCall()
        {
            var source = @"
public class MemberInitializerTest
{   
    public MemberInitializerTest Foo() {  return new MemberInitializerTest(); }
    public static void Main()
    {
        var i = new MemberInitializerTest() { Foo() = new MemberInitializerTest() };
    }    
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,45): error CS1922: Cannot initialize type 'MemberInitializerTest' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         var i = new MemberInitializerTest() { Foo() = new MemberInitializerTest() };
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ Foo() = new MemberInitializerTest() }").WithArguments("MemberInitializerTest").WithLocation(7, 45),
                // (7,47): error CS0747: Invalid initializer member declarator
                //         var i = new MemberInitializerTest() { Foo() = new MemberInitializerTest() };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "Foo() = new MemberInitializerTest()").WithLocation(7, 47),
                // (7,47): error CS0120: An object reference is required for the non-static field, method, or property 'MemberInitializerTest.Foo()'
                //         var i = new MemberInitializerTest() { Foo() = new MemberInitializerTest() };
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Foo").WithArguments("MemberInitializerTest.Foo()").WithLocation(7, 47));
        }

        [Fact]
        public void CS1950ERR_BadArgTypesForCollectionAdd()
        {
            var text = @"
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
        TestClass t = new TestClass { ""hi"" }; // CS1950
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
    // (14,39): error CS1950: The best overloaded Add method 'TestClass.Add(int)' for the collection initializer has some invalid arguments
    //         TestClass t = new TestClass { "hi" }; // CS1950
    Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, @"""hi""").WithArguments("TestClass.Add(int)"),
    // (14,39): error CS1503: Argument 1: cannot convert from 'string' to 'int'
    //         TestClass t = new TestClass { "hi" }; // CS1950
    Diagnostic(ErrorCode.ERR_BadArgType, @"""hi""").WithArguments("1", "string", "int"));
        }

        [Fact]
        public void CS1954ERR_InitializerAddHasParamModifiers()
        {
            var source = @"
using System.Collections.Generic;
using System.Collections;

class Test : IEnumerable
{
    public void Add(ref int i) {}

    public static void Main()
    {
        var coll = new Test() { 1 };
    }

    List<object> list = new List<object>();
    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (11,33): error CS1954: The best overloaded method match 'Test.Add(ref int)' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
                //         var coll = new Test() { 1 };
                Diagnostic(ErrorCode.ERR_InitializerAddHasParamModifiers, "1").WithArguments("Test.Add(ref int)").WithLocation(11, 33));
        }

        [Fact]
        public void CS1954ERR_InitializerAddHasParamModifiers02()
        {
            var text = @"
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

public class MyClass
{
    public string tree { get; set; }
}
class Program
{
    static void Main(string[] args)
    {
        MyList<MyClass> myList = new MyList<MyClass> { new MyClass { tree = ""maple"" } }; // CS1954
    }
}";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
    // (35,56): error CS1954: The best overloaded method match 'MyList<MyClass>.Add(ref MyClass)' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
    //         MyList<MyClass> myList = new MyList<MyClass> { new MyClass { tree = "maple" } }; // CS1954
    Diagnostic(ErrorCode.ERR_InitializerAddHasParamModifiers, @"new MyClass { tree = ""maple"" }").WithArguments("MyList<MyClass>.Add(ref MyClass)"),
    // (5,13): warning CS0649: Field 'MyList<T>._list' is never assigned to, and will always have its default value null
    //     List<T> _list;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "_list").WithArguments("MyList<T>._list", "null"));
        }

        [Fact]
        public void CS1958ERR_ObjectOrCollectionInitializerWithDelegateCreation()
        {
            var source = @"
public class MemberInitializerTest
{   
    delegate void D<T>();
    public static void GenericMethod<T>() { }
    public static void Main()
    {
        D<int> genD = new D<int>(GenericMethod<int>) { }; // CS1958
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,23): error CS1958: Object and collection initializer expressions may not be applied to a delegate creation expression
                //         D<int> genD = new D<int>(GenericMethod<int>) { }; // CS1958
                Diagnostic(ErrorCode.ERR_ObjectOrCollectionInitializerWithDelegateCreation, "new D<int>(GenericMethod<int>) { }").WithLocation(8, 23));
        }

        [Fact]
        public void CollectionInitializerTest_AddMethod_OverloadResolutionFailures()
        {
            var source = @"
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        B coll1 = new B { 1 };
        C coll2 = new C { 1 };
        D coll3 = new D { { 1, 2 } };
        E coll4 = new E { 1 };
        return 0;
    }
}

public class B : IEnumerable
{
    List<object> list = new List<object>();

    public B() {}

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}

public class C : IEnumerable
{
    List<object> list = new List<object>();

    public C() {}

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

public class D : IEnumerable
{
    List<object> list = new List<object>();

    public D() {}

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

public class E : IEnumerable
{
    List<object> list = new List<object>();

    public E() {}

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
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,27): error CS1061: 'B' does not contain a definition for 'Add' and no extension method 'Add' accepting a first argument of type 'B' could be found (are you missing a using directive or an assembly reference?)
                //         B coll1 = new B { 1 };
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "1").WithArguments("B", "Add").WithLocation(9, 27),
                // (10,27): error CS1950: The best overloaded Add method 'C.Add(string)' for the collection initializer has some invalid arguments
                //         C coll2 = new C { 1 };
                Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "1").WithArguments("C.Add(string)").WithLocation(10, 27),
                // (10,27): error CS1503: Argument 1: cannot convert from 'int' to 'string'
                //         C coll2 = new C { 1 };
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "string").WithLocation(10, 27),
                // (11,27): error CS0121: The call is ambiguous between the following methods or properties: 'D.Add(int, float)' and 'D.Add(float, int)'
                //         D coll3 = new D { { 1, 2 } };
                Diagnostic(ErrorCode.ERR_AmbigCall, "{ 1, 2 }").WithArguments("D.Add(int, float)", "D.Add(float, int)").WithLocation(11, 27),
                // (12,27): error CS0122: 'E.Add(int)' is inaccessible due to its protection level
                //         E coll4 = new E { 1 };
                Diagnostic(ErrorCode.ERR_BadAccess, "1").WithArguments("E.Add(int)").WithLocation(12, 27));
        }

        [WorkItem(543933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543933")]
        [Fact]
        public void ObjectInitializerTest_InvalidComplexElementInitializerExpression()
        {
            var source = @"
public class Test
{
    public int x;
}
class Program
{
    static void Main()
    {
        var p = new Test() { x = 1, { 1 }, { x = 1 } };
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,37): error CS0747: Invalid initializer member declarator
                //         var p = new Test() { x = 1, { 1 }, { x = 1 } };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "{ 1 }").WithLocation(10, 37),
                // (10,46): error CS0103: The name 'x' does not exist in the current context
                //         var p = new Test() { x = 1, { 1 }, { x = 1 } };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(10, 46),
                // (10,44): error CS0747: Invalid initializer member declarator
                //         var p = new Test() { x = 1, { 1 }, { x = 1 } };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "{ x = 1 }").WithLocation(10, 44));
        }

        [WorkItem(543933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543933")]
        [Fact]
        public void ObjectInitializerTest_IncompleteComplexElementInitializerExpression()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        var d = new Dictionary<object, object>()
        {
            {""s"", 1 },
        var x = 1;
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,13): error CS1003: Syntax error, ',' expected
                //         var x = 1;
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",", "").WithLocation(9, 13),
                // (9,18): error CS1513: } expected
                //         var x = 1;
                Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(9, 18),
                // (6,21): error CS0246: The type or namespace name 'Dictionary<,>' could not be found (are you missing a using directive or an assembly reference?)
                //         var d = new Dictionary<object, object>()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Dictionary<object, object>").WithArguments("Dictionary<,>").WithLocation(6, 21),
                // (8,13): error CS0747: Invalid initializer member declarator
                //             {"s", 1 },
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, @"{""s"", 1 }").WithLocation(8, 13),
                // (9,9): error CS0103: The name 'var' does not exist in the current context
                //         var x = 1;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(9, 9),
                // (9,9): error CS0747: Invalid initializer member declarator
                //         var x = 1;
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "var").WithLocation(9, 9)
                );
        }

        [WorkItem(543961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543961")]
        [Fact]
        public void CollectionInitializerTest_InvalidComplexElementInitializerSyntax()
        {
            var source = @"
class Test
{
  public static void Main()
  {
    new List<int>() { { { 1 } } };
  }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,25): error CS1513: } expected
                //     new List<int>() { { { 1 } } };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "{").WithLocation(6, 25),
                // (6,25): error CS1003: Syntax error, ',' expected
                //     new List<int>() { { { 1 } } };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",", "{").WithLocation(6, 25),
                // (6,33): error CS1002: ; expected
                //     new List<int>() { { { 1 } } };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(6, 33),
                // (6,34): error CS1597: Semicolon after method or accessor block is not valid
                //     new List<int>() { { { 1 } } };
                Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";").WithLocation(6, 34),
                // (8,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1),
                // (6,9): error CS0246: The type or namespace name 'List<>' could not be found (are you missing a using directive or an assembly reference?)
                //     new List<int>() { { { 1 } } };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "List<int>").WithArguments("List<>").WithLocation(6, 9),
                // (6,23): error CS1920: Element initializer cannot be empty
                //     new List<int>() { { { 1 } } };
                Diagnostic(ErrorCode.ERR_EmptyElementInitializer, "{ ").WithLocation(6, 23)
                );
        }

        [WorkItem(544484, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544484")]
        [Fact]
        public void EmptyCollectionInitPredefinedType()
        {
            var source = @"
class Program
{
    const int value = new int { };
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,23): error CS0133: The expression being assigned to 'Program.value' must be constant
                //     const int value = new int { };
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "new int { }").WithArguments("Program.value"));
        }

        [WorkItem(544349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544349")]
        [Fact]
        public void CollectionInitializerTest_Bug_12635()
        {
            var source = @"
using System.Collections.Generic;
 
class A
{
    static void Main()
    {
        var x = new List<int> { Count = { } };      // CS1918
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,33): error CS1918: Members of property 'System.Collections.Generic.List<int>.Count' of type 'int' cannot be assigned with an object initializer because it is of a value type
                //         var x = new List<int> { Count = { } };      // CS1918
                Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "Count").WithArguments("System.Collections.Generic.List<int>.Count", "int"));
        }

        [WorkItem(544349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544349")]
        [Fact]
        public void CollectionInitializerTest_Bug_12635_02()
        {
            var source = @"
namespace N
{
    public struct Struct { public int x; }
    public class Class { public int x; }

    public class C
    {
        public readonly Struct StructField;
        public Struct StructProp { get { return new Struct(); } }

        public readonly Class ClassField;
        public Class ClassProp { get { return new Class(); } }

        public static void Main()
        {
            var y = new C()
            {
                StructField = { },      // CS1917
                StructProp = { },       // CS1918
                ClassField = { },       // No error
                ClassProp = { }         // No error
            };
        }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (19,17): error CS1917: Members of readonly field 'N.C.StructField' of type 'N.Struct' cannot be assigned with an object initializer because it is of a value type
                //                 StructField = { },      // CS1917
                Diagnostic(ErrorCode.ERR_ReadonlyValueTypeInObjectInitializer, "StructField").WithArguments("N.C.StructField", "N.Struct"),
                // (20,17): error CS1918: Members of property 'N.C.StructProp' of type 'N.Struct' cannot be assigned with an object initializer because it is of a value type
                //                 StructProp = { },       // CS1918
                Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "StructProp").WithArguments("N.C.StructProp", "N.Struct"));
        }

        [WorkItem(544570, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544570")]
        [Fact]
        public void CollectionInitializerTest_Bug_12977()
        {
            var source = @"
using System.Collections.Generic;
using System.Collections;

static class Test
{
    static void Main()
    {
        var a = new A { 5, { 1, 2, {1, 2} }, 3 };
    }
}

public class A : IEnumerable
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
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,36): error CS1525: Invalid expression term '{'
                //         var a = new A { 5, { 1, 2, {1, 2} }, 3 };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{"),
                // (9,36): error CS1513: } expected
                //         var a = new A { 5, { 1, 2, {1, 2} }, 3 };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "{"),
                // (9,36): error CS1003: Syntax error, ',' expected
                //         var a = new A { 5, { 1, 2, {1, 2} }, 3 };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",", "{"),
                // (9,46): error CS1001: Identifier expected
                //         var a = new A { 5, { 1, 2, {1, 2} }, 3 };
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "3"),
                // (9,46): error CS1002: ; expected
                //         var a = new A { 5, { 1, 2, {1, 2} }, 3 };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "3"),
                // (9,48): error CS1002: ; expected
                //         var a = new A { 5, { 1, 2, {1, 2} }, 3 };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}"),
                // (9,49): error CS1597: Semicolon after method or accessor block is not valid
                //         var a = new A { 5, { 1, 2, {1, 2} }, 3 };
                Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";"),
                // (11,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}"));
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
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,21): error CS0826: No best type found for implicitly-typed array
                //         var array = new[] { Main() };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { Main() }"));
        }

        [Fact]
        public void AssignmentInOmittedCollectionElementInitializer()
        {
            var source = @"
using System.Collections;

partial class C : IEnumerable
{
    public IEnumerator GetEnumerator() { return null; }

    partial void M(int i);
    partial void Add(int i);

    static void Main()
    {
        int i, j, k;

        var c = new C { (i = 1) }; // NOTE: assignment is omitted.
        k = i;

        // Normal call, as reference.
        c.M(j = 3);
        k = j;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (16,13): error CS0165: Use of unassigned local variable 'i'
                //         k = i;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "i").WithArguments("i"),
                // (20,13): error CS0165: Use of unassigned local variable 'j'
                //         k = j;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "j").WithArguments("j"));
        }

        [Fact]
        public void OmittedCollectionElementInitializerInExpressionTree()
        {
            var source = @"
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
        Expression<Action> a = () => new C { 1 }; // Omitted element initializer.
        Expression<Action> b = () => new C().M(1); // Normal call, as reference.
    }
}";
            CreateCompilationWithMscorlibAndSystemCore(source).VerifyDiagnostics(
                // (15,46): error CS0765: Partial methods with only a defining declaration or removed conditional methods cannot be used in expression trees
                //         Expression<Action> a = () => new C { 1 }; // Omitted element initializer.
                Diagnostic(ErrorCode.ERR_PartialMethodInExpressionTree, "1"),
                // (16,38): error CS0765: Partial methods with only a defining declaration or removed conditional methods cannot be used in expression trees
                //         Expression<Action> b = () => new C().M(1); // Normal call, as reference.
                Diagnostic(ErrorCode.ERR_PartialMethodInExpressionTree, "new C().M(1)"));
        }

        [Fact]
        public void CS1918ERR_ValueTypePropertyInObjectInitializer_NonSpec_Dev10_Error()
        {
            // SPEC:    Nested object initializers cannot be applied to properties with a value type, or to read-only fields with a value type.

            // NOTE:    Dev10 compiler violates the specification here and applies this restriction to nested collection initializers as well.
            // NOTE:    Roslyn goes even further and requires a reference type (rather than "not a value type").  The rationale is that Add methods
            // NOTE:    nearly always manipulate the state of the collection, and those manipulations would be lost if the "this" argument was
            // NOTE:    just a copy.

            var source = @"
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
		get { return values[index];}
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
		B b = new B { a = {1, 2, 3} };
        b = new B { A = {4, 5, 6} };      // Dev10 incorrectly generates CS1918 here, we follow the spec and allow this to compile.
        return -1;
	}
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (47,21): error CS1918: Members of property 'B.A' of type 'A' cannot be assigned with an object initializer because it is of a value type
                //         b = new B { A = {4, 5, 6} };      // Dev10 incorrectly generates CS1918 here, we follow the spec and allow this to compile.
                Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "A").WithArguments("B.A", "A"));
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
            var compilation = CreateCompilationWithMscorlib(source);

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
            var compilation = CreateCompilationWithMscorlib(source);

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
            var compilation = CreateCompilationWithMscorlib(source);

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
            var compilation = CreateCompilationWithMscorlib(source);

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
            var compilation = CreateCompilationWithMscorlib(source);

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
            var compilation = CreateCompilationWithMscorlib(source);

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
            var compilation = CreateCompilationWithMscorlib(source);

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
            var compilation = CreateCompilationWithMscorlib(source);

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

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
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

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
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

            var compilation = CreateCompilationWithMscorlib(source);

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
    }
}
