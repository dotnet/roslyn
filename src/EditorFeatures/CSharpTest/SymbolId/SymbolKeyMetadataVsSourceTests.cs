// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SymbolId
{
    public partial class SymbolKeyTest : SymbolKeyTestBase
    {
        #region "Metadata vs. Source"

        [Fact]
        public void M2SNamedTypeSymbols01()
        {
            var src1 = @"using System;

public delegate void D(int p1, string p2);

namespace N1.N2
{
    public interface I { }
    namespace N3
    {
        public class C 
        {
            public struct S 
            {
                public enum E { Zero, One, Two }
                public void M(int n) { Console.WriteLine(n); }
            }
        }
    }
}
";

            var src2 = @"using System;
using N1.N2.N3;

public class App : C
{
    private event D myEvent;
    internal N1.N2.I Prop { get; set; }
    protected C.S.E this[int x] { set { } }
    public void M(C.S s) { s.M(123); }
}
";

            var comp1 = CreateCompilationWithMscorlib(src1);

            // Compilation to Compilation
            var comp2 = CreateCompilationWithMscorlib(src2, new MetadataReference[] { new CSharpCompilationReference(comp1) });

            var originalSymbols = GetSourceSymbols(comp1, SymbolCategory.DeclaredType).OrderBy(s => s.Name).ToList();
            Assert.Equal(5, originalSymbols.Count);

            // ---------------------------
            // Metadata symbols
            var typesym = comp2.SourceModule.GlobalNamespace.GetTypeMembers("App").FirstOrDefault() as NamedTypeSymbol;

            // 'D'
            var member01 = (typesym.GetMembers("myEvent").Single() as EventSymbol).Type;

            // 'I'
            var member02 = (typesym.GetMembers("Prop").Single() as PropertySymbol).Type;

            // 'C'
            var member03 = typesym.BaseType;

            // 'S'
            var member04 = (typesym.GetMembers("M").Single() as MethodSymbol).Parameters[0].Type;

            // 'E'
            var member05 = (typesym.GetMembers(WellKnownMemberNames.Indexer).Single() as PropertySymbol).Type;

            ResolveAndVerifySymbol(member03, comp2, originalSymbols[0], comp1, SymbolKeyComparison.CaseSensitive);
            ResolveAndVerifySymbol(member01, comp2, originalSymbols[1], comp1, SymbolKeyComparison.CaseSensitive);
            ResolveAndVerifySymbol(member05, comp2, originalSymbols[2], comp1, SymbolKeyComparison.CaseSensitive);
            ResolveAndVerifySymbol(member02, comp2, originalSymbols[3], comp1, SymbolKeyComparison.CaseSensitive);
            ResolveAndVerifySymbol(member04, comp2, originalSymbols[4], comp1, SymbolKeyComparison.CaseSensitive);
        }

        [WorkItem(542700, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542700")]
        [Fact]
        public void M2SNonTypeMemberSymbols01()
        {
            var src1 = @"using System;

namespace N1
{
    public interface IFoo
    {
        void M(int p1, int p2);
        void M(params short[] ary);

        void M(string p1);
        void M(ref string p1);
    }

    public struct S
    {
        public event Action<S> PublicEvent { add { } remove { } }
        public IFoo PublicField;
        public string PublicProp { get; set; }
        public short this[sbyte p] { get { return p; } }
    }
}
";

            var src2 = @"using System;
using AN = N1;

public class App
{
    static void Main()
    {
        var obj = new AN.S();

        /*<bind0>*/obj.PublicEvent/*</bind0>*/ += EH;

        var ifoo = /*<bind1>*/obj.PublicField/*</bind1>*/;

        /*<bind3>*/ifoo.M(/*<bind2>*/obj.PublicProp/*</bind2>*/)/*</bind3>*/;

        /*<bind5>*/ifoo.M(obj[12], /*<bind4>*/obj[123]/*</bind4>*/)/*</bind5>*/;
    }

    static void EH(AN.S s) { }
}
";

            var comp1 = CreateCompilationWithMscorlib(src1);

            // Compilation to Assembly
            var comp2 = CreateCompilationWithMscorlib(src2, new MetadataReference[] { comp1.EmitToImageReference() });

            // ---------------------------
            // Source symbols
            var originalSymbols = GetSourceSymbols(comp1, SymbolCategory.NonTypeMember | SymbolCategory.Parameter).ToList();
            originalSymbols = originalSymbols.Where(s => !s.IsAccessor() && s.Kind != SymbolKind.Parameter).OrderBy(s => s.Name).Select(s => s).ToList();
            Assert.Equal(8, originalSymbols.Count);

            // ---------------------------
            // Metadata symbols
            var bindingtuples = GetBindingNodesAndModel<ExpressionSyntax>(comp2);
            var model = bindingtuples.Item2;
            var list = bindingtuples.Item1;
            Assert.Equal(6, list.Count);

            // event
            ResolveAndVerifySymbol(list[0], originalSymbols[4], model, comp1, SymbolKeyComparison.CaseSensitive);

            // field
            ResolveAndVerifySymbol(list[1], originalSymbols[5], model, comp1, SymbolKeyComparison.CaseSensitive);

            // prop
            ResolveAndVerifySymbol(list[2], originalSymbols[6], model, comp1, SymbolKeyComparison.CaseSensitive);

            // index:
            ResolveAndVerifySymbol(list[4], originalSymbols[7], model, comp1, SymbolKeyComparison.CaseSensitive);

            // M(string p1)
            ResolveAndVerifySymbol(list[3], originalSymbols[2], model, comp1, SymbolKeyComparison.CaseSensitive);

            // M(params short[] ary)
            ResolveAndVerifySymbol(list[5], originalSymbols[1], model, comp1, SymbolKeyComparison.CaseSensitive);
        }

        #endregion

        #region "Metadata vs. Metadata"

        [Fact]
        public void M2MMultiTargetingMsCorLib01()
        {
            var src1 = @"using System;
using System.IO;

public class A
{
    public FileInfo GetFileInfo(string path)
    {
        if (File.Exists(path))
        {
            return new FileInfo(path);
        }

        return null;
    }

    public void PrintInfo(Array ary, ref DateTime time)
    {
        if (ary != null)
            Console.WriteLine(ary);
        else
            Console.WriteLine(""null"");

        time = DateTime.Now;
    }
}
";

            var src2 = @"using System;

class Test
{
    static void Main()
    {
        var a = new A();
        var fi = a.GetFileInfo(null);
        Console.WriteLine(fi);

        var dt = DateTime.Now;
        var ary = Array.CreateInstance(typeof(string), 2);
        a.PrintInfo(ary, ref dt);
    }
}
";
            var comp20 = CreateCompilation(src1, new[] { TestReferences.NetFx.v4_0_21006.mscorlib });

            // "Compilation 2 Assembly"
            var comp40 = CreateCompilationWithMscorlib(src2, new MetadataReference[] { comp20.EmitToImageReference() });

            var typeA = comp20.SourceModule.GlobalNamespace.GetTypeMembers("A").Single();
            var mem20_1 = typeA.GetMembers("GetFileInfo").Single() as MethodSymbol;
            var mem20_2 = typeA.GetMembers("PrintInfo").Single() as MethodSymbol;

            // FileInfo
            var mtsym20_1 = mem20_1.ReturnType;
            Assert.Equal(2, mem20_2.Parameters.Length);

            // Array
            var mtsym20_2 = mem20_2.Parameters[0].Type;

            // ref DateTime
            var mtsym20_3 = mem20_2.Parameters[1].Type;

            // ====================
            var typeTest = comp40.SourceModule.GlobalNamespace.GetTypeMembers("Test").FirstOrDefault();
            var mem40 = typeTest.GetMembers("Main").Single() as MethodSymbol;
            var list = GetBlockSyntaxList(mem40);
            var model = comp40.GetSemanticModel(comp40.SyntaxTrees[0]);

            foreach (var body in list)
            {
                var df = model.AnalyzeDataFlow(body.Statements.First(), body.Statements.Last());
                if (df.VariablesDeclared != null)
                {
                    foreach (var local in df.VariablesDeclared)
                    {
                        var localType = ((LocalSymbol)local).Type;

                        if (local.Name == "fi")
                        {
                            ResolveAndVerifySymbol(localType, comp40, mtsym20_1, comp20, SymbolKeyComparison.CaseSensitive);
                        }
                        else if (local.Name == "ary")
                        {
                            ResolveAndVerifySymbol(localType, comp40, mtsym20_2, comp20, SymbolKeyComparison.CaseSensitive);
                        }
                        else if (local.Name == "dt")
                        {
                            ResolveAndVerifySymbol(localType, comp40, mtsym20_3, comp20, SymbolKeyComparison.CaseSensitive);
                        }
                    }
                }
            }
        }

        [Fact, WorkItem(546255, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546255")]
        public void M2MMultiTargetingMsCorLib02()
        {
            var src1 = @"using System;
namespace Mscorlib20
{
    public interface IFoo
    {
        // interface
        IDisposable Prop { get; set; }
        // class
        Exception this[ArgumentException t] { get; }
    }

    public class CFoo : IFoo
    {
        // enum
        public DayOfWeek PublicField;
        // delegate
        public event System.Threading.ParameterizedThreadStart PublicEventField;

        public IDisposable Prop { get; set; }
        public Exception this[ArgumentException t] { get { return t; } }
    }
}
";

            var src2 = @"using System;
using N20 = Mscorlib20;

class Test
{
    public IDisposable M()
    {
        var obj = new N20::CFoo();
        N20.IFoo ifoo = obj;

        /*<bind0>*/obj.PublicEventField/*</bind0>*/ += /*<bind1>*/MyEveHandler/*</bind1>*/;
        var local = /*<bind2>*/ifoo[null]/*</bind2>*/;

        if (/*<bind3>*/obj.PublicField /*</bind3>*/== DayOfWeek.Friday)
        {
            return /*<bind4>*/(obj as N20.IFoo).Prop/*</bind4>*/;
        }
        return null;
    }

    public void MyEveHandler(object o) { }
}
";
            var comp20 = CreateCompilation(src1, new[] { TestReferences.NetFx.v4_0_21006.mscorlib });

            // "Compilation ref Compilation"
            var comp40 = CreateCompilationWithMscorlib(src2, new[] { new CSharpCompilationReference(comp20) });

            var originals = GetSourceSymbols(comp20, SymbolCategory.NonTypeMember | SymbolCategory.Parameter);
            var originalSymbols = originals.Where(s => !s.IsAccessor() && s.Kind != SymbolKind.Parameter).OrderBy(s => s.Name).ToList();

            // IFoo.Prop, CFoo.Prop, Event, Field, IFoo.This, CFoo.This
            Assert.Equal(6, originalSymbols.Count);

            // ====================
            var bindingtuples = GetBindingNodesAndModel<ExpressionSyntax>(comp40);
            var model = bindingtuples.Item2;
            var list = bindingtuples.Item1;
            Assert.Equal(5, list.Count);

            // PublicEventField
            ResolveAndVerifySymbol(list[0], originalSymbols[2], model, comp20);

            // delegate ParameterizedThreadStart
            ResolveAndVerifyTypeSymbol(list[0], (originalSymbols[2] as EventSymbol).Type, model, comp20);

            // MethodGroup
            ResolveAndVerifyTypeSymbol(list[1], (originalSymbols[2] as EventSymbol).Type, model, comp20);

            // Indexer
            ResolveAndVerifySymbol(list[2], originalSymbols[4], model, comp20);

            // class Exception
            ResolveAndVerifyTypeSymbol(list[2], (originalSymbols[4] as PropertySymbol).Type, model, comp20);

            // PublicField
            ResolveAndVerifySymbol(list[3], originalSymbols[3], model, comp20);

            // enum DayOfWeek
            ResolveAndVerifyTypeSymbol(list[3], (originalSymbols[3] as FieldSymbol).Type, model, comp20);

            // Prop
            ResolveAndVerifySymbol(list[4], originalSymbols[0], model, comp20);

            // interface IDisposable
            ResolveAndVerifyTypeSymbol(list[4], (originalSymbols[0] as PropertySymbol).Type, model, comp20);
        }

        [Fact, WorkItem(546255, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546255")]
        public void M2MMultiTargetingMsCorLib03()
        {
            var src1 = @"using System;
namespace Mscorlib20
{
    public interface IFoo
    {
        // interface
        IDisposable Prop { get; set; }
        // class
        Exception this[ArgumentException t] { get; }
    }

    public class CFoo : IFoo
    {
        // explicit
        IDisposable IFoo.Prop { get; set; }
        Exception IFoo.this[ArgumentException t] { get { return t; } }
    }
}
";

            var src2 = @"using System;
using N20 = Mscorlib20;

class Test
{
    public IDisposable M()
    {
        N20.IFoo ifoo = new N20::CFoo();

        var local = /*<bind0>*/ifoo[new ArgumentException()]/*</bind0>*/;
        return /*<bind1>*/ifoo.Prop/*</bind1>*/;
    }
}
";
            var comp20 = CreateCompilation(src1, new[] { TestReferences.NetFx.v4_0_21006.mscorlib });

            // "Compilation ref Compilation"
            var comp40 = CreateCompilationWithMscorlib(src2, new[] { new CSharpCompilationReference(comp20) });

            var originals = GetSourceSymbols(comp20, SymbolCategory.NonTypeMember | SymbolCategory.Parameter);
            var originalSymbols = originals.Where(s => !s.IsAccessor() && s.Kind != SymbolKind.Parameter).OrderBy(s => s.Name).ToList();

            // CFoo.Prop, CFoo.This, IFoo.Prop, IFoo.This
            Assert.Equal(4, originalSymbols.Count);

            // ====================
            var bindingtuples = GetBindingNodesAndModel<ExpressionSyntax>(comp40);
            var model = bindingtuples.Item2;
            var list = bindingtuples.Item1;
            Assert.Equal(2, list.Count);

            // Indexer
            ResolveAndVerifySymbol(list[0], originalSymbols[3], model, comp20);

            // class Exception
            ResolveAndVerifyTypeSymbol(list[0], (originalSymbols[3] as PropertySymbol).Type, model, comp20);

            // Prop
            ResolveAndVerifySymbol(list[1], originalSymbols[2], model, comp20);

            // interface IDisposable
            ResolveAndVerifyTypeSymbol(list[1], (originalSymbols[2] as PropertySymbol).Type, model, comp20);
        }

        #endregion
    }
}
