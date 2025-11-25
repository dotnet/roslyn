// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_CallerInfoAttributes : WellKnownAttributesTestBase
    {
        public static IEnumerable<MetadataReference> GetReferencesWithoutInteropServices() =>
            TargetFrameworkUtil.GetReferencesWithout(TargetFramework.Net50, "System.Runtime.InteropServices.dll");

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestBeginInvoke()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class OptionalAttribute : Attribute
    {
        public OptionalAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class DefaultParameterValueAttribute : Attribute
    {
        public DefaultParameterValueAttribute(object value)
        {
            Value = value;
        }
        public object Value { get; }
    }
}

class Program
{
    const string s1 = nameof(s1);
    delegate void D(string s1, [CallerArgumentExpression(s1)] [Optional] [DefaultParameterValue(""default"")] string s2);

    static void M(string s1, string s2)
    {
    }

    static string GetString() => null;

    public static void Main()
    {
        D d = M;
        d.BeginInvoke(GetString(), callback: null, @object: null);
    }
}
";

            var compilation = CreateEmptyCompilation(source, GetReferencesWithoutInteropServices(), options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            // Begin/EndInvoke are not currently supported.
            CompileAndVerify(compilation).VerifyDiagnostics().VerifyIL("Program.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  5
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void Program.M(string, string)""
  IL_0007:  newobj     ""Program.D..ctor(object, System.IntPtr)""
  IL_000c:  call       ""string Program.GetString()""
  IL_0011:  ldstr      ""default""
  IL_0016:  ldnull
  IL_0017:  ldnull
  IL_0018:  callvirt   ""System.IAsyncResult Program.D.BeginInvoke(string, string, System.AsyncCallback, object)""
  IL_001d:  pop
  IL_001e:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestEndInvoke()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class OptionalAttribute : Attribute
    {
        public OptionalAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class DefaultParameterValueAttribute : Attribute
    {
        public DefaultParameterValueAttribute(object value)
        {
            Value = value;
        }
        public object Value { get; }
    }
}

class Program
{
    const string s1 = nameof(s1);
    delegate void D(ref string s1, [CallerArgumentExpression(s1)] [Optional] [DefaultParameterValue(""default"")] string s2);

    static void M(ref string s1, string s2)
    {
    }

    public static void Main()
    {
        D d = M;
        string s = string.Empty;
        d.EndInvoke(ref s, null);
    }
}
";

            var compilation = CreateEmptyCompilation(source, GetReferencesWithoutInteropServices(), options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            // Begin/EndInvoke are not currently supported.
            CompileAndVerify(compilation).VerifyDiagnostics().VerifyIL("Program.Main", @"
{
  // Code size       27 (0x1b)
  .maxstack  3
  .locals init (string V_0) //s
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void Program.M(ref string, string)""
  IL_0007:  newobj     ""Program.D..ctor(object, System.IntPtr)""
  IL_000c:  ldsfld     ""string string.Empty""
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  ldnull
  IL_0015:  callvirt   ""void Program.D.EndInvoke(ref string, System.IAsyncResult)""
  IL_001a:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestEndInvoke2()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class OptionalAttribute : Attribute
    {
        public OptionalAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class DefaultParameterValueAttribute : Attribute
    {
        public DefaultParameterValueAttribute(object value)
        {
            Value = value;
        }
        public object Value { get; }
    }
}

class Program
{
    const string s5 = nameof(s5);
    delegate void D([CallerArgumentExpression(s5)] [Optional] [DefaultParameterValue(""default"")] ref string s1, string s2, string s3, string s4, string s5);

    static void M(ref string s1, string s2, string s3, string s4, string s5)
    {
    }

    public static void Main()
    {
        D d = M;
        d.EndInvoke(result: null);
    }
}
";

            var compilation = CreateEmptyCompilation(source, GetReferencesWithoutInteropServices(), options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            compilation.VerifyDiagnostics(
                // (29,22): error CS8964: The CallerArgumentExpressionAttribute may only be applied to parameters with default values
                //     delegate void D([CallerArgumentExpression(s5)] [Optional] [DefaultParameterValue("default")] ref string s1, string s2, string s3, string s4, string s5);
                Diagnostic(ErrorCode.ERR_BadCallerArgumentExpressionParamWithoutDefaultValue, "CallerArgumentExpression").WithLocation(29, 22),
                // (38,11): error CS7036: There is no argument given that corresponds to the required parameter 's1' of 'Program.D.EndInvoke(ref string, IAsyncResult)'
                //         d.EndInvoke(result: null);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "EndInvoke").WithArguments("s1", "Program.D.EndInvoke(ref string, System.IAsyncResult)").WithLocation(38, 11)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestEndInvoke3()
        {
            string source = @"
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class OptionalAttribute : Attribute
    {
        public OptionalAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class DefaultParameterValueAttribute : Attribute
    {
        public DefaultParameterValueAttribute(object value)
        {
            Value = value;
        }
        public object Value { get; }
    }
}

class Program
{
    const string s1 = nameof(s1);
    delegate void D(string s1, [CallerArgumentExpression(s1)] ref string s2 = ""default"");

    static void M(string s1, ref string s2)
    {
    }

    public static void Main()
    {
        D d = M;
        string s = string.Empty;
        d.EndInvoke(result: null);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            compilation.VerifyDiagnostics(
                // (28,33): error CS8964: The CallerArgumentExpressionAttribute may only be applied to parameters with default values
                //     delegate void D(string s1, [CallerArgumentExpression(s1)] ref string s2 = "default");
                Diagnostic(ErrorCode.ERR_BadCallerArgumentExpressionParamWithoutDefaultValue, "CallerArgumentExpression").WithLocation(28, 33),
                // (28,63): error CS1741: A ref or out parameter cannot have a default value
                //     delegate void D(string s1, [CallerArgumentExpression(s1)] ref string s2 = "default");
                Diagnostic(ErrorCode.ERR_RefOutDefaultValue, "ref").WithLocation(28, 63),
                // (38,11): error CS7036: There is no argument given that corresponds to the required parameter 's2' of 'Program.D.EndInvoke(ref string, IAsyncResult)'
                //         d.EndInvoke(result: null);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "EndInvoke").WithArguments("s2", "Program.D.EndInvoke(ref string, System.IAsyncResult)").WithLocation(38, 11));
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestEndInvoke4()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class OptionalAttribute : Attribute
    {
        public OptionalAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class DefaultParameterValueAttribute : Attribute
    {
        public DefaultParameterValueAttribute(object value)
        {
            Value = value;
        }
        public object Value { get; }
    }
}

class Program
{
    const string s1 = nameof(s1);
    delegate void D(string s1, [CallerArgumentExpression(s1)] [Optional] [DefaultParameterValue(""default"")] ref string s2);

    static void M(string s1, ref string s2)
    {
    }

    public static void Main()
    {
        D d = M;
        string s = string.Empty;
        d.EndInvoke(result: null);
    }
}
";

            var compilation = CreateEmptyCompilation(source, GetReferencesWithoutInteropServices(), options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            compilation.VerifyDiagnostics(
                // (29,33): error CS8964: The CallerArgumentExpressionAttribute may only be applied to parameters with default values
                //     delegate void D(string s1, [CallerArgumentExpression(s1)] [Optional] [DefaultParameterValue("default")] ref string s2);
                Diagnostic(ErrorCode.ERR_BadCallerArgumentExpressionParamWithoutDefaultValue, "CallerArgumentExpression").WithLocation(29, 33),
                // (39,11): error CS7036: There is no argument given that corresponds to the required parameter 's2' of 'Program.D.EndInvoke(ref string, IAsyncResult)'
                //         d.EndInvoke(result: null);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "EndInvoke").WithArguments("s2", "Program.D.EndInvoke(ref string, System.IAsyncResult)").WithLocation(39, 11));
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestBeginInvoke_ReferringToCallbackParameter()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class OptionalAttribute : Attribute
    {
        public OptionalAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class DefaultParameterValueAttribute : Attribute
    {
        public DefaultParameterValueAttribute(object value)
        {
            Value = value;
        }
        public object Value { get; }
    }
}

class Program
{
    const string callback = nameof(callback);
    delegate void D(string s1, [CallerArgumentExpression(callback)] [Optional] [DefaultParameterValue(""default"")] string s2);

    static void M(string s1, string s2)
    {
    }

    static string GetString() => null;

    public static void Main()
    {
        D d = M;
        d.BeginInvoke(GetString(), callback: null, @object: null);
    }
}
";

            var compilation = CreateEmptyCompilation(source, GetReferencesWithoutInteropServices(), options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation).VerifyDiagnostics(
                // (29,33): warning CS8963: The CallerArgumentExpressionAttribute applied to parameter 's2' will have no effect. It is applied with an invalid parameter name.
                //     delegate void D(string s1, [CallerArgumentExpression(callback)] [Optional] [DefaultParameterValue("default")] string s2);
                Diagnostic(ErrorCode.WRN_CallerArgumentExpressionAttributeHasInvalidParameterName, "CallerArgumentExpression").WithArguments("s2").WithLocation(29, 33)
                ).VerifyIL("Program.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  5
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void Program.M(string, string)""
  IL_0007:  newobj     ""Program.D..ctor(object, System.IntPtr)""
  IL_000c:  call       ""string Program.GetString()""
  IL_0011:  ldstr      ""default""
  IL_0016:  ldnull
  IL_0017:  ldnull
  IL_0018:  callvirt   ""System.IAsyncResult Program.D.BeginInvoke(string, string, System.AsyncCallback, object)""
  IL_001d:  pop
  IL_001e:  ret
}
");
        }

        #region CallerArgumentExpression - Invocations
        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestGoodCallerArgumentExpressionAttribute()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        Log(123);
    }
    const string p = nameof(p);
    static void Log(int p, [CallerArgumentExpression(p)] string arg = ""<default-arg>"")
    {
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "123").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestGoodCallerArgumentExpressionAttribute_MultipleAttributes()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
    public sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }
        public string ParameterName { get; }
    }
}

class Program
{
    public static void Main()
    {
        Log(123, 456);
    }
    const string p1 = nameof(p1);
    const string p2 = nameof(p2);
    static void Log(int p1, int p2, [CallerArgumentExpression(p1), CallerArgumentExpression(p2)] string arg = ""<default-arg>"")
    {
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "456").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestGoodCallerArgumentExpressionAttribute_MultipleAttributes_IncorrectCtor()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
    public sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName, int extraParam)
        {
            ParameterName = parameterName;
        }
        public string ParameterName { get; }
    }
}

class Program
{
    public static void Main()
    {
        Log(123, 456);
    }
    const string p1 = nameof(p1);
    const string p2 = nameof(p2);
    static void Log(int p1, int p2, [CallerArgumentExpression(p1, 0), CallerArgumentExpression(p2, 1)] string arg = ""<default-arg>"")
    {
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "<default-arg>").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestGoodCallerArgumentExpressionAttribute_ExpressionHasTrivia()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        Log(// comment
               123 /* comment */ +
               5 /* comment */ // comment
        );
    }
    const string p = nameof(p);
    static void Log(int p, [CallerArgumentExpression(p)] string arg = ""<default-arg>"")
    {
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput:
@"123 /* comment */ +
               5").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestGoodCallerArgumentExpressionAttribute_Pragmas()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        Log(
            #pragma warning disable IDE0001
            #nullable enable
               123 +
            #pragma warning disable IDE0002
            #nullable disable
               5 +
            #pragma warning disable IDE0003
            #nullable restore
               5
            #pragma warning disable IDE0004
            #nullable enable
        );
    }
    const string p = nameof(p);
    static void Log(int p, [CallerArgumentExpression(p)] string arg = ""<default-arg>"")
    {
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput:
@"123 +
            #pragma warning disable IDE0002
            #nullable disable
               5 +
            #pragma warning disable IDE0003
            #nullable restore
               5
").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestGoodCallerArgumentExpressionAttribute_ImplicitAndExplicitConstructorBaseCalls()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Base
{
    const string p = nameof(p);
    public Base(int p = 0, [CallerArgumentExpression(p)] string arg = ""<default-arg-base>"")
    {
        Console.WriteLine(""Base class: "" + arg);
    }
}

class Derived1 : Base
{
    const string ppp = nameof(ppp);
    public Derived1(int ppp, [CallerArgumentExpression(ppp)] string arg = ""<default-arg-derived1>"")
        : base(ppp)
    {
        Console.WriteLine(""Derived1 class: "" + arg);
    }
}

class Derived2 : Base
{
    const string p = nameof(p);
    public Derived2(int p, [CallerArgumentExpression(p)] string arg = ""<default-arg-derived2>"")
    {
        Console.WriteLine(""Derived2 class: "" + arg);
    }
}


class Program
{
    public static void Main(string[] args)
    {
        _ = new Base(1+4);
        Console.WriteLine();
        _ = new Derived1(2+ 5);
        Console.WriteLine();
        _ = new Derived2(3 +  6);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput:
@"Base class: 1+4

Base class: ppp
Derived1 class: 2+ 5

Base class: <default-arg-base>
Derived2 class: 3 +  6
").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestGoodCallerArgumentExpressionAttribute_SwapArguments()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        Log(q: 123, p: 124);
    }
    const string p = nameof(p);
    static void Log(int p, int q, [CallerArgumentExpression(p)] string arg = ""<default-arg>"")
    {
        Console.WriteLine($""{p}, {q}, {arg}"");
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "124, 123, 124").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestGoodCallerArgumentExpressionAttribute_DifferentAssembly()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

public static class FromFirstAssembly
{
    const string p = nameof(p);
    public static void Log(int p, int q, [CallerArgumentExpression(p)] string arg = ""<default-arg>"")
    {
        Console.WriteLine(arg);
    }
}
";
            var comp1 = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, parseOptions: TestOptions.Regular9);
            comp1.VerifyDiagnostics();
            var ref1 = comp1.EmitToImageReference();

            var source2 = @"
public static class Program
{
    public static void Main() => FromFirstAssembly.Log(2 + 2, 3 + 1);
}
";

            var compilation = CreateCompilation(source2, references: new[] { ref1 }, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "2 + 2").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestGoodCallerArgumentExpressionAttribute_ExtensionMethod_ThisParameter()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

public static class Program
{
    public static void Main()
    {
        int myIntegerExpression = 5;
        myIntegerExpression.M();
    }
    const string p = nameof(p);
    public static void M(this int p, [CallerArgumentExpression(p)] string arg = ""<default-arg>"")
    {
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "myIntegerExpression").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestGoodCallerArgumentExpressionAttribute_ExtensionMethod_NotThisParameter()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

public static class Program
{
    public static void Main()
    {
        int myIntegerExpression = 5;
        myIntegerExpression.M(myIntegerExpression * 2);
    }
    const string q = nameof(q);
    public static void M(this int p, int q, [CallerArgumentExpression(q)] string arg = ""<default-arg>"")
    {
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "myIntegerExpression * 2").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestIncorrectParameterNameInCallerArgumentExpressionAttribute()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        Log();
    }
    const string pp = nameof(pp);
    static void Log([CallerArgumentExpression(pp)] string arg = ""<default>"")
    {
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);
            CompileAndVerify(compilation, expectedOutput: "<default>").VerifyDiagnostics(
                // (12,22): warning CS8918: The CallerArgumentExpressionAttribute applied to parameter 'arg' will have no effect. It is applied with an invalid parameter name.
                //     static void Log([CallerArgumentExpression(pp)] string arg = "<default>")
                Diagnostic(ErrorCode.WRN_CallerArgumentExpressionAttributeHasInvalidParameterName, "CallerArgumentExpression").WithArguments("arg").WithLocation(12, 22)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentWithMemberNameAttributes()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        Log(default(int));
    }
    const string p = nameof(p);
    static void Log(int p, [CallerArgumentExpression(p)] [CallerMemberName] string arg = ""<default>"")
    {
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);
            CompileAndVerify(compilation, expectedOutput: "Main").VerifyDiagnostics(
                // (12,29): warning CS8962: The CallerArgumentExpressionAttribute applied to parameter 'arg' will have no effect. It is overridden by the CallerMemberNameAttribute.
                //     static void Log(int p, [CallerArgumentExpression(p)] [CallerMemberName] string arg = "<default>")
                Diagnostic(ErrorCode.WRN_CallerMemberNamePreferredOverCallerArgumentExpression, "CallerArgumentExpression").WithArguments("arg").WithLocation(12, 29)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentWithMemberNameAttributes2()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        Log(default(int));
    }
    const string p = nameof(p);
    static void Log(int p, [CallerArgumentExpression(p)] [CallerMemberName] string arg = ""<default>"")
    {
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "Main").VerifyDiagnostics(
                // (12,29): warning CS8962: The CallerArgumentExpressionAttribute applied to parameter 'arg' will have no effect. It is overridden by the CallerMemberNameAttribute.
                //     static void Log(int p, [CallerArgumentExpression(p)] [CallerMemberName] string arg = "<default>")
                Diagnostic(ErrorCode.WRN_CallerMemberNamePreferredOverCallerArgumentExpression, "CallerArgumentExpression").WithArguments("arg").WithLocation(12, 29)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentWitLineNumberAttributes()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        Log(default(int));
    }
    const string p = nameof(p);
    static void Log(int p, [CallerArgumentExpression(p)] [CallerLineNumber] string arg = ""<default>"")
    {
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);
            compilation.VerifyDiagnostics(
                // (9,9): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         Log(default(int));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "Log(default(int))").WithArguments("int", "string").WithLocation(9, 9),
                // (12,29): warning CS8960: The CallerArgumentExpressionAttribute applied to parameter 'arg' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void Log(int p, [CallerArgumentExpression(p)] [CallerLineNumber] string arg = "<default>")
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerArgumentExpression, "CallerArgumentExpression").WithArguments("arg").WithLocation(12, 29),
                // (12,59): error CS4017: CallerLineNumberAttribute cannot be applied because there are no standard conversions from type 'int' to type 'string'
                //     static void Log(int p, [CallerArgumentExpression(p)] [CallerLineNumber] string arg = "<default>")
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithArguments("int", "string").WithLocation(12, 59)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentWithLineNumberAttributes2()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        Log(default(int));
    }
    const string p = nameof(p);
    static void Log(int p, [CallerArgumentExpression(p)] [CallerLineNumber] string arg = ""<default>"")
    {
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            compilation.VerifyDiagnostics(
                // (9,9): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         Log(default(int));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "Log(default(int))").WithArguments("int", "string").WithLocation(9, 9),
                // (12,29): warning CS8960: The CallerArgumentExpressionAttribute applied to parameter 'arg' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void Log(int p, [CallerArgumentExpression(p)] [CallerLineNumber] string arg = "<default>")
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerArgumentExpression, "CallerArgumentExpression").WithArguments("arg").WithLocation(12, 29),
                // (12,59): error CS4017: CallerLineNumberAttribute cannot be applied because there are no standard conversions from type 'int' to type 'string'
                //     static void Log(int p, [CallerArgumentExpression(p)] [CallerLineNumber] string arg = "<default>")
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithArguments("int", "string").WithLocation(12, 59)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentWitLineNumberAttributes3()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        Log(default(int));
    }
    const string p = nameof(p);
    static void Log(int p, [CallerArgumentExpression(p)] [CallerLineNumber] int arg = 0)
    {
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);
            compilation.VerifyDiagnostics(
                // (12,29): error CS8959: CallerArgumentExpressionAttribute cannot be applied because there are no standard conversions from type 'string' to type 'int'
                //     static void Log(int p, [CallerArgumentExpression(p)] [CallerLineNumber] int arg = 0)
                Diagnostic(ErrorCode.ERR_NoConversionForCallerArgumentExpressionParam, "CallerArgumentExpression").WithArguments("string", "int").WithLocation(12, 29)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentWithLineNumberAttributes4()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        Log(default(int));
    }
    const string p = nameof(p);
    static void Log(int p, [CallerArgumentExpression(p)] [CallerLineNumber] int arg = 0)
    {
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            compilation.VerifyDiagnostics(
                // (12,29): error CS8959: CallerArgumentExpressionAttribute cannot be applied because there are no standard conversions from type 'string' to type 'int'
                //     static void Log(int p, [CallerArgumentExpression(p)] [CallerLineNumber] int arg = 0)
                Diagnostic(ErrorCode.ERR_NoConversionForCallerArgumentExpressionParam, "CallerArgumentExpression").WithArguments("string", "int").WithLocation(12, 29)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentNonOptionalParameter()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        Log(default(int));
    }
    const string p = nameof(p);
    static void Log(int p, [CallerArgumentExpression(p)] string arg)
    {
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            compilation.VerifyDiagnostics(
                // (9,9): error CS7036: There is no argument given that corresponds to the required parameter 'arg' of 'Program.Log(int, string)'
                //         Log(default(int));
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Log").WithArguments("arg", "Program.Log(int, string)").WithLocation(9, 9),
                // (12,29): error CS8964: The CallerArgumentExpressionAttribute may only be applied to parameters with default values
                //     static void Log(int p, [CallerArgumentExpression(p)] string arg)
                Diagnostic(ErrorCode.ERR_BadCallerArgumentExpressionParamWithoutDefaultValue, "CallerArgumentExpression").WithLocation(12, 29)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentNonOptionalParameter2()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        Log(default(int));
    }
    const string p = nameof(p);
    static void Log(int p, [CallerArgumentExpression(p)] string arg)
    {
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);
            compilation.VerifyDiagnostics(
                // (9,9): error CS7036: There is no argument given that corresponds to the required parameter 'arg' of 'Program.Log(int, string)'
                //         Log(default(int));
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Log").WithArguments("arg", "Program.Log(int, string)").WithLocation(9, 9),
                // (12,29): error CS8964: The CallerArgumentExpressionAttribute may only be applied to parameters with default values
                //     static void Log(int p, [CallerArgumentExpression(p)] string arg)
                Diagnostic(ErrorCode.ERR_BadCallerArgumentExpressionParamWithoutDefaultValue, "CallerArgumentExpression").WithLocation(12, 29)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentWithOverride()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

abstract class Base
{
    const string p = nameof(p);
    public abstract void Log_RemoveAttributeInOverride(int p, [CallerArgumentExpression(p)] string arg =""default"");
    public abstract void Log_AddAttributeInOverride(int p, string arg =""default"");
}

class Derived : Base
{
    const string p = nameof(p);
    public override void Log_AddAttributeInOverride(int p, [CallerArgumentExpression(p)] string arg = ""default"")
        => Console.WriteLine(arg);

    public override void Log_RemoveAttributeInOverride(int p, string arg = ""default"")
        => Console.WriteLine(arg);
}

class Program
{
    public static void Main()
    {
        var derived = new Derived();
        derived.Log_AddAttributeInOverride(5 + 4);
        derived.Log_RemoveAttributeInOverride(5 + 5);

        ((Base)derived).Log_AddAttributeInOverride(5 + 4);
        ((Base)derived).Log_RemoveAttributeInOverride(5 + 5);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: @"5 + 4
default
default
5 + 5").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentWithUserDefinedConversionFromString()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    public C(string s) => S = s;
    public string S { get; }
    public static implicit operator C(string s) => new C(s);
}

class Program
{
    public static void Main()
    {
        Log(default(int));
    }
    const string p = nameof(p);
    static void Log(int p, [CallerArgumentExpression(p)] C arg = null)
    {
        Console.WriteLine(arg.S);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            compilation.VerifyDiagnostics(
                // (19,29): error CS8959: CallerArgumentExpressionAttribute cannot be applied because there are no standard conversions from type 'string' to type 'C'
                //     static void Log(int p, [CallerArgumentExpression(p)] C arg = null)
                Diagnostic(ErrorCode.ERR_NoConversionForCallerArgumentExpressionParam, "CallerArgumentExpression").WithArguments("string", "C").WithLocation(19, 29)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentWithExtensionGetEnumerator()
        {
            string source = @"
using System;
using System.Collections;
using System.Runtime.CompilerServices;

public static class Extensions
{
    public static IEnumerator GetEnumerator(this IEnumerator enumerator, [CallerArgumentExpression(""enumerator"")] string s = ""default"")
    {
        Console.WriteLine(s);
        return enumerator;
    }
}

class Program
{
    static void Main()
    {
        var x = new [] { """", """" }.GetEnumerator();

        foreach (var y in x)
        {
        }
    }
}

";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "x").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentWithExtensionDeconstruct()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

public static class Extensions
{
    public static void Deconstruct(this Person p, out string firstName, out string lastName, [CallerArgumentExpression(""firstName"")] string s = ""default"")
    {
        firstName = p.FirstName;
        lastName = p.LastName;
        Console.WriteLine(s);
    }
}

public class Person
{
    public Person(string firstName, string lastName)
        => (FirstName, LastName) = (firstName, lastName);

    public string FirstName { get; }
    public string LastName { get; }
}

class Program
{
    static void Main()
    {
        var p = new Person(""myFirstName"", ""myLastName"");
        var (first, last) = p;
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            compilation.VerifyDiagnostics(
                // (29,29): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'Person', with 2 out parameters and a void return type.
                //         var (first, last) = p;
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "p").WithArguments("Person", "2").WithLocation(29, 29)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentExpressionWithOptionalTargetParameter()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        string callerTargetExp = ""caller target value"";
        Log(0);
        Log(0, callerTargetExp);
    }
    const string target = nameof(target);
    static void Log(int p, string target = ""target default value"", [CallerArgumentExpression(target)] string arg = ""arg default value"")
    {
        Console.WriteLine(target);
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput:
@"target default value
arg default value
caller target value
callerTargetExp").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentExpressionWithMultipleOptionalAttribute()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        string callerTargetExp = ""caller target value"";
        Log(0);
        Log(0, callerTargetExp);
        Log(0, target: callerTargetExp);
        Log(0, notTarget: ""Not target value"");
        Log(0, notTarget: ""Not target value"", target: callerTargetExp);
    }
    const string target = nameof(target);
    static void Log(int p, string target = ""target default value"", string notTarget = ""not target default value"", [CallerArgumentExpression(target)] string arg = ""arg default value"")
    {
        Console.WriteLine(target);
        Console.WriteLine(arg);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput:
@"target default value
arg default value
caller target value
callerTargetExp
caller target value
callerTargetExp
target default value
arg default value
caller target value
callerTargetExp").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentExpressionWithDifferentParametersReferringToEachOther()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        M();
        M(""param1_value"");
        M(param1: ""param1_value"");
        M(param2: ""param2_value"");
        M(param1: ""param1_value"", param2: ""param2_value"");
        M(param2: ""param2_value"", param1: ""param1_value"");
    }

    static void M([CallerArgumentExpression(""param2"")] string param1 = ""param1_default"", [CallerArgumentExpression(""param1"")] string param2 = ""param2_default"")
    {
        Console.WriteLine($""param1: {param1}, param2: {param2}"");
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput:
@"param1: param1_default, param2: param2_default
param1: param1_value, param2: ""param1_value""
param1: param1_value, param2: ""param1_value""
param1: ""param2_value"", param2: param2_value
param1: param1_value, param2: param2_value
param1: param1_value, param2: param2_value").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpressionIsCallerMember()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

C.M();

public static class C
{
    public static void M(
        [CallerMemberName] string callerName = ""<default-caller-name>"",
        [CallerArgumentExpression(""callerName"")] string argumentExp = ""<default-arg-expression>"")
    {
        Console.WriteLine(callerName);
        Console.WriteLine(argumentExp);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);
            CompileAndVerify(compilation, expectedOutput: @"<Main>$
<default-arg-expression>").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpressionIsSelfReferential()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

C.M();
C.M(""value"");

public static class C
{
    public static void M(
        [CallerArgumentExpression(""p"")] string p = ""<default>"")
    {
        Console.WriteLine(p);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: @"<default>
value").VerifyDiagnostics(
                // (11,10): warning CS8965: The CallerArgumentExpressionAttribute applied to parameter 'p' will have no effect because it's self-referential.
                //         [CallerArgumentExpression("p")] string p = "<default>")
                Diagnostic(ErrorCode.WRN_CallerArgumentExpressionAttributeSelfReferential, "CallerArgumentExpression").WithArguments("p").WithLocation(11, 10)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpressionIsSelfReferential_Metadata()
        {
            string il = @".class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi abstract sealed beforefieldinit C
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static 
        void M (
            [opt] string p
        ) cil managed 
    {
        .param [1] = ""<default>""
            .custom instance void [mscorlib]System.Runtime.CompilerServices.CallerArgumentExpressionAttribute::.ctor(string) = (
                01 00 01 70 00 00
            )
        // Method begins at RVA 0x2050
        // Code size 9 (0x9)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldarg.0
        IL_0002: call void [mscorlib]System.Console::WriteLine(string)
        IL_0007: nop
        IL_0008: ret
    } // end of method C::M

} // end of class C
";
            string source = @"
C.M();
C.M(""value"");
";

            var compilation = CreateCompilationWithIL(source, il, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: @"<default>
value").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestInterpolatedStringHandler()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;
using System.Text;

M(1 + /**/ 1, $""""); // Should print ""1 + 1""

void M(object o, [InterpolatedStringHandlerArgument(""o"")] CustomHandler c) => Console.WriteLine(c.ToString());

[InterpolatedStringHandler]
public ref struct CustomHandler
{
    private readonly StringBuilder _builder;
    public CustomHandler(int literalLength, int formattedCount, object o, [CallerArgumentExpression(""o"")] string s = """")
    {
        _builder = new StringBuilder();
        _builder.Append(s);
    }
    public void AppendLiteral(string s) => _builder.AppendLine(s.ToString());
    public void AppendFormatted(object o) => _builder.AppendLine(""value:"" + o.ToString());
    public override string ToString() => _builder.ToString();
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class InterpolatedStringHandlerArgumentAttribute : Attribute
    {
        public InterpolatedStringHandlerArgumentAttribute(string argument) => Arguments = new string[] { argument };
        public InterpolatedStringHandlerArgumentAttribute(params string[] arguments) => Arguments = arguments;
        public string[] Arguments { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class InterpolatedStringHandlerAttribute : Attribute
    {
        public InterpolatedStringHandlerAttribute()
        {
        }
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.Net50, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "1 + /**/ 1").VerifyDiagnostics();
        }
        #endregion

        #region CallerArgumentExpression - Attribute constructor

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestGoodCallerArgumentExpressionAttribute_Attribute()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

public class MyAttribute : Attribute
{
    const string p = nameof(p);
    public MyAttribute(int p, [CallerArgumentExpression(p)] string arg = ""<default-arg>"")
    {
        Console.WriteLine(arg);
    }
}

[My(123)]
public class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "123").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestGoodCallerArgumentExpressionAttribute_ExpressionHasTrivia_Attribute()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

public class MyAttribute : Attribute
{
    const string p = nameof(p);
    public MyAttribute(int p, [CallerArgumentExpression(p)] string arg = ""<default-arg>"")
    {
        Console.WriteLine(arg);
    }
}

[My(// comment
               123 /* comment */ +
               5 /* comment */ // comment
        )]
public class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput:
@"123 /* comment */ +
               5").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestGoodCallerArgumentExpressionAttribute_SwapArguments_AttributeConstructor()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

public class MyAttribute : Attribute
{
    const string p = nameof(p);
    public MyAttribute(int p, int q, [CallerArgumentExpression(p)] string arg = ""<default-arg>"")
    {
        Console.WriteLine($""{p}, {q}, {arg}"");
    }
}

[My(q: 123, p: 124)]
public class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "124, 123, 124").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestGoodCallerArgumentExpressionAttribute_DifferentAssembly_AttributeConstructor()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

public class MyAttribute : Attribute
{
    const string p = nameof(p);
    public MyAttribute(int p, int q, [CallerArgumentExpression(p)] string arg = ""<default-arg>"")
    {
        Console.WriteLine(arg);
    }
}
";
            var comp1 = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp1.VerifyDiagnostics();
            var ref1 = comp1.EmitToImageReference();

            var source2 = @"
using System.Reflection;

[My(2 + 2, 3 + 1)]
public class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
    }
}
";

            var compilation = CreateCompilation(source2, references: new[] { ref1 }, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "2 + 2").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestIncorrectParameterNameInCallerArgumentExpressionAttribute_AttributeConstructor()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

public class MyAttribute : Attribute
{
    const string pp = nameof(pp);
    public MyAttribute([CallerArgumentExpression(pp)] string arg = ""<default>"")
    {
        Console.WriteLine(arg);
    }
}

[My]
public class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);
            CompileAndVerify(compilation, expectedOutput: "<default>").VerifyDiagnostics(
                // (12,22): warning CS8918: The CallerArgumentExpressionAttribute applied to parameter 'arg' will have no effect. It is applied with an invalid parameter name.
                //     static void Log([CallerArgumentExpression(pp)] string arg = "<default>")
                Diagnostic(ErrorCode.WRN_CallerArgumentExpressionAttributeHasInvalidParameterName, "CallerArgumentExpression").WithArguments("arg").WithLocation(9, 25)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentWithMemberNameAttributes_AttributeConstructor()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

public class MyAttribute : Attribute
{
    const string p = nameof(p);
    public MyAttribute(int p, [CallerArgumentExpression(p)] [CallerMemberName] string arg = ""<default>"")
    {
        Console.WriteLine(arg);
    }
}

[My(default(int))]
public class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);
            CompileAndVerify(compilation, expectedOutput: "<default>").VerifyDiagnostics(
                // (9,32): warning CS8917: The CallerArgumentExpressionAttribute applied to parameter 'arg' will have no effect. It is overridden by the CallerMemberNameAttribute.
                //     public MyAttribute(int p, [CallerArgumentExpression(p)] [CallerMemberName] string arg = "<default>")
                Diagnostic(ErrorCode.WRN_CallerMemberNamePreferredOverCallerArgumentExpression, "CallerArgumentExpression").WithArguments("arg").WithLocation(9, 32)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentExpressionWithOptionalTargetParameter_AttributeConstructor()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MyAttribute : Attribute
{
    const string target = nameof(target);

    public MyAttribute(int p, string target = ""target default value"", [CallerArgumentExpression(target)] string arg = ""arg default value"")
    {
        Console.WriteLine(target);
        Console.WriteLine(arg);
    }
}

[My(0)]
[My(0, callerTargetExp)]
public class Program
{
    private const string callerTargetExp = ""caller target value"";
    static void Main()
    {
        typeof(Program).GetCustomAttributes(typeof(MyAttribute));
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput:
@"target default value
arg default value
caller target value
callerTargetExp").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentExpressionWithMultipleOptionalAttribute_AttributeConstructor()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MyAttribute : Attribute
{
    const string target = nameof(target);
    public MyAttribute(int p, string target = ""target default value"", string notTarget = ""not target default value"", [CallerArgumentExpression(target)] string arg = ""arg default value"")
    {
        Console.WriteLine(target);
        Console.WriteLine(arg);
    }
}

[My(0)]
[My(0, callerTargetExp)]
[My(0, target: callerTargetExp)]
[My(0, notTarget: ""Not target value"")]
[My(0, notTarget: ""Not target value"", target: callerTargetExp)]
public class Program
{
    private const string callerTargetExp = ""caller target value"";
    static void Main()
    {
        typeof(Program).GetCustomAttributes(typeof(MyAttribute));
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput:
@"target default value
arg default value
caller target value
callerTargetExp
caller target value
callerTargetExp
target default value
arg default value
caller target value
callerTargetExp").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestCallerArgumentExpressionWithDifferentParametersReferringToEachOther_AttributeConstructor()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MyAttribute : Attribute
{
    public MyAttribute([CallerArgumentExpression(""param2"")] string param1 = ""param1_default"", [CallerArgumentExpression(""param1"")] string param2 = ""param2_default"")
    {
        Console.WriteLine($""param1: {param1}, param2: {param2}"");
    }
}

[My()]
[My(""param1_value"")]
[My(param1: ""param1_value"")]
[My(param2: ""param2_value"")]
[My(param1: ""param1_value"", param2: ""param2_value"")]
[My(param2: ""param2_value"", param1: ""param1_value"")]
public class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttributes(typeof(MyAttribute));
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput:
@"param1: param1_default, param2: param2_default
param1: param1_value, param2: ""param1_value""
param1: param1_value, param2: ""param1_value""
param1: ""param2_value"", param2: param2_value
param1: param1_value, param2: param2_value
param1: param1_value, param2: param2_value").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpressionIsCallerMember_AttributeConstructor()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

public class MyAttribute : Attribute
{
    public MyAttribute(
        [CallerMemberName] string callerName = ""<default-caller-name>"",
        [CallerArgumentExpression(""callerName"")] string argumentExp = ""<default-arg-expression>"")
    {
        Console.WriteLine(callerName);
        Console.WriteLine(argumentExp);
    }
}

[My]
public class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);
            CompileAndVerify(compilation, expectedOutput: @"<default-caller-name>
<default-arg-expression>").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpressionIsReferringToItself_AttributeConstructor()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MyAttribute : Attribute
{
    public MyAttribute(
        [CallerArgumentExpression(""p"")] string p = ""<default>"")
    {
        Console.WriteLine(p);
    }
}

[My]
[My(""value"")]
public class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttributes(typeof(MyAttribute));
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);
            CompileAndVerify(compilation, expectedOutput: @"<default>
value").VerifyDiagnostics(
                // (10,10): warning CS8965: The CallerArgumentExpressionAttribute applied to parameter 'p' will have no effect because it's self-referential.
                //         [CallerArgumentExpression("p")] string p = "<default>")
                Diagnostic(ErrorCode.WRN_CallerArgumentExpressionAttributeSelfReferential, "CallerArgumentExpression").WithArguments("p").WithLocation(10, 10)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpressionIsReferringToItself_AttributeConstructor_Metadata()
        {
            string il = @"
.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi beforefieldinit MyAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 04 00 00 00 01 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 01
    )
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            [opt] string p
        ) cil managed 
    {
        .param [1] = ""<default>""
            .custom instance void [mscorlib]System.Runtime.CompilerServices.CallerArgumentExpressionAttribute::.ctor(string) = (
                01 00 01 70 00 00
            )
        // Method begins at RVA 0x2050
        // Code size 16 (0x10)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: nop
        IL_0008: ldarg.1
        IL_0009: call void [mscorlib]System.Console::WriteLine(string)
        IL_000e: nop
        IL_000f: ret
    } // end of method MyAttribute::.ctor

} // end of class MyAttribute
";
            string source = @"
using System.Reflection;

[My]
[My(""value"")]
public class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttributes(typeof(MyAttribute));
    }
}
";

            var compilation = CreateCompilationWithIL(source, il, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);
            CompileAndVerify(compilation, expectedOutput: @"<default>
value").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpressionInAttributeConstructor()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

public class MyAttribute : Attribute
{
    public MyAttribute(string s, [CallerArgumentExpression(""s"")] string x = """") => Console.WriteLine($""'{s}', '{x}'"");
}

[My(""Hello"")]
public class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "'Hello', '\"Hello\"'").VerifyDiagnostics();
            var namedType = compilation.GetTypeByMetadataName("Program").GetPublicSymbol();
            var attributeArguments = namedType.GetAttributes().Single().ConstructorArguments;
            Assert.Equal(2, attributeArguments.Length);
            Assert.Equal("Hello", attributeArguments[0].Value);
            Assert.Equal("\"Hello\"", attributeArguments[1].Value);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpressionInAttributeConstructor_NamedArgument()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

public class MyAttribute : Attribute
{
    public MyAttribute(string s, [CallerArgumentExpression(""s"")] string x = """") => Console.WriteLine($""'{s}', '{x}'"");
}

[My(s:""Hello"")]
public class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "'Hello', '\"Hello\"'").VerifyDiagnostics();
            var namedType = compilation.GetTypeByMetadataName("Program").GetPublicSymbol();
            var attributeArguments = namedType.GetAttributes().Single().ConstructorArguments;
            Assert.Equal(2, attributeArguments.Length);
            Assert.Equal("Hello", attributeArguments[0].Value);
            Assert.Equal("\"Hello\"", attributeArguments[1].Value);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpressionInAttributeConstructor_NamedArgumentsSameOrder()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

public class MyAttribute : Attribute
{
    public MyAttribute(string s, string s2, [CallerArgumentExpression(""s"")] string x = """") => Console.WriteLine($""'{s}', '{s2}', '{x}'"");
}

[My(s:""Hello"", s2:""World"")]
public class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "'Hello', 'World', '\"Hello\"'").VerifyDiagnostics();
            var namedType = compilation.GetTypeByMetadataName("Program").GetPublicSymbol();
            var attributeArguments = namedType.GetAttributes().Single().ConstructorArguments;
            Assert.Equal(3, attributeArguments.Length);
            Assert.Equal("Hello", attributeArguments[0].Value);
            Assert.Equal("World", attributeArguments[1].Value);
            Assert.Equal("\"Hello\"", attributeArguments[2].Value);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpressionInAttributeConstructor_NamedArgumentsOutOfOrder()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

public class MyAttribute : Attribute
{
    public MyAttribute(string s, string s2, [CallerArgumentExpression(""s"")] string x = """") => Console.WriteLine($""'{s}', '{s2}', '{x}'"");
}

[My(s2:""World"", s:""Hello"")]
public class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "'Hello', 'World', '\"Hello\"'").VerifyDiagnostics();
            var namedType = compilation.GetTypeByMetadataName("Program").GetPublicSymbol();
            var attributeArguments = namedType.GetAttributes().Single().ConstructorArguments;
            Assert.Equal(3, attributeArguments.Length);
            Assert.Equal("Hello", attributeArguments[0].Value);
            Assert.Equal("World", attributeArguments[1].Value);
            Assert.Equal("\"Hello\"", attributeArguments[2].Value);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpressionInAttributeConstructor_Complex()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MyAttribute : Attribute
{
    public MyAttribute([CallerArgumentExpression(""param2"")] string param1 = ""param1_default"", [CallerArgumentExpression(""param1"")] string param2 = ""param2_default"") => Console.WriteLine($""param1: {param1}, param2: {param2}"");
}

[My]
[My()]
[My(""param1_value"")]
[My(param1: ""param1_value"")]
[My(param2: ""param2_value"")]
[My(param1: ""param1_value"", param2: ""param2_value"")]
[My(param2: ""param2_value"", param1: ""param1_value"")]
public class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttributes(typeof(MyAttribute), false);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput:
@"param1: param1_default, param2: param2_default
param1: param1_default, param2: param2_default
param1: param1_value, param2: ""param1_value""
param1: param1_value, param2: ""param1_value""
param1: ""param2_value"", param2: param2_value
param1: param1_value, param2: param2_value
param1: param1_value, param2: param2_value").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpressionInAttributeConstructor_NamedAndOptionalParameters()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

class MyAttribute : Attribute
{
    public MyAttribute(int a = 1, int b = 2, int c = 3, [CallerArgumentExpression(""a"")] string expr_a = """", [CallerArgumentExpression(""b"")] string expr_b = """", [CallerArgumentExpression(""c"")] string expr_c = """")
    {
        Console.WriteLine($""'{a}', '{b}', '{c}', '{expr_a}', '{expr_b}', '{expr_c}'"");
        A = a;
        B = b;
        C = c;
    }

    public int X;
    public int A;
    public int B;
    public int C;
}

[My(0+0, c:1+1, X=2+2)]
class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
        _ = new MyAttribute(0+0, c: 1+1);
    }
}";
            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: @"'0', '2', '2', '0+0', '', '1+1'
'0', '2', '2', '0+0', '', '1+1'").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpressionInAttributeConstructor_OptionalAndFieldInitializer()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

class MyAttribute : Attribute
{
    public MyAttribute([CallerArgumentExpression(""a"")] string expr_a = ""<default0>"", string a = ""<default1>"")
    {
        Console.WriteLine($""'{a}', '{expr_a}'"");
    }

    public int A;
    public int B;
}

[My(A=1, B=2)]
class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
    }
}";
            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "'<default1>', '<default0>'").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpressionInAttributeConstructor_LangVersion9()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

class MyAttribute : Attribute
{
    public MyAttribute(int a = 1, [CallerArgumentExpression(""a"")] string expr_a = """")
    {
        Console.WriteLine($""'{a}', '{expr_a}'"");
    }
}

[My(1+2)]
class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
    }
}";
            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);
            CompileAndVerify(compilation, expectedOutput: "'3', '1+2'").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpression_ImplicitConversionFromStringExists()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

class MyAttribute : Attribute
{
    public MyAttribute(int a = 1, [CallerArgumentExpression(""a"")] object expr_a = null)
    {
        Console.WriteLine($""'{a}', '{expr_a}'"");
    }
}

[My(1+2)]
class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
    }
}";
            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: @"'3', '1+2'").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpression_ImplicitConversionFromStringDoesNotExists()
        {
            string source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

class MyAttribute : Attribute
{
    public MyAttribute(int a = 1, [CallerArgumentExpression(""a"")] int expr_a = 0)
    {
        Console.WriteLine($""'{a}', '{expr_a}'"");
    }
}

[My(1+2)]
class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
    }
}";
            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            compilation.VerifyDiagnostics(
                // (8,36): error CS8913: CallerArgumentExpressionAttribute cannot be applied because there are no standard conversions from type 'string' to type 'int'
                //     public MyAttribute(int a = 1, [CallerArgumentExpression("a")] int expr_a = 0)
                Diagnostic(ErrorCode.ERR_NoConversionForCallerArgumentExpressionParam, "CallerArgumentExpression").WithArguments("string", "int").WithLocation(8, 36)
            );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestArgumentExpression_ImplicitConversionFromStringDoesNotExists_Metadata()
        {
            string il = @"
.class public auto ansi beforefieldinit MyAttribute
    extends [mscorlib]System.Attribute
{
    // Methods
    .method public hidebysig specialname rtspecialname
        instance void .ctor (
            [opt] int32 a,
            [opt] int32 expr_a
        ) cil managed
    {
        .param [1] = int32(1)
        .param [2] = int32(0)
            .custom instance void [mscorlib]System.Runtime.CompilerServices.CallerArgumentExpressionAttribute::.ctor(string) = (
                01 00 01 61 00 00
            )
        // Method begins at RVA 0x2050
        // Code size 37 (0x25)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: nop
        IL_0008: ldstr ""'{0}', '{1}'""
        IL_000d: ldarg.1
        IL_000e: box [mscorlib]System.Int32
        IL_0013: ldarg.2
        IL_0014: box [mscorlib]System.Int32
        IL_0019: call string [mscorlib]System.String::Format(string, object, object)
        IL_001e: call void [mscorlib]System.Console::WriteLine(string)
        IL_0023: nop
        IL_0024: ret
    } // end of method MyAttribute::.ctor

} // end of class MyAttribute
";

            string source = @"
using System.Reflection;

[My(1+2)]
class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttribute(typeof(MyAttribute));
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, il, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: "'3', '0'").VerifyDiagnostics();
            var arguments = compilation.GetTypeByMetadataName("Program").GetAttributes().Single().CommonConstructorArguments;
            Assert.Equal(2, arguments.Length);
            Assert.Equal(3, arguments[0].Value);
            Assert.Equal(0, arguments[1].Value);
        }
        #endregion

        #region CallerArgumentExpression - Test various symbols
        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestIndexers()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    const string i = nameof(i);
    public int this[int i, [CallerArgumentExpression(i)] string s = ""<default-arg>""]
    {
        get => i;
        set => Console.WriteLine($""{i}, {s}"");
    }

    public static void Main()
    {
        new Program()[1+  1] = 5;
        new Program()[2+  2, ""explicit-value""] = 5;
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: @"2, 1+  1
4, explicit-value").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestDelegate()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    const string s1 = nameof(s1);
    delegate void D(string s1, ref string s2, out string s3, [CallerArgumentExpression(s1)] string s4 = """");

    static void M(string s1, ref string s2, out string s3, string s4 = """")
    {
        s3 = """";
        Console.WriteLine($""s1: {s1}"");
        Console.WriteLine($""s2: {s2}"");
        Console.WriteLine($""s3: {s3}"");
        Console.WriteLine($""s4: {s4}"");
    }

    public static void Main()
    {
        D d = M;
        string s2 = ""s2-arg"";
        d.Invoke(""s1-arg"", ref s2, out _);
        //d.EndInvoke(ref s2, out _, null);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: @"s1: s1-arg
s2: s2-arg
s3: 
s4: ""s1-arg""").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestDelegate2()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    const string s3 = nameof(s3);
    delegate void D(string s1, ref string s2, out string s3, [CallerArgumentExpression(s3)] string s4 = """");

    static void M(string s1, ref string s2, out string s3, string s4 = """")
    {
        s3 = """";
        Console.WriteLine($""s1: {s1}"");
        Console.WriteLine($""s2: {s2}"");
        Console.WriteLine($""s3: {s3}"");
        Console.WriteLine($""s4: {s4}"");
    }

    public static void Main()
    {
        D d = M;
        string s2 = ""s2-arg"";
        d.Invoke(""s1-arg"", ref s2, out _);
        //d.EndInvoke(ref s2, out _, null);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation, expectedOutput: @"s1: s1-arg
s2: s2-arg
s3: 
s4: _").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestDelegate3()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    const string s1 = nameof(s1);
    delegate void D(string s1, ref string s2, out string s3, [CallerArgumentExpression(s1)] string s4 = """");

    static void M(string s1, ref string s2, out string s3, string s4 = """")
    {
        s3 = """";
        Console.WriteLine($""s1: {s1}"");
        Console.WriteLine($""s2: {s2}"");
        Console.WriteLine($""s3: {s3}"");
        Console.WriteLine($""s4: {s4}"");
    }

    public static void Main()
    {
        D d = M;
        string s2 = ""s2-arg"";
        d.EndInvoke(ref s2, out _, null);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.Net50, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation).VerifyDiagnostics().VerifyIL("Program.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  4
  .locals init (string V_0, //s2
                string V_1)
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void Program.M(string, ref string, out string, string)""
  IL_0007:  newobj     ""Program.D..ctor(object, System.IntPtr)""
  IL_000c:  ldstr      ""s2-arg""
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  ldloca.s   V_1
  IL_0016:  ldnull
  IL_0017:  callvirt   ""void Program.D.EndInvoke(ref string, out string, System.IAsyncResult)""
  IL_001c:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestDelegate4()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    const string s3 = nameof(s3);
    delegate void D(string s1, ref string s2, out string s3, [CallerArgumentExpression(s3)] string s4 = """");

    static void M(string s1, ref string s2, out string s3, string s4 = """")
    {
        s3 = """";
        Console.WriteLine($""s1: {s1}"");
        Console.WriteLine($""s2: {s2}"");
        Console.WriteLine($""s3: {s3}"");
        Console.WriteLine($""s4: {s4}"");
    }

    public static void Main()
    {
        D d = M;
        string s2 = ""s2-arg"";
        d.EndInvoke(ref s2, out _, null);
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.Net50, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(compilation).VerifyDiagnostics().VerifyIL("Program.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  4
  .locals init (string V_0, //s2
                string V_1)
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void Program.M(string, ref string, out string, string)""
  IL_0007:  newobj     ""Program.D..ctor(object, System.IntPtr)""
  IL_000c:  ldstr      ""s2-arg""
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  ldloca.s   V_1
  IL_0016:  ldnull
  IL_0017:  callvirt   ""void Program.D.EndInvoke(ref string, out string, System.IAsyncResult)""
  IL_001c:  ret
}
");
        }
        #endregion

        [Fact]
        public void TestCallerInfoAttributesWithSaneDefaultValues()
        {
            string source = @"
using System.Runtime.CompilerServices;

class Test {
    static void LogCallerLineNumber([CallerLineNumber] int lineNumber = -1) { }

    static void LogCallerFilePath([CallerFilePath] string filePath = """") { }

    static void LogCallerMemberName([CallerMemberName] string memberName = """") { }
}";

            CreateCompilationWithMscorlib461(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestBadCallerInfoAttributesWithoutDefaultValues()
        {
            string source = @"
using System.Runtime.CompilerServices;

class Test {
    static void LogCallerLineNumber([CallerLineNumber] int lineNumber) { }

    static void LogCallerFilePath([CallerFilePath] string filePath) { }

    static void LogCallerMemberName([CallerMemberName] string memberName) { }
}";

            CreateCompilationWithMscorlib461(source).VerifyDiagnostics(
                // (5,38): error CS4020: The CallerLineNumberAttribute may only be applied to parameters with default values
                //     static void LogCallerLineNumber([CallerLineNumber] int lineNumber) { }
                Diagnostic(ErrorCode.ERR_BadCallerLineNumberParamWithoutDefaultValue, @"CallerLineNumber").WithLocation(5, 38),

                // (7,36): error CS4021: The CallerFilePathAttribute may only be applied to parameters with default values
                //     static void LogCallerFilePath([CallerFilePath] string filePath) { }
                Diagnostic(ErrorCode.ERR_BadCallerFilePathParamWithoutDefaultValue, @"CallerFilePath").WithLocation(7, 36),

                // (9,38): error CS4022: The CallerMemberNameAttribute may only be applied to parameters with default values
                //     static void LogCallerMemberName([CallerMemberName] string memberName) { }
                Diagnostic(ErrorCode.ERR_BadCallerMemberNameParamWithoutDefaultValue, @"CallerMemberName").WithLocation(9, 38));
        }

        [Fact]
        public void TestConversionForCallerLineNumber()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;

class Test {
    static void LogCallerLineNumber1([CallerLineNumber, Optional, DefaultParameterValue(1 )] decimal lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber2([CallerLineNumber, Optional, DefaultParameterValue(2 )] double  lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber3([CallerLineNumber, Optional, DefaultParameterValue(3 )] float   lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber4([CallerLineNumber, Optional, DefaultParameterValue(4 )] int     lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber5([CallerLineNumber, Optional, DefaultParameterValue(5u)] uint    lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber6([CallerLineNumber, Optional, DefaultParameterValue(6 )] long    lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber7([CallerLineNumber, Optional, DefaultParameterValue(7u)] ulong   lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber8([CallerLineNumber, Optional, DefaultParameterValue(8 )] object  lineNumber) { Console.WriteLine(""line: "" + lineNumber); }

    static void LogCallerLineNumber9 ([CallerLineNumber] decimal lineNumber =  9  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber10([CallerLineNumber] double  lineNumber = 10  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber11([CallerLineNumber] float   lineNumber = 11  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber12([CallerLineNumber] int     lineNumber = 12  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber13([CallerLineNumber] uint    lineNumber = 13  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber14([CallerLineNumber] long    lineNumber = 14  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber15([CallerLineNumber] ulong   lineNumber = 15  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber16([CallerLineNumber] object  lineNumber = null) { Console.WriteLine(""line: "" + lineNumber); }

    static void LogCallerLineNumber17([CallerLineNumber, Optional, DefaultParameterValue(17 )] decimal? lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber18([CallerLineNumber, Optional, DefaultParameterValue(18 )] double?  lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber19([CallerLineNumber, Optional, DefaultParameterValue(19 )] float?   lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber20([CallerLineNumber, Optional, DefaultParameterValue(20 )] int?     lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber21([CallerLineNumber, Optional, DefaultParameterValue(21u)] uint?    lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber22([CallerLineNumber, Optional, DefaultParameterValue(22 )] long?    lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber23([CallerLineNumber, Optional, DefaultParameterValue(23u)] ulong?   lineNumber) { Console.WriteLine(""line: "" + lineNumber); }

    static void LogCallerLineNumber25([CallerLineNumber] decimal? lineNumber = 25  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber26([CallerLineNumber] double?  lineNumber = 26  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber27([CallerLineNumber] float?   lineNumber = 27  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber28([CallerLineNumber] int?     lineNumber = 28  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber29([CallerLineNumber] uint?    lineNumber = 29  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber30([CallerLineNumber] long?    lineNumber = 30  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber31([CallerLineNumber] ulong?   lineNumber = 31  ) { Console.WriteLine(""line: "" + lineNumber); }

    static void LogCallerFilePath1([CallerFilePath] string filePath = """") { }
    static void LogCallerFilePath2([CallerFilePath] object filePath = null) { }
    static void LogCallerFilePath3([CallerFilePath] IComparable filePath = null) { }

    static void LogCallerMemberName1([CallerMemberName] string memberName = """") { }
    static void LogCallerMemberName2([CallerMemberName] object memberName = null) { }
    static void LogCallerMemberName3([CallerMemberName] IComparable memberName = null) { }

    public static void Main() {
        LogCallerLineNumber1();
        LogCallerLineNumber2();
        LogCallerLineNumber3();
        LogCallerLineNumber4();
        LogCallerLineNumber5();
        LogCallerLineNumber6();
        LogCallerLineNumber7();
        LogCallerLineNumber8();
        LogCallerLineNumber9();
        LogCallerLineNumber10();
        LogCallerLineNumber11();
        LogCallerLineNumber12();
        LogCallerLineNumber13();
        LogCallerLineNumber14();
        LogCallerLineNumber15();
        LogCallerLineNumber16();
        LogCallerLineNumber17();
        LogCallerLineNumber18();
        LogCallerLineNumber19();
        LogCallerLineNumber20();
        LogCallerLineNumber21();
        LogCallerLineNumber22();
        LogCallerLineNumber23();
        LogCallerLineNumber25();
        LogCallerLineNumber26();
        LogCallerLineNumber27();
        LogCallerLineNumber28();
        LogCallerLineNumber29();
        LogCallerLineNumber30();
        LogCallerLineNumber31();
    }
}";

            string expected = @"
line: 50
line: 51
line: 52
line: 53
line: 54
line: 55
line: 56
line: 57
line: 58
line: 59
line: 60
line: 61
line: 62
line: 63
line: 64
line: 65
line: 66
line: 67
line: 68
line: 69
line: 70
line: 71
line: 72
line: 73
line: 74
line: 75
line: 76
line: 77
line: 78
line: 79
";
            var compilation = CreateCompilationWithMscorlib461(source, new MetadataReference[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestDelegateInvoke()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

class Test {
    static void LogCallerLineNumber1(int lineNumber = -1) { Console.WriteLine(""line: "" + lineNumber); }

    public static void Main() {
        List<Action> list = new List<Action>();
        list.Add(() => LogCallerLineNumber1());
        list.Add(() => LogCallerLineNumber1());
        list.Add(() => LogCallerLineNumber1());
        
        foreach (var x in list) {
            x();
        }
    }
}";

            string expected = @"
line: -1
line: -1
line: -1
";

            var compilation = CreateCompilationWithMscorlib461(source, new MetadataReference[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestConversionForCallerInfoAttributes()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;

class Test {
    static void LogCallerLineNumber1([CallerLineNumber, Optional, DefaultParameterValue(1)] int lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber2([CallerLineNumber] long lineNumber   = 2) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber3([CallerLineNumber] double lineNumber = 3) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber4([CallerLineNumber] float lineNumber  = 4) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber5([CallerLineNumber] int? lineNumber   = 5) { Console.WriteLine(""line: "" + lineNumber); }

    static void LogCallerFilePath1([CallerFilePath] string filePath = """") { }
    static void LogCallerFilePath2([CallerFilePath] object filePath = null) { }
    static void LogCallerFilePath3([CallerFilePath] IComparable filePath = null) { }

    static void LogCallerMemberName1([CallerMemberName] string memberName = """") { }
    static void LogCallerMemberName2([CallerMemberName] object memberName = null) { }
    static void LogCallerMemberName3([CallerMemberName] IComparable memberName = null) { }

    public static void Main() {
        LogCallerLineNumber1();
        LogCallerLineNumber2();
        LogCallerLineNumber3();
        LogCallerLineNumber4();
        LogCallerLineNumber5();
    }
}";

            string expected = @"
line: 22
line: 23
line: 24
line: 25
line: 26
";

            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestBadConversionForCallerInfoAttributes()
        {
            string source = @"
using System.Runtime.CompilerServices;

class Test {
    static void LogCallerLineNumber1([CallerLineNumber] string lineNumber = """") { }
    static void LogCallerLineNumber2([CallerLineNumber] char lineNumber = '\0') { }
    static void LogCallerLineNumber3([CallerLineNumber] bool lineNumber = false) { }
    static void LogCallerLineNumber3([CallerLineNumber] short lineNumber = 0) { }
    static void LogCallerLineNumber3([CallerLineNumber] ushort lineNumber = 0) { }

    static void LogCallerFilePath1([CallerFilePath] int filePath = 0) { }
    static void LogCallerFilePath2([CallerFilePath] long filePath = 0) { }
    static void LogCallerFilePath3([CallerFilePath] double filePath = 0) { }
    static void LogCallerFilePath4([CallerFilePath] float filePath = 0) { }
    static void LogCallerFilePath5([CallerFilePath] int? filePath = 0) { }

    static void LogCallerMemberName1([CallerMemberName] int memberName = 0) { }
    static void LogCallerMemberName2([CallerMemberName] long memberName = 0) { }
    static void LogCallerMemberName3([CallerMemberName] double memberName = 0) { }
    static void LogCallerMemberName4([CallerMemberName] float memberName = 0) { }
    static void LogCallerMemberName5([CallerMemberName] int? memberName = 0) { }
}";

            CreateCompilationWithMscorlib461(source, references: new MetadataReference[] { SystemRef }).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(5, 39).WithArguments("int", "string"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(6, 39).WithArguments("int", "char"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(7, 39).WithArguments("int", "bool"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(8, 39).WithArguments("int", "short"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(9, 39).WithArguments("int", "ushort"),

                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(11, 37).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(12, 37).WithArguments("string", "long"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(13, 37).WithArguments("string", "double"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(14, 37).WithArguments("string", "float"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(15, 37).WithArguments("string", "int?"),

                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(17, 39).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(18, 39).WithArguments("string", "long"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(19, 39).WithArguments("string", "double"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(20, 39).WithArguments("string", "float"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(21, 39).WithArguments("string", "int?"));
        }

        [Fact]
        public void TestCallerLineNumber()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class Test
{
    static void Log(
        string message,
        [CallerLineNumber] int lineNumber = -1)
    {
        Console.WriteLine(""message: "" + message);
        Console.WriteLine(""line: "" + lineNumber);
    }

    public static void Main()
    {
        Log(""something happened"");
        // comment
        Log
            // comment
            (
            // comment
            ""something happened""
            // comment
            )
            // comment
            ;
        // comment
    }
}";

            var expected = @"
message: something happened
line: 17
message: something happened
line: 21
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerLineNumber_LocalFunctionAttribute()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class Test
{
    public static void Main()
    {
        log(""something happened"");
        // comment
        log
            // comment
            (
            // comment
            ""something happened""
            // comment
            )
            // comment
            ;
        // comment

        static void log(
            string message,
            [CallerLineNumber] int lineNumber = -1)
        {
            Console.WriteLine(""message: "" + message);
            Console.WriteLine(""line: "" + lineNumber);
        }
    }
}";

            var expected = @"
message: something happened
line: 9
message: something happened
line: 13
";

            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerLineNumberImplicitCall()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class A
{
    public A([CallerLineNumber] int lineNumber = -1)
    {
        Console.WriteLine(""line: "" + lineNumber);
    }
}

class B : A
{
}

class Test
{
    public static void Main()
    {
        new B();
    }
}";

            var expected = @"
line: -1
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerLineNumberConstructorCall()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class A
{
    public A([CallerLineNumber] int lineNumber = -1)
    {
        Console.WriteLine(""line: "" + lineNumber);
    }
}

class Test
{
    public static void Main()
    {
        new A();
    }
}";

            var expected = @"
line: 17
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerLineNumberCustomAttributeConstructorCall()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Reflection;
using System;

class MyCallerInfoAttribute : Attribute
{
    public MyCallerInfoAttribute(
        string message,
        [CallerLineNumber] int lineNumber = -1)
    {
        Console.WriteLine(""message: "" + message);
        Console.WriteLine(""line: "" + lineNumber);
    }
}

class MyCallerInfo2Attribute : Attribute
{
    public MyCallerInfo2Attribute([CallerLineNumber] int lineNumber = -1)
    {
        Console.WriteLine(""line: "" + lineNumber);
    }
}

[MyCallerInfo(""this is a message"")]
class A
{
}

// comment
[
// comment
MyCallerInfo
    // comment
    (
    // comment
    ""this is a message""
    // comment
    )
// comment
]
// comment
class B
{
}

[MyCallerInfo2]
class C
{
}

// comment
[
// comment
MyCallerInfo2
// comment
]
// comment
class D
{
}

class Test
{
    public static void Main()
    {
        typeof(A).GetCustomAttribute(typeof(MyCallerInfoAttribute), false);
        typeof(B).GetCustomAttribute(typeof(MyCallerInfoAttribute), false);
        typeof(C).GetCustomAttribute(typeof(MyCallerInfo2Attribute), false);
        typeof(D).GetCustomAttribute(typeof(MyCallerInfo2Attribute), false);
    }
}";

            var expected = @"
message: this is a message
line: 25
message: this is a message
line: 33
line: 47
line: 55
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerLineNumberMemberCall()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

public class B
{
    public void Log([CallerLineNumber] int a = -1)
    {
        Console.WriteLine(""line: "" + a);
    }
}

class Test
{
    public static void Main()
    {
        new B().Log();
    }
}";

            var expected = @"
line: 17
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestBadCallerLineNumberMetadata()
        {
            var iLSource = @"
.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  LogCallerLineNumber1([opt] int32 lineNumber) cil managed
  {
    .param [1] = int32(0xFFFFFFFF)
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CallerLineNumberAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      ""line: ""
    IL_0006:  ldarg.0
    IL_0007:  box        [mscorlib]System.Int32
    IL_000c:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_0011:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0016:  nop
    IL_0017:  ret
  } // end of method Test::LogCallerLineNumber1

  .method public hidebysig static void  LogCallerLineNumber2(int32 lineNumber) cil managed
  {
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CallerLineNumberAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      ""line: ""
    IL_0006:  ldarg.0
    IL_0007:  box        [mscorlib]System.Int32
    IL_000c:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_0011:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0016:  nop
    IL_0017:  ret
  } // end of method Test::LogCallerLineNumber2

  .method public hidebysig static void  LogCallerLineNumber3([opt] string lineNumber) cil managed
  {
	.param [1] = ""hello""
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CallerLineNumberAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       19 (0x13)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      ""line: ""
    IL_0006:  ldarg.0
    IL_0007:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_000c:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0011:  nop
    IL_0012:  ret
  } // end of method Test::LogCallerLineNumber3

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test
";

            var source = @"
using System.Runtime.CompilerServices;
using System;

class Driver {
    public static void Main() {
        Test.LogCallerLineNumber1();
        Test.LogCallerLineNumber2(42);
        Test.LogCallerLineNumber3();
    }
}
";

            var expected = @"
line: 7
line: 42
line: hello
";

            MetadataReference libReference = CompileIL(iLSource);
            var compilation = CreateCompilationWithMscorlib461(source, new[] { libReference }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerLineNumberDuplicateAttribute()
        {
            string source = @"
using System.Runtime.CompilerServices;

partial class D
{
    partial void Goo([CallerLineNumber] int x = 2);
}

partial class D
{
    partial void Goo([CallerLineNumber] int x)
    {
    }

    public static void Main()
    {
    }
}";

            CreateCompilationWithMscorlib461(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "CallerLineNumber").WithArguments("CallerLineNumber"),
                Diagnostic(ErrorCode.WRN_CallerLineNumberParamForUnconsumedLocation, "CallerLineNumber").WithArguments("x").WithLocation(11, 23));
        }

        [Fact, WorkItem(531044, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531044")]
        public void TestUnconsumedCallerInfoAttributes()
        {
            string source = @"
using System.Runtime.CompilerServices;

partial class D
{
    partial void Goo(int line, string member, string path);
}

partial class D
{
    partial void Goo(
        [CallerLineNumber] int line,
        [CallerMemberName] string member,
        [CallerFilePath] string path) { }

    public static void Main()
    {
    }
}";

            CreateCompilationWithMscorlib461(source).VerifyDiagnostics(
                // (12,10): warning CS4024: The CallerLineNumberAttribute applied to parameter 'line' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //         [CallerLineNumber] int line,
                Diagnostic(ErrorCode.WRN_CallerLineNumberParamForUnconsumedLocation, "CallerLineNumber").WithArguments("line"),
                // (13,10): warning CS4026: The CallerMemberNameAttribute applied to parameter 'member' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //         [CallerMemberName] string member,
                Diagnostic(ErrorCode.WRN_CallerMemberNameParamForUnconsumedLocation, "CallerMemberName").WithArguments("member"),
                // (14,10): warning CS4025: The CallerFilePathAttribute applied to parameter 'path' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //         [CallerFilePath] string path) { }
                Diagnostic(ErrorCode.WRN_CallerFilePathParamForUnconsumedLocation, "CallerFilePath").WithArguments("path"));

            CompileAndVerify(source, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: verify);

            void verify(ModuleSymbol module)
            {
                // https://github.com/dotnet/roslyn/issues/73482
                // These are ignored in source but they still get written out to metadata.
                // This means if the method is accessible from another compilation, then the attribute will be respected there, but not in the declaring compilation.
                var goo = module.GlobalNamespace.GetMember<MethodSymbol>("D.Goo");
                AssertEx.Equal(["System.Runtime.CompilerServices.CallerLineNumberAttribute"], goo.Parameters[0].GetAttributes().SelectAsArray(attr => attr.ToString()));
                AssertEx.Equal(["System.Runtime.CompilerServices.CallerMemberNameAttribute"], goo.Parameters[1].GetAttributes().SelectAsArray(attr => attr.ToString()));
                AssertEx.Equal(["System.Runtime.CompilerServices.CallerFilePathAttribute"], goo.Parameters[2].GetAttributes().SelectAsArray(attr => attr.ToString()));
            }
        }

        [Fact]
        public void TestCallerLineNumberViaDelegate()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class Test
{
    public static void Log([CallerLineNumber] int x = 1)
    {
        Console.WriteLine(""line: "" + x);
    }

    delegate void Del([CallerLineNumber] int x = 1);

    public static void Main()
    {
        Log();
        Del d = new Del(Log);
        d();
    }
}";

            var expected = @"
line: 16
line: 18
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestBadConversionCallerInfoMultipleAttributes()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class Test
{
    public static void Log1([CallerLineNumber, CallerFilePath, CallerMemberName] int x = 1) { Console.WriteLine(""line: "" + x); }
    public static void Log2([CallerLineNumber, CallerMemberName, CallerFilePath] int x = 1) { Console.WriteLine(""line: "" + x); }
    public static void Log3([CallerMemberName, CallerLineNumber, CallerFilePath] int x = 1) { Console.WriteLine(""line: "" + x); }
    public static void Log4([CallerMemberName, CallerFilePath, CallerLineNumber] int x = 1) { Console.WriteLine(""line: "" + x); }
    public static void Log5([CallerFilePath, CallerMemberName, CallerLineNumber] int x = 1) { Console.WriteLine(""line: "" + x); }
    public static void Log6([CallerFilePath, CallerLineNumber, CallerMemberName] int x = 1) { Console.WriteLine(""line: "" + x); }

    public static void Log7([CallerLineNumber, CallerFilePath, CallerMemberName] string x = """") { }
    public static void Log8([CallerLineNumber, CallerMemberName, CallerFilePath] string x = """") { }
    public static void Log9([CallerMemberName, CallerLineNumber, CallerFilePath] string x = """") { }
    public static void Log10([CallerMemberName, CallerFilePath, CallerLineNumber] string x = """") { }
    public static void Log11([CallerFilePath, CallerMemberName, CallerLineNumber] string x = """") { }
    public static void Log12([CallerFilePath, CallerLineNumber, CallerMemberName] string x = """") { }

    public static void Log13([CallerFilePath, CallerMemberName] string x = """") { }
    public static void Log14([CallerMemberName, CallerFilePath] string x = """") { }

    public static void Log15([CallerLineNumber, CallerFilePath] string x = """") { }
    public static void Log16([CallerFilePath, CallerLineNumber] string x = """") { }

    public static void Log17([CallerMemberName, CallerLineNumber] string x = """") { }
    public static void Log18([CallerLineNumber, CallerMemberName] string x = """") { }

    public static void Log19([CallerFilePath, CallerMemberName] int x = 1) { }
    public static void Log20([CallerMemberName, CallerFilePath] int x = 1) { }

    public static void Log21([CallerLineNumber, CallerFilePath] int x = 1) { Console.WriteLine(""line: "" + x); }
    public static void Log22([CallerFilePath, CallerLineNumber] int x = 1) { Console.WriteLine(""line: "" + x); }

    public static void Log23([CallerMemberName, CallerLineNumber] int x = 1) { Console.WriteLine(""line: "" + x); }
    public static void Log24([CallerLineNumber, CallerMemberName] int x = 1) { Console.WriteLine(""line: "" + x); }
}";
            CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseDll.WithWarningLevel(0)).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(7, 48).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(7, 64).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(8, 48).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(8, 66).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(9, 30).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(9, 66).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(10, 30).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(10, 48).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(11, 30).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(11, 46).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(12, 30).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(12, 64).WithArguments("string", "int"),

                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(14, 30).WithArguments("int", "string"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(15, 30).WithArguments("int", "string"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(16, 48).WithArguments("int", "string"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(17, 65).WithArguments("int", "string"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(18, 65).WithArguments("int", "string"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(19, 47).WithArguments("int", "string"),

                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(24, 31).WithArguments("int", "string"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(25, 47).WithArguments("int", "string"),

                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(27, 49).WithArguments("int", "string"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(28, 31).WithArguments("int", "string"),

                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(30, 31).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(30, 47).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(31, 31).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(31, 49).WithArguments("string", "int"),

                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(33, 49).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(34, 31).WithArguments("string", "int"),

                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(36, 31).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(37, 49).WithArguments("string", "int"));
        }

        [Fact]
        public void TestCallerInfoMultipleAttributes()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class Test
{
    public static void Log7([CallerFilePath, CallerMemberName] string x = """") { Console.WriteLine(x); }
    public static void Log8([CallerMemberName, CallerFilePath] string x = """") { Console.WriteLine(x); }

    public static void Main()
    {
        Log7();
        Log8();
    }
}";

            var expected = @"
C:\file.cs
C:\file.cs
";

            var compilation = CreateCompilationWithMscorlib461(
                new[] { Parse(source, @"C:\file.cs") },
                new[] { SystemRef },
                TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerAttributeBash()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Reflection;
using System;

class MyCallerInfoAttribute : Attribute
{
    public MyCallerInfoAttribute(
        [CallerLineNumber] double lineNumber = -1)
    {
        Console.WriteLine(""line: "" + lineNumber);
    }
}

[MyCallerInfo]
class A
{
}

class Test
{
    public static void Main()
    {
        typeof(A).GetCustomAttribute(typeof(MyCallerInfoAttribute), false);
    }
}";

            var expected = @"
line: 15
";

            var compilation = CreateCompilationWithMscorlib461(source, references: new MetadataReference[] { SystemRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerLineNumberUnconsumedBadType()
        {
            string source = @"
using System.Runtime.CompilerServices;

partial class D
{
    partial void Goo(string x = """");
}

partial class D
{
    partial void Goo([CallerLineNumber] string x)
    {
    }

    public static void Main()
    {
    }
}";

            var compilation = CreateCompilationWithMscorlib461(source, references: new MetadataReference[] { SystemRef });
            compilation.VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_CallerLineNumberParamForUnconsumedLocation, "CallerLineNumber").WithArguments("x").WithLocation(11, 23));
        }

        [WorkItem(689618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/689618")]
        [Fact]
        public void TestCallerMemberNameUnconsumedBadType()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

partial class D
{
    partial void Goo(string x = """");
}

partial class D
{
    partial void Goo([CallerMemberName] string x)
    {
        Console.WriteLine(x);
    }

    public static void Main()
    {
        new D().Goo();
    }
}";

            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemRef }, TestOptions.ReleaseExe);

            compilation.VerifyEmitDiagnostics(
                // (12,23): warning CS4026: The CallerMemberNameAttribute applied to parameter 'x' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     partial void Goo([CallerMemberName] string x)
                Diagnostic(ErrorCode.WRN_CallerMemberNameParamForUnconsumedLocation, "CallerMemberName").WithArguments("x").WithLocation(12, 23));

            CompileAndVerify(compilation, expectedOutput: "");
        }

        [WorkItem(689618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/689618")]
        [Fact]
        public void TestCallerMemberNameUnconsumedBadType02()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

partial class D
{
    partial void Goo([CallerMemberName] string x = """");
}

partial class D
{
    partial void Goo(string x)
    {
        Console.WriteLine(x);
    }

    public static void Main()
    {
        new D().Goo();
    }
}";

            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "Main");
        }

        [Fact]
        public void TestCallerMemberName_Lambda()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class D
{
    public void LambdaCaller()
    {
        new Action(() =>
        {
            Test.Log();
        })();
    }
}

class Test
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
        var d = new D();
        d.LambdaCaller();
    }
}";

            var expected = @"
name: LambdaCaller
";

            var compilation = CreateCompilationWithMscorlib461(source, references: new MetadataReference[] { SystemRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_LocalFunction()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class D
{
    public void LocalFunctionCaller()
    {
        void Local()
        {
            void LocalNested() => Test.Log();
            LocalNested();
        }
        Local();
    }
}

class Test
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
        var d = new D();
        d.LocalFunctionCaller();
    }
}";

            var expected = @"
name: LocalFunctionCaller
";

            var compilation = CreateCompilationWithMscorlib461(source, references: new MetadataReference[] { SystemRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_LocalFunctionAttribute_01()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class D
{
    public void LocalFunctionCaller()
    {
        static int log([CallerMemberName] string callerName = """")
        {
            Console.WriteLine(""name: "" + callerName);
            return 1;
        }

        log();
    }
}

class Test
{

    public static void Main()
    {
        var d = new D();
        d.LocalFunctionCaller();
    }
}";

            var expected = @"
name: LocalFunctionCaller
";

            var compilation = CreateCompilation(
                source,
                options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.Regular9);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_LocalFunctionAttribute_02()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class D
{
    public void LocalFunctionCaller()
    {
        static void local1()
        {
            static void log([CallerMemberName] string callerName = """")
            {
                Console.WriteLine(""name: "" + callerName);
            }

            log();
        }

        local1();
    }
}

class Test
{

    public static void Main()
    {
        var d = new D();
        d.LocalFunctionCaller();
    }
}";

            var expected = @"
name: LocalFunctionCaller
";

            var compilation = CreateCompilation(
                source,
                options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.Regular9);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_LocalFunctionAttribute_03()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class D
{
    public void LocalFunctionCaller()
    {
        new Action(() =>
        {
            static void local1()
            {
                static void log([CallerMemberName] string callerName = """")
                {
                    Console.WriteLine(""name: "" + callerName);
                }

                log();
            }

            local1();
        }).Invoke();
    }
}

class Test
{

    public static void Main()
    {
        var d = new D();
        d.LocalFunctionCaller();
    }
}";

            var expected = @"
name: LocalFunctionCaller
";

            var compilation = CreateCompilation(
                source,
                options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.Regular9);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_Operator()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class D
{
    public static D operator ++(D d)
    {
        Test.Log();
        return d;
    }
}

class Test
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
        var d = new D();
        d++;
    }
}";

            var expected = @"
name: op_Increment
";

            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_Property()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class D
{
    public bool IsTrue
    {
        get
        {
            Test.Log();
            return true;
        }
        set
        {
            Test.Log();
        }
    }
}

class Test
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
        var d = new D();
        bool truth = d.IsTrue;
        d.IsTrue = truth;
    }
}";

            var expected = @"
name: IsTrue
name: IsTrue
";

            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_CustomAttribute()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class DummyAttribute : Attribute
{
    public DummyAttribute([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
    }
}

class A
{
    [Dummy]
    public void MyMethod() {
    }
}

class Test
{
    public static void Main()
    {
         typeof(A).GetMethod(""MyMethod"").GetCustomAttributes(typeof(DummyAttribute), false);
    }
}";

            var expected = @"
name: MyMethod
";

            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_Generic()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class G
{
    public static int Compare<T>(T a, T b) where T : IComparable {
        A.Log();
        return a.CompareTo(b);
    }
}

class A
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
         G.Compare<int>(1, 2);
    }
}";

            var expected = @"
name: Compare
";

            var compilation = CreateCompilationWithMscorlib461(
                source,
                new[] { SystemRef },
                TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_ExplicitInterfaceInstantiation()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

interface I
{
    int Add(int a, int b);

    bool HasThing { get; }
}

class II : I
{
    int I.Add(int a, int b)
    {
        A.Log();
        return a + b;
    }

    bool I.HasThing
    {
        get
        {
            A.Log();
            return false;
        }
    }
}

class A
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
        var ii = new II();
        ((I)ii).Add(1, 2);
        bool truth = ((I)ii).HasThing;
    }
}";

            var expected = @"
name: Add
name: HasThing
";

            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_Event()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class E
{
    public event Action ThingHappened
    {
        add { A.Log(); }
        remove { A.Log(); }
    }
}

class A
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
        Action goo = new Action(() => { });
        var e = new E();
        e.ThingHappened += goo;
        e.ThingHappened -= goo;
    }
}";

            var expected = @"
name: ThingHappened
name: ThingHappened
";

            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void TestCallerMemberName_ConstructorDestructor()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class D
{
    static D()
    {
        A.Log();
    }

    public D()
    {
        A.Log();
    }

    ~D()
    {
        A.Log();
    }
}

class A
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
        D d = new D();
        d = null;
        GC.Collect(GC.MaxGeneration);
        GC.WaitForPendingFinalizers();
    }
}";

            var expected = @"
name: .cctor
name: .ctor
name: Finalize
";

            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_Indexer()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class D
{
    [IndexerName(""TheIndexer"")]
    public int this[int index]
    {
        get
        {
            A.Log();
            return -1;
        }
        set
        {
            A.Log();
        }
    }
}

class DX
{
    public int this[int index]
    {
        get
        {
            A.Log();
            return -1;
        }
        set
        {
            A.Log();
        }
    }
}

class A
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
        {
            var d = new D();
            int i = d[0];
            d[0] = i;
        }
        {
            var d = new DX();
            int i = d[0];
            d[0] = i;
        }
    }
}";

            var expected = @"
name: TheIndexer
name: TheIndexer
name: Item
name: Item
";

            var compilation = CreateCompilationWithMscorlib461(source, references: new MetadataReference[] { SystemRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerFilePath1()
        {
            string source1 = @"
using System.Runtime.CompilerServices;
using System;

partial class A
{
    static int i;

    public static void Log([CallerFilePath] string filePath = """")
    {
        Console.WriteLine(""{0}: '{1}'"", ++i, filePath);
    }

    public static void Main()
    {
        Log();
        Main2();
        Main3();
        Main4();
    }
}";
            string source2 = @"partial class A { static void Main2() { Log(); } }";
            string source3 = @"partial class A { static void Main3() { Log(); } }";
            string source4 = @"partial class A { static void Main4() { Log(); } }";

            var compilation = CreateCompilationWithMscorlib461(
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(source1, path: @"C:\filename", encoding: Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source2, path: @"a\b\..\c\d", encoding: Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source3, path: @"*", encoding: Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source4, path: @"       ", encoding: Encoding.UTF8),
                },
                new[] { SystemRef },
                TestOptions.ReleaseExe.WithSourceReferenceResolver(SourceFileResolver.Default));

            CompileAndVerify(compilation, expectedOutput: @"
1: 'C:\filename'
2: 'a\b\..\c\d'
3: '*'
4: '       '
");
        }

        [Fact]
        public void TestCallerFilePath_LocalFunctionAttribute()
        {
            string source1 = @"
using System.Runtime.CompilerServices;
using System;

partial class A
{
    static int i;

    public static void Main()
    {
        log();
        log();

        static void log([System.Runtime.CompilerServices.CallerFilePathAttribute] string filePath = """")
        {
            Console.WriteLine(""{0}: '{1}'"", ++i, filePath);
        }
    }
}";

            var compilation = CreateCompilation(
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.Regular9, path: @"C:\filename", encoding: Encoding.UTF8)
                },
                options: TestOptions.ReleaseExe.WithSourceReferenceResolver(SourceFileResolver.Default));

            CompileAndVerify(compilation, expectedOutput: @"
1: 'C:\filename'
2: 'C:\filename'
");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.TestExecutionHasNewLineDependency)]
        public void TestCallerFilePath2()
        {
            string source1 = @"
using System.Runtime.CompilerServices;
using System;

partial class A
{
    static int i;

    public static void Log([CallerFilePath] string filePath = """")
    {
        Console.WriteLine(""{0}: '{1}'"", ++i, filePath);
    }

    public static void Main()
    {
        Log();
        Main2();
        Main3();
        Main4();
        Main5();
    }
}";
            string source2 = @"partial class A { static void Main2() { Log(); } }";
            string source3 = @"
#line hidden
partial class A { static void Main3() { Log(); } }
";

            string source4 = @"
#line 30 ""abc""
partial class A { static void Main4() { Log(); } }
";
            string source5 = @"
#line 30 ""     ""
partial class A { static void Main5() { Log(); } }
";

            var compilation = CreateCompilationWithMscorlib461(
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(source1, path: @"C:\filename", encoding: Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source2, path: @"a\b\..\c\d.cs", encoding: Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source3, path: @"*", encoding: Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source4, path: @"C:\x.cs", encoding: Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source5, path: @"C:\x.cs", encoding: Encoding.UTF8),
                },
                new[] { SystemRef },
                TestOptions.ReleaseExe.WithSourceReferenceResolver(new SourceFileResolver(ImmutableArray<string>.Empty, baseDirectory: @"C:\A\B")));

            // On CoreClr the '*' is a legal path character
            // https://github.com/dotnet/docs/issues/4483
            var expectedStarPath = ExecutionConditionUtil.IsCoreClr
                ? @"C:\A\B\*"
                : "*";
            CompileAndVerify(compilation, expectedOutput: $@"
1: 'C:\filename'
2: 'C:\A\B\a\c\d.cs'
3: '{expectedStarPath}'
4: 'C:\abc'
5: '     '
");
        }

        [Fact]
        public void TestAssemblyAttribute()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: MyNamespace.MyCallerMemberName]

namespace MyNamespace
{

    class MyCallerMemberNameAttribute : Attribute
    {
        public MyCallerMemberNameAttribute(
            [CallerMemberName] string memberName = """")
        {
            Console.WriteLine(""member: "" + memberName);
        }
    }

    class B
    {
        [MyCallerMemberName]
        public static void MyMethod() { }
    }

    class A
    {
        public static void Main()
        {
            B b = new B();
            Type Type1;
            Type1 = b.GetType();

            typeof(B).GetMethod(""MyMethod"").GetCustomAttribute(typeof(MyCallerMemberNameAttribute));

            Assembly.GetAssembly(Type1).GetCustomAttribute(typeof(MyCallerMemberNameAttribute));
        }
    }
}";

            var expected = @"
member: MyMethod
member: 
";

            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberNameConversion()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;
using System.Reflection;

namespace MyNamespace
{

    class MyCallerMemberNameAttribute : Attribute
    {
        public MyCallerMemberNameAttribute(
            [CallerMemberName] object memberName = null)
        {
            Console.WriteLine(""member: "" + memberName);
        }
    }

    class B
    {
        [MyCallerMemberName]
        public static void MyMethod() { }
    }

    class A
    {
        public static void Main()
        {
            typeof(B).GetMethod(""MyMethod"").GetCustomAttribute(typeof(MyCallerMemberNameAttribute));
        }
    }
}";

            var expected = @"
member: MyMethod
";

            var compilation = CreateCompilationWithMscorlib461(source, references: new MetadataReference[] { SystemRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestRecursiveAttribute()
        {
            string source = @"
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class Goo: Attribute
{
    public Goo([Goo] int y = 1) {}
}

class Test
{
    public static void Main() { }
}
";
            var compilation = CreateCompilationWithMscorlib461(source, references: new MetadataReference[] { SystemRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: "");

            var ctor = compilation.GetMember<MethodSymbol>("Goo..ctor");
            Assert.Equal(MethodKind.Constructor, ctor.MethodKind);
            var attr = ctor.Parameters.Single().GetAttributes().Single();
            Assert.Equal(ctor, attr.AttributeConstructor);
            Assert.Equal(1, attr.CommonConstructorArguments.Length);
            // We want to ensure that we don't accidentally use default(T) instead of the real default value for the parameter.
            Assert.Equal(1, attr.CommonConstructorArguments[0].Value);
        }

        [Fact]
        public void TestRecursiveAttribute2()
        {
            string source = @"
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class Goo: Attribute
{
    public Goo([Goo, Optional, DefaultParameterValue(1)] int y) {}
}

class Test
{
    public static void Main() { }
}
";
            var compilation = CreateCompilationWithMscorlib461(source, references: new MetadataReference[] { SystemRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: "");

            var ctor = compilation.GetMember<MethodSymbol>("Goo..ctor");
            Assert.Equal(MethodKind.Constructor, ctor.MethodKind);
            var attr = ctor.Parameters.Single().GetAttributes()[0];
            Assert.Equal(ctor, attr.AttributeConstructor);
            Assert.Equal(1, attr.CommonConstructorArguments.Length);
            // We want to ensure that we don't accidentally use default(T) instead of the real default value for the parameter.
            Assert.Equal(1, attr.CommonConstructorArguments[0].Value);
        }

        [Fact]
        public void TestRecursiveAttributeMetadata()
        {
            var iLSource = @"
.class public auto ansi beforefieldinit Goo
       extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor([opt] int32 y) cil managed
  {
    .param [1] = int32(0x00000000)
    .custom instance void Goo::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  ret
  } // end of method Goo::.ctor

} // end of class Goo
";

            var source = @"
using System.Runtime.CompilerServices;
using System;

class Driver {

    [Goo]
    public static void AttrTarget() { }

    public static void Main() { }
}
";

            var expected = @"";

            MetadataReference libReference = CompileIL(iLSource);
            var compilation = CreateCompilationWithMscorlib461(source, new[] { libReference }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestMemberNameLookup()
        {
            var source = @"
using System.Reflection;
using System.Runtime.CompilerServices;
using System;

class My : Attribute
{
    public My([CallerMemberName] string a = """")
    {
        Console.WriteLine(a);
    }
}

class Driver
{
    public static void Bar([My] int x)
    {
    }

    public static void Main()
    {
        typeof(Driver).GetMethod(""Bar"").GetParameters()[0].GetCustomAttribute(typeof(My));
    }
}
";

            var expected = @"Bar";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestDuplicateCallerInfoMetadata()
        {
            var iLSource = @"
.class public auto ansi beforefieldinit Goo
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Log([opt] int32 callerName) cil managed
  {
    .param [1] = int32(0x00000000)
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CallerLineNumberAttribute::.ctor() = ( 01 00 00 00 ) 
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CallerMemberNameAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       29 (0x1d)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      ""name: ""
    IL_0006:  ldarg.0
    IL_0007:  box        [mscorlib]System.Int32
    IL_000c:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_0011:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0016:  nop
    IL_0017:  ldc.i4.1
    IL_0018:  stloc.0
    IL_0019:  br.s       IL_001b

    IL_001b:  ldloc.0
    IL_001c:  ret
  } // end of method Goo::Log

  .method public hidebysig static int32  Log2([opt] string callerName) cil managed
  {
    .param [1] = """"
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CallerLineNumberAttribute::.ctor() = ( 01 00 00 00 ) 
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CallerMemberNameAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       24 (0x18)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      ""name: ""
    IL_0006:  ldarg.0
    IL_0007:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_000c:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0011:  nop
    IL_0012:  ldc.i4.1
    IL_0013:  stloc.0
    IL_0014:  br.s       IL_0016

    IL_0016:  ldloc.0
    IL_0017:  ret
  } // end of method Goo::Log2

  .method public hidebysig static int32  Log3([opt] string callerName) cil managed
  {
    .param [1] = """"
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CallerMemberNameAttribute::.ctor() = ( 01 00 00 00 ) 
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CallerFilePathAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       24 (0x18)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      ""name: ""
    IL_0006:  ldarg.0
    IL_0007:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_000c:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0011:  nop
    IL_0012:  ldc.i4.1
    IL_0013:  stloc.0
    IL_0014:  br.s       IL_0016

    IL_0016:  ldloc.0
    IL_0017:  ret
  } // end of method Goo::Log3

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Goo::.ctor

} // end of class Goo
";

            var source = @"
using System.Runtime.CompilerServices;
using System;

class Driver {
    public static void Main() {
        Goo.Log();
        Goo.Log2();
        Goo.Log3();
    }
}
";

            var expected = @"
name: 7
name: 
name: C:\file.cs
";

            MetadataReference libReference = CompileIL(iLSource);

            var compilation = CreateCompilationWithMscorlib461(
                new[] { Parse(source, @"C:\file.cs") },
                new[] { libReference },
                TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact, WorkItem(546977, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546977")]
        public void Bug_17433()
        {
            var source = @"using System.Reflection;
using System.Runtime.CompilerServices;
using System;

class My : Attribute
{
    public My([CallerLineNumber] int x = -1)
    {
        Console.WriteLine(x);
    }
}

[My]
class Driver
{
    public static void Main()
    {
        typeof(Driver).GetCustomAttribute(typeof(My));
    }
}
";

            var expected = @"13";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact, WorkItem(531036, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531036")]
        public void Repro_17443()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

public class CallerInfoAttributedAttribute : Attribute
{
    public string FilePath { get; private set; }
    public int LineNumber { get; private set; }
    public string MemberName { get; private set; }
    public CallerInfoAttributedAttribute(
        [CallerFilePath] string filePath = """",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = """")
    {
        FilePath = filePath;
        LineNumber = lineNumber;
        MemberName = memberName;
    }
}
class Program
{
    [CallerInfoAttributed]
    public int Property1 { get; set; }
    static void Main(string[] args)
    {
        System.Reflection.PropertyInfo pi = typeof(Program).GetProperty(""Property1"");
        if (pi != null)
        {
            var a = Attribute.GetCustomAttribute(pi, typeof(CallerInfoAttributedAttribute)) as
CallerInfoAttributedAttribute;
            if (a != null)
            {
                Console.WriteLine(""CallerInfoAttributed: ({0}, {1}, {2})"", a.FilePath ?? ""<null>"",
a.LineNumber, a.MemberName ?? ""<null>"");
            }
        }
    }
}
";

            var expected = @"
CallerInfoAttributed: (, 22, Property1)
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact, WorkItem(531036, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531036")]
        public void CallerMemberNameAttributedAttributeOnNonMethodMembers()
        {
            var source = @"
using System.Reflection;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;

class NameAttr : Attribute
{
    public NameAttr([CallerMemberName] string name = ""<none>"")
    {
        Console.WriteLine(name);
    }
}

[NameAttr]
class Driver
{
    [NameAttr]
    public int myField;

    [NameAttr]
    public int MyProperty { get; set; }

    [NameAttr]
    public event Action MyEvent
    {
        add { }
        remove { }
    }

    [NameAttr]
    public int this[int i]
    {
        get { return -1; }
        set { }
    }

    [NameAttr]
    public int MyMethod() {
        return -1;
    }

    public static void Main()
    {
        typeof(Driver).GetCustomAttribute(typeof(NameAttr));
        typeof(Driver).GetField(""myField"").GetCustomAttribute(typeof(NameAttr));
        typeof(Driver).GetProperty(""MyProperty"").GetCustomAttribute(typeof(NameAttr));
        typeof(Driver).GetEvent(""MyEvent"").GetCustomAttribute(typeof(NameAttr));
        typeof(Driver).GetProperty(""Item"").GetCustomAttribute(typeof(NameAttr));
        typeof(Driver).GetMethod(""MyMethod"").GetCustomAttribute(typeof(NameAttr));
    }
}
";

            var expected = @"
<none>
<none>
MyProperty
MyEvent
Item
MyMethod
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact, WorkItem(531040, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531040")]
        public void Repro_17449()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

public class LineNumber2ObjectAttribute : Attribute
{
    public LineNumber2ObjectAttribute([CallerLineNumber] object lineNumber = null)
    {
        Console.WriteLine(lineNumber);
    }
}

[LineNumber2Object]
class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttributes(typeof(LineNumber2ObjectAttribute), false);
    }
}
";

            var expected = @"
13
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact, WorkItem(531040, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531040")]
        public void TestBadAttributeParameterTypeWithCallerLineNumber()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

public class LineNumber2NullableIntAttribute : Attribute
{
    public LineNumber2NullableIntAttribute([CallerLineNumber] int? lineNumber = null)
    {
        Console.WriteLine(lineNumber);
    }
}

public class LineNumber2ValueTypeAttribute : Attribute
{
    public LineNumber2ValueTypeAttribute([CallerLineNumber] ValueType lineNumber = null)
    {
        Console.WriteLine(lineNumber);
    }
}

[LineNumber2NullableInt, LineNumber2ValueType]
class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttributes(typeof(LineNumber2NullableIntAttribute), false);
        typeof(Program).GetCustomAttributes(typeof(LineNumber2ValueTypeAttribute), false);
    }
}
";

            CreateCompilationWithMscorlib461(source).VerifyDiagnostics(
                // (21,2): error CS0181: Attribute constructor parameter 'lineNumber' has type 'int?', which is not a valid attribute parameter type
                // [LineNumber2NullableInt, LineNumber2ValueType]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "LineNumber2NullableInt").WithArguments("lineNumber", "int?"),
                // (21,26): error CS0181: Attribute constructor parameter 'lineNumber' has type 'System.ValueType', which is not a valid attribute parameter type
                // [LineNumber2NullableInt, LineNumber2ValueType]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "LineNumber2ValueType").WithArguments("lineNumber", "System.ValueType"));
        }

        [Fact, WorkItem(531043, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531043")]
        public void Repro_17457()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;
public class LineNumber2LongAttribute : Attribute
{
    public LineNumber2LongAttribute([CallerLineNumber] long lineNumber = 0)
    {
        Console.WriteLine(lineNumber);
    }
}
public class LineNumber2FloatAttribute : Attribute
{
    public LineNumber2FloatAttribute([CallerLineNumber] float lineNumber = 0)
    {
        Console.WriteLine(lineNumber);
    }
}
[LineNumber2Long]
[LineNumber2Float]
class Test
{
    public static void Main()
    {
        typeof(Test).GetCustomAttributes(false);
    }
}
";

            var expected = @"
18
19
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact, WorkItem(531043, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531043")]
        public void InvalidDecimalInCustomAttributeParameterWithCallerLineNumber()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

public class LineNumber2DecimalAttribute : Attribute
{
    public LineNumber2DecimalAttribute([CallerLineNumber] decimal lineNumber = 42)
    {
        Console.WriteLine(lineNumber);
    }
}

[LineNumber2DecimalAttribute]
class Test
{
    public static void Main()
    {
        typeof(Test).GetCustomAttributes(false);
    }
}
";

            CreateCompilationWithMscorlib461(source).VerifyDiagnostics(
                // (13,2): error CS0181: Attribute constructor parameter 'lineNumber' has type 'decimal', which is not a valid attribute parameter type
                // [LineNumber2DecimalAttribute]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "LineNumber2DecimalAttribute").WithArguments("lineNumber", "decimal"));
        }

        [Fact, WorkItem(531043, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531043")]
        public void AllLegalConversionForCallerLineNumber()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

public class LineNumber2ObjectAttribute : Attribute
{
    public LineNumber2ObjectAttribute([CallerLineNumber] object lineNumber = null)
    {
        Console.WriteLine(lineNumber);
    }
}

public class LineNumber2UintAttribute : Attribute
{
    public LineNumber2UintAttribute([CallerLineNumber] uint lineNumber = 42)
    {
        Console.WriteLine(lineNumber);
    }
}

public class LineNumber2UlongAttribute : Attribute
{
    public LineNumber2UlongAttribute([CallerLineNumber] ulong lineNumber = 42)
    {
        Console.WriteLine(lineNumber);
    }
}

public class LineNumber2IntAttribute : Attribute
{
    public LineNumber2IntAttribute([CallerLineNumber] int lineNumber = 42)
    {
        Console.WriteLine(lineNumber);
    }
}

public class LineNumber2LongAttribute : Attribute
{
    public LineNumber2LongAttribute([CallerLineNumber] long lineNumber = 42)
    {
        Console.WriteLine(lineNumber);
    }
}

public class LineNumber2DoubleAttribute : Attribute
{
    public LineNumber2DoubleAttribute([CallerLineNumber] double lineNumber = 42)
    {
        Console.WriteLine(lineNumber);
    }
}

public class LineNumber2FloatAttribute : Attribute
{
    public LineNumber2FloatAttribute([CallerLineNumber] float lineNumber = 42)
    {
        Console.WriteLine(lineNumber);
    }
}

[LineNumber2UintAttribute, LineNumber2UlongAttribute, LineNumber2IntAttribute, LineNumber2LongAttribute, LineNumber2DoubleAttribute, LineNumber2FloatAttribute, LineNumber2ObjectAttribute]
class Test
{
    public static void Main()
    {
 
        typeof(Test).GetCustomAttributes(false);
    }
}";

            var expected = @"
61
61
61
61
61
61
61
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact, WorkItem(531046, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531046")]
        public void TestUserDefinedImplicitConversion()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

class Test
{
    public string Prop { get; set; }

    public static implicit operator Test(int i)
    {
        return new Test() { Prop = i.ToString() };
    }

    public static implicit operator Test(string i)
    {
        return new Test() { Prop = i };
    }

    public bool M1(string expected, [CallerLineNumber] Test line = null)
    {
        Console.WriteLine(""expected: {0}; actual: {1}"", expected, line);
        return expected == line.Prop;
    }

    public bool M2(string expected, [CallerMemberName] Test line = null)
    {
        Console.WriteLine(""expected: {0}; actual: {1}"", expected, line);
        return expected == line.Prop;
    }
}
";

            CreateCompilationWithMscorlib461(source).VerifyDiagnostics(
                // (19,38): error CS4017: CallerLineNumberAttribute cannot be applied because there are no standard conversions from type 'int' to type 'Test'
                //     public bool M1(string expected, [CallerLineNumber] Test line = null)
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithArguments("int", "Test"),
                // (25,38): error CS4019: CallerMemberNameAttribute cannot be applied because there are no standard conversions from type 'string' to type 'Test'
                //     public bool M2(string expected, [CallerMemberName] Test line = null)
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithArguments("string", "Test"));
        }

        [Fact, WorkItem(546980, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546980")]
        public void TestBaseCtorInvocation()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

public class B
{
    public B(
        [CallerMemberName] string memberName = ""<default>"",
        [CallerLineNumber] int lineNumber = -1,
        [CallerFilePath] string filePath = ""<default>"")
    {
        Console.WriteLine(""name : "" + memberName);
        Console.WriteLine(""line : "" + lineNumber);
        Console.WriteLine(""path : "" + filePath);
    }

    public B(bool b) : this
        (
        ) { }
}

public class D : B
{
    public D()  : base
        (
        ) { }
}

public class E : B
{
    public E()
    {
    }
}

public class I
{
    public int GetInt(
        [CallerMemberName] string memberName = ""<default>"",
        [CallerLineNumber] int lineNumber = -1,
        [CallerFilePath] string filePath = ""<default>"")
    {
        Console.WriteLine(""query name : "" + memberName);
        Console.WriteLine(""query line : "" + lineNumber);
        Console.WriteLine(""query path : "" + filePath);
        return lineNumber;
    }
}

class Program
{
    static void Main(string[] args)
    {
        new B(false);
        new D();
        new 
        B
        ();
        new E();

        var query =
            from x in new I[] { new I(), new I() }
            where x.GetInt
            (
            ) >= 0
            select x;

        foreach (var x in query) { }
    }
}
";

            var expected = @"
name : .ctor
line : 20
path : C:\filename
name : .ctor
line : 27
path : C:\filename
name : Main
line : 58
path : C:\filename
name : <default>
line : -1
path : <default>
query name : Main
query line : 66
query path : C:\filename
query name : Main
query line : 66
query path : C:\filename
";

            var compilation = CreateCompilationWithMscorlib461(
                new[] { SyntaxFactory.ParseSyntaxTree(source, path: @"C:\filename", encoding: Encoding.UTF8) },
                new[] { SystemCoreRef },
                TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact, WorkItem(531034, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531034")]
        public void WarnOnCallerInfoCollision()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

class Test
{
    static void M1([CallerMemberName,CallerFilePath] string s = null) { Console.WriteLine(s); }
    static void M2([CallerFilePath,CallerMemberName] string s = null) { Console.WriteLine(s); }
    static void M3([CallerLineNumber,CallerFilePath,CallerMemberName] object o = null) { Console.WriteLine(o); }
    static void M4([CallerLineNumber,CallerMemberName,CallerFilePath] object o = null) { Console.WriteLine(o); }
    static void M5([CallerFilePath,CallerLineNumber,CallerMemberName] object o = null) { Console.WriteLine(o); }
    static void M6([CallerMemberName,CallerLineNumber,CallerFilePath] object o = null) { Console.WriteLine(o); }
    static void M7([CallerFilePath,CallerMemberName,CallerLineNumber] object o = null) { Console.WriteLine(o); }
    static void M8([CallerMemberName,CallerFilePath,CallerLineNumber] object o = null) { Console.WriteLine(o); }

    static void Main(string[] args)
    {
        M1();
        M2();
        M3();
        M4();
        M5();
        M6();
        M7();
        M8();
    }
}
";

            var expected = @"
C:\filename
C:\filename
20
21
22
23
24
25
";

            var compilation = CreateCompilationWithMscorlib461(
                new[] { SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Regular7, path: @"C:\filename", encoding: Encoding.UTF8) },
                options: TestOptions.ReleaseExe);

            compilation.VerifyDiagnostics(
                // C:\filename(7,21): warning CS7072: The CallerMemberNameAttribute applied to parameter 's' will have no effect. It is overridden by the CallerFilePathAttribute.
                //     static void M1([CallerMemberName,CallerFilePath] string s = null) { Console.WriteLine(s); }
                Diagnostic(ErrorCode.WRN_CallerFilePathPreferredOverCallerMemberName, "CallerMemberName").WithArguments("s"),
                // C:\filename(8,36): warning CS7072: The CallerMemberNameAttribute applied to parameter 's' will have no effect. It is overridden by the CallerFilePathAttribute.
                //     static void M2([CallerFilePath,CallerMemberName] string s = null) { Console.WriteLine(s); }
                Diagnostic(ErrorCode.WRN_CallerFilePathPreferredOverCallerMemberName, "CallerMemberName").WithArguments("s"),
                // C:\filename(9,38): warning CS7074: The CallerFilePathAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M3([CallerLineNumber,CallerFilePath,CallerMemberName] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, "CallerFilePath").WithArguments("o"),
                // C:\filename(9,53): warning CS7073: The CallerMemberNameAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M3([CallerLineNumber,CallerFilePath,CallerMemberName] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, "CallerMemberName").WithArguments("o"),
                // C:\filename(10,38): warning CS7073: The CallerMemberNameAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M4([CallerLineNumber,CallerMemberName,CallerFilePath] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, "CallerMemberName").WithArguments("o"),
                // C:\filename(10,55): warning CS7074: The CallerFilePathAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M4([CallerLineNumber,CallerMemberName,CallerFilePath] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, "CallerFilePath").WithArguments("o"),
                // C:\filename(11,21): warning CS7074: The CallerFilePathAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M5([CallerFilePath,CallerLineNumber,CallerMemberName] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, "CallerFilePath").WithArguments("o"),
                // C:\filename(11,53): warning CS7073: The CallerMemberNameAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M5([CallerFilePath,CallerLineNumber,CallerMemberName] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, "CallerMemberName").WithArguments("o"),
                // C:\filename(12,21): warning CS7073: The CallerMemberNameAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M6([CallerMemberName,CallerLineNumber,CallerFilePath] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, "CallerMemberName").WithArguments("o"),
                // C:\filename(12,55): warning CS7074: The CallerFilePathAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M6([CallerMemberName,CallerLineNumber,CallerFilePath] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, "CallerFilePath").WithArguments("o"),
                // C:\filename(13,21): warning CS7074: The CallerFilePathAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M7([CallerFilePath,CallerMemberName,CallerLineNumber] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, "CallerFilePath").WithArguments("o"),
                // C:\filename(13,36): warning CS7073: The CallerMemberNameAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M7([CallerFilePath,CallerMemberName,CallerLineNumber] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, "CallerMemberName").WithArguments("o"),
                // C:\filename(14,21): warning CS7073: The CallerMemberNameAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M8([CallerMemberName,CallerFilePath,CallerLineNumber] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, "CallerMemberName").WithArguments("o"),
                // C:\filename(14,38): warning CS7074: The CallerFilePathAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M8([CallerMemberName,CallerFilePath,CallerLineNumber] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, "CallerFilePath").WithArguments("o"));

            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact, WorkItem(531034, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531034")]
        public void WarnOnCallerInfoCollisionWithBadType()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

class Test
{
    static void M1([CallerLineNumber,CallerFilePath,CallerMemberName] int i = 0) { Console.WriteLine(); }
    static void M2([CallerLineNumber,CallerFilePath,CallerMemberName] string s = null) { Console.WriteLine(s); }

    static void Main(string[] args)
    {
        M1();
        M2();
    }
}
";

            var compilation = CreateCompilationWithMscorlib461(new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Regular7, path: @"C:\filename") }).VerifyDiagnostics(
                // C:\filename(7,38): error CS4018: CallerFilePathAttribute cannot be applied because there are no standard conversions from type 'string' to type 'int'
                //     static void M1([CallerLineNumber,CallerFilePath,CallerMemberName] int i = 0) { Console.WriteLine(); }
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithArguments("string", "int").WithLocation(7, 38),
                // C:\filename(7,53): error CS4019: CallerMemberNameAttribute cannot be applied because there are no standard conversions from type 'string' to type 'int'
                //     static void M1([CallerLineNumber,CallerFilePath,CallerMemberName] int i = 0) { Console.WriteLine(); }
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithArguments("string", "int").WithLocation(7, 53),
                // C:\filename(8,21): error CS4017: CallerLineNumberAttribute cannot be applied because there are no standard conversions from type 'int' to type 'string'
                //     static void M2([CallerLineNumber,CallerFilePath,CallerMemberName] string s = null) { Console.WriteLine(s); }
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithArguments("int", "string").WithLocation(8, 21),
                // C:\filename(8,38): warning CS7082: The CallerFilePathAttribute applied to parameter 's' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M2([CallerLineNumber,CallerFilePath,CallerMemberName] string s = null) { Console.WriteLine(s); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, "CallerFilePath").WithArguments("s").WithLocation(8, 38),
                // C:\filename(8,53): warning CS7081: The CallerMemberNameAttribute applied to parameter 's' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M2([CallerLineNumber,CallerFilePath,CallerMemberName] string s = null) { Console.WriteLine(s); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, "CallerMemberName").WithArguments("s").WithLocation(8, 53),
                // C:\filename(13,9): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         M2();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "M2()").WithArguments("int", "string").WithLocation(13, 9));
        }

        [WorkItem(604367, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604367")]
        [Fact]
        public void TestCallerInfoInQuery()
        {
            string source =
@"using System;
using System.Runtime.CompilerServices;

class Test
{
    static int result = 0;
    static void Main()
    {
        var q = from x in new Test()
                where x != null
                select x.ToString();
        Console.WriteLine(result == 0 ? ""PASS"" : ""FAIL"");
    }
    public Test Select(
        Func<object, object> selector,
        [CallerFilePath]string file = null,
        [CallerLineNumber]int line = -1,
        [CallerMemberName]string member = null
    )
    {
        if (file != FILEPATH)
        {
            result++;
            Console.WriteLine(""File: Exp={0} Act={1} "", FILEPATH, file);
        }
        if (line != 11)
        {
            result++;
            Console.WriteLine(""Line: Exp=11 Act={0} "", line);
        }
        if (member != ""Main"")
        {
            result++;
            Console.WriteLine(""Member: Exp='Main' Act={0}"", member);
        }
        return new Test();
    }
    public Test Where(
        Func<object, bool> predicate,
        [CallerFilePath]string file = null,
        [CallerLineNumber]int line = -1,
        [CallerMemberName]string member = null
    )
    {
        if (file != FILEPATH)
        {
            result++;
            Console.WriteLine(""File: Exp={0} Act={1} "", FILEPATH, file);
        }
        if (line != 10)
        {
            result++;
            Console.WriteLine(""Line: Exp=10 Act={0} "", line);
        }
        if (member != ""Main"")
        {
            result++;
            Console.WriteLine(""Member: Exp='Main' Act={0}"", member);
        }
        return new Test();
    }
    public static readonly string FILEPATH = GetFilePath();
    public static string GetFilePath([CallerFilePath]string filePath = null) { return filePath; }
}";

            string expected = @"PASS";
            var compilation = CreateCompilationWithMscorlib461(source, new MetadataReference[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [WorkItem(949118, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/949118")]
        [WorkItem(152, "CodePlex")]
        [Fact]
        public void Bug949118_1()
        {
            string source =
@"using System;
using System.Runtime.CompilerServices;
using System.Globalization;
class Program
{
  static void Main()
  {
   var x = Goo.F1;
   var y = new Goo().F2;
  }
}
public class Goo
{
  static object Test([CallerMemberName] string bar = null)
  {
    Console.WriteLine(bar);
    return null;
  }
  
  public static readonly object F1 = Test();
  public readonly object F2 = Test();
}
";

            string expected = @"F1
F2";
            var compilation = CreateCompilationWithMscorlib461(source, new MetadataReference[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [WorkItem(949118, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/949118")]
        [WorkItem(152, "CodePlex")]
        [Fact]
        public void Bug949118_2()
        {
            string source =
@"using System;
using System.Runtime.CompilerServices;
using System.Globalization;
class Program
{
  static void Main()
  {
   var x = Goo.F1;
   var y = new Goo().F2;
  }
}
public class Goo
{
  static object Test([CallerMemberName] string bar = null)
  {
    Console.WriteLine(bar);
    return null;
  }
  
  public static object F1 {get;} = Test();
  public object F2 {get;} = Test();
}
";

            string expected = @"F1
F2";
            var compilation = CreateCompilationWithMscorlib461(source, new MetadataReference[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [WorkItem(949118, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/949118")]
        [WorkItem(152, "CodePlex")]
        [Fact]
        public void Bug949118_3()
        {
            string source =
@"using System;
using System.Runtime.CompilerServices;
using System.Globalization;
class Program
{
  static void Main()
  {
   var y = ((I1)new Goo()).F2;
  }
}

interface I1
{
  object F2 {get;}
}

public class Goo : I1
{
  static object Test([CallerMemberName] string bar = null)
  {
    Console.WriteLine(bar);
    return null;
  }
  
  object I1.F2 {get;} = Test();
}
";

            string expected = @"F2";
            var compilation = CreateCompilationWithMscorlib461(source, new MetadataReference[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        /// <summary>
        /// DELIBERATE SPEC VIOLATION: The C# spec currently requires to provide caller information only in explicit invocations and query expressions.
        /// We also provide caller information to an invocation of an <c>Add</c> method generated for an element-initializer in a collection-initializer
        /// to match the native compiler behavior and user requests. 
        /// </summary>
        [WorkItem(991476, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991476")]
        [WorkItem(171, "CodePlex")]
        [Fact]
        public void Bug991476_1()
        {
            const string source =
@"using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class MyCollectionWithInitializer : IEnumerable<DBNull>
{
    public string LastCallerFilePath { get; set; }

    public void Add<T>(T something, [CallerFilePath] string callerFilePath = """") where T : struct
    {
        LastCallerFilePath = callerFilePath;
        Console.WriteLine(""Caller file path: "" + (!string.IsNullOrEmpty(callerFilePath) ? callerFilePath : ""(nothing)""));
    }

    public IEnumerator<DBNull> GetEnumerator()
    {
        throw new NotSupportedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotSupportedException();
    }
}

class Program
{
    public static void Main()
    {
        var coll1 = new MyCollectionWithInitializer();
        coll1.Add(123);
        Console.WriteLine(coll1.LastCallerFilePath);

        var coll2 = new MyCollectionWithInitializer { 345 };
        Console.WriteLine(coll2.LastCallerFilePath);
    }
}";

            const string expected = @"Caller file path: C:\filename
C:\filename
Caller file path: C:\filename
C:\filename";

            var compilation = CreateCompilationWithMscorlib461(
                new[] { SyntaxFactory.ParseSyntaxTree(source, path: @"C:\filename", encoding: Encoding.UTF8) },
                new[] { SystemCoreRef },
                TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [WorkItem(991476, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991476")]
        [WorkItem(171, "CodePlex")]
        [Fact]
        public void Bug991476_2()
        {
            const string source =
@"using System;
using System.Collections;
using System.Runtime.CompilerServices;

class C : Stack
{
    static void Main()
    {
        new C
        {
            1, // line 11
            2  // line 12
        };

        new C
        {
            {  // line 17
                1,
                true
            },
            {  // line 21
                ""Hi""
            }
        };


    }

    public void Add(int x, [CallerLineNumber] int n = -1) { Console.WriteLine(n); }
    public void Add(int x, bool y, [CallerLineNumber] int n = -1) { Console.WriteLine(n); }
}

static class E
{
    public static void Add(this C c, string s, [CallerMemberName] string m = ""Default"", [CallerLineNumber] int n = -1)
    {
        Console.WriteLine(m);
        Console.WriteLine(n);
    }
}";

            const string expected = @"11
12
17
Main
21";

            var compilation = CreateCompilationWithMscorlib461(
                new[] { SyntaxFactory.ParseSyntaxTree(source, path: @"C:\filename", encoding: Encoding.UTF8) },
                new[] { SystemCoreRef },
                TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [WorkItem(1006447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1006447")]
        [Fact]
        public void Bug1006447_1()
        {
            const string vbSource =
@"Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Text
 
<ComImport>
<Guid(""1F9C3731-6AA1-498A-AFA0-359828FCF0CE"")>
Public Interface I
    Property X(Optional i as Integer = 0, <CallerFilePath> Optional s As String = Nothing) As StringBuilder
End Interface

Public Class A
    Implements I

    Public Property X(Optional i as Integer = 0, Optional s As String = Nothing) As StringBuilder Implements I.X
        Get
            Console.WriteLine(""Get X(""""{0}"""")"", s)
            Return New StringBuilder
        End Get
        Set(value As StringBuilder)
            Console.WriteLine(""Set X(""""{0}"""")"", s)
        End Set
    End Property
End Class";

            var vbReference = BasicCompilationUtils.CompileToMetadata(vbSource, references: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef });

            const string csSource =
@"using System;

class C
{
    I P = new A();
 
    static void Main()
    {
        new C().P.X = null;
        new C().P.X[1] = null;
        new C { P = { X = null } };
        new C { P = { X = { Length = 0 } } };
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(
                new[] { SyntaxFactory.ParseSyntaxTree(csSource, path: @"C:\filename", encoding: Encoding.UTF8) },
                new[] { SystemCoreRef, vbReference },
                TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput:
@"Set X(""C:\filename"")
Set X(""C:\filename"")
Set X(""C:\filename"")
Get X(""C:\filename"")
");
        }

        [WorkItem(1006447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1006447")]
        [Fact]
        public void Bug1006447_2()
        {
            const string source =
@"using System;
using System.Runtime.CompilerServices;

class C
{
    static void Main()
    {
        new C()[0] = 0;
    }

    int this[int x, [CallerMemberName] string s = null]
    {
        set
        {
            Console.WriteLine(s);
        }
    }
}";

            const string expected = "Main";

            var compilation = CreateCompilationWithMscorlib461(
                source,
                new[] { SystemCoreRef },
                TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [WorkItem(1006447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1006447")]
        [Fact]
        public void Bug1006447_3()
        {
            const string vbSource =
@"Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

<ComImport>
<Guid(""1F9C3731-6AA1-498A-AFA0-359828FCF0CE"")>
Public Interface I
    ReadOnly Property [Select](<CallerMemberName> Optional s As String = Nothing) As Func(Of Func(Of Integer, Integer), String)
End Interface

Public Class A
    Implements I

    Public ReadOnly Property [Select](<CallerMemberName> Optional s As String = Nothing) As Func(Of Func(Of Integer, Integer), String) Implements I.Select
         Get
            Console.WriteLine(""Get Select(""""{0}"""")"", s)
            Return Function() ""ABC""
        End Get
    End Property
End Class";

            var vbReference = BasicCompilationUtils.CompileToMetadata(vbSource, references: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef });

            const string csSource =
@"using System;

class Program
{
    static void Main()
    {
        I x = new A();
        Console.WriteLine(from y in x select y);
    }
}";
            var compilation = CreateCompilationWithMscorlib461(
                csSource,
                new[] { SystemCoreRef, vbReference },
                TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput:
@"Get Select(""Main"")
ABC
");
        }

        [Theory]
        [InlineData("out")]
        [InlineData("ref")]
        [InlineData("in")]
        public void CallerArgumentExpression_OnRefParameter01(string refType)
        {
            var comp = CreateCompilation(@$"
using System.Runtime.CompilerServices;
#pragma warning disable CS8321

void M(int i, [CallerArgumentExpression(""i"")] {refType} string s)
{{
    {(refType == "out" ? "s = null;" : "")}
}}
", targetFramework: TargetFramework.NetCoreApp);

            comp.VerifyDiagnostics(
                // (5,16): error CS8964: The CallerArgumentExpressionAttribute may only be applied to parameters with default values
                // void M(int i, [CallerArgumentExpression("i")] ref string s)
                Diagnostic(ErrorCode.ERR_BadCallerArgumentExpressionParamWithoutDefaultValue, "CallerArgumentExpression").WithLocation(5, 16)
            );
        }

        [Theory]
        [InlineData("out")]
        [InlineData("ref")]
        public void CallerArgumentExpression_OnRefParameter02(string refType)
        {
            var comp = CreateCompilation(@$"
using System.Runtime.CompilerServices;
#pragma warning disable CS8321

void M(int i, [CallerArgumentExpression(""i"")] {refType} string s = null)
{{
    {(refType == "out" ? "s = null;" : "")}
}}
", targetFramework: TargetFramework.NetCoreApp);

            comp.VerifyDiagnostics(
                // (5,16): error CS8964: The CallerArgumentExpressionAttribute may only be applied to parameters with default values
                // void M(int i, [CallerArgumentExpression("i")] out string s = null)
                Diagnostic(ErrorCode.ERR_BadCallerArgumentExpressionParamWithoutDefaultValue, "CallerArgumentExpression").WithLocation(5, 16),
                // (5,47): error CS1741: A ref or out parameter cannot have a default value
                // void M(int i, [CallerArgumentExpression("i")] out string s = null)
                Diagnostic(ErrorCode.ERR_RefOutDefaultValue, refType).WithLocation(5, 47)
            );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void CallerArgumentExpression_OnRefParameter03()
        {
            var comp = CreateCompilation(@"
using System;
using System.Runtime.CompilerServices;

M(1 + 1);

void M(int i, [CallerArgumentExpression(""i"")] in string s = ""default value"")
{
    Console.WriteLine(s);
}
", targetFramework: TargetFramework.NetCoreApp);

            CompileAndVerify(comp, expectedOutput: "1 + 1").VerifyDiagnostics();
        }

        [Fact]
        public void CallerArgumentExpression_Cycle()
        {
            string source =
@"namespace System.Runtime.CompilerServices
{
    public sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute([CallerArgumentExpression(nameof(parameterName))] string parameterName)
        {
            ParameterName = parameterName;
        }
        public string ParameterName { get; }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,51): error CS8964: The CallerArgumentExpressionAttribute may only be applied to parameters with default values
                //         public CallerArgumentExpressionAttribute([CallerArgumentExpression(nameof(parameterName))] string parameterName)
                Diagnostic(ErrorCode.ERR_BadCallerArgumentExpressionParamWithoutDefaultValue, "CallerArgumentExpression").WithLocation(5, 51));
        }

        [Fact]
        public void CallerMemberName_SetterValueParam()
        {
            // There is no way in C# to call a setter without passing an argument for the value, so the CallerMemberName effectively does nothing.
            var source = """
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;

                public partial class C
                {
                    public static void Main()
                    {
                        var c = new C();
                        c[1] = "1";
                    }

                    public string this[int x]
                    {
                        [param: Optional, DefaultParameterValue("0")]
                        [param: CallerMemberName]
                        set
                        {
                            Console.Write(value);
                        }
                    }
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();

            var source1 = """
                class D
                {
                    void M()
                    {
                        var c = new C();
                        c.set_Item(1);
                    }
                }
                """;
            var comp1 = CreateCompilation(source1, references: [verifier.Compilation.EmitToImageReference()]);
            comp1.VerifyEmitDiagnostics(
                // (6,11): error CS0571: 'C.this[int].set': cannot explicitly call operator or accessor
                //         c.set_Item(1);
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "set_Item").WithArguments("C.this[int].set").WithLocation(6, 11));
        }

        [Fact]
        public void CallerArgumentExpression_SetterValueParam()
        {
            var source = """
                using System;
                using System.Runtime.CompilerServices;

                namespace System.Runtime.CompilerServices
                {
                    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
                    public sealed class CallerArgumentExpressionAttribute : Attribute
                    {
                        public CallerArgumentExpressionAttribute(string parameterName)
                        {
                            ParameterName = parameterName;
                        }
                        public string ParameterName { get; }
                    }
                }

                partial class C
                {
                    public static void Main()
                    {
                        var c = new C();
                        c[1] = GetNumber();
                    }

                    public static int GetNumber() => 1;

                    public int this[int x, [CallerArgumentExpression("value")] string argumentExpression = "0"]
                    {
                        set
                        {
                            Console.Write(argumentExpression);
                        }
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "0", symbolValidator: verify);
            verifier.VerifyDiagnostics(
                // (27,29): warning CS8963: The CallerArgumentExpressionAttribute applied to parameter 'argumentExpression' will have no effect. It is applied with an invalid parameter name.
                //     public int this[int x, [CallerArgumentExpression("value")] string argumentExpression = "0"]
                Diagnostic(ErrorCode.WRN_CallerArgumentExpressionAttributeHasInvalidParameterName, "CallerArgumentExpression").WithArguments("argumentExpression").WithLocation(27, 29));

            void verify(ModuleSymbol module)
            {
                var indexer = (PropertySymbol)module.GlobalNamespace.GetMember<NamedTypeSymbol>("C").Indexers.Single();
                AssertEx.Equal(["""System.Runtime.CompilerServices.CallerArgumentExpressionAttribute("value")"""], indexer.Parameters[1].GetAttributes().SelectAsArray(attr => attr.ToString()));
            }
        }
    }
}
