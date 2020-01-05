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

            var comp1 = CreateCompilation(src1);

            // Compilation to Compilation
            var comp2 = (Compilation)CreateCompilation(src2, new MetadataReference[] { new CSharpCompilationReference(comp1) });

            var originalSymbols = GetSourceSymbols(comp1, SymbolCategory.DeclaredType).OrderBy(s => s.Name).ToList();
            Assert.Equal(5, originalSymbols.Count);

            // ---------------------------
            // Metadata symbols
            var typesym = comp2.SourceModule.GlobalNamespace.GetTypeMembers("App").FirstOrDefault() as INamedTypeSymbol;

            // 'D'
            var member01 = (typesym.GetMembers("myEvent").Single() as IEventSymbol).Type;

            // 'I'
            var member02 = (typesym.GetMembers("Prop").Single() as IPropertySymbol).Type;

            // 'C'
            var member03 = typesym.BaseType;

            // 'S'
            var member04 = (typesym.GetMembers("M").Single() as IMethodSymbol).Parameters[0].Type;

            // 'E'
            var member05 = (typesym.GetMembers(WellKnownMemberNames.Indexer).Single() as IPropertySymbol).Type;

            ResolveAndVerifySymbol(member03, originalSymbols[0], comp1, SymbolKeyComparison.None);
            ResolveAndVerifySymbol(member01, originalSymbols[1], comp1, SymbolKeyComparison.None);
            ResolveAndVerifySymbol(member05, originalSymbols[2], comp1, SymbolKeyComparison.None);
            ResolveAndVerifySymbol(member02, originalSymbols[3], comp1, SymbolKeyComparison.None);
            ResolveAndVerifySymbol(member04, originalSymbols[4], comp1, SymbolKeyComparison.None);
        }

        [WorkItem(542700, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542700")]
        [Fact]
        public void M2SNonTypeMemberSymbols01()
        {
            var src1 = @"using System;

namespace N1
{
    public interface IGoo
    {
        void M(int p1, int p2);
        void M(params short[] ary);

        void M(string p1);
        void M(ref string p1);
    }

    public struct S
    {
        public event Action<S> PublicEvent { add { } remove { } }
        public IGoo PublicField;
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

        var igoo = /*<bind1>*/obj.PublicField/*</bind1>*/;

        /*<bind3>*/igoo.M(/*<bind2>*/obj.PublicProp/*</bind2>*/)/*</bind3>*/;

        /*<bind5>*/igoo.M(obj[12], /*<bind4>*/obj[123]/*</bind4>*/)/*</bind5>*/;
    }

    static void EH(AN.S s) { }
}
";

            var comp1 = CreateCompilation(src1);

            // Compilation to Assembly
            var comp2 = CreateCompilation(src2, new MetadataReference[] { comp1.EmitToImageReference() });

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
            ResolveAndVerifySymbol(list[0], originalSymbols[4], model, comp1, SymbolKeyComparison.None);

            // field
            ResolveAndVerifySymbol(list[1], originalSymbols[5], model, comp1, SymbolKeyComparison.None);

            // prop
            ResolveAndVerifySymbol(list[2], originalSymbols[6], model, comp1, SymbolKeyComparison.None);

            // index:
            ResolveAndVerifySymbol(list[4], originalSymbols[7], model, comp1, SymbolKeyComparison.None);

            // M(string p1)
            ResolveAndVerifySymbol(list[3], originalSymbols[2], model, comp1, SymbolKeyComparison.None);

            // M(params short[] ary)
            ResolveAndVerifySymbol(list[5], originalSymbols[1], model, comp1, SymbolKeyComparison.None);
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
            var comp20 = (Compilation)CreateEmptyCompilation(src1, new[] { TestReferences.NetFx.v4_0_21006.mscorlib });

            // "Compilation 2 Assembly"
            var comp40 = (Compilation)CreateCompilation(src2, new MetadataReference[] { comp20.EmitToImageReference() });

            var typeA = comp20.SourceModule.GlobalNamespace.GetTypeMembers("A").Single();
            var mem20_1 = typeA.GetMembers("GetFileInfo").Single() as IMethodSymbol;
            var mem20_2 = typeA.GetMembers("PrintInfo").Single() as IMethodSymbol;

            // FileInfo
            var mtsym20_1 = mem20_1.ReturnType;
            Assert.Equal(2, mem20_2.Parameters.Length);

            // Array
            var mtsym20_2 = mem20_2.Parameters[0].Type;

            // ref DateTime
            var mtsym20_3 = mem20_2.Parameters[1].Type;

            // ====================
            var typeTest = comp40.SourceModule.GlobalNamespace.GetTypeMembers("Test").FirstOrDefault();
            var mem40 = typeTest.GetMembers("Main").Single() as IMethodSymbol;
            var list = GetBlockSyntaxList(mem40);
            var model = comp40.GetSemanticModel(comp40.SyntaxTrees.First());

            foreach (var body in list)
            {
                var df = model.AnalyzeDataFlow(body.Statements.First(), body.Statements.Last());
                foreach (var local in df.VariablesDeclared)
                {
                    var localType = ((ILocalSymbol)local).Type;

                    if (local.Name == "fi")
                    {
                        ResolveAndVerifySymbol(localType, mtsym20_1, comp20, SymbolKeyComparison.None);
                    }
                    else if (local.Name == "ary")
                    {
                        ResolveAndVerifySymbol(localType, mtsym20_2, comp20, SymbolKeyComparison.None);
                    }
                    else if (local.Name == "dt")
                    {
                        ResolveAndVerifySymbol(localType, mtsym20_3, comp20, SymbolKeyComparison.None);
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
    public interface IGoo
    {
        // interface
        IDisposable Prop { get; set; }
        // class
        Exception this[ArgumentException t] { get; }
    }

    public class CGoo : IGoo
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
        var obj = new N20::CGoo();
        N20.IGoo igoo = obj;

        /*<bind0>*/obj.PublicEventField/*</bind0>*/ += /*<bind1>*/MyEveHandler/*</bind1>*/;
        var local = /*<bind2>*/igoo[null]/*</bind2>*/;

        if (/*<bind3>*/obj.PublicField /*</bind3>*/== DayOfWeek.Friday)
        {
            return /*<bind4>*/(obj as N20.IGoo).Prop/*</bind4>*/;
        }
        return null;
    }

    public void MyEveHandler(object o) { }
}
";
            var comp20 = CreateEmptyCompilation(src1, new[] { TestReferences.NetFx.v4_0_21006.mscorlib });

            // "Compilation ref Compilation"
            var comp40 = CreateCompilation(src2, new[] { new CSharpCompilationReference(comp20) });

            var originals = GetSourceSymbols(comp20, SymbolCategory.NonTypeMember | SymbolCategory.Parameter);
            var originalSymbols = originals.Where(s => !s.IsAccessor() && s.Kind != SymbolKind.Parameter).OrderBy(s => s.Name).ToList();

            // IGoo.Prop, CGoo.Prop, Event, Field, IGoo.This, CGoo.This
            Assert.Equal(6, originalSymbols.Count);

            // ====================
            var bindingtuples = GetBindingNodesAndModel<ExpressionSyntax>(comp40);
            var model = bindingtuples.Item2;
            var list = bindingtuples.Item1;
            Assert.Equal(5, list.Count);

            // PublicEventField
            ResolveAndVerifySymbol(list[0], originalSymbols[2], model, comp20);

            // delegate ParameterizedThreadStart
            ResolveAndVerifyTypeSymbol(list[0], (originalSymbols[2] as IEventSymbol).Type, model, comp20);

            // MethodGroup
            ResolveAndVerifyTypeSymbol(list[1], (originalSymbols[2] as IEventSymbol).Type, model, comp20);

            // Indexer
            ResolveAndVerifySymbol(list[2], originalSymbols[4], model, comp20);

            // class Exception
            ResolveAndVerifyTypeSymbol(list[2], (originalSymbols[4] as IPropertySymbol).Type, model, comp20);

            // PublicField
            ResolveAndVerifySymbol(list[3], originalSymbols[3], model, comp20);

            // enum DayOfWeek
            ResolveAndVerifyTypeSymbol(list[3], (originalSymbols[3] as IFieldSymbol).Type, model, comp20);

            // Prop
            ResolveAndVerifySymbol(list[4], originalSymbols[0], model, comp20);

            // interface IDisposable
            ResolveAndVerifyTypeSymbol(list[4], (originalSymbols[0] as IPropertySymbol).Type, model, comp20);
        }

        [Fact, WorkItem(546255, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546255")]
        public void M2MMultiTargetingMsCorLib03()
        {
            var src1 = @"using System;
namespace Mscorlib20
{
    public interface IGoo
    {
        // interface
        IDisposable Prop { get; set; }
        // class
        Exception this[ArgumentException t] { get; }
    }

    public class CGoo : IGoo
    {
        // explicit
        IDisposable IGoo.Prop { get; set; }
        Exception IGoo.this[ArgumentException t] { get { return t; } }
    }
}
";

            var src2 = @"using System;
using N20 = Mscorlib20;

class Test
{
    public IDisposable M()
    {
        N20.IGoo igoo = new N20::CGoo();

        var local = /*<bind0>*/igoo[new ArgumentException()]/*</bind0>*/;
        return /*<bind1>*/igoo.Prop/*</bind1>*/;
    }
}
";
            var comp20 = CreateEmptyCompilation(src1, new[] { TestReferences.NetFx.v4_0_21006.mscorlib });

            // "Compilation ref Compilation"
            var comp40 = CreateCompilation(src2, new[] { new CSharpCompilationReference(comp20) });

            var originals = GetSourceSymbols(comp20, SymbolCategory.NonTypeMember | SymbolCategory.Parameter);
            var originalSymbols = originals.Where(s => !s.IsAccessor() && s.Kind != SymbolKind.Parameter).OrderBy(s => s.Name).ToList();

            // CGoo.Prop, CGoo.This, IGoo.Prop, IGoo.This
            Assert.Equal(4, originalSymbols.Count);

            // ====================
            var bindingtuples = GetBindingNodesAndModel<ExpressionSyntax>(comp40);
            var model = bindingtuples.Item2;
            var list = bindingtuples.Item1;
            Assert.Equal(2, list.Count);

            // Indexer
            ResolveAndVerifySymbol(list[0], originalSymbols[3], model, comp20);

            // class Exception
            ResolveAndVerifyTypeSymbol(list[0], (originalSymbols[3] as IPropertySymbol).Type, model, comp20);

            // Prop
            ResolveAndVerifySymbol(list[1], originalSymbols[2], model, comp20);

            // interface IDisposable
            ResolveAndVerifyTypeSymbol(list[1], (originalSymbols[2] as IPropertySymbol).Type, model, comp20);
        }

        #endregion
    }
}
