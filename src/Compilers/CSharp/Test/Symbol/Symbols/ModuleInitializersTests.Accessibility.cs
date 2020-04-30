// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public sealed partial class ModuleInitializersTests
    {
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
                // (6,6): error CS8794: Module initializer method 'M' must be accessible outside top-level type 'C'
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M", "C").WithLocation(6, 6)
                );
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        public void AllowedMethodAccessibility(string keywords)
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
            compilation.VerifyEmitDiagnostics();
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        public void AllowedTopLevelTypeAccessibility(string keywords)
        {
            string source = @"
using System.Runtime.CompilerServices;

" + keywords + @" class C
{
    [ModuleInitializer]
    public static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics();
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
                // (8,10): error CS8794: Module initializer method 'M' must be accessible outside top-level type 'C'
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M", "C").WithLocation(8, 10)
                );
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        public void AllowedNestedTypeAccessibility(string keywords)
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
            compilation.VerifyEmitDiagnostics();
        }

        [Fact]
        public void MayBeDeclaredByStruct()
        {
            string source = @"
using System.Runtime.CompilerServices;

struct S
{
    [ModuleInitializer]
    internal static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics();
        }

        [Fact]
        public void MayBeDeclaredByInterface()
        {
            string source = @"
using System.Runtime.CompilerServices;

interface I
{
    [ModuleInitializer]
    internal static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);

            compilation.VerifyEmitDiagnostics(
                // (7,26): error CS8701: Target runtime doesn't support default interface implementation.
                //     internal static void M() { }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "M").WithLocation(7, 26)
                );
        }

        [Fact]
        public void ImplicitPublicInterfaceMethodAccessibility()
        {
            string source = @"
using System.Runtime.CompilerServices;

interface I
{
    [ModuleInitializer]
    static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);

            // Intentionally demonstrated without DIM support.

            compilation.VerifyEmitDiagnostics(
                // (7,17): error CS8701: Target runtime doesn't support default interface implementation.
                //     static void M() { }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "M").WithLocation(7, 17)
                );
        }

        [Fact]
        public void ImplicitPublicInterfaceNestedTypeAccessibility()
        {
            string source = @"
using System.Runtime.CompilerServices;

interface I
{
    class Nested
    {
        [ModuleInitializer]
        internal static void M() { }
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics();
        }
    }
}
