// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SymbolId;

public sealed partial class SymbolKeyTest : SymbolKeyTestBase
{
    #region "No change to symbol"

    [Fact]
    public void C2CTypeSymbolUnchanged01()
    {
        var src1 = """
            using System;

            public delegate void DGoo(int p1, string p2);

            namespace N1.N2
            {
                public interface IGoo { }
                namespace N3
                {
                    public class CGoo 
                    {
                        public struct SGoo 
                        {
                            public enum EGoo { Zero, One }
                        }
                    }
                }
            }
            """;

        var src2 = """
            using System;

            public delegate void DGoo(int p1, string p2);

            namespace N1.N2
            {
                public interface IGoo 
                {
                    // Add member
                    N3.CGoo GetClass();
                }

                namespace N3
                {
                    public class CGoo 
                    {
                        public struct SGoo 
                        {
                            // Update member
                            public enum EGoo { Zero, One, Two }
                        }
                        // Add member
                        public void M(int n) { Console.WriteLine(n); }
                    }
                }
            }
            """;

        var comp1 = CreateCompilation(src1, assemblyName: "Test");
        var comp2 = CreateCompilation(src2, assemblyName: "Test");

        var originalSymbols = GetSourceSymbols(comp1, SymbolCategory.DeclaredType | SymbolCategory.DeclaredNamespace).OrderBy(s => s.Name);
        var newSymbols = GetSourceSymbols(comp2, SymbolCategory.DeclaredType | SymbolCategory.DeclaredNamespace).OrderBy(s => s.Name);

        ResolveAndVerifySymbolList(newSymbols, originalSymbols, comp1);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530171")]
    public void C2CErrorSymbolUnchanged01()
    {
        var src1 = @"public void Method() { }";

        var src2 = """
            public void Method() 
            { 
                System.Console.WriteLine(12345);
            }
            """;

        var comp1 = CreateCompilation(src1, assemblyName: "C2CErrorSymbolUnchanged01");
        var comp2 = CreateCompilation(src2, assemblyName: "C2CErrorSymbolUnchanged01");

        var symbol01 = comp1.SourceModule.GlobalNamespace.GetMembers().FirstOrDefault() as NamedTypeSymbol;
        var symbol02 = comp1.SourceModule.GlobalNamespace.GetMembers().FirstOrDefault() as NamedTypeSymbol;

        Assert.NotNull(symbol01);
        Assert.NotNull(symbol02);

        Assert.NotEqual(SymbolKind.ErrorType, symbol01.Kind);
        Assert.NotEqual(SymbolKind.ErrorType, symbol02.Kind);

        var originalSymbols = GetSourceSymbols(comp1, SymbolCategory.DeclaredType | SymbolCategory.DeclaredNamespace).OrderBy(s => s.Name);
        var newSymbols = GetSourceSymbols(comp2, SymbolCategory.DeclaredType | SymbolCategory.DeclaredNamespace).OrderBy(s => s.Name);

        ResolveAndVerifySymbolList(newSymbols, originalSymbols, comp1);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/820263")]
    public void PartialDefinitionAndImplementationResolveCorrectly()
    {
        var src = """
            using System;
            namespace NS
            {
                public partial class C1
                {
                    partial void M() { }
                    partial void M();
                }
            }
            """;

        var comp = (Compilation)CreateCompilation(src, assemblyName: "Test");

        var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as INamespaceSymbol;
        var type = ns.GetTypeMembers("C1").FirstOrDefault();
        var definition = type.GetMembers("M").First() as IMethodSymbol;
        var implementation = definition.PartialImplementationPart;

        // Assert that both the definition and implementation resolve back to themselves
        Assert.Equal(definition, ResolveSymbol(definition, comp, SymbolKeyComparison.None));
        Assert.Equal(implementation, ResolveSymbol(implementation, comp, SymbolKeyComparison.None));
    }

    [Fact]
    public void ExtendedPartialDefinitionAndImplementationResolveCorrectly()
    {
        var src = """
            using System;
            namespace NS
            {
                public partial class C1
                {
                    public partial void M() { }
                    public partial void M();
                }
            }
            """;

        var comp = (Compilation)CreateCompilation(src, assemblyName: "Test");

        var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as INamespaceSymbol;
        var type = ns.GetTypeMembers("C1").FirstOrDefault();
        var definition = type.GetMembers("M").First() as IMethodSymbol;
        var implementation = definition.PartialImplementationPart;

        // Assert that both the definition and implementation resolve back to themselves
        Assert.Equal(definition, ResolveSymbol(definition, comp, SymbolKeyComparison.None));
        Assert.Equal(implementation, ResolveSymbol(implementation, comp, SymbolKeyComparison.None));
    }

    [Fact]
    public void ExtendedPartialPropertyDefinitionAndImplementationResolveCorrectly()
    {
        var src = """
            using System;
            namespace NS
            {
                public partial class C1
                {
                    private int x;
                    public partial int Prop { get; set; }
                    public partial int Prop { get => x; set => x = value; }
                }
            }
            """;

        var comp = (Compilation)CreateCompilation(src, assemblyName: "Test");

        var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as INamespaceSymbol;
        var type = ns.GetTypeMembers("C1").FirstOrDefault();
        var definition = type.GetMembers("Prop").First() as IPropertySymbol;
        var implementation = definition.PartialImplementationPart;

        // Assert that both the definition and implementation resolve back to themselves
        Assert.Equal(definition, ResolveSymbol(definition, comp, SymbolKeyComparison.None));
        Assert.Equal(implementation, ResolveSymbol(implementation, comp, SymbolKeyComparison.None));
    }

    [Fact]
    public void ExtendedPartialEventDefinitionAndImplementationResolveCorrectly()
    {
        var src = """
            using System;
            namespace NS
            {
                public partial class C1
                {
                    public partial event System.Action Event;
                    public partial event System.Action Event { add { } remove { } }
                }
            }
            """;

        var comp = (Compilation)CreateCompilation(src, assemblyName: "Test");

        var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as INamespaceSymbol;
        var type = ns.GetTypeMembers("C1").FirstOrDefault();
        var definition = type.GetMembers("Event").First() as IEventSymbol;
        var implementation = definition.PartialImplementationPart;
        Assert.NotNull(implementation);
        Assert.NotEqual(implementation, definition);

        // Assert that both the definition and implementation resolve back to themselves
        Assert.Equal(definition, ResolveSymbol(definition, comp, SymbolKeyComparison.None));
        Assert.Equal(implementation, ResolveSymbol(implementation, comp, SymbolKeyComparison.None));
    }

    [Fact]
    public void ExtendedPartialConstructorDefinitionAndImplementationResolveCorrectly()
    {
        var src = """
            using System;
            namespace NS
            {
                public partial class C1
                {
                    public partial C1();
                    public partial C1() { }
                }
            }
            """;

        var comp = (Compilation)CreateCompilation(src, assemblyName: "Test");

        var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as INamespaceSymbol;
        var type = ns.GetTypeMembers("C1").FirstOrDefault();
        var definition = type.GetMembers(".ctor").First() as IMethodSymbol;
        var implementation = definition.PartialImplementationPart;
        Assert.NotNull(implementation);
        Assert.NotEqual(implementation, definition);

        // Assert that both the definition and implementation resolve back to themselves
        Assert.Equal(definition, ResolveSymbol(definition, comp, SymbolKeyComparison.None));
        Assert.Equal(implementation, ResolveSymbol(implementation, comp, SymbolKeyComparison.None));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916341")]
    public void ExplicitIndexerImplementationResolvesCorrectly()
    {
        var src = """
            interface I
            {
                object this[int index] { get; }
            }
            interface I<T>
            {
                T this[int index] { get; }
            }
            class C<T> : I<T>, I
            {
                object I.this[int index]
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }
                }
                T I<T>.this[int index]
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """;

        var compilation = (Compilation)CreateCompilation(src, assemblyName: "Test");

        var type = compilation.SourceModule.GlobalNamespace.GetTypeMembers("C").Single();
        var indexer1 = type.GetMembers().Where(m => m.MetadataName == "I.Item").Single() as IPropertySymbol;
        var indexer2 = type.GetMembers().Where(m => m.MetadataName == "I<T>.Item").Single() as IPropertySymbol;

        AssertSymbolKeysEqual(indexer1, indexer2, SymbolKeyComparison.None, expectEqual: false);

        Assert.Equal(indexer1, ResolveSymbol(indexer1, compilation, SymbolKeyComparison.None));
        Assert.Equal(indexer2, ResolveSymbol(indexer2, compilation, SymbolKeyComparison.None));
    }

    [Fact]
    public void RecursiveReferenceToConstructedGeneric()
    {
        var src1 =
            """
            using System.Collections.Generic;

            class C
            {
                public void M<Z>(List<Z> list)
                {
                    var v = list.Add(default(Z));
                }
            }
            """;

        var comp1 = CreateCompilation(src1);
        var comp2 = CreateCompilation(src1);

        var symbols1 = GetSourceSymbols(comp1, includeLocal: true).ToList();
        var symbols2 = GetSourceSymbols(comp1, includeLocal: true).ToList();

        // First, make sure that all the symbols in this file resolve properly 
        // to themselves.
        ResolveAndVerifySymbolList(symbols1, symbols2, comp1);

        // Now do this for the members of types we see.  We want this 
        // so we hit things like the members of the constructed type
        // List<Z>
        var members1 = symbols1.OfType<INamespaceOrTypeSymbol>().SelectMany(n => n.GetMembers()).ToList();
        var members2 = symbols2.OfType<INamespaceOrTypeSymbol>().SelectMany(n => n.GetMembers()).ToList();

        ResolveAndVerifySymbolList(members1, members2, comp1);
    }

    [Fact]
    public void FileType1()
    {
        var src1 = """
            using System;

            namespace N1.N2
            {
                file class C { }
            }
            """;
        var originalComp = CreateCompilation(src1, assemblyName: "Test");
        var newComp = CreateCompilation(src1, assemblyName: "Test");

        var originalSymbols = GetSourceSymbols(originalComp, SymbolCategory.DeclaredType | SymbolCategory.DeclaredNamespace).OrderBy(s => s.Name).ToArray();
        var newSymbols = GetSourceSymbols(newComp, SymbolCategory.DeclaredType | SymbolCategory.DeclaredNamespace).OrderBy(s => s.Name).ToArray();

        Assert.Equal(3, originalSymbols.Length);
        ResolveAndVerifySymbolList(newSymbols, originalSymbols, originalComp);
    }

    [Fact]
    public void FileType2()
    {
        var src1 = """
            using System;

            namespace N1.N2
            {
                file class C<T> { }
            }
            """;
        var originalComp = CreateCompilation(src1, assemblyName: "Test");
        var newComp = CreateCompilation(src1, assemblyName: "Test");

        var originalSymbols = GetSourceSymbols(originalComp, SymbolCategory.DeclaredType | SymbolCategory.DeclaredNamespace).OrderBy(s => s.Name).ToArray();
        var newSymbols = GetSourceSymbols(newComp, SymbolCategory.DeclaredType | SymbolCategory.DeclaredNamespace).OrderBy(s => s.Name).ToArray();

        Assert.Equal(3, originalSymbols.Length);
        ResolveAndVerifySymbolList(newSymbols, originalSymbols, originalComp);
    }

    [Fact]
    public void FileType3()
    {
        var src1 = """
            using System;

            namespace N1.N2
            {
                file class C { }
            }
            """;
        // this should result in two entirely separate file symbols.
        // note that the IDE can only distinguish file-local type symbols with the same name when they have distinct file paths.
        // We are OK with this as we will require file types with identical names to have distinct file paths later in the preview.
        // See https://github.com/dotnet/roslyn/issues/61999
        var originalComp = CreateCompilation([SyntaxFactory.ParseSyntaxTree(src1, path: "file1.cs"), SyntaxFactory.ParseSyntaxTree(src1, path: "file2.cs")], assemblyName: "Test");
        var newComp = CreateCompilation([SyntaxFactory.ParseSyntaxTree(src1, path: "file1.cs"), SyntaxFactory.ParseSyntaxTree(src1, path: "file2.cs")], assemblyName: "Test");

        var originalSymbols = GetSourceSymbols(originalComp, SymbolCategory.DeclaredType | SymbolCategory.DeclaredNamespace).OrderBy(s => s.Name).ToArray();
        var newSymbols = GetSourceSymbols(newComp, SymbolCategory.DeclaredType | SymbolCategory.DeclaredNamespace).OrderBy(s => s.Name).ToArray();

        Assert.Equal(4, originalSymbols.Length);
        ResolveAndVerifySymbolList(newSymbols, originalSymbols, originalComp);
    }

    [Fact]
    public void FileType4()
    {
        // we should be able to distinguish a file-local type and non-file-local type when they have the same source name.
        var src1 = SyntaxFactory.ParseSyntaxTree("""
            using System;

            namespace N1.N2
            {
                file class C { }
            }
            """, path: "File1.cs");

        var src2 = SyntaxFactory.ParseSyntaxTree("""
            namespace N1.N2
            {
                class C { }
            }
            """, path: "File2.cs");
        var originalComp = CreateCompilation([src1, src2], assemblyName: "Test");
        var newComp = CreateCompilation([src1, src2], assemblyName: "Test");

        var originalSymbols = GetSourceSymbols(originalComp, SymbolCategory.DeclaredType | SymbolCategory.DeclaredNamespace).OrderBy(s => s.Name).ToArray();
        var newSymbols = GetSourceSymbols(newComp, SymbolCategory.DeclaredType | SymbolCategory.DeclaredNamespace).OrderBy(s => s.Name).ToArray();

        Assert.Equal(4, originalSymbols.Length);
        ResolveAndVerifySymbolList(newSymbols, originalSymbols, originalComp);
    }

    #endregion

    #region "Change to symbol"

    [Fact]
    public void C2CTypeSymbolChanged01()
    {
        var src1 = """
            using System;

            public delegate void DGoo(int p1);

            namespace N1.N2
            {
                public interface IBase { }
                public interface IGoo { }
                namespace N3
                {
                    public class CGoo 
                    {
                        public struct SGoo 
                        {
                            public enum EGoo { Zero, One }
                        }
                    }
                }
            }
            """;

        var src2 = """
            using System;

            public delegate void DGoo(int p1, string p2); // add 1 more parameter

            namespace N1.N2
            {
                public interface IBase { }
                public interface IGoo : IBase // add base interface
                {
                }

                namespace N3
                {
                    public class CGoo : IGoo // impl interface
                    {
                        private struct SGoo // change modifier
                        {
                            internal enum EGoo : long { Zero, One } // change base class, and modifier
                        }
                    }
                }
            }
            """;

        var comp1 = CreateCompilation(src1, assemblyName: "Test");
        var comp2 = CreateCompilation(src2, assemblyName: "Test");

        var originalSymbols = GetSourceSymbols(comp1, SymbolCategory.DeclaredType);
        var newSymbols = GetSourceSymbols(comp2, SymbolCategory.DeclaredType);

        ResolveAndVerifySymbolList(newSymbols, originalSymbols, comp1);
    }

    [Fact]
    public void C2CTypeSymbolChanged02()
    {
        var src1 = """
            using System;
            namespace NS
            {
                public class C1 
                {
                    public void M() {}
                }
            }
            """;

        var src2 = """
            namespace NS
            {
                internal class C1 // add new C1
                {
                    public string P { get; set; }
                }

                public class C2  // rename C1 to C2
                {
                    public void M() {}
                }
            }
            """;
        var comp1 = (Compilation)CreateCompilation(src1, assemblyName: "Test");
        var comp2 = (Compilation)CreateCompilation(src2, assemblyName: "Test");

        var namespace1 = comp1.SourceModule.GlobalNamespace.GetMembers("NS").Single() as INamespaceSymbol;
        var typeSym00 = namespace1.GetTypeMembers("C1").FirstOrDefault();

        var namespace2 = comp2.SourceModule.GlobalNamespace.GetMembers("NS").Single() as INamespaceSymbol;
        var typeSym01 = namespace2.GetTypeMembers("C1").FirstOrDefault();
        var typeSym02 = namespace2.GetTypeMembers("C2").Single();

        // new C1 resolve to old C1
        ResolveAndVerifySymbol(typeSym01, typeSym00, comp1);

        // old C1 (new C2) NOT resolve to old C1
        var symkey = SymbolKey.Create(typeSym02, CancellationToken.None);
        var syminfo = symkey.Resolve(comp1);
        Assert.Null(syminfo.Symbol);
    }

    [Fact]
    public void C2CMemberSymbolChanged01()
    {
        var src1 = """
            using System;
            using System.Collections.Generic;

            public class Test
            {
                private byte field = 123;
                internal string P { get; set; }
                public void M(ref int n) { }
                event Action<string> myEvent;
            }
            """;

        var src2 = """
            using System;
            public class Test
            {
                internal protected byte field = 255;    // change modifier and init-value
                internal string P { get { return null; } }       // remove 'set'
                public int M(ref int n) { return 0;  }   // change ret type
                event Action<string> myEvent             // add add/remove
                {
                    add { }
                    remove { }
                }
            }
            """;
        var comp1 = CreateCompilation(src1, assemblyName: "Test");
        var comp2 = CreateCompilation(src2, assemblyName: "Test");

        var originalSymbols = GetSourceSymbols(comp1, SymbolCategory.NonTypeMember | SymbolCategory.Parameter)
                                  .Where(s => !s.IsAccessor()).OrderBy(s => s.Name);

        var newSymbols = GetSourceSymbols(comp2, SymbolCategory.NonTypeMember | SymbolCategory.Parameter)
                             .Where(s => !s.IsAccessor()).OrderBy(s => s.Name);

        ResolveAndVerifySymbolList(newSymbols, originalSymbols, comp1);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542700")]
    public void C2CIndexerSymbolChanged01()
    {
        var src1 = """
            using System;
            using System.Collections.Generic;

            public class Test
            {
                public string this[string p1] { set { } }
                protected long this[long p1] { set { } }
            }
            """;

        var src2 = """
            using System;
            public class Test
            {
                internal string this[string p1] { set { } } // change modifier
                protected long this[long p1] { get { return 0; } set { } }  // add 'get'
            }
            """;
        var comp1 = (Compilation)CreateCompilation(src1, assemblyName: "Test");
        var comp2 = (Compilation)CreateCompilation(src2, assemblyName: "Test");

        var typeSym1 = comp1.SourceModule.GlobalNamespace.GetTypeMembers("Test").Single();
        var originalSymbols = typeSym1.GetMembers(WellKnownMemberNames.Indexer);

        var typeSym2 = comp2.SourceModule.GlobalNamespace.GetTypeMembers("Test").Single();
        var newSymbols = typeSym2.GetMembers(WellKnownMemberNames.Indexer);

        ResolveAndVerifySymbol(newSymbols.First(), originalSymbols.First(), comp1, SymbolKeyComparison.None);
        ResolveAndVerifySymbol(newSymbols.Last(), originalSymbols.Last(), comp1, SymbolKeyComparison.None);
    }

    [Fact]
    public void C2CAssemblyChanged01()
    {
        var src = """
            namespace NS
            {
                public class C1 
                {
                    public void M() {}
                }
            }
            """;
        var comp1 = (Compilation)CreateCompilation(src, assemblyName: "Assembly1");
        var comp2 = (Compilation)CreateCompilation(src, assemblyName: "Assembly2");

        var namespace1 = comp1.SourceModule.GlobalNamespace.GetMembers("NS").Single() as INamespaceSymbol;
        var typeSym01 = namespace1.GetTypeMembers("C1").FirstOrDefault();

        var namespace2 = comp2.SourceModule.GlobalNamespace.GetMembers("NS").Single() as INamespaceSymbol;
        var typeSym02 = namespace2.GetTypeMembers("C1").FirstOrDefault();

        // new C1 resolves to old C1 if we ignore assembly and module ids
        ResolveAndVerifySymbol(typeSym02, typeSym01, comp1, SymbolKeyComparison.IgnoreAssemblyIds);

        // new C1 DOES NOT resolve to old C1 if we don't ignore assembly and module ids
        Assert.Null(ResolveSymbol(typeSym02, comp1, SymbolKeyComparison.None));
    }

    [WpfFact(Skip = "530169"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530169")]
    public void C2CAssemblyChanged02()
    {
        var src = @"[assembly: System.Reflection.AssemblyVersion(""1.2.3.4"")] public class C {}";

        // same identity
        var comp1 = (Compilation)CreateCompilation(src, assemblyName: "Assembly");
        var comp2 = (Compilation)CreateCompilation(src, assemblyName: "Assembly");

        ISymbol sym1 = comp1.Assembly;
        ISymbol sym2 = comp2.Assembly;

        // Not ignoreAssemblyAndModules
        ResolveAndVerifySymbol(sym1, sym2, comp2);

        AssertSymbolKeysEqual(sym1, sym2, SymbolKeyComparison.IgnoreAssemblyIds, true);
        Assert.NotNull(ResolveSymbol(sym1, comp2, SymbolKeyComparison.IgnoreAssemblyIds));

        // Module
        sym1 = comp1.Assembly.Modules.First();
        sym2 = comp2.Assembly.Modules.First();

        ResolveAndVerifySymbol(sym1, sym2, comp2);

        AssertSymbolKeysEqual(sym2, sym1, SymbolKeyComparison.IgnoreAssemblyIds, true);
        Assert.NotNull(ResolveSymbol(sym2, comp1, SymbolKeyComparison.IgnoreAssemblyIds));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530170")]
    public void C2CAssemblyChanged03()
    {
        var src = @"[assembly: System.Reflection.AssemblyVersion(""1.2.3.4"")] public class C {}";

        // -------------------------------------------------------
        // different name
        var compilation1 = (Compilation)CreateCompilation(src, assemblyName: "Assembly1");
        var compilation2 = (Compilation)CreateCompilation(src, assemblyName: "Assembly2");

        ISymbol assembly1 = compilation1.Assembly;
        ISymbol assembly2 = compilation2.Assembly;

        // different
        AssertSymbolKeysEqual(assembly2, assembly1, SymbolKeyComparison.None, expectEqual: false);
        Assert.Null(ResolveSymbol(assembly2, compilation1, SymbolKeyComparison.None));

        // ignore means ALL assembly/module symbols have same ID
        AssertSymbolKeysEqual(assembly2, assembly1, SymbolKeyComparison.IgnoreAssemblyIds, expectEqual: true);

        // But can NOT be resolved
        Assert.Null(ResolveSymbol(assembly2, compilation1, SymbolKeyComparison.IgnoreAssemblyIds));

        // Module
        var module1 = compilation1.Assembly.Modules.First();
        var module2 = compilation2.Assembly.Modules.First();

        // different
        AssertSymbolKeysEqual(module1, module2, SymbolKeyComparison.None, expectEqual: false);
        Assert.Null(ResolveSymbol(module1, compilation2, SymbolKeyComparison.None));

        AssertSymbolKeysEqual(module2, module1, SymbolKeyComparison.IgnoreAssemblyIds);
        Assert.Null(ResolveSymbol(module2, compilation1, SymbolKeyComparison.IgnoreAssemblyIds));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546254")]
    public void C2CAssemblyChanged04()
    {
        var src = """
            [assembly: System.Reflection.AssemblyVersion("1.2.3.4")] 
            [assembly: System.Reflection.AssemblyTitle("One Hundred Years of Solitude")]
            public class C {}
            """;

        var src2 = """
            [assembly: System.Reflection.AssemblyVersion("1.2.3.42")] 
            [assembly: System.Reflection.AssemblyTitle("One Hundred Years of Solitude")]
            public class C {}
            """;

        // different versions
        var comp1 = (Compilation)CreateCompilation(src, assemblyName: "Assembly");
        var comp2 = (Compilation)CreateCompilation(src2, assemblyName: "Assembly");

        ISymbol sym1 = comp1.Assembly;
        ISymbol sym2 = comp2.Assembly;

        // comment is changed to compare Name ONLY
        AssertSymbolKeysEqual(sym1, sym2, SymbolKeyComparison.None, expectEqual: true);
        var resolved = ResolveSymbol(sym2, comp1, SymbolKeyComparison.None);
        Assert.Equal(sym1, resolved);

        AssertSymbolKeysEqual(sym1, sym2, SymbolKeyComparison.IgnoreAssemblyIds);
        Assert.Null(ResolveSymbol(sym2, comp1, SymbolKeyComparison.IgnoreAssemblyIds));
    }

    #endregion
}
