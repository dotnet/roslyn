// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.ModuleInitializers
{
    [CompilerTrait(CompilerFeature.ModuleInitializers)]
    public sealed class AccessibilityTests : CSharpTestBase
    {
        private static readonly CSharpParseOptions s_parseOptions = TestOptions.Regular9;

        [Theory]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("private protected")]
        public void DisallowedMethodAccessibility(string keywords)
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    " + keywords + @" static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (6,6): error CS8796: Module initializer method 'M' must be accessible at the module level
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M").WithLocation(6, 6)
                );
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        public void AllowedMethodAccessibility(string keywords)
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    " + keywords + @" static void M() => Console.WriteLine(""C.M"");
}

class Program 
{
    static void Main() => Console.WriteLine(""Program.Main"");
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(source, parseOptions: s_parseOptions, expectedOutput: @"
C.M
Program.Main");
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        public void AllowedTopLevelTypeAccessibility(string keywords)
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

" + keywords + @" class C
{
    [ModuleInitializer]
    public static void M() => Console.WriteLine(""C.M"");
}

class Program 
{
    static void Main() => Console.WriteLine(""Program.Main"");
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(source, parseOptions: s_parseOptions, expectedOutput: @"
C.M
Program.Main");
        }

        [Theory]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("private protected")]
        public void DisallowedNestedTypeAccessibility(string keywords)
        {
            string source = @"
using System.Runtime.CompilerServices;

public class C
{
    " + keywords + @" class Nested
    {
        [ModuleInitializer]
        public static void M() { }
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (8,10): error CS8796: Module initializer method 'M' must be accessible at the module level
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M").WithLocation(8, 10)
                );
        }

        [Fact]
        public void ModuleInitializerOnPrivatePartialMethod()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

partial class C
{
    [ModuleInitializer] // 1
    static partial void M1();

    [ModuleInitializer] // 2
    static partial void M2();
    static partial void M2() { }

    static partial void M3();
    [ModuleInitializer] // 3
    static partial void M3() { }

    [ModuleInitializer] // 4
    static partial void M4();
    [ModuleInitializer] // 5
    static partial void M4() { }
}

class Program
{
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (7,6): error CS8814: Module initializer method 'M1' must be accessible at the module level
                //     [ModuleInitializer] // 1
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M1").WithLocation(7, 6),
                // (10,6): error CS8814: Module initializer method 'M2' must be accessible at the module level
                //     [ModuleInitializer] // 2
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M2").WithLocation(10, 6),
                // (15,6): error CS8814: Module initializer method 'M3' must be accessible at the module level
                //     [ModuleInitializer] // 3
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M3").WithLocation(15, 6),
                // (18,6): error CS8814: Module initializer method 'M4' must be accessible at the module level
                //     [ModuleInitializer] // 4
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M4").WithLocation(18, 6),
                // (20,6): error CS0579: Duplicate 'ModuleInitializer' attribute
                //     [ModuleInitializer] // 5
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "ModuleInitializer").WithArguments("ModuleInitializer").WithLocation(20, 6)
                );
        }

        [Fact]
        public void ModuleInitializerOnPrivatePartialMethod_AllowMultiple()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

partial class C
{
    [ModuleInitializer] // 1
    static partial void M1();

    [ModuleInitializer] // 2
    static partial void M2();
    static partial void M2() { }

    static partial void M3();
    [ModuleInitializer] // 3
    static partial void M3() { }

    [ModuleInitializer] // 4
    static partial void M4();
    [ModuleInitializer] // 5
    static partial void M4() { }
}

class Program
{
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    class ModuleInitializerAttribute : System.Attribute { }
}
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (7,6): error CS8814: Module initializer method 'M1' must be accessible at the module level
                //     [ModuleInitializer] // 1
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M1").WithLocation(7, 6),
                // (10,6): error CS8814: Module initializer method 'M2' must be accessible at the module level
                //     [ModuleInitializer] // 2
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M2").WithLocation(10, 6),
                // (15,6): error CS8814: Module initializer method 'M3' must be accessible at the module level
                //     [ModuleInitializer] // 3
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M3").WithLocation(15, 6),
                // (18,6): error CS8814: Module initializer method 'M4' must be accessible at the module level
                //     [ModuleInitializer] // 4
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M4").WithLocation(18, 6),
                // (20,6): error CS8814: Module initializer method 'M4' must be accessible at the module level
                //     [ModuleInitializer] // 5
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M4").WithLocation(20, 6)
                );
        }

        [Fact]
        public void ModuleInitializerOnPublicPartialMethod()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

partial class C
{
    [ModuleInitializer]
    public static partial void M1();
    public static partial void M1() { Console.Write(1); }

    public static partial void M2();
    [ModuleInitializer]
    public static partial void M2() { Console.Write(2); }
}

class Program
{
    public static void Main()
    {
        Console.Write(3);
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(source, parseOptions: s_parseOptions, expectedOutput: @"123");
        }

        [Fact]
        public void DuplicateModuleInitializerOnPublicPartialMethod()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

partial class C
{
    [ModuleInitializer]
    public static partial void M1();
    [ModuleInitializer] // 1
    public static partial void M1() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                    // (9,6): error CS0579: Duplicate 'ModuleInitializer' attribute
                    //     [ModuleInitializer] // 1
                    Diagnostic(ErrorCode.ERR_DuplicateAttribute, "ModuleInitializer").WithArguments("ModuleInitializer").WithLocation(9, 6)
                );
        }

        [Fact]
        public void DuplicateModuleInitializerOnPublicPartialMethod_AllowMultiple()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

partial class C
{
    [ModuleInitializer]
    public static partial void M1();
    [ModuleInitializer]
    public static partial void M1() { Console.Write(1); }
}

class Program
{
    static void Main()
    {
        Console.Write(2);
    }
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    class ModuleInitializerAttribute : System.Attribute { }
}
";
            CompileAndVerify(source, expectedOutput: "12", parseOptions: s_parseOptions);
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        public void AllowedNestedTypeAccessibility(string keywords)
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    " + keywords + @" class Nested
    {
        [ModuleInitializer]
        public static void M() => Console.WriteLine(""C.M"");
    }
}

class Program 
{
    static void Main() => Console.WriteLine(""Program.Main"");
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(source, parseOptions: s_parseOptions, expectedOutput: @"
C.M
Program.Main");
        }

        [Fact]
        public void ImplicitPublicInterfaceMethodAccessibility()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

interface I
{
    [ModuleInitializer]
    static void M() => Console.WriteLine(""I.M"");
}

class Program 
{
    static void Main() => Console.WriteLine(""Program.Main"");
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(
                source,
                parseOptions: s_parseOptions,
                targetFramework: TargetFramework.NetCoreApp,
                expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? @"
I.M
Program.Main" : null,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped);
        }

        [Fact]
        public void ImplicitPublicInterfaceNestedTypeAccessibility()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

interface I
{
    class Nested
    {
        [ModuleInitializer]
        internal static void M() => Console.WriteLine(""C.M"");
    }
}

class Program 
{
    static void Main() => Console.WriteLine(""Program.Main"");
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(source, parseOptions: s_parseOptions, expectedOutput: @"
C.M
Program.Main");
        }
    }
}
