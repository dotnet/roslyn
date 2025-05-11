// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
    public class ContainerTests : CSharpTestBase
    {
        [Fact]
        public void SimpleAssembly()
        {
            var text = @"namespace N
{
    class A {}
}
";
            var simpleName = GetUniqueName();
            var comp = CreateCompilation(text, assemblyName: simpleName);
            var sym = comp.Assembly;
            // See bug 2058: the following lines assume System.Reflection.AssemblyName preserves the case of
            // the "displayName" passed to it, but it sometimes does not.
            Assert.Equal(simpleName, sym.Name, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(simpleName + ", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", sym.ToTestDisplayString(), StringComparer.OrdinalIgnoreCase);
            Assert.Equal(String.Empty, sym.GlobalNamespace.Name);
            Assert.Equal(SymbolKind.Assembly, sym.Kind);
            Assert.Equal(Accessibility.NotApplicable, sym.DeclaredAccessibility);
            Assert.False(sym.IsStatic);
            Assert.False(sym.IsVirtual);
            Assert.False(sym.IsOverride);
            Assert.False(sym.IsAbstract);
            Assert.False(sym.IsSealed);
            Assert.False(sym.IsExtern);
            Assert.Null(sym.ContainingAssembly);
            Assert.Null(sym.ContainingSymbol);
        }

        [Theory, MemberData(nameof(FileScopedOrBracedNamespace)), WorkItem(1979, "DevDiv_Projects/Roslyn"), WorkItem(2026, "DevDiv_Projects/Roslyn"), WorkItem(544009, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544009")]
        public void SourceModule(string ob, string cb)
        {
            var text = @"namespace NS.NS1.NS2
" + ob + @"
    class A {}
" + cb + @"
";
            var comp = CreateCompilation(text, assemblyName: "Test");

            var sym = comp.SourceModule;
            Assert.Equal("Test.dll", sym.Name);
            // Bug: 2026
            Assert.Equal("Test.dll", sym.ToDisplayString());
            Assert.Equal(String.Empty, sym.GlobalNamespace.Name);
            Assert.Equal(SymbolKind.NetModule, sym.Kind);
            Assert.Equal(Accessibility.NotApplicable, sym.DeclaredAccessibility);
            Assert.False(sym.IsStatic);
            Assert.False(sym.IsVirtual);
            Assert.False(sym.IsOverride);
            Assert.False(sym.IsAbstract);
            Assert.False(sym.IsSealed);
            Assert.Equal("Test", sym.ContainingAssembly.Name);
            Assert.Equal("Test", sym.ContainingSymbol.Name);

            var ns = comp.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var ns1 = (ns.GetMembers("NS1").Single() as NamespaceSymbol).GetMembers("NS2").Single() as NamespaceSymbol;
            // NamespaceExtent 
            var ext = ns1.Extent;
            Assert.Equal(NamespaceKind.Module, ext.Kind);
            Assert.Equal(1, ns1.ConstituentNamespaces.Length);
            Assert.Same(ns1, ns1.ConstituentNamespaces[0]);

            // Bug: 1979
            Assert.Equal("Module: Test.dll", ext.ToString());
        }

        [Fact]
        public void SimpleNamespace()
        {
            var text = @"namespace N1
{
    namespace N11 {
        namespace N111 {
            class A {}
        }
    }
}

namespace N1 {
    struct S {}
}
";
            var text1 = @"namespace N1
{
    namespace N11 {
        namespace N111 {
            class B {}
        }
    }
}
";
            var text2 = @"namespace N1
    namespace N12 {
         struct S {}
    }
}
";
            var comp1 = CSharpCompilation.Create(assemblyName: "Test", options: TestOptions.DebugExe,
                            syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(text) }, references: new MetadataReference[] { });
            var compRef = new CSharpCompilationReference(comp1);

            var comp = CSharpCompilation.Create(assemblyName: "Test1", options: TestOptions.DebugExe,
                            syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(text1), SyntaxFactory.ParseSyntaxTree(text2) },
                            references: new MetadataReference[] { compRef });

            var global = comp.GlobalNamespace;
            var ns = global.GetMembers("N1").Single() as NamespaceSymbol;
            Assert.Equal(1, ns.GetTypeMembers().Length); // S
            Assert.Equal(3, ns.GetMembers().Length); // N11, N12, S

            var ns1 = (ns.GetMembers("N11").Single() as NamespaceSymbol).GetMembers("N111").Single() as NamespaceSymbol;
            Assert.Equal(2, ns1.GetTypeMembers().Length); // A & B
        }

        [Fact]
        public void UsingAliasForNamespace()
        {
            var text = @"using Gen = System.Collections.Generic;

namespace NS {
    public interface IGoo {}
}

namespace NS.NS1 {
    using F = NS.IGoo;
    class A : F { }
}
";
            var text1 = @"namespace NS.NS1 {
    public class B {
        protected Gen.List<int> field;
    }
}
";
            var text2 = @"namespace NS {
    namespace NS2 {
        using NN = NS.NS1;
        class C : NN.B { }
    }
}
";
            var comp1 = CreateCompilation(text);
            var compRef = new CSharpCompilationReference(comp1);

            var comp = CSharpCompilation.Create(assemblyName: "Test1", options: TestOptions.DebugExe,
                            syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(text1), SyntaxFactory.ParseSyntaxTree(text2) },
                            references: new MetadataReference[] { compRef });

            var global = comp.GlobalNamespace;
            var ns = global.GetMembers("NS").Single() as NamespaceSymbol;
            Assert.Equal(1, ns.GetTypeMembers().Length); // IGoo
            Assert.Equal(3, ns.GetMembers().Length); // NS1, NS2, IGoo

            var ns1 = ns.GetMembers("NS1").Single() as NamespaceSymbol;
            var type1 = ns1.GetTypeMembers("A").SingleOrDefault() as NamedTypeSymbol;
            Assert.Equal(1, type1.Interfaces().Length);
            Assert.Equal("IGoo", type1.Interfaces()[0].Name);

            var ns2 = ns.GetMembers("NS2").Single() as NamespaceSymbol;
            var type2 = ns2.GetTypeMembers("C").SingleOrDefault() as NamedTypeSymbol;
            Assert.NotNull(type2.BaseType());
            Assert.Equal("NS.NS1.B", type2.BaseType().ToTestDisplayString());
        }

        [Theory, MemberData(nameof(FileScopedOrBracedNamespace))]
        public void MultiModulesNamespace(string ob, string cb)
        {
            var text1 = @"namespace N1
" + ob + @"
    class A {}
" + cb + @"
";
            var text2 = @"namespace N1
" + ob + @"
    interface IGoo {}
" + cb + @"
";
            var text3 = @"namespace N1
" + ob + @"
    struct SGoo {}
" + cb + @"
";
            var comp1 = CreateCompilation(text1, assemblyName: "Compilation1");
            var comp2 = CreateCompilation(text2, assemblyName: "Compilation2");

            var compRef1 = new CSharpCompilationReference(comp1);
            var compRef2 = new CSharpCompilationReference(comp2);

            var comp = CreateEmptyCompilation(new string[] { text3 }, references: new MetadataReference[] { compRef1, compRef2 }.ToList(), assemblyName: "Test3");
            //Compilation.Create(outputName: "Test3", options: CompilationOptions.Default,
            //                        syntaxTrees: new SyntaxTree[] { SyntaxTree.ParseCompilationUnit(text3) },
            //                        references: new MetadataReference[] { compRef1, compRef2 });

            var global = comp.GlobalNamespace; // throw
            var ns = global.GetMembers("N1").Single() as NamespaceSymbol;
            Assert.Equal(3, ns.GetTypeMembers().Length); // A, IGoo & SGoo
            Assert.Equal(NamespaceKind.Compilation, ns.Extent.Kind);

            var constituents = ns.ConstituentNamespaces;
            Assert.Equal(3, constituents.Length);
            Assert.True(constituents.Contains(comp.SourceAssembly.GlobalNamespace.GetMembers("N1").Single() as NamespaceSymbol));
            Assert.True(constituents.Contains(comp.GetReferencedAssemblySymbol(compRef1).GlobalNamespace.GetMembers("N1").Single() as NamespaceSymbol));
            Assert.True(constituents.Contains(comp.GetReferencedAssemblySymbol(compRef2).GlobalNamespace.GetMembers("N1").Single() as NamespaceSymbol));

            foreach (var constituentNs in constituents)
            {
                Assert.Equal(NamespaceKind.Module, constituentNs.Extent.Kind);
                Assert.Equal(ns.ToTestDisplayString(), constituentNs.ToTestDisplayString());
            }
        }

        [WorkItem(537287, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537287")]
        [Fact]
        public void MultiModulesNamespaceCorLibraries()
        {
            var text1 = @"namespace N1
{
    class A {}
}
";
            var text2 = @"namespace N1
{
    interface IGoo {}
}
";
            var text3 = @"namespace N1
{
    struct SGoo {}
}
";

            var comp1 = CSharpCompilation.Create(assemblyName: "Test1", options: TestOptions.DebugExe, syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(text1) }, references: new MetadataReference[] { });
            var comp2 = CSharpCompilation.Create(assemblyName: "Test2", options: TestOptions.DebugExe, syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(text2) }, references: new MetadataReference[] { });

            var compRef1 = new CSharpCompilationReference(comp1);
            var compRef2 = new CSharpCompilationReference(comp2);

            var comp = CSharpCompilation.Create(assemblyName: "Test3", options: TestOptions.DebugExe,
                                        syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(text3) },
                                        references: new MetadataReference[] { compRef1, compRef2 });

            var global = comp.GlobalNamespace; // throw
            var ns = global.GetMembers("N1").Single() as NamespaceSymbol;
            Assert.Equal(3, ns.GetTypeMembers().Length); // A, IGoo & SGoo
            Assert.Equal(NamespaceKind.Compilation, ns.Extent.Kind);

            var constituents = ns.ConstituentNamespaces;
            Assert.Equal(3, constituents.Length);
            Assert.True(constituents.Contains(comp.SourceAssembly.GlobalNamespace.GetMembers("N1").Single() as NamespaceSymbol));
            Assert.True(constituents.Contains(comp.GetReferencedAssemblySymbol(compRef1).GlobalNamespace.GetMembers("N1").Single() as NamespaceSymbol));
            Assert.True(constituents.Contains(comp.GetReferencedAssemblySymbol(compRef2).GlobalNamespace.GetMembers("N1").Single() as NamespaceSymbol));
        }

        /// Container with nested types and non-type members with the same name
        [Theory, MemberData(nameof(FileScopedOrBracedNamespace))]
        public void ClassWithNestedTypesAndMembersWithSameName(string ob, string cb)
        {
            var text1 = @"namespace N1
" + ob + @"
    class A 
    {
        class b
        {
        }

        class b<T>
        {
        }

        int b;

        int b() {}

        int b(string s){}
    }
" + cb + @"
";

            var comp = CSharpCompilation.Create(
                assemblyName: "Test1",
                syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(text1) },
                references: new MetadataReference[] { });
            var global = comp.GlobalNamespace; // throw
            var ns = global.GetMembers("N1").Single() as NamespaceSymbol;
            Assert.Equal(1, ns.GetTypeMembers().Length); // A
            var b = ns.GetTypeMembers("A")[0].GetMembers("b");
            Assert.Equal(5, b.Length);
        }

        [WorkItem(537958, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537958")]
        [Fact]
        public void GetDeclaredSymbolDupNsAliasErr()
        {
            var compilation = CreateEmptyCompilation(@"
namespace NS1 {
	class A { }
}	

namespace NS2 {
	class B { }
}

namespace NS
{
	using ns = NS1;
	using ns = NS2;

	class C : ns.A {}
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var globalNS = compilation.SourceModule.GlobalNamespace;
            var ns1 = globalNS.GetMembers("NS").Single() as NamespaceSymbol;
            var type1 = ns1.GetTypeMembers("C").First() as NamedTypeSymbol;
            var b = type1.BaseType();
        }

        [WorkItem(540785, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540785")]
        [Theory, MemberData(nameof(FileScopedOrBracedNamespace))]
        public void GenericNamespace(string ob, string cb)
        {
            var compilation = CreateEmptyCompilation(@"
namespace Goo<T>
" + ob + @"
    class Program    
    {        
        static void Main()
        { 
        }
    }
" + cb + @"
");
            var global = compilation.GlobalNamespace;

            var @namespace = global.GetMember<NamespaceSymbol>("Goo");
            Assert.NotNull(@namespace);

            var @class = @namespace.GetMember<NamedTypeSymbol>("Program");
            Assert.NotNull(@class);

            var method = @class.GetMember<MethodSymbol>("Main");
            Assert.NotNull(method);
        }

        [WorkItem(690871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/690871")]
        [Fact]
        public void SpecialTypesAndAliases()
        {
            var source = @"public class C { }";

            var aliasedCorlib = NetFramework.mscorlib.WithAliases(ImmutableArray.Create("Goo"));

            var comp = CreateEmptyCompilation(source, new[] { aliasedCorlib });

            // NOTE: this doesn't compile in dev11 - it reports that it cannot find System.Object.
            // However, we've already changed how special type lookup works, so this is not a major issue.
            comp.VerifyDiagnostics();

            var objectType = comp.GetSpecialType(SpecialType.System_Object);
            Assert.Equal(TypeKind.Class, objectType.TypeKind);
            Assert.Equal("System.Object", objectType.ToTestDisplayString());

            Assert.Equal(objectType, comp.Assembly.GetSpecialType(SpecialType.System_Object));
            Assert.Equal(objectType, comp.Assembly.CorLibrary.GetSpecialType(SpecialType.System_Object));
        }

        [WorkItem(690871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/690871")]
        [Fact]
        public void WellKnownTypesAndAliases()
        {
            var lib = @"
namespace System.Threading.Tasks
{
    public class Task
    {
        public int Status;
    }
}
";
            var source = @"
extern alias myTask;
using System.Threading;
using System.Threading.Tasks;
class App
{
    async void AM() { }
}
";

            var libComp = CreateCompilationWithMscorlib461(lib, assemblyName: "lib");
            var libRef = libComp.EmitToImageReference(aliases: ImmutableArray.Create("myTask"));

            var comp = CreateCompilationWithMscorlib461(source, new[] { libRef });

            // NOTE: As in dev11, we don't consider myTask::System.Threading.Tasks.Task to be
            // ambiguous with global::System.Threading.Tasks.Task (prefer global).
            comp.VerifyDiagnostics(
                // (7,16): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async void AM() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "AM"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Threading;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Threading;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Threading.Tasks;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Threading.Tasks;"),
                // (2,1): info CS8020: Unused extern alias.
                // extern alias myTask;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias myTask;"));

            var taskType = comp.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task);
            Assert.Equal(TypeKind.Class, taskType.TypeKind);
            Assert.Equal("System.Threading.Tasks.Task", taskType.ToTestDisplayString());

            // When we look in a single assembly, we don't consider referenced assemblies.
            Assert.Null(comp.Assembly.GetTypeByMetadataName("System.Threading.Tasks.Task"));
            Assert.Equal(taskType, comp.Assembly.CorLibrary.GetTypeByMetadataName("System.Threading.Tasks.Task"));
        }

        [WorkItem(863435, "DevDiv/Personal")]
        [Theory, MemberData(nameof(FileScopedOrBracedNamespace))]
        public void CS1671ERR_BadModifiersOnNamespace01(string ob, string cb)
        {
            var test = @"
public namespace NS // CS1671
" + ob + @"
    class Test
    {
        public static int Main()
        {
            return 1;
        }
    }
" + cb + @"
";
            CreateCompilationWithMscorlib461(test, parseOptions: TestOptions.RegularWithFileScopedNamespaces).VerifyDiagnostics(
                // (2,1): error CS1671: A namespace declaration cannot have modifiers or attributes
                Diagnostic(ErrorCode.ERR_BadModifiersOnNamespace, "public").WithLocation(2, 1));
        }

        [Fact]
        public void CS1671ERR_BadModifiersOnNamespace02()
        {
            var test = @"[System.Obsolete]
namespace N { }
";

            CreateCompilationWithMscorlib461(test).VerifyDiagnostics(
                // (2,1): error CS1671: A namespace declaration cannot have modifiers or attributes
                Diagnostic(ErrorCode.ERR_BadModifiersOnNamespace, "[System.Obsolete]").WithLocation(1, 1));
        }

        [Fact]
        public void NamespaceWithSemicolon1()
        {
            var test =
@"namespace A;";

            CreateCompilationWithMscorlib461(test, parseOptions: TestOptions.RegularWithFileScopedNamespaces).VerifyDiagnostics();
        }

        [Fact]
        public void NamespaceWithSemicolon3()
        {
            var test =
@"namespace A.B;";

            CreateCompilationWithMscorlib461(test, parseOptions: TestOptions.RegularWithFileScopedNamespaces).VerifyDiagnostics();
        }

        [Fact]
        public void MultipleFileScopedNamespaces()
        {
            var test =
@"namespace A;
namespace B;";

            CreateCompilationWithMscorlib461(test, parseOptions: TestOptions.RegularWithFileScopedNamespaces).VerifyDiagnostics(
                // (2,11): error CS8907: Source file can only contain one file-scoped namespace declaration.
                // namespace B;
                Diagnostic(ErrorCode.ERR_MultipleFileScopedNamespace, "B").WithLocation(2, 11));
        }

        [Fact]
        public void FileScopedNamespaceNestedInNormalNamespace()
        {
            var test =
@"namespace A
{
    namespace B;
}";

            CreateCompilationWithMscorlib461(test, parseOptions: TestOptions.RegularWithFileScopedNamespaces).VerifyDiagnostics(
                // (3,15): error CS8908: Source file can not contain both file-scoped and normal namespace declarations.
                //     namespace B;
                Diagnostic(ErrorCode.ERR_FileScopedAndNormalNamespace, "B").WithLocation(3, 15));
        }

        [Fact]
        public void NormalAndFileScopedNamespace1()
        {
            var test =
@"namespace A;
namespace B
{
}";

            CreateCompilationWithMscorlib461(test, parseOptions: TestOptions.RegularWithFileScopedNamespaces).VerifyDiagnostics(
                // (2,11): error CS8908: error CS8908: Source file can not contain both file-scoped and normal namespace declarations.
                // namespace B
                Diagnostic(ErrorCode.ERR_FileScopedAndNormalNamespace, "B").WithLocation(2, 11));
        }

        [Fact]
        public void NormalAndFileScopedNamespace2()
        {
            var test =
@"namespace A
{
}
namespace B;";

            CreateCompilationWithMscorlib461(test, parseOptions: TestOptions.RegularWithFileScopedNamespaces).VerifyDiagnostics(
                // (4,11): error CS8909: File-scoped namespace must precede all other members in a file.
                // namespace B;
                Diagnostic(ErrorCode.ERR_FileScopedNamespaceNotBeforeAllMembers, "B").WithLocation(4, 11));
        }

        [Fact]
        public void NamespaceWithPrecedingUsing()
        {
            var test =
@"using System;
namespace A;";

            CreateCompilationWithMscorlib461(test, parseOptions: TestOptions.RegularWithFileScopedNamespaces).VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(1, 1));
        }

        [Fact]
        public void NamespaceWithFollowingUsing()
        {
            var test =
@"namespace X;
using System;";

            CreateCompilationWithMscorlib461(test, parseOptions: TestOptions.RegularWithFileScopedNamespaces).VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(2, 1));
        }

        [Fact]
        public void NamespaceWithPrecedingType()
        {
            var test =
@"class X { }
namespace System;";

            CreateCompilationWithMscorlib461(test, parseOptions: TestOptions.RegularWithFileScopedNamespaces).VerifyDiagnostics(
                // (2,11): error CS8909: File-scoped namespace must precede all other members in a file.
                // namespace System;
                Diagnostic(ErrorCode.ERR_FileScopedNamespaceNotBeforeAllMembers, "System").WithLocation(2, 11));
        }

        [Fact]
        public void NamespaceWithFollowingType()
        {
            var test =
@"namespace System;
class X { }";

            CreateCompilationWithMscorlib461(test, parseOptions: TestOptions.RegularWithFileScopedNamespaces).VerifyDiagnostics();
        }

        [Fact]
        public void FileScopedNamespaceWithPrecedingStatement()
        {
            var test =
@"
System.Console.WriteLine();
namespace B;";

            CreateCompilationWithMscorlib461(test, parseOptions: TestOptions.RegularWithFileScopedNamespaces).VerifyDiagnostics(
                // (3,11): error CS8914: File-scoped namespace must precede all other members in a file.
                // namespace B;
                Diagnostic(ErrorCode.ERR_FileScopedNamespaceNotBeforeAllMembers, "B").WithLocation(3, 11));
        }

        [Fact]
        public void FileScopedNamespaceWithFollowingStatement()
        {
            var test =
@"
namespace B;
System.Console.WriteLine();";

            CreateCompilationWithMscorlib461(test, parseOptions: TestOptions.RegularWithFileScopedNamespaces).VerifyDiagnostics(
                    // (3,16): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // System.Console.WriteLine();
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "WriteLine").WithLocation(3, 16),
                    // (3,26): error CS8124: Tuple must contain at least two elements.
                    // System.Console.WriteLine();
                    Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(3, 26),
                    // (3,27): error CS1022: Type or namespace definition, or end-of-file expected
                    // System.Console.WriteLine();
                    Diagnostic(ErrorCode.ERR_EOFExpected, ";").WithLocation(3, 27));
        }

        [Fact]
        public void FileScopedNamespaceUsingsBeforeAndAfter()
        {
            var source1 = @"
namespace A
{
    class C1 { }
}

namespace B
{
    class C2 { }
}
";
            var source2 = @"
using A;
namespace X;
using B;

class C
{
    void M()
    {
        new C1();
        new C2();
    }
}
";

            CreateCompilationWithMscorlib461(new[] { source1, source2 }, parseOptions: TestOptions.RegularWithFileScopedNamespaces).VerifyDiagnostics();
        }

        [Fact]
        public void FileScopedNamespaceFollowedByVariable()
        {
            var test = @"
namespace B;
int x; // 1
";

            CreateCompilationWithMscorlib461(test, parseOptions: TestOptions.RegularWithFileScopedNamespaces).VerifyDiagnostics(
                // (3,5): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // int x; // 1
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "x").WithLocation(3, 5));
        }

        [Fact, WorkItem(54836, "https://github.com/dotnet/roslyn/issues/54836")]
        public void AssemblyRetargetableAttributeIsRespected()
        {
            var code = @"
using System.Reflection;
[assembly: AssemblyFlags(AssemblyNameFlags.Retargetable)]";

            var comp = CreateCompilation(code);
            Assert.True(comp.Assembly.Identity.IsRetargetable);
            comp.VerifyEmitDiagnostics();
        }
    }
}
