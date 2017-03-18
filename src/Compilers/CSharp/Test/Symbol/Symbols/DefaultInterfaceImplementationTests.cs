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

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");

            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));

            compilation1.VerifyDiagnostics();
            Assert.True(m1.IsMetadataVirtual());

            CompileAndVerify(compilation1, verify:false,  
                symbolValidator: (m) =>
                {
                    var result = (PEMethodSymbol)m.GlobalNamespace.GetTypeMember("I1").GetMember("M1");

                    Assert.True(m1.IsMetadataVirtual());
                    Assert.False(m1.IsAbstract);
                    Assert.True(m1.IsVirtual);

                    int rva;
                    ((PEModuleSymbol)m).Module.GetMethodDefPropsOrThrow(result.Handle, out _, out _, out _, out rva);
                    Assert.NotEqual(0, rva);

                    var test1Result = m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });

            var source2 =
@"
class Test2 : I1
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
                    Assert.Equal("I1", test2Result.Interfaces.Single().ToTestDisplayString());
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
                    Assert.Equal("I1", test2Result.Interfaces.Single().ToTestDisplayString());
                });
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

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");

            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Equal("void Test1.M1()", test1.FindImplementationForInterfaceMember(m1).ToTestDisplayString());

            compilation1.VerifyDiagnostics();
            Assert.True(m1.IsMetadataVirtual());

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var result = (PEMethodSymbol)m.GlobalNamespace.GetTypeMember("I1").GetMember("M1");

                    Assert.True(m1.IsMetadataVirtual());
                    Assert.False(m1.IsAbstract);
                    Assert.True(m1.IsVirtual);

                    int rva;
                    ((PEModuleSymbol)m).Module.GetMethodDefPropsOrThrow(result.Handle, out _, out _, out _, out rva);
                    Assert.NotEqual(0, rva);

                    var test1Result = m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });

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
            m1 = compilation2.GetMember<MethodSymbol>("I1.M1");
            var test2 = compilation2.GetTypeByMetadataName("Test2");

            Assert.Equal("void Test2.M1()", test2.FindImplementationForInterfaceMember(m1).ToTestDisplayString());

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false,
                symbolValidator: (m) =>
                {
                    var test2Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test2");
                    Assert.Equal("I1", test2Result.Interfaces.Single().ToTestDisplayString());
                });

            var compilation3 = CreateCompilationWithMscorlib(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            m1 = compilation3.GetMember<MethodSymbol>("I1.M1");
            test2 = compilation3.GetTypeByMetadataName("Test2");

            Assert.Equal("void Test2.M1()", test2.FindImplementationForInterfaceMember(m1).ToTestDisplayString());

            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false,
                symbolValidator: (m) =>
                {
                    var test2Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test2");
                    Assert.Equal("I1", test2Result.Interfaces.Single().ToTestDisplayString());
                });
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

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");

            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Equal("void Test1.I1.M1()", test1.FindImplementationForInterfaceMember(m1).ToTestDisplayString());

            compilation1.VerifyDiagnostics();
            Assert.True(m1.IsMetadataVirtual());

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var result = (PEMethodSymbol)m.GlobalNamespace.GetTypeMember("I1").GetMember("M1");

                    Assert.True(m1.IsMetadataVirtual());
                    Assert.False(m1.IsAbstract);
                    Assert.True(m1.IsVirtual);

                    int rva;
                    ((PEModuleSymbol)m).Module.GetMethodDefPropsOrThrow(result.Handle, out _, out _, out _, out rva);
                    Assert.NotEqual(0, rva);

                    var test1Result = m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });

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
            m1 = compilation2.GetMember<MethodSymbol>("I1.M1");
            var test2 = compilation2.GetTypeByMetadataName("Test2");

            Assert.Equal("void Test2.I1.M1()", test2.FindImplementationForInterfaceMember(m1).ToTestDisplayString());

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false,
                symbolValidator: (m) =>
                {
                    var test2Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test2");
                    Assert.Equal("I1", test2Result.Interfaces.Single().ToTestDisplayString());
                });

            var compilation3 = CreateCompilationWithMscorlib(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            m1 = compilation3.GetMember<MethodSymbol>("I1.M1");
            test2 = compilation3.GetTypeByMetadataName("Test2");

            Assert.Equal("void Test2.I1.M1()", test2.FindImplementationForInterfaceMember(m1).ToTestDisplayString());

            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false,
                symbolValidator: (m) =>
                {
                    var test2Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test2");
                    Assert.Equal("I1", test2Result.Interfaces.Single().ToTestDisplayString());
                });
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

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var m2 = compilation1.GetMember<MethodSymbol>("I1.M2");

            var derived = compilation1.GetTypeByMetadataName("Derived");

            Assert.Same(m1, derived.FindImplementationForInterfaceMember(m1));
            Assert.Same(m2, derived.FindImplementationForInterfaceMember(m2));

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());
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

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var m2 = compilation1.GetMember<MethodSymbol>("I1.M2");

            var derived = compilation1.GetTypeByMetadataName("Derived");

            Assert.Same(m1, derived.FindImplementationForInterfaceMember(m1));
            Assert.Same(m2, derived.FindImplementationForInterfaceMember(m2));

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());
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

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var m2 = compilation1.GetMember<MethodSymbol>("I1.M2");

            var derived = compilation1.GetTypeByMetadataName("Derived");

            Assert.Equal("void Test.I1.M1()", derived.FindImplementationForInterfaceMember(m1).ToTestDisplayString());
            Assert.Equal("void Test.I1.M2()", derived.FindImplementationForInterfaceMember(m2).ToTestDisplayString());

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());
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

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var m2 = compilation1.GetMember<MethodSymbol>("I1.M2");

            var derived = compilation1.GetTypeByMetadataName("Derived");

            Assert.Equal("void Test.M1()", derived.FindImplementationForInterfaceMember(m1).ToTestDisplayString());
            Assert.Equal("void Test.M2()", derived.FindImplementationForInterfaceMember(m2).ToTestDisplayString());

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());
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
                    var result = (PEMethodSymbol)m.GlobalNamespace.GetTypeMember("I1").GetMember("M1");

                    Assert.True(m1.IsMetadataVirtual());
                    Assert.False(m1.IsAbstract);
                    Assert.True(m1.IsVirtual);

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
    }
}
