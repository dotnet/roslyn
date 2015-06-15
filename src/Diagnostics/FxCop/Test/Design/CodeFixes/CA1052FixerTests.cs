// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.AnalyzerPowerPack.Design;
using Microsoft.AnalyzerPowerPack.CSharp.Design;
using Xunit;

namespace Microsoft.AnalyzerPowerPack.UnitTests.Design.CodeFixes
{
    public class CA1052FixerTests : CodeFixTestBase
    {
        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            throw new NotImplementedException();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new CA1052DiagnosticAnalyzer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CA1052CSharpCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CA1052DiagnosticAnalyzer();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052FixesSealedClassWithOnlyStaticDeclaredMembersCSharp()
        {
            const string Code = @"
public sealed class C
{
    public static void Foo() { }
}
";

            const string FixedCode = @"
public static class C
{
    public static void Foo() { }
}
";

            VerifyCSharpFix(Code, FixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052FixesNonStaticClassWithOnlyStaticDeclaredMembersCSharp()
        {
            const string Code = @"
public class C
{
    public static void Foo() { }
}
";

            const string FixedCode = @"
public static class C
{
    public static void Foo() { }
}
";

            VerifyCSharpFix(Code, FixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052FixesNonStaticClassWithPublicDefaultConstructorAndStaticMethodCSharp()
        {
            const string Code = @"
public class C
{
    public C() { }
    public static void Foo() { }
}
";

            const string FixedCode = @"
public static class C
{
    public static void Foo() { }
}
";

            VerifyCSharpFix(Code, FixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052FixesNonStaticClassWithProtectedDefaultConstructorAndStaticMethodCSharp()
        {
            const string Code = @"
public class C
{
    protected C() { }
    public static void Foo() { }
}
";

            const string FixedCode = @"
public static class C
{
    public static void Foo() { }
}
";

            VerifyCSharpFix(Code, FixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052FixesNonStaticClassWithPrivateDefaultConstructorAndStaticMethodCSharp()
        {
            const string Code = @"
public class C
{
    private C() { }
    public static void Foo() { }
}
";

            const string FixedCode = @"
public static class C
{
    public static void Foo() { }
}
";

            VerifyCSharpFix(Code, FixedCode);
        }

        [Fact(Skip = "NYI"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052FixesNestedPublicNonStaticClassWithPublicDefaultConstructorAndStaticMethodCSharp()
        {
            const string Code = @"
public class C
{
    public void Moo() { }

    public class CInner
    {
        public CInner() { }
        public static void Foo() { }
    }
}
";

            const string FixedCode = @"
public class C
{
    public void Moo() { }

    public static class CInner
    {
        public static void Foo() { }
    }
}
";

            VerifyCSharpFix(Code, FixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052FixesNonStaticClassWithStaticConstructorCSharp()
        {
            const string Code = @"
public class C
{
    static C() { }
}
";

            const string FixedCode = @"
public static class C
{
    static C() { }
}
";

            VerifyCSharpFix(Code, FixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052FixesNonStaticClassWithStaticConstructorAndInstanceConstructorCSharp()
        {
            const string Code = @"
public class C
{
    public C() { }
    static C() { }
}
";
            const string FixedCode = @"
public static class C
{
    static C() { }
}
";

            VerifyCSharpFix(Code, FixedCode);
        }

        [Fact(Skip = "NYI"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052FixesNestedPublicClassInOtherwiseEmptyNonStaticClassCSharp()
        {
            const string Code = @"
public class C
{
    public class CInner
    {
    }
}
";

            const string FixedCode = @"
public static class C
{
    public static class CInner
    {
    }
}
";

            VerifyCSharpFix(Code, FixedCode);
        }
    }
}
