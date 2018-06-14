// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests.Symbols;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class SymbolKeyTests : SymbolKeyTestBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestNamespace()
        {
            const string code = @"
namespace N1
{
    namespace $$N2 { }
}
";

            const string expected = @"(N ""N2"" 0 (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 4) 3) 2) 1) 0)";

            await AssertDeclaredSymbol<NamespaceDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestClass()
        {
            const string code = @"
namespace N1
{
    class $$C1 { }
}
";

            const string expected = @"(D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 4) 3) 2) 1) 0 2 0 ! 0)";

            await AssertDeclaredSymbol<ClassDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestClassTypeParameter()
        {
            const string code = @"
namespace N1
{
    class C1<$$T> { }
}
";

            const string expected = @"(Y ""T"" (D ""C1`1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 1 2 0 ! 1) 0)";

            await AssertDeclaredSymbol<TypeParameterSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestStruct()
        {
            const string code = @"
namespace N1
{
    struct $$S1 { }
}
";

            const string expected = @"(D ""S1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 4) 3) 2) 1) 0 10 0 ! 0)";

            await AssertDeclaredSymbol<StructDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestStructTypeParameter()
        {
            const string code = @"
namespace N1
{
    struct S1<$$T> { }
}
";

            const string expected = @"(Y ""T"" (D ""S1`1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 1 10 0 ! 1) 0)";

            await AssertDeclaredSymbol<TypeParameterSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestInterface()
        {
            const string code = @"
namespace N1
{
    interface $$I1 { }
}
";

            const string expected = @"(D ""I1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 4) 3) 2) 1) 0 7 0 ! 0)";

            await AssertDeclaredSymbol<InterfaceDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestInterfaceTypeParameter()
        {
            const string code = @"
namespace N1
{
    interface I1<$$T> { }
}
";

            const string expected = @"(Y ""T"" (D ""I1`1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 1 7 0 ! 1) 0)";

            await AssertDeclaredSymbol<TypeParameterSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestDelegate()
        {
            const string code = @"
namespace N1
{
    delegate void $$D1();
}
";

            const string expected = @"(D ""D1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 4) 3) 2) 1) 0 3 0 ! 0)";

            await AssertDeclaredSymbol<DelegateDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestDelegateTypeParameter()
        {
            const string code = @"
namespace N1
{
    delegate void D1<$$T>();
}
";

            const string expected = @"(Y ""T"" (D ""D1`1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 1 3 0 ! 1) 0)";

            await AssertDeclaredSymbol<TypeParameterSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestEnum()
        {
            const string code = @"
namespace N1
{
    enum $$E1 { }
}
";

            const string expected = @"(D ""E1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 4) 3) 2) 1) 0 5 0 ! 0)";

            await AssertDeclaredSymbol<EnumDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestEnumMember()
        {
            const string code = @"
namespace N1
{
    enum E1 { One, $$Two, Three }
}
";

            const string expected = @"(F ""Two"" (D ""E1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 5 0 ! 1) 0)";

            await AssertDeclaredSymbol<EnumMemberDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestNamespaceAlias()
        {
            const string code = @"
using $$N2 = N1;

namespace N1 { }
";

            const string expected = @"(A ""N2"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 4) 3) 2) 1) ""TestFile"" 0)";

            await AssertDeclaredSymbol<UsingDirectiveSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestTypeAlias()
        {
            const string code = @"
using $$C2 = N1.C1;

namespace N1
{
    class C1 { }
}
";

            const string expected = @"(A ""C2"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) ""TestFile"" 0)";

            await AssertDeclaredSymbol<UsingDirectiveSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestGenericTypeAlias()
        {
            const string code = @"
using $$C2 = N1.C1<string>;

namespace N1
{
    class C1<T> { }
}
";

            const string expected = @"(A ""C2"" (D ""C1`1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 1 2 0 (% 1 (D ""String"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 2 0 ! 6)) 1) ""TestFile"" 0)";

            await AssertDeclaredSymbol<UsingDirectiveSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestAliasWithErrorTarget()
        {
            const string code = @"
using $$N2 = X.Y.Z;

namespace N1 { }
";

            const string expected = @"(A ""N2"" (E ""Z"" (E ""Y"" (E ""X"" (N """" 0 (U (S ""TestProject"" 6) 5) 4) 0 ! 3) 0 ! 2) 0 ! 1) ""TestFile"" 0)";

            await AssertDeclaredSymbol<UsingDirectiveSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestField()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        int $$_f1;
    }
}
";

            const string expected = @"(F ""_f1"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0)";

            await AssertDeclaredSymbol<VariableDeclaratorSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestProperty()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        int $$P => 42;
    }
}
";

            const string expected = @"(Q ""P"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 (% 0) (% 0) 0)";

            await AssertDeclaredSymbol<PropertyDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestPropertyGetter()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        int P { $$get => 42; }
    }
}
";

            const string expected = @"(M ""get_P"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 0) (% 0) ! 0)";

            await AssertDeclaredSymbol<AccessorDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestPropertySetter()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        int P { get { return 42; } $$set { } }
    }
}
";

            const string expected = @"(M ""set_P"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 1 0) (% 1 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6)) ! 0)";

            await AssertDeclaredSymbol<AccessorDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestIndexer()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        int $$this[int index] => 42;
    }
}
";

            const string expected = @"(Q ""Item"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 1 (% 1 0) (% 1 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6)) 0)";

            await AssertDeclaredSymbol<IndexerDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestIndexerGetter()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        int this[int index] { $$get => 42; }
    }
}
";

            const string expected = @"(M ""get_Item"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 1 0) (% 1 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6)) ! 0)";

            await AssertDeclaredSymbol<AccessorDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestIndexerSetter()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        int this[int index] { get { return 42; } $$set { } }
    }
}
";

            const string expected = @"(M ""set_Item"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 2 0 0) (% 2 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6) (# 6)) ! 0)";

            await AssertDeclaredSymbol<AccessorDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestIndexerWithGenericParameter()
        {
            const string code = @"
namespace N1
{
    class C1<T>
    {
        int $$this[T index] => 42;
    }
}
";

            const string expected = @"(Q ""Item"" (D ""C1`1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 1 2 0 ! 1) 1 (% 1 0) (% 1 (Y ""T"" (# 1) 6)) 0)";

            await AssertDeclaredSymbol<IndexerDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestEventField()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        event System.EventHandler $$E;
    }
}
";

            const string expected = @"(V ""E"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0)";

            await AssertDeclaredSymbol<VariableDeclaratorSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestEvent()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        event System.EventHandler $$E { add { } remove { } };
    }
}
";

            const string expected = @"(V ""E"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0)";

            await AssertDeclaredSymbol<EventDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestEventAdder()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        event System.EventHandler E { $$add { } remove { } };
    }
}
";

            const string expected = @"(M ""add_E"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 1 0) (% 1 (D ""EventHandler"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 3 0 ! 6)) ! 0)";

            await AssertDeclaredSymbol<AccessorDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestEventRemover()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        event System.EventHandler E { add { } $$remove { } };
    }
}
";

            const string expected = @"(M ""remove_E"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 1 0) (% 1 (D ""EventHandler"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 3 0 ! 6)) ! 0)";

            await AssertDeclaredSymbol<AccessorDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestOperator()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        public static bool operator $$==(C1 c, int i) => true;
    }
}
";

            const string expected = @"(M ""op_Equality"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 2 0 0) (% 2 (# 1) (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6)) ! 0)";

            await AssertDeclaredSymbol<OperatorDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestConversionOperator()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        public static implicit operator $$C1(int i) => null;
    }
}
";

            const string expected = @"(M ""op_Implicit"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 1 0) (% 1 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6)) (# 1) 0)";

            await AssertDeclaredSymbol<ConversionOperatorDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestMethod()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        void $$M() { }
    }
}
";

            const string expected = @"(M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 0) (% 0) ! 0)";

            await AssertDeclaredSymbol<MethodDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestMethodWithParameters()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        void $$M(int i, string s, ref bool b, out int j, in int k, params object[] args) { j = 42; }
    }
}
";

            const string expected = @"(M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 6 0 0 1 2 3 0) (% 6 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6) (D ""String"" (# 7) 0 2 0 ! 11) (D ""Boolean"" (# 7) 0 10 0 ! 12) (# 6) (# 6) (R (D ""Object"" (# 7) 0 2 0 ! 14) 1 13)) ! 0)";

            await AssertDeclaredSymbol<MethodDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestGenericMethodWithParameters()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        TResult $$M<T1, TResult>(T1 i, string s, ref bool b, out T1 j, in int k, params object[] args) { j = 42; }
    }
}
";

            // Note: There is a slight difference between this and the previous version.
            // In the previous version, type parameter ordinals were always directly written
            // and could not be referred to by reference.
            const string expected = @"(M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 2 0 (% 6 0 0 1 2 3 0) (% 6 (@ (# 0) 0 6) (D ""String"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 11) 10) 9) 8) 0 2 0 ! 7) (D ""Boolean"" (# 8) 0 10 0 ! 12) (# 6) (D ""Int32"" (# 8) 0 10 0 ! 13) (R (D ""Object"" (# 8) 0 2 0 ! 15) 1 14)) ! 0)";

            await AssertDeclaredSymbol<MethodDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestGenericMethodWithNestedGenericParameter()
        {
            const string code = @"
using System.Collections.Generic;

namespace N1
{
    class C1
    {
        void $$M<T>(Dictionary<int, List<T>> d) { }
    }
}
";

            // Note: There is a slight difference here because the new writer simply uses a symbol reference
            // to refer to the declaring method of a type parameter ordinal.
            const string expected = @"(M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 1 0 (% 1 0) (% 1 (D ""Dictionary`2"" (N ""Generic"" 0 (N ""Collections"" 0 (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 12) 11) 10) 9) 8) 7) 2 2 0 (% 2 (D ""Int32"" (# 9) 0 10 0 ! 13) (D ""List`1"" (# 7) 1 2 0 (% 1 (@ (# 0) 0 15)) 14)) 6)) ! 0)";

            await AssertDeclaredSymbol<MethodDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestUnsafeMethodWithPointerParameters()
        {
            const string code = @"
namespace N1
{
    unsafe class C1
    {
        void $$M(int* i, bool* b) { }
    }
}
";

            const string expected = @"(M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 2 0 0) (% 2 (O (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 11) 10) 9) 8) 0 10 0 ! 7) 6) (O (D ""Boolean"" (# 8) 0 10 0 ! 13) 12)) ! 0)";

            await AssertDeclaredSymbol<MethodDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestMethodWithDynamicParameter()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        void $$M(dynamic d) { }
    }
}
";

            const string expected = @"(M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 1 0) (% 1 (I 6)) ! 0)";

            await AssertDeclaredSymbol<MethodDeclarationSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestReducedExtensionMethod()
        {
            const string code = @"
static class Extensions
{
    public static int Square(this int i) => i * i;
}

class C1
{
    void M()
    {
        int i = 42;
        int squared = i.$$Square();
    }
}
";

            const string expected = @"(X (M ""Square"" (D ""Extensions"" (N """" 0 (U (S ""TestProject"" 5) 4) 3) 0 2 0 ! 2) 0 0 (% 1 0) (% 1 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6)) ! 1) (# 6) 0)";

            await AssertSymbol<InvocationExpressionSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestReducedByRefExtensionMethod()
        {
            const string code = @"
static class Extensions
{
    public static int Square(ref this int i) => i * i;
}

class C1
{
    void M()
    {
        int i = 42;
        i.$$Square();
    }
}
";

            const string expected = @"(X (M ""Square"" (D ""Extensions"" (N """" 0 (U (S ""TestProject"" 5) 4) 3) 0 2 0 ! 2) 0 0 (% 1 1) (% 1 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6)) ! 1) (# 6) 0)";

            await AssertSymbol<InvocationExpressionSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestLocalFunction()
        {
            const string code = @"
class C1
{
    void M()
    {
        void $$Foo() { }
    }
}
";

            const string expected = @"(B ""Foo"" (M ""M"" (D ""C1"" (N """" 0 (U (S ""TestProject"" 5) 4) 3) 0 2 0 ! 2) 0 0 (% 0) (% 0) ! 1) 0 9 0)";

            await AssertDeclaredSymbol<LocalFunctionStatementSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestParameter()
        {
            const string code = @"
namespace N1
    {
        class C1
        {
            void M(int $$i) { }
        }
    }
";

            const string expected = @"(P ""i"" (M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 6) 5) 4) 3) 0 2 0 ! 2) 0 0 (% 1 0) (% 1 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 11) 10) 9) 8) 0 10 0 ! 7)) ! 1) 0)";

            await AssertDeclaredSymbol<ParameterSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestParameterWithGenericType()
        {
            const string code = @"
namespace N1
    {
        class C1
        {
            void M<T>(T $$i) { }
        }
    }
";

            // Note: There is a slight difference here because the new writer simply uses a symbol reference
            // to refer to the declaring method of a type parameter ordinal.
            const string expected = @"(P ""i"" (M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 6) 5) 4) 3) 0 2 0 ! 2) 1 0 (% 1 0) (% 1 (@ (# 1) 0 7)) ! 1) 0)";

            await AssertDeclaredSymbol<ParameterSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestLocalVariable()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        void M()
        {
            int $$i;
        }
    }
}
";

            const string expected = @"(B ""i"" (M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 6) 5) 4) 3) 0 2 0 ! 2) 0 0 (% 0) (% 0) ! 1) 0 8 0)";

            await AssertDeclaredSymbol<VariableDeclaratorSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestLabel()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        void M()
        {
            $$label:
                int i;
        }
    }
}
";

            const string expected = @"(B ""label"" (M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 6) 5) 4) 3) 0 2 0 ! 2) 0 0 (% 0) (% 0) ! 1) 0 7 0)";

            await AssertDeclaredSymbol<LabeledStatementSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestRangeVariable()
        {
            const string code = @"
using System.Linq;

namespace N1
{
    class C1
    {
        void M()
        {
            var q = from $$x in Enumerable.Range(1, 10)
                    select x;
        }
    }
}
";

            const string expected = @"(B ""x"" (M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 6) 5) 4) 3) 0 2 0 ! 2) 0 0 (% 0) (% 0) ! 1) 0 16 0)";

            await AssertDeclaredSymbol<FromClauseSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestTupleType()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        void M()
        {
            $$var t = (1, 2);
        }
    }
}
";

            const string expected = @"(T 0 (D ""ValueTuple`2"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 5) 4) 3) 2) 2 10 0 (% 2 (D ""Int32"" (# 2) 0 10 0 ! 6) (# 6)) 1) (% 2 ! !) (% 2  1 ""TestFile"" 90 1  1 ""TestFile"" 93 1) 0)";

            await AssertSymbol<TypeSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestAnonymousType()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        void M()
        {
            $$var t = new { X = 19, Y = 42 };
        }
    }
}
";

            const string expected = @"(W (% 2 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 5) 4) 3) 2) 0 10 0 ! 1) (# 1)) (% 2 ""X"" ""Y"") (% 2 1 1) (% 2  1 ""TestFile"" 95 1  1 ""TestFile"" 103 1) 0)";

            await AssertSymbol<TypeSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestAnonymousMethod()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        void M()
        {
            Action<string> a = $$delegate(string s) { };
        }
    }
}
";

            const string expected = @"(Z 0  1 ""TestFile"" 100 22 0)";

            await AssertSymbol<AnonymousFunctionExpressionSyntax>(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)]
        public async Task TestLambdaExpression()
        {
            const string code = @"
namespace N1
{
    class C1
    {
        void M()
        {
            Action<string> a = $$s => { };
        }
    }
}
";

            const string expected = @"(Z 0  1 ""TestFile"" 100 8 0)";

            await AssertSymbol<AnonymousFunctionExpressionSyntax>(code, expected);
        }

        protected override string LanguageName => LanguageNames.CSharp;

        protected override ParseOptions SetLanguageVersion(ParseOptions options)
            => ((CSharpParseOptions)options).WithLanguageVersion(LanguageVersion.Latest);
    }
}
