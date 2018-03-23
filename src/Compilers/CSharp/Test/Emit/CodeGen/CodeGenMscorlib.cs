// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public partial class CodeGenMscorlibTests : EmitMetadataTestBase
    {
        [WorkItem(544591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544591")]
        [WorkItem(544609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544609")]
        [WorkItem(544595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544595")]
        [WorkItem(544596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544596")]
        [WorkItem(544624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544624")]
        [WorkItem(544592, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544592")]
        [WorkItem(544927, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544927")]
        [Fact]
        public void CoreLibrary1()
        {
            var text =
@"namespace System
{
    public class Object { }

    public abstract class ValueType { }

    public struct Void { }
    public struct Boolean { private Boolean m_value; Boolean Use(Boolean b) { m_value = b; return m_value; } }
    public struct Byte { private Byte m_value; Byte Use(Byte b) { m_value = b; return m_value; } }
    public struct Int16 { private Int16 m_value; Int16 Use(Int16 b) { m_value = b; return m_value; } }
    public struct Int32 { private Int32 m_value; Int32 Use(Int32 b) { m_value = b; return m_value; } }
    public struct Int64 { private Int64 m_value; Int64 Use(Int64 b) { m_value = b; return m_value; } }
    public struct UInt16 { private UInt16 m_value; UInt16 Use(UInt16 b) { m_value = b; return m_value; } }
    public struct UInt32 { private UInt32 m_value; UInt32 Use(UInt32 b) { m_value = b; return m_value; } }
    public struct UInt64 { private UInt64 m_value; UInt64 Use(UInt64 b) { m_value = b; return m_value; } }
    public struct Single { private Single m_value; Single Use(Single b) { m_value = b; return m_value; } }
    public struct Double { private Double m_value; Double Use(Double b) { m_value = b; return m_value; } }
    public struct Char { private Char m_value; Char Use(Char b) { m_value = b; return m_value; } }
    public struct SByte { private SByte m_value; SByte Use(SByte b) { m_value = b; return m_value; } }
    public struct UIntPtr { private UIntPtr m_value; UIntPtr Use(UIntPtr b) { m_value = b; return m_value; } }

    public struct IntPtr {
        unsafe private void* m_value;
        public unsafe IntPtr(void* value) { this.m_value = value; }
        unsafe void* Use() { return m_value; }
    }

    public class String { }
    public class Array { }
    public class Exception { }
    public class Type { }

    public abstract class Enum : ValueType { }
    public abstract class Delegate { }
    public abstract class MulticastDelegate : Delegate { }

    public abstract class Attribute
    {
        protected Attribute() { }
    }

    public struct Nullable<T> where T : struct
    {
        private bool hasValue;
        internal T value;

        public Nullable(T value)
        {
            this.value = value;
            this.hasValue = true;
        }

        public bool HasValue
        {
            get
            {
                return hasValue;
            }
        }

        public T Value
        {
            get
            {
                return value;
            }
        }

        public static implicit operator T?(T value)
        {
            return new Nullable<T>(value);
        }

        public static explicit operator T(T? value)
        {
            return value.Value;
        }
    }

    public class ParamArrayAttribute : Attribute { }
    public interface IDisposable { }
    public struct RuntimeTypeHandle { }
    public struct RuntimeFieldHandle { }

    public struct TypedReference
    {
        public static TypedReference MakeTypedReference()
        {
            return default(TypedReference);
        }
    }

    public struct ArgIterator
    {
        public TypedReference GetNextArg()
        {
            TypedReference result = new TypedReference ();
            unsafe
            {
                FCallGetNextArg (&result);
            }
            return result;
        }

        private static unsafe void FCallGetNextArg(void* result) { }
    }

    namespace Collections
    {
        public interface IEnumerable { }
        public interface IEnumerator { }
    }

    namespace Runtime.InteropServices
    {
        public class OutAttribute : Attribute { }
    }

    namespace Reflection
    {
        public class DefaultMemberAttribute : Attribute { }
    }
}";
            var c = CreateEmptyCompilation(text, options: TestOptions.UnsafeReleaseDll);

            c.VerifyDiagnostics();
        }

        [WorkItem(544918, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544918")]
        [Fact]
        public void CoreLibrary2()
        {
            var text =
@"class Program
{
    public static void Main(string[] args)
    {
    }

    [System.Security.Permissions.HostProtectionAttribute(UI = true)]
    public void M()
    {
    }
}";
            CreateCompilationWithMscorlib40(text).VerifyDiagnostics();
        }

        [WorkItem(546832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546832")]
        [Fact]
        public void CoreLibrary3()
        {
            var text =
@"namespace System
{
    public struct Nullable<T> where T : struct
    {
        public Nullable(T value) { }

        public static explicit operator T(T? value) { return default(T); }
        public static implicit operator T?(T value) { return default(T?); }
        public bool HasValue                  { get { return false; } }
        public T Value                        { get { return default(T); } }
        public T GetValueOrDefault()                { return default(T); }
        public T GetValueOrDefault(T defaultValue)  { return default(T); }
    }
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { private Boolean m_value; Boolean Use(Boolean b) { m_value = b; return m_value; } }
}";
            CreateEmptyCompilation(
                text,
                options: TestOptions.ReleaseDll)
            .VerifyDiagnostics();
        }

        /// <summary>
        /// Report CS0518 for missing System.Void
        /// when generating synthesized .ctor.
        /// </summary>
        [WorkItem(530859, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530859")]
        [Fact()]
        public void NoVoidForSynthesizedCtor()
        {
            var source =
@"namespace System
{
    public class Object { }
}";
            var compilation = CreateEmptyCompilation(source);
            compilation.VerifyEmitDiagnostics(
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion),
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Void")
                );
        }

        /// <summary>
        /// Report CS0656 for missing Decimal to int conversion.
        /// </summary>
        [WorkItem(530860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530860")]
        [Fact]
        public void NoDecimalConversion()
        {
            var source1 =
@"namespace System
{
    public class Object { }
    public struct Void { }
    public class ValueType { }
    public struct Int32 { }
    public struct Decimal { }
}";
            var compilation1 = CreateEmptyCompilation(source1, assemblyName: GetUniqueName());
            var reference1 = MetadataReference.CreateFromStream(compilation1.EmitToStream());
            var source2 =
@"class C
{
    static int M(decimal d)
    {
        return (int)d;
    }
}";
            var compilation2 = CreateEmptyCompilation(source2, new[] { reference1 });
            // Should report "CS0656: Missing compiler required member 'System.Decimal.op_Explicit_ToInt32'".
            // Instead, we report no errors and assert during emit.

            // no errors for compat reasons.
            compilation2.VerifyDiagnostics();

            // The bug has been resolved as Won't Fix for being extremely niche scenario and being a compat concern.
            // uncomment the following code if we are fixing this
            //var verifier = CompileAndVerify(compilation2);
        }

        [Fact, WorkItem(3593, "https://github.com/dotnet/roslyn/issues/3593")]
        public void NoTypedRef()
        {
            var source1 =
@"namespace System
{
    public class Object { }
    public struct Void { }
    public class ValueType { }
    public struct Int32 { }
    public struct Decimal { }
}";
            var compilation1 = CreateEmptyCompilation(source1, assemblyName: GetUniqueName());
            var reference1 = MetadataReference.CreateFromStream(compilation1.EmitToStream());
            var source2 =
@"    
public class C1
{
    public static T Read<T>()
    {
        T result = default(T);
        var refresult = __makeref(result);

        // ... method body

        return result;
    }
}
";
            var compilation2 = CreateEmptyCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
    // (7,25): error CS0518: Predefined type 'System.TypedReference' is not defined or imported
    //         var refresult = __makeref(result);
    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "__makeref(result)").WithArguments("System.TypedReference").WithLocation(7, 25)
);
        }

        [Fact, WorkItem(3746, "https://github.com/dotnet/roslyn/issues/3746")]
        public void NoTypedRefBox()
        {
            var source1 =
@"namespace System
{
    public class Object { }
    public struct Void { }
    public class ValueType { }
    public struct Int32 { }
    public struct Decimal { }
}";
            var compilation1 = CreateEmptyCompilation(source1, assemblyName: GetUniqueName());
            var reference1 = MetadataReference.CreateFromStream(compilation1.EmitToStream());
            var source2 =
@"    
public class C1
{
    public static object rrr;

    public static T Read<T>() where T : new()
    {
        T result = new T();
        var refresult = __makeref(result);
        rrr = refresult;
        return result;
    }
}
";
            var compilation2 = CreateEmptyCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
    // (9,25): error CS0518: Predefined type 'System.TypedReference' is not defined or imported
    //         var refresult = __makeref(result);
    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "__makeref(result)").WithArguments("System.TypedReference").WithLocation(9, 25)
);
        }

        [Fact, WorkItem(3746, "https://github.com/dotnet/roslyn/issues/3746")]
        public void NoTypedRefBox1()
        {
            var source1 =
@"namespace System
{
    public class Object { }
    public struct Void { }
    public class ValueType { }
    public struct Int32 { }
    public struct Decimal { }
    public struct TypedReference { }
}";
            var compilation1 = CreateEmptyCompilation(source1, assemblyName: GetUniqueName());
            var reference1 = MetadataReference.CreateFromStream(compilation1.EmitToStream());
            var source2 =
@"    
public class C1
{
    public static object rrr;

    public static T Read<T>() where T : new()
    {
        T result = new T();
        var refresult = __makeref(result);
        rrr = refresult;
        rrr = (object)__makeref(result);
        return result;
    }
}
";
            var compilation2 = CreateEmptyCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
    // (10,15): error CS0029: Cannot implicitly convert type 'System.TypedReference' to 'object'
    //         rrr = refresult;
    Diagnostic(ErrorCode.ERR_NoImplicitConv, "refresult").WithArguments("System.TypedReference", "object").WithLocation(10, 15),
    // (11,15): error CS0030: Cannot convert type 'System.TypedReference' to 'object'
    //         rrr = (object)__makeref(result);
    Diagnostic(ErrorCode.ERR_NoExplicitConv, "(object)__makeref(result)").WithArguments("System.TypedReference", "object").WithLocation(11, 15)
);
        }

        [Fact()]
        public void NoTypedRefBox_RefStruct()
        {
            var source1 =
@"namespace System
{
    public class Object { }
    public class String { }
    public struct Void { }
    public class ValueType { }
    public struct Int32 { }
    public struct Boolean { }
    public struct Decimal { }
    public class Attribute{ }
    public class ObsoleteAttribute: Attribute
    {
        public ObsoleteAttribute(string message, bool error){}
    }

    // Make the type ref struct. Should work just fine.
    public ref struct TypedReference { }
}";
            var compilation1 = CreateEmptyCompilation(source1, assemblyName: GetUniqueName());
            var reference1 = MetadataReference.CreateFromStream(compilation1.EmitToStream());
            var source2 =
@"    
public class C1
{
    public static object rrr;

    public static T Read<T>() where T : new()
    {
        T result = new T();
        var refresult = __makeref(result);
        rrr = refresult;
        rrr = (object)__makeref(result);
        return result;
    }
}
";
            var compilation2 = CreateEmptyCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
    // (10,15): error CS0029: Cannot implicitly convert type 'System.TypedReference' to 'object'
    //         rrr = refresult;
    Diagnostic(ErrorCode.ERR_NoImplicitConv, "refresult").WithArguments("System.TypedReference", "object").WithLocation(10, 15),
    // (11,15): error CS0030: Cannot convert type 'System.TypedReference' to 'object'
    //         rrr = (object)__makeref(result);
    Diagnostic(ErrorCode.ERR_NoExplicitConv, "(object)__makeref(result)").WithArguments("System.TypedReference", "object").WithLocation(11, 15)
);
        }

        [WorkItem(530861, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530861")]
        [Fact]
        public void MissingStringLengthForEach()
        {
            var source1 =
@"namespace System
{
    public class Object { }
    public struct Void { }
    public class ValueType { }
    public struct Boolean { }
    public class String : System.Collections.IEnumerable
    {
        public System.Collections.IEnumerator GetEnumerator() { return null; }
    }
    public interface IDisposable
    {
        void Dispose();
    }
}
namespace System.Collections
{
    public interface IEnumerable
    {
        IEnumerator GetEnumerator();
    }

    public interface IEnumerator
    {
        object Current { get; }
        bool MoveNext();
    }
}";
            var compilation1 = CreateEmptyCompilation(source1, assemblyName: GetUniqueName());
            var reference1 = MetadataReference.CreateFromStream(compilation1.EmitToStream());
            var source2 =
@"class C
{
    static void M(string s)
    {
        foreach (var c in s)
        {
            // comment
        }
    }
}";
            var compilation2 = CreateEmptyCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics();
            compilation2.Emit(new System.IO.MemoryStream()).Diagnostics.Verify(
                // (5,9): error CS0656: Missing compiler required member 'System.String.get_Length'
                //         foreach (var c in s)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"foreach (var c in s)
        {
            // comment
        }").WithArguments("System.String", "get_Length"),
                // (5,9): error CS0656: Missing compiler required member 'System.String.get_Chars'
                //         foreach (var c in s)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"foreach (var c in s)
        {
            // comment
        }").WithArguments("System.String", "get_Chars")
          );
        }

        [WorkItem(631443, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/631443")]
        [Fact]
        public void CoreLibrary4()
        {
            var text =
@"namespace System
{
    public struct Nullable<T> where T : struct
    {
        public Nullable(T value) { }

        public static explicit operator T(T? value) { return default(T); }
        public static implicit operator T?(T value) { return default(T?); }
        public bool HasValue                  { get { return false; } }
        public T Value                        { get { return default(T); } }
        public T GetValueOrDefault()                { return default(T); }
        public T GetValueOrDefault(T defaultValue)  { return default(T); }
    }
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { private Boolean m_value; Boolean Use(Boolean b) { m_value = b; return m_value; } }
    public struct Int32 { private Int32 m_value; Int32 Use(Int32 b) { m_value = b; return m_value; } }
    public struct Char {
        private Char m_value; Char Use(Char b) { m_value = b; return m_value; }
        public static implicit operator string(char c) { return default(string); }
    }
    public class String {
        public char CharAt(int i) { return default(char); }
    }

    internal class program
    {
        string M(string s)
        {
            return s.CharAt(1);
        }
    }
}";
            CreateEmptyCompilation(
                text,
                options: TestOptions.ReleaseDll)
            .VerifyDiagnostics();
        }

        [Fact]
        public void CoreLibraryInt32_m_value()
        {
            var text =
@"namespace System
{
    public class Object 
    { 
        public virtual bool Equals(Object obj) 
        {
            return this == obj;
        }

        public virtual int GetHashCode()
        {
            return 0;
        }
    }

    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { private Boolean m_value; Boolean Use(Boolean b) { m_value = b; return m_value; } }

    public struct Int32
    { 
        public Int32 m_value;

        public int CompareTo(int value) {
            // Need to use compare because subtraction will wrap
            // to positive for very large neg numbers, etc.
            if (m_value < value) return -1;
            if (m_value > value) return 1;
            return 0;
        }

        public override bool Equals(Object obj) {
            if (!(obj is Int32)) {
                return false;
            }
            return m_value == ((Int32)obj).m_value;
        }

        public override int GetHashCode()
        {
            // return m_value;    

            return m_value.m_value.m_value;
        }
    }

    internal class program
    {
        void Main()
        {
            int x = 42;
            x = x.CompareTo(x);
        }
    }
}";
            var comp = CreateEmptyCompilation(
                    text,
                    options: TestOptions.ReleaseDll)
                .VerifyDiagnostics();


            //IMPORTANT: we should NOT load fields of self-containing structs like - "ldfld int int.m_value"
            CompileAndVerify(comp, verify: Verification.Skipped).
                VerifyIL("int.CompareTo(int)", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldind.i4
  IL_0002:  ldarg.1
  IL_0003:  bge.s      IL_0007
  IL_0005:  ldc.i4.m1
  IL_0006:  ret
  IL_0007:  ldarg.0
  IL_0008:  ldind.i4
  IL_0009:  ldarg.1
  IL_000a:  ble.s      IL_000e
  IL_000c:  ldc.i4.1
  IL_000d:  ret
  IL_000e:  ldc.i4.0
  IL_000f:  ret
}
"
                ).
                VerifyIL("int.Equals(object)", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""int""
  IL_0006:  brtrue.s   IL_000a
  IL_0008:  ldc.i4.0
  IL_0009:  ret
  IL_000a:  ldarg.0
  IL_000b:  ldind.i4
  IL_000c:  ldarg.1
  IL_000d:  unbox.any  ""int""
  IL_0012:  ceq
  IL_0014:  ret
}
"
                ).
                VerifyIL("int.GetHashCode()", @"
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldind.i4
  IL_0002:  ret
}
"
                );
        }

        [Fact]
        public void Enum_GetHashCode()
        {
            var text =
@"
using System;

namespace System
{
    public class Object 
    { 
        public virtual bool Equals(Object obj) 
        {
            return this == obj;
        }

        public virtual int GetHashCode()
        {
            return 0;
        }
    }

    public abstract class ValueType { }

    public struct Void { }
    public struct Boolean { private Boolean m_value; Boolean Use(Boolean b) { m_value = b; return m_value; } }

    public struct Int32
    { 
        public Int32 m_value;

        public override bool Equals(Object obj) {
            if (!(obj is Int32)) {
                return false;
            }
            return m_value == ((Int32)obj).m_value;
        }

        public override int GetHashCode()
        {
            return 1;
        }
    }

    public abstract class Enum : ValueType 
    { 
        public override int GetHashCode()
        {
            return 42;
        }
    }

    enum E1
    {
        e
    }
}

    internal class program
    {
        void Main()
        {
            var i = 123;
            var e = (E1)i;

            var o = (object)i;

            if (i.GetHashCode() == e.GetHashCode())
            {
                i = i / 0;   // crash here
            }

            if (o.GetHashCode() != e.GetHashCode())
            {
                i = i / 0;   // crash here
            }
        }
    }
";
            var comp = CreateEmptyCompilation(
                    text,
                    options: TestOptions.ReleaseDll)
                .VerifyDiagnostics();


            //IMPORTANT: we should NOT delegate E1.GetHashCode() to int.GetHashCode()
            //           it is entirely possible that Enum.GetHashCode and int.GetHashCode 
            //           have different implementations
            CompileAndVerify(comp, verify: Verification.Fails).
                VerifyIL("program.Main()",
@"
{
  // Code size       62 (0x3e)
  .maxstack  3
  .locals init (int V_0, //i
  System.E1 V_1) //e
  IL_0000:  ldc.i4.s   123
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  stloc.1
  IL_0005:  ldloc.0
  IL_0006:  box        ""int""
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       ""int int.GetHashCode()""
  IL_0012:  ldloca.s   V_1
  IL_0014:  constrained. ""System.E1""
  IL_001a:  callvirt   ""int object.GetHashCode()""
  IL_001f:  bne.un.s   IL_0025
  IL_0021:  ldloc.0
  IL_0022:  ldc.i4.0
  IL_0023:  div
  IL_0024:  stloc.0
  IL_0025:  callvirt   ""int object.GetHashCode()""
  IL_002a:  ldloca.s   V_1
  IL_002c:  constrained. ""System.E1""
  IL_0032:  callvirt   ""int object.GetHashCode()""
  IL_0037:  beq.s      IL_003d
  IL_0039:  ldloc.0
  IL_003a:  ldc.i4.0
  IL_003b:  div
  IL_003c:  stloc.0
  IL_003d:  ret
}
"
                );
        }

        [Fact]
        public void CoreLibraryIntPtr_m_value()
        {
            var text =
@"namespace System
{
    public class Object 
    { 
        public virtual bool Equals(Object obj) 
        {
            return this == obj;
        }

        public virtual int GetHashCode()
        {
            return 0;
        }
    }

    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { private Boolean m_value; Boolean Use(Boolean b) { m_value = b; return m_value; } }
    public struct Int32 { private Int32 m_value; Int32 Use(Int32 b) { m_value = b; return m_value; } }
    public struct Int64 { private Int64 m_value; Int64 Use(Int64 b) { m_value = b; return m_value; } }

    public class Delegate{}

    public struct IntPtr
    { 
        unsafe private void* m_value;

        public unsafe IntPtr(int value)
        {
            m_value = (void *)(long)value;
        }
    
        public unsafe override bool Equals(Object obj) {
            if (obj is IntPtr) {
                return (m_value == ((IntPtr)obj).m_value);
            }
            return false;
        }

        public unsafe static bool operator == (IntPtr value1, IntPtr value2) 
        {
            return value1.m_value == value2.m_value;
        }

        public unsafe static bool operator != (IntPtr value1, IntPtr value2) 
        {
            return value1.m_value != value2.m_value;
        }

        public unsafe override int GetHashCode() {
            return unchecked((int)((long)m_value));
        }

        public unsafe static IntPtr Goo() 
        {
            return new IntPtr(0);
        }

        public unsafe static bool Bar(IntPtr value1) 
        {
            return value1.m_value == Goo().m_value;
        }
    }

    internal class program
    {
        void Main()
        {
            var x = new IntPtr(42);
        }
    }
}";
            var comp = CreateEmptyCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();

            //IMPORTANT: we should NOT load fields of clr-confusing structs off the field value.
            //           the field should be loaded off the reference like in 
            //           the following snippet  (note ldargA, not ldarg) -
            //      IL_0000:  ldarga.s   V_0
            //      IL_0002:  ldfld      ""void* System.IntPtr.m_value""
            //
            //           it may seem redundant since in general we can load the filed off the value
            //           but see the bug see VSW #396011, JIT needs references when loading
            //           fields of certain clr-ambiguous structs (only possible when building mscorlib)

            CompileAndVerify(comp, verify: Verification.Fails).
                VerifyIL("System.IntPtr..ctor(int)", @"
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  conv.i8
  IL_0003:  conv.u
  IL_0004:  stfld      ""void* System.IntPtr.m_value""
  IL_0009:  ret
}
"
).VerifyIL("System.IntPtr.Equals(object)", @"
{
  // Code size       30 (0x1e)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""System.IntPtr""
  IL_0006:  brfalse.s  IL_001c
  IL_0008:  ldarg.0
  IL_0009:  ldfld      ""void* System.IntPtr.m_value""
  IL_000e:  ldarg.1
  IL_000f:  unbox      ""System.IntPtr""
  IL_0014:  ldfld      ""void* System.IntPtr.m_value""
  IL_0019:  ceq
  IL_001b:  ret
  IL_001c:  ldc.i4.0
  IL_001d:  ret
}
"
).VerifyIL("System.IntPtr.GetHashCode()", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""void* System.IntPtr.m_value""
  IL_0006:  conv.u8
  IL_0007:  conv.i4
  IL_0008:  ret
}
"
).VerifyIL("bool System.IntPtr.op_Equality(System.IntPtr, System.IntPtr)",
@"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldfld      ""void* System.IntPtr.m_value""
  IL_0007:  ldarga.s   V_1
  IL_0009:  ldfld      ""void* System.IntPtr.m_value""
  IL_000e:  ceq
  IL_0010:  ret
}
").VerifyIL("bool System.IntPtr.op_Inequality(System.IntPtr, System.IntPtr)",
@"
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldfld      ""void* System.IntPtr.m_value""
  IL_0007:  ldarga.s   V_1
  IL_0009:  ldfld      ""void* System.IntPtr.m_value""
  IL_000e:  ceq
  IL_0010:  ldc.i4.0
  IL_0011:  ceq
  IL_0013:  ret
}
").VerifyIL("System.IntPtr.Bar(System.IntPtr)",
@"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (System.IntPtr V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldfld      ""void* System.IntPtr.m_value""
  IL_0007:  call       ""System.IntPtr System.IntPtr.Goo()""
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  ldfld      ""void* System.IntPtr.m_value""
  IL_0014:  ceq
  IL_0016:  ret
}
");
        }

        [Fact]
        public void FixedHelpersOnStringAndArray()
        {
            var text =
@"
namespace System
{
    public struct Void { }
    public class ValueType { }
    public struct Int32 { }
    public struct Char { }
    public struct Boolean { }

    public class String 
    { 
        public ref char GetPinnableReference() => throw null;
    }

    public class Object 
    { 
    }

    public class Array 
    { 
        public ref char GetPinnableReference() => throw null;
    }
}
  
unsafe internal class program
{
    public static void Main()
    {
        fixed (char* p = ""A"")
        {
            *p = default;
        }
    }

    public static void Main1()
    {
        fixed (char* p = new char[1])
        {
            *p = default;
        }
    }
}
";
            var comp = CreateEmptyCompilation(text, options: TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics();

            var c = CompileAndVerify(comp, verify: Verification.Skipped);

            c.VerifyIL("program.Main()", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (pinned char& V_0)
  IL_0000:  ldstr      ""A""
  IL_0005:  call       ""ref char string.GetPinnableReference()""
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  conv.u
  IL_000d:  ldc.i4.0
  IL_000e:  stind.i2
  IL_000f:  ldc.i4.0
  IL_0010:  conv.u
  IL_0011:  stloc.0
  IL_0012:  ret
}
"
                );

            c.VerifyIL("program.Main1()", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned char[] V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""char""
  IL_0006:  dup
  IL_0007:  stloc.1
  IL_0008:  brfalse.s  IL_000f
  IL_000a:  ldloc.1
  IL_000b:  ldlen
  IL_000c:  conv.i4
  IL_000d:  brtrue.s   IL_0014
  IL_000f:  ldc.i4.0
  IL_0010:  conv.u
  IL_0011:  stloc.0
  IL_0012:  br.s       IL_001d
  IL_0014:  ldloc.1
  IL_0015:  ldc.i4.0
  IL_0016:  ldelema    ""char""
  IL_001b:  conv.u
  IL_001c:  stloc.0
  IL_001d:  ldloc.0
  IL_001e:  ldc.i4.0
  IL_001f:  stind.i2
  IL_0020:  ldnull
  IL_0021:  stloc.1
  IL_0022:  ret
}
");
        }

        [Fact]
        public void FixedHelperOnObject()
        {
            var text =
@"
namespace System
{
    public struct Void { }
    public class ValueType { }
    public struct Int32 { }
    public struct Char { }
    public struct Boolean { }

    public class String 
    { 
    }

    public class Object 
    { 
        public ref char GetPinnableReference() => throw null;
    }

    public class Array 
    { 
    }
}

namespace System.Runtime.CompilerServices
{
    public class RuntimeHelpers
    {
        public static int OffsetToStringData => 0;
    }
}
  
unsafe internal class program
{
    public static void Main()
    {
        fixed (char* p = ""A"")
        {
            *p = default;
        }

        fixed (char* p = new char[1])
        {
            *p = default;
        }

        fixed (char* p = 42)
        {
            *p = default;
        }
    }
}
";
            var comp = CreateEmptyCompilation(text, options: TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics();

            CompileAndVerify(comp, verify: Verification.Skipped).
                VerifyIL("program.Main()", @"
{
  // Code size       83 (0x53)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1,
                char* V_2, //p
                pinned char[] V_3,
                pinned char& V_4)
  IL_0000:  ldstr      ""A""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  conv.u
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  brfalse.s  IL_0014
  IL_000c:  ldloc.0
  IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.0
  IL_0016:  stind.i2
  IL_0017:  ldnull
  IL_0018:  stloc.1
  IL_0019:  ldc.i4.1
  IL_001a:  newarr     ""char""
  IL_001f:  dup
  IL_0020:  stloc.3
  IL_0021:  brfalse.s  IL_0028
  IL_0023:  ldloc.3
  IL_0024:  ldlen
  IL_0025:  conv.i4
  IL_0026:  brtrue.s   IL_002d
  IL_0028:  ldc.i4.0
  IL_0029:  conv.u
  IL_002a:  stloc.2
  IL_002b:  br.s       IL_0036
  IL_002d:  ldloc.3
  IL_002e:  ldc.i4.0
  IL_002f:  ldelema    ""char""
  IL_0034:  conv.u
  IL_0035:  stloc.2
  IL_0036:  ldloc.2
  IL_0037:  ldc.i4.0
  IL_0038:  stind.i2
  IL_0039:  ldnull
  IL_003a:  stloc.3
  IL_003b:  ldc.i4.s   42
  IL_003d:  box        ""int""
  IL_0042:  call       ""ref char object.GetPinnableReference()""
  IL_0047:  stloc.s    V_4
  IL_0049:  ldloc.s    V_4
  IL_004b:  conv.u
  IL_004c:  ldc.i4.0
  IL_004d:  stind.i2
  IL_004e:  ldc.i4.0
  IL_004f:  conv.u
  IL_0050:  stloc.s    V_4
  IL_0052:  ret
}
"
                );
        }
    }
}
