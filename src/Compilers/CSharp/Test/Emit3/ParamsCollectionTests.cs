// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class ParamsCollectionTests : CompilingTestBase
    {
        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/73743")]
        public void ParamsSpanInExpression_01()
        {
            string source = """
class Program
{
    public static void Test()
    {
        System.Linq.Expressions.Expression<System.Action<string>> e = (s) => M(s, s, s);
    }

    static void M(params string[] p) {}
    static void M(params System.Span<string> p) {}
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (5,78): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'Span'.
                //         System.Linq.Expressions.Expression<System.Action<string>> e = (s) => M(s, s, s);
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "M(s, s, s)").WithArguments("Span").WithLocation(5, 78),
                // (5,78): error CS9226: An expression tree may not contain an expanded form of non-array params collection parameter.
                //         System.Linq.Expressions.Expression<System.Action<string>> e = (s) => M(s, s, s);
                Diagnostic(ErrorCode.ERR_ParamsCollectionExpressionTree, "M(s, s, s)").WithLocation(5, 78)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/73743")]
        public void ParamsSpanInExpression_02()
        {
            string source = """
class Program
{
    public static void Test()
    {
        System.Linq.Expressions.Expression<System.Action<string>> e = (s) => M(s, s, s);
    }

    static void M(params System.Span<string> p) {}
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (5,78): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'Span'.
                //         System.Linq.Expressions.Expression<System.Action<string>> e = (s) => M(s, s, s);
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "M(s, s, s)").WithArguments("Span").WithLocation(5, 78),
                // (5,78): error CS9226: An expression tree may not contain an expanded form of non-array params collection parameter.
                //         System.Linq.Expressions.Expression<System.Action<string>> e = (s) => M(s, s, s);
                Diagnostic(ErrorCode.ERR_ParamsCollectionExpressionTree, "M(s, s, s)").WithLocation(5, 78)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/74163")]
        public void StringInterpolation_01()
        {
            string source1 = """
class Program
{
    public static void Test1()
    {
        System.Linq.Expressions.Expression<System.Func<string, string>> e = (s) => $"{s} {2} {s} {4}";
    }

    public static void Test2()
    {
        System.Linq.Expressions.Expression<System.Func<string, System.FormattableString>> e = (s) => $"{s} {2} {s} {4}";
    }
}
""";
            string core = """
namespace System
{
    public class Object {}

    public class ValueType {}
    public abstract partial class Enum {}

    public struct Void {}
    public struct Boolean {}
    public struct Byte {}
    public struct Int16 {}
    public struct Int32 {}
    public struct Int64 {}
    public struct IntPtr {}

    public partial class Exception {}

    public class String
    {
        public static string Format(string format, params object[] args) => null;
        public static string Format(string format, params ReadOnlySpan<object> args) => null;
    }

    public abstract class FormattableString {}

    public abstract partial class Attribute {}
    public sealed class ParamArrayAttribute : Attribute {}

    public enum AttributeTargets
    {
        Assembly = 1,
        Module = 2,
        Class = 4,
        Struct = 8,
        Enum = 16,
        Constructor = 32,
        Method = 64,
        Property = 128,
        Field = 256,
        Event = 512,
        Interface = 1024,
        Parameter = 2048,
        Delegate = 4096,
        ReturnValue = 8192,
        GenericParameter = 16384,
        All = 32767
    }

    public sealed class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn)
        {
        }

        internal AttributeUsageAttribute(AttributeTargets validOn, bool allowMultiple, bool inherited)
        {
        }

        public AttributeTargets ValidOn {get; set;}

        public bool AllowMultiple {get; set;}

        public bool Inherited {get; set;}
    }

    public abstract partial class Delegate {}
    public abstract partial class MulticastDelegate : Delegate {}

    public delegate TResult Func<in T, out TResult>(T arg); 

    public interface IDisposable
    {
        void Dispose();
    }

    public partial struct Nullable<T> where T : struct {}

    public unsafe partial struct RuntimeTypeHandle {}
    public unsafe partial struct RuntimeMethodHandle {}
    public abstract partial class Type
    {
        public static Type GetTypeFromHandle(RuntimeTypeHandle handle) => null;
    }

    public ref struct ReadOnlySpan<T>
    {
        public ReadOnlySpan(T[] array)
        {
        }

        public ReadOnlySpan(T[] array, int start, int length)
        {
        }

        public unsafe ReadOnlySpan(void* pointer, int length)
        {
        }
    }
}

namespace System.Collections
{
    public interface IEnumerator
    {
        object Current { get; }
        bool MoveNext();
        void Reset();
    }

    public interface IEnumerable
    {
        IEnumerator GetEnumerator();
    }
}

namespace System.Collections.Generic
{
    public interface IEnumerator<out T> : IEnumerator, IDisposable
    {
        new T Current { get; }
    }

    public interface IEnumerable<out T> : IEnumerable
    {
        new IEnumerator<T> GetEnumerator();
    }
}

namespace System.Reflection
{
    public abstract unsafe partial class MethodBase
    {
        public static MethodBase GetMethodFromHandle(RuntimeMethodHandle handle) => null;
        public static MethodBase GetMethodFromHandle(RuntimeMethodHandle handle, RuntimeTypeHandle declaringType) => null;
    }

    public abstract partial class MethodInfo : MethodBase {}

    public abstract partial class ConstructorInfo : MethodBase {}
}

namespace System.Linq.Expressions
{
    using System.Collections.Generic;
    using System.Reflection;

    public partial class Expression
    {
        public static ParameterExpression Parameter(Type type) => null;
        public static ParameterExpression Parameter(Type type, string name) => null;
        public static ConstantExpression Constant(object value) => null;
        public static ConstantExpression Constant(object value, Type type) => null;

        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, params ParameterExpression[] parameters) => null;
        public static NewArrayExpression NewArrayInit(Type type, params Expression[] initializers) => null;
        public static NewExpression New(ConstructorInfo constructor, IEnumerable<Expression> arguments) => null;
        public static MethodCallExpression Call(Expression instance, MethodInfo method, params Expression[] arguments) => null;
        public static UnaryExpression Convert(Expression expression, Type type) => null;
    }

    public abstract class LambdaExpression : Expression {}

    public class Expression<TDelegate> : LambdaExpression {}

    public class ParameterExpression : Expression {}
    public class ConstantExpression : Expression {}
    public class NewArrayExpression : Expression {}
    public class NewExpression : Expression {}
    public class MethodCallExpression : Expression {}
    public class UnaryExpression : Expression {}
}

namespace System.Runtime.CompilerServices
{
    public sealed class InlineArrayAttribute : Attribute
    {
        public InlineArrayAttribute(int length)
        {
        }
    }

    public sealed class IsReadOnlyAttribute : Attribute
    {}

    public static class FormattableStringFactory
    {
        public static FormattableString Create(string format, params object[] arguments) => null;
        public static FormattableString Create(string format, params ReadOnlySpan<object> arguments) => null;
    }

    public static unsafe partial class Unsafe
    {
        public static ref TTo As<TFrom, TTo>(ref TFrom source) => throw null;
        public static ref T AsRef<T>(scoped ref readonly T source) => throw null;
        public static ref T Add<T>(ref T source, int elementOffset) => throw null;
    }
}

namespace System.Runtime.InteropServices
{
    public static partial class MemoryMarshal
    {
        public static ReadOnlySpan<T> CreateReadOnlySpan<T>(ref readonly T reference, int length) => default;
    }
}
""";

            var comp = CreateEmptyCompilation([source1, core], options: TestOptions.ReleaseDll.WithAllowUnsafe(true));
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped).VerifyDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1)
                );

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size      198 (0xc6)
  .maxstack  11
  .locals init (System.Linq.Expressions.ParameterExpression V_0)
  IL_0000:  ldtoken    ""string""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""s""
  IL_000f:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0014:  stloc.0
  IL_0015:  ldnull
  IL_0016:  ldtoken    ""string string.Format(string, params object[])""
  IL_001b:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0020:  castclass  ""System.Reflection.MethodInfo""
  IL_0025:  ldc.i4.2
  IL_0026:  newarr     ""System.Linq.Expressions.Expression""
  IL_002b:  dup
  IL_002c:  ldc.i4.0
  IL_002d:  ldstr      ""{0} {1} {2} {3}""
  IL_0032:  ldtoken    ""string""
  IL_0037:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_003c:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_0041:  stelem.ref
  IL_0042:  dup
  IL_0043:  ldc.i4.1
  IL_0044:  ldtoken    ""object""
  IL_0049:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_004e:  ldc.i4.4
  IL_004f:  newarr     ""System.Linq.Expressions.Expression""
  IL_0054:  dup
  IL_0055:  ldc.i4.0
  IL_0056:  ldloc.0
  IL_0057:  stelem.ref
  IL_0058:  dup
  IL_0059:  ldc.i4.1
  IL_005a:  ldc.i4.2
  IL_005b:  box        ""int""
  IL_0060:  ldtoken    ""int""
  IL_0065:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_006a:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_006f:  ldtoken    ""object""
  IL_0074:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0079:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type)""
  IL_007e:  stelem.ref
  IL_007f:  dup
  IL_0080:  ldc.i4.2
  IL_0081:  ldloc.0
  IL_0082:  stelem.ref
  IL_0083:  dup
  IL_0084:  ldc.i4.3
  IL_0085:  ldc.i4.4
  IL_0086:  box        ""int""
  IL_008b:  ldtoken    ""int""
  IL_0090:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0095:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_009a:  ldtoken    ""object""
  IL_009f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00a4:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type)""
  IL_00a9:  stelem.ref
  IL_00aa:  call       ""System.Linq.Expressions.NewArrayExpression System.Linq.Expressions.Expression.NewArrayInit(System.Type, params System.Linq.Expressions.Expression[])""
  IL_00af:  stelem.ref
  IL_00b0:  call       ""System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])""
  IL_00b5:  ldc.i4.1
  IL_00b6:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_00bb:  dup
  IL_00bc:  ldc.i4.0
  IL_00bd:  ldloc.0
  IL_00be:  stelem.ref
  IL_00bf:  call       ""System.Linq.Expressions.Expression<System.Func<string, string>> System.Linq.Expressions.Expression.Lambda<System.Func<string, string>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_00c4:  pop
  IL_00c5:  ret
}
");

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size      198 (0xc6)
  .maxstack  11
  .locals init (System.Linq.Expressions.ParameterExpression V_0)
  IL_0000:  ldtoken    ""string""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""s""
  IL_000f:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0014:  stloc.0
  IL_0015:  ldnull
  IL_0016:  ldtoken    ""System.FormattableString System.Runtime.CompilerServices.FormattableStringFactory.Create(string, params object[])""
  IL_001b:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0020:  castclass  ""System.Reflection.MethodInfo""
  IL_0025:  ldc.i4.2
  IL_0026:  newarr     ""System.Linq.Expressions.Expression""
  IL_002b:  dup
  IL_002c:  ldc.i4.0
  IL_002d:  ldstr      ""{0} {1} {2} {3}""
  IL_0032:  ldtoken    ""string""
  IL_0037:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_003c:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_0041:  stelem.ref
  IL_0042:  dup
  IL_0043:  ldc.i4.1
  IL_0044:  ldtoken    ""object""
  IL_0049:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_004e:  ldc.i4.4
  IL_004f:  newarr     ""System.Linq.Expressions.Expression""
  IL_0054:  dup
  IL_0055:  ldc.i4.0
  IL_0056:  ldloc.0
  IL_0057:  stelem.ref
  IL_0058:  dup
  IL_0059:  ldc.i4.1
  IL_005a:  ldc.i4.2
  IL_005b:  box        ""int""
  IL_0060:  ldtoken    ""int""
  IL_0065:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_006a:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_006f:  ldtoken    ""object""
  IL_0074:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0079:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type)""
  IL_007e:  stelem.ref
  IL_007f:  dup
  IL_0080:  ldc.i4.2
  IL_0081:  ldloc.0
  IL_0082:  stelem.ref
  IL_0083:  dup
  IL_0084:  ldc.i4.3
  IL_0085:  ldc.i4.4
  IL_0086:  box        ""int""
  IL_008b:  ldtoken    ""int""
  IL_0090:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0095:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_009a:  ldtoken    ""object""
  IL_009f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00a4:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type)""
  IL_00a9:  stelem.ref
  IL_00aa:  call       ""System.Linq.Expressions.NewArrayExpression System.Linq.Expressions.Expression.NewArrayInit(System.Type, params System.Linq.Expressions.Expression[])""
  IL_00af:  stelem.ref
  IL_00b0:  call       ""System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])""
  IL_00b5:  ldc.i4.1
  IL_00b6:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_00bb:  dup
  IL_00bc:  ldc.i4.0
  IL_00bd:  ldloc.0
  IL_00be:  stelem.ref
  IL_00bf:  call       ""System.Linq.Expressions.Expression<System.Func<string, System.FormattableString>> System.Linq.Expressions.Expression.Lambda<System.Func<string, System.FormattableString>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_00c4:  pop
  IL_00c5:  ret

}
");

            string source2 = """
class Program
{
    public static string Test1(string s) => $"{s} {2} {s} {4}";

    public static System.FormattableString Test2(string s) => $"{s} {2} {s} {4}";
}
""";
            comp = CreateEmptyCompilation([source2, core], options: TestOptions.ReleaseDll.WithAllowUnsafe(true));
            verifier = CompileAndVerify(comp, verify: Verification.Skipped).VerifyDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1)
                );

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       77 (0x4d)
  .maxstack  3
  .locals init (<>y__InlineArray4<object> V_0)
  IL_0000:  ldstr      ""{0} {1} {2} {3}""
  IL_0005:  ldloca.s   V_0
  IL_0007:  initobj    ""<>y__InlineArray4<object>""
  IL_000d:  ldloca.s   V_0
  IL_000f:  ldc.i4.0
  IL_0010:  call       ""ref object <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray4<object>, object>(ref <>y__InlineArray4<object>, int)""
  IL_0015:  ldarg.0
  IL_0016:  stind.ref
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldc.i4.1
  IL_001a:  call       ""ref object <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray4<object>, object>(ref <>y__InlineArray4<object>, int)""
  IL_001f:  ldc.i4.2
  IL_0020:  box        ""int""
  IL_0025:  stind.ref
  IL_0026:  ldloca.s   V_0
  IL_0028:  ldc.i4.2
  IL_0029:  call       ""ref object <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray4<object>, object>(ref <>y__InlineArray4<object>, int)""
  IL_002e:  ldarg.0
  IL_002f:  stind.ref
  IL_0030:  ldloca.s   V_0
  IL_0032:  ldc.i4.3
  IL_0033:  call       ""ref object <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray4<object>, object>(ref <>y__InlineArray4<object>, int)""
  IL_0038:  ldc.i4.4
  IL_0039:  box        ""int""
  IL_003e:  stind.ref
  IL_003f:  ldloca.s   V_0
  IL_0041:  ldc.i4.4
  IL_0042:  call       ""System.ReadOnlySpan<object> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<<>y__InlineArray4<object>, object>(in <>y__InlineArray4<object>, int)""
  IL_0047:  call       ""string string.Format(string, params System.ReadOnlySpan<object>)""
  IL_004c:  ret
}
");

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       77 (0x4d)
  .maxstack  3
  .locals init (<>y__InlineArray4<object> V_0)
  IL_0000:  ldstr      ""{0} {1} {2} {3}""
  IL_0005:  ldloca.s   V_0
  IL_0007:  initobj    ""<>y__InlineArray4<object>""
  IL_000d:  ldloca.s   V_0
  IL_000f:  ldc.i4.0
  IL_0010:  call       ""ref object <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray4<object>, object>(ref <>y__InlineArray4<object>, int)""
  IL_0015:  ldarg.0
  IL_0016:  stind.ref
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldc.i4.1
  IL_001a:  call       ""ref object <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray4<object>, object>(ref <>y__InlineArray4<object>, int)""
  IL_001f:  ldc.i4.2
  IL_0020:  box        ""int""
  IL_0025:  stind.ref
  IL_0026:  ldloca.s   V_0
  IL_0028:  ldc.i4.2
  IL_0029:  call       ""ref object <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray4<object>, object>(ref <>y__InlineArray4<object>, int)""
  IL_002e:  ldarg.0
  IL_002f:  stind.ref
  IL_0030:  ldloca.s   V_0
  IL_0032:  ldc.i4.3
  IL_0033:  call       ""ref object <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray4<object>, object>(ref <>y__InlineArray4<object>, int)""
  IL_0038:  ldc.i4.4
  IL_0039:  box        ""int""
  IL_003e:  stind.ref
  IL_003f:  ldloca.s   V_0
  IL_0041:  ldc.i4.4
  IL_0042:  call       ""System.ReadOnlySpan<object> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<<>y__InlineArray4<object>, object>(in <>y__InlineArray4<object>, int)""
  IL_0047:  call       ""System.FormattableString System.Runtime.CompilerServices.FormattableStringFactory.Create(string, params System.ReadOnlySpan<object>)""
  IL_004c:  ret
}
");
        }
    }
}
