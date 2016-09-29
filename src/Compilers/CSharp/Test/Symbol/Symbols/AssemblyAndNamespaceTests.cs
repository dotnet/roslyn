// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var comp = CreateCompilationWithMscorlib(text, assemblyName: simpleName);
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

        [Fact, WorkItem(1979, "DevDiv_Projects/Roslyn"), WorkItem(2026, "DevDiv_Projects/Roslyn"), WorkItem(544009, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544009")]
        public void SourceModule()
        {
            var text = @"namespace NS.NS1.NS2
{
    class A {}
}
";
            var comp = CreateCompilationWithMscorlib(text, assemblyName: "Test");

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
            var comp1 = CSharpCompilation.Create(assemblyName: "Test", options: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
                            syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(text) }, references: new MetadataReference[] { });
            var compRef = new CSharpCompilationReference(comp1);

            var comp = CSharpCompilation.Create(assemblyName: "Test1", options: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
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
    public interface IFoo {}
}

namespace NS.NS1 {
    using F = NS.IFoo;
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
            var comp1 = CreateCompilationWithMscorlib(text);
            var compRef = new CSharpCompilationReference(comp1);

            var comp = CSharpCompilation.Create(assemblyName: "Test1", options: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
                            syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(text1), SyntaxFactory.ParseSyntaxTree(text2) },
                            references: new MetadataReference[] { compRef });

            var global = comp.GlobalNamespace;
            var ns = global.GetMembers("NS").Single() as NamespaceSymbol;
            Assert.Equal(1, ns.GetTypeMembers().Length); // IFoo
            Assert.Equal(3, ns.GetMembers().Length); // NS1, NS2, IFoo

            var ns1 = ns.GetMembers("NS1").Single() as NamespaceSymbol;
            var type1 = ns1.GetTypeMembers("A").SingleOrDefault() as NamedTypeSymbol;
            Assert.Equal(1, type1.Interfaces.Length);
            Assert.Equal("IFoo", type1.Interfaces[0].Name);

            var ns2 = ns.GetMembers("NS2").Single() as NamespaceSymbol;
            var type2 = ns2.GetTypeMembers("C").SingleOrDefault() as NamedTypeSymbol;
            Assert.NotNull(type2.BaseType);
            Assert.Equal("NS.NS1.B", type2.BaseType.ToTestDisplayString());
        }

        [Fact]
        public void MultiModulesNamespace()
        {
            var text1 = @"namespace N1
{
    class A {}
}
";
            var text2 = @"namespace N1
{
    interface IFoo {}
}
";
            var text3 = @"namespace N1
{
    struct SFoo {}
}
";
            var comp1 = CreateCompilationWithMscorlib(text1, assemblyName: "Compilation1");
            var comp2 = CreateCompilationWithMscorlib(text2, assemblyName: "Compilation2");

            var compRef1 = new CSharpCompilationReference(comp1);
            var compRef2 = new CSharpCompilationReference(comp2);

            var comp = CreateCompilation(new string[] { text3 }, references: new MetadataReference[] { compRef1, compRef2 }.ToList(), assemblyName: "Test3");
            //Compilation.Create(outputName: "Test3", options: CompilationOptions.Default,
            //                        syntaxTrees: new SyntaxTree[] { SyntaxTree.ParseCompilationUnit(text3) },
            //                        references: new MetadataReference[] { compRef1, compRef2 });

            var global = comp.GlobalNamespace; // throw
            var ns = global.GetMembers("N1").Single() as NamespaceSymbol;
            Assert.Equal(3, ns.GetTypeMembers().Length); // A, IFoo & SFoo
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
    interface IFoo {}
}
";
            var text3 = @"namespace N1
{
    struct SFoo {}
}
";

            var comp1 = CSharpCompilation.Create(assemblyName: "Test1", options: new CSharpCompilationOptions(OutputKind.ConsoleApplication), syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(text1) }, references: new MetadataReference[] { });
            var comp2 = CSharpCompilation.Create(assemblyName: "Test2", options: new CSharpCompilationOptions(OutputKind.ConsoleApplication), syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(text2) }, references: new MetadataReference[] { });

            var compRef1 = new CSharpCompilationReference(comp1);
            var compRef2 = new CSharpCompilationReference(comp2);

            var comp = CSharpCompilation.Create(assemblyName: "Test3", options: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
                                        syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(text3) },
                                        references: new MetadataReference[] { compRef1, compRef2 });

            var global = comp.GlobalNamespace; // throw
            var ns = global.GetMembers("N1").Single() as NamespaceSymbol;
            Assert.Equal(3, ns.GetTypeMembers().Length); // A, IFoo & SFoo
            Assert.Equal(NamespaceKind.Compilation, ns.Extent.Kind);

            var constituents = ns.ConstituentNamespaces;
            Assert.Equal(3, constituents.Length);
            Assert.True(constituents.Contains(comp.SourceAssembly.GlobalNamespace.GetMembers("N1").Single() as NamespaceSymbol));
            Assert.True(constituents.Contains(comp.GetReferencedAssemblySymbol(compRef1).GlobalNamespace.GetMembers("N1").Single() as NamespaceSymbol));
            Assert.True(constituents.Contains(comp.GetReferencedAssemblySymbol(compRef2).GlobalNamespace.GetMembers("N1").Single() as NamespaceSymbol));
        }

        /// Container with nested types and non-type members with the same name
        [Fact]
        public void ClassWithNestedTypesAndMembersWithSameName()
        {
            var text1 = @"namespace N1
{
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
}
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
            var compilation = CreateCompilation(@"
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
            var b = type1.BaseType;
        }

        [WorkItem(540785, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540785")]
        [Fact]
        public void GenericNamespace()
        {
            var compilation = CreateCompilation(@"
namespace Foo<T>
{
    class Program    
    {        
        static void Main()
        { 
        }
    }
}
");
            var global = compilation.GlobalNamespace;

            var @namespace = global.GetMember<NamespaceSymbol>("Foo");
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

            var aliasedCorlib = TestReferences.NetFx.v4_0_30319.mscorlib.WithAliases(ImmutableArray.Create("Foo"));

            var comp = CreateCompilation(source, new[] { aliasedCorlib });

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

            var libComp = CreateCompilationWithMscorlib45(lib, assemblyName: "lib");
            var libRef = libComp.EmitToImageReference(aliases: ImmutableArray.Create("myTask"));

            var comp = CreateCompilationWithMscorlib45(source, new[] { libRef });

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
            Assert.Null(comp.Assembly.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task));
            Assert.Null(comp.Assembly.GetTypeByMetadataName("System.Threading.Tasks.Task"));
            Assert.Equal(taskType, comp.Assembly.CorLibrary.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task));
            Assert.Equal(taskType, comp.Assembly.CorLibrary.GetTypeByMetadataName("System.Threading.Tasks.Task"));
        }
    }
}
