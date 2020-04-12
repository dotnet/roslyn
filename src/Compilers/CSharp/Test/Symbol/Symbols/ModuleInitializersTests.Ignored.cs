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
        [ModuleInitializer]
        static void LocalFunction() { }
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
        }

        [Fact]
        public void IgnoredOnReturnValue()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [return: ModuleInitializer]
    internal static void M()
    {
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
        }

        [Fact]
        public void IgnoredOnMethodParameter()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    internal static void M([ModuleInitializer] int p)
    {
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
        }

        [Fact]
        public void IgnoredOnGenericParameter()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    internal static void M<[ModuleInitializer] T>()
    {
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
        }

        [Fact]
        public void IgnoredOnClass()
        {
            string source = @"
using System.Runtime.CompilerServices;

[ModuleInitializer]
class C
{
    internal static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
        }

        [Fact]
        public void IgnoredOnStaticConstructor()
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
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
        }

        [Fact]
        public void IgnoredOnInstanceConstructor()
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
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
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
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
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
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
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
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
        }

        [Fact]
        public void IgnoredOnEvent()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    public event System.Action E;
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
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
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
        }

        [Fact]
        public void IgnoredOnProperty()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    public int P { get; set; }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
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
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
        }

        [Fact]
        public void IgnoredOnIndexer()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    public int this[int p] => p;
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
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
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
        }

        [Fact]
        public void IgnoredOnField()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    public int F;
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
        }

        [Fact]
        public void IgnoredOnModule()
        {
            string source = @"
using System.Runtime.CompilerServices;

[module: ModuleInitializer]

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
        }

        [Fact]
        public void IgnoredOnAssembly()
        {
            string source = @"
using System.Runtime.CompilerServices;

[assembly: ModuleInitializer]

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
        }

        [Fact]
        public void IgnoredWhenConstructorArgumentIsSpecified()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer(42)]
    internal static void M()
    {
    }
}

namespace System.Runtime.CompilerServices
{
    class ModuleInitializerAttribute : System.Attribute
    {
        public ModuleInitializerAttribute(int p) { }
    }
}
";
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
        }

        [Fact]
        public void IgnoredWhenNamedArgumentIsSpecified()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer(P = 42)]
    internal static void M()
    {
    }
}

namespace System.Runtime.CompilerServices
{
    class ModuleInitializerAttribute : System.Attribute
    {
        public int P { get; set; }
    }
}
";
            var verifier = CompileAndVerify(source, parseOptions: s_parseOptions);

            verifier.VerifyMemberInIL("<Module>..cctor", expected: false);
        }
    }
}
