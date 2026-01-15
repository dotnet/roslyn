// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public class ExtensionIndexersTests : CompilingTestBase
{
    private static string ExpectedOutput(string output)
    {
        return ExecutionConditionUtil.IsMonoOrCoreClr ? output : null;
    }

    [Theory, CombinatorialData]
    public void LangVer_01(bool useCompilationReference)
    {
        var libSrc = """
public static class E
{
    extension(object o)
    {
        public int this[int i]
        {
            get { System.Console.Write($"get({i})"); return i + 1; }
            set { System.Console.Write($"set({i}, {value})"); }
        }
    }
}
""";
        var libComp = CreateCompilation(libSrc);
        var libRef = AsReference(libComp, useCompilationReference);

        var src = """
object o = new object();
o[0] = 1;
_ = o[2];
""";

        CreateCompilation(src, references: [libRef], parseOptions: TestOptions.Regular14).VerifyEmitDiagnostics(
            // (2,1): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // o[0] = 1;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "o[0]").WithArguments("extension indexers").WithLocation(2, 1),
            // (3,5): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = o[2];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "o[2]").WithArguments("extension indexers").WithLocation(3, 5));

        CreateCompilation(src, references: [libRef], parseOptions: TestOptions.RegularNext).VerifyEmitDiagnostics();

        CreateCompilation(src, references: [libRef], parseOptions: TestOptions.RegularPreview).VerifyEmitDiagnostics();

        CreateCompilation([src, libSrc], parseOptions: TestOptions.Regular14).VerifyEmitDiagnostics(
            // (5,20): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         public int this[int i]
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "this").WithArguments("extension indexers").WithLocation(5, 20));

        CreateCompilation([src, libSrc], parseOptions: TestOptions.RegularNext).VerifyEmitDiagnostics();

        CreateCompilation([src, libSrc], parseOptions: TestOptions.RegularPreview).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Declaration_01()
    {
        // unnamed extension parameter
        var src = """
public static class E
{
    extension(object)
    {
        public int this[int i]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,20): error CS9303: 'this[]': cannot declare instance members in an extension block with an unnamed receiver parameter
            //         public int this[int i]
            Diagnostic(ErrorCode.ERR_InstanceMemberWithUnnamedExtensionsParameter, "this").WithArguments("this[]").WithLocation(5, 20));
    }

    [Fact]
    public void Declaration_02()
    {
        // static indexer
        var src = """
public static class E
{
    extension(object o)
    {
        public static int this[int i]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,27): error CS0106: The modifier 'static' is not valid for this item
            //         public static int this[int i]
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(5, 27));
    }

    [Fact]
    public void Declaration_03()
    {
        // protected indexer
        var src = """
public static class E
{
    extension(object)
    {
        protected int this[int i]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,23): error CS9302: 'E.extension(object).this[int]': new protected member declared in an extension block
            //         protected int this[int i]
            Diagnostic(ErrorCode.ERR_ProtectedInExtension, "this").WithArguments("E.extension(object).this[int]").WithLocation(5, 23));
    }

    [Fact]
    public void Declaration_04()
    {
        // non-inferrable extension member
        var src = """
public static class E
{
    extension<T>(object o)
    {
        public int this[int i]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,20): error CS9295: The type parameter `T` is not referenced by either the extension parameter or a parameter of this member
            //         public int this[int i]
            Diagnostic(ErrorCode.ERR_UnderspecifiedExtension, "this").WithArguments("T").WithLocation(5, 20));
    }

    [Fact]
    public void Declaration_05()
    {
        // indexer uses same parameter name as extension parameter
        var src = """
public static class E
{
    extension(int i)
    {
        public int this[int i]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,29): error CS9290: 'i': a parameter, local variable, or local function cannot have the same name as an extension parameter
            //         public int this[int i]
            Diagnostic(ErrorCode.ERR_LocalSameNameAsExtensionParameter, "i").WithArguments("i").WithLocation(5, 29));
    }

    [Fact]
    public void Declaration_07()
    {
        // parameter name conflict with extension type parameter
        var src = """
public static class E
{
    extension<T>(T t)
    {
        public int this[object T] => throw null;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,32): error CS9288: 'T': a parameter, local variable, or local function cannot have the same name as an extension container type parameter
            //         public int this[object T] => throw null;
            Diagnostic(ErrorCode.ERR_LocalSameNameAsExtensionTypeParameter, "T").WithArguments("T").WithLocation(5, 32));
    }

    [Fact]
    public void Declaration_08()
    {
        // parameter name conflict with enclosing static class
        var src = """
public static class E
{
    extension(object o)
    {
        public int this[object E] => throw null;
    }
}

class C
{
    public int this[object C] => throw null;
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Declaration_09()
    {
        var src = """
public static class E
{
    extension(int i)
    {
        public int this[int j]
        {
            get => field;
            set { field = value; }
        }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (7,20): error CS0103: The name 'field' does not exist in the current context
            //             get => field;
            Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(7, 20),
            // (8,19): error CS0103: The name 'field' does not exist in the current context
            //             set { field = value; }
            Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(8, 19));
    }

    [Fact]
    public void Declaration_10()
    {
        var src = """
int i = 42;
i[43] = 0;

public static class E
{
    extension(int i)
    {
        public int this[int j]
        {
            set { var field = j; System.Console.Write(field); }
        }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "43").VerifyDiagnostics();
    }

    [Fact]
    public void Declaration_12()
    {
        var src = """
new object()[0] = 1;

static class E
{
    extension(object o)
    {
        public int this[int i] { init => throw null; }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS8852: Init-only property or indexer 'E.extension(object).this[int]' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
            // new object()[0] = 1;
            Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "new object()[0]").WithArguments("E.extension(object).this[int]").WithLocation(1, 1),
            // (7,34): error CS9304: 'E.extension(object).this[int]': cannot declare init-only accessors in an extension block
            //         public int this[int i] { init => throw null; }
            Diagnostic(ErrorCode.ERR_InitInExtension, "init").WithArguments("E.extension(object).this[int]").WithLocation(7, 34));
    }

    [Fact]
    public void Declaration_13()
    {
        // getter named after enclosing static class
        var src = """
static class get_Item
{
    extension(object o)
    {
        public int this[int i] { get => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,34): error CS0542: 'get_Item': member names cannot be the same as their enclosing type
            //         public int this[int i] { get => throw null; }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "get").WithArguments("get_Item").WithLocation(5, 34));
    }

    [Fact]
    public void Declaration_14()
    {
        // setter named after enclosing static class
        var src = """
static class set_Item
{
    extension(object o)
    {
        public int this[int i] { set => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,34): error CS0542: 'set_Item': member names cannot be the same as their enclosing type
            //         public int this[int i] { set => throw null; }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "set").WithArguments("set_Item").WithLocation(5, 34));
    }

    [Fact]
    public void InconsistentTypeAccessibility_01()
    {
        var src = """
public static class E
{
    extension(C c)
    {
        public int this[int i] => 0;
    }

    extension(int i)
    {
        public int this[C c] => 0;
        public C this[int j] => null;
    }

    private class C {}
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,20): error CS0055: Inconsistent accessibility: parameter type 'E.C' is less accessible than indexer or property 'E.extension(E.C).this[int]'
            //         public int this[int i] => 0;
            Diagnostic(ErrorCode.ERR_BadVisIndexerParam, "this").WithArguments("E.extension(E.C).this[int]", "E.C").WithLocation(5, 20),
            // (10,20): error CS0055: Inconsistent accessibility: parameter type 'E.C' is less accessible than indexer or property 'E.extension(int).this[E.C]'
            //         public int this[C c] => 0;
            Diagnostic(ErrorCode.ERR_BadVisIndexerParam, "this").WithArguments("E.extension(int).this[E.C]", "E.C").WithLocation(10, 20),
            // (11,18): error CS0054: Inconsistent accessibility: indexer return type 'E.C' is less accessible than indexer 'E.extension(int).this[int]'
            //         public C this[int j] => null;
            Diagnostic(ErrorCode.ERR_BadVisIndexerReturn, "this").WithArguments("E.extension(int).this[int]", "E.C").WithLocation(11, 18)
            );
    }

    [Fact]
    public void Indexing_01()
    {
        // no instance indexer
        var src = """
C c = new C();
c[0] = 1;
_ = c[2];

E.set_Item(c, 0, 1);
_ = E.get_Item(c, 2);

public static class E
{
    extension(C c)
    {
        public int this[int i]
        {
            get { System.Console.Write($"get({i}) "); return i + 1; }
            set { System.Console.Write($"set({i}, {value}) "); }
        }
    }
}

public class C
{
    public int this[string s]
    {
        get => throw null;
        set => throw null;
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "set(0, 1) get(2) set(0, 1) get(2)").VerifyDiagnostics();

        var indexer = comp.GlobalNamespace.GetTypeMember("E").GetTypeMember("").GetIndexer<PropertySymbol>("Item");
        AssertEx.Equal("E.extension(C).this[int]", indexer.ToDisplayString());
        AssertEx.Equal("System.Int32 E.get_Item(C c, System.Int32 i)", indexer.GetMethod.GetPublicSymbol().AssociatedExtensionImplementation.ToTestDisplayString());
        AssertEx.Equal("void E.set_Item(C c, System.Int32 i, System.Int32 value)", indexer.SetMethod.GetPublicSymbol().AssociatedExtensionImplementation.ToTestDisplayString());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var getterAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "c[0]");
        AssertEx.Equal("E.extension(C).this[int]", model.GetSymbolInfo(getterAccess).Symbol.ToDisplayString());

        var setterAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "c[2]");
        AssertEx.Equal("E.extension(C).this[int]", model.GetSymbolInfo(setterAccess).Symbol.ToDisplayString());
    }

    [Fact]
    public void Indexing_02()
    {
        // instance indexer is not applicable
        var src = """
C c = new C();
c[0] = 1;
_ = c[2];

public static class E
{
    extension(C c)
    {
        public int this[int i]
        {
            get { System.Console.Write($"get({i})"); return i + 1; }
            set { System.Console.Write($"set({i}, {value}) "); }
        }
    }
}

public class C
{
    public int this[string s]
    {
        get => throw null;
        set => throw null;
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "set(0, 1) get(2)").VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_03()
    {
        // ambiguous extension indexers
        var src = """
object o = new object();
o[0] = 1;
_ = o[2];

static class E1
{
    extension(object o)
    {
        public int this[int i]
        {
            get => throw null;
            set => throw null;
        }
    }
}

static class E2
{
    extension(object o)
    {
        public int this[int i]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";

        // PROTOTYPE diagnostic quality, report something better (ambiguity between X and Y)
        var comp = CreateCompilation(src).VerifyEmitDiagnostics(
            // (2,1): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
            // o[0] = 1;
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "o[0]").WithArguments("object").WithLocation(2, 1),
            // (3,5): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
            // _ = o[2];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "o[2]").WithArguments("object").WithLocation(3, 5));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var indexing = GetSyntax<ElementAccessExpressionSyntax>(tree, "o[0]");
        Assert.Null(model.GetSymbolInfo(indexing).Symbol);
        // PROTOTYPE public API, consider returning the candidate extension members (like we do for method calls)
        Assert.Empty(model.GetMemberGroup(indexing));
    }

    [Fact]
    public void Indexing_04()
    {
        // ambiguous extension indexers
        var src = """
object o = new object();
o[0, 1] = 2;
_ = o[3, 4];

static class E1
{
    extension(object o)
    {
        public int this[int i, long l]
        {
            get => throw null;
            set => throw null;
        }
    }
}

static class E2
{
    extension(object o)
    {
        public int this[long l, int i]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";

        // PROTOTYPE diagnostic quality, report something better (ambiguity between X and Y)
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (2,1): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
            // o[0, 1] = 2;
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "o[0, 1]").WithArguments("object").WithLocation(2, 1),
            // (3,5): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
            // _ = o[3, 4];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "o[3, 4]").WithArguments("object").WithLocation(3, 5));
    }

    [Fact]
    public void Indexing_05()
    {
        // instance indexer comes first
        var src = """
C c = new C();
c[0] = 1;
_ = c[2];

public static class E
{
    extension(C c)
    {
        public int this[int i]
        {
            get => throw null;
            set => throw null;
        }
    }
}

public class C
{
    public int this[int i]
    {
        get { System.Console.Write($"get({i})"); return i + 1; }
        set { System.Console.Write($"set({i}, {value}) "); }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "set(0, 1) get(2)").VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_06()
    {
        // instance Index indexer takes precedence over extension Index indexer
        var src = """
var c = new C();
_ = c[^1];
c[^2] = 10;

static class E
{
    extension(C c)
    {
        public int this[System.Index i]
        {
            get => throw null;
            set => throw null;
        }
    }
}

class C
{
    public int this[System.Index i]
    {
        get { System.Console.Write($"get({i}) "); return 42; }
        set { System.Console.Write($"set({i}, {value}) "); }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("get(^1) set(^2, 10)"), verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_07()
    {
        // instance Index indexer takes precedence over extension Index indexer
        var src = """
var c = new C();
_ = c[^1];
c[^2] = 10;

static class E
{
    extension(C c)
    {
        public int this[System.Index i]
        {
            get => throw null;
            set => throw null;
        }
    }
}

class C
{
    public int this[string s] => 0;
    public int this[System.Index i]
    {
        get { System.Console.Write($"get({i}) "); return 42; }
        set { System.Console.Write($"set({i}, {value}) "); }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("get(^1) set(^2, 10)"), verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_08()
    {
        // extension indexer is obsolete
        var src = """
var o = new object();
o[0] = 1;
_ = o[2];

E.set_Item(o, 0, 1);
_ = E.get_Item(o, 2);

public static class E
{
    extension(object o)
    {
        [System.Obsolete("indexer")]
        public int this[int i]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (2,1): warning CS0618: 'E.extension(object).this[int]' is obsolete: 'indexer'
            // o[0] = 1;
            Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "o[0]").WithArguments("E.extension(object).this[int]", "indexer").WithLocation(2, 1),
            // (3,5): warning CS0618: 'E.extension(object).this[int]' is obsolete: 'indexer'
            // _ = o[2];
            Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "o[2]").WithArguments("E.extension(object).this[int]", "indexer").WithLocation(3, 5));
    }

    [Fact]
    public void Indexing_09()
    {
        var src = """
C.M(new C());

public class C
{
    public static void M(C C)
    {
        C[0] = 1;
        _ = C[2];
    }
}

public static class E
{
    extension(C c)
    {
        public int this[int i]
        {
            get { System.Console.Write($"get({i})"); return i + 1; }
            set { System.Console.Write($"set({i}, {value}) "); }
        }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "set(0, 1) get(2)").VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_10()
    {
        // generic extension block
        var src = """
var o = new object();
o[0] = 1;
_ = o[2];

public static class E
{
    extension<T>(T t)
    {
        public int this[int i]
        {
            get { System.Console.Write($"get({i}) "); return i + 1; }
            set { System.Console.Write($"set({i}, {value}) "); }
        }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "set(0, 1) get(2)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var setterAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "o[0]");
        AssertEx.Equal("E.extension<object>(object).this[int]", model.GetSymbolInfo(setterAccess).Symbol.ToDisplayString());

        var getterAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "o[2]");
        AssertEx.Equal("E.extension<object>(object).this[int]", model.GetSymbolInfo(getterAccess).Symbol.ToDisplayString());

        var indexer = comp.GlobalNamespace.GetTypeMember("E").GetTypeMember("").GetMembers().OfType<PropertySymbol>().Single().GetPublicSymbol();
        var byteType = comp.GetSpecialType(SpecialType.System_Byte).GetPublicSymbol();
        AssertEx.Equal("E.extension<byte>(byte).this[int]", indexer.ReduceExtensionMember(byteType).ToDisplayString());

        AssertEx.Equal("System.Int32 E.get_Item<T>(T t, System.Int32 i)", indexer.GetMethod.AssociatedExtensionImplementation.ToTestDisplayString());
        AssertEx.Equal("void E.set_Item<T>(T t, System.Int32 i, System.Int32 value)", indexer.SetMethod.AssociatedExtensionImplementation.ToTestDisplayString());
    }

    [Fact]
    public void Indexing_11()
    {
        // generic extension block, type parameter used in indexer parameter
        var src = """
var o = new object();
o[0] = 1;
_ = o[2];

public static class E
{
    extension<T>(object o)
    {
        public int this[T t]
        {
            get { System.Console.Write($"get({t}) "); return 0; }
            set { System.Console.Write($"set({t}, {value}) "); }
        }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "set(0, 1) get(2)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var setterAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "o[0]");
        AssertEx.Equal("E.extension<int>(object).this[int]", model.GetSymbolInfo(setterAccess).Symbol.ToDisplayString());

        var getterAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "o[2]");
        AssertEx.Equal("E.extension<int>(object).this[int]", model.GetSymbolInfo(getterAccess).Symbol.ToDisplayString());
    }

    [Fact]
    public void Indexing_12()
    {
        // named arguments and evaluation order
        var src = """
var o = new object();
o[b: id(1), a: id(2)] = id(3);
_ = o[b: id(4), a: id(5)];

int id(int x) { System.Console.Write($"id({x}) "); return x; }

public static class E
{
    extension(object o)
    {
        public int this[int a, int b]
        {
            get { System.Console.Write($"get({a}, {b}) "); return 0; }
            set { System.Console.Write($"set({a}, {b}, {value}) "); }
        }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "id(1) id(2) id(3) set(2, 1, 3) id(4) id(5) get(5, 4)").VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_13()
    {
        // convert receiver
        var src = """
C c = new C();
c[0] = 1;

public static class E
{
    extension(object o)
    {
        public int this[int i]
        {
            set { System.Console.Write($"set({i}, {value}) "); }
        }
    }
}

class C { }
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "set(0, 1)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var setterAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "c[0]");
        var typeInfo = model.GetTypeInfo(setterAccess.Expression);
        AssertEx.Equal("C", typeInfo.Type.ToTestDisplayString());
        AssertEx.Equal("System.Object", typeInfo.ConvertedType.ToTestDisplayString());
    }

    [Fact]
    public void Indexing_14()
    {
        // default parameter values
        var src = """
var o = new object();
_ = o[a: 100];
o[b: 101] = 0;

public static class E
{
    extension(object o)
    {
        public int this[int a = 42, int b = 43]
        {
            get { System.Console.Write($"get({a}, {b}) "); return 0; }
            set { System.Console.Write($"set({a}, {b}, {value}) "); }
        }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "get(100, 43) set(42, 101, 0)").VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_15()
    {
        // params
        var src = """
var o = new object();
_ = o[42, 43, 44];

public static class E
{
    extension(object o)
    {
        public int this[int a, params int[] b]
        {
            get { System.Console.Write($"get({a}, {b[0]}, {b[1]}) "); return 0; }
        }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "get(42, 43, 44)").VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_16()
    {
        // multiple scopes
        var src = """
namespace N
{
    public class C
    {
        public static void Main()
        {
            var o = new object();
            _ = o[42];
        }
    }

    public static class E1
    {
        extension(object o)
        {
            public int this[int a]
            {
                get { System.Console.Write($"get({a}) "); return 0; }
            }
        }
    }
}

public static class E2
{
    extension(object o)
    {
        public int this[int a] => throw null;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "get(42)").VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_17()
    {
        // broken constraint
        var src = """
var o = new object();
_ = o[42];
E.get_Item(o, 42);

public static class E
{
    extension<T>(T t) where T : struct
    {
        public int this[int a] => throw null;
    }
}
""";

        // PROTOTYPE diagnostic quality, report that the constraint is not satisfied
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (2,5): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
            // _ = o[42];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "o[42]").WithArguments("object").WithLocation(2, 5),
            // (3,3): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'E.get_Item<T>(T, int)'
            // E.get_Item(o, 42);
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "get_Item").WithArguments("E.get_Item<T>(T, int)", "T", "object").WithLocation(3, 3));
    }

    [Fact]
    public void Indexing_18()
    {
        // no getter
        var src = """
var o = new object();
_ = o[42];
E.get_Item(o, 42);

_ = new C()[42];

public static class E
{
    extension(object o)
    {
        public int this[int a] { set { } }
    }
}

class C
{
    public int this[int a] { set { } }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (2,5): error CS0154: The property or indexer 'E.extension(object).this[int]' cannot be used in this context because it lacks the get accessor
            // _ = o[42];
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, "o[42]").WithArguments("E.extension(object).this[int]").WithLocation(2, 5),
            // (3,3): error CS0117: 'E' does not contain a definition for 'get_Item'
            // E.get_Item(o, 42);
            Diagnostic(ErrorCode.ERR_NoSuchMember, "get_Item").WithArguments("E", "get_Item").WithLocation(3, 3),
            // (5,5): error CS0154: The property or indexer 'C.this[int]' cannot be used in this context because it lacks the get accessor
            // _ = new C()[42];
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, "new C()[42]").WithArguments("C.this[int]").WithLocation(5, 5));
    }

    [Fact]
    public void Indexing_19()
    {
        // inaccessible, file-scope
        var src = """
var o = new object();
_ = o[42];
""";

        var src2 = """
file static class E
{
    extension(object o)
    {
        public int this[int a] => throw null;
    }
}
""";

        CreateCompilation([src, (src2, "program.cs")]).VerifyEmitDiagnostics(
            // (2,5): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
            // _ = o[42];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "o[42]").WithArguments("object").WithLocation(2, 5));
    }

    [Fact]
    public void Indexing_20()
    {
        // constant receiver
        var src = """
42[43] = 10;

static class E
{
    extension(int i)
    {
        public int this[int j] { get => 0; set { } }
    }
}
""";

        // Tracked by https://github.com/dotnet/roslyn/issues/79451 : consider adjusting receiver requirements for extension members
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,1): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            // 42[43] = 10;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "42[43]").WithLocation(1, 1));
    }

    [Fact]
    public void Indexing_21()
    {
        // ref receiver parameter
        var src = """
42[43] = 10;

static class E
{
    extension(ref int i)
    {
        public int this[int j] { get => 0; set { } }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,1): error CS1510: A ref or out value must be an assignable variable
            // 42[43] = 10;
            Diagnostic(ErrorCode.ERR_RefLvalueExpected, "42").WithLocation(1, 1));
    }

    [Fact]
    public void Indexing_22()
    {
        // ref receiver parameter, variable
        var src = """
int i = 42;
i[43] = 10;
System.Console.WriteLine(i);

static class E
{
    extension(ref int i)
    {
        public int this[int j] { set { System.Console.Write($"set({j}, {value}) "); i = 100; } }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("set(43, 10) 100")).VerifyDiagnostics();
    }

    [Theory]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void Indexing_23(string refKind)
    {
        // ref readonly receiver parameter, variable
        var src = $$"""
int i = 42;
i[43] = 10;

static class E
{
    extension({{refKind}} int i)
    {
        public int this[int j] { set { System.Console.Write($"set({j}, {value}) "); } }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("set(43, 10)")).VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_24()
    {
        // ref and out parameters in indexer
        var src = """
static class E
{
    extension(int i)
    {
        public int this[int j, ref int k, out int l] { set => throw null; }
    }
}

class C
{
    public int this[int j, ref int k, out int l] { set => throw null; }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (11,28): error CS0631: ref and out are not valid in this context
            //     public int this[int j, ref int k, out int l] { set => throw null; }
            Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(11, 28),
            // (11,39): error CS0631: ref and out are not valid in this context
            //     public int this[int j, ref int k, out int l] { set => throw null; }
            Diagnostic(ErrorCode.ERR_IllegalRefParam, "out").WithLocation(11, 39),
            // (5,32): error CS0631: ref and out are not valid in this context
            //         public int this[int j, ref int k, out int l] { set => throw null; }
            Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(5, 32),
            // (5,43): error CS0631: ref and out are not valid in this context
            //         public int this[int j, ref int k, out int l] { set => throw null; }
            Diagnostic(ErrorCode.ERR_IllegalRefParam, "out").WithLocation(5, 43));
    }

    [Fact]
    public void Indexing_25()
    {
        // in parameters in indexer
        var src = """
int i = 42;
i[k: 43, j: 44] = 100;

static class E
{
    extension(int i)
    {
        public int this[in int j, in int k] { set { System.Console.WriteLine($"set({j}, {k}, {value})"); } }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("set(44, 43, 100)")).VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_26()
    {
        // in parameters in indexer
        var src = """
int i = 42;
i[j: 44] = 100;

static class E
{
    extension(int i)
    {
        public int this[in int j, in int k = 43] { set { System.Console.WriteLine($"set({j}, {k}, {value})"); } }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("set(44, 43, 100)")).VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_27()
    {
        // in parameters in indexer
        var src = """
int i = 42;
i[j: 44] = 100;

static class E
{
    extension(in int i)
    {
        public int this[in int j, in int k = 43] { set { System.Console.WriteLine($"set({j}, {k}, {value})"); } }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("set(44, 43, 100)")).VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_28()
    {
        // ref-returning indexer
        var src = """
42[43] = 10;
System.Console.WriteLine(E.field);

static class E
{
    public static int field = 42;
    extension(int i)
    {
        public ref int this[int j] { get { return ref E.field; } }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("10")).VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_29()
    {
        var src = """
int x = 42;
_ = x[43];
x[43] = 10;

static class E
{
    public static int field = 42;
    extension(int i)
    {
        public int this[int j]
        {
            get => 0;
            private set { }
        }

        public void M()
        {
            int x = 42;
            _ = x[43];
            x[43] = 10;
        }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (3,1): error CS0272: The property or indexer 'E.extension(int).this[int]' cannot be used in this context because the set accessor is inaccessible
            // x[43] = 10;
            Diagnostic(ErrorCode.ERR_InaccessibleSetter, "x[43]").WithArguments("E.extension(int).this[int]").WithLocation(3, 1));
    }

    [Fact]
    public void Indexing_30()
    {
        var src = """
_ = 42[43];

static class E
{
    extension(ref int i)
    {
        public int this[int j]
        {
            get => 0;
        }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,5): error CS1510: A ref or out value must be an assignable variable
            // _ = 42[43];
            Diagnostic(ErrorCode.ERR_RefLvalueExpected, "42").WithLocation(1, 5));
    }

    [Fact]
    public void Indexing_31()
    {
        var src = """
_ = new Derived()[new Base()];

static class E
{
    extension<T>(T t1)
    {
        public int this[T t2]
        {
            get => 0;
        }
    }
}

public class Base { }
public class Derived : Base { }
""";

        var comp = CreateCompilation(src).VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "new Derived()[new Base()]");
        AssertEx.Equal("E.extension<Base>(Base).this[Base]", model.GetSymbolInfo(memberAccess).Symbol.ToDisplayString());
    }

    [Fact]
    public void Indexing_32()
    {
        var src = """
new Derived()[42] = new Base();
E.set_Item(new Derived(), 42, new Base());

static class E
{
    extension<T>(T t)
    {
        public T this[int i]
        {
            set { }
        }
    }
}

public class Base { }
public class Derived : Base { }
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,21): error CS0266: Cannot implicitly convert type 'Base' to 'Derived'. An explicit conversion exists (are you missing a cast?)
            // new Derived()[42] = new Base();
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new Base()").WithArguments("Base", "Derived").WithLocation(1, 21));
    }

    [Fact]
    public void Indexing_33()
    {
        var src = """
var result = new C()[42];
System.Console.WriteLine(result);

interface I { }
interface Indirect : I { }
class C : Indirect { }

static class E
{
    extension(I i)
    {
        public string this[int j] => "ran";
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var indexing = GetSyntax<ElementAccessExpressionSyntax>(tree, "new C()[42]");
        AssertEx.Equal("System.String E.<G>$3EADBD08A82F6ABA9495623CB335729C.this[System.Int32 j] { get; }", model.GetSymbolInfo(indexing).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(indexing));
    }

    [Fact]
    public void Indexing_34()
    {
        // inapplicable instance indexer and applicable extension indexer
        var source = """
_ = new C()[b: 42];

class C
{
    public int this[int a] { get { throw null; } }
}

static class E1
{
    extension(C c)
    {
        public int this[int b] { get { System.Console.Write($"E1.get_Item({b})"); return 0; } }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        CompileAndVerify(comp, expectedOutput: "E1.get_Item(42)");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "new C()[b: 42]");
        AssertEx.Equal("E1.extension(C).this[int]", model.GetSymbolInfo(memberAccess).Symbol.ToDisplayString());
        // PROTOTYPE public API, should probably return both candidates (see InstanceMethodInvocation_ArgumentName)
        AssertEx.SequenceEqual([], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
    }

    [Fact]
    public void Indexing_35()
    {
        // inapplicable inner extension indexer and applicable extension indexer
        var source = """
using N;

_ = new C()[b: 42];

class C { }

static class E1
{
    extension(C c)
    {
        public int this[int a] { get { throw null; } }
    }
}

namespace N
{
    static class E2
    {
        extension(C c)
        {
            public int this[int b] { get { System.Console.Write($"E2.get_Item({b})"); return 0; } }
        }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        CompileAndVerify(comp, expectedOutput: "E2.get_Item(42)");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "new C()[b: 42]");
        AssertEx.Equal("N.E2.extension(C).this[int]", model.GetSymbolInfo(memberAccess).Symbol.ToDisplayString());
        // PROTOTYPE public API, should probably return both candidates (see InstanceMethodInvocation_ArgumentName_02)
        AssertEx.SequenceEqual([], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
    }

    [Fact]
    public void Indexing_36()
    {
        // prefer more specific extension indexer
        var source = """
System.Console.Write(new C()[42]);

class Base { }

class C : Base { }

static class E1
{
    extension(Base b)
    {
        public int this[int i] { get { throw null; } }
    }
}

static class E2
{
    extension(C c)
    {
        public int this[int i] => i;
    }
}
""";
        var comp = CreateCompilation(source);
        CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "new C()[42]");
        AssertEx.Equal("E2.extension(C).this[int]", model.GetSymbolInfo(memberAccess).Symbol.ToDisplayString());
        // PROTOTYPE public API, should probably return both candidates (see InstanceMethodInvocation_AmbiguityWithExtensionOnBaseType_PreferMoreSpecific)
        AssertEx.SequenceEqual([], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
    }

    [Fact]
    public void Indexing_37()
    {
        var source = """
struct S1(Color Color)
{
    public void Test()
    {
        _ = Color[this];
    }
}

class Color { }

static class E
{
    extension(Color c)
    {
        public int this[S1 x, int y = 0] { get { System.Console.WriteLine("instance"); return 0; } }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();

        Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
    }

    [Fact]
    public void Indexing_38()
    {
        var source = """

class Base { }

class Derived : Base
{
    void Main()
    {
        _ = base[0];
        _ = (base)[0];
        _ = this[0];
    }
}

static class E
{
    extension(Base b)
    {
        public int this[int i] => 0;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (8,13): error CS0021: Cannot apply indexing with [] to an expression of type 'Base'
            //         _ = base[0];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "base[0]").WithArguments("Base").WithLocation(8, 13),
            // (9,14): error CS0175: Use of keyword 'base' is not valid in this context
            //         _ = (base)[0];
            Diagnostic(ErrorCode.ERR_BaseIllegal, "base").WithLocation(9, 14));
    }

    [Fact]
    public void Indexing_39()
    {
        var src = """
_ = new C()[0];

interface I<T> { }
class C : I<int>, I<string> { }

static class E
{
    extension<T>(I<T> i)
    {
        public int this[int j] => 0;
    }
}
""";
        var comp = CreateCompilation(src);
        // PROTOTYPE diagnostic quality, could report something better, like: CS0411: The type arguments for method 'E.extension<T>(I<T>).M()' cannot be inferred from the usage. Try specifying the type arguments explicitly.
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            // _ = new C()[0];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "new C()[0]").WithArguments("C").WithLocation(1, 5));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "new C()[0]");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess).CandidateSymbols);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [Fact]
    public void Indexing_40()
    {
        var missingSrc = """
public class Missing { }
""";
        var missingRef = CreateCompilation(missingSrc, assemblyName: "missing").EmitToImageReference();

        var derivedSrc = """
public class Derived : Missing { }
""";
        var derivedRef = CreateCompilation(derivedSrc, references: [missingRef]).EmitToImageReference();

        var src = """
_ = new Derived()[0];

static class E
{
    extension(Derived d)
    {
        public int this[int i] { get => 0; }
    }
}
""";
        var comp = CreateCompilation(src, references: [derivedRef]);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // _ = new Derived()[0];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "new Derived()[0]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 5));
    }

    [Fact]
    public void Indexing_41()
    {
        var src = """
new object()[0].field = 1;

public struct S
{
    public int field;
}
static class E
{
    extension(object o)
    {
        public S this[int i] { get => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS1612: Cannot modify the return value of 'E.extension(object).this[int]' because it is not a variable
            // new object()[0].field = 1;
            Diagnostic(ErrorCode.ERR_ReturnNotLValue, "new object()[0]").WithArguments("E.extension(object).this[int]").WithLocation(1, 1));
    }

    [Fact]
    public void Indexing_42()
    {
        // ref readonly extension parameter, constant receiver
        var src = """
_ = 42[0];

static class E
{
    extension(ref readonly int i)
    {
        public int this[int j] { get { System.Console.Write(42); return 0; } }
    }
}
""";
        CompileAndVerify(src, expectedOutput: "42").VerifyDiagnostics(
            // (1,5): warning CS9193: Argument 0 should be a variable because it is passed to a 'ref readonly' parameter
            // _ = 42[0];
            Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "42").WithArguments("0").WithLocation(1, 5));
    }

    [Fact]
    public void Indexing_43()
    {
        // ref readonly extension parameter, variable receiver
        var src = """
int i = 42;
_  = i[0];

static class E
{
    extension(ref readonly int i)
    {
        public int this[int j] { get { System.Console.Write(42); return 0; } }
    }
}
""";
        CompileAndVerify(src, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_44()
    {
        // ref readonly extension parameter, ref readonly receiver
        var src = """
var f = (ref readonly int i) => i[0];
int i = 42;
f(ref i);

static class E
{
    extension(ref readonly int i)
    {
        public int this[int j] { get { System.Console.Write(42); return 0; } }
    }
}
""";
        CompileAndVerify(src, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_45()
    {
        // inaccessible type argument
        var src = """
_ = new A.C()[0];

static class E1
{
    extension<T>(I<T> i)
    {
        public int this[int j] => 0;
    }
}

interface I<T> { }

class A
{
    private class B { }
    public class C : I<B> { }
}
""";
        var comp = CreateCompilation(src);
        // PROTOTYPE diagnostic quality, consider reporting something like CS0122: 'E1.extension<A.B>(I<A.B>).P' is inaccessible due to its protection level
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0021: Cannot apply indexing with [] to an expression of type 'A.C'
            // _ = new A.C()[0];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "new A.C()[0]").WithArguments("A.C").WithLocation(1, 5));
    }

    [Fact]
    public void Indexing_46()
    {
        // indexing on type
        var src = """
_ = C[0];

static class E
{
    extension(C c)
    {
        public int this[int j] => 0;
    }
}

class C { }
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,5): error CS0119: 'C' is a type, which is not valid in the given context
            // _ = C[0];
            Diagnostic(ErrorCode.ERR_BadSKunknown, "C").WithArguments("C", "type").WithLocation(1, 5));
    }

    [Fact]
    public void Indexing_47()
    {
        // extra ref
        var src = """
int i = 0;
_ = 0[ref i];

static class E
{
    extension(int i)
    {
        public int this[int j] => 0;
    }
}
""";
        // PROTOTYPE diagnostic quality, consider a better message for unexpected ref like CS1615: Argument 2 may not be passed with the 'ref' keyword
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (2,5): error CS0021: Cannot apply indexing with [] to an expression of type 'int'
            // _ = 0[ref i];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "0[ref i]").WithArguments("int").WithLocation(2, 5));
    }

    [Fact]
    public void Indexing_48()
    {
        // indexing null
        var src = """
_ = null[""];

static class E
{
    extension<T>(T t1)
    {
        public int this[T t2] => 0;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0021: Cannot apply indexing with [] to an expression of type '<null>'
            // _ = null[""];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, @"null[""""]").WithArguments("<null>").WithLocation(1, 5));
    }

    [Fact]
    public void Indexing_49()
    {
        // generic vs. non-generic extension indexer
        var src = """
_ = 42[43];

static class E
{
    extension<T>(T t)
    {
        public int this[int i] { get => throw null; }
    }

    extension(int i)
    {
        public int this[int j] { get { System.Console.Write("ran"); return 0; } }
    }
}
""";

        CompileAndVerify(src, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact]
    public void Indexing_50()
    {
        //public static class E
        //{
        //    extension(int i)
        //    {
        //        public void 'this[]'()
        //        {
        //        }
        //    }
        //}
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<M>$F4B4FFE41AB49E80A4ECF390CF6EB372'
            extends System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 i ) cil managed
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                ret
            }
        }

        .method public hidebysig instance void 'this[]' () cil managed
        {
            .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )

            newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            throw
        }
    }

    .method public hidebysig static void 'this[]' ( int32 i ) cil managed
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        ret
    }
}
""" + ExtensionMarkerAttributeIL;

        var src = """
_ = 42[43];
""";

        CreateCompilationWithIL(src, ilSrc).VerifyEmitDiagnostics(
            // (1,5): error CS0021: Cannot apply indexing with [] to an expression of type 'int'
            // _ = 42[43];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "42[43]").WithArguments("int").WithLocation(1, 5));
        // PROTOTYPE verify candidates from GetMemberGroup
    }

    [Fact(Skip = "PROTOTYPE existing crash")]
    public void Indexing_51()
    {
        //public class C
        //{
        //    public void 'this[]' () { }
        //}
        var ilSrc = """
.class public auto ansi beforefieldinit C
    extends System.Object
{
    .method public hidebysig instance void 'this[]' () cil managed
    {
        ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
""";

        var src = """
_ = new C()[43];
""";

        CreateCompilationWithIL(src, ilSrc).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Indexing_52()
    {
        // two ambiguous/applicable candidates mask an outer applicable candidate
        var src = """
using N;

_ = 42[new C()];

static class E1
{
    extension(int i)
    {
        public int this[I1 i1] { get => throw null; }
        public int this[I2 i2] { get => throw null; }
    }
}

interface I1 { }
interface I2 { }
class C : I1, I2 { }

namespace N
{
    static class E2
    {
        extension(int i)
        {
            public int this[I1 i1] { get => throw null; }
        }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (3,5): error CS0021: Cannot apply indexing with [] to an expression of type 'int'
            // _ = 42[new C()];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "42[new C()]").WithArguments("int").WithLocation(3, 5));
    }

    [Fact]
    public void Indexing_53()
    {
        // convert receiver, struct
        var src = """
S s = new S();
s[0] = 1;

public static class E
{
    extension(object o)
    {
        public int this[int i]
        {
            set { System.Console.Write($"set({i}, {value}) "); }
        }
    }
}

struct S { }
""";

        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp, expectedOutput: "set(0, 1)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var setterAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "s[0]");
        var typeInfo = model.GetTypeInfo(setterAccess.Expression);
        AssertEx.Equal("S", typeInfo.Type.ToTestDisplayString());
        AssertEx.Equal("System.Object", typeInfo.ConvertedType.ToTestDisplayString());

        // receiver is boxed
        verifier.VerifyIL("<top-level-statements-entry-point>", """
{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "S"
  IL_0008:  ldloc.0
  IL_0009:  box        "S"
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.1
  IL_0010:  call       "void E.set_Item(object, int, int)"
  IL_0015:  ret
}
""");
    }

    [Fact]
    public void Indexing_54()
    {
        // inaccessible, private
        var src = """
var o = new object();
_ = o[42];

static class E
{
    extension(object o)
    {
        private int this[int a] => throw null;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (2,5): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
            // _ = o[42];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "o[42]").WithArguments("object").WithLocation(2, 5));
    }

    [Fact]
    public void Indexing_55()
    {
        var src = """
System.Console.Write(""[""]);

public static class E
{
    extension(string s1)
    {
        public int this[string s2] { get => 42; }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("42"), verify: Verification.Skipped).VerifyDiagnostics();
    }

    [Fact]
    public void BadContainer_01()
    {
        // lacking static enclosing type
        var src = """
_ = 42[43];

extension(int i)
{
    public int this[int j]
    {
        get => 0;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,5): error CS0021: Cannot apply indexing with [] to an expression of type 'int'
            // _ = 42[43];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "42[43]").WithArguments("int").WithLocation(1, 5),
            // (3,1): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            // extension(int i)
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(3, 1));
    }

    [Fact]
    public void BadContainer_02()
    {
        // nested in extension block
        var src = """
_ = 42[43];

static class E
{
    extension(int i)
    {
        extension(int i)
        {
            public int this[int j]
            {
                get => 0;
            }
        }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,5): error CS0021: Cannot apply indexing with [] to an expression of type 'int'
            // _ = 42[43];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "42[43]").WithArguments("int").WithLocation(1, 5),
            // (7,9): error CS9282: This member is not allowed in an extension block
            //         extension(int i)
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "extension").WithLocation(7, 9));
    }

    [Fact]
    public void BadContainer_03()
    {
        // in nested type
        var src = """
_ = 42[43];

static class E
{
    static class Nested
    {
        extension(int i)
        {
            public int this[int j]
            {
                get => 0;
            }
        }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,5): error CS0021: Cannot apply indexing with [] to an expression of type 'int'
            // _ = 42[43];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "42[43]").WithArguments("int").WithLocation(1, 5),
            // (7,9): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            //         extension(int i)
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(7, 9));
    }

    [Fact]
    public void BadContainer_04()
    {
        // in generic type
        var src = """
_ = 42[43];

static class E<T>
{
    extension(int i)
    {
        public int this[int j]
        {
            get => 0;
        }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,5): error CS0021: Cannot apply indexing with [] to an expression of type 'int'
            // _ = 42[43];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "42[43]").WithArguments("int").WithLocation(1, 5),
            // (5,5): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            //     extension(int i)
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(5, 5));
    }

    [Fact]
    public void BadContainer_05()
    {
        // in non-static enclosing type
        var src = """
_ = 42[43];

class E
{
    extension(int i)
    {
        public int this[int j]
        {
            get => 0;
        }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,5): error CS0021: Cannot apply indexing with [] to an expression of type 'int'
            // _ = 42[43];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "42[43]").WithArguments("int").WithLocation(1, 5),
            // (5,5): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            //     extension(int i)
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(5, 5));
    }

    [Fact]
    public void BadContainer_06()
    {
        var src = """
_ = 42[43];

static class E
{
    extension(__arglist)
    {
        public int this[int j]
        {
            get => 0;
        }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,5): error CS0021: Cannot apply indexing with [] to an expression of type 'int'
            // _ = 42[43];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "42[43]").WithArguments("int").WithLocation(1, 5),
            // (5,15): error CS1669: __arglist is not valid in this context
            //     extension(__arglist)
            Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(5, 15));
    }

    [Fact]
    public void ImplicitIndexIndexer_01()
    {
        // extension Index indexer takes precedence over implicit indexer
        var src = """
var c = new C();
_ = c[^1];
c[^2] = 10;

static class E
{
    extension(C c)
    {
        public int this[System.Index i]
        {
            get { System.Console.Write($"get({i}) "); return 42; }
            set { System.Console.Write($"set({i}, {value}) "); }
        }
    }
}

class C
{
    public int Length => throw null;
    public int this[int i]
    {
        get => throw null;
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("get(^1) set(^2, 10)"), verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void ImplicitIndexIndexer_02()
    {
        // extension Length doesn't count towards implicit Index indexer
        var src = """
var c = new C();
_ = c[^1];
c[^2] = 10;

static class E
{
    extension(C c)
    {
        public int Length => throw null;
    }
}

class C
{
    public int this[int i]
    {
        get => throw null;
    }
}
""";

        // PROTOTYPE where should extension Length/Count count?
        CreateCompilation(src, targetFramework: TargetFramework.Net100).VerifyEmitDiagnostics(
            // (2,7): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            // _ = c[^1];
            Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "int").WithLocation(2, 7),
            // (3,3): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            // c[^2] = 10;
            Diagnostic(ErrorCode.ERR_BadArgType, "^2").WithArguments("1", "System.Index", "int").WithLocation(3, 3));
    }

    [Fact]
    public void ImplicitIndexIndexer_03()
    {
        // extension this[int] doesn't count towards implicit Index indexer
        var src = """
var c = new C();
_ = c[^1];
c[^2] = 10;

static class E
{
    extension(C c)
    {
        public int this[int i]
        {
            get => throw null;
        }
    }
}

class C
{
    public int Length => throw null;
}
""";

        CreateCompilation(src, targetFramework: TargetFramework.Net100).VerifyEmitDiagnostics(
            // (2,5): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            // _ = c[^1];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "c[^1]").WithArguments("C").WithLocation(2, 5),
            // (3,1): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            // c[^2] = 10;
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "c[^2]").WithArguments("C").WithLocation(3, 1));
    }

    [Fact]
    public void ImplicitRangeIndexer_01()
    {
        // extension Range indexer takes precedence over implicit indexer
        var src = """
var c = new C();
_ = c[0..^1];
c[0..^1] = 10;

static class E
{
    extension(C c)
    {
        public int this[System.Range i]
        {
            get { System.Console.Write($"get({i}) "); return 42; }
            set { System.Console.Write($"set({i}, {value}) "); }
        }
    }
}

class C
{
    public int Length => throw null;
    public int Slice(int i, int j) => throw null;
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("get(0..^1) set(0..^1, 10)"), verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void ImplicitRangeIndexer_02()
    {
        // extension Length doesn't count towards implicit Range indexer
        var src = """
var c = new C();
_ = c[0..^1];

static class E
{
    extension(C c)
    {
        public int Length => throw null;
    }
}

class C
{
    public int Slice(int i, int j) => throw null;
}
""";

        // PROTOTYPE where should extension Length/Count count?
        CreateCompilation(src, targetFramework: TargetFramework.Net100).VerifyEmitDiagnostics(
            // (2,5): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            // _ = c[0..^1];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "c[0..^1]").WithArguments("C").WithLocation(2, 5));
    }

    [Fact]
    public void ImplicitRangeIndexer_03()
    {
        // extension Slice doesn't count towards implicit Range indexer
        var src = """
var c = new C();
_ = c[0..^1];

static class E
{
    extension(C c)
    {
        public int Slice(int i, int j) => throw null;
    }
}

class C
{
    public int Length => throw null;
}
""";

        CreateCompilation(src, targetFramework: TargetFramework.Net100).VerifyEmitDiagnostics(
            // (2,5): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            // _ = c[0..^1];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "c[0..^1]").WithArguments("C").WithLocation(2, 5));
    }

    [Fact]
    public void ObjectInitializer_01()
    {
        var src = """
var c = new C() { [0] = 1 };

static class E
{
    extension(C c)
    {
        public int this[int i] { set { System.Console.Write($"set({i}, {value}) "); } }
    }
}

class C { }
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "set(0, 1)").VerifyDiagnostics();
    }

    [Fact]
    public void ObjectInitializer_02()
    {
        // boxed receiver
        var src = """
var c = new S() { [0] = 1 };

static class E
{
    extension(object o)
    {
        public int this[int i] { set { System.Console.Write($"set({i}, {value}) "); } }
    }
}

struct S { }
""";

        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp, expectedOutput: "set(0, 1)").VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
{
  // Code size       24 (0x18)
  .maxstack  4
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "S"
  IL_0008:  ldloc.0
  IL_0009:  dup
  IL_000a:  box        "S"
  IL_000f:  ldc.i4.0
  IL_0010:  ldc.i4.1
  IL_0011:  call       "void E.set_Item(object, int, int)"
  IL_0016:  pop
  IL_0017:  ret
}
""");
    }

    [Fact]
    public void WithInitializer_01()
    {
        var src = """
var c = new S() with { [0] = 1 };

public static class E
{
    extension(S s)
    {
        public int this[int i] { set { } }
    }
}

public struct S { }
""";

        // Tracked by https://github.com/dotnet/roslyn/issues/79451 : consider adjusting receiver requirements for extension members
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,24): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            // var c = new S() with { [0] = 1 };
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "[0]").WithLocation(1, 24),
            // (1,24): error CS0747: Invalid initializer member declarator
            // var c = new S() with { [0] = 1 };
            Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "[0] = 1").WithLocation(1, 24));

        src = """
var c = new S() with { [0] = 1 };

public struct S
{
    public int this[int i] { set { } }
}
""";

        // The first diagnostic is unexpected
        // Tracked by https://github.com/dotnet/roslyn/issues/81666
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,24): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            // var c = new S() with { [0] = 1 };
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "[0]").WithLocation(1, 24),
            // (1,24): error CS0747: Invalid initializer member declarator
            // var c = new S() with { [0] = 1 };
            Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "[0] = 1").WithLocation(1, 24));
    }

    [Theory, CombinatorialData]
    public void ListPattern_01(bool useCompilationReference)
    {
        var libSrc = """
public class C { }

public static class E
{
    extension(C c)
    {
        public int Length => 3;
        public int this[System.Index i] { get { System.Console.WriteLine(i); return 0; } }
    }
}
""";
        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net70);
        var libRef = AsReference(libComp, useCompilationReference);

        var src = """
_ = new C() is [.., 1];
""";

        var comp = CreateCompilation(src, references: [libRef], targetFramework: TargetFramework.Net70, parseOptions: TestOptions.Regular14);
        comp.VerifyEmitDiagnostics(
            // (1,16): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            // _ = new C() is [.., 1];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[.., 1]").WithArguments("C").WithLocation(1, 16),
            // (1,16): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = new C() is [.., 1];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "[.., 1]").WithArguments("extension indexers").WithLocation(1, 16));

        // PROTOTYPE where should extension Length/Count count?
        comp = CreateCompilation(src, references: [libRef], targetFramework: TargetFramework.Net70);
        comp.VerifyEmitDiagnostics(
            // (1,16): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            // _ = new C() is [.., 1];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[.., 1]").WithArguments("C").WithLocation(1, 16));

        comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net70, parseOptions: TestOptions.Regular14);
        comp.VerifyEmitDiagnostics(
            // (1,16): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            // _ = new C() is [.., 1];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[.., 1]").WithArguments("C").WithLocation(1, 16),
            // (8,20): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         public int this[System.Index i] { get { System.Console.WriteLine(i); return 0; } }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "this").WithArguments("extension indexers").WithLocation(8, 20));
    }

    [Fact]
    public void ListPattern_02()
    {
        // boxed receiver
        var src = """
_ = new S() is [.., 1];

public struct S
{
    public int Length => 3;
}

public static class E
{
    extension(object o)
    {
        public int this[System.Index i] { get { System.Console.WriteLine(i); return 0; } }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        var verifier = CompileAndVerify(comp, expectedOutput: ExpectedOutput("^1"), verify: Verification.FailsPEVerify).VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "S"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "int S.Length.get"
  IL_000f:  ldc.i4.1
  IL_0010:  blt.s      IL_0029
  IL_0012:  ldloc.0
  IL_0013:  box        "S"
  IL_0018:  ldc.i4.1
  IL_0019:  ldc.i4.1
  IL_001a:  newobj     "System.Index..ctor(int, bool)"
  IL_001f:  call       "int E.get_Item(object, System.Index)"
  IL_0024:  ldc.i4.1
  IL_0025:  ceq
  IL_0027:  br.s       IL_002a
  IL_0029:  ldc.i4.0
  IL_002a:  pop
  IL_002b:  ret
}
""");
    }

    [Fact]
    public void ListPattern_03()
    {
        // nullable value type receiver
        var src = """
System.Console.Write(E.Test(null)); 
System.Console.Write(E.Test(new S()));

public struct S
{
    public int Length => 3;
}

public static class E
{
    public static bool Test(S? s)
    {
        return s is [.., 1];
    }

    extension(object o)
    {
        public int this[System.Index i] { get { System.Console.Write($" {i} "); return 0; } }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        var verifier = CompileAndVerify(comp, expectedOutput: ExpectedOutput("False ^1 False"), verify: Verification.FailsPEVerify).VerifyDiagnostics();
        verifier.VerifyIL("E.Test", """
{
  // Code size       51 (0x33)
  .maxstack  3
  .locals init (S V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "readonly bool S?.HasValue.get"
  IL_0007:  brfalse.s  IL_0031
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       "readonly S S?.GetValueOrDefault()"
  IL_0010:  stloc.0
  IL_0011:  ldloca.s   V_0
  IL_0013:  call       "int S.Length.get"
  IL_0018:  ldc.i4.1
  IL_0019:  blt.s      IL_0031
  IL_001b:  ldloc.0
  IL_001c:  box        "S"
  IL_0021:  ldc.i4.1
  IL_0022:  ldc.i4.1
  IL_0023:  newobj     "System.Index..ctor(int, bool)"
  IL_0028:  call       "int E.get_Item(object, System.Index)"
  IL_002d:  ldc.i4.1
  IL_002e:  ceq
  IL_0030:  ret
  IL_0031:  ldc.i4.0
  IL_0032:  ret
}
""");
    }

    [Fact]
    public void ListPattern_04()
    {
        // optional parameter
        var src = """
_ = new S() is [.., 1];

public struct S
{
    public int Length => 3;
}

public static class E
{
    extension(object o)
    {
        public int this[System.Index i, int i2 = 42] { get { System.Console.WriteLine((i, i2)); return 0; } }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("(^1, 42)"), verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void SpreadPattern_01(bool useCompilationReference)
    {
        var libSrc = """
public class C { }

public static class E
{
    extension(C c)
    {
        public int Length => 3;
        public int this[System.Index i] { get { System.Console.WriteLine(i); return 0; } }
        public int this[System.Range r] { get { System.Console.WriteLine(r); return 0; } }
    }
}
""";
        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net70);
        var libRef = AsReference(libComp, useCompilationReference);

        var src = """
_ = new C() is [_, .. var x];
""";

        var comp = CreateCompilation(src, references: [libRef], targetFramework: TargetFramework.Net70, parseOptions: TestOptions.Regular14);
        comp.VerifyEmitDiagnostics(
            // (1,16): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            // _ = new C() is [_, .. var x];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[_, .. var x]").WithArguments("C").WithLocation(1, 16),
            // (1,16): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = new C() is [_, .. var x];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "[_, .. var x]").WithArguments("extension indexers").WithLocation(1, 16),
            // (1,20): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = new C() is [_, .. var x];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, ".. var x").WithArguments("extension indexers").WithLocation(1, 20));

        // PROTOTYPE where should extension Length/Count count?
        comp = CreateCompilation(src, references: [libRef], targetFramework: TargetFramework.Net70);
        comp.VerifyEmitDiagnostics(
            // (1,16): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            // _ = new C() is [_, .. var x];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[_, .. var x]").WithArguments("C").WithLocation(1, 16));

        comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net70, parseOptions: TestOptions.Regular14);
        comp.VerifyEmitDiagnostics(
            // (1,16): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            // _ = new C() is [_, .. var x];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[_, .. var x]").WithArguments("C").WithLocation(1, 16),
            // (8,20): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         public int this[System.Index i] { get { System.Console.WriteLine(i); return 0; } }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "this").WithArguments("extension indexers").WithLocation(8, 20),
            // (9,20): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         public int this[System.Range r] { get { System.Console.WriteLine(r); return 0; } }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "this").WithArguments("extension indexers").WithLocation(9, 20));
    }

    [Fact]
    public void SpreadPattern_02()
    {
        // boxed receiver
        var src = """
_ = new S() is [_, .. var x];

public struct S
{
    public int Length => 3;
}

public static class E
{
    extension(object o)
    {
        public int this[System.Index i] { get { System.Console.WriteLine(i); return 0; } }
        public int this[System.Range r] { get { System.Console.WriteLine(r); return 0; } }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        var verifier = CompileAndVerify(comp, expectedOutput: ExpectedOutput("1..^0"), verify: Verification.FailsPEVerify).VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
{
  // Code size       55 (0x37)
  .maxstack  4
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "S"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "int S.Length.get"
  IL_000f:  ldc.i4.1
  IL_0010:  blt.s      IL_0034
  IL_0012:  ldloc.0
  IL_0013:  box        "S"
  IL_0018:  ldc.i4.1
  IL_0019:  ldc.i4.0
  IL_001a:  newobj     "System.Index..ctor(int, bool)"
  IL_001f:  ldc.i4.0
  IL_0020:  ldc.i4.1
  IL_0021:  newobj     "System.Index..ctor(int, bool)"
  IL_0026:  newobj     "System.Range..ctor(System.Index, System.Index)"
  IL_002b:  call       "int E.get_Item(object, System.Range)"
  IL_0030:  pop
  IL_0031:  ldc.i4.1
  IL_0032:  br.s       IL_0035
  IL_0034:  ldc.i4.0
  IL_0035:  pop
  IL_0036:  ret
}
""");
    }

    [Theory, CombinatorialData]
    public void ConditionalAssignment_01(bool useCompilationReference)
    {
        var libSrc = """
public class C { }

public static class E
{
    extension(C c)
    {
        public int this[int i]
        {
            set { System.Console.WriteLine($"set_Item({i} {value})"); }
        }
    }
}
""";
        var libComp = CreateCompilation(libSrc);
        var libRef = AsReference(libComp, useCompilationReference);

        var src = """
C c = null;
c?[42] = 0;

c = new C();
c?[43] = 100;
""";

        var comp = CreateCompilation(src, references: [libRef], parseOptions: TestOptions.Regular14);
        comp.VerifyEmitDiagnostics(
            // (2,3): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // c?[42] = 0;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "[42]").WithArguments("extension indexers").WithLocation(2, 3),
            // (5,3): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // c?[43] = 100;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "[43]").WithArguments("extension indexers").WithLocation(5, 3));

        comp = CreateCompilation(src, references: [libRef], parseOptions: TestOptions.RegularNext);
        comp.VerifyEmitDiagnostics();

        comp = CreateCompilation(src, references: [libRef]);
        CompileAndVerify(comp, expectedOutput: "set_Item(43 100)").VerifyDiagnostics();

        comp = CreateCompilation([src, libSrc], parseOptions: TestOptions.Regular14);
        comp.VerifyEmitDiagnostics(
            // (7,20): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         public int this[int i]
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "this").WithArguments("extension indexers").WithLocation(7, 20));
    }

    [Fact]
    public void ConditionalAssignment_02()
    {
        // nullable value type receiver
        var src = """
E.Test(null, 42, 0);
E.Test(new S(), 43, 100);

public struct S { }

public static class E
{
    public static void Test(S? s, int index, int value)
    {
        s?[index] = value;
    }

    extension(S s)
    {
        public int this[int i]
        {
            set { System.Console.WriteLine($"set_Item({i} {value})"); }
        }
    }
}
""";

        // Tracked by https://github.com/dotnet/roslyn/issues/79451 : consider adjusting receiver requirements for extension members
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (10,11): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         s?[index] = value;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "[index]").WithLocation(10, 11));

        //var verifier = CompileAndVerify(comp, expectedOutput: "set_Item(43 100)").VerifyDiagnostics();
        //verifier.VerifyIL("E.Test", "");
    }

    [Theory, CombinatorialData]
    public void ConditionalAccess_01(bool useCompilationReference)
    {
        var libSrc = """
public class C { }

public static class E
{
    extension(C c)
    {
        public int this[int i]
        {
            get { System.Console.Write($" get_Item({i}) "); return 100; }
        }
    }
}
""";

        var libComp = CreateCompilation(libSrc);
        var libRef = AsReference(libComp, useCompilationReference);

        var src = """
C c = null;
System.Console.Write(c?[42] is null);

c = new C();
System.Console.Write(c?[43] is 100);
""";

        var comp = CreateCompilation(src, references: [libRef], parseOptions: TestOptions.Regular14);
        comp.VerifyEmitDiagnostics(
            // (2,24): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // System.Console.Write(c?[42] is null);
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "[42]").WithArguments("extension indexers").WithLocation(2, 24),
            // (5,24): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // System.Console.Write(c?[43] is 100);
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "[43]").WithArguments("extension indexers").WithLocation(5, 24));

        comp = CreateCompilation(src, references: [libRef], parseOptions: TestOptions.RegularNext);
        comp.VerifyEmitDiagnostics();

        comp = CreateCompilation(src, references: [libRef]);
        CompileAndVerify(comp, expectedOutput: "True get_Item(43) True").VerifyDiagnostics();

        comp = CreateCompilation([src, libSrc], parseOptions: TestOptions.Regular14);
        comp.VerifyEmitDiagnostics(
            // (7,20): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         public int this[int i]
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "this").WithArguments("extension indexers").WithLocation(7, 20));
    }

    [Fact]
    public void ConditionalAccess_02()
    {
        // nullable value type receiver
        var src = """
System.Console.Write(E.Test(null, 42) is null);

System.Console.Write(E.Test(new S(), 43) is 100);

public struct S { }

public static class E
{
    public static int? Test(S? s, int index)
    {
        return s?[index];
    }

    extension(S s)
    {
        public int this[int i]
        {
            get { System.Console.Write($" get_Item({i}) "); return 100; }
        }
    }
}
""";

        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp, expectedOutput: "True get_Item(43) True").VerifyDiagnostics();
        verifier.VerifyIL("E.Test", """
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (int? V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "bool S?.HasValue.get"
  IL_0007:  brtrue.s   IL_0013
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    "int?"
  IL_0011:  ldloc.0
  IL_0012:  ret
  IL_0013:  ldarga.s   V_0
  IL_0015:  call       "S S?.GetValueOrDefault()"
  IL_001a:  ldarg.1
  IL_001b:  call       "int E.get_Item(S, int)"
  IL_0020:  newobj     "int?..ctor(int)"
  IL_0025:  ret
} 
""");
    }

    [Fact]
    public void Nameof_01()
    {
        var src = """
object o = new object();
_ = nameof(o[0]);

public static class E
{
    extension(object o)
    {
        public int this[int i] => 0;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (2,12): error CS8081: Expression does not have a name.
            // _ = nameof(o[0]);
            Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "o[0]").WithLocation(2, 12));
    }

    [Fact]
    public void Nameof_02()
    {
        // setter-only indexer
        var src = """
object o = new object();
_ = nameof(o[0]);

public static class E
{
    extension(object o)
    {
        public int this[int i] { set { } }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (2,12): error CS8081: Expression does not have a name.
            // _ = nameof(o[0]);
            Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "o[0]").WithLocation(2, 12),
            // (2,12): error CS0154: The property or indexer 'E.extension(object).this[int]' cannot be used in this context because it lacks the get accessor
            // _ = nameof(o[0]);
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, "o[0]").WithArguments("E.extension(object).this[int]").WithLocation(2, 12));
    }

    [Fact]
    public void ORPA_01()
    {
        var source = """
_ = 42[43];

static class E
{
    extension(int i)
    {
        public int this[int j] => throw null;

        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        public int this[long l] { get { System.Console.Write("ran"); return 0; } }
    }
}
""";
        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact]
    public void ORPA_02()
    {
        var source = """
_ = 42[43];

static class E
{
    extension(int i)
    {
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        public int this[int j] { get { System.Console.Write("ran"); return 0; } }

        public int this[long l] => throw null;
    }
}
""";
        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact]
    public void ORPA_03()
    {
        var source = """
_ = 42[43];

static class E1
{
    extension(int i)
    {
        public int this[int j] { get { System.Console.Write("ran"); return 0; } }
    }
}

static class E2
{
    extension(int i)
    {
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        public int this[long l] => throw null;
    }
}
""";
        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact]
    public void Metadata_01()
    {
        var source = """
public static class E
{
    extension(int i)
    {
        public int this[int j] { get { return 42; } }
    }
}
""";
        var comp = CreateCompilation(source);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();

        verifier.VerifyTypeIL("E", """
.class public auto ansi abstract sealed beforefieldinit E
    extends [netstandard]System.Object
{
    .custom instance void [netstandard]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [netstandard]System.Object
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        .custom instance void [netstandard]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
            01 00 04 49 74 65 6d 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$F4B4FFE41AB49E80A4ECF390CF6EB372'
            extends [netstandard]System.Object
        {
            // Methods
            .method public hidebysig specialname static
                void '<Extension>$' (
                    int32 i
                ) cil managed
            {
                .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x2089
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$F4B4FFE41AB49E80A4ECF390CF6EB372'::'<Extension>$'
        } // end of class <M>$F4B4FFE41AB49E80A4ECF390CF6EB372
        // Methods
        .method public hidebysig specialname
            instance int32 get_Item (
                int32 j
            ) cil managed
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            // Method begins at RVA 0x2082
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [netstandard]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::get_Item
        // Properties
        .property instance int32 Item(
            int32 j
        )
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            .get instance int32 E/'<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::get_Item(int32)
        }
    } // end of class <G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69
    // Methods
    .method public hidebysig static
        int32 get_Item (
            int32 i,
            int32 j
        ) cil managed
    {
        // Method begins at RVA 0x207e
        // Code size 3 (0x3)
        .maxstack 8
        IL_0000: ldc.i4.s 42
        IL_0002: ret
    } // end of method E::get_Item
} // end of class E
""".Replace("[netstandard]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Theory, CombinatorialData]
    public void Metadata_02(bool useCompilationReference)
    {
        var libSrc = """
public static class E
{
    extension(int i)
    {
        public int this[int j]
        {
            get { System.Console.Write($"get({j}) "); return 42; }
            set { System.Console.Write($"set({j}, {value}) "); }
        }
    }
}
""";
        var libComp = CreateCompilation(libSrc);
        var libRef = AsReference(libComp, useCompilationReference);

        var src = """
int i = 0;
_ = i[43];
i[101] = 102;
""";
        var comp = CreateCompilation(src, [libRef]);
        CompileAndVerify(comp, expectedOutput: "get(43) set(101, 102)").VerifyDiagnostics();

        var indexer = comp.GlobalNamespace.GetTypeMember("E").GetTypeMember("").GetMembers().OfType<PropertySymbol>().Single();
        AssertEx.Equal("E.extension(int).this[int]", indexer.ToDisplayString());
        Assert.True(indexer.IsIndexer);
        AssertEx.Equal("E.extension(int).this[int].get", indexer.GetMethod.ToDisplayString());
        AssertEx.Equal("E.extension(int).this[int].set", indexer.SetMethod.ToDisplayString());

        comp = CreateCompilation(src, [libRef], parseOptions: TestOptions.Regular14);
        comp.VerifyEmitDiagnostics(
            // (2,5): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = i[43];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "i[43]").WithArguments("extension indexers").WithLocation(2, 5),
            // (3,1): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // i[101] = 102;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "i[101]").WithArguments("extension indexers").WithLocation(3, 1));
    }

    [Theory, CombinatorialData]
    public void Metadata_03(bool useCompilationReference)
    {
        // IndexerName attribute on single indexer
        var libSrc = """
public static class E
{
    extension(int i)
    {
        [System.Runtime.CompilerServices.IndexerName("MyIndexer")]
        public int this[int j]
        {
            get { System.Console.Write($"get({j}) "); return 42; }
            set { System.Console.Write($"set({j}, {value}) "); }
        }
    }
}
""";
        var libComp = CreateCompilation(libSrc);
        var verifier = CompileAndVerify(libComp).VerifyDiagnostics();

        verifier.VerifyTypeIL("E", """
.class public auto ansi abstract sealed beforefieldinit E
    extends [netstandard]System.Object
{
    .custom instance void [netstandard]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [netstandard]System.Object
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        .custom instance void [netstandard]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
            01 00 09 4d 79 49 6e 64 65 78 65 72 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$F4B4FFE41AB49E80A4ECF390CF6EB372'
            extends [netstandard]System.Object
        {
            // Methods
            .method public hidebysig specialname static
                void '<Extension>$' (
                    int32 i
                ) cil managed
            {
                .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x20bb
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$F4B4FFE41AB49E80A4ECF390CF6EB372'::'<Extension>$'
        } // end of class <M>$F4B4FFE41AB49E80A4ECF390CF6EB372
        // Methods
        .method public hidebysig specialname
            instance int32 get_MyIndexer (
                int32 j
            ) cil managed
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            // Method begins at RVA 0x20b4
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [netstandard]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::get_MyIndexer
        .method public hidebysig specialname
            instance void set_MyIndexer (
                int32 j,
                int32 'value'
            ) cil managed
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            // Method begins at RVA 0x20b4
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [netstandard]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::set_MyIndexer
        // Properties
        .property instance int32 MyIndexer(
            int32 j
        )
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            .get instance int32 E/'<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::get_MyIndexer(int32)
            .set instance void E/'<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::set_MyIndexer(int32, int32)
        }
    } // end of class <G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69
    // Methods
    .method public hidebysig static
        int32 get_MyIndexer (
            int32 i,
            int32 j
        ) cil managed
    {
        // Method begins at RVA 0x207e
        // Code size 24 (0x18)
        .maxstack 8
        IL_0000: ldstr "get({0}) "
        IL_0005: ldarg.1
        IL_0006: box [netstandard]System.Int32
        IL_000b: call string [netstandard]System.String::Format(string, object)
        IL_0010: call void [netstandard]System.Console::Write(string)
        IL_0015: ldc.i4.s 42
        IL_0017: ret
    } // end of method E::get_MyIndexer
    .method public hidebysig static
        void set_MyIndexer (
            int32 i,
            int32 j,
            int32 'value'
        ) cil managed
    {
        // Method begins at RVA 0x2097
        // Code size 28 (0x1c)
        .maxstack 8
        IL_0000: ldstr "set({0}, {1}) "
        IL_0005: ldarg.1
        IL_0006: box [netstandard]System.Int32
        IL_000b: ldarg.2
        IL_000c: box [netstandard]System.Int32
        IL_0011: call string [netstandard]System.String::Format(string, object, object)
        IL_0016: call void [netstandard]System.Console::Write(string)
        IL_001b: ret
    } // end of method E::set_MyIndexer
} // end of class E
""".Replace("[netstandard]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));

        var src = """
int i = 0;
_ = i[43];
i[101] = 102;

E.get_MyIndexer(i, 43);
E.set_MyIndexer(i, 101, 102);
""";
        var comp = CreateCompilation(src, [AsReference(libComp, useCompilationReference)]);
        CompileAndVerify(comp, expectedOutput: "get(43) set(101, 102) get(43) set(101, 102)").VerifyDiagnostics();

        var indexer = comp.GlobalNamespace.GetTypeMember("E").GetTypeMember("").GetMembers().OfType<PropertySymbol>().Single();
        AssertEx.Equal("E.extension(int).this[int]", indexer.ToDisplayString());
        Assert.True(indexer.IsIndexer);
        AssertEx.Equal("E.extension(int).this[int].get", indexer.GetMethod.ToDisplayString());
        AssertEx.Equal("E.extension(int).this[int].set", indexer.SetMethod.ToDisplayString());
    }

    [Theory, CombinatorialData]
    public void Metadata_04(bool useCompilationReference)
    {
        // Matching IndexerName attributes on both indexers
        var libSrc = """
public static class E
{
    extension(int i)
    {
        [System.Runtime.CompilerServices.IndexerName("MyIndexer")]
        public int this[int j]
        {
            get { System.Console.Write($"get({j}) "); return 42; }
            set { System.Console.Write($"set({j}, {value}) "); }
        }

        [System.Runtime.CompilerServices.IndexerName("MyIndexer")]
        public int this[string s]
        {
            get { System.Console.Write($"get2({s}) "); return 42; }
            set { System.Console.Write($"set2({s}, {value}) "); }
        }
    }
}
""";
        var libComp = CreateCompilation(libSrc);
        var verifier = CompileAndVerify(libComp).VerifyDiagnostics();

        var src = """
int i = 0;
_ = i[43];
i[101] = 102;

E.get_MyIndexer(i, 43);
E.set_MyIndexer(i, 101, 102);

_ = i["A"];
i["B"] = 102;

E.get_MyIndexer(i, "A");
E.set_MyIndexer(i, "B", 102);
""";
        var comp = CreateCompilation(src, [AsReference(libComp, useCompilationReference)]);
        CompileAndVerify(comp, expectedOutput: "get(43) set(101, 102) get(43) set(101, 102) get2(A) set2(B, 102) get2(A) set2(B, 102)").VerifyDiagnostics();

        var indexers = comp.GlobalNamespace.GetTypeMember("E").GetTypeMember("").GetMembers().OfType<PropertySymbol>().ToArray();
        AssertEx.Equal("E.extension(int).this[int]", indexers[0].ToDisplayString());
        Assert.True(indexers[0].IsIndexer);

        AssertEx.Equal("E.extension(int).this[string]", indexers[1].ToDisplayString());
        Assert.True(indexers[1].IsIndexer);
    }

    [Fact]
    public void IndexerName_01()
    {
        // IndexerName attribute on one of the indexers
        var source = """
public static class E
{
    extension((int a, int b) t)
    {
        public int this[string s]
        {
            get => throw null;
            set => throw null;
        }
    }

    extension((int c, int d) t)
    {
        [System.Runtime.CompilerServices.IndexerName("MyIndexer")]
        public int this[int j]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";
        CreateCompilation(source).VerifyEmitDiagnostics(
            // (15,20): error CS0668: Two indexers have different names; the IndexerName attribute must be used with the same name on every indexer within a type
            //         public int this[int j]
            Diagnostic(ErrorCode.ERR_InconsistentIndexerNames, "this").WithLocation(15, 20));
    }

    [Fact]
    public void IndexerName_02()
    {
        // IndexerName attribute uses same name as extension parameter
        var source = """
public static class E
{
    extension(int parameter)
    {
        [System.Runtime.CompilerServices.IndexerName("parameter")]
        public int this[int i]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";
        CreateCompilation(source).VerifyEmitDiagnostics();
    }

    [Fact]
    public void IndexerName_03()
    {
        // IndexerName attribute uses same name as static enclosing class
        var source = """
public static class E
{
    extension(int i)
    {
        [System.Runtime.CompilerServices.IndexerName("E")]
        public int this[int j]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";
        CreateCompilation(source).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Extern_03()
    {
        var source = """
using System.Runtime.InteropServices;
static class E
{
    extension(int i)
    {
        extern int this[int j]
        {
            [DllImport("something.dll")]
            get;
            [DllImport("something.dll")]
            set;
        }
    }
}
""";
        var verifier = CompileAndVerify(source).VerifyDiagnostics();
        // Note: skeleton methods have "throw" bodies and lack pinvokeimpl/preservesig. Implementation methods have pinvokeimpl/preservesig and no body.
        verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [netstandard]System.Object
{
    .custom instance void [netstandard]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [netstandard]System.Object
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        .custom instance void [netstandard]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
            01 00 04 49 74 65 6d 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$F4B4FFE41AB49E80A4ECF390CF6EB372'
            extends [netstandard]System.Object
        {
            // Methods
            .method private hidebysig specialname static
                void '<Extension>$' (
                    int32 i
                ) cil managed
            {
                .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x2085
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$F4B4FFE41AB49E80A4ECF390CF6EB372'::'<Extension>$'
        } // end of class <M>$F4B4FFE41AB49E80A4ECF390CF6EB372
        // Methods
        .method private hidebysig specialname
            instance int32 get_Item (
                int32 j
            ) cil managed
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            // Method begins at RVA 0x207e
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [netstandard]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::get_Item
        .method private hidebysig specialname
            instance void set_Item (
                int32 j,
                int32 'value'
            ) cil managed
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            // Method begins at RVA 0x207e
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [netstandard]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::set_Item
        // Properties
        .property instance int32 Item(
            int32 j
        )
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            .get instance int32 E/'<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::get_Item(int32)
            .set instance void E/'<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::set_Item(int32, int32)
        }
    } // end of class <G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69
    // Methods
    .method private hidebysig static pinvokeimpl("something.dll" winapi)
        int32 get_Item (
            int32 i,
            int32 j
        ) cil managed preservesig
    {
    } // end of method E::get_Item
    .method private hidebysig static pinvokeimpl("something.dll" winapi)
        void set_Item (
            int32 i,
            int32 j,
            int32 'value'
        ) cil managed preservesig
    {
    } // end of method E::set_Item
} // end of class E
""".Replace("[netstandard]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Extern_05()
    {
        var source = """
static class E
{
    extension(int i)
    {
        extern int this[int j] { get; set; }
    }
}
""";
        var verifier = CompileAndVerify(source, verify: Verification.FailsPEVerify with { PEVerifyMessage = """
            Error: Method marked Abstract, Runtime, InternalCall or Imported must have zero RVA, and vice versa.
            Error: Method marked Abstract, Runtime, InternalCall or Imported must have zero RVA, and vice versa.
            Type load failed.
            """ });

        verifier.VerifyDiagnostics(
            // (5,34): warning CS0626: Method, operator, or accessor 'E.extension(int).this[int].get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
            //         extern int this[int j] { get; set; }
            Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "get").WithArguments("E.extension(int).this[int].get").WithLocation(5, 34),
            // (5,39): warning CS0626: Method, operator, or accessor 'E.extension(int).this[int].set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
            //         extern int this[int j] { get; set; }
            Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "set").WithArguments("E.extension(int).this[int].set").WithLocation(5, 39));

        verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [netstandard]System.Object
{
    .custom instance void [netstandard]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [netstandard]System.Object
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        .custom instance void [netstandard]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
            01 00 04 49 74 65 6d 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$F4B4FFE41AB49E80A4ECF390CF6EB372'
            extends [netstandard]System.Object
        {
            // Methods
            .method private hidebysig specialname static
                void '<Extension>$' (
                    int32 i
                ) cil managed
            {
                .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x2085
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$F4B4FFE41AB49E80A4ECF390CF6EB372'::'<Extension>$'
        } // end of class <M>$F4B4FFE41AB49E80A4ECF390CF6EB372
        // Methods
        .method private hidebysig specialname
            instance int32 get_Item (
                int32 j
            ) cil managed
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            // Method begins at RVA 0x207e
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [netstandard]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::get_Item
        .method private hidebysig specialname
            instance void set_Item (
                int32 j,
                int32 'value'
            ) cil managed
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            // Method begins at RVA 0x207e
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [netstandard]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::set_Item
        // Properties
        .property instance int32 Item(
            int32 j
        )
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            .get instance int32 E/'<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::get_Item(int32)
            .set instance void E/'<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::set_Item(int32, int32)
        }
    } // end of class <G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69
    // Methods
    .method private hidebysig static
        int32 get_Item (
            int32 i,
            int32 j
        ) cil managed
    {
    } // end of method E::get_Item
    .method private hidebysig static
        void set_Item (
            int32 i,
            int32 j,
            int32 'value'
        ) cil managed
    {
    } // end of method E::set_Item
} // end of class E
""".Replace("[netstandard]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));

        source = """
class C
{
    extern int this[int j] { get; set; }
}
""";
        verifier = CompileAndVerify(source, verify: Verification.FailsPEVerify with { PEVerifyMessage = """
            Error: Method marked Abstract, Runtime, InternalCall or Imported must have zero RVA, and vice versa.
            Error: Method marked Abstract, Runtime, InternalCall or Imported must have zero RVA, and vice versa.
            Type load failed.
            """ });

        verifier.VerifyDiagnostics(
            // (3,30): warning CS0626: Method, operator, or accessor 'C.this[int].get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
            //     extern int this[int j] { get; set; }
            Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "get").WithArguments("C.this[int].get").WithLocation(3, 30),
            // (3,35): warning CS0626: Method, operator, or accessor 'C.this[int].set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
            //     extern int this[int j] { get; set; }
            Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "set").WithArguments("C.this[int].set").WithLocation(3, 35));

        verifier.VerifyTypeIL("C", """
.class private auto ansi beforefieldinit C
    extends [netstandard]System.Object
{
    .custom instance void [netstandard]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )
    // Methods
    .method private hidebysig specialname
        instance int32 get_Item (
            int32 j
        ) cil managed
    {
    } // end of method C::get_Item
    .method private hidebysig specialname
        instance void set_Item (
            int32 j,
            int32 'value'
        ) cil managed
    {
    } // end of method C::set_Item
    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        // Method begins at RVA 0x2067
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void [netstandard]System.Object::.ctor()
        IL_0006: ret
    } // end of method C::.ctor
    // Properties
    .property instance int32 Item(
        int32 j
    )
    {
        .get instance int32 C::get_Item(int32)
        .set instance void C::set_Item(int32, int32)
    }
} // end of class C
""".Replace("[netstandard]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Extern_09()
    {
        var source = """
static class E
{
    extension(int i)
    {
        extern int this[int j]
        {
            [method: System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
            get;

            [method: System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
            set;
        }
    }
}
""";
        var comp = CreateCompilation(source);

        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [netstandard]System.Object
{
    .custom instance void [netstandard]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [netstandard]System.Object
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        .custom instance void [netstandard]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
            01 00 04 49 74 65 6d 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$F4B4FFE41AB49E80A4ECF390CF6EB372'
            extends [netstandard]System.Object
        {
            // Methods
            .method private hidebysig specialname static
                void '<Extension>$' (
                    int32 i
                ) cil managed
            {
                .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x2085
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$F4B4FFE41AB49E80A4ECF390CF6EB372'::'<Extension>$'
        } // end of class <M>$F4B4FFE41AB49E80A4ECF390CF6EB372
        // Methods
        .method private hidebysig specialname
            instance int32 get_Item (
                int32 j
            ) cil managed
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            // Method begins at RVA 0x207e
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [netstandard]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::get_Item
        .method private hidebysig specialname
            instance void set_Item (
                int32 j,
                int32 'value'
            ) cil managed
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            // Method begins at RVA 0x207e
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [netstandard]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::set_Item
        // Properties
        .property instance int32 Item(
            int32 j
        )
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            .get instance int32 E/'<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::get_Item(int32)
            .set instance void E/'<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::set_Item(int32, int32)
        }
    } // end of class <G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69
    // Methods
    .method private hidebysig static
        int32 get_Item (
            int32 i,
            int32 j
        ) cil managed internalcall
    {
    } // end of method E::get_Item
    .method private hidebysig static
        void set_Item (
            int32 i,
            int32 j,
            int32 'value'
        ) cil managed internalcall
    {
    } // end of method E::set_Item
} // end of class E
""".Replace("[netstandard]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Extern_10()
    {
        var source = """
using System.Runtime.InteropServices;
static class E
{
    extension(int i)
    {
        int this[int j]
        {
            [DllImport("something.dll")]
            get => 0;
            [DllImport("something.dll")]
            set { }
        }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (8,14): error CS0601: The DllImport attribute must be specified on a method marked 'extern' that is either 'static' or an extension member
            //             [DllImport("something.dll")]
            Diagnostic(ErrorCode.ERR_DllImportOnInvalidMethod, "DllImport").WithLocation(8, 14),
            // (10,14): error CS0601: The DllImport attribute must be specified on a method marked 'extern' that is either 'static' or an extension member
            //             [DllImport("something.dll")]
            Diagnostic(ErrorCode.ERR_DllImportOnInvalidMethod, "DllImport").WithLocation(10, 14));
    }

    [Fact]
    public void Extern_11()
    {
        var source = """
using System.Runtime.InteropServices;
static class E
{
    extension(int i)
    {
        extern int this[int j]
        {
            [DllImport("something.dll")]
            get => 0;
            [DllImport("something.dll")]
            set { }
        }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (9,13): error CS0179: 'E.extension(int).this[int].get' cannot be extern and declare a body
            //             get => 0;
            Diagnostic(ErrorCode.ERR_ExternHasBody, "get").WithArguments("E.extension(int).this[int].get").WithLocation(9, 13),
            // (11,13): error CS0179: 'E.extension(int).this[int].set' cannot be extern and declare a body
            //             set { }
            Diagnostic(ErrorCode.ERR_ExternHasBody, "set").WithArguments("E.extension(int).this[int].set").WithLocation(11, 13));
    }

    [Fact]
    public void LookupSymbols_01()
    {
        var src = """
_ = new object()[0];

public static class E
{
    extension(object o)
    {
        public int this[int i] => throw null;
    }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var e = ((Compilation)comp).GlobalNamespace.GetTypeMember("E");

        AssertEqualAndNoDuplicates([], model.LookupSymbols(position: 0, e, name: "this[]").ToTestDisplayStrings());
        AssertEqualAndNoDuplicates(["System.Int32 E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.this[System.Int32 i] { get; }"],
            model.LookupSymbols(position: 0, e, name: "this[]", includeReducedExtensionMethods: true).ToTestDisplayStrings());

        Assert.Empty(model.LookupSymbols(position: 0, e, name: "Item").ToTestDisplayStrings());
        Assert.Empty(model.LookupSymbols(position: 0, e, name: "Item", includeReducedExtensionMethods: true).ToTestDisplayStrings());

        AssertEqualAndNoDuplicates(["System.Int32 E.get_Item(System.Object o, System.Int32 i)"], model.LookupSymbols(position: 0, e, name: "get_Item").ToTestDisplayStrings());

        AssertEqualAndNoDuplicates(["System.Int32 E.get_Item(System.Object o, System.Int32 i)"],
            model.LookupSymbols(position: 0, e, name: "get_Item", includeReducedExtensionMethods: true).ToTestDisplayStrings());

        var o = ((Compilation)comp).GetSpecialType(SpecialType.System_Object);

        AssertEqualAndNoDuplicates([], model.LookupSymbols(position: 0, o, name: "this[]").ToTestDisplayStrings());
        AssertEqualAndNoDuplicates(["System.Int32 E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.this[System.Int32 i] { get; }"],
            model.LookupSymbols(position: 0, o, name: "this[]", includeReducedExtensionMethods: true).ToTestDisplayStrings());

        Assert.Empty(model.LookupSymbols(position: 0, o, name: "Item").ToTestDisplayStrings());
        Assert.Empty(model.LookupSymbols(position: 0, o, name: "Item", includeReducedExtensionMethods: true).ToTestDisplayStrings());

        // Indexer cannot be referenced by name
        Assert.DoesNotContain("System.Int32 E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.this[System.Int32 i] { get; }",
            model.LookupSymbols(position: 0, o, name: null, includeReducedExtensionMethods: true).ToTestDisplayStrings());

        Assert.Empty(model.LookupNamespacesAndTypes(position: 0, o, name: null));
    }

    [Fact]
    public void LookupSymbols_02()
    {
        // with generic extension block
        var src = """
_ = 0[1];

public static class E
{
    extension<T>(T t)
    {
        public int this[int i] => throw null;
    }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var o = ((Compilation)comp).GetSpecialType(SpecialType.System_Object);
        AssertEqualAndNoDuplicates([], model.LookupSymbols(position: 0, o, name: "this[]").ToDisplayStrings());
        AssertEqualAndNoDuplicates(["E.extension<object>(object).this[int]"], model.LookupSymbols(position: 0, o, name: "this[]", includeReducedExtensionMethods: true).ToDisplayStrings());

        AssertEqualAndNoDuplicates([], model.LookupSymbols(position: 0, o, name: "Item").ToTestDisplayStrings());
        AssertEqualAndNoDuplicates([], model.LookupSymbols(position: 0, o, name: "Item", includeReducedExtensionMethods: true).ToDisplayStrings());

        AssertEqualAndNoDuplicates([], model.LookupSymbols(position: 0, o, name: "get_Item").ToTestDisplayStrings());

        var s = ((Compilation)comp).GetSpecialType(SpecialType.System_String);
        AssertEqualAndNoDuplicates(["string.this[int]"], model.LookupSymbols(position: 0, s, name: "this[]").ToDisplayStrings());
        AssertEqualAndNoDuplicates(["string.this[int]", "E.extension<string>(string).this[int]"],
            model.LookupSymbols(position: 0, s, name: "this[]", includeReducedExtensionMethods: true).ToDisplayStrings());
    }

    [Fact]
    public void LookupSymbols_03()
    {
        // didn't fully infer
        var src = """
public static class E
{
    extension<T, U>(T t)
    {
        public int this[int i] => 0;
    }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,20): error CS9295: The type parameter `U` is not referenced by either the extension parameter or a parameter of this member
            //         public int this[int i] => 0;
            Diagnostic(ErrorCode.ERR_UnderspecifiedExtension, "this").WithArguments("U").WithLocation(5, 20));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var o = ((Compilation)comp).GetSpecialType(SpecialType.System_Object);
        AssertEqualAndNoDuplicates(["E.extension<object, U>(object).this[int]"],
            model.LookupSymbols(position: 0, o, name: "this[]", includeReducedExtensionMethods: true).ToDisplayStrings());
    }

    [Fact]
    public void Nullability_Indexing_01()
    {
        // maybe-null receiver with generic indexer
        string source = """
#nullable enable

object? o = null;
_ = o[0];

static class E
{
    extension<T>(T t)
    {
        public int this[int i] { get { System.Console.Write(t is null); return 0; } }
    }
}
""";
        var comp = CreateCompilation(source);
        CompileAndVerify(comp, expectedOutput: "True").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "o[0]");
        AssertEx.Equal("E.extension<object?>(object?).this[int]", model.GetSymbolInfo(memberAccess).Symbol.ToDisplayString());
    }

    [Fact]
    public void Nullability_Indexing_02()
    {
        // string indexer
        string source = """
#nullable enable

string? o = null;
try
{
    _ = o[0];
}
catch (System.NullReferenceException)
{
    System.Console.Write("ran");
}
""";
        var comp = CreateCompilation(source);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics(
            // (6,9): warning CS8602: Dereference of a possibly null reference.
            //     _ = o[0];
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(6, 9));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "o[0]");
        AssertEx.Equal("string.this[int]", model.GetSymbolInfo(memberAccess).Symbol.ToDisplayString());
    }

    [Fact]
    public void Nullability_Indexing_03()
    {
        // maybe-null argument with generic indexer
        string source = """
#nullable enable

string s = "";
_ = s[null];

static class E
{
    extension<T>(T t)
    {
        public int this[T t2] { get { System.Console.Write(t2 is null); return 0; } }
    }
}
""";
        var comp = CreateCompilation(source);
        CompileAndVerify(comp, expectedOutput: "True").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "s[null]");
        AssertEx.Equal("E.extension<string?>(string?).this[string?]", model.GetSymbolInfo(memberAccess).Symbol.ToDisplayString());
    }

    [Fact]
    public void Nullability_Indexing_04()
    {
        // with named arguments
        string source = """
#nullable enable

string s = "";
_ = s[t3: null, t2: null];

static class E
{
    extension<T>(T t)
    {
        public int this[T t2, T t3] { get { System.Console.Write((t2 is null, t3 is null)); return 0; } }
    }
}
""";
        var comp = CreateCompilation(source);
        CompileAndVerify(comp, expectedOutput: "(True, True)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "s[t3: null, t2: null]");
        AssertEx.Equal("E.extension<string?>(string?).this[string?, string?]", model.GetSymbolInfo(memberAccess).Symbol.ToDisplayString());
    }

    [Fact]
    public void Nullability_Indexing_05()
    {
        // warning in receiver
        string source = """
#nullable enable

_ = M(null)[0];

object? M(object o) => o;

static class E
{
    extension<T>(T t)
    {
        public int this[int i] { get { System.Console.Write(t is null); return 0; } }
    }
}
""";
        var comp = CreateCompilation(source);
        CompileAndVerify(comp, expectedOutput: "True").VerifyDiagnostics(
            // (3,7): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // _ = M(null)[0];
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 7));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "M(null)[0]");
        AssertEx.Equal("E.extension<object?>(object?).this[int]", model.GetSymbolInfo(memberAccess).Symbol.ToDisplayString());
    }

    [Fact]
    public void Nullability_Indexing_06()
    {
        // warning in argument
        string source = """
#nullable enable

_ = new object()[M(null)];

int M(object o) => 0;

static class E
{
    extension(object o)
    {
        public int this[int i] { get { return 0; } }
    }
}
""";
        CreateCompilation(source).VerifyEmitDiagnostics(
            // (3,20): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // _ = new object()[M(null)];
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 20));
    }

    [Fact]
    public void Nullability_Indexing_07()
    {
        // chained
        string source = """
#nullable enable

object? o = null;
o[0][1].ToString();

static class E
{
    extension<T>(T t)
    {
        public T this[int i] => throw null!;
    }
}
""";
        var comp = CreateCompilation(source).VerifyEmitDiagnostics(
            // (4,1): warning CS8602: Dereference of a possibly null reference.
            // o[0][1].ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o[0][1]").WithLocation(4, 1));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "o[0][1]");
        AssertEx.Equal("E.extension<object?>(object?).this[int]", model.GetSymbolInfo(memberAccess).Symbol.ToDisplayString());
    }

    [Fact]
    public void Nullability_Indexing_09()
    {
        // indexer parameter disallows null
        string source = """
#nullable enable

object? oNull = null;
_ = 42[oNull];

object? oNotNull = new object();
_ = 42[oNotNull];

static class E
{
    extension(int i)
    {
        public int this[object o] { get { return 0; } }
    }
}
""";
        CreateCompilation(source).VerifyEmitDiagnostics(
            // (4,8): warning CS8604: Possible null reference argument for parameter 'o' in 'int E.extension(int).this[object o]'.
            // _ = 42[oNull];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "oNull").WithArguments("o", "int E.extension(int).this[object o]").WithLocation(4, 8));
    }

    [Fact]
    public void Nullability_Indexing_10()
    {
        string source = """
static class E
{
    extension(ref object o)
    {
    }
}
""";
        CreateCompilation(source).VerifyEmitDiagnostics(
            // (3,19): error CS9300: The 'ref' receiver parameter of an extension block must be a value type or a generic type constrained to struct.
            //     extension(ref object o)
            Diagnostic(ErrorCode.ERR_RefExtensionParameterMustBeValueTypeOrConstrainedToOne, "object").WithLocation(3, 19));
    }

    [Fact]
    public void Nullability_Indexing_11()
    {
        // generic, check returned value
        string source = """
#nullable enable

object? oNull = null;
oNull[0].ToString(); // 1

object? oNotNull = new object();
oNotNull[0].ToString();

static class E
{
    extension<T>(T t)
    {
        public T this[int i] { get => throw null!; }
    }
}
""";
        CreateCompilation(source).VerifyEmitDiagnostics(
            // (4,1): warning CS8602: Dereference of a possibly null reference.
            // oNull[0].ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "oNull[0]").WithLocation(4, 1));
    }

    [Fact]
    public void Nullability_Indexing_12()
    {
        // `ref` extension parameter
        var src = """
#nullable enable

S1<object?> s1 = default;
_ = s1[0];

S1<object> s2 = default;
_ = s2[0]; // 1

S2<object?> s3 = default;
_ = s3[0]; // 2

S2<object> s4 = default;
_ = s4[0];

struct S1<T> { }
struct S2<T> { }

static class E
{
    extension(ref S1<object?> o)
    {
        public int this[int i] { get => throw null!; }
    }
    extension(ref S2<object> o)
    {
        public int this[int i] { get => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (7,5): warning CS8620: Argument of type 'S1<object>' cannot be used for parameter 'o' of type 'S1<object?>' in 'E.extension(ref S1<object?>)' due to differences in the nullability of reference types.
            // _ = s2[0]; // 1
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "s2").WithArguments("S1<object>", "S1<object?>", "o", "E.extension(ref S1<object?>)").WithLocation(7, 5),
            // (10,5): warning CS8620: Argument of type 'S2<object?>' cannot be used for parameter 'o' of type 'S2<object>' in 'E.extension(ref S2<object>)' due to differences in the nullability of reference types.
            // _ = s3[0]; // 2
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "s3").WithArguments("S2<object?>", "S2<object>", "o", "E.extension(ref S2<object>)").WithLocation(10, 5));
    }

    [Fact]
    public void Nullability_Indexing_13()
    {
        // `in` extension parameter
        var src = """
#nullable enable

S1<object?> s1 = default;
_ = s1[0];

S1<object> s2 = default;
_ = s2[0]; // 1

S2<object?> s3 = default;
_ = s3[0]; // 2

S2<object> s4 = default;
_ = s4[0];
""";
        var libSrc = """
#nullable enable

public struct S1<T> { }
public struct S2<T> { }

public static class E
{
    extension(in S1<object?> o)
    {
        public int this[int i] { get => throw null!; }
    }
    extension(in S2<object> o)
    {
        public int this[int i] { get => throw null!; }
    }
}
""";
        DiagnosticDescription[] expected = [
            // (7,5): warning CS8620: Argument of type 'S1<object>' cannot be used for parameter 'o' of type 'S1<object?>' in 'E.extension(in S1<object?>)' due to differences in the nullability of reference types.
            // _ = s2[0]; // 1
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "s2").WithArguments("S1<object>", "S1<object?>", "o", "E.extension(in S1<object?>)").WithLocation(7, 5),
            // (10,5): warning CS8620: Argument of type 'S2<object?>' cannot be used for parameter 'o' of type 'S2<object>' in 'E.extension(in S2<object>)' due to differences in the nullability of reference types.
            // _ = s3[0]; // 2
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "s3").WithArguments("S2<object?>", "S2<object>", "o", "E.extension(in S2<object>)").WithLocation(10, 5)
            ];

        var comp = CreateCompilation([src, libSrc]);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void Nullability_Indexing_14()
    {
        // NotNullIfNotNull
        var src = """
#nullable enable

object? oNull = null;
oNull[0].ToString(); // 1

object? oNull2 = null;
E.get_Item(oNull2, 0).ToString(); // 2

object oNotNull = new object();
oNotNull[0].ToString();

E.get_Item(oNotNull, 0).ToString();
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object? o)
    {
        [property: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(o))]
        public object? this[int i] { get => throw null!; }
    }
}
""";
        DiagnosticDescription[] expected = [
            // (4,1): warning CS8602: Dereference of a possibly null reference.
            // oNull[0].ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "oNull[0]").WithLocation(4, 1),
            // (7,1): warning CS8602: Dereference of a possibly null reference.
            // E.get_Item(oNull2, 0).ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "E.get_Item(oNull2, 0)").WithLocation(7, 1),

            // Tracked by https://github.com/dotnet/roslyn/issues/37238 : NotNullIfNotNull not yet supported on indexers. The last two warnings are spurious

            // (10,1): warning CS8602: Dereference of a possibly null reference.
            // oNotNull[0].ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "oNotNull[0]").WithLocation(10, 1),
            // (12,1): warning CS8602: Dereference of a possibly null reference.
            // E.get_Item(oNotNull, 0).ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "E.get_Item(oNotNull, 0)").WithLocation(12, 1)
            ];

        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics(expected);

        src = """
#nullable enable

object? oNull = null;
new C()[oNull].ToString(); // 1

object oNotNull = new object();
new C()[oNotNull].ToString();

class C
{
    [property: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(o))]
    public object? this[object? o] { get => throw null!; }
}
""";
        comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        // Tracked by https://github.com/dotnet/roslyn/issues/37238 : NotNullIfNotNull not yet supported on indexers. 
        comp.VerifyEmitDiagnostics(
            // (4,1): warning CS8602: Dereference of a possibly null reference.
            // new C()[oNull].ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "new C()[oNull]").WithLocation(4, 1),
            // (7,1): warning CS8602: Dereference of a possibly null reference.
            // new C()[oNotNull].ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "new C()[oNotNull]").WithLocation(7, 1));
    }

    [Fact]
    public void Nullability_Indexing_15()
    {
        // NotNull
        var src = """
#nullable enable

new object()[0].ToString();
new object()[0] = null;

E.get_Item(new object(), 0).ToString();
E.set_Item(new object(), 0, null);
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object o)
    {
        [property: System.Diagnostics.CodeAnalysis.NotNull]
        public object? this[int i] { get => throw null!; set => throw null!; }
    }
}
""";
        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics();

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.Skipped);

        static void validate(ModuleSymbol m)
        {
            AssertEx.SetEqual(m is SourceModuleSymbol ? new string[] { } : ["System.Diagnostics.CodeAnalysis.NotNullAttribute", "System.Runtime.CompilerServices.NullableAttribute(2)"],
                m.GlobalNamespace.GetMember<MethodSymbol>("E.get_Item").GetReturnTypeAttributes().ToStrings());

            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_Item").GetReturnTypeAttributes());
            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_Item").Parameters[0].GetAttributes());
        }
    }

    [Fact]
    public void Nullability_Indexing_16()
    {
        // MaybeNull
        var src = """
#nullable enable

new object()[0].ToString(); // 1
new object()[0] = null; // 2
new object()[0] = "";

E.get_Item(new object(), 0).ToString(); // 3
E.set_Item(new object(), 0, null); // 4
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object o)
    {
        [property: System.Diagnostics.CodeAnalysis.MaybeNull]
        public object this[int i] { get => throw null!; set => throw null!; }
    }
}
""";
        DiagnosticDescription[] expected = [
            // (3,1): warning CS8602: Dereference of a possibly null reference.
            // new object()[0].ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "new object()[0]").WithLocation(3, 1),
            // (4,19): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // new object()[0] = null; // 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 19),
            // (7,1): warning CS8602: Dereference of a possibly null reference.
            // E.get_Item(new object(), 0).ToString(); // 3
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "E.get_Item(new object(), 0)").WithLocation(7, 1),
            // (8,29): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // E.set_Item(new object(), 0, null); // 4
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(8, 29)
            ];

        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics(expected);

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.Skipped);

        static void validate(ModuleSymbol m)
        {
            AssertEx.SetEqual(m is SourceModuleSymbol ? new string[] { } : ["System.Diagnostics.CodeAnalysis.MaybeNullAttribute"],
                m.GlobalNamespace.GetMember<MethodSymbol>("E.get_Item").GetReturnTypeAttributes().ToStrings());

            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_Item").GetReturnTypeAttributes());
            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_Item").Parameters[0].GetAttributes());
        }
    }

    [Fact]
    public void Nullability_Indexing_17()
    {
        // AllowNull
        var src = """
#nullable enable

new object()[0].ToString();
new object()[0] = null;

E.get_Item(new object(), 0).ToString();
E.set_Item(new object(), 0, null);
""";
        var libSrc = """
public static class E
{
    extension(object o)
    {
        [property: System.Diagnostics.CodeAnalysis.AllowNull]
        public object this[int i] { get => throw null!; set => throw null!; }
    }
}
""";
        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics();

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.Skipped);

        static void validate(ModuleSymbol m)
        {
            AssertEx.SetEqual(m is SourceModuleSymbol ? new string[] { } : ["System.Diagnostics.CodeAnalysis.AllowNullAttribute"],
                m.GlobalNamespace.GetMember<MethodSymbol>("E.set_Item").Parameters[2].GetAttributes().ToStrings());

            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_Item").GetReturnTypeAttributes());
        }
    }

    [Fact]
    public void Nullability_Indexing_18()
    {
        // DisallowNull
        var src = """
#nullable enable

new object()[0].ToString();
new object()[0] = null;

E.get_Item(new object(), 0).ToString();
E.set_Item(new object(), 0, null);
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object o)
    {
        [property: System.Diagnostics.CodeAnalysis.DisallowNull]
        public object? this[int i] { get => throw null!; set => throw null!; }
    }
}
""";
        DiagnosticDescription[] expected = [
            // (3,1): warning CS8602: Dereference of a possibly null reference.
            // new object()[0].ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "new object()[0]").WithLocation(3, 1),
            // (4,19): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // new object()[0] = null;
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 19),
            // (6,1): warning CS8602: Dereference of a possibly null reference.
            // E.get_Item(new object(), 0).ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "E.get_Item(new object(), 0)").WithLocation(6, 1),
            // (7,29): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // E.set_Item(new object(), 0, null);
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(7, 29)
            ];

        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics(expected);

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.Skipped);

        static void validate(ModuleSymbol m)
        {
            AssertEx.SetEqual(m is SourceModuleSymbol ? new string[] { } : ["System.Diagnostics.CodeAnalysis.DisallowNullAttribute", "System.Runtime.CompilerServices.NullableAttribute(2)"],
                m.GlobalNamespace.GetMember<MethodSymbol>("E.set_Item").Parameters[2].GetAttributes().ToStrings());

            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_Item").GetReturnTypeAttributes());
        }
    }

    [Fact]
    public void Nullability_Indexing_19()
    {
        // DoesNotReturn
        var src = """
#nullable enable

bool b = false;
object? o = null;

if (b)
{
    _ = new object()[0];
    o.ToString(); // incorrect
}

if (b)
{
    new object()[0] = 0;
    o.ToString(); // incorrect
}

if (b)
{
    E.get_Item(new object(), 0);
    o.ToString();
}

if (b)
{
    E.set_Item(new object(), 0, 0);
    o.ToString();
}
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object o)
    {
        public int this[int i]
        {
            [System.Diagnostics.CodeAnalysis.DoesNotReturn]
            get => throw null!;
            [System.Diagnostics.CodeAnalysis.DoesNotReturn]
            set => throw null!;
        }
    }
}
""";
        // Tracked by https://github.com/dotnet/roslyn/issues/50018 : DoesNotReturn not yet supported on indexers.
        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (9,5): warning CS8602: Dereference of a possibly null reference.
            //     o.ToString(); // incorrect
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(9, 5),
            // (15,5): warning CS8602: Dereference of a possibly null reference.
            //     o.ToString(); // incorrect
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(15, 5));

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics(
            // (9,5): warning CS8602: Dereference of a possibly null reference.
            //     o.ToString(); // incorrect
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(9, 5),
            // (15,5): warning CS8602: Dereference of a possibly null reference.
            //     o.ToString(); // incorrect
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(15, 5));
    }

    [Fact]
    public void Nullability_Indexing_20()
    {
        // NotNullWhen
        var src = """
#nullable enable

object? o = null;
if (o[0])
    o.ToString();
else
    o.ToString(); // 1

object? o2 = null;
if (E.get_Item(o2, 0))
    o2.ToString();
else
    o2.ToString(); // 2
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? o)
    {
        public bool this[int i] => throw null!;
    }
}
""";
        DiagnosticDescription[] expected = [
            // (7,5): warning CS8602: Dereference of a possibly null reference.
            //     o.ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(7, 5),
            // (13,5): warning CS8602: Dereference of a possibly null reference.
            //     o2.ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o2").WithLocation(13, 5)
            ];

        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void Nullability_Indexing_21()
    {
        // MaybeNullWhen
        var src = """
#nullable enable

object o = new object();
if (o[0])
    o.ToString(); // 1
else
    o.ToString();

object o2 = new object();
if (E.get_Item(o2, 0))
    o2.ToString(); // 2
else
    o2.ToString();
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension([System.Diagnostics.CodeAnalysis.MaybeNullWhen(true)] object? o)
    {
        public bool this[int i] => throw null!;
    }
}
""";
        DiagnosticDescription[] expected = [
            // (5,5): warning CS8602: Dereference of a possibly null reference.
            //     o.ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(5, 5),
            // (11,5): warning CS8602: Dereference of a possibly null reference.
            //     o2.ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o2").WithLocation(11, 5)
            ];

        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void Nullability_Indexing_22()
    {
        // MemberNotNull
        var src = """
#nullable enable

if (new object()[0])
    object.P2.ToString(); // 1
else
    object.P2.ToString();

if (E.get_Item(new object(), 0))
    E.get_P2().ToString(); // 2
else
    E.get_P2().ToString();

""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object o)
    {
        [System.Diagnostics.CodeAnalysis.MemberNotNull("P2")]
        public bool this[int i] => throw null!;

        public static object? P2 => throw null!;
    }
}
""";
        // Tracked by https://github.com/dotnet/roslyn/issues/78828 : nullability, should we extend member post-conditions to work with extension members?
        DiagnosticDescription[] expected = [
            // (4,5): warning CS8602: Dereference of a possibly null reference.
            //     object.P2.ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "object.P2").WithLocation(4, 5),
            // (6,5): warning CS8602: Dereference of a possibly null reference.
            //     object.P2.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "object.P2").WithLocation(6, 5),
            // (9,5): warning CS8602: Dereference of a possibly null reference.
            //     E.get_P2().ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "E.get_P2()").WithLocation(9, 5),
            // (11,5): warning CS8602: Dereference of a possibly null reference.
            //     E.get_P2().ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "E.get_P2()").WithLocation(11, 5)
            ];

        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net100);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void Nullability_Indexing_23()
    {
        // value of type parameter as receiver
        var src = """
#nullable enable

public static class E
{
    extension<T>(T t)
    {
        public int this[int i] { get { _ = t[0]; return 42; } }
        public int M() { _ = t.M(); return 42; }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_Indexing_24()
    {
        // nullability check on the receiver, un-annotated extension parameter
        var src = """
#nullable enable

object? oNull = null;
_ = oNull[0];

object? oNull2 = null;
E.get_Item(oNull2, 0);

object? oNotNull = new object();
_ = oNotNull[0];

E.get_Item(oNotNull, 0);
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object o)
    {
        public int this[int i] { get => throw null!; }
    }
}
""";
        DiagnosticDescription[] expected = [
            // (4,5): warning CS8604: Possible null reference argument for parameter 'o' in 'E.extension(object)'.
            // _ = oNull[0];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "oNull").WithArguments("o", "E.extension(object)").WithLocation(4, 5),
            // (7,12): warning CS8604: Possible null reference argument for parameter 'o' in 'int E.get_Item(object o, int i)'.
            // E.get_Item(oNull2, 0);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "oNull2").WithArguments("o", "int E.get_Item(object o, int i)").WithLocation(7, 12)
            ];

        var comp = CreateCompilation([src, libSrc]);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void Nullability_Indexing_25()
    {
        // nullability check on the receiver, annotated extension parameter
        var src = """
#nullable enable

object? oNull = null;
_ = oNull[0];

object? oNull2 = null;
E.get_Item(oNull2, 0);

object? oNotNull = new object();
_ = oNotNull[0];

E.get_Item(oNotNull, 0);
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object? o)
    {
        public int this[int i] { get => throw null!; }
    }
}
""";
        DiagnosticDescription[] expected = [];

        var comp = CreateCompilation([src, libSrc]);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void Nullability_Indexing_26()
    {
        // nullability check on the return value
        var src = """
#nullable enable

object o1 = new C()[0]; // 1
object? o2 = new C()[0];

object o3 = E.get_Item(new C(), 0); // 2
object? o4 = E.get_Item(new C(), 0);

object o5 = new D()[0];
object? o6 = new D()[0];

object o7 = E.get_Item(new D(), 0);
object? o8 = E.get_Item(new D(), 0);
""";
        var libSrc = """
#nullable enable

public class C { }
public class D { }

public static class E
{
    extension(C c)
    {
        public object? this[int i] { get => throw null!; }
    }
    extension(D d)
    {
        public object this[int i] { get => throw null!; }
    }
}
""";
        DiagnosticDescription[] expected = [
            // (3,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
            // object o1 = new C()[0]; // 1
            Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "new C()[0]").WithLocation(3, 13),
            // (6,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
            // object o3 = E.get_Item(new C(), 0); // 2
            Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "E.get_Item(new C(), 0)").WithLocation(6, 13)
            ];

        var comp = CreateCompilation([src, libSrc]);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void Nullability_Indexing_27()
    {
        // nullability check on the set value
        var src = """
#nullable enable

new C()[0] = null;
new C()[0] = new object();

E.set_Item(new C(), 0, null);
E.set_Item(new C(), 0, new object());

new D()[0] = null; // 1
new D()[0] = new object();

E.set_Item(new D(), 0, null); // 2
E.set_Item(new D(), 0, new object());
""";
        var libSrc = """
#nullable enable

public class C { }
public class D { }

public static class E
{
    extension(C c)
    {
        public object? this[int i] { set => throw null!; }
    }
    extension(D d)
    {
        public object this[int i] { set => throw null!; }
    }
}
""";
        DiagnosticDescription[] expected = [
            // (9,14): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // new D()[0] = null; // 1
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(9, 14),
            // (12,24): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // E.set_Item(new D(), 0, null); // 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(12, 24)
            ];

        var comp = CreateCompilation([src, libSrc]);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void Nullability_Indexing_28()
    {
        // nullability check on compound assignment
        var src = """
#nullable enable

new C()[0] ??= null;
new C()[0] ??= new object();

new D()[0] ??= null; // 1
new D()[0] ??= new object();

public class C { }
public class D { }

static class E
{
    extension(C c)
    {
        public object? this[int i] { get => throw null!; set => throw null!; }
    }
    extension(D d)
    {
        public object this[int i] { get => throw null!; set => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (6,16): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // new D()[0] ??= null; // 1
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(6, 16));
    }

    [Fact]
    public void Nullability_Indexing_29()
    {
        // generic extension parameter, property read access
        var src = """
#nullable enable

object? oNull = null;
oNull[0].ToString(); // 1

object? oNotNull = new object();
oNotNull[0].ToString();

static class E
{
    extension<T>(T t)
    {
        public T this[int i] { get => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,1): warning CS8602: Dereference of a possibly null reference.
            // oNull[0].ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "oNull[0]").WithLocation(4, 1));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var elementAccess1 = GetSyntax<ElementAccessExpressionSyntax>(tree, "oNull[0]");
        AssertEx.Equal("System.Object? E.extension<System.Object?>(System.Object?).this[System.Int32 i] { get; }",
            model.GetSymbolInfo(elementAccess1).Symbol.ToTestDisplayString(includeNonNullable: true));

        var elementAccess2 = GetSyntax<ElementAccessExpressionSyntax>(tree, "oNotNull[0]");
        AssertEx.Equal("System.Object! E.extension<System.Object!>(System.Object!).this[System.Int32 i] { get; }",
            model.GetSymbolInfo(elementAccess2).Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void Nullability_Indexing_30()
    {
        // generic extension parameter, instance member, property write access
        var src = """
#nullable enable

object? oNull = null;
oNull[0] = null;

object? oNull2 = null;
oNull2[0] = new object();

object? oNotNull = new object();
oNotNull[0] = null; // 1
E.set_Item(oNotNull, 0, null);

oNotNull[0] = new object();

oNotNull?[0] = null; // 2

static class E
{
    extension<T>(T t)
    {
        public T this[int i] { set => throw null!; }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (10,15): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // oNotNull[0] = null; // 1
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(10, 15),
            // (15,16): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // oNotNull?[0] = null; // 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(15, 16));
    }

    [Fact]
    public void Nullability_Indexing_31()
    {
        // notnull constraint
        var src = """
#nullable enable

object? oNull = null;
_ = oNull[0]; // 1

object? oNull2 = null;
_ = oNull2?[0];

object? oNotNull = new object();
_ = oNotNull[0];

static class E
{
    extension<T>(T t) where T : notnull
    {
        public T this[int i] { get => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,5): warning CS8714: The type 'object?' cannot be used as type parameter 'T' in the generic type or method 'E.extension<T>(T)'. Nullability of type argument 'object?' doesn't match 'notnull' constraint.
            // _ = oNull[0]; // 1
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "oNull[0]").WithArguments("E.extension<T>(T)", "T", "object?").WithLocation(4, 5));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var elementAccess1 = GetSyntax<ElementAccessExpressionSyntax>(tree, "oNull[0]");
        AssertEx.Equal("System.Object? E.extension<System.Object?>(System.Object?).this[System.Int32 i] { get; }",
            model.GetSymbolInfo(elementAccess1).Symbol.ToTestDisplayString(includeNonNullable: true));

        var elementAccess2 = GetSyntax<ElementAccessExpressionSyntax>(tree, "oNotNull[0]");
        AssertEx.Equal("System.Object! E.extension<System.Object!>(System.Object!).this[System.Int32 i] { get; }",
            model.GetSymbolInfo(elementAccess2).Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void Nullability_Indexing_32()
    {
        // notnull constraint, in tuple
        var src = """
#nullable enable

object? oNull = null;
_ = (1, oNull[0]);

static class E
{
    extension<T>(T t) where T : notnull
    {
        public T this[int i] { get => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,9): warning CS8714: The type 'object?' cannot be used as type parameter 'T' in the generic type or method 'E.extension<T>(T)'. Nullability of type argument 'object?' doesn't match 'notnull' constraint.
            // _ = (1, oNull[0]);
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "oNull[0]").WithArguments("E.extension<T>(T)", "T", "object?").WithLocation(4, 9));
    }

    [Fact]
    public void Nullability_Indexing_34()
    {
        // implicit reference conversion on the receiver
        var src = """
#nullable enable

C? cNull = null;
_ = cNull[0];

C? cNotNull = new C();
_ = cNotNull[0];

public class C { }

static class E
{
    extension(object? o)
    {
        public int this[int i] { get => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_Indexing_35()
    {
        // implicit reference conversion on the receiver
        var src = """
#nullable enable

C? cNull = null;
_ = cNull[0]; // 1

C? cNotNull = new C();
_ = cNotNull[0];

public class C { }

static class E
{
    extension(object o)
    {
        public int this[int i] { get => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,5): warning CS8604: Possible null reference argument for parameter 'o' in 'E.extension(object)'.
            // _ = cNull[0]; // 1
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "cNull").WithArguments("o", "E.extension(object)").WithLocation(4, 5));
    }

    [Fact]
    public void Nullability_Indexing_36()
    {
        // optional parameter
        var src = """
#nullable enable

object o = new object();
_ = o[0];
o[0] = 0;

static class E
{
    extension<T>(T t)
    {
        public int this[int i1, int i2 = 42] { get => throw null!; set => throw null!; }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_Indexing_37()
    {
        // optional parameter with nullability warning
        var src = """
#nullable enable

object o = new object();
_ = o[0];
o[0] = 0;

static class E
{
    extension(object o1)
    {
        public int this[int i1, object o2 = null] { get => throw null!; set => throw null!; }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (11,45): warning CS8625: Cannot convert null literal to non-nullable reference type.
            //         public int this[int i1, object o2 = null] { get => throw null!; set => throw null!; }
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(11, 45));
    }

    [Fact]
    public void Nullability_ObjectInitializer_01()
    {
        string source = """
#nullable enable

_ = new object() { [null] = 1 }; // 1
E.set_Item(new object(), null, 1);

_ = new object() { [null!] = 1 };

static class E
{
    extension<T>(T t)
    {
        public int this[T t2] { set { System.Console.Write(t2 is null); } }
    }
}
""";
        var comp = CreateCompilation(source).VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ImplicitElementAccessSyntax>(tree, "[null]");
        AssertEx.Equal("E.extension<object?>(object?).this[object?]", model.GetSymbolInfo(memberAccess).Symbol.ToDisplayString());
    }

    [Fact]
    public void Nullability_ObjectInitializer_02()
    {
        string source = """
#nullable enable

_ = new object() { [t2: (object?)null, i1: 42] = "" };

static class E
{
    extension<T>(object o)
    {
        public string this[int i1, T t2] { set { System.Console.Write((i1, t2 is null)); } }
    }
}
""";
        var comp = CreateCompilation(source);
        CompileAndVerify(comp, expectedOutput: "(42, True)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ImplicitElementAccessSyntax>(tree, "[t2: (object?)null, i1: 42]");
        AssertEx.Equal("E.extension<object?>(object).this[int, object?]", model.GetSymbolInfo(memberAccess).Symbol.ToDisplayString());
    }

    [Fact]
    public void Nullability_ObjectInitializer_03()
    {
        // nested initializer
        string source = """
#nullable enable

_ = new object() { [null] = { [null] = new object() } };

static class E
{
    extension(object o1)
    {
        public object this[object o2] { get => throw null!; set { } }
    }
}
""";
        var comp = CreateCompilation(source).VerifyEmitDiagnostics(
            // (3,21): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // _ = new object() { [null] = { [null] = new object() } };
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 21),
            // (3,32): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // _ = new object() { [null] = { [null] = new object() } };
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 32));
    }

    [Fact]
    public void Nullability_ObjectInitializer_04()
    {
        var src = """
#nullable enable

object? oNull = null;
_ = new object() { [0] = oNull }; // 1

object oNotNull = new object();
_ = new object() { [0] = oNotNull };

static class E
{
    extension(object o)
    {
        public object this[int i] { set { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,26): warning CS8601: Possible null reference assignment.
            // _ = new object() { [0] = oNull }; // 1
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNull").WithLocation(4, 26));
    }

    [Fact]
    public void Nullability_ObjectInitializer_05()
    {
        var src = """
#nullable enable

_ = new object() { [0] = 42 };

static class E
{
    extension<T>(T t)
    {
        public int this[int i] { set { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var assignment = GetSyntax<AssignmentExpressionSyntax>(tree, "[0] = 42");
        AssertEx.Equal("System.Int32 E.extension<System.Object!>(System.Object!).this[System.Int32 i] { set; }",
            model.GetSymbolInfo(assignment.Left).Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void Nullability_ObjectInitializer_06()
    {
        var src = """
#nullable enable

object? oNull = null;
_ = new object() { [0] = oNull }; // 1

object oNotNull = new object();
_ = new object() { [0] = oNotNull };

static class E
{
    extension<T>(T t)
    {
        public T this[int i] { set { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,26): warning CS8601: Possible null reference assignment.
            // _ = new object() { [0] = oNull }; // 1
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNull").WithLocation(4, 26));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var assignment = GetSyntax<AssignmentExpressionSyntax>(tree, "[0] = oNull");

        AssertEx.Equal("System.Object! E.extension<System.Object!>(System.Object!).this[System.Int32 i] { set; }",
            model.GetSymbolInfo(assignment.Left).Symbol.ToTestDisplayString(includeNonNullable: true));

        assignment = GetSyntax<AssignmentExpressionSyntax>(tree, "[0] = oNotNull");
        AssertEx.Equal("System.Object! E.extension<System.Object!>(System.Object!).this[System.Int32 i] { set; }",
            model.GetSymbolInfo(assignment.Left).Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void Nullability_ObjectInitializer_07()
    {
        var src = """
#nullable enable

_ = new C<string>() { [0] = null }; // 1
_ = new C<string>() { [0] = "a" };

_ = new C<string?>() { [0] = null };
_ = new C<string?>() { [0] = "a" };

class C<T> { }

static class E
{
    extension<T>(C<T> c)
    {
        public T this[int i] { set { } }
    }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyTypes(comp.SyntaxTrees[0]);
        comp.VerifyEmitDiagnostics(
            // (3,29): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // _ = new C<string>() { [0] = null }; // 1
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 29));
    }

    [Fact]
    public void Nullability_ObjectInitializer_08()
    {
        var src = """
#nullable enable

var s = "a";
Use(s, (new(s) { [0] = null })/*T:C<string!>!*/); // 1
Use(s, (new(s) { [0] = "a" })/*T:C<string!>!*/);

Use("a", (new(s) { [0] = null })/*T:C<string!>!*/); // 2
Use("a", (new(s) { [0] = "a" })/*T:C<string!>!*/);

Use(s, (new("a") { [0] = null })/*T:C<string!>!*/); // 3
Use(s, (new("a") { [0] = "a" })/*T:C<string!>!*/);

Use("a", (new("a") { [0] = null })/*T:C<string!>!*/); // 4
Use("a", (new("a") { [0] = "a" })/*T:C<string!>!*/);

if (s != null) return;
Use(s, (new(s) { [0] = null })/*T:C<string?>!*/);

if (s != null) return;
Use(s, (new(s) { [0] = "a" })/*T:C<string?>!*/);

if (s != null) return;
Use("a", (new(s) { [0] = null })/*T:C<string!>!*/); // 5

if (s != null) return;
Use("a", (new(s) { [0] = "a" })/*T:C<string!>!*/); // 6

if (s != null) return;
Use(s, (new("a") { [0] = null })/*T:C<string?>!*/);

if (s != null) return;
Use(s, (new("a") { [0] = "a" })/*T:C<string?>!*/);

if (s != null) return;
Use("a", (new("a") { [0] = null })/*T:C<string!>!*/); // 7

if (s != null) return;
Use("a", (new("a") { [0] = "a" })/*T:C<string!>!*/);

void Use<T>(T value, C<T> c) => throw null!;

record C<T>(T Value) { }

static class E
{
    extension<T>(C<T> c)
    {
        public T this[int i] { set { } }
    }
}
""";

        var comp = CreateCompilation([src, IsExternalInitTypeDefinition]);
        comp.VerifyTypes(comp.SyntaxTrees[0]);
        comp.VerifyEmitDiagnostics(
            // (4,24): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // Use(s, (new(s) { [0] = null })/*T:C<string!>!*/); // 1
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 24),
            // (7,26): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // Use("a", (new(s) { [0] = null })/*T:C<string!>!*/); // 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(7, 26),
            // (10,26): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // Use(s, (new("a") { [0] = null })/*T:C<string!>!*/); // 3
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(10, 26),
            // (13,28): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // Use("a", (new("a") { [0] = null })/*T:C<string!>!*/); // 4
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(13, 28),
            // (23,15): warning CS8604: Possible null reference argument for parameter 'Value' in 'C<string>.C(string Value)'.
            // Use("a", (new(s) { [0] = null })/*T:C<string!>!*/); // 5
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("Value", "C<string>.C(string Value)").WithLocation(23, 15),
            // (23,26): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // Use("a", (new(s) { [0] = null })/*T:C<string!>!*/); // 5
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(23, 26),
            // (26,15): warning CS8604: Possible null reference argument for parameter 'Value' in 'C<string>.C(string Value)'.
            // Use("a", (new(s) { [0] = "a" })/*T:C<string!>!*/); // 6
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("Value", "C<string>.C(string Value)").WithLocation(26, 15),
            // (35,28): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // Use("a", (new("a") { [0] = null })/*T:C<string!>!*/); // 7
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(35, 28));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var assignment = GetSyntaxes<AssignmentExpressionSyntax>(tree, "[0] = null").First();

        AssertEx.Equal("System.String! E.extension<System.String!>(C<System.String!>!).this[System.Int32 i] { set; }",
            model.GetSymbolInfo(assignment.Left).Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void Nullability_ObjectInitializer_09()
    {
        // notnull constraint
        var src = """
#nullable enable

var s = "a";
Use(s, (new(s) { [0] = null })/*T:C<string!>!*/); // 1
Use(s, (new(s) { [0] = "a" })/*T:C<string!>!*/);

Use("a", (new(s) { [0] = null })/*T:C<string!>!*/); // 2
Use("a", (new(s) { [0] = "a" })/*T:C<string!>!*/);

Use(s, (new("a") { [0] = null })/*T:C<string!>!*/); // 3
Use(s, (new("a") { [0] = "a" })/*T:C<string!>!*/);

Use("a", (new("a") { [0] = null })/*T:C<string!>!*/); // 4
Use("a", (new("a") { [0] = "a" })/*T:C<string!>!*/);

if (s != null) return;
Use(s, (new(s) { [0] = null })/*T:C<string?>!*/); // 5

if (s != null) return;
Use(s, (new(s) { [0] = "a" })/*T:C<string?>!*/); // 6

if (s != null) return;
Use("a", (new(s) { [0] = null })/*T:C<string!>!*/); // 7

if (s != null) return;
Use("a", (new(s) { [0] = "a" })/*T:C<string!>!*/); // 8

if (s != null) return;
Use(s, (new("a") { [0] = null })/*T:C<string?>!*/); // 9

if (s != null) return;
Use(s, (new("a") { [0] = "a" })/*T:C<string?>!*/); // 10

if (s != null) return;
Use("a", (new("a") { [0] = null })/*T:C<string!>!*/); // 11

if (s != null) return;
Use("a", (new("a") { [0] = "a" })/*T:C<string!>!*/);

void Use<T>(T value, C<T> c) => throw null!;

record C<T>(T Value) { }

static class E
{
    extension<T>(C<T> c) where T : notnull
    {
        public T this[int i] { set { } }
    }
}
""";

        var comp = CreateCompilation([src, IsExternalInitTypeDefinition]);
        comp.VerifyTypes(comp.SyntaxTrees[0]);
        comp.VerifyEmitDiagnostics(
            // (4,24): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // Use(s, (new(s) { [0] = null })/*T:C<string!>!*/); // 1
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 24),
            // (7,26): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // Use("a", (new(s) { [0] = null })/*T:C<string!>!*/); // 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(7, 26),
            // (10,26): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // Use(s, (new("a") { [0] = null })/*T:C<string!>!*/); // 3
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(10, 26),
            // (13,28): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // Use("a", (new("a") { [0] = null })/*T:C<string!>!*/); // 4
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(13, 28),
            // (17,18): warning CS8714: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'E.extension<T>(C<T>)'. Nullability of type argument 'string?' doesn't match 'notnull' constraint.
            // Use(s, (new(s) { [0] = null })/*T:C<string?>!*/); // 5
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "[0]").WithArguments("E.extension<T>(C<T>)", "T", "string?").WithLocation(17, 18),
            // (20,18): warning CS8714: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'E.extension<T>(C<T>)'. Nullability of type argument 'string?' doesn't match 'notnull' constraint.
            // Use(s, (new(s) { [0] = "a" })/*T:C<string?>!*/); // 6
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "[0]").WithArguments("E.extension<T>(C<T>)", "T", "string?").WithLocation(20, 18),
            // (23,15): warning CS8604: Possible null reference argument for parameter 'Value' in 'C<string>.C(string Value)'.
            // Use("a", (new(s) { [0] = null })/*T:C<string!>!*/); // 7
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("Value", "C<string>.C(string Value)").WithLocation(23, 15),
            // (23,26): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // Use("a", (new(s) { [0] = null })/*T:C<string!>!*/); // 7
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(23, 26),
            // (26,15): warning CS8604: Possible null reference argument for parameter 'Value' in 'C<string>.C(string Value)'.
            // Use("a", (new(s) { [0] = "a" })/*T:C<string!>!*/); // 8
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("Value", "C<string>.C(string Value)").WithLocation(26, 15),
            // (29,20): warning CS8714: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'E.extension<T>(C<T>)'. Nullability of type argument 'string?' doesn't match 'notnull' constraint.
            // Use(s, (new("a") { [0] = null })/*T:C<string?>!*/); // 9
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "[0]").WithArguments("E.extension<T>(C<T>)", "T", "string?").WithLocation(29, 20),
            // (32,20): warning CS8714: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'E.extension<T>(C<T>)'. Nullability of type argument 'string?' doesn't match 'notnull' constraint.
            // Use(s, (new("a") { [0] = "a" })/*T:C<string?>!*/); // 10
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "[0]").WithArguments("E.extension<T>(C<T>)", "T", "string?").WithLocation(32, 20),
            // (35,28): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // Use("a", (new("a") { [0] = null })/*T:C<string!>!*/); // 11
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(35, 28));
    }

    [Fact]
    public void Nullability_ObjectInitializer_10()
    {
        var src = """
#nullable enable

var s = "a";

Create(s).Use(new() { [0] = null });
Create(s).Use(new() { [0] = "a" });

if (s != null) return;
Create(s).Use(new() { [0] = null });

if (s != null) return;
Create(s).Use(new() { [0] = "a" });

Consumer<T> Create<T>(T value) => throw null!;

class Consumer<T>
{
    public void Use(C<T> c) => throw null!;
}

record C<T> { }

static class E
{
    extension<T>(C<T> c)
    {
        public T this[int i] { set { } }
    }
}
""";

        var comp = CreateCompilation([src, IsExternalInitTypeDefinition]);
        comp.VerifyEmitDiagnostics(
            // (5,29): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // Create(s).Use(new() { [0] = null });
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(5, 29));
    }

    [Fact]
    public void Nullability_ObjectInitializer_11()
    {
        // nested
        var src = """
#nullable enable

_ = new object() { [0] = { F = "" } };

public static class E
{
    extension<T>(T t)
    {
        public C this[int i] { get => new C(); }
    }
}

public class C
{
    public string? F;
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var assignment = GetSyntax<AssignmentExpressionSyntax>(tree, """[0] = { F = "" }""");

        AssertEx.Equal("C! E.extension<System.Object!>(System.Object!).this[System.Int32 i] { get; }",
            model.GetSymbolInfo(assignment.Left).Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void Nullability_ObjectInitializer_12()
    {
        // nested
        var src = """
#nullable enable

object o = new() { [0] = { F = "" } };

public static class E
{
    extension<T>(T t)
    {
        public C this[int i] { get => new C(); }
    }
}

public class C
{
    public string? F;
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var assignment = GetSyntax<AssignmentExpressionSyntax>(tree, """[0] = { F = "" }""");

        AssertEx.Equal("C! E.extension<System.Object!>(System.Object!).this[System.Int32 i] { get; }",
            model.GetSymbolInfo(assignment.Left).Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void Nullability_ObjectInitializer_13()
    {
        // nested
        var src = """
#nullable enable

_ = new C<string>() { [0] = { F = null } }; // 1

public static class E
{
    extension<T>(C<T> c)
    {
        public C<T> this[int i] { get => c; }
    }
}

public class C<T>
{
    public T F = default!;
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,35): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // _ = new C<string>() { [0] = { F = null } }; // 1
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 35));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var assignment = GetSyntax<AssignmentExpressionSyntax>(tree, "[0] = { F = null }");

        AssertEx.Equal("C<System.String!>! E.extension<System.String!>(C<System.String!>!).this[System.Int32 i] { get; }",
            model.GetSymbolInfo(assignment.Left).Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void Nullability_ObjectInitializer_14()
    {
        // nested, target-typed
        var src = """
#nullable enable

C<string> c = new() { [0] = { F = null } }; // 1

public static class E
{
    extension<T>(C<T> c)
    {
        public C<T> this[int i] { get => c; }
    }
}

public class C<T>
{
    public T F = default!;
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,35): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // C<string> c = new() { [0] = { F = null } }; // 1
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 35));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var assignment = GetSyntax<AssignmentExpressionSyntax>(tree, "[0] = { F = null }");

        AssertEx.Equal("C<System.String!>! E.extension<System.String!>(C<System.String!>!).this[System.Int32 i] { get; }",
            model.GetSymbolInfo(assignment.Left).Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void Nullability_ObjectInitializer_15()
    {
        // nested, target-typed, no getter
        var src = """
#nullable enable

C<string> c = new() { [0] = { F = null } };

public static class E
{
    extension<T>(C<T> c)
    {
        public C<T> this[int i] { set { } } // no getter
    }
}

public class C<T>
{
    public T F = default!;
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,23): error CS0154: The property or indexer 'E.extension<string>(C<string>).this[int]' cannot be used in this context because it lacks the get accessor
            // C<string> c = new() { [0] = { F = null } };
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, "[0]").WithArguments("E.extension<string>(C<string>).this[int]").WithLocation(3, 23),
            // (3,35): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // C<string> c = new() { [0] = { F = null } };
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 35));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var assignment = GetSyntax<AssignmentExpressionSyntax>(tree, "[0] = { F = null }");
        Assert.Null(model.GetSymbolInfo(assignment.Left).Symbol);
    }

    [Fact]
    public void Nullability_ObjectInitializer_16()
    {
        // nested, no setter
        var src = """
#nullable enable

_ = new C<string>() { P = { [0] = null } };

public static class E
{
    extension<T>(C<T> c)
    {
        public T this[int i] => default!; // no setter
    }
}

public class C<T>
{
    public C<T> P { get => this; }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,29): error CS0200: Property or indexer 'E.extension<string>(C<string>).this[int]' cannot be assigned to -- it is read only
            // _ = new C<string>() { P = { [0] = null } };
            Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "[0]").WithArguments("E.extension<string>(C<string>).this[int]").WithLocation(3, 29),
            // (3,35): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // _ = new C<string>() { P = { [0] = null } };
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 35));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var assignment = GetSyntax<AssignmentExpressionSyntax>(tree, "[0] = null");
        Assert.Null(model.GetSymbolInfo(assignment.Left).Symbol);
    }

    [Fact]
    public void Nullability_ObjectInitializer_17()
    {
        // nested
        var src = """
#nullable enable

_ = new C<string>() { P = { [0] = null } };

public static class E
{
    extension<T>(C<T> c)
    {
        public T this[int i] { set { } }
    }
}

public class C<T>
{
    public C<T> P { get => this; }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,35): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // _ = new C<string>() { P = { [0] = null } };
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 35));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var assignment = GetSyntax<AssignmentExpressionSyntax>(tree, "[0] = null");

        AssertEx.Equal("System.String! E.extension<System.String!>(C<System.String!>!).this[System.Int32 i] { set; }",
            model.GetSymbolInfo(assignment.Left).Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void Nullability_ObjectInitializer_18()
    {
        string source = """
#nullable enable

_ = new object() { [null] = 1 }; // 1
""";
        var comp = CreateCompilation(source).VerifyEmitDiagnostics(
            // (3,20): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
            // _ = new object() { [null] = 1 }; // 1
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[null]").WithArguments("object").WithLocation(3, 20));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ImplicitElementAccessSyntax>(tree, "[null]");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
    }

    [Fact]
    public void Nullability_ObjectInitializer_19()
    {
        // optional parameter
        var src = """
#nullable enable

_ = new C<string>() { P = { [0] = null } };

public static class E
{
    extension<T>(C<T> c)
    {
        public T this[int i1, int i2 = 42] { set { } }
    }
}

public class C<T>
{
    public C<T> P { get => this; }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (3,35): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // _ = new C<string>() { P = { [0] = null } };
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 35));
    }

    [Fact]
    public void Nullability_ObjectInitializer_20()
    {
        // target-typed, warning in indexer arguments
        var src = """
#nullable enable

object c = new() { [null, 0, null] = 10 }; // 1, 2

public static class E
{
    extension(object o1)
    {
        public int this[object o2, int i, object o3] { set => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,21): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // object c = new() { [null, 0, null] = 10 }; // 1, 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 21),
            // (3,30): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // object c = new() { [null, 0, null] = 10 }; // 1, 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 30));
    }

    [Fact]
    public void Nullability_ObjectInitializer_21()
    {
        var src = """
#nullable enable

object c = new() { [0] = 10, [1] = 20 };

public static class E
{
    extension(ref object? o)
    {
        public int this[int i] { set { o = null; } }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (3,20): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
            // object c = new() { [0] = 10, [1] = 20 };
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[0]").WithArguments("object").WithLocation(3, 20),
            // (3,30): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
            // object c = new() { [0] = 10, [1] = 20 };
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[1]").WithArguments("object").WithLocation(3, 30),
            // (7,19): error CS9300: The 'ref' receiver parameter of an extension block must be a value type or a generic type constrained to struct.
            //     extension(ref object? o)
            Diagnostic(ErrorCode.ERR_RefExtensionParameterMustBeValueTypeOrConstrainedToOne, "object?").WithLocation(7, 19));
    }

    [Fact]
    public void Nullability_Increment_01()
    {
        string source = """
#nullable enable

new object()[0]++;

public class C
{
    public static C? operator ++(C? c) => throw null!; 
}

public static class E
{
    extension(object o)
    {
        public C? this[int i]
        {
            get => throw null!;
            set { }
        }
    }
}
""";
        CreateCompilation(source).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_Increment_02()
    {
        string source = """
#nullable enable

new object()[0]++;

public class C
{
    public static C? operator ++(C? c) => throw null!; 
}

public static class E
{
    extension(object o)
    {
        [property: System.Diagnostics.CodeAnalysis.DisallowNull]
        public C? this[int i]
        {
            get => throw null!;
            set { }
        }
    }
}
""";
        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_Increment_03()
    {
        string source = """
#nullable enable

new object()[0]++;

public class C
{
    public static C operator ++(C c) => throw null!; 
}

public static class E
{
    extension(object o)
    {
        public C this[int i]
        {
            get => throw null!;
            set { }
        }
    }
}
""";
        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_Increment_04()
    {
        string source = """
#nullable enable

new object()[0]++;

public class C
{
    public static C operator ++(C c) => throw null!; 
}

public static class E
{
    extension(object o)
    {
        [property: System.Diagnostics.CodeAnalysis.MaybeNull]
        public C this[int i]
        {
            get => throw null!;
            set { }
        }
    }
}
""";
        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyEmitDiagnostics(
            // (3,1): warning CS8604: Possible null reference argument for parameter 'c' in 'C C.operator ++(C c)'.
            // new object()[0]++;
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "new object()[0]").WithArguments("c", "C C.operator ++(C c)").WithLocation(3, 1));
    }

    [Fact]
    public void Nullability_Increment_05()
    {
        // with supression
        string source = """
#nullable enable

new object()[0]!++;

public class C
{
    public static C operator ++(C c) => throw null!; 
}

public static class E
{
    extension(object o)
    {
        [property: System.Diagnostics.CodeAnalysis.MaybeNull]
        public C this[int i]
        {
            get => throw null!;
            set { }
        }
    }
}
""";
        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyEmitDiagnostics();
    }

    [Fact(Skip = "PROTOTYPE assertion in NullableWalker.DebugVerifier")]
    public void Nullability_ListPattern_01()
    {
        string source = """
#nullable enable

if (new C<string?>() is [var x1])
{
    string y1 = x1;
}

if (new C<string>() is [var x2])
{
    string y2 = x2;
}

public class C<T>
{
    public int Length => throw null!;
}

public static class E
{
    extension<T>(C<T> c)
    {
        public T this[System.Index i] { get => throw null!; }
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (5,17): warning CS8600: Converting null literal or possible null value to non-nullable type.
            //     string y1 = x1;
            Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "x1").WithLocation(5, 17));
    }

    [Fact]
    public void Cref_01()
    {
        var src = """
/// <see cref="E.extension(int).this[string]"/>
/// <see cref="E.extension(int).get_Item(string)"/>
/// <see cref="E.extension(int).get_Item"/>
/// <see cref="E.extension(int).set_Item(string, int)"/>
/// <see cref="E.extension(int).set_Item"/>
/// <see cref="E.get_Item(int, string)"/>
/// <see cref="E.get_Item"/>
/// <see cref="E.set_Item(int, string, int)"/>
/// <see cref="E.set_Item"/>
/// <see cref="E.extension(int).this[]"/>
/// <see cref="E.extension(int).Item(string)"/>
public static class E
{
    extension(int i)
    {
        /// <summary></summary>
        public int this[string s]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (10,16): warning CS1574: XML comment has cref attribute 'extension(int).this[]' that could not be resolved
            // /// <see cref="E.extension(int).this[]"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int).this[]").WithArguments("extension(int).this[]").WithLocation(10, 16),
            // (11,16): warning CS1574: XML comment has cref attribute 'extension(int).Item(string)' that could not be resolved
            // /// <see cref="E.extension(int).Item(string)"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int).Item(string)").WithArguments("extension(int).Item(string)").WithLocation(11, 16));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension(int).this[string], System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s] { get; set; })",
            "(E.extension(int).get_Item(string), System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].get)",
            "(E.extension(int).get_Item, System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].get)",
            "(E.extension(int).set_Item(string, int), void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].set)",
            "(E.extension(int).set_Item, void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].set)",
            "(E.get_Item(int, string), System.Int32 E.get_Item(System.Int32 i, System.String s))",
            "(E.get_Item, System.Int32 E.get_Item(System.Int32 i, System.String s))",
            "(E.set_Item(int, string, int), void E.set_Item(System.Int32 i, System.String s, System.Int32 value))",
            "(E.set_Item, void E.set_Item(System.Int32 i, System.String s, System.Int32 value))",
            "(E.extension(int).this[], null)",
            "(E.extension(int).Item(string), null)"],
            PrintXmlCrefSymbols(tree, model));

        src = """
/// <see cref="E.extension(int).this[string]"/>
/// <see cref="E.extension(int).get_Item(string)"/>
public static class E
{
    extension(int i)
    {
        /// <summary></summary>
        public int this[string s]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";

        // Tracked by https://github.com/dotnet/roslyn/issues/78830 : diagnostic quality, it would be better if the location for the "feature not available" errors pointed to the extension keyword only
        CreateCompilation(src, parseOptions: TestOptions.Regular13.WithDocumentationMode(DocumentationMode.Diagnose)).VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(int).this[string]' that could not be resolved
            // /// <see cref="E.extension(int).this[string]"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int).this[string]").WithArguments("extension(int).this[string]").WithLocation(1, 16),
            // (1,18): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // /// <see cref="E.extension(int).this[string]"/>
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "extension(int).this[string]").WithArguments("extension indexers").WithLocation(1, 18),
            // (2,16): warning CS1574: XML comment has cref attribute 'extension(int).get_Item(string)' that could not be resolved
            // /// <see cref="E.extension(int).get_Item(string)"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int).get_Item(string)").WithArguments("extension(int).get_Item(string)").WithLocation(2, 16),
            // (2,18): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
            // /// <see cref="E.extension(int).get_Item(string)"/>
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "extension(int).get_Item(string)").WithArguments("extensions", "14.0").WithLocation(2, 18),
            // (5,5): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
            //     extension(int i)
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, @"extension(int i)
    {
").WithArguments("extensions", "14.0").WithLocation(5, 5),
            // (5,5): error CS0710: Static classes cannot have instance constructors
            //     extension(int i)
            Diagnostic(ErrorCode.ERR_ConstructorInStaticClass, "extension").WithLocation(5, 5),
            // (6,6): error CS1513: } expected
            //     {
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(6, 6),
            // (8,20): error CS0720: 'E.this[string]': cannot declare indexers in a static class
            //         public int this[string s]
            Diagnostic(ErrorCode.ERR_IndexerInStaticClass, "this").WithArguments("E.this[string]").WithLocation(8, 20),
            // (14,1): error CS1022: Type or namespace definition, or end-of-file expected
            // }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(14, 1));
    }

    [Fact]
    public void Cref_02()
    {
        // LangVer of consuming compilation
        var libSrc = """
public static class E
{
    extension(int i)
    {
        public int this[string s]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";
        var libComp = CreateCompilation(libSrc);

        var src = """
/// <see cref="E.extension(int).this[string]"/>
/// <see cref="E.extension(int).get_Item(string)"/>
class C { }
""";
        var comp = CreateCompilation(src, references: [libComp.EmitToImageReference()], parseOptions: TestOptions.Regular14.WithDocumentationMode(DocumentationMode.Diagnose));
        comp.VerifyEmitDiagnostics(
            // (1,18): error CS8652: The feature 'extension indexers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // /// <see cref="E.extension(int).this[string]"/>
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "extension(int).this[string]").WithArguments("extension indexers").WithLocation(1, 18));

        validateCrefSymbols(comp);

        comp = CreateCompilation(src, references: [libComp.EmitToImageReference()], parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
        comp.VerifyEmitDiagnostics();

        validateCrefSymbols(comp);

        comp = CreateCompilation(src, references: [libComp.EmitToImageReference()], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
        comp.VerifyEmitDiagnostics();

        validateCrefSymbols(comp);

        static void validateCrefSymbols(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            AssertEx.Equal([
                "(E.extension(int).this[string], System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s] { get; set; })",
                "(E.extension(int).get_Item(string), System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].get)"],
                PrintXmlCrefSymbols(tree, model));
        }
    }

    [Fact]
    public void Cref_03()
    {
        // [IndexerName]
        var libSrc = """
static class E
{
    extension(int i)
    {
        [System.Runtime.CompilerServices.IndexerName("MyIndexer")]
        public int this[string s]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";
        var src = """
/// <see cref="E.extension(int).this[string]"/>
/// <see cref="E.extension(int).get_MyIndexer(string)"/>
/// <see cref="E.extension(int).get_MyIndexer"/>
/// <see cref="E.extension(int).set_MyIndexer(string, int)"/>
/// <see cref="E.extension(int).set_MyIndexer"/>
/// <see cref="E.get_MyIndexer(int, string)"/>
/// <see cref="E.get_MyIndexer"/>
/// <see cref="E.set_MyIndexer(int, string, int)"/>
/// <see cref="E.set_MyIndexer"/>
static class C
{
}
""";

        var libComp = CreateCompilation(libSrc);

        var comp = CreateCompilation(src, references: [libComp.EmitToImageReference()], parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension(int).this[string], System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s] { get; set; })",
            "(E.extension(int).get_MyIndexer(string), System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].get)",
            "(E.extension(int).get_MyIndexer, System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].get)",
            "(E.extension(int).set_MyIndexer(string, int), void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].set)",
            "(E.extension(int).set_MyIndexer, void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].set)",
            "(E.get_MyIndexer(int, string), System.Int32 E.get_MyIndexer(System.Int32 i, System.String s))",
            "(E.get_MyIndexer, System.Int32 E.get_MyIndexer(System.Int32 i, System.String s))",
            "(E.set_MyIndexer(int, string, int), void E.set_MyIndexer(System.Int32 i, System.String s, System.Int32 value))",
            "(E.set_MyIndexer, void E.set_MyIndexer(System.Int32 i, System.String s, System.Int32 value))"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_04()
    {
        // generic
        var src = """
/// <see cref="E.extension{U}(int).this[U]"/>
/// <see cref="E.extension{U}(int).get_Item(U)"/>
/// <see cref="E.extension{U}(int).get_Item"/>
/// <see cref="E.extension{U}(int).set_Item(U, int)"/>
/// <see cref="E.extension{U}(int).set_Item"/>
/// <see cref="E.get_Item{U}(int, U)"/>
/// <see cref="E.get_Item"/>
/// <see cref="E.set_Item{U}(int, U, int)"/>
/// <see cref="E.set_Item"/>
public static class E
{
    extension<T>(int i)
    {
        /// <summary></summary>
        public int this[T t]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension{U}(int).this[U], System.Int32 E.<G>$B8D310208B4544F25EEBACB9990FC73B<U>.this[U t] { get; set; })",
            "(E.extension{U}(int).get_Item(U), System.Int32 E.<G>$B8D310208B4544F25EEBACB9990FC73B<U>.this[U t].get)",
            "(E.extension{U}(int).get_Item, System.Int32 E.<G>$B8D310208B4544F25EEBACB9990FC73B<U>.this[U t].get)",
            "(E.extension{U}(int).set_Item(U, int), void E.<G>$B8D310208B4544F25EEBACB9990FC73B<U>.this[U t].set)",
            "(E.extension{U}(int).set_Item, void E.<G>$B8D310208B4544F25EEBACB9990FC73B<U>.this[U t].set)",
            "(E.get_Item{U}(int, U), System.Int32 E.get_Item<U>(System.Int32 i, U t))",
            "(E.get_Item, System.Int32 E.get_Item<T>(System.Int32 i, T t))",
            "(E.set_Item{U}(int, U, int), void E.set_Item<U>(System.Int32 i, U t, System.Int32 value))",
            "(E.set_Item, void E.set_Item<T>(System.Int32 i, T t, System.Int32 value))"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void XmlDoc_01()
    {
        var src = """
static class E
{
    /// <summary>Summary for extension block</summary>
    /// <typeparam name="T">Description for T</typeparam>
    /// <param name="t">Description for t</param>
    extension<T>(T t)
    {
        /// <summary>Summary for indexer with references to <typeparamref name="T"/> and <paramref name="t"/> and <paramref name="s"/>.</summary>
        /// <param name="s">Description for s</param>
        public int this[string s]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";
        var comp = CreateCompilation(src, assemblyName: "assembly", parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var expected = """
<?xml version="1.0"?>
<doc>
    <assembly>
        <name>assembly</name>
    </assembly>
    <members>
        <member name="M:E.get_Item``1(``0,System.String)">
            <inheritdoc cref="P:E.&lt;G&gt;$8048A6C8BE30A622530249B904B537EB`1.Item(System.String)"/>
        </member>
        <member name="M:E.set_Item``1(``0,System.String,System.Int32)">
            <inheritdoc cref="P:E.&lt;G&gt;$8048A6C8BE30A622530249B904B537EB`1.Item(System.String)"/>
        </member>
        <member name="T:E.&lt;G&gt;$8048A6C8BE30A622530249B904B537EB`1.&lt;M&gt;$D1693D81A12E8DED4ED68FE22D9E856F">
            <summary>Summary for extension block</summary>
            <typeparam name="T">Description for T</typeparam>
            <param name="t">Description for t</param>
        </member>
        <member name="P:E.&lt;G&gt;$8048A6C8BE30A622530249B904B537EB`1.Item(System.String)">
            <summary>Summary for indexer with references to <typeparamref name="T"/> and <paramref name="t"/> and <paramref name="s"/>.</summary>
            <param name="s">Description for s</param>
        </member>
    </members>
</doc>
""";
        AssertEx.Equal(expected, GetDocumentationCommentText(comp));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.SequenceEqual(["(T, T)", "(t, T t)", "(T, T)", "(t, T t)", "(s, System.String s)", "(s, System.String s)"],
            PrintXmlNameSymbols(tree, model));
    }

    [Fact]
    public void XmlDoc_02()
    {
        var src = """
static class E
{
    extension<T>(T t)
    {
        public int this[string s]
        {
            /// <summary>Summary for getter</summary>
            get => throw null;

            /// <summary>Summary for setter</summary>
            set => throw null;
        }
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (7,13): warning CS1587: XML comment is not placed on a valid language element
            //             /// <summary>Summary for getter</summary>
            Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/").WithLocation(7, 13),
            // (10,13): warning CS1587: XML comment is not placed on a valid language element
            //             /// <summary>Summary for setter</summary>
            Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/").WithLocation(10, 13));
    }

    [Theory, CombinatorialData]
    public void CallerArgumentExpression_01(bool useCompilationReference)
    {
        var callerSrc = """
_ = 42[0];
E.get_Item(43, 0);

""";
        var src = callerSrc + """
public static class E
{
    extension(int i)
    {
        public int this[int j, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(i))] string caller = ""]
        {
            get { System.Console.Write($"{caller}={i} "); return 0; }
        }
    }
}
""" + CallerArgumentExpressionAttributeDefinition;

        var expectedOutput = """
42=42 43=43
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        var comp2 = CreateCompilation(callerSrc, references: [AsReference(comp, useCompilationReference)]);
        CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Fact]
    public void CallerArgumentExpression_02()
    {
        var src = """
static class E
{
    extension(string s)
    {
        public int this[int i, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(caller))] string caller = ""] => 0;
    }
}
""" + CallerArgumentExpressionAttributeDefinition;

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,33): warning CS8965: The CallerArgumentExpressionAttribute applied to parameter 'caller' will have no effect because it's self-referential.
            //         public int this[int i, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(caller))] string caller = ""] => 0;
            Diagnostic(ErrorCode.WRN_CallerArgumentExpressionAttributeSelfReferential, "System.Runtime.CompilerServices.CallerArgumentExpression").WithArguments("caller").WithLocation(5, 33));
    }

    [Theory, CombinatorialData]
    public void CallerArgumentExpression_06(bool useCompilationReference)
    {
        // non-static extension, caller expression refers to last parameter
        var callerSrc = """
_ = 42[43, s2: "A"];
_ = E.get_Item(42, 43, s2: "B");

""";

        var src = callerSrc + """
public static class E
{
    extension(int i)
    {
        public int this[int j, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(s2))] string caller = "", string s2 = ""]
        {
            get { System.Console.Write($"{caller}={s2} "); return 0; }
        }
    }
}
""" + CallerArgumentExpressionAttributeDefinition;

        var expectedOutput = """
"A"=A "B"=B
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        var comp2 = CreateCompilation(callerSrc, references: [AsReference(comp, useCompilationReference)]);
        CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void CallerArgumentExpression_16(bool useCompilationReference)
    {
        var callerSrc = """
_ = 42[43, s2: "C"];

""";
        var src = """
public static class E
{
    extension(int i)
    {
        public int this[int j, string s1 = "B", string s2 = "",
            [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(i))] string expr_s0 = "",
            [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(s1))] string expr_s1 = "",
            [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(s2))] string expr_s2 = ""]
        {
            get { System.Console.Write($"{expr_s0}={i} {expr_s1}={s1} {expr_s2}={s2}"); return 0; }
        }
    }
}
""" + CallerArgumentExpressionAttributeDefinition;

        var expectedOutput = """
42=42 =B "C"=C
""";
        var comp = CreateCompilation([src, callerSrc]);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        var libComp = CreateCompilation(src);
        var comp2 = CreateCompilation(callerSrc, references: [AsReference(libComp, useCompilationReference)]);
        CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Fact]
    public void CallerArgumentExpression_17()
    {
        var src = """
public static class E
{
    extension(string s)
    {
        public int this[int j, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))] string expr_value = ""]
        {
            set { }
        }
    }
}
""" + CallerArgumentExpressionAttributeDefinition;

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,97): error CS0103: The name 'value' does not exist in the current context
            //         public int this[int j, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))] string expr_value = ""]
            Diagnostic(ErrorCode.ERR_NameNotInContext, "value").WithArguments("value").WithLocation(5, 97));
    }

    [Fact]
    public void CallerArgumentExpression_18()
    {
        // in indexer access
        var src = """
_ = new object()[0];

public static class E
{
    extension(object o)
    {
        public int this[int j, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(o))] string expr_o = ""]
        {
            get { System.Console.Write($"{expr_o}={o}"); return 0; }
        }
    }
}
""" + CallerArgumentExpressionAttributeDefinition;

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "new object()=System.Object").VerifyDiagnostics();
    }

    [Fact]
    public void CallerArgumentExpression_19()
    {
        // in object initializer
        var src = """
_ = new C() { [0] = 1 };

public static class E
{
    extension(C c)
    {
        public int this[int j, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(c))] string expr_c = ""]
        {
            set { System.Console.Write($"{expr_c}={c}"); }
        }
    }
}

public class C { }
""" + CallerArgumentExpressionAttributeDefinition;

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "C=C").VerifyDiagnostics();
    }

    [Fact]
    public void CallerArgumentExpression_20()
    {
        // in list pattern
        var src = """
_ = new C() is [0];

public static class E
{
    extension(C c)
    {
        public int this[System.Index i, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(c))] string expr_c = ""]
        {
            get { System.Console.Write($"{expr_c}={c}"); return 0; }
        }
    }
}

public class C
{
    public int Length => 1;
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("[0]=C"), verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void CallerMemberName_01()
    {
        var src = """
_ = 42[43];

public static class E
{
    extension(int i)
    {
        public int this[int j, [System.Runtime.CompilerServices.CallerMemberName] string s = ""]
        {
            get { System.Console.Write(s); return 0; }
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("<Main>$"), verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void CallerMemberName_02()
    {
        var src = """
_ = 42[43];

public static class E
{
    extension(int i)
    {
        public int this[int j]
        {
            get
            {
                local();
                return 0;

                void local([System.Runtime.CompilerServices.CallerMemberName] string s = "")
                {
                    System.Console.Write(s);
                }
            }
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("Item"), verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void CallerMemberName_03()
    {
        var src = """
_ = 42[43];

public static class E
{
    extension(int i)
    {
        [System.Runtime.CompilerServices.IndexerName("MyIndexer")]
        public int this[int j]
        {
            get
            {
                local();
                return 0;

                void local([System.Runtime.CompilerServices.CallerMemberName] string s = "")
                {
                    System.Console.Write(s);
                }
            }
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("MyIndexer"), verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void CallerMemberName_04()
    {
        var src = """
int i = 43;
_ = 42[in i];

public static class E
{
    extension(int i)
    {
        public int this[in int j, [System.Runtime.CompilerServices.CallerMemberName] string s = ""]
        {
            get { System.Console.Write(s); return 0; }
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("<Main>$"), verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void ElementAccess_01()
    {
        var src = """
public unsafe class C
{
    public static void Main()
    {
        int[] a = [1, 2, 3];
        fixed (int* i = a)
        {
            System.Console.Write(i[1]);
        }
    }
}

public static unsafe class E
{
    extension(int* p)
    {
        public int this[int i]
        {
            get => throw null;
        }
    }
}
""";

        CreateCompilation(src, options: TestOptions.UnsafeDebugExe).VerifyEmitDiagnostics(
            // (15,15): error CS1103: The receiver parameter of an extension cannot be of type 'int*'
            //     extension(int* p)
            Diagnostic(ErrorCode.ERR_BadTypeforThis, "int*").WithArguments("int*").WithLocation(15, 15));
    }

    [Fact]
    public void ElementAccess_02()
    {
        var src = """
int[] a = [1, 2, 3];
System.Console.Write(a[1]);

public static class E
{
    extension(int[] a)
    {
        public int this[int i]
        {
            get => throw null;
        }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "2").VerifyDiagnostics();
    }

    [Fact]
    public void ElementAccess_03()
    {
        var src = """
string s = "abc";
System.Console.Write(s[1]);

public static class E
{
    extension(string s)
    {
        public int this[int i]
        {
            get => throw null;
        }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "b").VerifyDiagnostics();
    }

    [Fact]
    public void Usings_01()
    {
        var src = """
using N;

_ = 42[43];

namespace N
{
    public static class E
    {
        extension(int i)
        {
            public int this[int j]
            {
                get { System.Console.Write("ran"); return 10; }
            }
        }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact]
    public void Usings_02()
    {
        var src = """
using static N.E;

_ = 42[43];

namespace N
{
    public static class E
    {
        extension(int i)
        {
            public int this[int j]
            {
                get { System.Console.Write("ran"); return 10; }
            }
        }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact]
    public void Usings_03()
    {
        var src = """
using N1;
using N2;

_ = 42[43];

namespace N1
{
    public static class E
    {
        extension(int i)
        {
            public int this[int j]
            {
                get { System.Console.Write("ran"); return 10; }
            }
        }
    }
}

namespace N2
{
    public static class E
    {
        extension(long i)
        {
            public int this[int j] => throw null;
        }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact]
    public void Usings_04()
    {
        var src = """
using N1;
using N2;

_ = 42[43];

namespace N1
{
    public static class E
    {
        extension(int i)
        {
            public int this[int j]
            {
                get { System.Console.Write("ran"); return 10; }
            }
        }
    }
}

namespace N2
{
    public static class E
    {
        extension(int i)
        {
            public void M() { }
        }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics(
            // (2,1): hidden CS8019: Unnecessary using directive.
            // using N2;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N2;").WithLocation(2, 1));
    }

    [Fact]
    public void RefOmittedComCall_01()
    {
        // For COM import type, omitting the ref is allowed, but indexers with ref/out parameters are disallowed

        // [ComImport, Guid("1234C65D-1234-447A-B786-64682CBEF136")]
        //public class C { }
        //
        //public static class E
        //{
        //    extension(C c)
        //    {
        //        public int this[ref short p] { get { return 0; } }
        //    }
        //}
        var ilSrc = """
.class public auto ansi import beforefieldinit C
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = (
        01 00 24 31 32 33 34 43 36 35 44 2d 31 32 33 34
        2d 34 34 37 41 2d 42 37 38 36 2d 36 34 36 38 32
        43 42 45 46 31 33 36 00 00
    )
    .method public hidebysig specialname rtspecialname
        instance void .ctor () runtime managed internalcall
    {
    }
}

.class public auto ansi abstract sealed beforefieldinit E
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<G>$9794DAFCCB9E752B29BFD6350ADA77F2'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
            01 00 04 49 74 65 6d 00 00
        )
        .class nested public auto ansi abstract sealed specialname '<M>$73AD9F89912BC4337338E3DE7182B785'
            extends System.Object
        {
            // Methods
            .method public hidebysig specialname static void '<Extension>$' ( class C c ) cil managed
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                ret
            }
        }
        .method public hidebysig specialname instance int32 get_Item ( int16& p ) cil managed
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 37 33 41 44 39 46 38 39 39
                31 32 42 43 34 33 33 37 33 33 38 45 33 44 45 37
                31 38 32 42 37 38 35 00 00
            )
            IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            IL_0005: throw
        }
        .property instance int32 Item( int16& p )
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 37 33 41 44 39 46 38 39 39
                31 32 42 43 34 33 33 37 33 33 38 45 33 44 45 37
                31 38 32 42 37 38 35 00 00
            )
            .get instance int32 E/'<G>$9794DAFCCB9E752B29BFD6350ADA77F2'::get_Item(int16&)
        }
    }
    .method public hidebysig static int32 get_Item ( class C c, int16& p ) cil managed
    {
        ldc.i4.0
        ret
    }
}
""" + ExtensionMarkerAttributeIL;

        string source = """
short x = 123;
C c = new C();
_ = c[ref x];
_ = c[x];
""";
        var comp = CreateCompilationWithIL(source, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (3,5): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            // _ = c[ref x];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "c[ref x]").WithArguments("C").WithLocation(3, 5),
            // (4,5): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            // _ = c[x];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "c[x]").WithArguments("C").WithLocation(4, 5));
    }

    [Fact]
    public void RefOmittedComCall_02()
    {
        string source = @"
using System;
using System.Runtime.InteropServices;

C c = default;
c.M();
c.M2();

[ComImport, Guid(""1234C65D-1234-447A-B786-64682CBEF136"")]
class C { }

static class E
{
    extension(ref C c)
    {
        public void M() { }
    }
    public static void M2(this ref C c) { }
}
";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (6,3): error CS1061: 'C' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
            // c.M();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("C", "M").WithLocation(6, 3),
            // (7,3): error CS1061: 'C' does not contain a definition for 'M2' and no accessible extension method 'M2' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
            // c.M2();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M2").WithArguments("C", "M2").WithLocation(7, 3),
            // (14,19): error CS9300: The 'ref' receiver parameter of an extension block must be a value type or a generic type constrained to struct.
            //     extension(ref C c)
            Diagnostic(ErrorCode.ERR_RefExtensionParameterMustBeValueTypeOrConstrainedToOne, "C").WithLocation(14, 19),
            // (18,24): error CS8337: The first parameter of a 'ref' extension method 'M2' must be a value type or a generic type constrained to struct.
            //     public static void M2(this ref C c) { }
            Diagnostic(ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne, "M2").WithArguments("M2").WithLocation(18, 24));
    }

    [Fact]
    public void Dynamic_01()
    {
        var src = """
dynamic d = new object();
_ = new object()[d];
_ = new object().M(d);

static class E
{
    extension(object o)
    {
        public int this[object o2] { get => 0; }
        public void M(object o2) { }
    }
}
""";
        // PROTOTYPE extension indexer access with dynamic argument should be disallowed
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (3,5): error CS1973: 'object' has no applicable method named 'M' but appears to have an extension method by that name. Extension methods cannot be dynamically dispatched. Consider casting the dynamic arguments or calling the extension method without the extension method syntax.
            // _ = new object().M(d);
            Diagnostic(ErrorCode.ERR_BadArgTypeDynamicExtension, "new object().M(d)").WithArguments("object", "M").WithLocation(3, 5));
    }

    [Fact]
    public void Dynamic_04()
    {
        var src = """
int i = 42;
_ = i[i];

static class E
{
    extension(object o)
    {
        public int this[dynamic d] { get { System.Console.Write(d); return 0; } }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("42"), verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void Dynamic_05()
    {
        var src = """
try
{
    dynamic d = 42;
    _ = d[0];
}
catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
{
    System.Console.Write(e.Message);
}

static class E
{
    extension(object o)
    {
        public int this[int i] => 0;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("Cannot apply indexing with [] to an expression of type 'int'"), verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();
    }

    [Fact]
    public void CheckAndCoerceArguments_01()
    {
        // Irregular/legacy behavior for indexer arguments (we skip the safety check for usage of pointer types in indexer arguments)
        var libSrc = """
public static unsafe class E
{
    extension(int i)
    {
        public int this[int* j]
        {
            get => throw null;
            set => throw null;
        }
    }
}
""";
        var libComp = CreateCompilation(libSrc, options: TestOptions.UnsafeDebugDll);

        var src = """
_ = 42[null];
E.get_Item(42, null);
""";
        var comp = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        comp.VerifyEmitDiagnostics(
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // E.get_Item(42, null);
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "E.get_Item(42, null)").WithLocation(2, 1),
            // (2,16): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // E.get_Item(42, null);
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "null").WithLocation(2, 16));

        // Compared with non-extension indexers
        libSrc = """
public unsafe class C
{
    public int this[int* j]
    {
        get => throw null;
        set => throw null;
    }
}
""";
        libComp = CreateCompilation(libSrc, options: TestOptions.UnsafeDebugDll);

        src = """
_ = new C()[null];
""";
        comp = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void ConditionalAttribute_01()
    {
        var src = """
static class E
{
    extension(int i)
    {
        [System.Diagnostics.Conditional("DEBUG")]
        public int this[int j] => 0;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,10): error CS0592: Attribute 'System.Diagnostics.Conditional' is not valid on this declaration type. It is only valid on 'class, method' declarations.
            //         [System.Diagnostics.Conditional("DEBUG")]
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "System.Diagnostics.Conditional").WithArguments("System.Diagnostics.Conditional", "class, method").WithLocation(5, 10));
    }

    [Fact]
    public void ConditionalAttribute_02()
    {
        var src = """
static class E
{
    extension(int i)
    {
        public int this[int j]
        {
            [System.Diagnostics.Conditional("DEBUG")]
            get => 0;
        }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (7,14): error CS1667: Attribute 'System.Diagnostics.Conditional' is not valid on property or event accessors. It is only valid on 'class, method' declarations.
            //             [System.Diagnostics.Conditional("DEBUG")]
            Diagnostic(ErrorCode.ERR_AttributeNotOnAccessor, "System.Diagnostics.Conditional").WithArguments("System.Diagnostics.Conditional", "class, method").WithLocation(7, 14));
    }

    [Fact, CompilerTrait(CompilerFeature.RefLifetime)]
    public void RefAnalysis_Indexing_01()
    {
        string source = """
class C
{
    ref int M2()
    {
        int i = 0;
        return ref i[1];
    }

    ref int M3()
    {
        int i = 0;
        return ref E.get_Item(ref i, 1);
    }
}

static class E
{
    extension(ref int i)
    {
        public ref int this[int j] => ref i;
    }
}
""";

        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (6,20): error CS8168: Cannot return local 'i' by reference because it is not a ref local
            //         return ref i[1];
            Diagnostic(ErrorCode.ERR_RefReturnLocal, "i").WithArguments("i").WithLocation(6, 20),
            // (6,20): error CS8347: Cannot use a result of 'E.extension(ref int).this[int]' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
            //         return ref i[1];
            Diagnostic(ErrorCode.ERR_EscapeCall, "i[1]").WithArguments("E.extension(ref int).this[int]", "i").WithLocation(6, 20),
            // (12,20): error CS8347: Cannot use a result of 'E.get_Item(ref int, int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
            //         return ref E.get_Item(ref i, 1);
            Diagnostic(ErrorCode.ERR_EscapeCall, "E.get_Item(ref i, 1)").WithArguments("E.get_Item(ref int, int)", "i").WithLocation(12, 20),
            // (12,35): error CS8168: Cannot return local 'i' by reference because it is not a ref local
            //         return ref E.get_Item(ref i, 1);
            Diagnostic(ErrorCode.ERR_RefReturnLocal, "i").WithArguments("i").WithLocation(12, 35));
    }

    [Fact, CompilerTrait(CompilerFeature.RefLifetime)]
    public void RefAnalysis_Indexing_02()
    {
        string source = """
class C
{
    ref int M2()
    {
        int i = 0;
        ref int ri = ref i;
        return ref ri[0];
    }

    ref int M3()
    {
        int i = 0;
        ref int ri = ref i;
        return ref E.get_Item(ref ri, 0);
    }
}

static class E
{
    extension(ref int i)
    {
        public ref int this[int j] => ref i;
    }
}
""";

        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (7,20): error CS8157: Cannot return 'ri' by reference because it was initialized to a value that cannot be returned by reference
            //         return ref ri[0];
            Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "ri").WithArguments("ri").WithLocation(7, 20),
            // (7,20): error CS8347: Cannot use a result of 'E.extension(ref int).this[int]' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
            //         return ref ri[0];
            Diagnostic(ErrorCode.ERR_EscapeCall, "ri[0]").WithArguments("E.extension(ref int).this[int]", "i").WithLocation(7, 20),
            // (14,20): error CS8347: Cannot use a result of 'E.get_Item(ref int, int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
            //         return ref E.get_Item(ref ri, 0);
            Diagnostic(ErrorCode.ERR_EscapeCall, "E.get_Item(ref ri, 0)").WithArguments("E.get_Item(ref int, int)", "i").WithLocation(14, 20),
            // (14,35): error CS8157: Cannot return 'ri' by reference because it was initialized to a value that cannot be returned by reference
            //         return ref E.get_Item(ref ri, 0);
            Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "ri").WithArguments("ri").WithLocation(14, 35));
    }

    [Fact, CompilerTrait(CompilerFeature.RefLifetime)]
    public void RefAnalysis_Indexing_03()
    {
        string source = """
class C
{
    RS MA(RS rs) => rs[0];
    RS MB(RS rs) => E.get_Item(rs, 0);

    ref RS MC(ref RS rs) => ref rs[""];
    ref RS MD(ref RS rs) => ref E.get_Item(ref rs, "");
}

static class E
{
    extension(RS rs)
    {
        public RS this[int i] => rs;
    }
    extension(ref RS rs)
    {
        public ref RS this[string s] => ref rs;
    }
}

ref struct RS { }
""";

        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
    }

    [Fact, CompilerTrait(CompilerFeature.RefLifetime)]
    public void RefAnalysis_Indexing_07()
    {
        string source = """
class C
{
    void MA(ref readonly int j)
    {
        int i = 0;
        j = ref i[1];
    }
}

static class E
{
    extension(ref readonly int i)
    {
        public ref readonly int this[int j] => ref i;
    }
}
""";

        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (6,9): error CS8374: Cannot ref-assign 'i[1]' to 'j' because 'i[1]' has a narrower escape scope than 'j'.
            //         j = ref i[1];
            Diagnostic(ErrorCode.ERR_RefAssignNarrower, "j = ref i[1]").WithArguments("j", "i[1]").WithLocation(6, 9));
    }

    [Fact, CompilerTrait(CompilerFeature.RefLifetime)]
    public void RefAnalysis_Indexing_08()
    {
        // Based on RefLikeEscapeMixingIndexer3
        var src = """
using System;

public static class E
{
    extension(S s)
    {
        public int this[Span<byte> span] { readonly get => 0; set {} }
    }
}

public ref struct S
{
    static void M(S s)
    {
        Span<byte> span = stackalloc byte[10];

        // `span` can't escape into `s` here because the get is readonly
        _ = s[span];

        s[span] = 42; // 1
        s[span]++; // 2
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (7,53): error CS0106: The modifier 'readonly' is not valid for this item
            //         public int this[Span<byte> span] { readonly get => 0; set {} }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("readonly").WithLocation(7, 53));
    }

    [Fact, CompilerTrait(CompilerFeature.RefLifetime)]
    public void RefAnalysis_Indexing_09()
    {
        var src = """
public static class E
{
    extension(object o)
    {
        public int this[int i] { get => 0; set {} }
    }
}

public ref struct S
{
    object GetReceiver(System.Span<byte> span) => throw null;
    int GetIndex(System.Span<byte> span) => throw null;
    
    void M()
    {
        System.Span<byte> span = stackalloc byte[10];
        System.Span<byte> span2 = stackalloc byte[10];

        this.GetReceiver(span)[this.GetIndex(span2)] = 42; // 1
        _ = this.GetReceiver(span)[this.GetIndex(span2)] = 42; // 2
        this.GetReceiver(span)[this.GetIndex(span2)] += 42; // 3
        this.GetReceiver(span)[this.GetIndex(span2)]++; // 4
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (19,9): error CS8350: This combination of arguments to 'S.GetReceiver(Span<byte>)' is disallowed because it may expose variables referenced by parameter 'span' outside of their declaration scope
            //         this.GetReceiver(span)[this.GetIndex(span2)] = 42; // 1
            Diagnostic(ErrorCode.ERR_CallArgMixing, "this.GetReceiver(span)").WithArguments("S.GetReceiver(System.Span<byte>)", "span").WithLocation(19, 9),
            // (19,26): error CS8352: Cannot use variable 'span' in this context because it may expose referenced variables outside of their declaration scope
            //         this.GetReceiver(span)[this.GetIndex(span2)] = 42; // 1
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span").WithArguments("span").WithLocation(19, 26),
            // (19,32): error CS8350: This combination of arguments to 'S.GetIndex(Span<byte>)' is disallowed because it may expose variables referenced by parameter 'span' outside of their declaration scope
            //         this.GetReceiver(span)[this.GetIndex(span2)] = 42; // 1
            Diagnostic(ErrorCode.ERR_CallArgMixing, "this.GetIndex(span2)").WithArguments("S.GetIndex(System.Span<byte>)", "span").WithLocation(19, 32),
            // (19,46): error CS8352: Cannot use variable 'span2' in this context because it may expose referenced variables outside of their declaration scope
            //         this.GetReceiver(span)[this.GetIndex(span2)] = 42; // 1
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span2").WithArguments("span2").WithLocation(19, 46),
            // (20,13): error CS8350: This combination of arguments to 'S.GetReceiver(Span<byte>)' is disallowed because it may expose variables referenced by parameter 'span' outside of their declaration scope
            //         _ = this.GetReceiver(span)[this.GetIndex(span2)] = 42; // 2
            Diagnostic(ErrorCode.ERR_CallArgMixing, "this.GetReceiver(span)").WithArguments("S.GetReceiver(System.Span<byte>)", "span").WithLocation(20, 13),
            // (20,30): error CS8352: Cannot use variable 'span' in this context because it may expose referenced variables outside of their declaration scope
            //         _ = this.GetReceiver(span)[this.GetIndex(span2)] = 42; // 2
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span").WithArguments("span").WithLocation(20, 30),
            // (20,36): error CS8350: This combination of arguments to 'S.GetIndex(Span<byte>)' is disallowed because it may expose variables referenced by parameter 'span' outside of their declaration scope
            //         _ = this.GetReceiver(span)[this.GetIndex(span2)] = 42; // 2
            Diagnostic(ErrorCode.ERR_CallArgMixing, "this.GetIndex(span2)").WithArguments("S.GetIndex(System.Span<byte>)", "span").WithLocation(20, 36),
            // (20,50): error CS8352: Cannot use variable 'span2' in this context because it may expose referenced variables outside of their declaration scope
            //         _ = this.GetReceiver(span)[this.GetIndex(span2)] = 42; // 2
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span2").WithArguments("span2").WithLocation(20, 50),
            // (21,9): error CS8350: This combination of arguments to 'S.GetReceiver(Span<byte>)' is disallowed because it may expose variables referenced by parameter 'span' outside of their declaration scope
            //         this.GetReceiver(span)[this.GetIndex(span2)] += 42; // 3
            Diagnostic(ErrorCode.ERR_CallArgMixing, "this.GetReceiver(span)").WithArguments("S.GetReceiver(System.Span<byte>)", "span").WithLocation(21, 9),
            // (21,26): error CS8352: Cannot use variable 'span' in this context because it may expose referenced variables outside of their declaration scope
            //         this.GetReceiver(span)[this.GetIndex(span2)] += 42; // 3
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span").WithArguments("span").WithLocation(21, 26),
            // (21,32): error CS8350: This combination of arguments to 'S.GetIndex(Span<byte>)' is disallowed because it may expose variables referenced by parameter 'span' outside of their declaration scope
            //         this.GetReceiver(span)[this.GetIndex(span2)] += 42; // 3
            Diagnostic(ErrorCode.ERR_CallArgMixing, "this.GetIndex(span2)").WithArguments("S.GetIndex(System.Span<byte>)", "span").WithLocation(21, 32),
            // (21,46): error CS8352: Cannot use variable 'span2' in this context because it may expose referenced variables outside of their declaration scope
            //         this.GetReceiver(span)[this.GetIndex(span2)] += 42; // 3
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span2").WithArguments("span2").WithLocation(21, 46),
            // (22,9): error CS8350: This combination of arguments to 'S.GetReceiver(Span<byte>)' is disallowed because it may expose variables referenced by parameter 'span' outside of their declaration scope
            //         this.GetReceiver(span)[this.GetIndex(span2)]++; // 4
            Diagnostic(ErrorCode.ERR_CallArgMixing, "this.GetReceiver(span)").WithArguments("S.GetReceiver(System.Span<byte>)", "span").WithLocation(22, 9),
            // (22,26): error CS8352: Cannot use variable 'span' in this context because it may expose referenced variables outside of their declaration scope
            //         this.GetReceiver(span)[this.GetIndex(span2)]++; // 4
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span").WithArguments("span").WithLocation(22, 26),
            // (22,32): error CS8350: This combination of arguments to 'S.GetIndex(Span<byte>)' is disallowed because it may expose variables referenced by parameter 'span' outside of their declaration scope
            //         this.GetReceiver(span)[this.GetIndex(span2)]++; // 4
            Diagnostic(ErrorCode.ERR_CallArgMixing, "this.GetIndex(span2)").WithArguments("S.GetIndex(System.Span<byte>)", "span").WithLocation(22, 32),
            // (22,46): error CS8352: Cannot use variable 'span2' in this context because it may expose referenced variables outside of their declaration scope
            //         this.GetReceiver(span)[this.GetIndex(span2)]++; // 4
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span2").WithArguments("span2").WithLocation(22, 46));
    }

    [Fact, CompilerTrait(CompilerFeature.RefLifetime)]
    public void RefAnalysis_Indexing_10()
    {
        var src = """
public static class E
{
    extension(S s)
    {
        public int this[System.Span<byte> span] { get => throw null; set { } }
    }
}

public ref struct S
{
    void M()
    {
        System.Span<byte> span = stackalloc byte[10];

        this[span] = 42;
        E.set_Item(this, span, 42);

        _ = this[span];
        E.get_Item(this, span);

        this[span] += 42;
        this[span]++;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();
    }

    [Fact, CompilerTrait(CompilerFeature.RefLifetime)]
    public void RefAnalysis_Indexing_11()
    {
        var src = """
public static class E
{
    extension(ref S s)
    {
        public int this[System.Span<byte> span] { get => throw null; set { } }
    }
}

public ref struct S
{
    void M()
    {
        System.Span<byte> span = stackalloc byte[10];

        this[span] = 42; // 1
        E.set_Item(ref this, span, 42); // 2

        _ = this[span]; // 3
        E.get_Item(ref this, span); // 4

        this[span] += 42; // 5
        this[span]++; // 6
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (15,9): error CS8350: This combination of arguments to 'E.extension(ref S).this[Span<byte>]' is disallowed because it may expose variables referenced by parameter 'span' outside of their declaration scope
            //         this[span] = 42; // 1
            Diagnostic(ErrorCode.ERR_CallArgMixing, "this[span]").WithArguments("E.extension(ref S).this[System.Span<byte>]", "span").WithLocation(15, 9),
            // (15,14): error CS8352: Cannot use variable 'span' in this context because it may expose referenced variables outside of their declaration scope
            //         this[span] = 42; // 1
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span").WithArguments("span").WithLocation(15, 14),
            // (16,9): error CS8350: This combination of arguments to 'E.set_Item(ref S, Span<byte>, int)' is disallowed because it may expose variables referenced by parameter 'span' outside of their declaration scope
            //         E.set_Item(ref this, span, 42); // 2
            Diagnostic(ErrorCode.ERR_CallArgMixing, "E.set_Item(ref this, span, 42)").WithArguments("E.set_Item(ref S, System.Span<byte>, int)", "span").WithLocation(16, 9),
            // (16,30): error CS8352: Cannot use variable 'span' in this context because it may expose referenced variables outside of their declaration scope
            //         E.set_Item(ref this, span, 42); // 2
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span").WithArguments("span").WithLocation(16, 30),
            // (18,13): error CS8350: This combination of arguments to 'E.extension(ref S).this[Span<byte>]' is disallowed because it may expose variables referenced by parameter 'span' outside of their declaration scope
            //         _ = this[span]; // 3
            Diagnostic(ErrorCode.ERR_CallArgMixing, "this[span]").WithArguments("E.extension(ref S).this[System.Span<byte>]", "span").WithLocation(18, 13),
            // (18,18): error CS8352: Cannot use variable 'span' in this context because it may expose referenced variables outside of their declaration scope
            //         _ = this[span]; // 3
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span").WithArguments("span").WithLocation(18, 18),
            // (19,9): error CS8350: This combination of arguments to 'E.get_Item(ref S, Span<byte>)' is disallowed because it may expose variables referenced by parameter 'span' outside of their declaration scope
            //         E.get_Item(ref this, span); // 4
            Diagnostic(ErrorCode.ERR_CallArgMixing, "E.get_Item(ref this, span)").WithArguments("E.get_Item(ref S, System.Span<byte>)", "span").WithLocation(19, 9),
            // (19,30): error CS8352: Cannot use variable 'span' in this context because it may expose referenced variables outside of their declaration scope
            //         E.get_Item(ref this, span); // 4
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span").WithArguments("span").WithLocation(19, 30),
            // (21,9): error CS8350: This combination of arguments to 'E.extension(ref S).this[Span<byte>]' is disallowed because it may expose variables referenced by parameter 'span' outside of their declaration scope
            //         this[span] += 42; // 5
            Diagnostic(ErrorCode.ERR_CallArgMixing, "this[span]").WithArguments("E.extension(ref S).this[System.Span<byte>]", "span").WithLocation(21, 9),
            // (21,9): error CS8350: This combination of arguments to 'E.extension(ref S).this[Span<byte>]' is disallowed because it may expose variables referenced by parameter 'span' outside of their declaration scope
            //         this[span] += 42; // 5
            Diagnostic(ErrorCode.ERR_CallArgMixing, "this[span] += 42").WithArguments("E.extension(ref S).this[System.Span<byte>]", "span").WithLocation(21, 9),
            // (21,14): error CS8352: Cannot use variable 'span' in this context because it may expose referenced variables outside of their declaration scope
            //         this[span] += 42; // 5
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span").WithArguments("span").WithLocation(21, 14),
            // (21,14): error CS8352: Cannot use variable 'span' in this context because it may expose referenced variables outside of their declaration scope
            //         this[span] += 42; // 5
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span").WithArguments("span").WithLocation(21, 14),
            // (22,9): error CS8350: This combination of arguments to 'E.extension(ref S).this[Span<byte>]' is disallowed because it may expose variables referenced by parameter 'span' outside of their declaration scope
            //         this[span]++; // 6
            Diagnostic(ErrorCode.ERR_CallArgMixing, "this[span]").WithArguments("E.extension(ref S).this[System.Span<byte>]", "span").WithLocation(22, 9),
            // (22,9): error CS8350: This combination of arguments to 'E.extension(ref S).this[Span<byte>]' is disallowed because it may expose variables referenced by parameter 'span' outside of their declaration scope
            //         this[span]++; // 6
            Diagnostic(ErrorCode.ERR_CallArgMixing, "this[span]").WithArguments("E.extension(ref S).this[System.Span<byte>]", "span").WithLocation(22, 9),
            // (22,14): error CS8352: Cannot use variable 'span' in this context because it may expose referenced variables outside of their declaration scope
            //         this[span]++; // 6
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span").WithArguments("span").WithLocation(22, 14),
            // (22,14): error CS8352: Cannot use variable 'span' in this context because it may expose referenced variables outside of their declaration scope
            //         this[span]++; // 6
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span").WithArguments("span").WithLocation(22, 14));
    }

    [Fact, CompilerTrait(CompilerFeature.RefLifetime)]
    public void RefAnalysis_ObjectCreation_01()
    {
        string source = """
class C
{
    ref int M2()
    {
        int i = 0;
        return ref i[1];
    }

    ref int M3()
    {
        int i = 0;
        return ref E.get_Item(ref i, 1);
    }
}

static class E
{
    extension(ref int i)
    {
        public ref int this[int j] => ref i;
    }
}
""";

        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (6,20): error CS8168: Cannot return local 'i' by reference because it is not a ref local
            //         return ref i[1];
            Diagnostic(ErrorCode.ERR_RefReturnLocal, "i").WithArguments("i").WithLocation(6, 20),
            // (6,20): error CS8347: Cannot use a result of 'E.extension(ref int).this[int]' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
            //         return ref i[1];
            Diagnostic(ErrorCode.ERR_EscapeCall, "i[1]").WithArguments("E.extension(ref int).this[int]", "i").WithLocation(6, 20),
            // (12,20): error CS8347: Cannot use a result of 'E.get_Item(ref int, int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
            //         return ref E.get_Item(ref i, 1);
            Diagnostic(ErrorCode.ERR_EscapeCall, "E.get_Item(ref i, 1)").WithArguments("E.get_Item(ref int, int)", "i").WithLocation(12, 20),
            // (12,35): error CS8168: Cannot return local 'i' by reference because it is not a ref local
            //         return ref E.get_Item(ref i, 1);
            Diagnostic(ErrorCode.ERR_RefReturnLocal, "i").WithArguments("i").WithLocation(12, 35));
    }

    [Fact, CompilerTrait(CompilerFeature.RefLifetime)]
    public void RefAnalysis_ObjectCreation_02()
    {
        string source = """
System.Span<byte> span = stackalloc byte[10];
_ = new S() { [span] = 42 };
E.set_Item(new S(), span, 42);

ref struct S { }

static class E
{
    extension(S s)
    {
        public int this[System.Span<byte> s2] { get => throw null; set { } }
    }
}
""";

        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();
    }

    [Fact, CompilerTrait(CompilerFeature.RefLifetime)]
    public void RefAnalysis_ObjectCreation_03()
    {
        string source = """
System.Span<byte> span = stackalloc byte[10];
_ = new S() { [span] = 42 };

S s = new S();
E.set_Item(ref s, span, 42); // 1

ref struct S { }

static class E
{
    extension(ref S s)
    {
        public int this[System.Span<byte> s2] { get => throw null; set { } }
    }
}
""";

        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (5,1): error CS8350: This combination of arguments to 'E.set_Item(ref S, Span<byte>, int)' is disallowed because it may expose variables referenced by parameter 's2' outside of their declaration scope
            // E.set_Item(ref s, span, 42); // 1
            Diagnostic(ErrorCode.ERR_CallArgMixing, "E.set_Item(ref s, span, 42)").WithArguments("E.set_Item(ref S, System.Span<byte>, int)", "s2").WithLocation(5, 1),
            // (5,19): error CS8352: Cannot use variable 'span' in this context because it may expose referenced variables outside of their declaration scope
            // E.set_Item(ref s, span, 42); // 1
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span").WithArguments("span").WithLocation(5, 19));
    }

    [Fact(Skip = "PROTOTYPE assertion in StackOptimizerPass1 due to assigning to sequence"), CompilerTrait(CompilerFeature.RefLifetime)]
    public void RefAnalysis_ObjectCreation_04()
    {
        string source = """
System.Span<byte> span = stackalloc byte[10];
_ = new S() { [span] = 42 };

S s = new S();
E.get_Item(s, span) = 42;

ref struct S { }

static class E
{
    extension(S s)
    {
        public ref int this[System.Span<byte> s2] { get => throw null; }
    }
}
""";

        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();
    }

    [Fact(Skip = "PROTOTYPE assertion in StackOptimizerPass1 due to assigning to sequence"), CompilerTrait(CompilerFeature.RefLifetime)]
    public void RefAnalysis_ObjectCreation_05()
    {
        string source = """
_ = new S() { [GetIndex()] = GetValue() };

int GetIndex() { System.Console.Write("GetIndex "); return 0; }
int GetValue() { System.Console.Write("GetValue "); return 0; }

ref struct S { }

static class E
{
    public static int Field;

    extension(S s)
    {
        public ref int this[int i] { get { System.Console.Write("get "); return ref Field; } }
    }
}
""";

        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, expectedOutput: "").VerifyDiagnostics();
    }

    [Fact, CompilerTrait(CompilerFeature.RefLifetime)]
    public void RefAnalysis_ObjectCreation_06()
    {
        string source = """
System.Span<byte> span = stackalloc byte[10];
S s1 = new S() { [span] = 42 };

S s2 = new S();
E.get_Item(ref s2, span) = 42; // 1

ref struct S { }

static class E
{
    extension(ref S s)
    {
        public ref int this[System.Span<byte> s2] { get => throw null; }
    }
}
""";

        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (5,1): error CS8350: This combination of arguments to 'E.get_Item(ref S, Span<byte>)' is disallowed because it may expose variables referenced by parameter 's2' outside of their declaration scope
            // E.get_Item(ref s2, span) = 42; // 1
            Diagnostic(ErrorCode.ERR_CallArgMixing, "E.get_Item(ref s2, span)").WithArguments("E.get_Item(ref S, System.Span<byte>)", "s2").WithLocation(5, 1),
            // (5,20): error CS8352: Cannot use variable 'span' in this context because it may expose referenced variables outside of their declaration scope
            // E.get_Item(ref s2, span) = 42; // 1
            Diagnostic(ErrorCode.ERR_EscapeVariable, "span").WithArguments("span").WithLocation(5, 20));
    }

    [Fact, CompilerTrait(CompilerFeature.RefLifetime)]
    public void RefAnalysis_ObjectCreation_07()
    {
        string source = """
System.Span<byte> span = stackalloc byte[10];
_ = new C() { [span] = 42 };
System.Console.Write(E.Field);

C c = new C();
E.get_Item(c, span) = 43;
System.Console.Write(E.Field);

class C { }

static class E
{
    public static int Field;

    extension(C c)
    {
        public ref int this[System.Span<byte> s2] { get => ref Field; }
    }
}
""";

        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("4243"), verify: Verification.Skipped).VerifyDiagnostics();
    }

    [Fact]
    public void SynthesizedAttributeOnParameters_In_01()
    {
        var src = """
class C
{
    void M(in int i)
    {
        _ = i[0];
        _ = E.get_Item(i, 0);

        _ = i[""];
        _ = E.get_Item(ref i, "");
    }
}
""";
        var libSrc = """
public static class E
{
    extension(in int i)
    {
        public int this[int j] => 0;
    }

    extension(ref int i)
    {
        public int this[string s] => 0;
    }
}
""";
        DiagnosticDescription[] expected = [
            // (8,13): error CS8329: Cannot use variable 'i' as a ref or out value because it is a readonly variable
            //         _ = i[""];
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "i").WithArguments("variable", "i").WithLocation(8, 13),
            // (9,28): error CS8329: Cannot use variable 'i' as a ref or out value because it is a readonly variable
            //         _ = E.get_Item(ref i, "");
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "i").WithArguments("variable", "i").WithLocation(9, 28)
            ];

        var comp = CreateCompilation([src, libSrc]);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc);
        CompileAndVerify(libComp, symbolValidator: validate);

        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics(expected);

        static void validate(ModuleSymbol m)
        {
            var module = (PEModuleSymbol)m;
            var getters = m.GlobalNamespace.GetTypeMember("E").GetMembers("get_Item").OfType<MethodSymbol>().ToArray();

            AssertEx.Equal("E.get_Item(in int, int)", getters[0].ToDisplayString());
            var parameterSymbol = (PEParameterSymbol)getters[0].Parameters[0];
            Assert.True(module.Module.HasIsReadOnlyAttribute(parameterSymbol.Handle));

            AssertEx.Equal("E.get_Item(ref int, string)", getters[1].ToDisplayString());
            parameterSymbol = (PEParameterSymbol)getters[1].Parameters[0];
            Assert.False(module.Module.HasIsReadOnlyAttribute(parameterSymbol.Handle));
        }
    }

    [Fact]
    public void IOperation_01()
    {
        // indexing
        var src = """
C c = new C();

/*<bind>*/
c[0] = 1;
/*</bind>*/

public static class E
{
    extension(C c)
    {
        public int this[int i]
        {
            get  => 0;
            set { }
        }
    }
}

public class C { }
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        string expectedOperationTree = """
ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'c[0] = 1')
Left:
  IPropertyReferenceOperation: System.Int32 E.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'c[0]')
    Instance Receiver:
      ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '0')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
Right:
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
""";

        VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(src, expectedOperationTree, [], targetFramework: TargetFramework.Net100);
    }

    [Fact]
    public void IOperation_02()
    {
        // list pattern, extension Index indexer
        var src = """
C c = new C();

/*<bind>*/
_ = c is [42];
/*</bind>*/

public static class E
{
    extension(C c)
    {
        public int this[System.Index i] => 42;
    }
}

public class C
{
    public int Length => 1;
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        string expectedOperationTree = """
ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: '_ = c is [42]')
Left:
  IDiscardOperation (Symbol: System.Boolean _) (OperationKind.Discard, Type: System.Boolean) (Syntax: '_')
Right:
  IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'c is [42]')
    Value:
      ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
    Pattern:
      IListPatternOperation (OperationKind.ListPattern, Type: null) (Syntax: '[42]') (InputType: C, NarrowedType: C, DeclaredSymbol: null, LengthSymbol: System.Int32 C.Length { get; }, IndexerSymbol: System.Int32 E.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.this[System.Index i] { get; })
        Patterns (1):
            IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '42') (InputType: System.Int32, NarrowedType: System.Int32)
              Value:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
""";

        VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(src, expectedOperationTree, [], targetFramework: TargetFramework.Net100);
    }

    [Fact]
    public void IOperation_03()
    {
        // list pattern, extension Length
        var src = """
C c = new C();

/*<bind>*/
_ = c is [42];
/*</bind>*/

public static class E
{
    extension(C c)
    {
        public int Length => 1;
    }
}

public class C
{
    public int this[System.Index i] => 42;
}
""";

        // PROTOTYPE where should extension Length/Count count?
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (4,10): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            // _ = c is [42];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[42]").WithArguments("C").WithLocation(4, 10));

        //string expectedOperationTree = "";
        //VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(src, expectedOperationTree, [], targetFramework: TargetFramework.Net100);
    }

    [Fact]
    public void IOperation_04()
    {
        // spread pattern, extension Range indexer
        var src = """
C c = new C();

/*<bind>*/
_ = c is [_, .. 42];
/*</bind>*/

public static class E
{
    extension(C c)
    {
        public int this[System.Range r] => 42;
    }
}

public class C
{
    public int this[System.Index i] => 42;
    public int Length => 1;
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        string expectedOperationTree = """
ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: '_ = c is [_, .. 42]')
  Left:
    IDiscardOperation (Symbol: System.Boolean _) (OperationKind.Discard, Type: System.Boolean) (Syntax: '_')
  Right:
    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'c is [_, .. 42]')
      Value:
        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
      Pattern:
        IListPatternOperation (OperationKind.ListPattern, Type: null) (Syntax: '[_, .. 42]') (InputType: C, NarrowedType: C, DeclaredSymbol: null, LengthSymbol: System.Int32 C.Length { get; }, IndexerSymbol: System.Int32 C.this[System.Index i] { get; })
          Patterns (2):
              IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32, NarrowedType: System.Int32)
              ISlicePatternOperation (OperationKind.SlicePattern, Type: null) (Syntax: '.. 42') (InputType: C, NarrowedType: C, SliceSymbol: System.Int32 E.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.this[System.Range r] { get; }
                Pattern:
                  IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '42') (InputType: System.Int32, NarrowedType: System.Int32)
                    Value:
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
""";

        VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(src, expectedOperationTree, [], targetFramework: TargetFramework.Net100);
    }

    [Fact]
    public void IOperation_05()
    {
        // object initializer
        var src = """
C c;

/*<bind>*/
c = new C { [0] = 1 };
/*</bind>*/

public static class E
{
    extension(C c)
    {
        public int this[int i] { set { } }
    }
}

public class C { }
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        string expectedOperationTree = """
ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: 'c = new C { [0] = 1 }')
Left:
  ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
Right:
  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C { [0] = 1 }')
    Arguments(0)
    Initializer:
      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ [0] = 1 }')
        Initializers(1):
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[0] = 1')
              Left:
                IPropertyReferenceOperation: System.Int32 E.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.this[System.Int32 i] { set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '[0]')
                  Instance Receiver:
                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '[0]')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '0')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Right:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
""";

        VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(src, expectedOperationTree, [], targetFramework: TargetFramework.Net100);
    }

    [Fact]
    public void IOperation_06()
    {
        // null-conditional indexing
        var src = """
C c = new C();

/*<bind>*/
c?[0] = 1;
/*</bind>*/

public static class E
{
    extension(C c)
    {
        public int this[int i] { set { } }
    }
}

public class C { }
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        string expectedOperationTree = """
IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: System.Int32?) (Syntax: 'c?[0] = 1')
Operation:
  ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
WhenNotNull:
  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[0] = 1')
    Left:
      IPropertyReferenceOperation: System.Int32 E.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.this[System.Int32 i] { set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '[0]')
        Instance Receiver:
          IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: C, IsImplicit) (Syntax: 'c')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '0')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Right:
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
""";

        VerifyOperationTreeAndDiagnosticsForTest<ConditionalAccessExpressionSyntax>(src, expectedOperationTree, [], targetFramework: TargetFramework.Net100);
    }

    [Fact]
    public void IOperation_07()
    {
        // null-conditional access
        var src = """
C c = new C();

/*<bind>*/
_ = c?[0] ?? -1;
/*</bind>*/

public static class E
{
    extension(C c)
    {
        public int this[int i] { get => 0; }
    }
}

public class C { }
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        string expectedOperationTree = """
ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '_ = c?[0] ?? -1')
Left:
  IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
Right:
  ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32) (Syntax: 'c?[0] ?? -1')
    Expression:
      IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: System.Int32?) (Syntax: 'c?[0]')
        Operation:
          ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
        WhenNotNull:
          IPropertyReferenceOperation: System.Int32 E.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.this[System.Int32 i] { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '[0]')
            Instance Receiver:
              IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: C, IsImplicit) (Syntax: 'c')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '0')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      (Identity)
    WhenNull:
      IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.Unary, Type: System.Int32, Constant: -1) (Syntax: '-1')
        Operand:
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
""";

        VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(src, expectedOperationTree, [], targetFramework: TargetFramework.Net100);
    }

    [Fact]
    public void IOperation_08()
    {
        var src = """
C c = new C();

/*<bind>*/
c[42] += 100;
/*</bind>*/

public static class E
{
    extension(C c)
    {
        public ref D this[int i] { get => throw null; }
    }

    extension(D d)
    {
        public void operator +=(int i) => throw null;
    }
}

public class C { }
public class D { }
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics();

        string expectedOperationTree = """
ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperatorMethod: void E.<G>$AFA52AAB7DF1E8FB2EB2AED850FD4E4B.op_AdditionAssignment(System.Int32 i)) (OperationKind.CompoundAssignment, Type: System.Void) (Syntax: 'c[42] += 100')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
    IPropertyReferenceOperation: ref D E.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.this[System.Int32 i] { get; } (OperationKind.PropertyReference, Type: D) (Syntax: 'c[42]')
      Instance Receiver:
        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '42')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Right:
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 100) (Syntax: '100')
""";

        VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(src, expectedOperationTree, [], targetFramework: TargetFramework.Net100);
    }

    [Fact]
    public void MissingMembers_01()
    {
        // missing DefaultMemberAttribute
        var src = """
public static class E
{
    extension(object o)
    {
        public int this[int i]
        {
            get => 0;
            set { }
        }
    }
}
""";

        var comp = CreateCompilation(src);
        comp.MakeMemberMissing(WellKnownMember.System_Reflection_DefaultMemberAttribute__ctor);
        comp.VerifyEmitDiagnostics(
            // (5,20): error CS0656: Missing compiler required member 'System.Reflection.DefaultMemberAttribute..ctor'
            //         public int this[int i]
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "this").WithArguments("System.Reflection.DefaultMemberAttribute", ".ctor").WithLocation(5, 20));
    }

    [Fact]
    public void Nullability_ReceiverConversion_01()
    {
        var src = """
#nullable enable

(object, object)? o = (new object(), new object());
_ = o[0];
o[0] = 42;

static class E
{
    extension((object?, object?)? o)
    {
        public int this[int i] { get => throw null!; set => throw null!; }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (4,5): warning CS8620: Argument of type '(object, object)?' cannot be used for parameter 'o' of type '(object?, object?)?' in 'E.extension((object?, object?)?)' due to differences in the nullability of reference types.
            // _ = o[0];
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "o").WithArguments("(object, object)?", "(object?, object?)?", "o", "E.extension((object?, object?)?)").WithLocation(4, 5),
            // (5,1): warning CS8620: Argument of type '(object, object)?' cannot be used for parameter 'o' of type '(object?, object?)?' in 'E.extension((object?, object?)?)' due to differences in the nullability of reference types.
            // o[0] = 42;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "o").WithArguments("(object, object)?", "(object?, object?)?", "o", "E.extension((object?, object?)?)").WithLocation(5, 1));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81851")]
    public void Nullability_ReceiverConversion_02()
    {
        // initializer
        var src = """
#nullable enable

_ = new System.Nullable<System.ValueTuple<object, object>>() { [0] = null };

public static class E
{
    extension((object?, object?)? t)
    {
        public string? this[int i] { set { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,64): warning CS8620: Argument of type '(object, object)?' cannot be used for parameter 't' of type '(object?, object?)?' in 'E.extension((object?, object?)?)' due to differences in the nullability of reference types.
            // _ = new System.Nullable<System.ValueTuple<object, object>>() { [0] = null };
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "[0]").WithArguments("(object, object)?", "(object?, object?)?", "t", "E.extension((object?, object?)?)").WithLocation(3, 64));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81851")]
    public void Nullability_ReceiverConversion_03()
    {
        // initializer, target-typed
        var src = """
#nullable enable

System.Nullable<System.ValueTuple<object, object>> t = new() { [0] = null };

public static class E
{
    extension((object?, object?)? t)
    {
        public string? this[int i] { set { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,64): error CS0021: Cannot apply indexing with [] to an expression of type '(object, object)'
            // System.Nullable<System.ValueTuple<object, object>> t = new() { [0] = null };
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[0]").WithArguments("(object, object)").WithLocation(3, 64));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81851")]
    public void Nullability_ReceiverConversion_04()
    {
        // initializer, target-typed
        var src = """
#nullable enable

System.Nullable<System.ValueTuple<object, object>> t = new() { [0] = null };

public static class E
{
    extension((object?, object?) t)
    {
        public string? this[int i] { set { } }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81851")]
    public void Nullability_ReceiverConversion_05()
    {
        // nested initializer
        var src = """
#nullable enable

_ = new System.Nullable<System.ValueTuple<object, object>>() { [0] = { F = "" } };

public static class E
{
    extension((object?, object?)? t)
    {
        public C this[int i] { get => new C(); }
    }
}

public class C
{
    public string? F;
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,64): warning CS8620: Argument of type '(object, object)?' cannot be used for parameter 't' of type '(object?, object?)?' in 'E.extension((object?, object?)?)' due to differences in the nullability of reference types.
            // _ = new System.Nullable<System.ValueTuple<object, object>>() { [0] = { F = "" } };
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "[0]").WithArguments("(object, object)?", "(object?, object?)?", "t", "E.extension((object?, object?)?)").WithLocation(3, 64));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81851")]
    public void Nullability_ReceiverConversion_06()
    {
        // list pattern
        var src = """
#nullable enable

(object, object)? o = (new object(), new object());
_ = o is [var x];

static class E
{
    extension((object?, object?)? o)
    {
        public int this[System.Index i] { get => throw null!; }
    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public ValueTuple(T1 t1, T2 t2) { }
        public int Length => throw null!;
    }
}
""";
        CreateCompilation(src, targetFramework: TargetFramework.Net100).VerifyEmitDiagnostics(
            // (4,10): error CS0021: Cannot apply indexing with [] to an expression of type '(object, object)'
            // _ = o is [var x];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[var x]").WithArguments("(object, object)").WithLocation(4, 10));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81851")]
    public void Nullability_ReceiverConversion_07()
    {
        // list pattern
        var src = """
#nullable enable

(object, object)? o = (new object(), new object());
_ = o is [var x];

static class E
{
    extension((object?, object?) o)
    {
        public int this[System.Index i] { get => throw null!; }
    }

    extension((object, object) o)
    {
        public int Length { get => throw null!; }
    }
}
""";
        // PROTOTYPE where should extension Length/Count count?
        CreateCompilation(src, targetFramework: TargetFramework.Net100).VerifyEmitDiagnostics(
            // (4,10): error CS8985: List patterns may not be used for a value of type '(object, object)'. No suitable 'Length' or 'Count' property was found.
            // _ = o is [var x];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[var x]").WithArguments("(object, object)").WithLocation(4, 10));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81851")]
    public void Nullability_ReceiverConversion_08()
    {
        // 'with' expression
        var src = """
#nullable enable

(object, object)? o = (new object(), new object());
_ = o with { [0] = 42 };

static class E
{
    extension((object?, object?)? o)
    {
        public int this[int i] { set { } }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (4,14): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            // _ = o with { [0] = 42 };
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "[0]").WithLocation(4, 14),
            // (4,14): error CS0747: Invalid initializer member declarator
            // _ = o with { [0] = 42 };
            Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "[0] = 42").WithLocation(4, 14));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81851")]
    public void Nullability_ReceiverConversion_09()
    {
        // increment operator
        var src = """
#nullable enable

(object, object)? o = (new object(), new object());
o++;

static class E
{
    extension((object?, object?)?)
    {
        public static (object?, object?)? operator++((object?, object?)? t) => t;
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (4,1): warning CS8619: Nullability of reference types in value of type '(object?, object?)?' doesn't match target type '(object, object)?'.
            // o++;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "o++").WithArguments("(object?, object?)?", "(object, object)?").WithLocation(4, 1));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81851")]
    public void Nullability_ReceiverConversion_10()
    {
        // compound assignment
        var src = """
#nullable enable

(object, object)? o = (new object(), new object());
o.P++;

static class E
{
    extension((object?, object?)? o)
    {
        public int P { get => throw null!; set { } }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (4,1): warning CS8620: Argument of type '(object, object)?' cannot be used for parameter 'o' of type '(object?, object?)?' in 'E.extension((object?, object?)?)' due to differences in the nullability of reference types.
            // o.P++;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "o").WithArguments("(object, object)?", "(object?, object?)?", "o", "E.extension((object?, object?)?)").WithLocation(4, 1));
    }

    [Fact]
    public void Nullability_Setter_01()
    {
        // generic extension parameter, property write access, notnull constraint
        var src = """
#nullable enable

object? oNull = null;
oNull[0] = null; // 1

object? oNull2 = null;
oNull2[0] = new object(); // 2

object? oNotNull = new object();
oNotNull[0] = null; // 3
E.set_Item(oNotNull, 0, null); // 4

oNotNull[0] = new object();

oNotNull?[0] = null; // 5

static class E
{
    extension<T>(T t) where T : notnull
    {
        public T this[int i] { set => throw null!; }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (4,1): warning CS8714: The type 'object?' cannot be used as type parameter 'T' in the generic type or method 'E.extension<T>(T)'. Nullability of type argument 'object?' doesn't match 'notnull' constraint.
            // oNull[0] = null; // 1
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "oNull[0]").WithArguments("E.extension<T>(T)", "T", "object?").WithLocation(4, 1),
            // (7,1): warning CS8714: The type 'object?' cannot be used as type parameter 'T' in the generic type or method 'E.extension<T>(T)'. Nullability of type argument 'object?' doesn't match 'notnull' constraint.
            // oNull2[0] = new object(); // 2
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "oNull2[0]").WithArguments("E.extension<T>(T)", "T", "object?").WithLocation(7, 1),
            // (10,15): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // oNotNull[0] = null; // 3
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(10, 15),
            // (11,1): warning CS8714: The type 'object?' cannot be used as type parameter 'T' in the generic type or method 'E.set_Item<T>(T, int, T)'. Nullability of type argument 'object?' doesn't match 'notnull' constraint.
            // E.set_Item(oNotNull, 0, null); // 4
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "E.set_Item").WithArguments("E.set_Item<T>(T, int, T)", "T", "object?").WithLocation(11, 1),
            // (15,16): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // oNotNull?[0] = null; // 5
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(15, 16));
    }

    [Fact]
    public void Nullability_Setter_02()
    {
        // generic extension parameter, property write access
        var src = """
#nullable enable

object? oNull = null;
oNull[0] = null;

object? oNull2 = null;
oNull2[0] = new object();

object? oNotNull = new object();
oNotNull[0] = null; // 1
E.set_Item(oNotNull, 0, null);

oNotNull[0] = new object();

oNotNull?[0] = null; // 2

static class E
{
    extension<T>(T t)
    {
        public T this[int i] { set => throw null!; }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (10,15): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // oNotNull[0] = null; // 1
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(10, 15),
            // (15,16): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // oNotNull?[0] = null; // 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(15, 16));
    }

    [Fact]
    public void Nullability_Setter_03()
    {
        // property write access, warning in receiver
        var src = """
#nullable enable

object? oNull = null;
M(oNull)[0] = null; // 1, 2

object? oNull2 = null;
M(oNull2)[0] = new object(); // 3

object M(object o) => throw null!;

static class E
{
    extension<T>(T t)
    {
        public T this[int i] { set => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,3): warning CS8604: Possible null reference argument for parameter 'o' in 'object M(object o)'.
            // M(oNull)[0] = null; // 1, 2
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "oNull").WithArguments("o", "object M(object o)").WithLocation(4, 3),
            // (4,15): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // M(oNull)[0] = null; // 1, 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 15),
            // (7,3): warning CS8604: Possible null reference argument for parameter 'o' in 'object M(object o)'.
            // M(oNull2)[0] = new object(); // 3
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "oNull2").WithArguments("o", "object M(object o)").WithLocation(7, 3));
    }

    [Fact]
    public void Nullability_Setter_04()
    {
        // generic extension parameter, check result of assignment
        var src = """
#nullable enable

object? oNull = null;
(oNull[0] = null).ToString(); // 1, 2

object? oNull2 = null;
(oNull2[0] = new object()).ToString();

object? oNotNull = new object();
(oNotNull[0] = null).ToString(); // 2

(oNotNull[0] = new object()).ToString();

static class E
{
    extension<T>(T t)
    {
        public T this[int i] { set => throw null!; }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (4,2): warning CS8602: Dereference of a possibly null reference.
            // (oNull[0] = null).ToString(); // 1, 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "oNull[0] = null").WithLocation(4, 2),
            // (10,2): warning CS8602: Dereference of a possibly null reference.
            // (oNotNull[0] = null).ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "oNotNull[0] = null").WithLocation(10, 2),
            // (10,16): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // (oNotNull[0] = null).ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(10, 16));
    }

    [Fact]
    public void Nullability_Setter_05()
    {
        // variance in receiver
        var src = """
#nullable enable

I<object?> iNull = null!;
iNull[0] = 42;
_ = iNull[0];

I<object> iNotNull = null!;
iNotNull[0] = 42;
_ = iNotNull[0];

static class E
{
    extension(I<object?> t)
    {
        public int this[int i] { get => throw null!; set => throw null!; }
    }
}

interface I<out T> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_Setter_06()
    {
        // variance in receiver
        var src = """
#nullable enable

I<object?> iNull = null!;
iNull[0] = 42;
_ = iNull[0];

I<object> iNotNull = null!;
iNotNull[0] = 42;
_ = iNotNull[0];

static class E
{
    extension(I<object> t)
    {
        public int this[int i] { get => throw null!; set => throw null!; }
    }
}

interface I<out T> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,1): warning CS8620: Argument of type 'I<object?>' cannot be used for parameter 't' of type 'I<object>' in 'E.extension(I<object>)' due to differences in the nullability of reference types.
            // iNull[0] = 42;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "iNull").WithArguments("I<object?>", "I<object>", "t", "E.extension(I<object>)").WithLocation(4, 1),
            // (5,5): warning CS8620: Argument of type 'I<object?>' cannot be used for parameter 't' of type 'I<object>' in 'E.extension(I<object>)' due to differences in the nullability of reference types.
            // _ = iNull[0];
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "iNull").WithArguments("I<object?>", "I<object>", "t", "E.extension(I<object>)").WithLocation(5, 5));
    }

    [Fact]
    public void Nullability_Setter_08()
    {
        // ref-returning property
        var src = """
#nullable enable
object? oNull3 = null;

object? oNull = null;
oNull[0] = oNull3;

object? oNull2 = null;
object? oNotNull = new object();
oNull2[0] = oNotNull;

oNotNull[0] = oNull3; // 1

oNotNull[0] = oNotNull;

static class E
{
    extension<T>(T t)
    {
        public ref T this[int i] { get => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (11,15): warning CS8601: Possible null reference assignment.
            // oNotNull[0] = oNull3; // 1
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNull3").WithLocation(11, 15));
    }

    [Fact]
    public void Nullability_Setter_09()
    {
        // indexer's state
        var src = """
#nullable enable

object o = new object();
o[0] = null;
o[0].ToString(); // 1

o[0] = new object();
o[0].ToString(); // 2

static class E
{
    extension(object o)
    {
        public object? this[int i] { get => throw null!; set => throw null!; }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,1): warning CS8602: Dereference of a possibly null reference.
            // o[0].ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o[0]").WithLocation(5, 1),
            // (8,1): warning CS8602: Dereference of a possibly null reference.
            // o[0].ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o[0]").WithLocation(8, 1));
    }

    [Fact]
    public void Nullability_Setter_10()
    {
        // assignment resulting in maybe-null converted value
        var src = """
#nullable enable

(new object()[0] = new D()).ToString(); // 1

static class E
{
    extension(object o)
    {
        public C? this[int i] { get => throw null!; set => throw null!; }
    }
}

class D
{
    public static implicit operator C?(D d) => throw null!;
}

class C { }
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (3,2): warning CS8602: Dereference of a possibly null reference.
            // (new object()[0] = new D()).ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "new object()[0] = new D()").WithLocation(3, 2));
    }

    [Fact]
    public void Nullability_Setter_11()
    {
        // assignment resulting in not-null converted value
        var src = """
#nullable enable

D? dNull = null;
(new object()[0] = dNull).ToString();

static class E
{
    extension(object o)
    {
        public C? this[int i] { get => throw null!; set => throw null!; }
    }
}

class D
{
    public static implicit operator C(D? d) => throw null!;
}

class C { }
""";
        CreateCompilation(src).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_Setter_13()
    {
        // compound assignment
        var src = """
#nullable enable

object? oNull = null;
oNull[0] += (object?)null;

object? oNull2 = null;
oNull2[0] += new object();

object? oNotNull = new object();
oNotNull[0] += (object?)null; // 1
oNotNull[0] += new object();

static class E
{
    extension<T>(T t)
    {
        public T this[int i] { get => throw null!; set => throw null!; }
        public static T operator +(T t1, T t2) => throw null!;
    }
}
""";
        var comp = CreateCompilation(src).VerifyEmitDiagnostics(
            // (10,1): warning CS8601: Possible null reference assignment.
            // oNotNull[0] += (object?)null; // 1
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNotNull[0] += (object?)null").WithLocation(10, 1));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var elementAccess = GetSyntaxes<ElementAccessExpressionSyntax>(tree, "oNull[0]").First();
        AssertEx.Equal("System.Object? E.extension<System.Object?>(System.Object?).this[System.Int32 i] { get; set; }",
            model.GetSymbolInfo(elementAccess).Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void Nullability_Setter_14()
    {
        // compound assignment
        var src = """
#nullable enable

object? oNull = null;
oNull[0] += (object?)null; // 1

object? oNull2 = null;
oNull2[0] += new object(); // 2

object? oNotNull = new object();
oNotNull[0] += (object?)null; // 3
oNotNull[0] += new object();

static class E
{
    extension<T>(T t)
    {
        [property: System.Diagnostics.CodeAnalysis.DisallowNull]
        public T this[int i] { get => throw null!; set => throw null!; }

        public static T operator +(T t1, T t2) => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyEmitDiagnostics(
            // (4,1): warning CS8601: Possible null reference assignment.
            // oNull[0] += (object?)null; // 1
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNull[0] += (object?)null").WithLocation(4, 1),
            // (7,1): warning CS8601: Possible null reference assignment.
            // oNull2[0] += new object(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNull2[0] += new object()").WithLocation(7, 1),
            // (10,1): warning CS8601: Possible null reference assignment.
            // oNotNull[0] += (object?)null; // 3
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNotNull[0] += (object?)null").WithLocation(10, 1));
    }

    [Fact]
    public void Nullability_Setter_15()
    {
        // + returns nullable reference type
        var src = """
#nullable enable

C? oNull = null;
oNull[0] += null;

C? oNull2 = null;
oNull2[0] += new C();

C? oNotNull = new C();
oNotNull[0] += null; // 1

var x = E.get_Item(oNotNull, 0);
x += null;
E.set_Item(oNotNull, 0, x);

oNotNull[0] += new C(); // 2

static class E
{
    extension<T>(T t)
    {
        public T this[int i] { get => throw null!; set => throw null!; }
    }
}

class C
{
    public static C? operator +(C? c1, C? c2) => throw null!;
}
""";
        var comp = CreateCompilation(src).VerifyEmitDiagnostics(
            // (10,1): warning CS8601: Possible null reference assignment.
            // oNotNull[0] += null; // 1
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNotNull[0] += null").WithLocation(10, 1),
            // (16,1): warning CS8601: Possible null reference assignment.
            // oNotNull[0] += new C(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNotNull[0] += new C()").WithLocation(16, 1));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var elementAccess = GetSyntaxes<ElementAccessExpressionSyntax>(tree, "oNull[0]").First();
        AssertEx.Equal("C? E.extension<C?>(C?).this[System.Int32 i] { get; set; }",
            model.GetSymbolInfo(elementAccess).Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void Nullability_Setter_16()
    {
        // + returns non-nullable reference type
        var src = """
#nullable enable

C? oNull = null;
oNull[0] += null;

C? oNull2 = null;
oNull2[0] += new C();

C? oNotNull = new C();
oNotNull[0] += null;
oNotNull[0] += new C();

static class E
{
    extension<T>(T t)
    {
        public T this[int i] { get => throw null!; set => throw null!; }
    }
}

class C
{
    public static C operator +(C? c1, C? c2) => throw null!;
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_Setter_17()
    {
        // compound assignment, + returns nullable reference type, ref-returning property
        var src = """
#nullable enable

C? oNull = null;
oNull[0] += null;

C? oNull2 = null;
oNull2[0] += new C();

C? oNotNull = new C();
oNotNull[0] += null;
oNotNull[0] += new C();

static class E
{
    extension<T>(T t)
    {
        public ref T this[int i] { get => throw null!; }
    }
}

class C
{
    public static C? operator +(C? c1, C? c2) => throw null!;
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (10,1): warning CS8601: Possible null reference assignment.
            // oNotNull[0] += null;
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNotNull[0] += null").WithLocation(10, 1),
            // (11,1): warning CS8601: Possible null reference assignment.
            // oNotNull[0] += new C();
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNotNull[0] += new C()").WithLocation(11, 1));
    }

    [Fact]
    public void Nullability_Setter_18()
    {
        // compound assignment, + returns non-nullable reference type, ref-returning property
        var src = """
#nullable enable

C? oNull = null;
oNull[0] += null;

C? oNull2 = null;
oNull2[0] += new C();

C? oNotNull = new C();
oNotNull[0] += null;
oNotNull[0] += new C();

static class E
{
    extension<T>(T t)
    {
        public ref T this[int i] { get => throw null!; }
    }
}

class C
{
    public static C operator +(C? c1, C? c2) => throw null!;
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_Setter_19()
    {
        // compound assignment, + returns nullable reference type, check result value
        var src = """
#nullable enable

C? oNull = null;
(oNull[0] += null).ToString(); // 1

C? oNull2 = null;
(oNull2[0] += new C()).ToString(); // 2

C? oNotNull = new C();
(oNotNull[0] += null).ToString(); // 3, 4

(oNotNull[0] += new C()).ToString(); // 5, 6

static class E
{
    extension<T>(T t)
    {
        public T this[int i] { get => throw null!; set => throw null!; }
    }
}

class C
{
    public static C? operator +(C? c1, C? c2) => throw null!;
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (4,2): warning CS8602: Dereference of a possibly null reference.
            // (oNull[0] += null).ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "oNull[0] += null").WithLocation(4, 2),
            // (7,2): warning CS8602: Dereference of a possibly null reference.
            // (oNull2[0] += new C()).ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "oNull2[0] += new C()").WithLocation(7, 2),
            // (10,2): warning CS8601: Possible null reference assignment.
            // (oNotNull[0] += null).ToString(); // 3, 4
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNotNull[0] += null").WithLocation(10, 2),
            // (10,2): warning CS8602: Dereference of a possibly null reference.
            // (oNotNull[0] += null).ToString(); // 3, 4
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "oNotNull[0] += null").WithLocation(10, 2),
            // (12,2): warning CS8601: Possible null reference assignment.
            // (oNotNull[0] += new C()).ToString(); // 5, 6
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNotNull[0] += new C()").WithLocation(12, 2),
            // (12,2): warning CS8602: Dereference of a possibly null reference.
            // (oNotNull[0] += new C()).ToString(); // 5, 6
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "oNotNull[0] += new C()").WithLocation(12, 2));
    }

    [Fact]
    public void Nullability_Setter_20()
    {
        // compound assignment, + returns non-nullable reference type, check result value
        var src = """
#nullable enable

C? oNull = null;
(oNull[0] += null).ToString();

C? oNull2 = null;
(oNull2[0] += new C()).ToString();

C? oNotNull = new C();
(oNotNull[0] += null).ToString();

(oNotNull[0] += new C()).ToString();

static class E
{
    extension<T>(T t)
    {
        public T this[int i] { get => throw null!; set => throw null!; }
    }
}

class C
{
    public static C operator +(C? c1, C? c2) => throw null!;
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_Setter_21()
    {
        // compound assignment, warning in receiver
        var src = """
#nullable enable

M(null)[0] += 0;

object M(object o) => throw null!;

static class E
{
    extension(object o)
    {
        public int this[int i] { get => throw null!; set => throw null!; }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (3,3): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // M(null)[0] += 0;
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 3));
    }

    [Fact]
    public void Nullability_Setter_22()
    {
        // compound assignment, warning in RHS
        var src = """
#nullable enable

new object()[0] += M(null);

int M(object o) => throw null!;

static class E
{
    extension(object o)
    {
        public int this[int i] { get => throw null!; set => throw null!; }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (3,22): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // new object()[0] += M(null);
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 22));
    }

    [Fact]
    public void Nullability_Setter_23()
    {
        // compound assignment, warning in indexer argument
        var src = """
#nullable enable

new object()[M(null)] += 0;

int M(object o) => throw null!;

static class E
{
    extension(object o)
    {
        public int this[int i] { get => throw null!; set => throw null!; }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (3,16): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // new object()[M(null)] += 0;
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 16));
    }

    [Fact]
    public void Nullability_Setter_24()
    {
        // compound assignment resulting in maybe-null converted value
        var src = """
#nullable enable

(new object()[0] += new D()).ToString(); // 1

static class E
{
    extension(object o)
    {
        public C? this[int i] { get => throw null!; set => throw null!; }
    }
}

class D { }

class C
{
    public static C? operator+(C? c1, D d2) => throw null!;
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (3,2): warning CS8602: Dereference of a possibly null reference.
            // (new object()[0] += new D()).ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "new object()[0] += new D()").WithLocation(3, 2));
    }

    [Fact]
    public void Nullability_Setter_25()
    {
        // compound assignment resulting in not-null converted value
        var src = """
#nullable enable

(new object()[0] += new D()).ToString();

static class E
{
    extension(object o)
    {
        public C? this[int i] { get => throw null!; set => throw null!; }
    }
}

class D { }

class C
{
    public static C operator+(C? c1, D d2) => throw null!;
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_Setter_26()
    {
        // compound assignment, + returns nullable reference type, ref-returning property, with suppression
        var src = """
#nullable enable

C? oNotNull = new C();
oNotNull[0] += null!;
oNotNull[0] += new C()!;

static class E
{
    extension<T>(T t)
    {
        public ref T this[int i] { get => throw null!; }
    }
}

class C
{
    public static C? operator +(C? c1, C? c2) => throw null!;
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (4,1): warning CS8601: Possible null reference assignment.
            // oNotNull[0] += null!;
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNotNull[0] += null!").WithLocation(4, 1),
            // (5,1): warning CS8601: Possible null reference assignment.
            // oNotNull[0] += new C()!;
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNotNull[0] += new C()!").WithLocation(5, 1));

        src = """
#nullable enable

C? oNotNull = new C();
oNotNull[0] += null!;
oNotNull[0] += new C()!;

class C
{
    public static C? operator +(C? c1, C? c2) => throw null!;
    public ref C this[int i] { get => throw null!; }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (4,1): warning CS8601: Possible null reference assignment.
            // oNotNull[0] += null!;
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNotNull[0] += null!").WithLocation(4, 1),
            // (5,1): warning CS8601: Possible null reference assignment.
            // oNotNull[0] += new C()!;
            Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "oNotNull[0] += new C()!").WithLocation(5, 1));
    }

    [Fact]
    public void Nullability_Params_01()
    {
        // in indexing, null disallowed
        var src = """
#nullable enable

var o = new object();
_ = o[42, null, new object(), null]; // 1, 2

o[42, null, new object(), null] = 0; // 3, 4

o?[42, null, new object(), null] = 0; // 5, 6

public static class E
{
    extension(object o)
    {
        public int this[int a, params object[] b] { get => throw null!; set => throw null!; }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (4,11): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // _ = o[42, null, new object(), null]; // 1, 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 11),
            // (4,31): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // _ = o[42, null, new object(), null]; // 1, 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 31),
            // (6,7): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // o[42, null, new object(), null] = 0; // 3, 4
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(6, 7),
            // (6,27): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // o[42, null, new object(), null] = 0; // 3, 4
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(6, 27),
            // (8,8): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // o?[42, null, new object(), null] = 0; // 5, 6
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(8, 8),
            // (8,28): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // o?[42, null, new object(), null] = 0; // 5, 6
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(8, 28));
    }

    [Fact]
    public void Nullability_Params_02()
    {
        // in indexing, null allowed
        var src = """
#nullable enable

var o = new object();
_ = o[42, null, new object(), null];

o[42, null, new object(), null] = 0;

o?[42, null, new object(), null] = 0;

public static class E
{
    extension(object o)
    {
        public int this[int a, params object?[] b] { get => throw null!; set => throw null!; }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_Params_03()
    {
        // in indexing, generic
        var src = """
#nullable enable

var o = new object();
_ = o[42, null, new object(), null];

o[42, null, new object(), null] = 0;

o?[42, null, new object(), null] = 0;

public static class E
{
    extension<T>(object o)
    {
        public int this[int a, params T[] b] { get => throw null!; set => throw null!; }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_Params_04()
    {
        // in indexing, params generic
        var src = """
#nullable enable

var o = new object();
o[42, new object()] = null; // 1
o[42, new object()].ToString();

public static class E
{
    extension<T>(object o)
    {
        public T this[int a, params T[] b] { get => throw null!; set => throw null!; }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (4,23): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // o[42, new object()] = null; // 1
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 23));
    }

    [Fact]
    public void Nullability_Params_05()
    {
        // in indexing, params and return generic
        var src = """
#nullable enable

var o = new object();
o[42, new object(), null] = null;
o[42, new object(), null].ToString(); // 1

public static class E
{
    extension<T>(object o)
    {
        public T this[int a, params T[] b] { get => throw null!; set => throw null!; }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,1): warning CS8602: Dereference of a possibly null reference.
            // o[42, new object(), null].ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o[42, new object(), null]").WithLocation(5, 1));
    }

    [Fact]
    public void Nullability_Params_06()
    {
        // in object initializer, null disallowed
        var src = """
#nullable enable

_ = new object() { [42, null, new object(), null] = 0 }; // 1, 2

public static class E
{
    extension(object o)
    {
        public int this[int a, params object[] b] { get => throw null!; set => throw null!; }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (3,25): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // _ = new object() { [42, null, new object(), null] = 0 }; // 1, 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 25),
            // (3,45): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // _ = new object() { [42, null, new object(), null] = 0 }; // 1, 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 45));
    }

    [Fact]
    public void Nullability_Params_07()
    {
        // in object initializer, null allowed
        var src = """
#nullable enable

_ = new object() { [42, null, new object(), null] = 0 };

public static class E
{
    extension(object o)
    {
        public int this[int a, params object?[] b] { get => throw null!; set => throw null!; }
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_Params_08()
    {
        // in compound assignment, null disallowed
        var src = """
#nullable enable

new object()[0, null, new object(), null] += 0; // 1, 2

static class E
{
    extension(object o1)
    {
        public int this[int i, params object[] o2] { get => throw null!; set => throw null!; }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (3,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // new object()[0, null, new object(), null] += 0; // 1, 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 17),
            // (3,37): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // new object()[0, null, new object(), null] += 0; // 1, 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(3, 37));
    }

    [Fact]
    public void Nullability_Params_09()
    {
        // in compound assignment, null allowed
        var src = """
#nullable enable

new object()[0, null, new object(), null] += 0;

static class E
{
    extension(object o1)
    {
        public int this[int i, params object?[] o2] { get => throw null!; set => throw null!; }
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_Params_10()
    {
        // disambiguation invocation
        var src = """
#nullable enable

var o = new object();
E.get_Item(o, 42, null, new object(), null); // 1, 2

E.set_Item(o, 42, null, new object(), null, 0); // 3

public static class E
{
    extension(object o)
    {
        public int this[int a, params object[] b] { get => throw null!; set => throw null!; }
    }
}
""";

        var comp = CreateCompilation(src).VerifyEmitDiagnostics(
            // (4,19): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // E.get_Item(o, 42, null, new object(), null); // 1, 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 19),
            // (4,39): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // E.get_Item(o, 42, null, new object(), null); // 1, 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 39),
            // (6,3): error CS1501: No overload for method 'set_Item' takes 6 arguments
            // E.set_Item(o, 42, null, new object(), null, 0); // 3
            Diagnostic(ErrorCode.ERR_BadArgCount, "set_Item").WithArguments("set_Item", "6").WithLocation(6, 3));

        var bParameter = comp.GlobalNamespace.GetTypeMember("E").GetMethod("set_Item").Parameters[2];
        Assert.Equal("b", bParameter.Name);
        Assert.True(bParameter.IsParams); // PROTOTYPE params, should we disallow that or not emit it?
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81953")]
    public void DynamicIndexerAccess_11()
    {
        var src = """
#nullable enable

class C
{
    public dynamic D = null!;

    static void M()
    {
        var c = new C
        {
            D =
            {
                [0] = M3(null),
                [M2(null)] = 0
            }
        };
    }

    static int M2(object o) => 0;
    static int M3(object o) => 0;
}
""";
        CreateCompilation(src, targetFramework: TargetFramework.Net100).VerifyEmitDiagnostics(
            // (14,21): warning CS8625: Cannot convert null literal to non-nullable reference type.
            //                 [M2(null)] = 0
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(14, 21));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81953")]
    public void DynamicIndexerAccess_12()
    {
        var src = """
#nullable enable

class C
{
    public dynamic D = null!;

    static void M()
    {
        C c = new()
        {
            D =
            {
                [0] = M3(null),
                [M2(null)] = 0
            }
        };
    }

    static int M2(object o) => 0;
    static int M3(object o) => 0;
}
""";
        CreateCompilation(src, targetFramework: TargetFramework.Net100).VerifyEmitDiagnostics(
            // (14,21): warning CS8625: Cannot convert null literal to non-nullable reference type.
            //                 [M2(null)] = 0
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(14, 21));
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_ReceiverParameter_WithOtherParameters(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, C s1, string s2)
    {
        System.Console.Write(s1.value);
        System.Console.Write(s2);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class E
{
    extension(C s1)
    {
        public int this[string s2, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("s1", "s2")] InterpolationHandler h] { get => 0; }
    }
}

public class C(string s)
{
    public string value = s;
}
""";

        var exeSource = """
_ = new C("1")["2", $""];
_ = E.get_Item(new C("3"), "4", $"");
""";

        var expectedOutput = ExecutionConditionUtil.IsCoreClr ? "1234" : null;
        CompileAndVerify([exeSource, src], targetFramework: TargetFramework.Net100, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        var comp1 = CreateCompilation(src, targetFramework: TargetFramework.Net100);

        var verifier = CompileAndVerify(exeSource, references: [AsReference(comp1, useMetadataRef)], targetFramework: TargetFramework.Net100, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var comp = (CSharpCompilation)verifier.Compilation;
        var tree = comp.SyntaxTrees[0];
        var compRoot = tree.GetCompilationUnitRoot();
        var model = comp.GetSemanticModel(tree);
        var opRoot = model.GetOperation(compRoot);
        VerifyOperationTree(comp, opRoot, expectedOperationTree: """
IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: '_ = new C(" ...  "4", $"");')
  BlockBody:
    IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: '_ = new C(" ...  "4", $"");')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = new C(" ... ["2", $""];')
        Expression:
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '_ = new C("1")["2", $""]')
            Left:
              IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
            Right:
              IPropertyReferenceOperation: System.Int32 E.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.this[System.String s2, InterpolationHandler h] { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'new C("1")["2", $""]')
                Instance Receiver:
                  IObjectCreationOperation (Constructor: C..ctor(System.String s)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C("1")')
                    Arguments(1):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null) (Syntax: '"1"')
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "1") (Syntax: '"1"')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Initializer:
                      null
                Arguments(2):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s2) (OperationKind.Argument, Type: null) (Syntax: '"2"')
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "2") (Syntax: '"2"')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: h) (OperationKind.Argument, Type: null) (Syntax: '$""')
                      IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: InterpolationHandler, IsImplicit) (Syntax: '$""')
                        Creation:
                          IObjectCreationOperation (Constructor: InterpolationHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, C s1, System.String s2)) (OperationKind.ObjectCreation, Type: InterpolationHandler, IsImplicit) (Syntax: '$""')
                            Arguments(4):
                                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""')
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$""')
                                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""')
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$""')
                                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s1) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new C("1")')
                                  IInterpolatedStringHandlerArgumentPlaceholderOperation (CallsiteReceiver) (OperationKind.InterpolatedStringHandlerArgumentPlaceholder, Type: null, IsImplicit) (Syntax: 'new C("1")')
                                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"2"')
                                  IInterpolatedStringHandlerArgumentPlaceholderOperation (ArgumentIndex: 0) (OperationKind.InterpolatedStringHandlerArgumentPlaceholder, Type: null, IsImplicit) (Syntax: '"2"')
                                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Initializer:
                              null
                        Content:
                          IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, Constant: "") (Syntax: '$""')
                            Parts(0)
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = E.get_I ...  "4", $"");')
        Expression:
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '_ = E.get_I ... , "4", $"")')
            Left:
              IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
            Right:
              IInvocationOperation (System.Int32 E.get_Item(C s1, System.String s2, InterpolationHandler h)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'E.get_Item( ... , "4", $"")')
                Instance Receiver:
                  null
                Arguments(3):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s1) (OperationKind.Argument, Type: null) (Syntax: 'new C("3")')
                      IObjectCreationOperation (Constructor: C..ctor(System.String s)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C("3")')
                        Arguments(1):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null) (Syntax: '"3"')
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "3") (Syntax: '"3"')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Initializer:
                          null
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s2) (OperationKind.Argument, Type: null) (Syntax: '"4"')
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "4") (Syntax: '"4"')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: h) (OperationKind.Argument, Type: null) (Syntax: '$""')
                      IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: InterpolationHandler, IsImplicit) (Syntax: '$""')
                        Creation:
                          IObjectCreationOperation (Constructor: InterpolationHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, C s1, System.String s2)) (OperationKind.ObjectCreation, Type: InterpolationHandler, IsImplicit) (Syntax: '$""')
                            Arguments(4):
                                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""')
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$""')
                                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""')
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$""')
                                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s1) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new C("3")')
                                  IInterpolatedStringHandlerArgumentPlaceholderOperation (ArgumentIndex: 0) (OperationKind.InterpolatedStringHandlerArgumentPlaceholder, Type: null, IsImplicit) (Syntax: 'new C("3")')
                                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"4"')
                                  IInterpolatedStringHandlerArgumentPlaceholderOperation (ArgumentIndex: 1) (OperationKind.InterpolatedStringHandlerArgumentPlaceholder, Type: null, IsImplicit) (Syntax: '"4"')
                                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Initializer:
                              null
                        Content:
                          IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, Constant: "") (Syntax: '$""')
                            Parts(0)
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  ExpressionBody:
    null
""");
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_ReceiverParameter_WithConversion(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, object o)
    {
        System.Console.Write(o);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class E
{
    extension(object o)
    {
        public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("o")] InterpolationHandler h] { get => 0; }
    }
}

public class C(string s)
{
    public string value = s;
    public override string ToString() => value;
}
""";

        // The receiver is a C, so there's a BoundConversion to object as the receiver of the extension indexer
        var exeSource = """
_ = new C("1")[$""];
_ = E.get_Item(new C("2"), $"");
""";

        var expectedOutput = ExecutionConditionUtil.IsCoreClr ? "12" : null;
        var verifier = CompileAndVerify([exeSource, src], targetFramework: TargetFramework.Net100, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        verifier.VerifyIL("<top-level-statements-entry-point>", """
{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (object V_0)
  IL_0000:  ldstr      "1"
  IL_0005:  newobj     "C..ctor(string)"
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  ldc.i4.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldloc.0
  IL_000f:  newobj     "InterpolationHandler..ctor(int, int, object)"
  IL_0014:  call       "int E.get_Item(object, InterpolationHandler)"
  IL_0019:  pop
  IL_001a:  ldstr      "2"
  IL_001f:  newobj     "C..ctor(string)"
  IL_0024:  stloc.0
  IL_0025:  ldloc.0
  IL_0026:  ldc.i4.0
  IL_0027:  ldc.i4.0
  IL_0028:  ldloc.0
  IL_0029:  newobj     "InterpolationHandler..ctor(int, int, object)"
  IL_002e:  call       "int E.get_Item(object, InterpolationHandler)"
  IL_0033:  pop
  IL_0034:  ret
}
""");

        var comp1 = CreateCompilation(src, targetFramework: TargetFramework.Net100);

        CompileAndVerify(exeSource, references: [useMetadataRef ? comp1.ToMetadataReference() : comp1.EmitToImageReference()], targetFramework: TargetFramework.Net100, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_ReceiverParameter_WithConversion_ExtensionParameterNarrowerThanConstructor(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, object o)
    {
        System.Console.Write(o);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class E
{
    extension(C o)
    {
        public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("o")] InterpolationHandler h] { get => 0; }
    }
}

public class C(string s)
{
    public string value = s;
    public override string ToString() => value;
}
""";

        var exeSource = """
_ = new C("1")[$""];
_ = E.get_Item(new ("2"), $"");
""";

        var expectedOutput = ExecutionConditionUtil.IsCoreClr ? "12" : null;
        CompileAndVerify([exeSource, src], targetFramework: TargetFramework.Net100, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        var comp1 = CreateCompilation(src, targetFramework: TargetFramework.Net100);

        CompileAndVerify(exeSource, references: [useMetadataRef ? comp1.ToMetadataReference() : comp1.EmitToImageReference()], targetFramework: TargetFramework.Net100, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_ReceiverParameter_WithConversion_ExtensionParameterWiderThanConstructor(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, C s)
    {
        System.Console.WriteLine(s);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class E
{
    extension(object o)
    {
        public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("o")] InterpolationHandler h] { get => 0; }
    }
}

public class C(string s)
{
    public string value = s;
    public override string ToString() => value;
}
""";

        var exeSource = """
_ = new C("1")[$""];
_ = E.get_Item(new C("2"), $"");
""";

        var expectedDiagnostics = new[] {
            // (1,5): error CS1503: Argument 3: cannot convert from 'object' to 'C'
            // _ = new C("1")[$""];
            Diagnostic(ErrorCode.ERR_BadArgType, @"new C(""1"")").WithArguments("3", "object", "C").WithLocation(1, 5),
            // (2,16): error CS1503: Argument 3: cannot convert from 'object' to 'C'
            // _ = E.get_Item(new C("2"), $"");
            Diagnostic(ErrorCode.ERR_BadArgType, @"new C(""2"")").WithArguments("3", "object", "C").WithLocation(2, 16)
        };

        CreateCompilation([exeSource, src], targetFramework: TargetFramework.Net100).VerifyDiagnostics(expectedDiagnostics);

        var comp1 = CreateCompilation(src, targetFramework: TargetFramework.Net100);

        CreateCompilation(exeSource, references: [useMetadataRef ? comp1.ToMetadataReference() : comp1.EmitToImageReference()], targetFramework: TargetFramework.Net100).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_ReceiverParameter_ByRef(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, ref int i)
    {
        System.Console.Write(i);
        i = 2;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class E
{
    extension(ref int i)
    {
        public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("i")] InterpolationHandler h]
        {
            get
            {
                System.Console.Write(i);
                i = 3;
                return 0;
            }
        }
    }
}
""";

        var exeSource = """
class Program
{
    static void Main()
    {
        int i = 1;
        Test1(ref i);
        System.Console.Write(i);
        i = 4;
        Test2(ref i);
        System.Console.Write(i);
    }

    static void Test1(ref int i)
    {
        _ = i[$""];
    }

    static void Test2(ref int i)
    {
        E.get_Item(ref i, $"");
    }
}
""";

        var expectedOutput = "123423";
        var verifier = CompileAndVerify([exeSource, src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], expectedOutput: expectedOutput).VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1", """
{
  // Code size       18 (0x12)
  .maxstack  4
  .locals init (int& V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  ldc.i4.0
  IL_0005:  ldloc.0
  IL_0006:  newobj     "InterpolationHandler..ctor(int, int, ref int)"
  IL_000b:  call       "int E.get_Item(ref int, InterpolationHandler)"
  IL_0010:  pop
  IL_0011:  ret
}
""");

        verifier.VerifyIL("Program.Test2", """
{
  // Code size       18 (0x12)
  .maxstack  4
  .locals init (int& V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  ldc.i4.0
  IL_0005:  ldloc.0
  IL_0006:  newobj     "InterpolationHandler..ctor(int, int, ref int)"
  IL_000b:  call       "int E.get_Item(ref int, InterpolationHandler)"
  IL_0010:  pop
  IL_0011:  ret
}
""");

        var comp1 = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);

        CompileAndVerify(exeSource, references: [AsReference(comp1, useMetadataRef)], expectedOutput: expectedOutput)
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_ReceiverParameter_ByRef_WithConstantReceiver(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, ref int i)
    {
        System.Console.Write(i);
        System.Runtime.CompilerServices.Unsafe.AsRef(in i)++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class E
{
    extension(ref int i)
    {
        public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("i")] InterpolationHandler h]
        {
            get
            {
                System.Console.Write(i);
                return 0;
            }
        }
    }
}
""";

        var exeSource = """
_ = 1[$""];
E.get_Item(ref 3, $"");
""";

        var expectedDiagnostic = new DiagnosticDescription[] {
            // (1,5): error CS1510: A ref or out value must be an assignable variable
            // _ = 1[$""];
            Diagnostic(ErrorCode.ERR_RefLvalueExpected, "1").WithLocation(1, 5),
            // (2,16): error CS1510: A ref or out value must be an assignable variable
            // E.get_Item(ref 3, $"");
            Diagnostic(ErrorCode.ERR_RefLvalueExpected, "3").WithLocation(2, 16)
        };

        CreateCompilation([exeSource, src], targetFramework: TargetFramework.Net100).VerifyDiagnostics(expectedDiagnostic);

        var comp1 = CreateCompilation(src, targetFramework: TargetFramework.Net100);

        CreateCompilation(exeSource, references: [AsReference(comp1, useMetadataRef)], targetFramework: TargetFramework.Net100).VerifyDiagnostics(expectedDiagnostic);
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_ReceiverParameter_Generic_ByRef(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler<TR>
{
    public InterpolationHandler(int literalLength, int formattedCount, ref TR i)
    {
        System.Console.Write(i);
        i = (TR)(object)2;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class E
{
    extension<T>(ref T i) where T : struct
    {
        public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("i")] InterpolationHandler<T> h]
        {
            get
            {
                System.Console.Write(i);
                i = (T)(object)3;
                return 0;
            }
        }
    }
}
""";

        var exeSource = """
class Program
{
    static void Main()
    {
        int i = 1;
        Test1(ref i);
        System.Console.Write(i);
        i = 4;
        Test2(ref i);
        System.Console.Write(i);
    }

    static void Test1<T>(ref T i) where T : struct
    {
        _ = i[$""];
    }

    static void Test2<T>(ref T i) where T : struct
    {
        E.get_Item(ref i, $"");
    }
}
""";

        var expectedOutput = "123423";
        var verifier = CompileAndVerify([exeSource, src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], expectedOutput: expectedOutput).VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>", """
{
  // Code size       18 (0x12)
  .maxstack  4
  .locals init (T& V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  ldc.i4.0
  IL_0005:  ldloc.0
  IL_0006:  newobj     "InterpolationHandler<T>..ctor(int, int, ref T)"
  IL_000b:  call       "int E.get_Item<T>(ref T, InterpolationHandler<T>)"
  IL_0010:  pop
  IL_0011:  ret
}
""");

        verifier.VerifyIL("Program.Test2<T>", """
{
  // Code size       18 (0x12)
  .maxstack  4
  .locals init (T& V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  ldc.i4.0
  IL_0005:  ldloc.0
  IL_0006:  newobj     "InterpolationHandler<T>..ctor(int, int, ref T)"
  IL_000b:  call       "int E.get_Item<T>(ref T, InterpolationHandler<T>)"
  IL_0010:  pop
  IL_0011:  ret
}
""");

        var comp1 = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);

        CompileAndVerify(exeSource, references: [AsReference(comp1, useMetadataRef)], expectedOutput: expectedOutput)
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_ReceiverParameter_ByIn_WithConstantReceiver(bool useMetadataRef, [CombinatorialValues("ref readonly", "in")] string refkind)
    {
        var src = $$$"""
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount, {{{refkind}}} int i)
    {
        System.Console.Write(i);
        System.Runtime.CompilerServices.Unsafe.AsRef(in i)++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class E
{
    extension({{{refkind}}} int i)
    {
        public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("i")] InterpolationHandler h]
        {
            get
            {
                System.Console.Write(i);
                return 0;
            }
        }
    }
}
""";

        var exeSource = """
#pragma warning disable CS9193 // Argument 0 should be a variable because it is passed to a 'ref readonly' parameter

class Program
{
    static void Main()
    {
        Test1();
        Test2();
    }

    static void Test1()
    {
        _ = 1[$""];
    }

    static void Test2()
    {
        E.get_Item(3, $"");
    }
}
""";

        var expectedOutput = ExecutionConditionUtil.IsCoreClr ? "1234" : null;
        var verifier = CompileAndVerify([exeSource, src], targetFramework: TargetFramework.Net100, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1", $$$"""
{
  // Code size       20 (0x14)
  .maxstack  4
  .locals init (int V_0)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.0
  IL_0005:  ldc.i4.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  newobj     "InterpolationHandler..ctor(int, int, {{{refkind}}} int)"
  IL_000d:  call       "int E.get_Item({{{refkind}}} int, InterpolationHandler)"
  IL_0012:  pop
  IL_0013:  ret
}
""");

        verifier.VerifyIL("Program.Test2", $$$"""
{
  // Code size       20 (0x14)
  .maxstack  4
  .locals init (int V_0)
  IL_0000:  ldc.i4.3
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.0
  IL_0005:  ldc.i4.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  newobj     "InterpolationHandler..ctor(int, int, {{{refkind}}} int)"
  IL_000d:  call       "int E.get_Item({{{refkind}}} int, InterpolationHandler)"
  IL_0012:  pop
  IL_0013:  ret
}
""");

        var comp1 = CreateCompilation(src, targetFramework: TargetFramework.Net100);

        CompileAndVerify(exeSource, references: [AsReference(comp1, useMetadataRef)], targetFramework: TargetFramework.Net100, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify);
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_ReceiverParameter_ByIn_WithLocalReceiver(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount, in int i)
    {
        System.Console.Write(i);
        System.Runtime.CompilerServices.Unsafe.AsRef(in i)++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class E
{
    extension(in int i)
    {
        public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("i")] InterpolationHandler h]
        {
            get
            {
                System.Console.Write(i);
                System.Runtime.CompilerServices.Unsafe.AsRef(in i)++;
                return 0;
            }
        }
    }
}
""";

        var exeSource = """
int i = 1;
_ = i[$""];
System.Console.Write(i);
E.get_Item(i, $"");
System.Console.Write(i);
""";

        var expectedOutput = ExecutionConditionUtil.IsCoreClr ? "123345" : null;
        var verifier = CompileAndVerify([exeSource, src], targetFramework: TargetFramework.Net100, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
{
  // Code size       51 (0x33)
  .maxstack  4
  .locals init (int V_0, //i
                int& V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  stloc.1
  IL_0005:  ldloc.1
  IL_0006:  ldc.i4.0
  IL_0007:  ldc.i4.0
  IL_0008:  ldloc.1
  IL_0009:  newobj     "InterpolationHandler..ctor(int, int, in int)"
  IL_000e:  call       "int E.get_Item(in int, InterpolationHandler)"
  IL_0013:  pop
  IL_0014:  ldloc.0
  IL_0015:  call       "void System.Console.Write(int)"
  IL_001a:  ldloca.s   V_0
  IL_001c:  stloc.1
  IL_001d:  ldloc.1
  IL_001e:  ldc.i4.0
  IL_001f:  ldc.i4.0
  IL_0020:  ldloc.1
  IL_0021:  newobj     "InterpolationHandler..ctor(int, int, in int)"
  IL_0026:  call       "int E.get_Item(in int, InterpolationHandler)"
  IL_002b:  pop
  IL_002c:  ldloc.0
  IL_002d:  call       "void System.Console.Write(int)"
  IL_0032:  ret
}
""");

        var comp1 = CreateCompilation(src, targetFramework: TargetFramework.Net100);

        CompileAndVerify(exeSource, references: [AsReference(comp1, useMetadataRef)], targetFramework: TargetFramework.Net100, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_ReceiverParameter_ByRefMismatch_01(bool useMetadataRef, [CombinatorialValues("ref readonly", "in", "")] string refkind)
    {
        var src = $$"""
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount, ref int i)
    {
        System.Console.Write(i);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class E
{
    extension({{refkind}} int i)
    {
        public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("i")] InterpolationHandler h] { get => 0; }
    }
}
""";

        var exeSource = $$"""
int i = 1;
_ = i[$""];
E.get_Item({{(refkind == "" ? "" : "in ")}}i, $"");
""";

        var expectedDiagnostic = new[] {
            // (2,5): error CS1620: Argument 3 must be passed with the 'ref' keyword
            // _ = i[$""];
            Diagnostic(ErrorCode.ERR_BadArgRef, "i").WithArguments("3", "ref").WithLocation(2, 5),
            // (3,12): error CS1620: Argument 3 must be passed with the 'ref' keyword
            // E.get_Item(i, $"");
            Diagnostic(ErrorCode.ERR_BadArgRef, "i").WithArguments("3", "ref")
        };

        CreateCompilation([exeSource, src], targetFramework: TargetFramework.Net100).VerifyDiagnostics(expectedDiagnostic);

        var comp1 = CreateCompilation(src, targetFramework: TargetFramework.Net100);

        CreateCompilation(exeSource, references: [AsReference(comp1, useMetadataRef)], targetFramework: TargetFramework.Net100).VerifyDiagnostics(expectedDiagnostic);
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_ReceiverParameter_ByRefMismatch_02(bool useMetadataRef, [CombinatorialValues("ref readonly", "in")] string refkind)
    {
        var src = $$"""
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount, {{refkind}} int i)
    {
        System.Console.Write(i);
        System.Runtime.CompilerServices.Unsafe.AsRef(in i)++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class E
{
    extension(ref int i)
    {
        public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("i")] InterpolationHandler h]
        {
            get
            {
                System.Console.Write(i);
                i++;
                return 0;
            }
        }
    }
}
""";

        var exeSource = """
int i = 1;
_ = i[$""];
System.Console.Write(i);
E.get_Item(ref i, $"");
System.Console.Write(i);
""";

        var expectedOutput = ExecutionConditionUtil.IsCoreClr ? "123345" : null;
        CompileAndVerify([exeSource, src], targetFramework: TargetFramework.Net100, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        var comp1 = CreateCompilation(src, targetFramework: TargetFramework.Net100);

        CompileAndVerify(exeSource, references: [AsReference(comp1, useMetadataRef)], targetFramework: TargetFramework.Net100, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_ReceiverParameter_ByRefMismatch_03(bool useMetadataRef)
    {
        var src = $$"""
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount, int i)
    {
        System.Console.WriteLine(i);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class E
{
    extension(ref int i)
    {
        public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("i")] InterpolationHandler h] { get => 0; }
    }
}
""";

        var exeSource = """
int i = 1;
_ = i[$""];
E.get_Item(ref i, $"");
""";

        var expectedDiagnostics = new[] {
            // (2,5): error CS1615: Argument 3 may not be passed with the 'ref' keyword
            // _ = i[$""];
            Diagnostic(ErrorCode.ERR_BadArgExtraRef, "i").WithArguments("3", "ref").WithLocation(2, 5),
            // (3,16): error CS1615: Argument 3 may not be passed with the 'ref' keyword
            // E.get_Item(ref i, $"");
            Diagnostic(ErrorCode.ERR_BadArgExtraRef, "i").WithArguments("3", "ref").WithLocation(3, 16)
         };

        CreateCompilation([exeSource, src], targetFramework: TargetFramework.Net100).VerifyDiagnostics(expectedDiagnostics);

        var comp1 = CreateCompilation(src, targetFramework: TargetFramework.Net100);

        CreateCompilation(exeSource, references: [AsReference(comp1, useMetadataRef)], targetFramework: TargetFramework.Net100).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_StructReceiverParameter_ByValue(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount, MyStruct s)
    {
        System.Console.Write(s.i);
        s.i++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public struct MyStruct
{
    public int i;
}

public static class E
{
    extension(MyStruct s)
    {
        public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("s")] InterpolationHandler h]
        {
            get
            {
                System.Console.Write(s.i);
                s.i++;
                return 0;
            }
        }
    }
}
""";

        var exeSource = """
_ = new MyStruct()[$""];
E.get_Item(new MyStruct(), $"");
""";

        var expectedOutput = ExecutionConditionUtil.IsCoreClr ? "0000" : null;
        var verifier = CompileAndVerify([exeSource, src], targetFramework: TargetFramework.Net100, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        verifier.VerifyIL("<top-level-statements-entry-point>", """
{
  // Code size       47 (0x2f)
  .maxstack  4
  .locals init (MyStruct V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "MyStruct"
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  newobj     "InterpolationHandler..ctor(int, int, MyStruct)"
  IL_0011:  call       "int E.get_Item(MyStruct, InterpolationHandler)"
  IL_0016:  pop
  IL_0017:  ldloca.s   V_0
  IL_0019:  initobj    "MyStruct"
  IL_001f:  ldloc.0
  IL_0020:  ldc.i4.0
  IL_0021:  ldc.i4.0
  IL_0022:  ldloc.0
  IL_0023:  newobj     "InterpolationHandler..ctor(int, int, MyStruct)"
  IL_0028:  call       "int E.get_Item(MyStruct, InterpolationHandler)"
  IL_002d:  pop
  IL_002e:  ret
}
""");

        var comp1 = CreateCompilation(src, targetFramework: TargetFramework.Net100);

        CompileAndVerify(exeSource, references: [AsReference(comp1, useMetadataRef)], targetFramework: TargetFramework.Net100, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_StructReceiverParameter_ByValueThroughField(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount, MyStruct s)
    {
        System.Console.Write(s.i);
        E.field.i++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public struct MyStruct
{
    public int i;
}

public static class E
{
    extension(MyStruct s)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("s")] InterpolationHandler h]
        {
            get
            {
                System.Console.Write(s.i);
                E.field.i++;
                return 0;
            }
        }
    }

    public static MyStruct field;
}
""";

        var exeSource = """
_ = E.field[Increment(), $""];
E.get_Item(E.field, Increment(), $"");

int Increment() => E.field.i++;
""";

        // See GetExtensionBlockMemberReceiverCaptureRefKind
        var expectedOutput = "1233";
        var verifier = CompileAndVerify([exeSource, src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], expectedOutput: expectedOutput)
            .VerifyDiagnostics();

        verifier.VerifyIL("<top-level-statements-entry-point>", """
{
  // Code size       67 (0x43)
  .maxstack  5
  .locals init (MyStruct& V_0,
                int V_1,
                InterpolationHandler V_2,
                MyStruct V_3)
  IL_0000:  ldsflda    "MyStruct E.field"
  IL_0005:  stloc.0
  IL_0006:  call       "int Program.<<Main>$>g__Increment|0_0()"
  IL_000b:  stloc.1
  IL_000c:  ldc.i4.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldloc.0
  IL_000f:  ldobj      "MyStruct"
  IL_0014:  newobj     "InterpolationHandler..ctor(int, int, MyStruct)"
  IL_0019:  stloc.2
  IL_001a:  ldloc.0
  IL_001b:  ldobj      "MyStruct"
  IL_0020:  ldloc.1
  IL_0021:  ldloc.2
  IL_0022:  call       "int E.get_Item(MyStruct, int, InterpolationHandler)"
  IL_0027:  pop
  IL_0028:  ldsfld     "MyStruct E.field"
  IL_002d:  stloc.3
  IL_002e:  ldloc.3
  IL_002f:  call       "int Program.<<Main>$>g__Increment|0_0()"
  IL_0034:  ldc.i4.0
  IL_0035:  ldc.i4.0
  IL_0036:  ldloc.3
  IL_0037:  newobj     "InterpolationHandler..ctor(int, int, MyStruct)"
  IL_003c:  call       "int E.get_Item(MyStruct, int, InterpolationHandler)"
  IL_0041:  pop
  IL_0042:  ret
}
""");

        var comp1 = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);

        CompileAndVerify(exeSource, references: [AsReference(comp1, useMetadataRef)], expectedOutput: expectedOutput)
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_StructReceiverParameter_Generic_ByValueThroughField(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler<TR>
{
    public InterpolationHandler(int literalLength, int formattedCount, TR s)
    {
        System.Console.Write(((MyStruct)(object)s).i);
        E<MyStruct>.field.i++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public struct MyStruct
{
    public int i;
}

public static class E
{
    extension<T>(T s)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("s")] InterpolationHandler<T> h]
        {
            get
            {
                System.Console.Write(((MyStruct)(object)s).i);
                E<MyStruct>.field.i++;
                return 0;
            }
        }
    }
}

public static class E<T>
{
    public static T field;
}
""";

        var exeSource = """
class Program
{
    static void Main()
    {
        Test1<MyStruct>();
        Test2<MyStruct>();
        Test3<MyStruct>();
        Test4<MyStruct>();
    }

    static void Test1<T>()
    {
        _ = E<T>.field[Increment(), $""];
    }

    static void Test2<T>()
    {
        E.get_Item(E<T>.field, Increment(), $"");
    }

    static void Test3<T>() where T : struct
    {
        _ = E<T>.field[Increment(), $""];
    }

    static void Test4<T>() where T : struct
    {
        E.get_Item(E<T>.field, Increment(), $"");
    }

    static int Increment() => E<MyStruct>.field.i++;
}
""";

        // See GetExtensionBlockMemberReceiverCaptureRefKind
        var expectedOutput = "12337899";
        var verifier = CompileAndVerify([exeSource, src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], expectedOutput: expectedOutput)
            .VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>", """
{
  // Code size       73 (0x49)
  .maxstack  3
  .locals init (T& V_0,
                T V_1,
                T& V_2,
                int V_3,
                InterpolationHandler<T> V_4,
                T V_5)
  IL_0000:  ldsflda    "T E<T>.field"
  IL_0005:  stloc.2
  IL_0006:  ldloca.s   V_5
  IL_0008:  initobj    "T"
  IL_000e:  ldloc.s    V_5
  IL_0010:  box        "T"
  IL_0015:  brtrue.s   IL_0022
  IL_0017:  ldloc.2
  IL_0018:  ldobj      "T"
  IL_001d:  stloc.1
  IL_001e:  ldloca.s   V_1
  IL_0020:  br.s       IL_0023
  IL_0022:  ldloc.2
  IL_0023:  stloc.0
  IL_0024:  call       "int Program.Increment()"
  IL_0029:  stloc.3
  IL_002a:  ldc.i4.0
  IL_002b:  ldc.i4.0
  IL_002c:  ldloc.0
  IL_002d:  ldobj      "T"
  IL_0032:  newobj     "InterpolationHandler<T>..ctor(int, int, T)"
  IL_0037:  stloc.s    V_4
  IL_0039:  ldloc.0
  IL_003a:  ldobj      "T"
  IL_003f:  ldloc.3
  IL_0040:  ldloc.s    V_4
  IL_0042:  call       "int E.get_Item<T>(T, int, InterpolationHandler<T>)"
  IL_0047:  pop
  IL_0048:  ret
}
""");

        var expectedIL = """
{
  // Code size       27 (0x1b)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  ldsfld     "T E<T>.field"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       "int Program.Increment()"
  IL_000c:  ldc.i4.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldloc.0
  IL_000f:  newobj     "InterpolationHandler<T>..ctor(int, int, T)"
  IL_0014:  call       "int E.get_Item<T>(T, int, InterpolationHandler<T>)"
  IL_0019:  pop
  IL_001a:  ret
}
""";
        verifier.VerifyIL("Program.Test2<T>", expectedIL);

        verifier.VerifyIL("Program.Test3<T>", """
{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (T& V_0,
                int V_1,
                InterpolationHandler<T> V_2)
  IL_0000:  ldsflda    "T E<T>.field"
  IL_0005:  stloc.0
  IL_0006:  call       "int Program.Increment()"
  IL_000b:  stloc.1
  IL_000c:  ldc.i4.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldloc.0
  IL_000f:  ldobj      "T"
  IL_0014:  newobj     "InterpolationHandler<T>..ctor(int, int, T)"
  IL_0019:  stloc.2
  IL_001a:  ldloc.0
  IL_001b:  ldobj      "T"
  IL_0020:  ldloc.1
  IL_0021:  ldloc.2
  IL_0022:  call       "int E.get_Item<T>(T, int, InterpolationHandler<T>)"
  IL_0027:  pop
  IL_0028:  ret
}
""");

        verifier.VerifyIL("Program.Test4<T>", expectedIL);

        var comp1 = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);

        CompileAndVerify(exeSource, references: [AsReference(comp1, useMetadataRef)], expectedOutput: expectedOutput)
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_StructReceiverParameter_Generic_ByValueThroughField_CompoundAssignment(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler<TR>
{
    public InterpolationHandler(int literalLength, int formattedCount, TR s)
    {
        System.Console.Write(((MyStruct)(object)s).i);
        E<MyStruct>.field.i++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public struct MyStruct
{
    public int i;
}

public static class E
{
    extension<T>(T s)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("s")] InterpolationHandler<T> h]
        {
            get
            {
                System.Console.Write(((MyStruct)(object)s).i);
                E<MyStruct>.field.i++;
                return 0;
            }
            set
            {
                System.Console.Write(((MyStruct)(object)s).i);
                System.Console.Write(" ");
                E<MyStruct>.field.i++;
            }
        }
    }
}

public static class E<T>
{
    public static T field;
}
""";

        var exeSource = """
class Program
{
    static void Main()
    {
        Test1<MyStruct>();
        Test3<MyStruct>();
    }

    static void Test1<T>()
    {
        E<T>.field[Increment(), $""] += 0;
    }

    static void Test3<T>() where T : struct
    {
        E<T>.field[Increment(), $""] += 0;
    }

    static int Increment() => E<MyStruct>.field.i++;
}
""";

        var expectedOutput = "123 567";
        var verifier = CompileAndVerify([exeSource, src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], expectedOutput: expectedOutput)
            .VerifyDiagnostics();

        var comp1 = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);

        CompileAndVerify(exeSource, references: [AsReference(comp1, useMetadataRef)], expectedOutput: expectedOutput)
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_StructReceiverParameter_GenericStruct_ByValueThroughField(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler<TR>
{
    public InterpolationHandler(int literalLength, int formattedCount, TR s)
    {
        System.Console.Write(((MyStruct)(object)s).i);
        E<MyStruct>.field.i++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public struct MyStruct
{
    public int i;
}

public static class E
{
    extension<T>(T s) where T : struct
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("s")] InterpolationHandler<T> h]
        {
            get
            {
                System.Console.Write(((MyStruct)(object)s).i);
                E<MyStruct>.field.i++;
                return 0;
            }
        }
    }
}

public static class E<T>
{
    public static T field;
}
""";

        var exeSource = """
class Program
{
    static void Main()
    {
        Test3<MyStruct>();
        Test4<MyStruct>();
    }

    static void Test3<T>() where T : struct
    {
        _ = E<T>.field[Increment(), $""];
    }

    static void Test4<T>() where T : struct
    {
        E.get_Item(E<T>.field, Increment(), $"");
    }

    static int Increment() => E<MyStruct>.field.i++;
}
""";

        var expectedOutput = "1233";
        var verifier = CompileAndVerify([exeSource, src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], expectedOutput: expectedOutput)
            .VerifyDiagnostics();

        verifier.VerifyIL("Program.Test3<T>", """
{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (T& V_0,
                int V_1,
                InterpolationHandler<T> V_2)
  IL_0000:  ldsflda    "T E<T>.field"
  IL_0005:  stloc.0
  IL_0006:  call       "int Program.Increment()"
  IL_000b:  stloc.1
  IL_000c:  ldc.i4.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldloc.0
  IL_000f:  ldobj      "T"
  IL_0014:  newobj     "InterpolationHandler<T>..ctor(int, int, T)"
  IL_0019:  stloc.2
  IL_001a:  ldloc.0
  IL_001b:  ldobj      "T"
  IL_0020:  ldloc.1
  IL_0021:  ldloc.2
  IL_0022:  call       "int E.get_Item<T>(T, int, InterpolationHandler<T>)"
  IL_0027:  pop
  IL_0028:  ret
}
""");

        verifier.VerifyIL("Program.Test4<T>", """
{
  // Code size       27 (0x1b)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  ldsfld     "T E<T>.field"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       "int Program.Increment()"
  IL_000c:  ldc.i4.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldloc.0
  IL_000f:  newobj     "InterpolationHandler<T>..ctor(int, int, T)"
  IL_0014:  call       "int E.get_Item<T>(T, int, InterpolationHandler<T>)"
  IL_0019:  pop
  IL_001a:  ret
}
""");

        var comp1 = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);

        CompileAndVerify(exeSource, references: [AsReference(comp1, useMetadataRef)], expectedOutput: expectedOutput)
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_ClassReceiverParameter_GenericClass_ByValueThroughField(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler<TR>
{
    public InterpolationHandler(int literalLength, int formattedCount, TR s)
    {
        System.Console.Write(((MyClass)(object)s).i);
        E<MyClass>.field = new MyClass() { i = E<MyClass>.field.i + 1 };
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public class MyClass
{
    public int i;
}

public static class E
{
    extension<T>(T s)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("s")] InterpolationHandler<T> h]
        {
            get
            {
                System.Console.Write(((MyClass)(object)s).i);
                E<MyClass>.field = new MyClass() { i = E<MyClass>.field.i + 1 };
                return 0;
            }
        }
    }
}

public static class E<T>
{
    public static T field;
}
""";

        var exeSource = """
class Program
{
    static void Main()
    {
        E<MyClass>.field = new MyClass();
        Test1<MyClass>();
        Test2<MyClass>();
        Test3<MyClass>();
        Test4<MyClass>();
    }

    static void Test1<T>()
    {
        _ = E<T>.field[Increment(), $""];
    }

    static void Test2<T>()
    {
        E.get_Item(E<T>.field, Increment(), $"");
    }

    static void Test3<T>() where T : class
    {
        _ = E<T>.field[Increment(), $""];
    }

    static void Test4<T>() where T : class
    {
        E.get_Item(E<T>.field, Increment(), $"");
    }

    static int Increment() => (E<MyClass>.field = new MyClass() { i = E<MyClass>.field.i + 1 }).i;
}
""";

        var expectedOutput = "00336699";
        var verifier = CompileAndVerify([exeSource, src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], expectedOutput: expectedOutput)
            .VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>", """
{
  // Code size       73 (0x49)
  .maxstack  3
  .locals init (T& V_0,
                T V_1,
                T& V_2,
                int V_3,
                InterpolationHandler<T> V_4,
                T V_5)
  IL_0000:  ldsflda    "T E<T>.field"
  IL_0005:  stloc.2
  IL_0006:  ldloca.s   V_5
  IL_0008:  initobj    "T"
  IL_000e:  ldloc.s    V_5
  IL_0010:  box        "T"
  IL_0015:  brtrue.s   IL_0022
  IL_0017:  ldloc.2
  IL_0018:  ldobj      "T"
  IL_001d:  stloc.1
  IL_001e:  ldloca.s   V_1
  IL_0020:  br.s       IL_0023
  IL_0022:  ldloc.2
  IL_0023:  stloc.0
  IL_0024:  call       "int Program.Increment()"
  IL_0029:  stloc.3
  IL_002a:  ldc.i4.0
  IL_002b:  ldc.i4.0
  IL_002c:  ldloc.0
  IL_002d:  ldobj      "T"
  IL_0032:  newobj     "InterpolationHandler<T>..ctor(int, int, T)"
  IL_0037:  stloc.s    V_4
  IL_0039:  ldloc.0
  IL_003a:  ldobj      "T"
  IL_003f:  ldloc.3
  IL_0040:  ldloc.s    V_4
  IL_0042:  call       "int E.get_Item<T>(T, int, InterpolationHandler<T>)"
  IL_0047:  pop
  IL_0048:  ret
}
""");

        verifier.VerifyIL("Program.Test2<T>", """
{
  // Code size       27 (0x1b)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  ldsfld     "T E<T>.field"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       "int Program.Increment()"
  IL_000c:  ldc.i4.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldloc.0
  IL_000f:  newobj     "InterpolationHandler<T>..ctor(int, int, T)"
  IL_0014:  call       "int E.get_Item<T>(T, int, InterpolationHandler<T>)"
  IL_0019:  pop
  IL_001a:  ret
}
""");

        verifier.VerifyIL("Program.Test3<T>", """
{
  // Code size       27 (0x1b)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  ldsfld     "T E<T>.field"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       "int Program.Increment()"
  IL_000c:  ldc.i4.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldloc.0
  IL_000f:  newobj     "InterpolationHandler<T>..ctor(int, int, T)"
  IL_0014:  call       "int E.get_Item<T>(T, int, InterpolationHandler<T>)"
  IL_0019:  pop
  IL_001a:  ret
}
""");

        verifier.VerifyIL("Program.Test4<T>", """
{
  // Code size       27 (0x1b)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  ldsfld     "T E<T>.field"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       "int Program.Increment()"
  IL_000c:  ldc.i4.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldloc.0
  IL_000f:  newobj     "InterpolationHandler<T>..ctor(int, int, T)"
  IL_0014:  call       "int E.get_Item<T>(T, int, InterpolationHandler<T>)"
  IL_0019:  pop
  IL_001a:  ret
}
""");

        var comp1 = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);

        CompileAndVerify(exeSource, references: [AsReference(comp1, useMetadataRef)], expectedOutput: expectedOutput)
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_ClassReceiverParameter_Generic_ByValueThroughField(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler<TR>
{
    public InterpolationHandler(int literalLength, int formattedCount, TR s)
    {
        System.Console.Write(((MyClass)(object)s).i);
        E<MyClass>.field = new MyClass() { i = E<MyClass>.field.i + 1 };
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public class MyClass
{
    public int i;
}

public static class E
{
    extension<T>(T s) where T : class
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("s")] InterpolationHandler<T> h]
        {
            get
            {
                System.Console.Write(((MyClass)(object)s).i);
                E<MyClass>.field = new MyClass() { i = E<MyClass>.field.i + 1 };
                return 0;
            }
        }
    }
}

public static class E<T>
{
    public static T field;
}
""";

        var exeSource = """
class Program
{
    static void Main()
    {
        E<MyClass>.field = new MyClass();
        Test3<MyClass>();
        Test4<MyClass>();
    }

    static void Test3<T>() where T : class
    {
        _ = E<T>.field[Increment(), $""];
    }

    static void Test4<T>() where T : class
    {
        E.get_Item(E<T>.field, Increment(), $"");
    }

    static int Increment() => (E<MyClass>.field = new MyClass() { i = E<MyClass>.field.i + 1 }).i;
}
""";

        var expectedOutput = "0033";
        var verifier = CompileAndVerify([exeSource, src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], expectedOutput: expectedOutput)
            .VerifyDiagnostics();

        verifier.VerifyIL("Program.Test3<T>", """
{
  // Code size       27 (0x1b)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  ldsfld     "T E<T>.field"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       "int Program.Increment()"
  IL_000c:  ldc.i4.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldloc.0
  IL_000f:  newobj     "InterpolationHandler<T>..ctor(int, int, T)"
  IL_0014:  call       "int E.get_Item<T>(T, int, InterpolationHandler<T>)"
  IL_0019:  pop
  IL_001a:  ret
}
""");

        verifier.VerifyIL("Program.Test4<T>", """
{
  // Code size       27 (0x1b)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  ldsfld     "T E<T>.field"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       "int Program.Increment()"
  IL_000c:  ldc.i4.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldloc.0
  IL_000f:  newobj     "InterpolationHandler<T>..ctor(int, int, T)"
  IL_0014:  call       "int E.get_Item<T>(T, int, InterpolationHandler<T>)"
  IL_0019:  pop
  IL_001a:  ret
}
""");

        var comp1 = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);

        CompileAndVerify(exeSource, references: [AsReference(comp1, useMetadataRef)], expectedOutput: expectedOutput)
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_RefStructReceiverParameter_EscapeScopes_01(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public ref struct InterpolationHandler
{
#pragma warning disable CS0169 // The field 'InterpolationHandler.i' is never used
    private ref int i;
#pragma warning restore CS0169 // The field 'InterpolationHandler.i' is never used

    public InterpolationHandler(int literalLength, int formattedCount, scoped MyStruct s)
    {
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public ref struct MyStruct
{
    public ref int i;
}

public static class E
{
    extension(MyStruct s)
    {
        public MyStruct this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("s")] InterpolationHandler h]
        {
            get
            {
                return new();
            }
        }
    }
}
""";

        var exeSource = """
#pragma warning disable CS8321 // The local function 'localFunc' is declared but never used
MyStruct localFunc()
#pragma warning restore CS8321 // The local function 'localFunc' is declared but never used
{
    return new MyStruct()[$""];
}
""";

        var verifier = CompileAndVerify([exeSource, src], targetFramework: TargetFramework.Net100, verify: Verification.Fails).VerifyDiagnostics();

        verifier.VerifyIL("Program.<<Main>$>g__localFunc|0_0()", """
{
  // Code size       23 (0x17)
  .maxstack  4
  .locals init (MyStruct V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "MyStruct"
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  newobj     "InterpolationHandler..ctor(int, int, scoped MyStruct)"
  IL_0011:  call       "MyStruct E.get_Item(MyStruct, InterpolationHandler)"
  IL_0016:  ret
}
""");

        var comp1 = CreateCompilation(src, targetFramework: TargetFramework.Net100);

        CompileAndVerify(exeSource, references: [AsReference(comp1, useMetadataRef)], targetFramework: TargetFramework.Net100, verify: Verification.Fails)
            .VerifyDiagnostics();
    }

    [Fact]
    public void InterpolationHandler_RefStructReceiverParameter_EscapeScopes_02()
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public ref struct InterpolationHandler
{
    public ref int i;

    public InterpolationHandler(int literalLength, int formattedCount, MyStruct s2)
    {
        i = ref s2.i;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public ref struct MyStruct
{
    public ref int i;
}

public static class E
{
    extension(MyStruct s1)
    {
        public MyStruct this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("s1")] InterpolationHandler h]
            => new() { i = ref h.i };
    }
}
""";

        var exeSource = """
#pragma warning disable CS8321 // The local function 'localFunc' is declared but never used
MyStruct localFunc()
#pragma warning restore CS8321 // The local function 'localFunc' is declared but never used
{
    int i = 0;
    return new MyStruct() { i = ref i }[$""];
}
""";

        CreateCompilation([exeSource, src], targetFramework: TargetFramework.Net100).VerifyDiagnostics(
            // (6,27): error CS8352: Cannot use variable 'i = ref i' in this context because it may expose referenced variables outside of their declaration scope
            //     return new MyStruct() { i = ref i }[$""];
            Diagnostic(ErrorCode.ERR_EscapeVariable, "{ i = ref i }").WithArguments("i = ref i").WithLocation(6, 27),
            // (6,12): error CS8347: Cannot use a result of 'E.extension(MyStruct).this[InterpolationHandler]' in this context because it may expose variables referenced by parameter 's1' outside of their declaration scope
            //     return new MyStruct() { i = ref i }[$""];
            Diagnostic(ErrorCode.ERR_EscapeCall, @"new MyStruct() { i = ref i }[$""""]").WithArguments("E.extension(MyStruct).this[InterpolationHandler]", "s1").WithLocation(6, 12)
        );
    }

    [Fact]
    public void InterpolationHandler_RefStructReceiverParameter_EscapeScopes_05()
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public ref struct InterpolationHandler
{
    public ref int i;

    public InterpolationHandler(int literalLength, int formattedCount, MyStruct s2)
    {
        i = ref s2.i;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public ref struct MyStruct
{
    public ref int i;
}

public static class E
{
    extension(scoped MyStruct s1)
    {
        public MyStruct this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("s1")] InterpolationHandler h]
            => new() { i = ref h.i };
    }
}
""";

        var exeSource = """
#pragma warning disable CS8321 // The local function 'localFunc' is declared but never used
MyStruct localFunc()
#pragma warning restore CS8321 // The local function 'localFunc' is declared but never used
{
    int i = 0;
    return new MyStruct() { i = ref i }[$""];
}
""";

        CreateCompilation([exeSource, src], targetFramework: TargetFramework.Net100).VerifyDiagnostics(
            // (6,12): error CS8352: Cannot use variable 'new MyStruct() { i = ref i }' in this context because it may expose referenced variables outside of their declaration scope
            //     return new MyStruct() { i = ref i }[$""];
            Diagnostic(ErrorCode.ERR_EscapeVariable, "new MyStruct() { i = ref i }").WithArguments("new MyStruct() { i = ref i }").WithLocation(6, 12),
            // (6,12): error CS8347: Cannot use a result of 'E.extension(scoped MyStruct).this[InterpolationHandler]' in this context because it may expose variables referenced by parameter 'h' outside of their declaration scope
            //     return new MyStruct() { i = ref i }[$""];
            Diagnostic(ErrorCode.ERR_EscapeCall, @"new MyStruct() { i = ref i }[$""""]").WithArguments("E.extension(scoped MyStruct).this[InterpolationHandler]", "h").WithLocation(6, 12),
            // (6,41): error CS8347: Cannot use a result of 'InterpolationHandler.InterpolationHandler(int, int, MyStruct)' in this context because it may expose variables referenced by parameter 's2' outside of their declaration scope
            //     return new MyStruct() { i = ref i }[$""];
            Diagnostic(ErrorCode.ERR_EscapeCall, @"$""""").WithArguments("InterpolationHandler.InterpolationHandler(int, int, MyStruct)", "s2").WithLocation(6, 41)
        );
    }

    [Fact]
    public void InterpolationHandler_RefStructReceiverParameter_EscapeScopes_06()
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public ref struct InterpolationHandler
{
    public ref int i;

    public InterpolationHandler(int literalLength, int formattedCount, MyStruct s2)
    {
        i = ref s2.i;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public ref struct MyStruct
{
    public ref int i;
}

public static class E
{
    extension(scoped MyStruct s1)
    {
        public MyStruct this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("s1")] scoped InterpolationHandler h]
            => new() { i = ref h.i };
    }
}
""";

        var exeSource = """
#pragma warning disable CS8321 // The local function 'localFunc' is declared but never used
MyStruct localFunc()
#pragma warning restore CS8321 // The local function 'localFunc' is declared but never used
{
    int i = 0;
    return new MyStruct() { i = ref i }[$""];
}
""";

        CreateCompilation([exeSource, src], targetFramework: TargetFramework.Net100).VerifyDiagnostics(
            // (24,22): error CS8352: Cannot use variable 'i = ref h.i' in this context because it may expose referenced variables outside of their declaration scope
            //             => new() { i = ref h.i };
            Diagnostic(ErrorCode.ERR_EscapeVariable, "{ i = ref h.i }").WithArguments("i = ref h.i").WithLocation(24, 22)
        );
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_RefStructReceiverParameter_EscapeScopes_07(bool useMetadataRef)
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public ref struct InterpolationHandler
{
#pragma warning disable CS0169 // The field 'InterpolationHandler.i' is never used
    private ref int i;
#pragma warning restore CS0169 // The field 'InterpolationHandler.i' is never used

    public InterpolationHandler(int literalLength, int formattedCount, scoped MyStruct s2)
    {
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public ref struct MyStruct
{
    public ref int i;
}

public static class E
{
    extension(scoped MyStruct s1)
    {
        public MyStruct this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("s1")] scoped InterpolationHandler h]
            => new();
    }
}
""";

        var exeSource = """
#pragma warning disable CS8321 // The local function 'localFunc' is declared but never used
MyStruct localFunc()
#pragma warning restore CS8321 // The local function 'localFunc' is declared but never used
{
    int i = 0;
    return new MyStruct() { i = ref i }[$""];
}
""";

        var verifier = CompileAndVerify([exeSource, src], targetFramework: TargetFramework.Net100, verify: Verification.Fails).VerifyDiagnostics();

        verifier.VerifyIL("Program.<<Main>$>g__localFunc|0_0()", """
{
  // Code size       36 (0x24)
  .maxstack  4
  .locals init (int V_0, //i
                MyStruct V_1,
                MyStruct V_2)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_2
  IL_0004:  initobj    "MyStruct"
  IL_000a:  ldloca.s   V_2
  IL_000c:  ldloca.s   V_0
  IL_000e:  stfld      "ref int MyStruct.i"
  IL_0013:  ldloc.2
  IL_0014:  stloc.1
  IL_0015:  ldloc.1
  IL_0016:  ldc.i4.0
  IL_0017:  ldc.i4.0
  IL_0018:  ldloc.1
  IL_0019:  newobj     "InterpolationHandler..ctor(int, int, scoped MyStruct)"
  IL_001e:  call       "MyStruct E.get_Item(scoped MyStruct, scoped InterpolationHandler)"
  IL_0023:  ret
}
""");

        var comp1 = CreateCompilation(src, targetFramework: TargetFramework.Net100);

        CompileAndVerify(exeSource, references: [AsReference(comp1, useMetadataRef)], targetFramework: TargetFramework.Net100, verify: Verification.Fails)
            .VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_ReceiverParameter_NullableMismatch_01(bool useMetadataRef, bool useOut)
    {
        string outParam = useOut ? ", out bool valid" : "";
        var src = $$"""
#nullable enable
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount, C s1, string s2{{outParam}})
    {
        System.Console.Write(s1);
        System.Console.Write(s2);
        {{(useOut ? "valid = true;" : "")}}
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string? format = null) => throw null!;
}

public static class E
{
    extension(C? s1)
    {
        public int this[string? s2, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("s1", "s2")] InterpolationHandler h] { get => 0; }
    }
}

public class C { }
""";

        var exeSource = """
#nullable enable
_ = ((C?)null)[null, $""]; // 1, 2
_ = ((C?)null)["", $""]; // 3
_ = new C()[null, $""]; // 4
_ = new C()["", $""];

_ = E.get_Item(null, null, $""); // 5, 6
_ = E.get_Item(new C(), null, $""); // 7
_ = E.get_Item(null, "", $""); // 8
_ = E.get_Item(new C(), "", $"");
""";

        var expectedDiagnostics = new[] {
            // (2,6): warning CS8604: Possible null reference argument for parameter 's1' in 'InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, C s1, string s2, out bool valid)'.
            // _ = ((C?)null)[null, $""]; // 1, 2
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "(C?)null").WithArguments("s1", $"InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, C s1, string s2{outParam})").WithLocation(2, 6),
            // (2,16): warning CS8604: Possible null reference argument for parameter 's2' in 'InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, C s1, string s2, out bool valid)'.
            // _ = ((C?)null)[null, $""]; // 1, 2
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null").WithArguments("s2", $"InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, C s1, string s2{outParam})").WithLocation(2, 16),
            // (3,6): warning CS8604: Possible null reference argument for parameter 's1' in 'InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, C s1, string s2, out bool valid)'.
            // _ = ((C?)null)["", $""]; // 3
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "(C?)null").WithArguments("s1", $"InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, C s1, string s2{outParam})").WithLocation(3, 6),
            // (4,13): warning CS8604: Possible null reference argument for parameter 's2' in 'InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, C s1, string s2, out bool valid)'.
            // _ = new C()[null, $""]; // 4
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null").WithArguments("s2", $"InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, C s1, string s2{outParam})").WithLocation(4, 13),
            // (7,16): warning CS8604: Possible null reference argument for parameter 's1' in 'InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, C s1, string s2, out bool valid)'.
            // _ = E.get_Item(null, null, $""); // 5, 6
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null").WithArguments("s1", $"InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, C s1, string s2{outParam})").WithLocation(7, 16),
            // (7,22): warning CS8604: Possible null reference argument for parameter 's2' in 'InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, C s1, string s2, out bool valid)'.
            // _ = E.get_Item(null, null, $""); // 5, 6
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null").WithArguments("s2", $"InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, C s1, string s2{outParam})").WithLocation(7, 22),
            // (8,25): warning CS8604: Possible null reference argument for parameter 's2' in 'InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, C s1, string s2, out bool valid)'.
            // _ = E.get_Item(new C(), null, $""); // 7
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null").WithArguments("s2", $"InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, C s1, string s2{outParam})").WithLocation(8, 25),
            // (9,16): warning CS8604: Possible null reference argument for parameter 's1' in 'InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, C s1, string s2, out bool valid)'.
            // _ = E.get_Item(null, "", $""); // 8
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null").WithArguments("s1", $"InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, C s1, string s2{outParam})").WithLocation(9, 16)
        };

        CreateCompilation([exeSource, src], targetFramework: TargetFramework.Net100).VerifyDiagnostics(expectedDiagnostics);

        var comp1 = CreateCompilation(src, targetFramework: TargetFramework.Net100);

        CreateCompilation(exeSource, references: [AsReference(comp1, useMetadataRef)], targetFramework: TargetFramework.Net100).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, CombinatorialData]
    public void InterpolationHandler_ReceiverParameter_NullableMismatch_02(bool useMetadataRef, bool useOut)
    {
        string outParam = useOut ? ", out bool valid" : "";
        var src = $$"""
#nullable enable
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount, string s1, string s2{{outParam}})
    {
        System.Console.Write(s1);
        System.Console.Write(s2);
        {{(useOut ? "valid = true;" : "")}}
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string? format = null) => throw null!;
}

public static class E
{
    extension(C? s1)
    {
        public int this[string? s2, string? s3, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("s2", "s3")] InterpolationHandler h] { get => 0; }
    }
}

public class C { }
""";

        var exeSource = """
#nullable enable
_ = new C()[null, null, $""];
_ = new C()[null, "", $""];
_ = new C()["", null, $""];
_ = new C()["", "", $""];

E.get_Item(new C(), null, null, $"");
E.get_Item(new C(), "", null, $"");
E.get_Item(new C(), null, "", $"");
E.get_Item(new C(), "", "", $"");
""";

        var expectedDiagnostics = new[] {
            // (2,13): warning CS8604: Possible null reference argument for parameter 's1' in 'InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, string s1, string s2)'.
            // _ = new C()[null, null, $""];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null").WithArguments("s1", $"InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, string s1, string s2{outParam})").WithLocation(2, 13),
            // (2,19): warning CS8604: Possible null reference argument for parameter 's2' in 'InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, string s1, string s2)'.
            // _ = new C()[null, null, $""];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null").WithArguments("s2", $"InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, string s1, string s2{outParam})").WithLocation(2, 19),
            // (3,13): warning CS8604: Possible null reference argument for parameter 's1' in 'InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, string s1, string s2)'.
            // _ = new C()[null, "", $""];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null").WithArguments("s1", $"InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, string s1, string s2{outParam})").WithLocation(3, 13),
            // (4,17): warning CS8604: Possible null reference argument for parameter 's2' in 'InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, string s1, string s2)'.
            // _ = new C()["", null, $""];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null").WithArguments("s2", $"InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, string s1, string s2{outParam})").WithLocation(4, 17),
            // (7,21): warning CS8604: Possible null reference argument for parameter 's1' in 'InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, string s1, string s2)'.
            // E.get_Item(new C(), null, null, $"");
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null").WithArguments("s1", $"InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, string s1, string s2{outParam})").WithLocation(7, 21),
            // (7,27): warning CS8604: Possible null reference argument for parameter 's2' in 'InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, string s1, string s2)'.
            // E.get_Item(new C(), null, null, $"");
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null").WithArguments("s2", $"InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, string s1, string s2{outParam})").WithLocation(7, 27),
            // (8,25): warning CS8604: Possible null reference argument for parameter 's2' in 'InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, string s1, string s2)'.
            // E.get_Item(new C(), "", null, $"");
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null").WithArguments("s2", $"InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, string s1, string s2{outParam})").WithLocation(8, 25),
            // (9,21): warning CS8604: Possible null reference argument for parameter 's1' in 'InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, string s1, string s2)'.
            // E.get_Item(new C(), null, "", $"");
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "null").WithArguments("s1", $"InterpolationHandler.InterpolationHandler(int literalLength, int formattedCount, string s1, string s2{outParam})").WithLocation(9, 21)
        };

        CreateCompilation([exeSource, src], targetFramework: TargetFramework.Net100).VerifyDiagnostics(expectedDiagnostics);

        var comp1 = CreateCompilation(src, targetFramework: TargetFramework.Net100);

        CreateCompilation(exeSource, references: [useMetadataRef ? comp1.ToMetadataReference() : comp1.EmitToImageReference()], targetFramework: TargetFramework.Net100).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void InterpolationHandler_ParameterErrors_MappedCorrectly_01()
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount, int i)
    {
        System.Console.WriteLine(i);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class E
{
    extension(int i)
    {
        public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("nonexistent")] InterpolationHandler h] { get => 0; }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (16,26): error CS8945: 'nonexistent' is not a valid parameter name from 'E.extension(int).this[InterpolationHandler]'.
            //         public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("nonexistent")] InterpolationHandler h] { get => 0; }
            Diagnostic(ErrorCode.ERR_InvalidInterpolatedStringHandlerArgumentName, @"System.Runtime.CompilerServices.InterpolatedStringHandlerArgument(""nonexistent"")").WithArguments("nonexistent", "E.extension(int).this[InterpolationHandler]").WithLocation(16, 26)
        );

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionBlockDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.True(symbol.IsExtension);
        var underlying = symbol.GetSymbol<NamedTypeSymbol>();
        var indexer = underlying.GetMember<PropertySymbol>("this[]");
        Assert.False(underlying.ExtensionParameter.HasInterpolatedStringHandlerArgumentError);
        Assert.True(underlying.ExtensionParameter.InterpolatedStringHandlerArgumentIndexes.IsEmpty);
        Assert.True(indexer.Parameters[0].HasInterpolatedStringHandlerArgumentError);
        Assert.True(indexer.Parameters[0].InterpolatedStringHandlerArgumentIndexes.IsEmpty);

        var implGetter = underlying.ContainingType.GetMember<MethodSymbol>("get_Item");
        Assert.False(implGetter.Parameters[0].HasInterpolatedStringHandlerArgumentError);
        Assert.True(implGetter.Parameters[0].InterpolatedStringHandlerArgumentIndexes.IsEmpty);
        Assert.True(implGetter.Parameters[1].HasInterpolatedStringHandlerArgumentError);
        Assert.True(implGetter.Parameters[1].InterpolatedStringHandlerArgumentIndexes.IsEmpty);
    }

    [Theory]
    [InlineData("i")]
    [InlineData("")]
    [InlineData("nonexistent")]
    public void InterpolationHandler_ParameterErrors_MappedCorrectly_02(string attributeValue)
    {
        var src = $$"""
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount, InterpolationHandler i)
    {
        System.Console.WriteLine(i);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class E
{
    extension([System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("{{attributeValue}}")] InterpolationHandler i)
    {
        public int this[int j] { get => 0; }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (14,16): error CS9325: Interpolated string handler arguments are not allowed in this context.
            //     extension([System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("")] InterpolationHandler i)
            Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerArgumentDisallowed, $@"System.Runtime.CompilerServices.InterpolatedStringHandlerArgument(""{attributeValue}"")").WithLocation(14, 16)
        );

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionBlockDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.True(symbol.IsExtension);
        var underlying = symbol.GetSymbol<NamedTypeSymbol>();
        Assert.True(underlying.ExtensionParameter.HasInterpolatedStringHandlerArgumentError);
        Assert.True(underlying.ExtensionParameter.InterpolatedStringHandlerArgumentIndexes.IsEmpty);

        var implGetter = underlying.ContainingType.GetMember<MethodSymbol>("get_Item");
        Assert.True(implGetter.Parameters[0].HasInterpolatedStringHandlerArgumentError);
        Assert.True(implGetter.Parameters[0].InterpolatedStringHandlerArgumentIndexes.IsEmpty);
    }

    [Fact]
    public void InterpolationHandler_ParameterErrors_MappedCorrectly_03()
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount, int i)
    {
        System.Console.WriteLine(i);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class E
{
    extension(int i)
    {
        public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("")] InterpolationHandler h] { get => 0; }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (16,26): error CS8944: 'E.extension(int).this[InterpolationHandler]' is not an instance method, the receiver or extension receiver parameter cannot be an interpolated string handler argument.
            //         public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("")] InterpolationHandler h] { get => 0; }
            Diagnostic(ErrorCode.ERR_NotInstanceInvalidInterpolatedStringHandlerArgumentName, @"System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("""")").WithArguments("E.extension(int).this[InterpolationHandler]").WithLocation(16, 26)
        );

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionBlockDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.True(symbol.IsExtension);
        var underlying = symbol.GetSymbol<NamedTypeSymbol>();
        var indexer = underlying.GetMember<PropertySymbol>("this[]");
        Assert.False(underlying.ExtensionParameter.HasInterpolatedStringHandlerArgumentError);
        Assert.True(underlying.ExtensionParameter.InterpolatedStringHandlerArgumentIndexes.IsEmpty);
        Assert.True(indexer.Parameters[0].HasInterpolatedStringHandlerArgumentError);
        Assert.True(indexer.Parameters[0].InterpolatedStringHandlerArgumentIndexes.IsEmpty);

        var implGetter = underlying.ContainingType.GetMember<MethodSymbol>("get_Item");
        Assert.False(implGetter.Parameters[0].HasInterpolatedStringHandlerArgumentError);
        Assert.True(implGetter.Parameters[0].InterpolatedStringHandlerArgumentIndexes.IsEmpty);
        Assert.True(implGetter.Parameters[1].HasInterpolatedStringHandlerArgumentError);
        Assert.True(implGetter.Parameters[1].InterpolatedStringHandlerArgumentIndexes.IsEmpty);
    }

    [Fact]
    public void InterpolationHandler_ReferencesInstanceParameter_FromMetadata()
    {
        // Equivalent to:
        // [System.Runtime.CompilerServices.InterpolatedStringHandler]
        // public struct InterpolationHandler
        // {
        //     public InterpolationHandler(int literalLength, int formattedCount, string param)
        //     {
        //     }
        //     public void AppendLiteral(string value) { }
        //     public void AppendFormatted<T>(T hole, int alignment = 0, string? format = null) { }
        // }

        //public static class E
        //{
        //    extension(int i)
        //    {
        //        public int this[[System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("i")] InterpolationHandler h]
        //        {
        //        }
        //    }
        //}

        var il = """
.class public sequential ansi sealed beforefieldinit InterpolationHandler
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute::.ctor() = (01 00 00 00)
    .pack 0
    .size 1

    // Methods
    .method public hidebysig specialname rtspecialname instance void .ctor (int32 literalLength, int32 formattedCount, int32 param) cil managed
    {
        nop
        ret
    }

    .method public hidebysig instance void AppendLiteral (string 'value') cil managed
    {
        nop
        ret
    }

    .method public hidebysig instance void AppendFormatted<T> (!!T hole, [opt] int32 'alignment', [opt] string format) cil managed
    {
        .param [2] = int32(0)
        .param [3] = nullref
        nop
        ret
    }
}

.class public auto ansi abstract sealed beforefieldinit E
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6d 00 00 )
        .class nested public auto ansi abstract sealed specialname '<M>$F4B4FFE41AB49E80A4ECF390CF6EB372'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 i ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )

                ret
            }
        }

        .method public hidebysig specialname instance int32 get_Item ( valuetype InterpolationHandler h ) cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            .param [1]
            .custom instance void [mscorlib]System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute::.ctor(string) = (01 00 00 00 00)

            newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            throw
        }

        .property instance int32 Item( valuetype InterpolationHandler h )
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            .get instance int32 E/'<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::get_Item(valuetype InterpolationHandler)
        }
    }

    .method public hidebysig static int32 get_Item ( int32 i, valuetype InterpolationHandler h ) cil managed 
    {
        .param [2]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute::.ctor(string) = (01 00 00 00 00)

        ldc.i4.0
        ret
    }
}
""" + ExtensionMarkerAttributeIL;

        var src = """
_ = 1[$""];
E.get_Item(1, $"");
""";

        CreateCompilationWithIL(src, ilSource: il, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (1,7): error CS8949: The InterpolatedStringHandlerArgumentAttribute applied to parameter 'InterpolationHandler h' is malformed and cannot be interpreted. Construct an instance of 'InterpolationHandler' manually.
            // _ = 1[$""];
            Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerArgumentAttributeMalformed, @"$""""").WithArguments("InterpolationHandler h", "InterpolationHandler").WithLocation(1, 7),
            // (1,7): error CS7036: There is no argument given that corresponds to the required parameter 'param' of 'InterpolationHandler.InterpolationHandler(int, int, int)'
            // _ = 1[$""];
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, @"$""""").WithArguments("param", "InterpolationHandler.InterpolationHandler(int, int, int)").WithLocation(1, 7),
            // (1,7): error CS1615: Argument 3 may not be passed with the 'out' keyword
            // _ = 1[$""];
            Diagnostic(ErrorCode.ERR_BadArgExtraRef, @"$""""").WithArguments("3", "out").WithLocation(1, 7),
            // (2,15): error CS8949: The InterpolatedStringHandlerArgumentAttribute applied to parameter 'InterpolationHandler h' is malformed and cannot be interpreted. Construct an instance of 'InterpolationHandler' manually.
            // E.get_Item(1, $"");
            Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerArgumentAttributeMalformed, @"$""""").WithArguments("InterpolationHandler h", "InterpolationHandler").WithLocation(2, 15),
            // (2,15): error CS7036: There is no argument given that corresponds to the required parameter 'param' of 'InterpolationHandler.InterpolationHandler(int, int, int)'
            // E.get_Item(1, $"");
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, @"$""""").WithArguments("param", "InterpolationHandler.InterpolationHandler(int, int, int)").WithLocation(2, 15),
            // (2,15): error CS1615: Argument 3 may not be passed with the 'out' keyword
            // E.get_Item(1, $"");
            Diagnostic(ErrorCode.ERR_BadArgExtraRef, @"$""""").WithArguments("3", "out").WithLocation(2, 15)
        );
    }

    [Fact]
    public void InterpolationHandler_AsExtensionParameter()
    {
        var src = """
_ = $"{42}"[""];

[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount)
    {
    }

    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class E
{
    extension(InterpolationHandler h)
    {
        public int this[string s] { get => 0; }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        comp.VerifyDiagnostics(
            // (1,13): error CS1503: Argument 1: cannot convert from 'string' to 'int'
            // _ = $"{42}"[""];
            Diagnostic(ErrorCode.ERR_BadArgType, @"""""").WithArguments("1", "string", "int").WithLocation(1, 13));
    }

    [Fact]
    public void InterpolationHandler_ObjectInitializer_01()
    {
        var code = """
public class Program
{
    public static void Main()
    {
        /*<bind>*/
        _ = new C() { [42, $"{43}"] = 1 };
        /*</bind>*/
    }
}

public class C { }

[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, int i)
    {
        System.Console.Write($"{i} ");
    }

    public void AppendFormatted(int i) 
    {
        System.Console.Write($"{i} ");
    }
}

public static class CExt
{
    extension(C c)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("i")] CustomHandler h]
        {
            set { }
        }
    }
}
""";

        var comp = CreateCompilation(code, targetFramework: TargetFramework.Net100, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("42 43"), verify: Verification.Skipped).VerifyDiagnostics();

        string expectedOperationTree = """
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = new C() ... 3}"] = 1 };')
  Expression:
    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: '_ = new C() ... 43}"] = 1 }')
      Left:
        IDiscardOperation (Symbol: C _) (OperationKind.Discard, Type: C) (Syntax: '_')
      Right:
        IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { [ ... 43}"] = 1 }')
          Arguments(0)
          Initializer:
            IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ [42, $"{43}"] = 1 }')
              Initializers(1):
                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[42, $"{43}"] = 1')
                    Left:
                      IPropertyReferenceOperation: System.Int32 CExt.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.this[System.Int32 i, CustomHandler h] { set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '[42, $"{43}"]')
                        Instance Receiver:
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '[42, $"{43}"]')
                        Arguments(2):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '42')
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: h) (OperationKind.Argument, Type: null) (Syntax: '$"{43}"')
                              IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler, IsImplicit) (Syntax: '$"{43}"')
                                Creation:
                                  IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, System.Int32 i)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$"{43}"')
                                    Arguments(3):
                                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$"{43}"')
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$"{43}"')
                                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$"{43}"')
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$"{43}"')
                                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '42')
                                          IInterpolatedStringHandlerArgumentPlaceholderOperation (ArgumentIndex: 0) (OperationKind.InterpolatedStringHandlerArgumentPlaceholder, Type: null, IsImplicit) (Syntax: '42')
                                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    Initializer:
                                      null
                                Content:
                                  IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"{43}"')
                                    Parts(1):
                                        IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{43}')
                                          AppendCall:
                                            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{43}')
                                              Instance Receiver:
                                                IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$"{43}"')
                                              Arguments(1):
                                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '43')
                                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 43) (Syntax: '43')
                                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Right:
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
""";

        VerifyOperationTreeAndDiagnosticsForTest<ExpressionStatementSyntax>(comp, expectedOperationTree, expectedDiagnostics: []);

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var mainDeclaration = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();

        var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(mainDeclaration.Body, model);
        ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C() { [ ... 43}"] = 1 }')
              Value:
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { [ ... 43}"] = 1 }')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1] [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (4)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '42')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$"{43}"')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, System.Int32 i)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$"{43}"')
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$"{43}"')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$"{43}"')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$"{43}"')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$"{43}"')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '42')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 42, IsImplicit) (Syntax: '42')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
                IInvocationOperation ( void CustomHandler.AppendFormatted(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{43}')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$"{43}"')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '43')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 43) (Syntax: '43')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[42, $"{43}"] = 1')
                  Left:
                    IPropertyReferenceOperation: System.Int32 CExt.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.this[System.Int32 i, CustomHandler h] { set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '[42, $"{43}"]')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { [ ... 43}"] = 1 }')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '42')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 42, IsImplicit) (Syntax: '42')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: h) (OperationKind.Argument, Type: null) (Syntax: '$"{43}"')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$"{43}"')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = new C() ... 3}"] = 1 };')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: '_ = new C() ... 43}"] = 1 }')
                  Left:
                    IDiscardOperation (Symbol: C _) (OperationKind.Discard, Type: C) (Syntax: '_')
                  Right:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { [ ... 43}"] = 1 }')
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
""", graph, symbol);
    }

    [Fact]
    public void InterpolationHandler_ObjectInitializer_02()
    {
        var code = """
public class Program
{
    public static void Main()
    {
        /*<bind>*/
        _ = new C() { [42, $"{43}"] = { Field = 0 } };
        /*</bind>*/
    }
}

public class C { }

public class D
{
    public int Field;
}

[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, int i)
    {
        System.Console.Write($"{i} ");
    }

    public void AppendFormatted(int i) 
    {
        System.Console.Write($"{i} ");
    }
}

public static class CExt
{
    extension(C c)
    {
        public D this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("i")] CustomHandler h]
        {
            get => new D();
        }
    }
}
""";

        var comp = CreateCompilation(code, targetFramework: TargetFramework.Net100, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("42 43"), verify: Verification.Skipped).VerifyDiagnostics();

        string expectedOperationTree = """
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = new C() ... ld = 0 } };')
  Expression:
    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: '_ = new C() ... eld = 0 } }')
      Left:
        IDiscardOperation (Symbol: C _) (OperationKind.Discard, Type: C) (Syntax: '_')
      Right:
        IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { [ ... eld = 0 } }')
          Arguments(0)
          Initializer:
            IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ [42, $"{4 ... eld = 0 } }')
              Initializers(1):
                  IMemberInitializerOperation (OperationKind.MemberInitializer, Type: D) (Syntax: '[42, $"{43} ... Field = 0 }')
                    InitializedMember:
                      IPropertyReferenceOperation: D CExt.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.this[System.Int32 i, CustomHandler h] { get; } (OperationKind.PropertyReference, Type: D) (Syntax: '[42, $"{43}"]')
                        Instance Receiver:
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '[42, $"{43}"]')
                        Arguments(2):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '42')
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: h) (OperationKind.Argument, Type: null) (Syntax: '$"{43}"')
                              IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler, IsImplicit) (Syntax: '$"{43}"')
                                Creation:
                                  IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, System.Int32 i)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$"{43}"')
                                    Arguments(3):
                                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$"{43}"')
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$"{43}"')
                                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$"{43}"')
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$"{43}"')
                                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '42')
                                          IInterpolatedStringHandlerArgumentPlaceholderOperation (ArgumentIndex: 0) (OperationKind.InterpolatedStringHandlerArgumentPlaceholder, Type: null, IsImplicit) (Syntax: '42')
                                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    Initializer:
                                      null
                                Content:
                                  IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"{43}"')
                                    Parts(1):
                                        IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{43}')
                                          AppendCall:
                                            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{43}')
                                              Instance Receiver:
                                                IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$"{43}"')
                                              Arguments(1):
                                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '43')
                                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 43) (Syntax: '43')
                                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Initializer:
                      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: D) (Syntax: '{ Field = 0 }')
                        Initializers(1):
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'Field = 0')
                              Left:
                                IFieldReferenceOperation: System.Int32 D.Field (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field')
                                  Instance Receiver:
                                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: D, IsImplicit) (Syntax: 'Field')
                              Right:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
""";

        VerifyOperationTreeAndDiagnosticsForTest<ExpressionStatementSyntax>(comp, expectedOperationTree, expectedDiagnostics: []);

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var mainDeclaration = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();

        var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(mainDeclaration.Body, model);
        ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C() { [ ... eld = 0 } }')
              Value:
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { [ ... eld = 0 } }')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1] [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (4)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '42')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$"{43}"')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, System.Int32 i)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$"{43}"')
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$"{43}"')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$"{43}"')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$"{43}"')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$"{43}"')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '42')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 42, IsImplicit) (Syntax: '42')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
                IInvocationOperation ( void CustomHandler.AppendFormatted(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{43}')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$"{43}"')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '43')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 43) (Syntax: '43')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'Field = 0')
                  Left:
                    IFieldReferenceOperation: System.Int32 D.Field (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field')
                      Instance Receiver:
                        IPropertyReferenceOperation: D CExt.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.this[System.Int32 i, CustomHandler h] { get; } (OperationKind.PropertyReference, Type: D) (Syntax: '[42, $"{43}"]')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { [ ... eld = 0 } }')
                          Arguments(2):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '42')
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 42, IsImplicit) (Syntax: '42')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: h) (OperationKind.Argument, Type: null) (Syntax: '$"{43}"')
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$"{43}"')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = new C() ... ld = 0 } };')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: '_ = new C() ... eld = 0 } }')
                  Left:
                    IDiscardOperation (Symbol: C _) (OperationKind.Discard, Type: C) (Syntax: '_')
                  Right:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { [ ... eld = 0 } }')
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
""", graph, symbol);
    }

}
