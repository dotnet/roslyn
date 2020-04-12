// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public sealed partial class ModuleInitializersTests
    {
        [Fact]
        public void VisibilityMustNotBePrivate()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    private static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics(
                // (6,6): error CS8793: Module initializer method 'M' must be accessible outside top-level type 'C'
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M", "C").WithLocation(6, 6)
                );
        }

        [Fact]
        public void VisibilityMustNotBeProtected()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    protected static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics(
                // (6,6): error CS8793: Module initializer method 'M' must be accessible outside top-level type 'C'
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M", "C").WithLocation(6, 6)
                );
        }

        [Fact]
        public void VisibilityMustNotBePrivateProtected()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    private protected static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics(
                // (6,6): error CS8793: Module initializer method 'M' must be accessible outside top-level type 'C'
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M", "C").WithLocation(6, 6)
                );
        }

        [Fact]
        public void VisibilityMayBePublic()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    public static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void VisibilityMayBeInternal()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    internal static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void VisibilityMayBeProtectedInternal()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    protected internal static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void TopLevelTypeMayBeInternal()
        {
            string source = @"
using System.Runtime.CompilerServices;

internal class C
{
    [ModuleInitializer]
    public static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void TopLevelTypeMayBePublic()
        {
            string source = @"
using System.Runtime.CompilerServices;

public class C
{
    [ModuleInitializer]
    public static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void NestedTypeVisibilityMustNotBePrivate()
        {
            string source = @"
using System.Runtime.CompilerServices;

public class C
{
    private class Nested
    {
        [ModuleInitializer]
        public static void M() { }
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics(
                // (8,10): error CS8793: Module initializer method 'M' must be accessible outside top-level type 'C'
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M", "C").WithLocation(8, 10)
                );
        }

        [Fact]
        public void NestedTypeVisibilityMustNotBeProtected()
        {
            string source = @"
using System.Runtime.CompilerServices;

public class C
{
    protected class Nested
    {
        [ModuleInitializer]
        public static void M() { }
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics(
                // (8,10): error CS8793: Module initializer method 'M' must be accessible outside top-level type 'C'
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M", "C").WithLocation(8, 10)
                );
        }

        [Fact]
        public void NestedTypeVisibilityMustNotBePrivateProtected()
        {
            string source = @"
using System.Runtime.CompilerServices;

public class C
{
    private protected class Nested
    {
        [ModuleInitializer]
        public static void M() { }
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics(
                // (8,10): error CS8793: Module initializer method 'M' must be accessible outside top-level type 'C'
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, "ModuleInitializer").WithArguments("M", "C").WithLocation(8, 10)
                );
        }

        [Fact]
        public void NestedTypeVisibilityMayBePublic()
        {
            string source = @"
using System.Runtime.CompilerServices;

public class C
{
    public class Nested
    {
        [ModuleInitializer]
        public static void M() { }
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void NestedTypeVisibilityMayBeInternal()
        {
            string source = @"
using System.Runtime.CompilerServices;

public class C
{
    internal class Nested
    {
        [ModuleInitializer]
        public static void M() { }
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void NestedTypeVisibilityMayBeProtectedInternal()
        {
            string source = @"
using System.Runtime.CompilerServices;

public class C
{
    protected internal class Nested
    {
        [ModuleInitializer]
        public static void M() { }
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics();
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
            compilation.VerifyDiagnostics();
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

            compilation.VerifyDiagnostics(
                // (7,26): error CS8701: Target runtime doesn't support default interface implementation.
                //     internal static void M() { }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "M").WithLocation(7, 26)
                );
        }

        [Fact]
        public void ImplicitPublicInterfaceMethodVisibility()
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

            compilation.VerifyDiagnostics(
                // (7,17): error CS8701: Target runtime doesn't support default interface implementation.
                //     static void M() { }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "M").WithLocation(7, 17)
                );
        }

        [Fact]
        public void ImplicitPublicInterfaceNestedTypeVisibility()
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
            compilation.VerifyDiagnostics();
        }
    }
}
