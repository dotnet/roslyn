// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public sealed partial class ModuleInitializersTests
    {
        [Fact]
        public void IgnoredOnLocalFunction()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    internal static void M()
    {
        LocalFunction();
        [ModuleInitializer]
        static void LocalFunction() { }
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics(
                // (9,10): error CS8793: A module initializer must be an ordinary method
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(9, 10)
                );
        }

        [Fact]
        public void IgnoredOnDestructor()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    ~C() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics(
                // (6,6): error CS8793: A module initializer must be an ordinary method
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(6, 6)
                );
        }

        [Fact]
        public void IgnoredOnOperator()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    public static C operator -(C p) => p;
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics(
                // (6,6): error CS8793: A module initializer must be an ordinary method
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(6, 6)
                );
        }

        [Fact]
        public void IgnoredOnConversionOperator()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    public static explicit operator int(C p) => default;
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics(
                // (6,6): error CS8793: A module initializer must be an ordinary method
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(6, 6)
                );
        }

        [Fact]
        public void IgnoredOnEventAccessors()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    public event System.Action E
    {
        [ModuleInitializer]
        add { }
        [ModuleInitializer]
        remove { }
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics(
                // (8,10): error CS8793: A module initializer must be an ordinary method
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(8, 10),
                // (10,10): error CS8793: A module initializer must be an ordinary method
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(10, 10)
                );
        }

        [Fact]
        public void IgnoredOnPropertyAccessors()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    public int P 
    {
        [ModuleInitializer]
        get;
        [ModuleInitializer]
        set;
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics(
                // (8,10): error CS8793: A module initializer must be an ordinary method
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(8, 10),
                // (10,10): error CS8793: A module initializer must be an ordinary method
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(10, 10)
                );
        }

        [Fact]
        public void IgnoredOnIndexerAccessors()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    public int this[int p]
    {
        [ModuleInitializer]
        get => p;
        [ModuleInitializer]
        set { }
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyDiagnostics(
                // (8,10): error CS8793: A module initializer must be an ordinary method
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(8, 10),
                // (10,10): error CS8793: A module initializer must be an ordinary method
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(10, 10)
                );
        }
    }
}
