// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ExternAliasTests : CSharpTestBase
    {
        private static MetadataReference s_foo1;
        public static MetadataReference Foo1
        {
            get
            {
                if (s_foo1 == null)
                {
                    var src =
        @"
namespace NS
{
    public class Foo
    {
      public int M() { return 1; }
    }
}
";
                    var comp = CreateCompilationWithMscorlib(src, assemblyName: "Foo1", options: TestOptions.ReleaseDll);
                    s_foo1 = comp.EmitToImageReference(aliases: ImmutableArray.Create("Bar"));
                }

                return s_foo1;
            }
        }

        private static MetadataReference s_foo2;
        public static MetadataReference Foo2
        {
            get
            {
                if (s_foo2 == null)
                {
                    var src =
        @"
namespace NS
{
    public class Foo2
    {
      public int M() { return 2; }
    }
}
";
                    var comp = CreateCompilationWithMscorlib(src, assemblyName: "Foo2", options: TestOptions.ReleaseDll);
                    s_foo2 = comp.EmitToImageReference(aliases: ImmutableArray.Create("Bar"));
                }

                return s_foo2;
            }
        }

        [Fact]
        public void ExternCanBeUsedByUsingAlias()
        {
            var src =
@"
extern alias Bar;
using MClass=Bar::NS.Foo2;

class Maine
{
    public static void Main()
    {
        MClass c = new MClass();
        Bar::NS.Foo d = new Bar::NS.Foo();
    }
}
";
            var comp = CreateCompilationWithMscorlib(src);
            comp = comp.AddReferences(Foo1, Foo2);
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void ExternAliasInScript()
        {
            var src =
@"
extern alias Bar;
Bar::NS.Foo d = new Bar::NS.Foo();
";
            var comp = CreateCompilationWithMscorlib45(src, options: new CSharpCompilationOptions(OutputKind.ConsoleApplication), parseOptions: TestOptions.Script);
            comp = comp.AddReferences(Foo1, Foo2);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ExternAliasInInteractive_Error()
        {
            var src = "extern alias Bar;";

            var comp = CSharpCompilation.CreateScriptCompilation(
                GetUniqueName(),
                syntaxTree: SyntaxFactory.ParseSyntaxTree(src, options: TestOptions.Script),
                references: new MetadataReference[] { MscorlibRef, ExternAliasTests.Foo1, ExternAliasTests.Foo2 });

            comp.VerifyDiagnostics(
                // (1,1): error CS7015: 'extern alias' is not valid in this context
                // extern alias Bar;
                Diagnostic(ErrorCode.ERR_ExternAliasNotAllowed, "extern alias Bar;"));
        }

        [Fact]
        public void ExternInNamespace()
        {
            var src =
            @"
namespace NS
{
    extern alias Bar;

    class C
    {
        public static void Main()
        {
            Bar::NS.Foo d = new Bar::NS.Foo();
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(src);
            comp = comp.AddReferences(Foo1, Foo2);
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void Error_DuplicateAliases()
        {
            var src =
            @"
extern alias Bar;
using Bar = System.Console;

class Maine
{
    public static void Main()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(src);
            comp = comp.AddReferences(Foo1, Foo2);
            comp.VerifyDiagnostics(
                // (3,1): error CS1537: The using alias 'Bar' appeared previously in this namespace
                // using Bar = System.Console;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "using Bar = System.Console;").WithArguments("Bar"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using Bar = System.Console;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Bar = System.Console;"),
                // (2,1): info CS8020: Unused extern alias.
                // extern alias Bar;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias Bar;"));
        }

        [Fact]
        public void Error_BadExternAlias()
        {
            var src =
            @"
extern alias bar;

class Maine
{
    public static void Main()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(src);
            comp = comp.AddReferences(Foo1);
            comp.VerifyDiagnostics(
                // (2,14): error CS0430: The extern alias 'bar' was not specified in a /reference option
                // extern alias bar;
                Diagnostic(ErrorCode.ERR_BadExternAlias, "bar").WithArguments("bar"),
                // (2,1): info CS8020: Unused extern alias.
                // extern alias bar;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias bar;"));
        }

        [Fact]
        public void ExternAliasDoesntFailNonSourceBinds()
        {
            // Ensure that adding an alias doesn't interfere with resolution among metadata references. The alias only affects source usage
            // of the types defined in the aliased assembly.

            var src =
        @"
namespace NS
{
    public class Baz
    {
      public int M() { return 1; }
    }
}
";
            var comp = CreateCompilationWithMscorlib(src, assemblyName: "Baz.dll", options: TestOptions.ReleaseDll);
            var outputMetadata = AssemblyMetadata.CreateFromImage(comp.EmitToArray());
            var foo1 = outputMetadata.GetReference();
            var foo1Alias = outputMetadata.GetReference(aliases: ImmutableArray.Create("Baz"));

            src =
        @"
namespace NS
{
    public class Bar : Baz
    {
      public int M2() { return 2; }
    }
}
";
            comp = CreateCompilationWithMscorlib(src, assemblyName: "Bar.dll", options: TestOptions.ReleaseDll);
            comp = comp.AddReferences(foo1);
            var foo2 = MetadataReference.CreateFromImage(comp.EmitToArray());

            src =
            @"
class Maine
{
    public static void Main()
    {
            NS.Bar d = null;
    }
}
";
            comp = CreateCompilationWithMscorlib(src);
            comp = comp.AddReferences(foo2, foo1Alias);
            comp.VerifyDiagnostics(
                // (6,20): warning CS0219: The variable 'd' is assigned but its value is never used
                //             NS.Bar d = null;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "d").WithArguments("d")
            );
        }

        [Fact]
        public void Error_DontLookInExternAliasWithoutQualifier()
        {
            var src =
            @"
class Maine
{
    public static void Main()
    {
            NS.Foo d = null;    //shouldn't be able to see this type w/o qualification. it is in an extern alias.
    }
}
";
            var comp = CreateCompilationWithMscorlib(src);
            comp = comp.AddReferences(Foo1);
            comp.VerifyDiagnostics(
                // (6,13): error CS0246: The type or namespace name 'NS' could not be found (are you missing a using directive or an assembly reference?)
                //             NS.Foo d = null;    //shouldn't be able to see this type w/o qualification. it is in an extern alias.
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "NS").WithArguments("NS")
                );
        }

        [Fact]
        public void Error_SameExternTwice()
        {
            var src =
            @"
extern alias Bar;
extern alias Bar;

class Maine
{
    public static void Main()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(src);
            comp = comp.AddReferences(Foo1);
            comp.VerifyDiagnostics(
                // (2,14): error CS1537: The using alias 'Bar' appeared previously in this namespace
                // extern alias Bar;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "Bar").WithArguments("Bar"),
                // (2,1): info CS8020: Unused extern alias.
                // extern alias Bar;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias Bar;"),
                // (3,1): info CS8020: Unused extern alias.
                // extern alias Bar;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias Bar;"));
        }

        [Fact]
        public void Error_ExternAliasIdentifierIsGlobalKeyword()
        {
            var src =
        @"
namespace NS
{
    public class Baz
    {
      public int M() { return 1; }
    }
}
";
            var comp = CreateCompilationWithMscorlib(src, options: TestOptions.ReleaseDll);
            var foo1Alias = comp.EmitToImageReference(aliases: ImmutableArray.Create("global"));

            src =
            @"
extern alias global;

class Maine
{
    public static void Main()
    {
    }
}
";
            comp = CreateCompilationWithMscorlib(src);
            comp = comp.AddReferences(foo1Alias);
            comp.VerifyDiagnostics(
                // (2,14): error CS1681: You cannot redefine the global extern alias
                // extern alias global;
                Diagnostic(ErrorCode.ERR_GlobalExternAlias, "global"),
                // (2,1): info CS8020: Unused extern alias.
                // extern alias global;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias global;"));
        }

        [Fact]
        public void GetAliasInfo()
        {
            var text =
@"extern alias Bar;

class A : Bar::NS.Foo {}
";
            var tree = Parse(text);
            var root = tree.GetCompilationUnitRoot() as CompilationUnitSyntax;
            var comp = CreateCompilationWithMscorlib(tree);
            comp = comp.AddReferences(Foo1);

            var model = comp.GetSemanticModel(tree);

            //find the alias qualifier on the base type and get its semantic info.
            var a1 = root.Members[0] as TypeDeclarationSyntax;
            var base1 = a1.BaseList.Types[0].Type as QualifiedNameSyntax;
            var left = base1.Left as AliasQualifiedNameSyntax;
            var qualifier = left.Alias;

            var alias1 = model.GetAliasInfo(qualifier);

            Assert.NotNull(alias1);
            Assert.Equal(SymbolKind.Alias, alias1.Kind);
        }

        [WorkItem(546729, "DevDiv")]
        [Fact]
        public void Crash16681()
        {
            var text =
@"namespace X.Y
{
    extern alias Bar;
    class Program
    {
        public static void Main(string[] args)
        {
            System.Console.WriteLine(12);
        }
    }
}";
            var comp = CreateCompilationWithMscorlib(text).AddReferences(Foo1);
            comp.VerifyDiagnostics(
                // (3,5): info CS8020: Unused extern alias.
                //     extern alias Bar;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias Bar;"));
        }

        [WorkItem(529751, "DevDiv")]
        [Fact]
        public void SameExternAliasInMultipleTreesValid()
        {
            var comp1 = CreateCompilationWithMscorlib("public class C { }", assemblyName: "A1");
            var ref1 = comp1.EmitToImageReference(aliases: ImmutableArray.Create("X"));

            var comp2 = CreateCompilationWithMscorlib("public class D { }", assemblyName: "A2");
            var ref2 = comp2.EmitToImageReference(aliases: ImmutableArray.Create("X"));

            const int numFiles = 20;
            var comp3 = CreateCompilationWithMscorlib(Enumerable.Range(1, numFiles).Select(x => "extern alias X;"), new[] { ref1, ref2 }, assemblyName: "A3.dll");

            var targets = comp3.SyntaxTrees.AsParallel().Select(tree =>
            {
                var model = comp3.GetSemanticModel(tree);

                var aliasSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().Single();

                var aliasSymbol = model.GetDeclaredSymbol(aliasSyntax);
                return (NamespaceSymbol)aliasSymbol.Target;
            }).ToArray(); //force evaluation

            var firstTarget = targets.First();
            Assert.NotNull(firstTarget);
            Assert.IsType<MergedNamespaceSymbol>(firstTarget);
            firstTarget.GetMember<NamedTypeSymbol>("C");
            firstTarget.GetMember<NamedTypeSymbol>("D");

            Assert.True(targets.All(target => ReferenceEquals(firstTarget, target)));
        }

        [WorkItem(529751, "DevDiv")]
        [Fact]
        public void SameExternAliasInMultipleTreesInvalid()
        {
            const int numFiles = 20;
            var comp3 = CreateCompilationWithMscorlib(Enumerable.Range(1, numFiles).Select(x => "extern alias X;"), assemblyName: "A3.dll");

            var targets = comp3.SyntaxTrees.AsParallel().Select(tree =>
            {
                var model = comp3.GetSemanticModel(tree);

                var aliasSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().Single();

                var aliasSymbol = model.GetDeclaredSymbol(aliasSyntax);
                return (NamespaceSymbol)aliasSymbol.Target;
            }).ToArray(); //force evaluation

            var firstTarget = targets.First();
            Assert.NotNull(firstTarget);
            Assert.IsType<MissingNamespaceSymbol>(firstTarget);
            Assert.Equal(0, firstTarget.GetMembers().Length);

            Assert.True(targets.All(target => ReferenceEquals(firstTarget, target)));
        }

        [WorkItem(875899, "DevDiv")]
        [Fact]
        public void SymbolInfoForExternAliasInAliasTarget()
        {
            var libSource = @"
namespace N
{
    public class C { }
}
";

            var source = @"
extern alias A;

using C = A::N.C;

class Test
{
    static void Main()
    {
        C c = new C();
    }
}";
            var libRef = new CSharpCompilationReference(CreateCompilationWithMscorlib(libSource, assemblyName: "lib"), aliases: ImmutableArray.Create("A"));
            var comp = CreateCompilationWithMscorlib(source, new[] { libRef });
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var root = tree.GetRoot();
            var externAliasSyntax = root.DescendantNodes().OfType<ExternAliasDirectiveSyntax>().Single();
            var usingSyntax = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Single();
            var usingTargetSyntax = (QualifiedNameSyntax)usingSyntax.Name;
            var aliasQualifiedNameSyntax = (AliasQualifiedNameSyntax)usingTargetSyntax.Left;

            var aliasedGlobalNamespace = comp.GetReferencedAssemblySymbol(libRef).GlobalNamespace;
            var namespaceN = aliasedGlobalNamespace.GetMember<NamespaceSymbol>("N");
            var typeC = namespaceN.GetMember<NamedTypeSymbol>("C");

            var externAliasSymbol = model.GetDeclaredSymbol(externAliasSyntax);
            Assert.Equal("A", externAliasSymbol.Name);
            Assert.Equal(aliasedGlobalNamespace, externAliasSymbol.Target);

            var usingAliasSymbol = model.GetDeclaredSymbol(usingSyntax);
            Assert.Equal("C", usingAliasSymbol.Name);
            Assert.Equal(typeC, usingAliasSymbol.Target);

            var qualifiedNameInfo = model.GetSymbolInfo(usingTargetSyntax);
            Assert.Equal(typeC, qualifiedNameInfo.Symbol);

            var aliasQualifiedNameInfo = model.GetSymbolInfo(aliasQualifiedNameSyntax);
            Assert.Equal(typeC.ContainingNamespace, aliasQualifiedNameInfo.Symbol);

            var aliasNameInfo = model.GetSymbolInfo(aliasQualifiedNameSyntax.Alias);
            Assert.Equal(aliasedGlobalNamespace, aliasNameInfo.Symbol);
        }
    }
}
