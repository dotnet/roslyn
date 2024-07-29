// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Test.Utilities;
using System;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class SymbolExtensionTests : CSharpTestBase
    {
        [Fact]
        public void HasNameQualifier()
        {
            var source =
@"class C { }
namespace N
{
    class C { }
    namespace NA
    {
        class C { }
        namespace NB
        {
            class C { }
        }
    }
}
namespace NA
{
    class C { }
    namespace NA
    {
        class C { }
    }
    namespace NB
    {
        class C { }
    }
}
namespace NB
{
    class C { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();
            var namespaceNames = new[]
            {
                "",
                ".",
                "N",
                "NA",
                "NB",
                "n",
                "AN",
                "NAB",
                "N.",
                ".NA",
                ".NB",
                "N.N",
                "N.NA",
                "N.NB",
                "N..NB",
                "N.NA.NA",
                "N.NA.NB",
                "NA.N",
                "NA.NA",
                "NA.NB",
                "NA.NA.NB",
                "NA.NB.NB",
            };
            HasNameQualifierCore(namespaceNames, compilation.GetMember<NamedTypeSymbol>("C"), "");
            HasNameQualifierCore(namespaceNames, compilation.GetMember<NamedTypeSymbol>("N.C"), "N");
            HasNameQualifierCore(namespaceNames, compilation.GetMember<NamedTypeSymbol>("N.NA.C"), "N.NA");
            HasNameQualifierCore(namespaceNames, compilation.GetMember<NamedTypeSymbol>("N.NA.NB.C"), "N.NA.NB");
            HasNameQualifierCore(namespaceNames, compilation.GetMember<NamedTypeSymbol>("NA.C"), "NA");
            HasNameQualifierCore(namespaceNames, compilation.GetMember<NamedTypeSymbol>("NA.NA.C"), "NA.NA");
            HasNameQualifierCore(namespaceNames, compilation.GetMember<NamedTypeSymbol>("NA.NB.C"), "NA.NB");
            HasNameQualifierCore(namespaceNames, compilation.GetMember<NamedTypeSymbol>("NB.C"), "NB");
        }

        [Fact]
        public void VisitType_AnonymousDelegate()
        {
            var source = @"
void F<T>(T t)
{
    var f = (ref T x) => 0;
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            var tree = compilation.SyntaxTrees.Single();
            var identifier = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().First(id => id.Identifier.Text == "var");
            var model = compilation.GetSemanticModel(tree);
            var anonymousType = model.GetSymbolInfo(identifier).Symbol.GetSymbol<TypeSymbol>();

            Assert.True(anonymousType.ContainsTypeParameter());
        }

        [Fact]
        public void VisitType_AnonymousDelegateWithAnonymousClass()
        {
            var source = @"
void F<T>(T t)
{
    var f = (ref int x) => new { t };
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            var tree = compilation.SyntaxTrees.Single();
            var identifier = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().First(id => id.Identifier.Text == "var");
            var model = compilation.GetSemanticModel(tree);
            var anonymousType = model.GetSymbolInfo(identifier).Symbol.GetSymbol<TypeSymbol>();

            Assert.True(anonymousType.ContainsTypeParameter());
        }

        [Fact]
        public void VisitType_AnonymousClass()
        {
            var source = @"
void F<T>(T t)
{
    var f = new { t };
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            var tree = compilation.SyntaxTrees.Single();
            var identifier = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().First(id => id.Identifier.Text == "var");
            var model = compilation.GetSemanticModel(tree);
            var anonymousType = model.GetSymbolInfo(identifier).Symbol.GetSymbol<TypeSymbol>();

            Assert.True(anonymousType.ContainsTypeParameter());
        }

        [Fact]
        public void VisitType_AnonymousClassWithAnonymousDelegate()
        {
            var source = @"
void F<T>(T t)
{
    var f = (ref int x) => t;
    var g = new { f };
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            var tree = compilation.SyntaxTrees.Single();
            var identifier = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Last(id => id.Identifier.Text == "var");
            var model = compilation.GetSemanticModel(tree);
            var anonymousType = model.GetSymbolInfo(identifier).Symbol.GetSymbol<TypeSymbol>();

            Assert.True(anonymousType.ContainsTypeParameter());
        }

        [Fact]
        public void VisitType_CustomModifiers()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit C1`1<T>
    extends System.Object
{
    // Methods
    .method public hidebysig static 
        string Method () cil managed 
    {
        // Method begins at RVA 0x2050
        // Code size 6 (0x6)
        .maxstack 8

        IL_0000: ldstr ""Method""
        IL_0005: ret
    } // end of method C1`1::Method

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2057
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: ret
    } // end of method C1`1::.ctor

} // end of class C1`1

.class public auto ansi beforefieldinit C2`1<T>
    extends System.Object
{
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2057
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: ret
    } // end of method C2`1::.ctor

} // end of class C2`1

.class public auto ansi beforefieldinit C3`1<T>
    extends class C1`1<int32 modopt(class C2`1<!T>)>
{
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x205f
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void class C1`1<int32>::.ctor()
        IL_0006: ret
    } // end of method C3`1::.ctor

} // end of class C3`1
";

            var source = @"
class Test
{
    static void Main()
    {
        M<int>();
    }

    static void M<G>()
    {
        System.Func<string> x = C3<G>.Method;
        System.Console.WriteLine(x());
    }
}
";
            var compilation = CreateCompilationWithIL(source, ilSource, options: TestOptions.ReleaseExe);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var method = model.GetSymbolInfo(tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Method").Single()).Symbol.GetSymbol<MethodSymbol>();

            AssertEx.Equal("System.String C1<System.Int32 modopt(C2<G>)>.Method()", method.ToTestDisplayString());

            var typeParameters = PooledHashSet<TypeParameterSymbol>.GetInstance();
            try
            {
                method.ContainingType.VisitType(static (typeSymbol, typeParameters, _, _) =>
                {
                    if (typeSymbol is TypeParameterSymbol typeParameter)
                    {
                        typeParameters.Add(typeParameter);
                    }

                    return false;
                },
                typeParameters, visitCustomModifiers: true);

                var typeParameter = typeParameters.Single();
                Assert.Equal("G", typeParameter.Name);
                Assert.Equal("M", typeParameter.ContainingSymbol.Name);
            }
            finally
            {
                typeParameters.Free();
            }
        }

        private void HasNameQualifierCore(string[] namespaceNames, NamedTypeSymbol type, string expectedName)
        {
            Assert.True(Array.IndexOf(namespaceNames, expectedName) >= 0);
            foreach (var namespaceName in namespaceNames)
            {
                Assert.Equal(namespaceName == expectedName, type.HasNameQualifier(namespaceName));
            }
        }
    }
}
