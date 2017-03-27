// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    [CompilerTrait(CompilerFeature.DefaultInterfaceImplementation)]
    public class DefaultInterfaceImplementationTests : CSharpTestBase
    {
        [Fact]
        public void MethodImplementation_011()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options:TestOptions.DebugDll, 
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate1(ModuleSymbol m)
            {
                ValidateMethodImplementationTest1_011(m, "void I1.M1()");
            }

            Validate1(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify:false, symbolValidator: Validate1);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateMethodImplementationTest2_011(m, "void I1.M1()");
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);

            var compilation3 = CreateCompilationWithMscorlib(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            Validate2(compilation3.SourceModule);

            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate2);
        }

        private static void ValidateMethodImplementationTest1_011(ModuleSymbol m, string expectedImplementation)
        {
            var i1 = m.GlobalNamespace.GetTypeMember("I1");
            var m1 = i1.GetMember<MethodSymbol>("M1");

            Assert.True(m1.IsMetadataVirtual());
            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsVirtual);
            Assert.True(i1.IsAbstract);
            Assert.True(i1.IsMetadataAbstract);

            if (m is PEModuleSymbol peModule)
            {
                int rva;
                peModule.Module.GetMethodDefPropsOrThrow(((PEMethodSymbol)m1).Handle, out _, out _, out _, out rva);
                Assert.NotEqual(0, rva);
            }

            var test1 = m.GlobalNamespace.GetTypeMember("Test1");
            Assert.Equal(expectedImplementation, test1.FindImplementationForInterfaceMember(m1).ToTestDisplayString());
            Assert.Equal("I1", test1.Interfaces.Single().ToTestDisplayString());
        }

        private static void ValidateMethodImplementationTest2_011(ModuleSymbol m, string expectedImplementation)
        {
            var test2 = m.GlobalNamespace.GetTypeMember("Test2");
            Assert.Equal("I1", test2.Interfaces.Single().ToTestDisplayString());
            var m1 = test2.Interfaces.Single().GetMember<MethodSymbol>("M1");

            Assert.Equal(expectedImplementation, test2.FindImplementationForInterfaceMember(m1).ToTestDisplayString());
        }

        [Fact]
        public void MethodImplementation_012()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

class Test1 : I1
{
    public void M1() 
    {
        System.Console.WriteLine(""Test1 M1"");
    }
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate1(ModuleSymbol m) 
            {
                ValidateMethodImplementationTest1_011(m, "void Test1.M1()");
            }

            Validate1(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            var source2 =
@"
class Test2 : I1
{
    public void M1() 
    {
        System.Console.WriteLine(""Test2 M1"");
    }
}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateMethodImplementationTest2_011(m, "void Test2.M1()");
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);

            var compilation3 = CreateCompilationWithMscorlib(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            Validate2(compilation3.SourceModule);
            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void MethodImplementation_013()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

class Test1 : I1
{
    void I1.M1() 
    {
        System.Console.WriteLine(""Test1 M1"");
    }
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate1(ModuleSymbol m)
            {
                ValidateMethodImplementationTest1_011(m, "void Test1.I1.M1()");
            }

            Validate1(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            var source2 =
@"
class Test2 : I1
{
    void I1.M1() 
    {
        System.Console.WriteLine(""Test2 M1"");
    }
}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateMethodImplementationTest2_011(m, "void Test2.I1.M1()");
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);

            var compilation3 = CreateCompilationWithMscorlib(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            Validate2(compilation3.SourceModule);

            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void MethodImplementation_021()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
    void M2() {}
}

class Base
{
    void M1() { }
}

class Derived : Base, I1
{
    void M2() { }
}

class Test : I1 {}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate(ModuleSymbol m)
            {
                var m1 = m.GlobalNamespace.GetMember<MethodSymbol>("I1.M1");
                var m2 = m.GlobalNamespace.GetMember<MethodSymbol>("I1.M2");

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Same(m1, derived.FindImplementationForInterfaceMember(m1));
                Assert.Same(m2, derived.FindImplementationForInterfaceMember(m2));
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void MethodImplementation_022()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
    void M2() {}
}

class Base : Test
{
    void M1() { }
}

class Derived : Base, I1
{
    void M2() { }
}

class Test : I1 {}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate(ModuleSymbol m)
            {
                var m1 = m.GlobalNamespace.GetMember<MethodSymbol>("I1.M1");
                var m2 = m.GlobalNamespace.GetMember<MethodSymbol>("I1.M2");

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Same(m1, derived.FindImplementationForInterfaceMember(m1));
                Assert.Same(m2, derived.FindImplementationForInterfaceMember(m2));
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void MethodImplementation_023()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
    void M2() {}
}

class Base : Test
{
    void M1() { }
}

class Derived : Base, I1
{
    void M2() { }
}

class Test : I1 
{
    void I1.M1() {}
    void I1.M2() {}
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate(ModuleSymbol m)
            {
                var m1 = m.GlobalNamespace.GetMember<MethodSymbol>("I1.M1");
                var m2 = m.GlobalNamespace.GetMember<MethodSymbol>("I1.M2");

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Equal("void Test.I1.M1()", derived.FindImplementationForInterfaceMember(m1).ToTestDisplayString());
                Assert.Equal("void Test.I1.M2()", derived.FindImplementationForInterfaceMember(m2).ToTestDisplayString());
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void MethodImplementation_024()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
    void M2() {}
}

class Base : Test
{
    new void M1() { }
}

class Derived : Base, I1
{
    new void M2() { }
}

class Test : I1 
{
    public void M1() {}
    public void M2() {}
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate(ModuleSymbol m)
            {
                var m1 = m.GlobalNamespace.GetMember<MethodSymbol>("I1.M1");
                var m2 = m.GlobalNamespace.GetMember<MethodSymbol>("I1.M2");

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Equal("void Test.M1()", derived.FindImplementationForInterfaceMember(m1).ToTestDisplayString());
                Assert.Equal("void Test.M2()", derived.FindImplementationForInterfaceMember(m2).ToTestDisplayString());
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void MethodImplementation_031()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
}

class Test1 : I1
{
    public static void M1() { }
}

class Test2 : I1 {}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var test1Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        [Fact]
        public void MethodImplementation_032()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
}

class Test1 : Test2, I1
{
    public static void M1() { }
}

class Test2 : I1 {}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var test1Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        [Fact]
        public void MethodImplementation_033()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
}

class Test1 : Test2, I1
{
    public static void M1() { }
}

class Test2 : I1 
{
    void I1.M1() {}
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Equal("void Test2.I1.M1()", test1.FindImplementationForInterfaceMember(m1).ToTestDisplayString());

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var test1Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        [Fact]
        public void MethodImplementation_034()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
}

class Test1 : Test2, I1
{
    new public static void M1() { }
}

class Test2 : I1 
{
    public void M1() {}
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Equal("void Test2.M1()", test1.FindImplementationForInterfaceMember(m1).ToTestDisplayString());

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var test1Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        [Fact]
        public void MethodImplementation_041()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
    int M2() => 1; 
}

class Test1 : I1
{
    public int M1() { return 0; }
    public ref int M2() { throw null; }
}

class Test2 : I1 {}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var m2 = compilation1.GetMember<MethodSymbol>("I1.M2");

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));
            Assert.Same(m2, test1.FindImplementationForInterfaceMember(m2));

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var test1Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        [Fact]
        public void MethodImplementation_042()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
    int M2() => 1; 
}

class Test1 : Test2, I1
{
    public int M1() { return 0; }
    public ref int M2() { throw null; }
}

class Test2 : I1 {}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var m2 = compilation1.GetMember<MethodSymbol>("I1.M2");

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));
            Assert.Same(m2, test1.FindImplementationForInterfaceMember(m2));

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var test1Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        [Fact]
        public void MethodImplementation_043()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
    int M2() => 1; 
}

class Test1 : Test2, I1
{
    public int M1() { return 0; }
    public ref int M2() { throw null; }
}

class Test2 : I1 
{
    void I1.M1() {}
    int I1.M2() => 1; 
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var m2 = compilation1.GetMember<MethodSymbol>("I1.M2");

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Equal("void Test2.I1.M1()", test1.FindImplementationForInterfaceMember(m1).ToTestDisplayString());
            Assert.Equal("System.Int32 Test2.I1.M2()", test1.FindImplementationForInterfaceMember(m2).ToTestDisplayString());

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var test1Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        [Fact]
        public void MethodImplementation_044()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
    int M2() => 1; 
}

class Test1 : Test2, I1
{
    new public int M1() { return 0; }
    new public ref int M2() { throw null; }
}

class Test2 : I1 
{
    public void M1() {}
    public int M2() => 1; 
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var m2 = compilation1.GetMember<MethodSymbol>("I1.M2");

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Equal("void Test2.M1()", test1.FindImplementationForInterfaceMember(m1).ToTestDisplayString());
            Assert.Equal("System.Int32 Test2.M2()", test1.FindImplementationForInterfaceMember(m2).ToTestDisplayString());

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var test1Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        private static MetadataReference MscorlibRefWithoutSharingCachedSymbols
        {
            get
            {
                return ((AssemblyMetadata)((MetadataImageReference)MscorlibRef).GetMetadata()).CopyWithoutSharingCachedSymbols().
                    GetReference(display: "mscorlib.v4_0_30319.dll");
            }
        }

        [Fact]
        public void MethodImplementation_051()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

class Test1 : I1
{}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation1 = CreateCompilation(source1, new [] { mscorLibRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");

            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));

            compilation1.VerifyDiagnostics(
                // (4,10): error CS8501: Target runtime doesn't support default interface implementation.
                //     void M1() 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "M1").WithLocation(4, 10)
                );

            Assert.True(m1.IsMetadataVirtual());

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.False(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            m1 = compilation3.GetMember<MethodSymbol>("I1.M1");
            var test2 = compilation3.GetTypeByMetadataName("Test2");

            Assert.Same(m1, test2.FindImplementationForInterfaceMember(m1));

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.M1()' cannot implement interface member 'I1.M1()' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.M1()", "I1.M1()", "Test2").WithLocation(2, 15)
                );
        }

        [Fact]
        public void MethodImplementation_052()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}
";

            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var source2 =
@"
class Test2 : I1
{}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;
            var m1 = compilation3.GetMember<MethodSymbol>("I1.M1");
            var test2 = compilation3.GetTypeByMetadataName("Test2");

            Assert.Same(m1, test2.FindImplementationForInterfaceMember(m1));

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.M1()' cannot implement interface member 'I1.M1()' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.M1()", "I1.M1()", "Test2").WithLocation(2, 15)
                );
        }

        [Fact]
        public void MethodImplementation_053()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}
";

            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var source2 =
@"
public interface I2
{
    void M2();
}

class Test2 : I2
{
    public void M2() {}
}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;
            var m1 = compilation3.GetMember<MethodSymbol>("I1.M1");
            var test2 = compilation3.GetTypeByMetadataName("Test2");

            Assert.Null(test2.FindImplementationForInterfaceMember(m1));

            compilation3.VerifyDiagnostics();
        }

        [Fact]
        public void MethodImplementation_061()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

class Test1 : I1
{}
";
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation1 = CreateCompilation(source1, new[] { mscorLibRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");

            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));

            compilation1.VerifyDiagnostics(
                // (4,10): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     void M1() 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "M1").WithArguments("default interface implementation", "7.1").WithLocation(4, 10),
                // (4,10): error CS8501: Target runtime doesn't support default interface implementation.
                //     void M1() 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "M1").WithLocation(4, 10)
                );

            Assert.True(m1.IsMetadataVirtual());

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.False(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            m1 = compilation3.GetMember<MethodSymbol>("I1.M1");
            var test2 = compilation3.GetTypeByMetadataName("Test2");

            Assert.Same(m1, test2.FindImplementationForInterfaceMember(m1));

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.M1()' cannot implement interface member 'I1.M1()' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.M1()", "I1.M1()", "Test2").WithLocation(2, 15)
                );
        }

        [Fact]
        public void MethodImplementation_071()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");

            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));

            compilation1.VerifyDiagnostics(
                // (4,10): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     void M1() 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "M1").WithArguments("default interface implementation", "7.1").WithLocation(4, 10)
                );

            Assert.True(m1.IsMetadataVirtual());

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                            parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            m1 = compilation2.GetMember<MethodSymbol>("I1.M1");
            var test2 = compilation2.GetTypeByMetadataName("Test2");

            Assert.Same(m1, test2.FindImplementationForInterfaceMember(m1));

            compilation2.VerifyDiagnostics();

            CompileAndVerify(compilation2, verify: false,
                symbolValidator: (m) =>
                {
                    var test2Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test2");
                    Assert.Equal("I1", test2Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        [Fact]
        public void MethodImplementation_081()
        {
            var source1 =
@"
public interface I1
{
    I1 M1() 
    {
        throw null;
    }
}
";
            var compilation1 = CreateCompilation(source1, new[] { SystemCoreRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.False(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");

            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsVirtual);

            compilation1.VerifyDiagnostics(
                // (4,8): error CS8501: Target runtime doesn't support default interface implementation.
                //     I1 M1() 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "M1").WithLocation(4, 8)
                );

            Assert.True(m1.IsMetadataVirtual());

            Assert.Throws<System.InvalidOperationException>(() => compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false);
        }

        [Fact]
        public void MethodImplementation_091()
        {
            var source1 =
@"
public interface I1
{
    static void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");

            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsVirtual);
            Assert.False(m1.IsStatic);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));

            compilation1.VerifyDiagnostics(
                // (4,17): error CS0106: The modifier 'static' is not valid for this item
                //     static void M1() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M1").WithArguments("static").WithLocation(4, 17),
                // (4,17): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     static void M1() 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "M1").WithArguments("default interface implementation", "7.1").WithLocation(4, 17)
                );

            Assert.True(m1.IsMetadataVirtual());
        }

        [Fact]
        public void MethodImplementation_101()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

public interface I2 : I1
{}

class Test1 : I2
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");

            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));

            compilation1.VerifyDiagnostics();
            Assert.True(m1.IsMetadataVirtual());

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var i1 = m.GlobalNamespace.GetTypeMember("I1");
                    var result = (PEMethodSymbol)i1.GetMember("M1");

                    Assert.True(result.IsMetadataVirtual());
                    Assert.False(result.IsAbstract);
                    Assert.True(result.IsVirtual);
                    Assert.True(i1.IsAbstract);
                    Assert.True(i1.IsMetadataAbstract);

                    int rva;
                    ((PEModuleSymbol)m).Module.GetMethodDefPropsOrThrow(result.Handle, out _, out _, out _, out rva);
                    Assert.NotEqual(0, rva);

                    var test1Result = m.GlobalNamespace.GetTypeMember("Test1");
                    var interfaces = test1Result.Interfaces.ToArray();
                    Assert.Equal(2, interfaces.Length);
                    Assert.Equal("I2", interfaces[0].ToTestDisplayString());
                    Assert.Equal("I1", interfaces[1].ToTestDisplayString());
                });

            var source2 =
@"
class Test2 : I2
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            m1 = compilation2.GetMember<MethodSymbol>("I1.M1");
            var test2 = compilation2.GetTypeByMetadataName("Test2");

            Assert.Same(m1, test2.FindImplementationForInterfaceMember(m1));

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false,
                symbolValidator: (m) =>
                {
                    var test2Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test2");
                    var interfaces = test2Result.Interfaces.ToArray();
                    Assert.Equal(2, interfaces.Length);
                    Assert.Equal("I2", interfaces[0].ToTestDisplayString());
                    Assert.Equal("I1", interfaces[1].ToTestDisplayString());
                });

            var compilation3 = CreateCompilationWithMscorlib(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            m1 = compilation3.GetMember<MethodSymbol>("I1.M1");
            test2 = compilation3.GetTypeByMetadataName("Test2");

            Assert.Same(m1, test2.FindImplementationForInterfaceMember(m1));

            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false,
                symbolValidator: (m) =>
                {
                    var test2Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test2");
                    var interfaces = test2Result.Interfaces.ToArray();
                    Assert.Equal(2, interfaces.Length);
                    Assert.Equal("I2", interfaces[0].ToTestDisplayString());
                    Assert.Equal("I1", interfaces[1].ToTestDisplayString());
                });
        }

        [Fact]
        public void PropertyImplementation_101()
        {
            var source1 =
@"
public interface I1
{
    int P1 
    {
        get
        {
            System.Console.WriteLine(""get P1"");
            return 0;
        }
    }
}

class Test1 : I1
{}
";
            ValidatePropertyImplementation_101(source1);
        }

        private void ValidatePropertyImplementation_101(string source1)
        { 
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate1(ModuleSymbol m)
            {
                ValidatePropertyImplementationTest1_101(m, haveGet: true, haveSet: false);
            }

            Validate1(compilation1.SourceModule);
            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidatePropertyImplementationTest2_101(m, haveGet: true, haveSet: false);
            }

            Validate2(compilation2.SourceModule);
            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);

            var compilation3 = CreateCompilationWithMscorlib(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            Validate2(compilation3.SourceModule);
            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate2);
        }

        private static void ValidatePropertyImplementationTest1_101(ModuleSymbol m, bool haveGet, bool haveSet)
        {
            var i1 = m.GlobalNamespace.GetTypeMember("I1");
            var p1 = i1.GetMember<PropertySymbol>("P1");
            var getP1 = p1.GetMethod;
            var setP1 = p1.SetMethod;

            Assert.Equal(!haveSet, p1.IsReadOnly);
            Assert.Equal(!haveGet, p1.IsWriteOnly);

            if (haveGet)
            {
                Assert.False(getP1.IsAbstract);
                Assert.True(getP1.IsVirtual);
                Assert.True(getP1.IsMetadataVirtual());
            }
            else
            {
                Assert.Null(getP1);
            }

            if (haveSet)
            {
                Assert.False(setP1.IsAbstract);
                Assert.True(setP1.IsVirtual);
                Assert.True(setP1.IsMetadataVirtual());
            }
            else
            {
                Assert.Null(setP1);
            }

            Assert.False(p1.IsAbstract);
            Assert.True(p1.IsVirtual);
            Assert.True(i1.IsAbstract);
            Assert.True(i1.IsMetadataAbstract);

            if (m is PEModuleSymbol peModule)
            {
                int rva;

                if (haveGet)
                {
                    peModule.Module.GetMethodDefPropsOrThrow(((PEMethodSymbol)getP1).Handle, out _, out _, out _, out rva);
                    Assert.NotEqual(0, rva);
                }

                if (haveSet)
                {
                    peModule.Module.GetMethodDefPropsOrThrow(((PEMethodSymbol)setP1).Handle, out _, out _, out _, out rva);
                    Assert.NotEqual(0, rva);
                }
            }

            var test1 = m.GlobalNamespace.GetTypeMember("Test1");
            Assert.Equal("I1", test1.Interfaces.Single().ToTestDisplayString());
            Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));

            if (haveGet)
            {
                Assert.Same(getP1, test1.FindImplementationForInterfaceMember(getP1));
            }

            if (haveSet)
            {
                Assert.Same(setP1, test1.FindImplementationForInterfaceMember(setP1));
            }
        }

        private static void ValidatePropertyImplementationTest2_101(ModuleSymbol m, bool haveGet, bool haveSet)
        {
            var test2 = m.GlobalNamespace.GetTypeMember("Test2");
            Assert.Equal("I1", test2.Interfaces.Single().ToTestDisplayString());

            var p1 = test2.Interfaces.Single().GetMember<PropertySymbol>("P1");
            Assert.Same(p1, test2.FindImplementationForInterfaceMember(p1));

            if (haveGet)
            {
                var getP1 = p1.GetMethod;
                Assert.Same(getP1, test2.FindImplementationForInterfaceMember(getP1));
            }

            if (haveSet)
            {
                var setP1 = p1.SetMethod;
                Assert.Same(setP1, test2.FindImplementationForInterfaceMember(setP1));
            }
        }

        [Fact]
        public void PropertyImplementation_102()
        {
            var source1 =
@"
public interface I1
{
    int P1 
    {
        get
        {
            System.Console.WriteLine(""get P1"");
            return 0;
        }
        set
        {
            System.Console.WriteLine(""set P1"");
        }
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate1(ModuleSymbol m)
            {
                ValidatePropertyImplementationTest1_101(m, haveGet: true, haveSet: true);
            }

            Validate1(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidatePropertyImplementationTest2_101(m, haveGet: true, haveSet: true);
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);

            var compilation3 = CreateCompilationWithMscorlib(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            Validate2(compilation3.SourceModule);

            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void PropertyImplementation_103()
        {
            var source1 =
@"
public interface I1
{
    int P1 
    {
        set
        {
            System.Console.WriteLine(""set P1"");
        }
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics();

            void Validate1(ModuleSymbol m)
            {
                ValidatePropertyImplementationTest1_101(m, haveGet: false, haveSet: true);
            }

            Validate1(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidatePropertyImplementationTest2_101(m, haveGet: false, haveSet: true);
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);

            var compilation3 = CreateCompilationWithMscorlib(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            Validate2(compilation3.SourceModule);

            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void PropertyImplementation_104()
        {
            var source1 =
@"
public interface I1
{
    int P1 => 0;
}

class Test1 : I1
{}
";
            ValidatePropertyImplementation_101(source1);
        }

        [Fact]
        public void PropertyImplementation_105()
        {
            var source1 =
@"
public interface I1
{
    int P1 {add; remove;} => 0;
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,16): error CS0073: An add or remove accessor must have a body
                //     int P1 {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 16),
                // (4,24): error CS0073: An add or remove accessor must have a body
                //     int P1 {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 24),
                // (4,13): error CS1014: A get or set accessor expected
                //     int P1 {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "add").WithLocation(4, 13),
                // (4,18): error CS1014: A get or set accessor expected
                //     int P1 {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "remove").WithLocation(4, 18),
                // (4,9): error CS0548: 'I1.P1': property or indexer must have at least one accessor
                //     int P1 {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "P1").WithArguments("I1.P1").WithLocation(4, 9),
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     int P1 {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, "int P1 {add; remove;} => 0;").WithLocation(4, 5)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.P1");
            Assert.True(p1.IsAbstract);
            Assert.Null(p1.GetMethod);
            Assert.Null(p1.SetMethod);
            Assert.True(p1.IsReadOnly);
            Assert.True(p1.IsWriteOnly);
        }

        [Fact]
        public void PropertyImplementation_106()
        {
            var source1 =
@"
public interface I1
{
    int P1 {get; set;} => 0;
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     int P1 {get; set;} => 0;
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, "int P1 {get; set;} => 0;").WithLocation(4, 5)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.P1");
            Assert.True(p1.IsAbstract);
            Assert.True(p1.GetMethod.IsAbstract);
            Assert.True(p1.SetMethod.IsAbstract);
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);
        }

        [Fact]
        public void PropertyImplementation_107()
        {
            var source1 =
@"
public interface I1
{
    int P1 {add; remove;} = 0;
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,16): error CS0073: An add or remove accessor must have a body
                //     int P1 {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 16),
                // (4,24): error CS0073: An add or remove accessor must have a body
                //     int P1 {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 24),
                // (4,13): error CS1014: A get or set accessor expected
                //     int P1 {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "add").WithLocation(4, 13),
                // (4,18): error CS1014: A get or set accessor expected
                //     int P1 {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "remove").WithLocation(4, 18),
                // (4,9): error CS8052: Auto-implemented properties inside interfaces cannot have initializers.
                //     int P1 {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_AutoPropertyInitializerInInterface, "P1").WithArguments("I1.P1").WithLocation(4, 9),
                // (4,9): error CS0548: 'I1.P1': property or indexer must have at least one accessor
                //     int P1 {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "P1").WithArguments("I1.P1").WithLocation(4, 9)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.P1");
            Assert.True(p1.IsAbstract);
            Assert.Null(p1.GetMethod);
            Assert.Null(p1.SetMethod);
            Assert.True(p1.IsReadOnly);
            Assert.True(p1.IsWriteOnly);
        }

        [Fact]
        public void PropertyImplementation_108()
        {
            var source1 =
@"
public interface I1
{
    int P1 {get; set;} = 0;
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,9): error CS8052: Auto-implemented properties inside interfaces cannot have initializers.
                //     int P1 {get; set;} = 0;
                Diagnostic(ErrorCode.ERR_AutoPropertyInitializerInInterface, "P1").WithArguments("I1.P1").WithLocation(4, 9)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.P1");
            Assert.True(p1.IsAbstract);
            Assert.True(p1.GetMethod.IsAbstract);
            Assert.True(p1.SetMethod.IsAbstract);
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);
        }

        [Fact]
        public void PropertyImplementation_109()
        {
            var source1 =
@"
public interface I1
{
    int P1 
    {
        get
        {
            System.Console.WriteLine(""get P1"");
            return 0;
        }
        set;
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            // PROTOTYPE(DefaultInterfaceImplementation): We might want to allow code like this.
            compilation1.VerifyDiagnostics(
                // (11,9): error CS0501: 'I1.P1.set' must declare a body because it is not marked abstract, extern, or partial
                //         set;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("I1.P1.set").WithLocation(11, 9)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.P1");
            var getP1 = p1.GetMethod;
            var setP1 = p1.SetMethod;
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);

            Assert.False(p1.IsAbstract);
            Assert.True(p1.IsVirtual);
            Assert.False(getP1.IsAbstract);
            Assert.True(getP1.IsVirtual);
            Assert.False(setP1.IsAbstract);
            Assert.True(setP1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));
            Assert.Same(getP1, test1.FindImplementationForInterfaceMember(getP1));
            Assert.Same(setP1, test1.FindImplementationForInterfaceMember(setP1));

            Assert.True(getP1.IsMetadataVirtual());
            Assert.True(setP1.IsMetadataVirtual());
        }

        [Fact]
        public void PropertyImplementation_110()
        {
            var source1 =
@"
public interface I1
{
    int P1 
    {
        get;
        set => System.Console.WriteLine(""set P1"");
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            // PROTOTYPE(DefaultInterfaceImplementation): We might want to allow code like this.
            compilation1.VerifyDiagnostics(
                // (6,9): error CS0501: 'I1.P1.get' must declare a body because it is not marked abstract, extern, or partial
                //         get;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I1.P1.get").WithLocation(6, 9)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.P1");
            var getP1 = p1.GetMethod;
            var setP1 = p1.SetMethod;
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);

            Assert.False(p1.IsAbstract);
            Assert.True(p1.IsVirtual);
            Assert.False(getP1.IsAbstract);
            Assert.True(getP1.IsVirtual);
            Assert.False(setP1.IsAbstract);
            Assert.True(setP1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));
            Assert.Same(getP1, test1.FindImplementationForInterfaceMember(getP1));
            Assert.Same(setP1, test1.FindImplementationForInterfaceMember(setP1));

            Assert.True(getP1.IsMetadataVirtual());
            Assert.True(setP1.IsMetadataVirtual());
        }

        [Fact]
        public void PropertyImplementation_201()
        {
            var source1 =
@"
interface I1
{
    int P1 => 1;
    int P2 => 2;
    int P3 { get => 3; }
    int P4 { get => 4; }
    int P5 { set => System.Console.WriteLine(5); }
    int P6 { set => System.Console.WriteLine(6); }
    int P7 { get { return 7;} set {} }
    int P8 { get { return 8;} set {} }
}

class Base
{
    int P1 => 10;
    int P3 { get => 30; }
    int P5 { set => System.Console.WriteLine(50); }
    int P7 { get { return 70;} set {} }
}

class Derived : Base, I1
{
    int P2 => 20;
    int P4 { get => 40; }
    int P6 { set => System.Console.WriteLine(60); }
    int P8 { get { return 80;} set {} }
}

class Test : I1 {}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            ValidatePropertyImplementation_201(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    ValidatePropertyImplementation_201(m);
                });
        }

        private static void ValidatePropertyImplementation_201(ModuleSymbol m)
        {
            var p1 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P1");
            var p2 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P2");
            var p3 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P3");
            var p4 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P4");
            var p5 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P5");
            var p6 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P6");
            var p7 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P7");
            var p8 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P8");

            var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

            Assert.Same(p1, derived.FindImplementationForInterfaceMember(p1));
            Assert.Same(p2, derived.FindImplementationForInterfaceMember(p2));
            Assert.Same(p3, derived.FindImplementationForInterfaceMember(p3));
            Assert.Same(p4, derived.FindImplementationForInterfaceMember(p4));
            Assert.Same(p5, derived.FindImplementationForInterfaceMember(p5));
            Assert.Same(p6, derived.FindImplementationForInterfaceMember(p6));
            Assert.Same(p7, derived.FindImplementationForInterfaceMember(p7));
            Assert.Same(p8, derived.FindImplementationForInterfaceMember(p8));

            Assert.Same(p1.GetMethod, derived.FindImplementationForInterfaceMember(p1.GetMethod));
            Assert.Same(p2.GetMethod, derived.FindImplementationForInterfaceMember(p2.GetMethod));
            Assert.Same(p3.GetMethod, derived.FindImplementationForInterfaceMember(p3.GetMethod));
            Assert.Same(p4.GetMethod, derived.FindImplementationForInterfaceMember(p4.GetMethod));
            Assert.Same(p5.SetMethod, derived.FindImplementationForInterfaceMember(p5.SetMethod));
            Assert.Same(p6.SetMethod, derived.FindImplementationForInterfaceMember(p6.SetMethod));
            Assert.Same(p7.GetMethod, derived.FindImplementationForInterfaceMember(p7.GetMethod));
            Assert.Same(p8.GetMethod, derived.FindImplementationForInterfaceMember(p8.GetMethod));
            Assert.Same(p7.SetMethod, derived.FindImplementationForInterfaceMember(p7.SetMethod));
            Assert.Same(p8.SetMethod, derived.FindImplementationForInterfaceMember(p8.SetMethod));
        }

        [Fact]
        public void PropertyImplementation_202()
        {
            var source1 =
@"
interface I1
{
    int P1 => 1;
    int P2 => 2;
    int P3 { get => 3; }
    int P4 { get => 4; }
    int P5 { set => System.Console.WriteLine(5); }
    int P6 { set => System.Console.WriteLine(6); }
    int P7 { get { return 7;} set {} }
    int P8 { get { return 8;} set {} }
}

class Base : Test
{
    int P1 => 10;
    int P3 { get => 30; }
    int P5 { set => System.Console.WriteLine(50); }
    int P7 { get { return 70;} set {} }
}

class Derived : Base, I1
{
    int P2 => 20;
    int P4 { get => 40; }
    int P6 { set => System.Console.WriteLine(60); }
    int P8 { get { return 80;} set {} }
}

class Test : I1 {}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            ValidatePropertyImplementation_201(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    ValidatePropertyImplementation_201(m);
                });
        }

        [Fact]
        public void PropertyImplementation_203()
        {
            var source1 =
@"
interface I1
{
    int P1 => 1;
    int P2 => 2;
    int P3 { get => 3; }
    int P4 { get => 4; }
    int P5 { set => System.Console.WriteLine(5); }
    int P6 { set => System.Console.WriteLine(6); }
    int P7 { get { return 7;} set {} }
    int P8 { get { return 8;} set {} }
}

class Base : Test
{
    int P1 => 10;
    int P3 { get => 30; }
    int P5 { set => System.Console.WriteLine(50); }
    int P7 { get { return 70;} set {} }
}

class Derived : Base, I1
{
    int P2 => 20;
    int P4 { get => 40; }
    int P6 { set => System.Console.WriteLine(60); }
    int P8 { get { return 80;} set {} }
}

class Test : I1 
{
    int I1.P1 => 100;
    int I1.P2 => 200;
    int I1.P3 { get => 300; }
    int I1.P4 { get => 400; }
    int I1.P5 { set => System.Console.WriteLine(500); }
    int I1.P6 { set => System.Console.WriteLine(600); }
    int I1.P7 { get { return 700;} set {} }
    int I1.P8 { get { return 800;} set {} }
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate(ModuleSymbol m)
            {
                var p1 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P1");
                var p2 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P2");
                var p3 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P3");
                var p4 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P4");
                var p5 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P5");
                var p6 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P6");
                var p7 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P7");
                var p8 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P8");

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Equal("System.Int32 Test.I1.P1 { get; }", derived.FindImplementationForInterfaceMember(p1).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P2 { get; }", derived.FindImplementationForInterfaceMember(p2).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P3 { get; }", derived.FindImplementationForInterfaceMember(p3).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P4 { get; }", derived.FindImplementationForInterfaceMember(p4).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P5 { set; }", derived.FindImplementationForInterfaceMember(p5).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P6 { set; }", derived.FindImplementationForInterfaceMember(p6).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P7 { get; set; }", derived.FindImplementationForInterfaceMember(p7).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P8 { get; set; }", derived.FindImplementationForInterfaceMember(p8).ToTestDisplayString());

                Assert.Equal("System.Int32 Test.I1.P1.get", derived.FindImplementationForInterfaceMember(p1.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P2.get", derived.FindImplementationForInterfaceMember(p2.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P3.get", derived.FindImplementationForInterfaceMember(p3.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P4.get", derived.FindImplementationForInterfaceMember(p4.GetMethod).ToTestDisplayString());
                Assert.Equal("void Test.I1.P5.set", derived.FindImplementationForInterfaceMember(p5.SetMethod).ToTestDisplayString());
                Assert.Equal("void Test.I1.P6.set", derived.FindImplementationForInterfaceMember(p6.SetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P7.get", derived.FindImplementationForInterfaceMember(p7.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P8.get", derived.FindImplementationForInterfaceMember(p8.GetMethod).ToTestDisplayString());
                Assert.Equal("void Test.I1.P7.set", derived.FindImplementationForInterfaceMember(p7.SetMethod).ToTestDisplayString());
                Assert.Equal("void Test.I1.P8.set", derived.FindImplementationForInterfaceMember(p8.SetMethod).ToTestDisplayString());
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void PropertyImplementation_204()
        {
            var source1 =
@"
interface I1
{
    int P1 => 1;
    int P2 => 2;
    int P3 { get => 3; }
    int P4 { get => 4; }
    int P5 { set => System.Console.WriteLine(5); }
    int P6 { set => System.Console.WriteLine(6); }
    int P7 { get { return 7;} set {} }
    int P8 { get { return 8;} set {} }
}

class Base : Test
{
    new int P1 => 10;
    new int P3 { get => 30; }
    new int P5 { set => System.Console.WriteLine(50); }
    new int P7 { get { return 70;} set {} }
}

class Derived : Base, I1
{
    new int P2 => 20;
    new int P4 { get => 40; }
    new int P6 { set => System.Console.WriteLine(60); }
    new int P8 { get { return 80;} set {} }
}

class Test : I1 
{
    public int P1 => 100;
    public int P2 => 200;
    public int P3 { get => 300; }
    public int P4 { get => 400; }
    public int P5 { set => System.Console.WriteLine(500); }
    public int P6 { set => System.Console.WriteLine(600); }
    public int P7 { get { return 700;} set {} }
    public int P8 { get { return 800;} set {} }
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate(ModuleSymbol m)
            {
                var p1 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P1");
                var p2 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P2");
                var p3 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P3");
                var p4 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P4");
                var p5 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P5");
                var p6 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P6");
                var p7 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P7");
                var p8 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P8");

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Equal("System.Int32 Test.P1 { get; }", derived.FindImplementationForInterfaceMember(p1).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P2 { get; }", derived.FindImplementationForInterfaceMember(p2).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P3 { get; }", derived.FindImplementationForInterfaceMember(p3).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P4 { get; }", derived.FindImplementationForInterfaceMember(p4).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P5 { set; }", derived.FindImplementationForInterfaceMember(p5).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P6 { set; }", derived.FindImplementationForInterfaceMember(p6).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P7 { get; set; }", derived.FindImplementationForInterfaceMember(p7).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P8 { get; set; }", derived.FindImplementationForInterfaceMember(p8).ToTestDisplayString());

                Assert.Equal("System.Int32 Test.P1.get", derived.FindImplementationForInterfaceMember(p1.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P2.get", derived.FindImplementationForInterfaceMember(p2.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P3.get", derived.FindImplementationForInterfaceMember(p3.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P4.get", derived.FindImplementationForInterfaceMember(p4.GetMethod).ToTestDisplayString());
                Assert.Equal("void Test.P5.set", derived.FindImplementationForInterfaceMember(p5.SetMethod).ToTestDisplayString());
                Assert.Equal("void Test.P6.set", derived.FindImplementationForInterfaceMember(p6.SetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P7.get", derived.FindImplementationForInterfaceMember(p7.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P8.get", derived.FindImplementationForInterfaceMember(p8.GetMethod).ToTestDisplayString());
                Assert.Equal("void Test.P7.set", derived.FindImplementationForInterfaceMember(p7.SetMethod).ToTestDisplayString());
                Assert.Equal("void Test.P8.set", derived.FindImplementationForInterfaceMember(p8.SetMethod).ToTestDisplayString());
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void PropertyImplementation_501()
        {
            var source1 =
@"
public interface I1
{
    int P1 => 1;
    int P3 
    { get => 3; }
    int P5 
    { set => System.Console.WriteLine(5); }
    int P7 
    { 
        get { return 7;} 
        set {} 
    }
}

class Test1 : I1
{}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation1 = CreateCompilation(source1, new[] { mscorLibRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;
            compilation1.VerifyDiagnostics(
                // (4,15): error CS8501: Target runtime doesn't support default interface implementation.
                //     int P1 => 1;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "1").WithLocation(4, 15),
                // (6,7): error CS8501: Target runtime doesn't support default interface implementation.
                //     { get => 3; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(6, 7),
                // (8,7): error CS8501: Target runtime doesn't support default interface implementation.
                //     { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "set").WithLocation(8, 7),
                // (11,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         get { return 7;} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(11, 9),
                // (12,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         set {} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "set").WithLocation(12, 9)
                );

            ValidatePropertyImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.False(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.P7.set' cannot implement interface member 'I1.P7.set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P7.set", "I1.P7.set", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P1.get' cannot implement interface member 'I1.P1.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P1.get", "I1.P1.get", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P3.get' cannot implement interface member 'I1.P3.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P3.get", "I1.P3.get", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P5.set' cannot implement interface member 'I1.P5.set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P5.set", "I1.P5.set", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P7.get' cannot implement interface member 'I1.P7.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P7.get", "I1.P7.get", "Test2").WithLocation(2, 15)
                );

            ValidatePropertyImplementation_501(compilation3.SourceModule, "Test2");
        }

        private static void ValidatePropertyImplementation_501(ModuleSymbol m, string typeName)
        {
            var derived = m.GlobalNamespace.GetTypeMember(typeName);
            var i1 = derived.Interfaces.Single();
            Assert.Equal("I1", i1.ToTestDisplayString());

            var p1 = i1.GetMember<PropertySymbol>("P1");
            var p3 = i1.GetMember<PropertySymbol>("P3");
            var p5 = i1.GetMember<PropertySymbol>("P5");
            var p7 = i1.GetMember<PropertySymbol>("P7");

            Assert.True(p1.IsVirtual);
            Assert.True(p3.IsVirtual);
            Assert.True(p5.IsVirtual);
            Assert.True(p7.IsVirtual);

            Assert.False(p1.IsAbstract);
            Assert.False(p3.IsAbstract);
            Assert.False(p5.IsAbstract);
            Assert.False(p7.IsAbstract);

            Assert.Same(p1, derived.FindImplementationForInterfaceMember(p1));
            Assert.Same(p3, derived.FindImplementationForInterfaceMember(p3));
            Assert.Same(p5, derived.FindImplementationForInterfaceMember(p5));
            Assert.Same(p7, derived.FindImplementationForInterfaceMember(p7));

            Assert.True(p1.GetMethod.IsVirtual);
            Assert.True(p3.GetMethod.IsVirtual);
            Assert.True(p5.SetMethod.IsVirtual);
            Assert.True(p7.GetMethod.IsVirtual);
            Assert.True(p7.SetMethod.IsVirtual);

            Assert.True(p1.GetMethod.IsMetadataVirtual());
            Assert.True(p3.GetMethod.IsMetadataVirtual());
            Assert.True(p5.SetMethod.IsMetadataVirtual());
            Assert.True(p7.GetMethod.IsMetadataVirtual());
            Assert.True(p7.SetMethod.IsMetadataVirtual());

            Assert.False(p1.GetMethod.IsAbstract);
            Assert.False(p3.GetMethod.IsAbstract);
            Assert.False(p5.SetMethod.IsAbstract);
            Assert.False(p7.GetMethod.IsAbstract);
            Assert.False(p7.SetMethod.IsAbstract);

            Assert.Same(p1.GetMethod, derived.FindImplementationForInterfaceMember(p1.GetMethod));
            Assert.Same(p3.GetMethod, derived.FindImplementationForInterfaceMember(p3.GetMethod));
            Assert.Same(p5.SetMethod, derived.FindImplementationForInterfaceMember(p5.SetMethod));
            Assert.Same(p7.GetMethod, derived.FindImplementationForInterfaceMember(p7.GetMethod));
            Assert.Same(p7.SetMethod, derived.FindImplementationForInterfaceMember(p7.SetMethod));
        }

        [Fact]
        public void PropertyImplementation_502()
        {
            var source1 =
@"
public interface I1
{
    int P1 => 1;
    int P3 { get => 3; }
    int P5 { set => System.Console.WriteLine(5); }
    int P7 { get { return 7;} set {} }
}
";

            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var source2 =
@"
class Test2 : I1
{}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.P7.set' cannot implement interface member 'I1.P7.set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P7.set", "I1.P7.set", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P1.get' cannot implement interface member 'I1.P1.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P1.get", "I1.P1.get", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P3.get' cannot implement interface member 'I1.P3.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P3.get", "I1.P3.get", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P5.set' cannot implement interface member 'I1.P5.set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P5.set", "I1.P5.set", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P7.get' cannot implement interface member 'I1.P7.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P7.get", "I1.P7.get", "Test2").WithLocation(2, 15)
                );

            ValidatePropertyImplementation_501(compilation3.SourceModule, "Test2");
        }

        [Fact]
        public void PropertyImplementation_503()
        {
            var source1 =
@"
public interface I1
{
    int P1 => 1;
    int P3 { get => 3; }
    int P5 { set => System.Console.WriteLine(5); }
    int P7 { get { return 7;} set {} }
}
";

            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var source2 =
@"
public interface I2
{
    void M2();
}

class Test2 : I2
{
    public void M2() {}
}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            var test2 = compilation3.GetTypeByMetadataName("Test2");
            var i1 = compilation3.GetTypeByMetadataName("I1");
            Assert.Equal("I1", i1.ToTestDisplayString());

            var p1 = i1.GetMember<PropertySymbol>("P1");
            var p3 = i1.GetMember<PropertySymbol>("P3");
            var p5 = i1.GetMember<PropertySymbol>("P5");
            var p7 = i1.GetMember<PropertySymbol>("P7");

            Assert.Null(test2.FindImplementationForInterfaceMember(p1));
            Assert.Null(test2.FindImplementationForInterfaceMember(p3));
            Assert.Null(test2.FindImplementationForInterfaceMember(p5));
            Assert.Null(test2.FindImplementationForInterfaceMember(p7));

            Assert.Null(test2.FindImplementationForInterfaceMember(p1.GetMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(p3.GetMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(p5.SetMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(p7.GetMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(p7.SetMethod));

            compilation3.VerifyDiagnostics();
        }

        [Fact]
        public void PropertyImplementation_601()
        {
            var source1 =
@"
public interface I1
{
    int P1 => 1;
    int P3 { get => 3; }
    int P5 { set => System.Console.WriteLine(5); }
    int P7 { get { return 7;} set {} }
}

class Test1 : I1
{}
";
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation1 = CreateCompilation(source1, new[] { mscorLibRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            compilation1.VerifyDiagnostics(
                // (4,15): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P1 => 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "1").WithArguments("default interface implementation", "7.1").WithLocation(4, 15),
                // (4,15): error CS8501: Target runtime doesn't support default interface implementation.
                //     int P1 => 1;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "1").WithLocation(4, 15),
                // (5,14): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P3 { get => 3; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(5, 14),
                // (5,14): error CS8501: Target runtime doesn't support default interface implementation.
                //     int P3 { get => 3; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(5, 14),
                // (6,14): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P5 { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(6, 14),
                // (6,14): error CS8501: Target runtime doesn't support default interface implementation.
                //     int P5 { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "set").WithLocation(6, 14),
                // (7,14): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(7, 14),
                // (7,14): error CS8501: Target runtime doesn't support default interface implementation.
                //     int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(7, 14),
                // (7,31): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(7, 31),
                // (7,31): error CS8501: Target runtime doesn't support default interface implementation.
                //     int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "set").WithLocation(7, 31)
                );

            ValidatePropertyImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.False(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.P7.set' cannot implement interface member 'I1.P7.set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P7.set", "I1.P7.set", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P1.get' cannot implement interface member 'I1.P1.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P1.get", "I1.P1.get", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P3.get' cannot implement interface member 'I1.P3.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P3.get", "I1.P3.get", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P5.set' cannot implement interface member 'I1.P5.set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P5.set", "I1.P5.set", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P7.get' cannot implement interface member 'I1.P7.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P7.get", "I1.P7.get", "Test2").WithLocation(2, 15)
                );

            ValidatePropertyImplementation_501(compilation3.SourceModule, "Test2");
        }

        [Fact]
        public void PropertyImplementation_701()
        {
            var source1 =
@"
public interface I1
{
    int P1 => 1;
    int P3 { get => 3; }
    int P5 { set => System.Console.WriteLine(5); }
    int P7 { get { return 7;} set {} }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (4,15): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P1 => 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "1").WithArguments("default interface implementation", "7.1").WithLocation(4, 15),
                // (5,14): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P3 { get => 3; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(5, 14),
                // (6,14): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P5 { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(6, 14),
                // (7,14): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(7, 14),
                // (7,31): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(7, 31)
                );

            ValidatePropertyImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                            parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics();

            ValidatePropertyImplementation_501(compilation2.SourceModule, "Test2");

            CompileAndVerify(compilation2, verify: false,
                symbolValidator: (m) =>
                {
                    var test2Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test2");
                    Assert.Equal("I1", test2Result.Interfaces.Single().ToTestDisplayString());
                    ValidatePropertyImplementation_501(m, "Test2");
                });
        }

        [Fact]
        public void PropertyImplementation_901()
        {
            var source1 =
@"
public interface I1
{
    static int P1 => 1;
    static int P3 { get => 3; }
    static int P5 { set => System.Console.WriteLine(5); }
    static int P7 { get { return 7;} set {} }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (4,16): error CS0106: The modifier 'static' is not valid for this item
                //     static int P1 => 1;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P1").WithArguments("static").WithLocation(4, 16),
                // (4,22): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     static int P1 => 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "1").WithArguments("default interface implementation", "7.1").WithLocation(4, 22),
                // (5,16): error CS0106: The modifier 'static' is not valid for this item
                //     static int P3 { get => 3; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P3").WithArguments("static").WithLocation(5, 16),
                // (5,21): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     static int P3 { get => 3; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(5, 21),
                // (6,16): error CS0106: The modifier 'static' is not valid for this item
                //     static int P5 { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P5").WithArguments("static").WithLocation(6, 16),
                // (6,21): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     static int P5 { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(6, 21),
                // (7,16): error CS0106: The modifier 'static' is not valid for this item
                //     static int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P7").WithArguments("static").WithLocation(7, 16),
                // (7,21): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     static int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(7, 21),
                // (7,38): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     static int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(7, 38)
                );

            ValidatePropertyImplementation_501(compilation1.SourceModule, "Test1");
        }

        [Fact]
        public void IndexerImplementation_101()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] 
    {
        get
        {
            System.Console.WriteLine(""get P1"");
            return 0;
        }
    }
}

class Test1 : I1
{}
";
            IndexerImplementation_101(source1);
        }

        private void IndexerImplementation_101(string source1)
        {
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate1(ModuleSymbol m)
            {
                ValidateIndexerImplementationTest1_101(m, haveGet: true, haveSet: false);
            }

            Validate1(compilation1.SourceModule);
            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateIndexerImplementationTest2_101(m, haveGet: true, haveSet: false);
            }

            Validate2(compilation2.SourceModule);
            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);

            var compilation3 = CreateCompilationWithMscorlib(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            Validate2(compilation3.SourceModule);
            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate2);
        }

        private static void ValidateIndexerImplementationTest1_101(ModuleSymbol m, bool haveGet, bool haveSet)
        {
            var i1 = m.GlobalNamespace.GetTypeMember("I1");
            var p1 = i1.GetMember<PropertySymbol>("this[]");
            var getP1 = p1.GetMethod;
            var setP1 = p1.SetMethod;

            Assert.Equal(!haveSet, p1.IsReadOnly);
            Assert.Equal(!haveGet, p1.IsWriteOnly);

            if (haveGet)
            {
                Assert.False(getP1.IsAbstract);
                Assert.True(getP1.IsVirtual);
                Assert.True(getP1.IsMetadataVirtual());
            }
            else
            {
                Assert.Null(getP1);
            }

            if (haveSet)
            {
                Assert.False(setP1.IsAbstract);
                Assert.True(setP1.IsVirtual);
                Assert.True(setP1.IsMetadataVirtual());
            }
            else
            {
                Assert.Null(setP1);
            }

            Assert.False(p1.IsAbstract);
            Assert.True(p1.IsVirtual);
            Assert.True(i1.IsAbstract);
            Assert.True(i1.IsMetadataAbstract);

            if (m is PEModuleSymbol peModule)
            {
                int rva;

                if (haveGet)
                {
                    peModule.Module.GetMethodDefPropsOrThrow(((PEMethodSymbol)getP1).Handle, out _, out _, out _, out rva);
                    Assert.NotEqual(0, rva);
                }

                if (haveSet)
                {
                    peModule.Module.GetMethodDefPropsOrThrow(((PEMethodSymbol)setP1).Handle, out _, out _, out _, out rva);
                    Assert.NotEqual(0, rva);
                }
            }

            var test1 = m.GlobalNamespace.GetTypeMember("Test1");
            Assert.Equal("I1", test1.Interfaces.Single().ToTestDisplayString());
            Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));

            if (haveGet)
            {
                Assert.Same(getP1, test1.FindImplementationForInterfaceMember(getP1));
            }

            if (haveSet)
            {
                Assert.Same(setP1, test1.FindImplementationForInterfaceMember(setP1));
            }
        }

        private static void ValidateIndexerImplementationTest2_101(ModuleSymbol m, bool haveGet, bool haveSet)
        {
            var test2 = m.GlobalNamespace.GetTypeMember("Test2");
            Assert.Equal("I1", test2.Interfaces.Single().ToTestDisplayString());

            var p1 = test2.Interfaces.Single().GetMember<PropertySymbol>("this[]");
            Assert.Same(p1, test2.FindImplementationForInterfaceMember(p1));

            if (haveGet)
            {
                var getP1 = p1.GetMethod;
                Assert.Same(getP1, test2.FindImplementationForInterfaceMember(getP1));
            }

            if (haveSet)
            {
                var setP1 = p1.SetMethod;
                Assert.Same(setP1, test2.FindImplementationForInterfaceMember(setP1));
            }
        }

        [Fact]
        public void IndexerImplementation_102()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] 
    {
        get
        {
            System.Console.WriteLine(""get P1"");
            return 0;
        }
        set
        {
            System.Console.WriteLine(""set P1"");
        }
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate1(ModuleSymbol m)
            {
                ValidateIndexerImplementationTest1_101(m, haveGet: true, haveSet: true);
            }

            Validate1(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateIndexerImplementationTest2_101(m, haveGet: true, haveSet: true);
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);

            var compilation3 = CreateCompilationWithMscorlib(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            Validate2(compilation3.SourceModule);

            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void IndexerImplementation_103()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] 
    {
        set
        {
            System.Console.WriteLine(""set P1"");
        }
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics();

            void Validate1(ModuleSymbol m)
            {
                ValidateIndexerImplementationTest1_101(m, haveGet: false, haveSet: true);
            }

            Validate1(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateIndexerImplementationTest2_101(m, haveGet: false, haveSet: true);
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);

            var compilation3 = CreateCompilationWithMscorlib(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            Validate2(compilation3.SourceModule);

            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void IndexerImplementation_104()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] => 0;
}

class Test1 : I1
{}
";
            IndexerImplementation_101(source1);
        }

        [Fact]
        public void IndexerImplementation_105()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] {add; remove;} => 0;
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,25): error CS0073: An add or remove accessor must have a body
                //     int this[int i] {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 25),
                // (4,33): error CS0073: An add or remove accessor must have a body
                //     int this[int i] {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 33),
                // (4,22): error CS1014: A get or set accessor expected
                //     int this[int i] {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "add").WithLocation(4, 22),
                // (4,27): error CS1014: A get or set accessor expected
                //     int this[int i] {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "remove").WithLocation(4, 27),
                // (4,9): error CS0548: 'I1.this[int]': property or indexer must have at least one accessor
                //     int this[int i] {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "this").WithArguments("I1.this[int]").WithLocation(4, 9),
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     int this[int i] {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, "int this[int i] {add; remove;} => 0;").WithLocation(4, 5)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.this[]");
            Assert.True(p1.IsAbstract);
            Assert.Null(p1.GetMethod);
            Assert.Null(p1.SetMethod);
            Assert.True(p1.IsReadOnly);
            Assert.True(p1.IsWriteOnly);
        }

        [Fact]
        public void IndexerImplementation_106()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] {get; set;} => 0;
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     int this[int i] {get; set;} => 0;
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, "int this[int i] {get; set;} => 0;").WithLocation(4, 5)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.this[]");
            Assert.True(p1.IsAbstract);
            Assert.True(p1.GetMethod.IsAbstract);
            Assert.True(p1.SetMethod.IsAbstract);
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);
        }

        [Fact]
        public void IndexerImplementation_107()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] {add; remove;} = 0;
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,25): error CS0073: An add or remove accessor must have a body
                //     int this[int i] {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 25),
                // (4,33): error CS0073: An add or remove accessor must have a body
                //     int this[int i] {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 33),
                // (4,36): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                //     int this[int i] {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(4, 36),
                // (4,22): error CS1014: A get or set accessor expected
                //     int this[int i] {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "add").WithLocation(4, 22),
                // (4,27): error CS1014: A get or set accessor expected
                //     int this[int i] {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "remove").WithLocation(4, 27),
                // (4,9): error CS0548: 'I1.this[int]': property or indexer must have at least one accessor
                //     int this[int i] {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "this").WithArguments("I1.this[int]").WithLocation(4, 9)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.this[]");
            Assert.True(p1.IsAbstract);
            Assert.Null(p1.GetMethod);
            Assert.Null(p1.SetMethod);
            Assert.True(p1.IsReadOnly);
            Assert.True(p1.IsWriteOnly);
        }

        [Fact]
        public void IndexerImplementation_108()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] {get; set;} = 0;
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,33): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                //     int this[int i] {get; set;} = 0;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(4, 33)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.this[]");
            Assert.True(p1.IsAbstract);
            Assert.True(p1.GetMethod.IsAbstract);
            Assert.True(p1.SetMethod.IsAbstract);
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);
        }

        [Fact]
        public void IndexerImplementation_109()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] 
    {
        get
        {
            System.Console.WriteLine(""get P1"");
            return 0;
        }
        set;
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            // PROTOTYPE(DefaultInterfaceImplementation): We might want to allow code like this.
            compilation1.VerifyDiagnostics(
                // (11,9): error CS0501: 'I1.this[int].set' must declare a body because it is not marked abstract, extern, or partial
                //         set;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("I1.this[int].set")
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.this[]");
            var getP1 = p1.GetMethod;
            var setP1 = p1.SetMethod;
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);

            Assert.False(p1.IsAbstract);
            Assert.True(p1.IsVirtual);
            Assert.False(getP1.IsAbstract);
            Assert.True(getP1.IsVirtual);
            Assert.False(setP1.IsAbstract);
            Assert.True(setP1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));
            Assert.Same(getP1, test1.FindImplementationForInterfaceMember(getP1));
            Assert.Same(setP1, test1.FindImplementationForInterfaceMember(setP1));

            Assert.True(getP1.IsMetadataVirtual());
            Assert.True(setP1.IsMetadataVirtual());
        }

        [Fact]
        public void IndexerImplementation_110()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] 
    {
        get;
        set => System.Console.WriteLine(""set P1"");
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            // PROTOTYPE(DefaultInterfaceImplementation): We might want to allow code like this.
            compilation1.VerifyDiagnostics(
                // (6,9): error CS0501: 'I1.this[int].get' must declare a body because it is not marked abstract, extern, or partial
                //         get;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I1.this[int].get")
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.this[]");
            var getP1 = p1.GetMethod;
            var setP1 = p1.SetMethod;
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);

            Assert.False(p1.IsAbstract);
            Assert.True(p1.IsVirtual);
            Assert.False(getP1.IsAbstract);
            Assert.True(getP1.IsVirtual);
            Assert.False(setP1.IsAbstract);
            Assert.True(setP1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));
            Assert.Same(getP1, test1.FindImplementationForInterfaceMember(getP1));
            Assert.Same(setP1, test1.FindImplementationForInterfaceMember(setP1));

            Assert.True(getP1.IsMetadataVirtual());
            Assert.True(setP1.IsMetadataVirtual());
        }

        [Fact]
        public void IndexerImplementation_201()
        {
            var source1 =
@"
interface I1
{
    int this[sbyte i] => 1;
    int this[byte i] => 2;
    int this[short i] { get => 3; }
    int this[ushort i] { get => 4; }
    int this[int i] { set => System.Console.WriteLine(5); }
    int this[uint i] { set => System.Console.WriteLine(6); }
    int this[long i] { get { return 7;} set {} }
    int this[ulong i] { get { return 8;} set {} }
}

class Base
{
    int this[sbyte i] => 10;
    int this[short i] { get => 30; }
    int this[int i] { set => System.Console.WriteLine(50); }
    int this[long i] { get { return 70;} set {} }
}

class Derived : Base, I1
{
    int this[byte i] => 20;
    int this[ushort i] { get => 40; }
    int this[uint i] { set => System.Console.WriteLine(60); }
    int this[ulong i] { get { return 80;} set {} }
}

class Test : I1 {}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            ValidateIndexerImplementation_201(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    ValidateIndexerImplementation_201(m);
                });
        }

        private static void ValidateIndexerImplementation_201(ModuleSymbol m)
        {
            var i1 = m.GlobalNamespace.GetTypeMember("I1");
            var indexers = i1.GetMembers("this[]");
            var p1 = (PropertySymbol)indexers[0];
            var p2 = (PropertySymbol)indexers[1];
            var p3 = (PropertySymbol)indexers[2];
            var p4 = (PropertySymbol)indexers[3];
            var p5 = (PropertySymbol)indexers[4];
            var p6 = (PropertySymbol)indexers[5];
            var p7 = (PropertySymbol)indexers[6];
            var p8 = (PropertySymbol)indexers[7];

            var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

            Assert.Same(p1, derived.FindImplementationForInterfaceMember(p1));
            Assert.Same(p2, derived.FindImplementationForInterfaceMember(p2));
            Assert.Same(p3, derived.FindImplementationForInterfaceMember(p3));
            Assert.Same(p4, derived.FindImplementationForInterfaceMember(p4));
            Assert.Same(p5, derived.FindImplementationForInterfaceMember(p5));
            Assert.Same(p6, derived.FindImplementationForInterfaceMember(p6));
            Assert.Same(p7, derived.FindImplementationForInterfaceMember(p7));
            Assert.Same(p8, derived.FindImplementationForInterfaceMember(p8));

            Assert.Same(p1.GetMethod, derived.FindImplementationForInterfaceMember(p1.GetMethod));
            Assert.Same(p2.GetMethod, derived.FindImplementationForInterfaceMember(p2.GetMethod));
            Assert.Same(p3.GetMethod, derived.FindImplementationForInterfaceMember(p3.GetMethod));
            Assert.Same(p4.GetMethod, derived.FindImplementationForInterfaceMember(p4.GetMethod));
            Assert.Same(p5.SetMethod, derived.FindImplementationForInterfaceMember(p5.SetMethod));
            Assert.Same(p6.SetMethod, derived.FindImplementationForInterfaceMember(p6.SetMethod));
            Assert.Same(p7.GetMethod, derived.FindImplementationForInterfaceMember(p7.GetMethod));
            Assert.Same(p8.GetMethod, derived.FindImplementationForInterfaceMember(p8.GetMethod));
            Assert.Same(p7.SetMethod, derived.FindImplementationForInterfaceMember(p7.SetMethod));
            Assert.Same(p8.SetMethod, derived.FindImplementationForInterfaceMember(p8.SetMethod));
        }

        [Fact]
        public void IndexerImplementation_202()
        {
            var source1 =
@"
interface I1
{
    int this[sbyte i] => 1;
    int this[byte i] => 2;
    int this[short i] { get => 3; }
    int this[ushort i] { get => 4; }
    int this[int i] { set => System.Console.WriteLine(5); }
    int this[uint i] { set => System.Console.WriteLine(6); }
    int this[long i] { get { return 7;} set {} }
    int this[ulong i] { get { return 8;} set {} }
}

class Base : Test
{
    int this[sbyte i] => 10;
    int this[short i] { get => 30; }
    int this[int i] { set => System.Console.WriteLine(50); }
    int this[long i] { get { return 70;} set {} }
}

class Derived : Base, I1
{
    int this[byte i] => 20;
    int this[ushort i] { get => 40; }
    int this[uint i] { set => System.Console.WriteLine(60); }
    int this[ulong i] { get { return 80;} set {} }
}

class Test : I1 {}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            ValidateIndexerImplementation_201(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    ValidateIndexerImplementation_201(m);
                });
        }

        [Fact]
        public void IndexerImplementation_203()
        {
            var source1 =
@"
interface I1
{
    int this[sbyte i] => 1;
    int this[byte i] => 2;
    int this[short i] { get => 3; }
    int this[ushort i] { get => 4; }
    int this[int i] { set => System.Console.WriteLine(5); }
    int this[uint i] { set => System.Console.WriteLine(6); }
    int this[long i] { get { return 7;} set {} }
    int this[ulong i] { get { return 8;} set {} }
}

class Base : Test
{
    int this[sbyte i] => 10;
    int this[short i] { get => 30; }
    int this[int i] { set => System.Console.WriteLine(50); }
    int this[long i] { get { return 70;} set {} }
}

class Derived : Base, I1
{
    int this[byte i] => 20;
    int this[ushort i] { get => 40; }
    int this[uint i] { set => System.Console.WriteLine(60); }
    int this[ulong i] { get { return 80;} set {} }
}

class Test : I1 
{
    int I1.this[sbyte i] => 100;
    int I1.this[byte i] => 200;
    int I1.this[short i] { get => 300; }
    int I1.this[ushort i] { get => 400; }
    int I1.this[int i] { set => System.Console.WriteLine(500); }
    int I1.this[uint i] { set => System.Console.WriteLine(600); }
    int I1.this[long i] { get { return 700;} set {} }
    int I1.this[ulong i] { get { return 800;} set {} }
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate(ModuleSymbol m)
            {
                var i1 = m.GlobalNamespace.GetTypeMember("I1");
                var indexers = i1.GetMembers("this[]");
                var p1 = (PropertySymbol)indexers[0];
                var p2 = (PropertySymbol)indexers[1];
                var p3 = (PropertySymbol)indexers[2];
                var p4 = (PropertySymbol)indexers[3];
                var p5 = (PropertySymbol)indexers[4];
                var p6 = (PropertySymbol)indexers[5];
                var p7 = (PropertySymbol)indexers[6];
                var p8 = (PropertySymbol)indexers[7];

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                string name = m is PEModuleSymbol ? "Item" : "this";

                Assert.Equal("System.Int32 Test.I1." + name + "[System.SByte i] { get; }", derived.FindImplementationForInterfaceMember(p1).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1." + name + "[System.Byte i] { get; }", derived.FindImplementationForInterfaceMember(p2).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1." + name + "[System.Int16 i] { get; }", derived.FindImplementationForInterfaceMember(p3).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1." + name + "[System.UInt16 i] { get; }", derived.FindImplementationForInterfaceMember(p4).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1." + name + "[System.Int32 i] { set; }", derived.FindImplementationForInterfaceMember(p5).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1." + name + "[System.UInt32 i] { set; }", derived.FindImplementationForInterfaceMember(p6).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1." + name + "[System.Int64 i] { get; set; }", derived.FindImplementationForInterfaceMember(p7).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1." + name + "[System.UInt64 i] { get; set; }", derived.FindImplementationForInterfaceMember(p8).ToTestDisplayString());

                if (m is PEModuleSymbol)
                {
                    Assert.Equal("System.Int32 Test.I1.get_Item(System.SByte i)", derived.FindImplementationForInterfaceMember(p1.GetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.get_Item(System.Byte i)", derived.FindImplementationForInterfaceMember(p2.GetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.get_Item(System.Int16 i)", derived.FindImplementationForInterfaceMember(p3.GetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.get_Item(System.UInt16 i)", derived.FindImplementationForInterfaceMember(p4.GetMethod).ToTestDisplayString());
                    Assert.Equal("void Test.I1.set_Item(System.Int32 i, System.Int32 value)", derived.FindImplementationForInterfaceMember(p5.SetMethod).ToTestDisplayString());
                    Assert.Equal("void Test.I1.set_Item(System.UInt32 i, System.Int32 value)", derived.FindImplementationForInterfaceMember(p6.SetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.get_Item(System.Int64 i)", derived.FindImplementationForInterfaceMember(p7.GetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.get_Item(System.UInt64 i)", derived.FindImplementationForInterfaceMember(p8.GetMethod).ToTestDisplayString());
                    Assert.Equal("void Test.I1.set_Item(System.Int64 i, System.Int32 value)", derived.FindImplementationForInterfaceMember(p7.SetMethod).ToTestDisplayString());
                    Assert.Equal("void Test.I1.set_Item(System.UInt64 i, System.Int32 value)", derived.FindImplementationForInterfaceMember(p8.SetMethod).ToTestDisplayString());
                }
                else
                {
                    Assert.Equal("System.Int32 Test.I1.this[System.SByte i].get", derived.FindImplementationForInterfaceMember(p1.GetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.this[System.Byte i].get", derived.FindImplementationForInterfaceMember(p2.GetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.this[System.Int16 i].get", derived.FindImplementationForInterfaceMember(p3.GetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.this[System.UInt16 i].get", derived.FindImplementationForInterfaceMember(p4.GetMethod).ToTestDisplayString());
                    Assert.Equal("void Test.I1.this[System.Int32 i].set", derived.FindImplementationForInterfaceMember(p5.SetMethod).ToTestDisplayString());
                    Assert.Equal("void Test.I1.this[System.UInt32 i].set", derived.FindImplementationForInterfaceMember(p6.SetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.this[System.Int64 i].get", derived.FindImplementationForInterfaceMember(p7.GetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.this[System.UInt64 i].get", derived.FindImplementationForInterfaceMember(p8.GetMethod).ToTestDisplayString());
                    Assert.Equal("void Test.I1.this[System.Int64 i].set", derived.FindImplementationForInterfaceMember(p7.SetMethod).ToTestDisplayString());
                    Assert.Equal("void Test.I1.this[System.UInt64 i].set", derived.FindImplementationForInterfaceMember(p8.SetMethod).ToTestDisplayString());
                }
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void IndexerImplementation_204()
        {
            var source1 =
@"
interface I1
{
    int this[sbyte i] => 1;
    int this[byte i] => 2;
    int this[short i] { get => 3; }
    int this[ushort i] { get => 4; }
    int this[int i] { set => System.Console.WriteLine(5); }
    int this[uint i] { set => System.Console.WriteLine(6); }
    int this[long i] { get { return 7;} set {} }
    int this[ulong i] { get { return 8;} set {} }
}

class Base : Test
{
    new int this[sbyte i] => 10;
    new int this[short i] { get => 30; }
    new int this[int i] { set => System.Console.WriteLine(50); }
    new int this[long i] { get { return 70;} set {} }
}

class Derived : Base, I1
{
    new int this[byte i] => 20;
    new int this[ushort i] { get => 40; }
    new int this[uint i] { set => System.Console.WriteLine(60); }
    new int this[ulong i] { get { return 80;} set {} }
}

class Test : I1 
{
    public int this[sbyte i] => 100;
    public int this[byte i] => 200;
    public int this[short i] { get => 300; }
    public int this[ushort i] { get => 400; }
    public int this[int i] { set => System.Console.WriteLine(500); }
    public int this[uint i] { set => System.Console.WriteLine(600); }
    public int this[long i] { get { return 700;} set {} }
    public int this[ulong i] { get { return 800;} set {} }
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate(ModuleSymbol m)
            {
                var i1 = m.GlobalNamespace.GetTypeMember("I1");
                var indexers = i1.GetMembers("this[]");
                var p1 = (PropertySymbol)indexers[0];
                var p2 = (PropertySymbol)indexers[1];
                var p3 = (PropertySymbol)indexers[2];
                var p4 = (PropertySymbol)indexers[3];
                var p5 = (PropertySymbol)indexers[4];
                var p6 = (PropertySymbol)indexers[5];
                var p7 = (PropertySymbol)indexers[6];
                var p8 = (PropertySymbol)indexers[7];

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Equal("System.Int32 Test.this[System.SByte i] { get; }", derived.FindImplementationForInterfaceMember(p1).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.Byte i] { get; }", derived.FindImplementationForInterfaceMember(p2).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.Int16 i] { get; }", derived.FindImplementationForInterfaceMember(p3).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.UInt16 i] { get; }", derived.FindImplementationForInterfaceMember(p4).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.Int32 i] { set; }", derived.FindImplementationForInterfaceMember(p5).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.UInt32 i] { set; }", derived.FindImplementationForInterfaceMember(p6).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.Int64 i] { get; set; }", derived.FindImplementationForInterfaceMember(p7).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.UInt64 i] { get; set; }", derived.FindImplementationForInterfaceMember(p8).ToTestDisplayString());

                Assert.Equal("System.Int32 Test.this[System.SByte i].get", derived.FindImplementationForInterfaceMember(p1.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.Byte i].get", derived.FindImplementationForInterfaceMember(p2.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.Int16 i].get", derived.FindImplementationForInterfaceMember(p3.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.UInt16 i].get", derived.FindImplementationForInterfaceMember(p4.GetMethod).ToTestDisplayString());
                Assert.Equal("void Test.this[System.Int32 i].set", derived.FindImplementationForInterfaceMember(p5.SetMethod).ToTestDisplayString());
                Assert.Equal("void Test.this[System.UInt32 i].set", derived.FindImplementationForInterfaceMember(p6.SetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.Int64 i].get", derived.FindImplementationForInterfaceMember(p7.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.UInt64 i].get", derived.FindImplementationForInterfaceMember(p8.GetMethod).ToTestDisplayString());
                Assert.Equal("void Test.this[System.Int64 i].set", derived.FindImplementationForInterfaceMember(p7.SetMethod).ToTestDisplayString());
                Assert.Equal("void Test.this[System.UInt64 i].set", derived.FindImplementationForInterfaceMember(p8.SetMethod).ToTestDisplayString());
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void IndexerImplementation_501()
        {
            var source1 =
@"
public interface I1
{
    int this[sbyte i] => 1;
    int this[short i] 
    { get => 3; }
    int this[int i] 
    { set => System.Console.WriteLine(5); }
    int this[long i] 
    { 
        get { return 7;} 
        set {} 
    }
}

class Test1 : I1
{}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation1 = CreateCompilation(source1, new[] { mscorLibRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;
            compilation1.VerifyDiagnostics(
                // (4,26): error CS8501: Target runtime doesn't support default interface implementation.
                //     int this[sbyte i] => 1;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "1").WithLocation(4, 26),
                // (6,7): error CS8501: Target runtime doesn't support default interface implementation.
                //     { get => 3; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(6, 7),
                // (8,7): error CS8501: Target runtime doesn't support default interface implementation.
                //     { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "set").WithLocation(8, 7),
                // (11,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         get { return 7;} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(11, 9),
                // (12,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         set {} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "set").WithLocation(12, 9)
                );

            ValidateIndexerImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.False(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.this[long].set' cannot implement interface member 'I1.this[long].set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[long].set", "I1.this[long].set", "Test2"),
                // (2,15): error CS8502: 'I1.this[sbyte].get' cannot implement interface member 'I1.this[sbyte].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[sbyte].get", "I1.this[sbyte].get", "Test2"),
                // (2,15): error CS8502: 'I1.this[short].get' cannot implement interface member 'I1.this[short].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[short].get", "I1.this[short].get", "Test2"),
                // (2,15): error CS8502: 'I1.this[int].set' cannot implement interface member 'I1.this[int].set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[int].set", "I1.this[int].set", "Test2"),
                // (2,15): error CS8502: 'I1.this[long].get' cannot implement interface member 'I1.this[long].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[long].get", "I1.this[long].get", "Test2")
                );

            ValidateIndexerImplementation_501(compilation3.SourceModule, "Test2");
        }

        private static void ValidateIndexerImplementation_501(ModuleSymbol m, string typeName)
        {
            var derived = m.GlobalNamespace.GetTypeMember(typeName);
            var i1 = derived.Interfaces.Single();
            Assert.Equal("I1", i1.ToTestDisplayString());

            var indexers = i1.GetMembers("this[]");
            var p1 = (PropertySymbol)indexers[0];
            var p3 = (PropertySymbol)indexers[1];
            var p5 = (PropertySymbol)indexers[2];
            var p7 = (PropertySymbol)indexers[3];

            Assert.True(p1.IsVirtual);
            Assert.True(p3.IsVirtual);
            Assert.True(p5.IsVirtual);
            Assert.True(p7.IsVirtual);

            Assert.False(p1.IsAbstract);
            Assert.False(p3.IsAbstract);
            Assert.False(p5.IsAbstract);
            Assert.False(p7.IsAbstract);

            Assert.Same(p1, derived.FindImplementationForInterfaceMember(p1));
            Assert.Same(p3, derived.FindImplementationForInterfaceMember(p3));
            Assert.Same(p5, derived.FindImplementationForInterfaceMember(p5));
            Assert.Same(p7, derived.FindImplementationForInterfaceMember(p7));

            Assert.True(p1.GetMethod.IsVirtual);
            Assert.True(p3.GetMethod.IsVirtual);
            Assert.True(p5.SetMethod.IsVirtual);
            Assert.True(p7.GetMethod.IsVirtual);
            Assert.True(p7.SetMethod.IsVirtual);

            Assert.True(p1.GetMethod.IsMetadataVirtual());
            Assert.True(p3.GetMethod.IsMetadataVirtual());
            Assert.True(p5.SetMethod.IsMetadataVirtual());
            Assert.True(p7.GetMethod.IsMetadataVirtual());
            Assert.True(p7.SetMethod.IsMetadataVirtual());

            Assert.False(p1.GetMethod.IsAbstract);
            Assert.False(p3.GetMethod.IsAbstract);
            Assert.False(p5.SetMethod.IsAbstract);
            Assert.False(p7.GetMethod.IsAbstract);
            Assert.False(p7.SetMethod.IsAbstract);

            Assert.Same(p1.GetMethod, derived.FindImplementationForInterfaceMember(p1.GetMethod));
            Assert.Same(p3.GetMethod, derived.FindImplementationForInterfaceMember(p3.GetMethod));
            Assert.Same(p5.SetMethod, derived.FindImplementationForInterfaceMember(p5.SetMethod));
            Assert.Same(p7.GetMethod, derived.FindImplementationForInterfaceMember(p7.GetMethod));
            Assert.Same(p7.SetMethod, derived.FindImplementationForInterfaceMember(p7.SetMethod));
        }

        [Fact]
        public void IndexerImplementation_502()
        {
            var source1 =
@"
public interface I1
{
    int this[sbyte i] => 1;
    int this[short i] 
    { get => 3; }
    int this[int i] 
    { set => System.Console.WriteLine(5); }
    int this[long i] 
    { 
        get { return 7;} 
        set {} 
    }
}
";

            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var source2 =
@"
class Test2 : I1
{}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.this[short].get' cannot implement interface member 'I1.this[short].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[short].get", "I1.this[short].get", "Test2"),
                // (2,15): error CS8502: 'I1.this[int].set' cannot implement interface member 'I1.this[int].set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[int].set", "I1.this[int].set", "Test2"),
                // (2,15): error CS8502: 'I1.this[long].get' cannot implement interface member 'I1.this[long].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[long].get", "I1.this[long].get", "Test2"),
                // (2,15): error CS8502: 'I1.this[long].set' cannot implement interface member 'I1.this[long].set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[long].set", "I1.this[long].set", "Test2"),
                // (2,15): error CS8502: 'I1.this[sbyte].get' cannot implement interface member 'I1.this[sbyte].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[sbyte].get", "I1.this[sbyte].get", "Test2")
                );

            ValidateIndexerImplementation_501(compilation3.SourceModule, "Test2");
        }

        [Fact]
        public void IndexerImplementation_503()
        {
            var source1 =
@"
public interface I1
{
    int this[sbyte i] => 1;
    int this[short i] 
    { get => 3; }
    int this[int i] 
    { set => System.Console.WriteLine(5); }
    int this[long i] 
    { 
        get { return 7;} 
        set {} 
    }
}
";

            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var source2 =
@"
public interface I2
{
    void M2();
}

class Test2 : I2
{
    public void M2() {}
}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            var test2 = compilation3.GetTypeByMetadataName("Test2");
            var i1 = compilation3.GetTypeByMetadataName("I1");
            Assert.Equal("I1", i1.ToTestDisplayString());

            var indexers = i1.GetMembers("this[]");
            var p1 = (PropertySymbol)indexers[0];
            var p3 = (PropertySymbol)indexers[1];
            var p5 = (PropertySymbol)indexers[2];
            var p7 = (PropertySymbol)indexers[3];

            Assert.Null(test2.FindImplementationForInterfaceMember(p1));
            Assert.Null(test2.FindImplementationForInterfaceMember(p3));
            Assert.Null(test2.FindImplementationForInterfaceMember(p5));
            Assert.Null(test2.FindImplementationForInterfaceMember(p7));

            Assert.Null(test2.FindImplementationForInterfaceMember(p1.GetMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(p3.GetMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(p5.SetMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(p7.GetMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(p7.SetMethod));

            compilation3.VerifyDiagnostics();
        }

        [Fact]
        public void IndexerImplementation_601()
        {
            var source1 =
@"
public interface I1
{
    int this[sbyte i] => 1;
    int this[short i] 
    { get => 3; }
    int this[int i] 
    { set => System.Console.WriteLine(5); }
    int this[long i] 
    { 
        get { return 7;} 
        set {} 
    }
}

class Test1 : I1
{}
";
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation1 = CreateCompilation(source1, new[] { mscorLibRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            compilation1.VerifyDiagnostics(
                // (4,26): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int this[sbyte i] => 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "1").WithArguments("default interface implementation", "7.1").WithLocation(4, 26),
                // (4,26): error CS8501: Target runtime doesn't support default interface implementation.
                //     int this[sbyte i] => 1;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "1").WithLocation(4, 26),
                // (6,7): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     { get => 3; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(6, 7),
                // (6,7): error CS8501: Target runtime doesn't support default interface implementation.
                //     { get => 3; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(6, 7),
                // (8,7): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(8, 7),
                // (8,7): error CS8501: Target runtime doesn't support default interface implementation.
                //     { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "set").WithLocation(8, 7),
                // (11,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         get { return 7;} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(11, 9),
                // (11,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         get { return 7;} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(11, 9),
                // (12,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         set {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(12, 9),
                // (12,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         set {} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "set").WithLocation(12, 9)
                );

            ValidateIndexerImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.False(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.this[long].set' cannot implement interface member 'I1.this[long].set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[long].set", "I1.this[long].set", "Test2"),
                // (2,15): error CS8502: 'I1.this[sbyte].get' cannot implement interface member 'I1.this[sbyte].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[sbyte].get", "I1.this[sbyte].get", "Test2"),
                // (2,15): error CS8502: 'I1.this[short].get' cannot implement interface member 'I1.this[short].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[short].get", "I1.this[short].get", "Test2"),
                // (2,15): error CS8502: 'I1.this[int].set' cannot implement interface member 'I1.this[int].set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[int].set", "I1.this[int].set", "Test2"),
                // (2,15): error CS8502: 'I1.this[long].get' cannot implement interface member 'I1.this[long].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[long].get", "I1.this[long].get", "Test2")
                );

            ValidateIndexerImplementation_501(compilation3.SourceModule, "Test2");
        }

        [Fact]
        public void IndexerImplementation_701()
        {
            var source1 =
@"
public interface I1
{
    int this[sbyte i] => 1;
    int this[short i] 
    { get => 3; }
    int this[int i] 
    { set => System.Console.WriteLine(5); }
    int this[long i] 
    { 
        get { return 7;} 
        set {} 
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (4,26): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int this[sbyte i] => 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "1").WithArguments("default interface implementation", "7.1").WithLocation(4, 26),
                // (6,7): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     { get => 3; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(6, 7),
                // (8,7): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(8, 7),
                // (11,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         get { return 7;} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(11, 9),
                // (12,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         set {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(12, 9)
                );

            ValidateIndexerImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                            parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics();

            ValidateIndexerImplementation_501(compilation2.SourceModule, "Test2");

            CompileAndVerify(compilation2, verify: false,
                symbolValidator: (m) =>
                {
                    var test2Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test2");
                    Assert.Equal("I1", test2Result.Interfaces.Single().ToTestDisplayString());
                    ValidateIndexerImplementation_501(m, "Test2");
                });
        }

        [Fact]
        public void IndexerImplementation_901()
        {
            var source1 =
@"
public interface I1
{
    static int this[sbyte i] => 1;
    static int this[short i] 
    { get => 3; }
    static int this[int i] 
    { set => System.Console.WriteLine(5); }
    static int this[long i] 
    { 
        get { return 7;} 
        set {} 
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (4,16): error CS0106: The modifier 'static' is not valid for this item
                //     static int this[sbyte i] => 1;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(4, 16),
                // (4,33): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     static int this[sbyte i] => 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "1").WithArguments("default interface implementation", "7.1").WithLocation(4, 33),
                // (5,16): error CS0106: The modifier 'static' is not valid for this item
                //     static int this[short i] 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(5, 16),
                // (6,7): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     { get => 3; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(6, 7),
                // (7,16): error CS0106: The modifier 'static' is not valid for this item
                //     static int this[int i] 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(7, 16),
                // (8,7): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(8, 7),
                // (9,16): error CS0106: The modifier 'static' is not valid for this item
                //     static int this[long i] 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(9, 16),
                // (11,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         get { return 7;} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(11, 9),
                // (12,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         set {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(12, 9)
                );

            ValidateIndexerImplementation_501(compilation1.SourceModule, "Test1");
        }

        [Fact]
        public void EventImplementation_101()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
        add
        {
            System.Console.WriteLine(""add E1"");
        }
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,25): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 25)
                );

            ValidateEventImplementationTest1_101(compilation1.SourceModule, haveAdd: true, haveRemove: false);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateEventImplementationTest2_101(m, haveAdd: true, haveRemove: false);
            }

            Validate2(compilation2.SourceModule);
            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);
        }

        private static void ValidateEventImplementationTest1_101(ModuleSymbol m, bool haveAdd, bool haveRemove)
        {
            var i1 = m.GlobalNamespace.GetTypeMember("I1");
            var e1 = i1.GetMember<EventSymbol>("E1");
            var addE1 = e1.AddMethod;
            var rmvE1 = e1.RemoveMethod;

            if (haveAdd)
            {
                Assert.False(addE1.IsAbstract);
                Assert.True(addE1.IsVirtual);
                Assert.True(addE1.IsMetadataVirtual());
            }
            else
            {
                Assert.Null(addE1);
            }

            if (haveRemove)
            {
                Assert.False(rmvE1.IsAbstract);
                Assert.True(rmvE1.IsVirtual);
                Assert.True(rmvE1.IsMetadataVirtual());
            }
            else
            {
                Assert.Null(rmvE1);
            }

            Assert.False(e1.IsAbstract);
            Assert.True(e1.IsVirtual);
            Assert.True(i1.IsAbstract);
            Assert.True(i1.IsMetadataAbstract);

            if (m is PEModuleSymbol peModule)
            {
                int rva;

                if (haveAdd)
                {
                    peModule.Module.GetMethodDefPropsOrThrow(((PEMethodSymbol)addE1).Handle, out _, out _, out _, out rva);
                    Assert.NotEqual(0, rva);
                }

                if (haveRemove)
                {
                    peModule.Module.GetMethodDefPropsOrThrow(((PEMethodSymbol)rmvE1).Handle, out _, out _, out _, out rva);
                    Assert.NotEqual(0, rva);
                }
            }

            var test1 = m.GlobalNamespace.GetTypeMember("Test1");
            Assert.Equal("I1", test1.Interfaces.Single().ToTestDisplayString());
            Assert.Same(e1, test1.FindImplementationForInterfaceMember(e1));

            if (haveAdd)
            {
                Assert.Same(addE1, test1.FindImplementationForInterfaceMember(addE1));
            }

            if (haveRemove)
            {
                Assert.Same(rmvE1, test1.FindImplementationForInterfaceMember(rmvE1));
            }
        }

        private static void ValidateEventImplementationTest2_101(ModuleSymbol m, bool haveAdd, bool haveRemove)
        {
            var test2 = m.GlobalNamespace.GetTypeMember("Test2");
            Assert.Equal("I1", test2.Interfaces.Single().ToTestDisplayString());

            var e1 = test2.Interfaces.Single().GetMember<EventSymbol>("E1");
            Assert.Same(e1, test2.FindImplementationForInterfaceMember(e1));

            if (haveAdd)
            {
                var addP1 = e1.AddMethod;
                Assert.Same(addP1, test2.FindImplementationForInterfaceMember(addP1));
            }

            if (haveRemove)
            {
                var rmvP1 = e1.RemoveMethod;
                Assert.Same(rmvP1, test2.FindImplementationForInterfaceMember(rmvP1));
            }
        }

        [Fact]
        public void EventImplementation_102()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
        add => System.Console.WriteLine(""add E1"");
        remove => System.Console.WriteLine(""remove E1"");
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate1(ModuleSymbol m)
            {
                ValidateEventImplementationTest1_101(m, haveAdd: true, haveRemove: true);
            }

            Validate1(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateEventImplementationTest2_101(m, haveAdd: true, haveRemove: true);
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);

            var compilation3 = CreateCompilationWithMscorlib(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            Validate2(compilation3.SourceModule);

            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void EventImplementation_103()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
        remove
        {
            System.Console.WriteLine(""remove E1"");
        }
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (4,25): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 25)
                );

            ValidateEventImplementationTest1_101(compilation1.SourceModule, haveAdd: false, haveRemove: true);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateEventImplementationTest2_101(m, haveAdd: false, haveRemove: true);
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void EventImplementation_104()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1
    {
        add;
    }
}

class Test1 : I1
{}
";

            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (6,12): error CS0073: An add or remove accessor must have a body
                //         add;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(6, 12),
                // (4,25): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 25)
                );

            ValidateEventImplementationTest1_101(compilation1.SourceModule, haveAdd: true, haveRemove: false);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateEventImplementationTest2_101(m, haveAdd: true, haveRemove: false);
            }

            Validate2(compilation2.SourceModule);
            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void EventImplementation_105()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
        remove;
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (6,15): error CS0073: An add or remove accessor must have a body
                //         remove;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(6, 15),
                // (4,25): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 25)
                );

            ValidateEventImplementationTest1_101(compilation1.SourceModule, haveAdd: false, haveRemove: true);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateEventImplementationTest2_101(m, haveAdd: false, haveRemove: true);
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void EventImplementation_106()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (4,25): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 25)
                );

            ValidateEventImplementationTest1_101(compilation1.SourceModule, haveAdd: false, haveRemove: false);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateEventImplementationTest2_101(m, haveAdd: false, haveRemove: false);
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void EventImplementation_107()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
        add;
        remove;
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (6,12): error CS0073: An add or remove accessor must have a body
                //         add;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(6, 12),
                // (7,15): error CS0073: An add or remove accessor must have a body
                //         remove;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(7, 15)
                );

            ValidateEventImplementationTest1_101(compilation1.SourceModule, haveAdd: true, haveRemove: true);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateEventImplementationTest2_101(m, haveAdd: true, haveRemove: true);
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void EventImplementation_108()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
        get;
        set;
    } => 0;
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (8,7): error CS1519: Invalid token '=>' in class, struct, or interface member declaration
                //     } => 0;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=>").WithArguments("=>").WithLocation(8, 7),
                // (6,9): error CS1055: An add or remove accessor expected
                //         get;
                Diagnostic(ErrorCode.ERR_AddOrRemoveExpected, "get").WithLocation(6, 9),
                // (7,9): error CS1055: An add or remove accessor expected
                //         set;
                Diagnostic(ErrorCode.ERR_AddOrRemoveExpected, "set").WithLocation(7, 9),
                // (4,25): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1")
                );

            ValidateEventImplementationTest1_101(compilation1.SourceModule, haveAdd: false, haveRemove: false);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateEventImplementationTest2_101(m, haveAdd: false, haveRemove: false);
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void EventImplementation_109()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
        add => throw null;
        remove;
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (6,12): error CS0073: An add or remove accessor must have a body
                //         add;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";")
                );

            ValidateEventImplementationTest1_101(compilation1.SourceModule, haveAdd: true, haveRemove: true);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateEventImplementationTest2_101(m, haveAdd: true, haveRemove: true);
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void EventImplementation_110()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
        add;
        remove => throw null;
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (6,12): error CS0073: An add or remove accessor must have a body
                //         add;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";")
                );

            ValidateEventImplementationTest1_101(compilation1.SourceModule, haveAdd: true, haveRemove: true);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateEventImplementationTest2_101(m, haveAdd: true, haveRemove: true);
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void EventImplementation_201()
        {
            var source1 =
@"
interface I1
{
    event System.Action E7 { add {} remove {} }
    event System.Action E8 { add {} remove {} }
}

class Base
{
    event System.Action E7;
}

class Derived : Base, I1
{
    event System.Action E8 { add {} remove {} }
}

class Test : I1 {}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (10,25): warning CS0067: The event 'Base.E7' is never used
                //     event System.Action E7;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E7").WithArguments("Base.E7").WithLocation(10, 25)
                );

            ValidateEventImplementation_201(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    ValidateEventImplementation_201(m);
                });
        }

        private static void ValidateEventImplementation_201(ModuleSymbol m)
        {
            var e7 = m.GlobalNamespace.GetMember<EventSymbol>("I1.E7");
            var e8 = m.GlobalNamespace.GetMember<EventSymbol>("I1.E8");

            var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

            Assert.Same(e7, derived.FindImplementationForInterfaceMember(e7));
            Assert.Same(e8, derived.FindImplementationForInterfaceMember(e8));

            Assert.Same(e7.AddMethod, derived.FindImplementationForInterfaceMember(e7.AddMethod));
            Assert.Same(e8.AddMethod, derived.FindImplementationForInterfaceMember(e8.AddMethod));
            Assert.Same(e7.RemoveMethod, derived.FindImplementationForInterfaceMember(e7.RemoveMethod));
            Assert.Same(e8.RemoveMethod, derived.FindImplementationForInterfaceMember(e8.RemoveMethod));
        }

        [Fact]
        public void EventImplementation_202()
        {
            var source1 =
@"
interface I1
{
    event System.Action E7 { add {} remove {} }
    event System.Action E8 { add {} remove {} }
}

class Base : Test
{
    event System.Action E7;
}

class Derived : Base, I1
{
    event System.Action E8 { add {} remove {} }
}

class Test : I1 {}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (10,25): warning CS0067: The event 'Base.E7' is never used
                //     event System.Action E7;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E7").WithArguments("Base.E7").WithLocation(10, 25)
                );

            ValidateEventImplementation_201(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    ValidateEventImplementation_201(m);
                });
        }

        [Fact]
        public void EventImplementation_203()
        {
            var source1 =
@"
interface I1
{
    event System.Action E7 { add {} remove {} }
    event System.Action E8 { add {} remove {} }
}

class Base : Test
{
    event System.Action E7;
}

class Derived : Base, I1
{
    event System.Action E8 { add {} remove {} }
}

class Test : I1 
{
    event System.Action I1.E7 { add {} remove {} }
    event System.Action I1.E8 { add {} remove {} }
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (10,25): warning CS0067: The event 'Base.E7' is never used
                //     event System.Action E7;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E7").WithArguments("Base.E7").WithLocation(10, 25)
                );

            void Validate(ModuleSymbol m)
            {
                var e7 = m.GlobalNamespace.GetMember<EventSymbol>("I1.E7");
                var e8 = m.GlobalNamespace.GetMember<EventSymbol>("I1.E8");

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Equal("event System.Action Test.I1.E7", derived.FindImplementationForInterfaceMember(e7).ToTestDisplayString());
                Assert.Equal("event System.Action Test.I1.E8", derived.FindImplementationForInterfaceMember(e8).ToTestDisplayString());

                Assert.Equal("void Test.I1.E7.add", derived.FindImplementationForInterfaceMember(e7.AddMethod).ToTestDisplayString());
                Assert.Equal("void Test.I1.E8.add", derived.FindImplementationForInterfaceMember(e8.AddMethod).ToTestDisplayString());
                Assert.Equal("void Test.I1.E7.remove", derived.FindImplementationForInterfaceMember(e7.RemoveMethod).ToTestDisplayString());
                Assert.Equal("void Test.I1.E8.remove", derived.FindImplementationForInterfaceMember(e8.RemoveMethod).ToTestDisplayString());
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void EventImplementation_204()
        {
            var source1 =
@"
interface I1
{
    event System.Action E7 { add {} remove {} }
    event System.Action E8 { add {} remove {} }
}

class Base : Test
{
    new event System.Action E7;
}

class Derived : Base, I1
{
    new event System.Action E8 { add {} remove {} }
}

class Test : I1 
{
    public event System.Action E7 { add {} remove {} }
    public event System.Action E8 { add {} remove {} }
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (10,29): warning CS0067: The event 'Base.E7' is never used
                //     new event System.Action E7;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E7").WithArguments("Base.E7").WithLocation(10, 29)
                );

            void Validate(ModuleSymbol m)
            {
                var e7 = m.GlobalNamespace.GetMember<EventSymbol>("I1.E7");
                var e8 = m.GlobalNamespace.GetMember<EventSymbol>("I1.E8");

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Equal("event System.Action Test.E7", derived.FindImplementationForInterfaceMember(e7).ToTestDisplayString());
                Assert.Equal("event System.Action Test.E8", derived.FindImplementationForInterfaceMember(e8).ToTestDisplayString());

                Assert.Equal("void Test.E7.add", derived.FindImplementationForInterfaceMember(e7.AddMethod).ToTestDisplayString());
                Assert.Equal("void Test.E8.add", derived.FindImplementationForInterfaceMember(e8.AddMethod).ToTestDisplayString());
                Assert.Equal("void Test.E7.remove", derived.FindImplementationForInterfaceMember(e7.RemoveMethod).ToTestDisplayString());
                Assert.Equal("void Test.E8.remove", derived.FindImplementationForInterfaceMember(e8.RemoveMethod).ToTestDisplayString());
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void EventImplementation_501()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E7 
    { 
        add {} 
        remove {} 
    }
}

class Test1 : I1
{}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation1 = CreateCompilation(source1, new[] { mscorLibRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;
            compilation1.VerifyDiagnostics(
                // (6,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         add {} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "add").WithLocation(6, 9),
                // (7,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         remove {} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "remove").WithLocation(7, 9)
                );

            ValidateEventImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.False(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.E7.remove' cannot implement interface member 'I1.E7.remove' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.E7.remove", "I1.E7.remove", "Test2"),
                // (2,15): error CS8502: 'I1.E7.add' cannot implement interface member 'I1.E7.add' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.E7.add", "I1.E7.add", "Test2")
                );

            ValidateEventImplementation_501(compilation3.SourceModule, "Test2");
        }

        private static void ValidateEventImplementation_501(ModuleSymbol m, string typeName)
        {
            var derived = m.GlobalNamespace.GetTypeMember(typeName);
            var i1 = derived.Interfaces.Single();
            Assert.Equal("I1", i1.ToTestDisplayString());

            var e7 = i1.GetMember<EventSymbol>("E7");

            Assert.True(e7.IsVirtual);
            Assert.False(e7.IsAbstract);

            Assert.Same(e7, derived.FindImplementationForInterfaceMember(e7));

            Assert.True(e7.AddMethod.IsVirtual);
            Assert.True(e7.RemoveMethod.IsVirtual);

            Assert.True(e7.AddMethod.IsMetadataVirtual());
            Assert.True(e7.RemoveMethod.IsMetadataVirtual());

            Assert.False(e7.AddMethod.IsAbstract);
            Assert.False(e7.RemoveMethod.IsAbstract);

            Assert.Same(e7.AddMethod, derived.FindImplementationForInterfaceMember(e7.AddMethod));
            Assert.Same(e7.RemoveMethod, derived.FindImplementationForInterfaceMember(e7.RemoveMethod));
        }

        [Fact]
        public void EventImplementation_502()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E7 
    { 
        add {} 
        remove {} 
    }
}
";

            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var source2 =
@"
class Test2 : I1
{}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.E7.remove' cannot implement interface member 'I1.E7.remove' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.E7.remove", "I1.E7.remove", "Test2"),
                // (2,15): error CS8502: 'I1.E7.add' cannot implement interface member 'I1.E7.add' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.E7.add", "I1.E7.add", "Test2")
                );

            ValidateEventImplementation_501(compilation3.SourceModule, "Test2");
        }

        [Fact]
        public void EventImplementation_503()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E7 
    { 
        add {} 
        remove {} 
    }
}
";

            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var source2 =
@"
public interface I2
{
    void M2();
}

class Test2 : I2
{
    public void M2() {}
}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            var test2 = compilation3.GetTypeByMetadataName("Test2");
            var i1 = compilation3.GetTypeByMetadataName("I1");
            Assert.Equal("I1", i1.ToTestDisplayString());

            var e7 = i1.GetMember<EventSymbol>("E7");

            Assert.Null(test2.FindImplementationForInterfaceMember(e7));

            Assert.Null(test2.FindImplementationForInterfaceMember(e7.AddMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(e7.RemoveMethod));

            compilation3.VerifyDiagnostics();
        }

        [Fact]
        public void EventImplementation_601()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E7 
    { 
        add {} 
        remove {} 
    }
}

class Test1 : I1
{}
";
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation1 = CreateCompilation(source1, new[] { mscorLibRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            compilation1.VerifyDiagnostics(
                // (6,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         add {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(6, 9),
                // (6,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         add {} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "add").WithLocation(6, 9),
                // (7,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         remove {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(7, 9),
                // (7,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         remove {} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "remove").WithLocation(7, 9)
                );

            ValidateEventImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.False(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.E7.remove' cannot implement interface member 'I1.E7.remove' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.E7.remove", "I1.E7.remove", "Test2"),
                // (2,15): error CS8502: 'I1.E7.add' cannot implement interface member 'I1.E7.add' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.E7.add", "I1.E7.add", "Test2")
                );

            ValidateEventImplementation_501(compilation3.SourceModule, "Test2");
        }

        [Fact]
        public void EventImplementation_701()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E7 
    { 
        add {} 
        remove {} 
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (6,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         add {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(6, 9),
                // (7,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         remove {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(7, 9)
                );

            ValidateEventImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                            parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics();

            ValidateEventImplementation_501(compilation2.SourceModule, "Test2");

            CompileAndVerify(compilation2, verify: false,
                symbolValidator: (m) =>
                {
                    var test2Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test2");
                    Assert.Equal("I1", test2Result.Interfaces.Single().ToTestDisplayString());
                    ValidateEventImplementation_501(m, "Test2");
                });
        }

        [Fact]
        public void EventImplementation_901()
        {
            var source1 =
@"
public interface I1
{
    static event System.Action E7 
    { 
        add {} 
        remove {} 
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (4,32): error CS0106: The modifier 'static' is not valid for this item
                //     static event System.Action E7 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "E7").WithArguments("static").WithLocation(4, 32),
                // (6,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         add {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(6, 9),
                // (7,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         remove {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(7, 9)
                );

            ValidateEventImplementation_501(compilation1.SourceModule, "Test1");
        }
    }
}
