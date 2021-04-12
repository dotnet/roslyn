// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.ModuleInitializers
{
    [CompilerTrait(CompilerFeature.ModuleInitializers)]
    public sealed class TargetsTests : CSharpTestBase
    {
        private static readonly CSharpParseOptions s_parseOptions = TestOptions.Regular9;

        [Fact]
        public void TargetMustNotBeLocalFunction()
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
            compilation.VerifyEmitDiagnostics(
                // (9,10): error CS8795: A module initializer must be an ordinary member method
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(9, 10)
                );
        }

        [Fact]
        public void IgnoredOnLocalFunctionWithBadAttributeTargets()
        {
            string source = @"
using System;
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

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class)]
    class ModuleInitializerAttribute : System.Attribute { }
}
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (10,10): error CS0592: Attribute 'ModuleInitializer' is not valid on this declaration type. It is only valid on 'class' declarations.
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ModuleInitializer").WithArguments("ModuleInitializer", "class").WithLocation(10, 10)
                );
        }

        [Fact]
        public void TargetMustNotBeDestructor()
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
            compilation.VerifyEmitDiagnostics(
                // (6,6): error CS8795: A module initializer must be an ordinary member method
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(6, 6)
                );
        }

        [Fact]
        public void IgnoredOnDestructorWithBadAttributeTargets()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    ~C() { }
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class)]
    class ModuleInitializerAttribute : System.Attribute { }
}
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (7,6): error CS0592: Attribute 'ModuleInitializer' is not valid on this declaration type. It is only valid on 'class' declarations.
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ModuleInitializer").WithArguments("ModuleInitializer", "class").WithLocation(7, 6)
                );
        }

        [Fact]
        public void TargetMustNotBeOperator()
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
            compilation.VerifyEmitDiagnostics(
                // (6,6): error CS8795: A module initializer must be an ordinary member method
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(6, 6)
                );
        }

        [Fact]
        public void IgnoredOnOperatorWithBadAttributeTargets()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    public static C operator -(C p) => p;
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class)]
    class ModuleInitializerAttribute : System.Attribute { }
}
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (7,6): error CS0592: Attribute 'ModuleInitializer' is not valid on this declaration type. It is only valid on 'class' declarations.
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ModuleInitializer").WithArguments("ModuleInitializer", "class").WithLocation(7, 6)
                );
        }

        [Fact]
        public void TargetMustNotBeConversionOperator()
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
            compilation.VerifyEmitDiagnostics(
                // (6,6): error CS8795: A module initializer must be an ordinary member method
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(6, 6)
                );
        }

        [Fact]
        public void IgnoredOnConversionOperatorWithBadAttributeTargets()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    public static explicit operator int(C p) => default;
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class)]
    class ModuleInitializerAttribute : System.Attribute { }
}
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (7,6): error CS0592: Attribute 'ModuleInitializer' is not valid on this declaration type. It is only valid on 'class' declarations.
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ModuleInitializer").WithArguments("ModuleInitializer", "class").WithLocation(7, 6)
                );
        }

        [Fact]
        public void TargetMustNotBeEventAccessor()
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
            compilation.VerifyEmitDiagnostics(
                // (8,10): error CS8795: A module initializer must be an ordinary member method
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(8, 10),
                // (10,10): error CS8795: A module initializer must be an ordinary member method
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(10, 10)
                );
        }

        [Fact]
        public void IgnoredOnEventAccessorsWithBadAttributeTargets()
        {
            string source = @"
using System;
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

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class)]
    class ModuleInitializerAttribute : System.Attribute { }
}
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (9,10): error CS0592: Attribute 'ModuleInitializer' is not valid on this declaration type. It is only valid on 'class' declarations.
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ModuleInitializer").WithArguments("ModuleInitializer", "class").WithLocation(9, 10),
                // (11,10): error CS0592: Attribute 'ModuleInitializer' is not valid on this declaration type. It is only valid on 'class' declarations.
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ModuleInitializer").WithArguments("ModuleInitializer", "class").WithLocation(11, 10)
                );
        }

        [Fact]
        public void TargetMustNotBePropertyAccessor()
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
            compilation.VerifyEmitDiagnostics(
                // (8,10): error CS8795: A module initializer must be an ordinary member method
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(8, 10),
                // (10,10): error CS8795: A module initializer must be an ordinary member method
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(10, 10)
                );
        }

        [Fact]
        public void IgnoredOnPropertyAccessorsWithBadAttributeTargets()
        {
            string source = @"
using System;
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

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class)]
    class ModuleInitializerAttribute : System.Attribute { }
}
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (9,10): error CS0592: Attribute 'ModuleInitializer' is not valid on this declaration type. It is only valid on 'class' declarations.
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ModuleInitializer").WithArguments("ModuleInitializer", "class").WithLocation(9, 10),
                // (11,10): error CS0592: Attribute 'ModuleInitializer' is not valid on this declaration type. It is only valid on 'class' declarations.
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ModuleInitializer").WithArguments("ModuleInitializer", "class").WithLocation(11, 10)
                );
        }

        [Fact]
        public void TargetMustNotBeIndexerAccessor()
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
            compilation.VerifyEmitDiagnostics(
                // (8,10): error CS8795: A module initializer must be an ordinary member method
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(8, 10),
                // (10,10): error CS8795: A module initializer must be an ordinary member method
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(10, 10)
                );
        }

        [Fact]
        public void IgnoredOnIndexerAccessorsWithBadAttributeTargets()
        {
            string source = @"
using System;
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

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class)]
    class ModuleInitializerAttribute : System.Attribute { }
}
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (9,10): error CS0592: Attribute 'ModuleInitializer' is not valid on this declaration type. It is only valid on 'class' declarations.
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ModuleInitializer").WithArguments("ModuleInitializer", "class").WithLocation(9, 10),
                // (11,10): error CS0592: Attribute 'ModuleInitializer' is not valid on this declaration type. It is only valid on 'class' declarations.
                //         [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ModuleInitializer").WithArguments("ModuleInitializer", "class").WithLocation(11, 10)
                );
        }

        [Fact]
        public void TargetMustNotBeStaticConstructor()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    static C() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (6,6): error CS8795: A module initializer must be an ordinary member method
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(6, 6)
                );
        }

        [Fact]
        public void IgnoredOnStaticConstructorWithBadAttributeTargets()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    static C() { }
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method)]
    class ModuleInitializerAttribute : System.Attribute { }
}
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (7,6): error CS0592: Attribute 'ModuleInitializer' is not valid on this declaration type. It is only valid on 'method' declarations.
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ModuleInitializer").WithArguments("ModuleInitializer", "method").WithLocation(7, 6)
                );
        }

        [Fact]
        public void TargetMustNotBeInstanceConstructor()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    public C() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (6,6): error CS8795: A module initializer must be an ordinary member method
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "ModuleInitializer").WithLocation(6, 6)
                );
        }

        [Fact]
        public void IgnoredOnInstanceConstructorWithBadAttributeTargets()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    public C() { }
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method)]
    class ModuleInitializerAttribute : System.Attribute { }
}
";
            var compilation = CreateCompilation(source, parseOptions: s_parseOptions);
            compilation.VerifyEmitDiagnostics(
                // (7,6): error CS0592: Attribute 'ModuleInitializer' is not valid on this declaration type. It is only valid on 'method' declarations.
                //     [ModuleInitializer]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ModuleInitializer").WithArguments("ModuleInitializer", "method").WithLocation(7, 6)
                );
        }
    }
}
