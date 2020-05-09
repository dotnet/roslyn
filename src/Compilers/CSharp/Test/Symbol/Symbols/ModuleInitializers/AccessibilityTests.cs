// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.ModuleInitializers
{
    [CompilerTrait(CompilerFeature.ModuleInitializers)]
    public sealed class AccessibilityTests : CSharpTestBase
    {
        private static readonly CSharpParseOptions s_parseOptions = TestOptions.RegularPreview;

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
                targetFramework: TargetFramework.NetStandardLatest,
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
