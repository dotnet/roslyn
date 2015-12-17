// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SymbolId
{
    public partial class SymbolKeyTest : SymbolKeyTestBase
    {
        #region "No change to symbol"

        [Fact]
        public void C2CTypeSymbolUnchanged01()
        {
            var src1 = @"using System;

public delegate void DFoo(int p1, string p2);

namespace N1.N2
{
    public interface IFoo { }
    namespace N3
    {
        public class CFoo 
        {
            public struct SFoo 
            {
                public enum EFoo { Zero, One }
            }
        }
    }
}
";

            var src2 = @"using System;

public delegate void DFoo(int p1, string p2);

namespace N1.N2
{
    public interface IFoo 
    {
        // Add member
        N3.CFoo GetClass();
    }

    namespace N3
    {
        public class CFoo 
        {
            public struct SFoo 
            {
                // Update member
                public enum EFoo { Zero, One, Two }
            }
            // Add member
            public void M(int n) { Console.WriteLine(n); }
        }
    }
}
";

            var comp1 = CreateCompilationWithMscorlib(src1, assemblyName: "Test");
            var comp2 = CreateCompilationWithMscorlib(src2, assemblyName: "Test");

            var originalSymbols = GetSourceSymbols(comp1, SymbolCategory.DeclaredType | SymbolCategory.DeclaredNamespace).OrderBy(s => s.Name);
            var newSymbols = GetSourceSymbols(comp2, SymbolCategory.DeclaredType | SymbolCategory.DeclaredNamespace).OrderBy(s => s.Name);

            ResolveAndVerifySymbolList(newSymbols, comp2, originalSymbols, comp1);
        }

        [Fact, WorkItem(530171)]
        public void C2CErrorSymbolUnchanged01()
        {
            var src1 = @"public void Method() { }";

            var src2 = @"
public void Method() 
{ 
    System.Console.WriteLine(12345);
}
";

            var comp1 = CreateCompilationWithMscorlib(src1);
            var comp2 = CreateCompilationWithMscorlib(src2);

            var symbol01 = comp1.SourceModule.GlobalNamespace.GetMembers().FirstOrDefault() as NamedTypeSymbol;
            var symbol02 = comp1.SourceModule.GlobalNamespace.GetMembers().FirstOrDefault() as NamedTypeSymbol;

            Assert.NotNull(symbol01);
            Assert.NotNull(symbol02);

            Assert.NotEqual(symbol01.Kind, SymbolKind.ErrorType);
            Assert.NotEqual(symbol02.Kind, SymbolKind.ErrorType);

            var originalSymbols = GetSourceSymbols(comp1, SymbolCategory.DeclaredType | SymbolCategory.DeclaredNamespace).OrderBy(s => s.Name);
            var newSymbols = GetSourceSymbols(comp2, SymbolCategory.DeclaredType | SymbolCategory.DeclaredNamespace).OrderBy(s => s.Name);

            ResolveAndVerifySymbolList(newSymbols, comp2, originalSymbols, comp1);
        }

        [Fact]
        [WorkItem(820263)]
        public void PartialDefinitionAndImplementationResolveCorrectly()
        {
            var src = @"using System;
namespace NS
{
    public partial class C1
    {
        partial void M() { }
        partial void M();
    }
}
";

            var comp = CreateCompilationWithMscorlib(src, assemblyName: "Test");

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var type = ns.GetTypeMembers("C1").FirstOrDefault() as NamedTypeSymbol;
            var definition = type.GetMembers("M").First() as IMethodSymbol;
            var implementation = definition.PartialImplementationPart;

            // Assert that both the definition and implementation resolve back to themselves
            Assert.Equal(definition, ResolveSymbol(definition, comp, comp, SymbolKeyComparison.None));
            Assert.Equal(implementation, ResolveSymbol(implementation, comp, comp, SymbolKeyComparison.None));
        }

        [Fact]
        [WorkItem(916341)]
        public void ExplicitIndexerImplementationResolvesCorrectly()
        {
            var src = @"
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

";

            var compilation = CreateCompilationWithMscorlib(src, assemblyName: "Test");

            var type = compilation.SourceModule.GlobalNamespace.GetTypeMembers("C").Single() as NamedTypeSymbol;
            var indexer1 = type.GetMembers().Where(m => m.MetadataName == "I.Item").Single() as IPropertySymbol;
            var indexer2 = type.GetMembers().Where(m => m.MetadataName == "I<T>.Item").Single() as IPropertySymbol;

            AssertSymbolKeysEqual(indexer1, compilation, indexer2, compilation, SymbolKeyComparison.None, expectEqual: false);

            Assert.Equal(indexer1, ResolveSymbol(indexer1, compilation, compilation, SymbolKeyComparison.None));
            Assert.Equal(indexer2, ResolveSymbol(indexer2, compilation, compilation, SymbolKeyComparison.None));
        }

        #endregion

        #region "Change to symbol"

        [Fact]
        public void C2CTypeSymbolChanged01()
        {
            var src1 = @"using System;

public delegate void DFoo(int p1);

namespace N1.N2
{
    public interface IBase { }
    public interface IFoo { }
    namespace N3
    {
        public class CFoo 
        {
            public struct SFoo 
            {
                public enum EFoo { Zero, One }
            }
        }
    }
}
";

            var src2 = @"using System;

public delegate void DFoo(int p1, string p2); // add 1 more parameter

namespace N1.N2
{
    public interface IBase { }
    public interface IFoo : IBase // add base interface
    {
    }

    namespace N3
    {
        public class CFoo : IFoo // impl interface
        {
            private struct SFoo // change modifier
            {
                internal enum EFoo : long { Zero, One } // change base class, and modifier
            }
        }
    }
}
";

            var comp1 = CreateCompilationWithMscorlib(src1, assemblyName: "Test");
            var comp2 = CreateCompilationWithMscorlib(src2, assemblyName: "Test");

            var originalSymbols = GetSourceSymbols(comp1, SymbolCategory.DeclaredType);
            var newSymbols = GetSourceSymbols(comp2, SymbolCategory.DeclaredType);

            ResolveAndVerifySymbolList(newSymbols, comp2, originalSymbols, comp1);
        }

        [Fact]
        public void C2CTypeSymbolChanged02()
        {
            var src1 = @"using System;
namespace NS
{
    public class C1 
    {
        public void M() {}
    }
}
";

            var src2 = @"
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
";
            var comp1 = CreateCompilationWithMscorlib(src1, assemblyName: "Test");
            var comp2 = CreateCompilationWithMscorlib(src2, assemblyName: "Test");

            var namespace1 = comp1.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var typeSym00 = namespace1.GetTypeMembers("C1").FirstOrDefault() as NamedTypeSymbol;

            var namespace2 = comp2.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var typeSym01 = namespace2.GetTypeMembers("C1").FirstOrDefault() as NamedTypeSymbol;
            var typeSym02 = namespace2.GetTypeMembers("C2").Single() as NamedTypeSymbol;

            // new C1 resolve to old C1
            ResolveAndVerifySymbol(typeSym01, comp2, typeSym00, comp1);

            // old C1 (new C2) NOT resolve to old C1
            var symkey = SymbolKey.Create(typeSym02, comp1, CancellationToken.None);
            var syminfo = symkey.Resolve(comp1);
            Assert.Null(syminfo.Symbol);
        }

        [Fact]
        public void C2CMemberSymbolChanged01()
        {
            var src1 = @"using System;
using System.Collections.Generic;

public class Test
{
    private byte field = 123;
    internal string P { get; set; }
    public void M(ref int n) { }
    event Action<string> myEvent;
}
";

            var src2 = @"using System;
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
";
            var comp1 = CreateCompilationWithMscorlib(src1, assemblyName: "Test");
            var comp2 = CreateCompilationWithMscorlib(src2, assemblyName: "Test");

            var originalSymbols = GetSourceSymbols(comp1, SymbolCategory.NonTypeMember | SymbolCategory.Parameter)
                                      .Where(s => !s.IsAccessor()).OrderBy(s => s.Name);

            var newSymbols = GetSourceSymbols(comp2, SymbolCategory.NonTypeMember | SymbolCategory.Parameter)
                                 .Where(s => !s.IsAccessor()).OrderBy(s => s.Name);

            ResolveAndVerifySymbolList(newSymbols, comp2, originalSymbols, comp1);
        }

        [WorkItem(542700)]
        [Fact]
        public void C2CIndexerSymbolChanged01()
        {
            var src1 = @"using System;
using System.Collections.Generic;

public class Test
{
    public string this[string p1] { set { } }
    protected long this[long p1] { set { } }
}
";

            var src2 = @"using System;
public class Test
{
    internal string this[string p1] { set { } } // change modifier
    protected long this[long p1] { get { return 0; } set { } }  // add 'get'
}
";
            var comp1 = CreateCompilationWithMscorlib(src1, assemblyName: "Test");
            var comp2 = CreateCompilationWithMscorlib(src2, assemblyName: "Test");

            var typeSym1 = comp1.SourceModule.GlobalNamespace.GetTypeMembers("Test").Single() as NamedTypeSymbol;
            var originalSymbols = typeSym1.GetMembers(WellKnownMemberNames.Indexer);

            var typeSym2 = comp2.SourceModule.GlobalNamespace.GetTypeMembers("Test").Single() as NamedTypeSymbol;
            var newSymbols = typeSym2.GetMembers(WellKnownMemberNames.Indexer);

            ResolveAndVerifySymbol(newSymbols.First(), comp2, originalSymbols.First(), comp1, SymbolKeyComparison.CaseSensitive);
            ResolveAndVerifySymbol(newSymbols.Last(), comp2, originalSymbols.Last(), comp1, SymbolKeyComparison.CaseSensitive);
        }

        [Fact]
        public void C2CAssemblyChanged01()
        {
            var src = @"
namespace NS
{
    public class C1 
    {
        public void M() {}
    }
}
";
            var comp1 = CreateCompilationWithMscorlib(src, assemblyName: "Assembly1");
            var comp2 = CreateCompilationWithMscorlib(src, assemblyName: "Assembly2");

            var namespace1 = comp1.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var typeSym01 = namespace1.GetTypeMembers("C1").FirstOrDefault() as NamedTypeSymbol;

            var namespace2 = comp2.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var typeSym02 = namespace2.GetTypeMembers("C1").FirstOrDefault() as NamedTypeSymbol;

            // new C1 resolves to old C1 if we ignore assembly and module ids
            ResolveAndVerifySymbol(typeSym02, comp2, typeSym01, comp1, SymbolKeyComparison.CaseSensitive | SymbolKeyComparison.IgnoreAssemblyIds);

            // new C1 DOES NOT resolve to old C1 if we don't ignore assembly and module ids
            Assert.Null(ResolveSymbol(typeSym02, comp2, comp1, SymbolKeyComparison.CaseSensitive));
        }

        [WpfFact(Skip = "530169"), WorkItem(530169)]
        public void C2CAssemblyChanged02()
        {
            var src = @"[assembly: System.Reflection.AssemblyVersion(""1.2.3.4"")] public class C {}";

            // same identity
            var comp1 = CreateCompilationWithMscorlib(src, assemblyName: "Assembly");
            var comp2 = CreateCompilationWithMscorlib(src, assemblyName: "Assembly");

            Symbol sym1 = comp1.Assembly;
            Symbol sym2 = comp2.Assembly;

            // Not ignoreAssemblyAndModules
            ResolveAndVerifySymbol(sym1, comp2, sym2, comp2);

            AssertSymbolKeysEqual(sym1, comp1, sym2, comp2, SymbolKeyComparison.IgnoreAssemblyIds, true);
            Assert.NotNull(ResolveSymbol(sym1, comp1, comp2, SymbolKeyComparison.IgnoreAssemblyIds));

            // Module
            sym1 = comp1.Assembly.Modules[0];
            sym2 = comp2.Assembly.Modules[0];

            ResolveAndVerifySymbol(sym1, comp1, sym2, comp2);

            AssertSymbolKeysEqual(sym2, comp2, sym1, comp1, SymbolKeyComparison.IgnoreAssemblyIds, true);
            Assert.NotNull(ResolveSymbol(sym2, comp2, comp1, SymbolKeyComparison.IgnoreAssemblyIds));
        }

        [Fact, WorkItem(530170)]
        public void C2CAssemblyChanged03()
        {
            var src = @"[assembly: System.Reflection.AssemblyVersion(""1.2.3.4"")] public class C {}";

            // -------------------------------------------------------
            // different name
            var compilation1 = CreateCompilationWithMscorlib(src, assemblyName: "Assembly1");
            var compilation2 = CreateCompilationWithMscorlib(src, assemblyName: "Assembly2");

            ISymbol assembly1 = compilation1.Assembly;
            ISymbol assembly2 = compilation2.Assembly;

            // different
            AssertSymbolKeysEqual(assembly2, compilation2, assembly1, compilation1, SymbolKeyComparison.CaseSensitive, expectEqual: false);
            Assert.Null(ResolveSymbol(assembly2, compilation2, compilation1, SymbolKeyComparison.CaseSensitive));

            // ignore means ALL assembly/module symbols have same ID
            AssertSymbolKeysEqual(assembly2, compilation2, assembly1, compilation1, SymbolKeyComparison.IgnoreAssemblyIds, expectEqual: true);

            // But can NOT be resolved
            Assert.Null(ResolveSymbol(assembly2, compilation2, compilation1, SymbolKeyComparison.IgnoreAssemblyIds));

            // Module
            var module1 = compilation1.Assembly.Modules[0];
            var module2 = compilation2.Assembly.Modules[0];

            // different
            AssertSymbolKeysEqual(module1, compilation1, module2, compilation2, SymbolKeyComparison.CaseSensitive, expectEqual: false);
            Assert.Null(ResolveSymbol(module1, compilation1, compilation2, SymbolKeyComparison.CaseSensitive));

            AssertSymbolKeysEqual(module2, compilation2, module1, compilation1, SymbolKeyComparison.IgnoreAssemblyIds);
            Assert.Null(ResolveSymbol(module2, compilation2, compilation1, SymbolKeyComparison.IgnoreAssemblyIds));
        }

        [Fact, WorkItem(546254)]
        public void C2CAssemblyChanged04()
        {
            var src = @"
[assembly: System.Reflection.AssemblyVersion(""1.2.3.4"")] 
[assembly: System.Reflection.AssemblyTitle(""One Hundred Years of Solitude"")]
public class C {}
";

            var src2 = @"
[assembly: System.Reflection.AssemblyVersion(""1.2.3.42"")] 
[assembly: System.Reflection.AssemblyTitle(""One Hundred Years of Solitude"")]
public class C {}
";

            // different versions
            var comp1 = CreateCompilationWithMscorlib(src, assemblyName: "Assembly");
            var comp2 = CreateCompilationWithMscorlib(src2, assemblyName: "Assembly");

            Symbol sym1 = comp1.Assembly;
            Symbol sym2 = comp2.Assembly;

            // comment is changed to compare Name ONLY
            AssertSymbolKeysEqual(sym1, comp1, sym2, comp2, SymbolKeyComparison.CaseSensitive, expectEqual: true);
            var resolved = ResolveSymbol(sym2, comp2, comp1, SymbolKeyComparison.CaseSensitive);
            Assert.Equal(sym1, resolved);

            AssertSymbolKeysEqual(sym1, comp1, sym2, comp2, SymbolKeyComparison.IgnoreAssemblyIds);
            Assert.Null(ResolveSymbol(sym2, comp2, comp1, SymbolKeyComparison.IgnoreAssemblyIds));
        }

        #endregion
    }
}
