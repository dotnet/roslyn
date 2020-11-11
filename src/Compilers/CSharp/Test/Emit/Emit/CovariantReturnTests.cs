// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class CovariantReturnTests : EmitMetadataTestBase
    {
        private static MetadataReference _corelibraryWithCovariantReturnSupport;
        private static MetadataReference CorelibraryWithCovariantReturnSupport
        {
            get
            {
                if (_corelibraryWithCovariantReturnSupport == null)
                {
                    _corelibraryWithCovariantReturnSupport = MakeCorelibraryWithCovariantReturnSupport();
                }

                return _corelibraryWithCovariantReturnSupport;
            }
        }

        private static MetadataReference MakeCorelibraryWithCovariantReturnSupport()
        {
            const string corLibraryCore = @"
namespace System
{
    public class Array
    {
        public static T[] Empty<T>() => throw null;
    }
    public class Console
    {
        public static void WriteLine(string message) => throw null;
    }
    public class Attribute { }
    [Flags]
    public enum AttributeTargets
    {
        Assembly = 0x1,
        Module = 0x2,
        Class = 0x4,
        Struct = 0x8,
        Enum = 0x10,
        Constructor = 0x20,
        Method = 0x40,
        Property = 0x80,
        Field = 0x100,
        Event = 0x200,
        Interface = 0x400,
        Parameter = 0x800,
        Delegate = 0x1000,
        ReturnValue = 0x2000,
        GenericParameter = 0x4000,
        All = 0x7FFF
    }
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn) { }
        public bool AllowMultiple
        {
            get => throw null;
            set { }
        }
        public bool Inherited
        {
            get => throw null;
            set { }
        }
        public AttributeTargets ValidOn => throw null;
    }
    public struct Boolean { }
    public struct Byte { }
    public class Delegate
    {
        public static Delegate CreateDelegate(Type type, object firstArgument, Reflection.MethodInfo method) => null;
    }
    public abstract class Enum : IComparable { }
    public class Exception
    {
        public Exception(string message) => throw null;
    }
    public class FlagsAttribute : Attribute { }
    public delegate T Func<out T>();
    public delegate U Func<in T, out U>(T arg);
    public interface IComparable { }
    public interface IDisposable
    {
        void Dispose();
    }
    public struct Int16 { }
    public struct Int32 { }
    public struct IntPtr { }
    public class MulticastDelegate : Delegate { }
    public struct Nullable<T> { }
    public class Object
    {
        public virtual string ToString() => throw null;
        public virtual int GetHashCode() => throw null;
        public virtual bool Equals(object other) => throw null;
    }
    public sealed class ParamArrayAttribute : Attribute { }
    public struct RuntimeMethodHandle { }
    public struct RuntimeTypeHandle { }
    public class String : IComparable { 
        public static String Empty = null;
        public override string ToString() => throw null;
        public static bool operator ==(string a, string b) => throw null;
        public static bool operator !=(string a, string b) => throw null;
        public override bool Equals(object other) => throw null;
        public override int GetHashCode() => throw null;
    }
    public class Type
    {
        public Reflection.FieldInfo GetField(string name) => null;
        public static Type GetType(string name) => null;
        public static Type GetTypeFromHandle(RuntimeTypeHandle handle) => null;
    }
    public class ValueType { }
    public struct Void { }

    namespace Collections
    {
        public interface IEnumerable
        {
            IEnumerator GetEnumerator();
        }
        public interface IEnumerator
        {
            object Current
            {
                get;
            }
            bool MoveNext();
            void Reset();
        }
    }
    namespace Collections.Generic
    {
        public interface IEnumerable<out T> : IEnumerable
        {
            new IEnumerator<T> GetEnumerator();
        }
        public interface IEnumerator<out T> : IEnumerator, IDisposable
        {
            new T Current
            {
                get;
            }
        }
    }
    namespace Linq.Expressions
    {
        public class Expression
        {
            public static ParameterExpression Parameter(Type type) => throw null;
            public static ParameterExpression Parameter(Type type, string name) => throw null;
            public static MethodCallExpression Call(Expression instance, Reflection.MethodInfo method, params Expression[] arguments) => throw null;
            public static Expression<TDelegate> Lambda<TDelegate>(Expression body, params ParameterExpression[] parameters) => throw null;
            public static MemberExpression Property(Expression expression, Reflection.MethodInfo propertyAccessor) => throw null;
            public static ConstantExpression Constant(object value, Type type) => throw null;
            public static UnaryExpression Convert(Expression expression, Type type) => throw null;
        }
        public class ParameterExpression : Expression { }
        public class MethodCallExpression : Expression { }
        public abstract class LambdaExpression : Expression { }
        public class Expression<T> : LambdaExpression { }
        public class MemberExpression : Expression { }
        public class ConstantExpression : Expression { }
        public sealed class UnaryExpression : Expression { }
    }
    namespace Reflection
    {
        public class AssemblyVersionAttribute : Attribute
        {
            public AssemblyVersionAttribute(string version) { }
        }
        public class DefaultMemberAttribute : Attribute
        {
            public DefaultMemberAttribute(string name) { }
        }
        public abstract class MemberInfo { }
        public abstract class MethodBase : MemberInfo
        {
            public static MethodBase GetMethodFromHandle(RuntimeMethodHandle handle) => throw null;
        }
        public abstract class MethodInfo : MethodBase
        {
            public virtual Delegate CreateDelegate(Type delegateType, object target) => throw null;
        }
        public abstract class FieldInfo : MemberInfo
        {
            public abstract object GetValue(object obj);
        }
    }
    namespace Runtime.CompilerServices
    {
        public static class RuntimeHelpers
        {
            public static object GetObjectValue(object obj) => null;
        }
    }
}
";
            const string corlibWithCovariantSupport = corLibraryCore + @"
namespace System.Runtime.CompilerServices
{
    public static class RuntimeFeature
    {
        public const string CovariantReturnsOfClasses = nameof(CovariantReturnsOfClasses);
        public const string DefaultImplementationsOfInterfaces = nameof(DefaultImplementationsOfInterfaces);
    }
    public sealed class PreserveBaseOverridesAttribute : Attribute { }
}
";
            var compilation = CreateEmptyCompilation(new string[] {
                corlibWithCovariantSupport,
                @"[assembly: System.Reflection.AssemblyVersion(""4.0.0.0"")]"
            }, assemblyName: "mscorlib");
            compilation.VerifyDiagnostics();
            return compilation.EmitToImageReference(options: new CodeAnalysis.Emit.EmitOptions(runtimeMetadataVersion: "v5.1"));
        }

        private static CSharpCompilation CreateCovariantCompilation(
            string source,
            CSharpCompilationOptions options = null,
            IEnumerable<MetadataReference> references = null,
            string assemblyName = null)
        {
            Assert.NotNull(CorelibraryWithCovariantReturnSupport);
            references = (references == null) ?
                new[] { CorelibraryWithCovariantReturnSupport } :
                references.ToArray().Prepend(CorelibraryWithCovariantReturnSupport);
            return CreateEmptyCompilation(
                source,
                options: options,
                parseOptions: TestOptions.WithCovariantReturns,
                references: references,
                assemblyName: assemblyName);
        }

        [ConditionalFact(typeof(CovariantReturnRuntimeOnly))]
        public void SimpleCovariantReturnEndToEndTest()
        {
            var source = @"
using System;
class Base
{
    public virtual object M() => ""Base.M"";
}
class Derived : Base
{
    public override string M() => ""Derived.M"";
}
class Program
{
    static void Main()
    {
        Derived d = new Derived();
        Base b = d;
        string s = d.M();
        object o = b.M();
        Console.WriteLine(s.ToString());
        Console.WriteLine(o.ToString());
    }
}
";
            var compilation = CreateCovariantCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"Derived.M
Derived.M";
            CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.Skipped);
        }

        [ConditionalFact(typeof(CovariantReturnRuntimeOnly))]
        public void CovariantRuntimeHasRequiredMembers()
        {
            var source = @"
using System;
class Base
{
    public virtual object M() => ""Base.M"";
}
class Derived : Base
{
    public override string M() => ""Derived.M"";
}
class Program
{
    static void Main()
    {
        var value = (string)Type.GetType(""System.Runtime.CompilerServices.RuntimeFeature"").GetField(""CovariantReturnsOfClasses"").GetValue(null);
        if (value != ""CovariantReturnsOfClasses"")
            throw new Exception(value.ToString());

        var attr = Type.GetType(""System.Runtime.CompilerServices.PreserveBaseOverridesAttribute"");
        if (attr == null)
            throw new Exception(""missing System.Runtime.CompilerServices.PreserveBaseOverridesAttribute"");
    }
}
";
            var compilation = CreateCovariantCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"";
            CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.Skipped);
        }

        [Fact]
        public void VbOverrideOfCSharpCovariantReturn_01()
        {
            var cSharpSource = @"
public class Base
{
    public virtual object M() => null;
    public virtual object P => null;
    public virtual object this[int i] => null;
}
public abstract class Derived : Base
{
    public override string M() => null;
    public override string P => null;
    public override string this[int i] => null;
}
";
            var csharpCompilation = CreateCovariantCompilation(cSharpSource).VerifyDiagnostics();
            var csharpReference = csharpCompilation.EmitToImageReference();

            var vbSource = @"
Public Class Derived2 : Inherits Derived
    Public Overrides Function M() As Object
        Return Nothing
    End Function
    Public Overrides ReadOnly Property P As Object
        Get
            Return Nothing
        End Get
    End Property
    Public Overrides Default ReadOnly Property Item(i As Integer) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class
";
            var ERR_InvalidOverrideDueToReturn2 =
                typeof(VisualBasic.VisualBasicCompilation).Assembly.GetType("Microsoft.CodeAnalysis.VisualBasic.ERRID").GetField("ERR_InvalidOverrideDueToReturn2").GetValue(null);
            CreateVisualBasicCompilation(vbSource, referencedAssemblies: new[] { CorelibraryWithCovariantReturnSupport, csharpReference })
                .VerifyDiagnostics(
        //BC30437: 'Public Overrides Function M() As Object' cannot override 'Public Overridable Overloads Function M() As String' because they differ by their return types.
        //Public Overrides Function M() As Object
        //                      ~
        Diagnostic(ERR_InvalidOverrideDueToReturn2, "M").WithArguments("Public Overrides Function M() As Object", "Public Overridable Overloads Function M() As String").WithLocation(3, 31),
        //BC30437: 'Public Overrides ReadOnly Property P As Object' cannot override 'Public Overridable Overloads ReadOnly Property P As String' because they differ by their return types.
        //Public Overrides ReadOnly Property P As Object
        //                               ~
        Diagnostic(ERR_InvalidOverrideDueToReturn2, "P").WithArguments("Public Overrides ReadOnly Property P As Object", "Public Overridable Overloads ReadOnly Property P As String").WithLocation(6, 40),
        //BC30437: 'Public Overrides ReadOnly Default Property Item(i As Integer) As Object' cannot override 'Public Overridable Overloads ReadOnly Default Property Item(i As Integer) As String' because they differ by their return types.
        //Public Overrides Default ReadOnly Property Item(i As Integer) As Object
        //                                       ~~~~
        Diagnostic(ERR_InvalidOverrideDueToReturn2, "Item").WithArguments("Public Overrides ReadOnly Default Property Item(i As Integer) As Object", "Public Overridable Overloads ReadOnly Default Property Item(i As Integer) As String").WithLocation(11, 48)
                );
        }

        [Fact]
        public void VbOverrideOfCSharpCovariantReturn_02()
        {
            var cSharpSource = @"
public class Base
{
    public virtual object M() => ""Base.M"";
    public virtual object P => ""Base.P"";
    public virtual object this[int i] => ""Base[]"";
}
public abstract class Derived : Base
{
    public override string M() => ""Derived.M"";
    public override string P => ""Derived.P"";
    public override string this[int i] => ""Derived[]"";
}
";
            var csharpCompilation = CreateCovariantCompilation(cSharpSource).VerifyDiagnostics();
            var csharpReference = csharpCompilation.EmitToImageReference();

            var vbSource = @"
Imports System
Public Class Derived2 : Inherits Derived
    Public Overrides Function M() As String
        Return ""Derived2.M""
    End Function
    Public Overrides ReadOnly Property P As String
        Get
            Return ""Derived2.P""
        End Get
    End Property
    Public Overrides Default ReadOnly Property Item(i As Integer) As String
        Get
            Return ""Derived2[]""
        End Get
    End Property
    Public Shared Sub Test(b As Base, d As Derived, d2 As Derived2)
        Console.WriteLine(b.M().ToString())
        Console.WriteLine(b.P.ToString())
        Console.WriteLine(b(0).ToString())
        Console.WriteLine(d.M())
        Console.WriteLine(d.P)
        Console.WriteLine(d(0))
        Console.WriteLine(d2.M())
        Console.WriteLine(d2.P)
        Console.WriteLine(d2(0))
    End Sub
    public Shared Sub Main()
        Dim d2 = new Derived2
        Test(d2, d2, d2)
    End Sub
End Class
";
            var compilationOptions = new VisualBasic.VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptimizationLevel(OptimizationLevel.Release);
            var vbCompilation = CreateVisualBasicCompilation(vbSource, compilationOptions: compilationOptions, referencedAssemblies: new[] { CorelibraryWithCovariantReturnSupport, csharpReference })
                .VerifyDiagnostics(
                );

            var expectedOutput = !ExecutionConditionUtil.RuntimeSupportsCovariantReturnsOfClasses ? null : @"
Derived2.M
Derived2.P
Derived2[]
Derived2.M
Derived2.P
Derived2[]
Derived2.M
Derived2.P
Derived2[]";
            CompileAndVerify(vbCompilation, verify: Verification.Skipped, expectedOutput: expectedOutput)
                .VerifyIL("Derived2.Test", @"
{
  // Code size      118 (0x76)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""Function Base.M() As Object""
  IL_0006:  callvirt   ""Function Object.ToString() As String""
  IL_000b:  call       ""Sub System.Console.WriteLine(String)""
  IL_0010:  ldarg.0
  IL_0011:  callvirt   ""Function Base.get_P() As Object""
  IL_0016:  callvirt   ""Function Object.ToString() As String""
  IL_001b:  call       ""Sub System.Console.WriteLine(String)""
  IL_0020:  ldarg.0
  IL_0021:  ldc.i4.0
  IL_0022:  callvirt   ""Function Base.get_Item(Integer) As Object""
  IL_0027:  callvirt   ""Function Object.ToString() As String""
  IL_002c:  call       ""Sub System.Console.WriteLine(String)""
  IL_0031:  ldarg.1
  IL_0032:  callvirt   ""Function Derived.M() As String""
  IL_0037:  call       ""Sub System.Console.WriteLine(String)""
  IL_003c:  ldarg.1
  IL_003d:  callvirt   ""Function Derived.get_P() As String""
  IL_0042:  call       ""Sub System.Console.WriteLine(String)""
  IL_0047:  ldarg.1
  IL_0048:  ldc.i4.0
  IL_0049:  callvirt   ""Function Derived.get_Item(Integer) As String""
  IL_004e:  call       ""Sub System.Console.WriteLine(String)""
  IL_0053:  ldarg.2
  IL_0054:  callvirt   ""Function Derived2.M() As String""
  IL_0059:  call       ""Sub System.Console.WriteLine(String)""
  IL_005e:  ldarg.2
  IL_005f:  callvirt   ""Function Derived2.get_P() As String""
  IL_0064:  call       ""Sub System.Console.WriteLine(String)""
  IL_0069:  ldarg.2
  IL_006a:  ldc.i4.0
  IL_006b:  callvirt   ""Function Derived2.get_Item(Integer) As String""
  IL_0070:  call       ""Sub System.Console.WriteLine(String)""
  IL_0075:  ret
}
");
        }

        [ConditionalFact(typeof(CovariantReturnRuntimeOnly))]
        public void CheckPreserveBaseOverride_01()
        {
            var s0 = @"
public class Base
{
    public virtual object M() => ""Base.M"";
}
";
            var ref0 = CreateCovariantCompilation(
                s0,
                assemblyName: "ref0").VerifyEmitDiagnostics().EmitToImageReference();

            var s1a = @"
public class Mid : Base
{
}
";
            var ref1a = CreateCovariantCompilation(
                s1a,
                references: new[] { ref0 },
                assemblyName: "ref1").VerifyEmitDiagnostics().EmitToImageReference();

            var s1b = @"
public class Mid : Base
{
    public override string M() => ""Mid.M"";
}
";
            var ref1b = CreateCovariantCompilation(
                s1b,
                references: new[] { ref0 },
                assemblyName: "ref1").VerifyEmitDiagnostics().EmitToImageReference();

            var s2 = @"
public class Derived : Mid
{
    public override string M() => ""Derived.M"";
}
";
            var ref2 = CreateCovariantCompilation(
                s2,
                references: new[] { ref0, ref1a },
                assemblyName: "ref2").VerifyEmitDiagnostics().EmitToImageReference();

            var program = @"
using System;
public class Program
{
    static void Main()
    {
        Derived d = new Derived();
        Mid m = d;
        Base b = m;
        Console.WriteLine(b.M().ToString());
        Console.WriteLine(m.M().ToString());
        Console.WriteLine(d.M().ToString());
    }
}
";
            var compilation = CreateCovariantCompilation(program, options: TestOptions.DebugExe, references: new[] { ref0, ref1b, ref2 });
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"Derived.M
Derived.M
Derived.M";
            CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.Skipped);
        }
    }
}
