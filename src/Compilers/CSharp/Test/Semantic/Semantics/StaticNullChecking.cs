// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class StaticNullChecking : CompilingTestBase
    {
        [Fact]
        public void Test0()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
        string? x = null;
    }
}
");

            c.VerifyDiagnostics(
    // (6,9): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
    //         string? x = null;
    Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "string?").WithArguments("System.Nullable<T>", "T", "string").WithLocation(6, 9),
    // (6,17): warning CS0219: The variable 'x' is assigned but its value is never used
    //         string? x = null;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(6, 17)
                );
        }

        [Fact]
        public void MissingNullableType_01()
        {
            CSharpCompilation core = CreateCompilation(@"
namespace System
{
    public class Object {}
    public struct Int32 {}
    public struct Void {}
}
");


            CSharpCompilation c = CreateCompilation(@"
class C
{
    static void Main()
    {
        int? x = null;
    }

    static void Test(int? x) {}
}
", new[] { core.ToMetadataReference() }, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (9,22): error CS0518: Predefined type 'System.Nullable`1' is not defined or imported
    //     static void Test(int? x) {}
    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "int?").WithArguments("System.Nullable`1").WithLocation(9, 22),
    // (6,9): error CS0518: Predefined type 'System.Nullable`1' is not defined or imported
    //         int? x = null;
    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "int?").WithArguments("System.Nullable`1").WithLocation(6, 9)
                );
        }

        [Fact]
        public void MissingNullableType_02()
        {
            CSharpCompilation core = CreateCompilation(@"
namespace System
{
    public class Object {}
    public struct Void {}
}
");


            CSharpCompilation c = CreateCompilation(@"
class C
{
    static void Main()
    {
        object? x = null;
    }

    static void Test(object? x) {}
}
", new[] { core.ToMetadataReference() }, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (6,17): warning CS0219: The variable 'x' is assigned but its value is never used
    //         object? x = null;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(6, 17)
                );
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_01()
        {
            var source = @"
class A
{
    public virtual T? Foo<T>() where T : struct 
    { 
        return null; 
    }
}

class B : A
{
    public override T? Foo<T>()
    {
        return null;
    }
} 
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            //var a = compilation.GetTypeByMetadataName("A");
            //var aFoo = a.GetMember<MethodSymbol>("Foo");
            //Assert.Equal("T? A.Foo<T>()", aFoo.ToTestDisplayString());

            //var b = compilation.GetTypeByMetadataName("B");
            //var bFoo = b.GetMember<MethodSymbol>("Foo");
            //Assert.Equal("T? A.Foo<T>()", bFoo.OverriddenMethod.ToTestDisplayString());

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_02()
        {
            var source = @"
class A
{
    public virtual void Foo<T>(T? x) where T : struct 
    { 
    }
}

class B : A
{
    public override void Foo<T>(T? x)
    {
    }
} 
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_03()
        {
            var source = @"
class A
{
    public virtual System.Nullable<T> Foo<T>() where T : struct 
    { 
        return null; 
    }
}

class B : A
{
    public override T? Foo<T>()
    {
        return null;
    }
} 
";
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true")).VerifyDiagnostics();
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_04()
        {
            var source = @"
class A
{
    public virtual void Foo<T>(System.Nullable<T> x) where T : struct 
    { 
    }
}

class B : A
{
    public override void Foo<T>(T? x)
    {
    }
} 
";
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true")).VerifyDiagnostics();
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_05()
        {
            var source = @"
class A
{
    public virtual T? Foo<T>() where T : struct 
    { 
        return null; 
    }
}

class B : A
{
    public override System.Nullable<T> Foo<T>()
    {
        return null;
    }
} 
";
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true")).VerifyDiagnostics();
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_06()
        {
            var source = @"
class A
{
    public virtual void Foo<T>(T? x) where T : struct 
    { 
    }
}

class B : A
{
    public override void Foo<T>(System.Nullable<T> x)
    {
    }
} 
";
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true")).VerifyDiagnostics();
        }

        [Fact]
        public void InheritedClassConstraintForNullable1_01()
        {
            var source = @"
class A
{
    public virtual T? Foo<T>() where T : class 
    { 
        return null; 
    }
}

class B : A
{
    public override T? Foo<T>()
    {
        return null;
    }
} 
";
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true")).VerifyDiagnostics();
        }

        [Fact(Skip = "Unexpected errors!")]
        public void InheritedClassConstraintForNullable1_02()
        {
            var source = @"
class A
{
    public virtual void Foo<T>(T? x) where T : class 
    { 
    }
}

class B : A
{
    public override void Foo<T>(T? x)
    {
    }
} 
";
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true")).VerifyDiagnostics();
        }

        [Fact(Skip = "")]
        public void Test1()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
        string? x = null;
        var y1 = x.Length; 
        var y2 = x?.Length;
    }
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
                );
        }

        [Fact(Skip = "")]
        public void Test2()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
using nullableString = System.String?;

class C
{
    static void Main()
    {
        nullableString? x = null;
    }
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
                );
        }

    }
}
