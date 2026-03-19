// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        private static MetadataReference s_goo1;
        public static MetadataReference Goo1
        {
            get
            {
                if (s_goo1 == null)
                {
                    var src =
        @"
namespace NS
{
    public class Goo
    {
      public int M() { return 1; }
    }
}
";
                    var comp = CreateCompilation(src, assemblyName: "Goo1", options: TestOptions.ReleaseDll);
                    s_goo1 = comp.EmitToImageReference(aliases: ImmutableArray.Create("Bar"));
                }

                return s_goo1;
            }
        }

        private static MetadataReference s_goo2;
        public static MetadataReference Goo2
        {
            get
            {
                if (s_goo2 == null)
                {
                    var src =
        @"
namespace NS
{
    public class Goo2
    {
      public int M() { return 2; }
    }
}
";
                    var comp = CreateCompilation(src, assemblyName: "Goo2", options: TestOptions.ReleaseDll);
                    s_goo2 = comp.EmitToImageReference(aliases: ImmutableArray.Create("Bar"));
                }

                return s_goo2;
            }
        }

        [Fact]
        public void ExternCanBeUsedByUsingAlias()
        {
            var src =
@"
extern alias Bar;
using MClass=Bar::NS.Goo2;

class Maine
{
    public static void Main()
    {
        MClass c = new MClass();
        Bar::NS.Goo d = new Bar::NS.Goo();
    }
}
";
            var comp = CreateCompilation(src);
            comp = comp.AddReferences(Goo1, Goo2);
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void ExternAliasInScript()
        {
            var src =
@"
extern alias Bar;
Bar::NS.Goo d = new Bar::NS.Goo();
";
            var comp = CreateCompilationWithMscorlib461(src, options: TestOptions.DebugExe, parseOptions: TestOptions.Script);
            comp = comp.AddReferences(Goo1, Goo2);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ExternAliasInInteractive_Error()
        {
            var src = "extern alias Bar;";

            var comp = CSharpCompilation.CreateScriptCompilation(
                GetUniqueName(),
                syntaxTree: SyntaxFactory.ParseSyntaxTree(src, options: TestOptions.Script),
                references: new MetadataReference[] { MscorlibRef, ExternAliasTests.Goo1, ExternAliasTests.Goo2 });

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
            Bar::NS.Goo d = new Bar::NS.Goo();
        }
    }
}
";
            var comp = CreateCompilation(src);
            comp = comp.AddReferences(Goo1, Goo2);
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
            var comp = CreateCompilation(src);
            comp = comp.AddReferences(Goo1, Goo2);
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
            var comp = CreateCompilation(src);
            comp = comp.AddReferences(Goo1);
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
            var comp = CreateCompilation(src, assemblyName: "Baz.dll", options: TestOptions.ReleaseDll);
            var outputMetadata = AssemblyMetadata.CreateFromImage(comp.EmitToArray());
            var goo1 = outputMetadata.GetReference();
            var goo1Alias = outputMetadata.GetReference(aliases: ImmutableArray.Create("Baz"));

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
            comp = CreateCompilation(src, assemblyName: "Bar.dll", options: TestOptions.ReleaseDll);
            comp = comp.AddReferences(goo1);
            var goo2 = MetadataReference.CreateFromImage(comp.EmitToArray());

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
            comp = CreateCompilation(src);
            comp = comp.AddReferences(goo2, goo1Alias);
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
            NS.Goo d = null;    //shouldn't be able to see this type w/o qualification. it is in an extern alias.
    }
}
";
            var comp = CreateCompilation(src);
            comp = comp.AddReferences(Goo1);
            comp.VerifyDiagnostics(
                // (6,13): error CS0246: The type or namespace name 'NS' could not be found (are you missing a using directive or an assembly reference?)
                //             NS.Goo d = null;    //shouldn't be able to see this type w/o qualification. it is in an extern alias.
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
            var comp = CreateCompilation(src);
            comp = comp.AddReferences(Goo1);
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
            var comp = CreateCompilation(src, options: TestOptions.ReleaseDll);
            var goo1Alias = comp.EmitToImageReference(aliases: ImmutableArray.Create("global"));

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
            comp = CreateCompilation(src);
            comp = comp.AddReferences(goo1Alias);
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

class A : Bar::NS.Goo {}
";
            var tree = Parse(text);
            var root = tree.GetCompilationUnitRoot() as CompilationUnitSyntax;
            var comp = CreateCompilation(tree);
            comp = comp.AddReferences(Goo1);

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

        [WorkItem(546729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546729")]
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
            var comp = CreateCompilation(text).AddReferences(Goo1);
            comp.VerifyDiagnostics(
                // (3,5): info CS8020: Unused extern alias.
                //     extern alias Bar;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias Bar;"));
        }

        [WorkItem(529751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529751")]
        [Fact]
        public void SameExternAliasInMultipleTreesValid()
        {
            var comp1 = CreateCompilation("public class C { }", assemblyName: "A1");
            var ref1 = comp1.EmitToImageReference(aliases: ImmutableArray.Create("X"));

            var comp2 = CreateCompilation("public class D { }", assemblyName: "A2");
            var ref2 = comp2.EmitToImageReference(aliases: ImmutableArray.Create("X"));

            const int numFiles = 20;
            var comp3 = CreateCompilation(Enumerable.Range(1, numFiles).Select(x => "extern alias X;").ToArray(), new[] { ref1, ref2 }, assemblyName: "A3.dll");

            var targets = comp3.SyntaxTrees.AsParallel().Select(tree =>
            {
                var model = comp3.GetSemanticModel(tree);

                var aliasSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().Single();

                var aliasSymbol = model.GetDeclaredSymbol(aliasSyntax);
                return (INamespaceSymbol)aliasSymbol.Target;
            }).ToArray(); //force evaluation

            var firstTarget = targets.First();
            Assert.NotNull(firstTarget);
            Assert.IsType<MergedNamespaceSymbol>(firstTarget.GetSymbol());
            firstTarget.GetMember<INamedTypeSymbol>("C");
            firstTarget.GetMember<INamedTypeSymbol>("D");

            Assert.True(targets.All(target => ReferenceEquals(firstTarget, target)));
        }

        [WorkItem(529751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529751")]
        [Fact]
        public void SameExternAliasInMultipleTreesInvalid()
        {
            const int numFiles = 20;
            var comp3 = CreateCompilation(Enumerable.Range(1, numFiles).Select(x => "extern alias X;").ToArray(), assemblyName: "A3.dll");

            var targets = comp3.SyntaxTrees.AsParallel().Select(tree =>
            {
                var model = comp3.GetSemanticModel(tree);

                var aliasSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().Single();

                var aliasSymbol = model.GetDeclaredSymbol(aliasSyntax);
                return (INamespaceSymbol)aliasSymbol.Target;
            }).ToArray(); //force evaluation

            var firstTarget = targets.First();
            Assert.NotNull(firstTarget);
            Assert.IsType<MissingNamespaceSymbol>(firstTarget.GetSymbol());
            Assert.Empty(firstTarget.GetMembers());

            Assert.True(targets.All(target => ReferenceEquals(firstTarget, target)));
        }

        [WorkItem(875899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/875899")]
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
            var libRef = new CSharpCompilationReference(CreateCompilation(libSource, assemblyName: "lib"), aliases: ImmutableArray.Create("A"));
            var comp = (Compilation)CreateCompilation(source, new[] { libRef });
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var root = tree.GetRoot();
            var externAliasSyntax = root.DescendantNodes().OfType<ExternAliasDirectiveSyntax>().Single();
            var usingSyntax = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Single();
            var usingTargetSyntax = (QualifiedNameSyntax)usingSyntax.Name;
            var aliasQualifiedNameSyntax = (AliasQualifiedNameSyntax)usingTargetSyntax.Left;

            var aliasedGlobalNamespace = ((IAssemblySymbol)comp.GetAssemblyOrModuleSymbol(libRef)).GlobalNamespace;
            var namespaceN = aliasedGlobalNamespace.GetMember<INamespaceSymbol>("N");
            var typeC = namespaceN.GetMember<INamedTypeSymbol>("C");

            var externAliasSymbol = model.GetDeclaredSymbol(externAliasSyntax);
            Assert.Equal("A", externAliasSymbol.Name);
            Assert.Equal(aliasedGlobalNamespace, externAliasSymbol.Target);
            Assert.Equal(1, externAliasSymbol.DeclaringSyntaxReferences.Length);
            Assert.Same(externAliasSyntax, externAliasSymbol.DeclaringSyntaxReferences.Single().GetSyntax());

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
