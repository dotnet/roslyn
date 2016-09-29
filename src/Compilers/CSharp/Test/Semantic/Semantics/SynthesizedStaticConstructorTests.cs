// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SynthesizedStaticConstructorTests : CompilingTestBase
    {
        [Fact]
        public void NoStaticMembers()
        {
            var source = @"
class C
{
    int i1;
}";

            var typeSymbol = CompileAndExtractTypeSymbol(source);
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol));
            Assert.True(IsBeforeFieldInit(typeSymbol));
        }

        [Fact]
        public void NoStaticFields()
        {
            var source = @"
class C
{
    int i1;

    static void Foo() { }
}";

            var typeSymbol = CompileAndExtractTypeSymbol(source);
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol));
            Assert.True(IsBeforeFieldInit(typeSymbol));
        }

        [Fact]
        public void NoStaticInitializers()
        {
            var source = @"
class C
{
    int i1;
    static int s1;

    static void Foo() { }
}";

            var typeSymbol = CompileAndExtractTypeSymbol(source);
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol));
            Assert.True(IsBeforeFieldInit(typeSymbol));
        }

        [Fact]
        public void StaticInitializers()
        {
            var source = @"
class C
{
    int i1;
    static int s1 = 1;

    static void Foo() { }
}";

            var typeSymbol = CompileAndExtractTypeSymbol(source);
            Assert.True(HasSynthesizedStaticConstructor(typeSymbol));
            Assert.True(IsBeforeFieldInit(typeSymbol));
        }

        [Fact]
        public void ConstantInitializers()
        {
            var source = @"
class C
{
    int i1;
    const int s1 = 1;

    static void Foo() { }
}";

            var typeSymbol = CompileAndExtractTypeSymbol(source);
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol));
            Assert.True(IsBeforeFieldInit(typeSymbol));
        }

        [Fact]
        public void SourceStaticConstructorNoStaticMembers()
        {
            var source = @"
class C
{
    static C() { }

    int i1;
}";

            var typeSymbol = CompileAndExtractTypeSymbol(source);
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol));
            Assert.False(IsBeforeFieldInit(typeSymbol));
        }

        [Fact]
        public void SourceStaticConstructorNoStaticFields()
        {
            var source = @"
class C
{
    static C() { }

    int i1;

    static void Foo() { }
}";

            var typeSymbol = CompileAndExtractTypeSymbol(source);
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol));
            Assert.False(IsBeforeFieldInit(typeSymbol));
        }

        [Fact]
        public void SourceStaticConstructorNoStaticInitializers()
        {
            var source = @"
class C
{
    static C() { }

    int i1;
    static int s1;

    static void Foo() { }
}";

            var typeSymbol = CompileAndExtractTypeSymbol(source);
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol));
            Assert.False(IsBeforeFieldInit(typeSymbol));
        }

        [Fact]
        public void SourceStaticConstructorStaticInitializers()
        {
            var source = @"
class C
{
    static C() { }

    int i1;
    static int s1 = 1;

    static void Foo() { }
}";

            var typeSymbol = CompileAndExtractTypeSymbol(source);
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol));
            Assert.False(IsBeforeFieldInit(typeSymbol));
        }

        [Fact]
        public void SourceStaticConstructorConstantInitializers()
        {
            var source = @"
class C
{
    static C() { }

    int i1;
    const int s1 = 1;

    static void Foo() { }
}";

            var typeSymbol = CompileAndExtractTypeSymbol(source);
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol));
            Assert.False(IsBeforeFieldInit(typeSymbol));
        }

        [WorkItem(543606, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543606")]
        [Fact]
        public void SourceStaticConstructorConstantInitializersDecimal01()
        {
            var source = @"
class C
{
    const decimal dec1 = 12345;
}";

            var typeSymbol = CompileAndExtractTypeSymbol(source);
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol));
            Assert.True(IsBeforeFieldInit(typeSymbol));
        }

        [WorkItem(543606, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543606")]
        [Fact]
        public void SourceStaticConstructorConstantInitializersDecimal02()
        {
            var source = @"
class C
{
    static C() { }

    const decimal dec1 = 12345;
}";

            var typeSymbol = CompileAndExtractTypeSymbol(source);
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol));
            Assert.False(IsBeforeFieldInit(typeSymbol));
        }

        [WorkItem(543606, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543606")]
        [Fact]
        public void SourceStaticConstructorConstantInitializersDecimal03()
        {
            var source = @"
class C
{
    decimal dec1 = 12345;
}";

            var typeSymbol = CompileAndExtractTypeSymbol(source);
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol));
            Assert.True(IsBeforeFieldInit(typeSymbol));
        }

        [WorkItem(543606, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543606")]
        [Fact]
        public void SourceStaticConstructorConstantInitializersDecimal04()
        {
            var source = @"
class C
{
    static C() { }

    decimal dec1 = 12345;
}";

            var typeSymbol = CompileAndExtractTypeSymbol(source);
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol));
            Assert.False(IsBeforeFieldInit(typeSymbol));
        }

        [WorkItem(543606, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543606")]
        [Fact]
        public void SourceStaticConstructorConstantInitializersDecimal05()
        {
            var source = @"
class C
{
    static int s1 = 1;
    const decimal dec1 = 12345;
}";

            var typeSymbol = CompileAndExtractTypeSymbol(source);
            Assert.True(HasSynthesizedStaticConstructor(typeSymbol));
            Assert.True(IsBeforeFieldInit(typeSymbol));
        }

        private static SourceNamedTypeSymbol CompileAndExtractTypeSymbol(string source)
        {
            var compilation = CreateCompilationWithMscorlib(source);
            var typeSymbol = (SourceNamedTypeSymbol)compilation.GlobalNamespace.GetMembers("C").Single();
            return typeSymbol;
        }

        private static bool HasSynthesizedStaticConstructor(NamedTypeSymbol typeSymbol)
        {
            foreach (var member in typeSymbol.GetMembers(WellKnownMemberNames.StaticConstructorName))
            {
                if (member.IsImplicitlyDeclared)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsBeforeFieldInit(NamedTypeSymbol typeSymbol)
        {
            return ((Microsoft.Cci.ITypeDefinition)typeSymbol).IsBeforeFieldInit;
        }
    }
}
