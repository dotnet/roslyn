// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using System.Collections.Generic;
using System.Linq;
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
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("get(^1) set(^2, 10)"), verify: Verification.Skipped).VerifyDiagnostics();
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
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("get(^1) set(^2, 10)"), verify: Verification.Skipped).VerifyDiagnostics();
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
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("get(^1) set(^2, 10)"), verify: Verification.Skipped).VerifyDiagnostics();
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
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("get(0..^1) set(0..^1, 10)"), verify: Verification.Skipped).VerifyDiagnostics();
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
        var verifier = CompileAndVerify(comp, expectedOutput: "^1").VerifyDiagnostics();
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
        var verifier = CompileAndVerify(comp, expectedOutput: "False ^1 False").VerifyDiagnostics();
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
        var verifier = CompileAndVerify(comp, expectedOutput: "1..^0").VerifyDiagnostics();
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

    [Fact(Skip = "PROTOTYPE nullability")]
    public void Nullability_Indexing_01()
    {
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

    [Fact(Skip = "PROTOTYPE nullability")]
    public void Nullability_Indexing_02()
    {
        // string indexer
        string source = """
#nullable enable

string? o = null;
_ = o[0];
""";
        var comp = CreateCompilation(source);
        CompileAndVerify(comp, expectedOutput: "True").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ElementAccessExpressionSyntax>(tree, "o[0]");
        AssertEx.Equal("PROTOTYPE", model.GetSymbolInfo(memberAccess).Symbol.ToDisplayString());
    }

    [Fact(Skip = "PROTOTYPE nullability")]
    public void Nullability_Indexing_03()
    {
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

    [Fact(Skip = "PROTOTYPE nullability")]
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

    [Fact(Skip = "PROTOTYPE nullability")]
    public void Nullability_Indexing_05()
    {
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

    [Fact(Skip = "PROTOTYPE nullability")]
    public void Nullability_Indexing_06()
    {
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

    [Fact(Skip = "PROTOTYPE nullability")]
    public void Nullability_ObjectInitializer_01()
    {
        string source = """
#nullable enable

_ = new object() { [null] = 1 };

static class E
{
    extension<T>(T t)
    {
        public int this[T t2] { set { System.Console.Write(t2 is null); } }
    }
}
""";
        var comp = CreateCompilation(source);
        CompileAndVerify(comp, expectedOutput: "True").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ImplicitElementAccessSyntax>(tree, "[null]");
        AssertEx.Equal("PROTOTYPE", model.GetSymbolInfo(memberAccess).Symbol.ToDisplayString());
    }

    [Fact(Skip = "PROTOTYPE nullability")]
    public void Nullability_ObjectInitializer_02()
    {
        string source = """
#nullable enable

_ = new object() { [t2: (object?)null, t1: null] = 1 };

static class E
{
    extension<T>(object o)
    {
        public int this[T t1, T t2] { set { System.Console.Write((t1 is null, t2 is null)); } }
    }
}
""";
        var comp = CreateCompilation(source);
        CompileAndVerify(comp, expectedOutput: "(True, True)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<ImplicitElementAccessSyntax>(tree, "[t2: (object?)null, t1: null]");
        AssertEx.Equal("PROTOTYPE", model.GetSymbolInfo(memberAccess).Symbol.ToDisplayString());
    }

    [Fact]
    public void Cref_01()
    {
        var src = """
/// <see cref="E.extension(int).this[string]"/>
/// <see cref="E.extension(int).Item(string)"/>
/// <see cref="E.extension(int).get_Item(string)"/>
/// <see cref="E.extension(int).get_Item"/>
/// <see cref="E.extension(int).set_Item(string, int)"/>
/// <see cref="E.extension(int).set_Item"/>
/// <see cref="E.get_Item(int, string)"/>
/// <see cref="E.get_Item"/>
/// <see cref="E.set_Item(int, string, int)"/>
/// <see cref="E.set_Item"/>
static class E
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
        // PROTOTYPE cref, we should bind `this[string]`
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(int).this[string]' that could not be resolved
            // /// <see cref="E.extension(int).this[string]"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int).this[string]").WithArguments("extension(int).this[string]").WithLocation(1, 16),
            // (2,16): warning CS1574: XML comment has cref attribute 'extension(int).Item(string)' that could not be resolved
            // /// <see cref="E.extension(int).Item(string)"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int).Item(string)").WithArguments("extension(int).Item(string)").WithLocation(2, 16));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension(int).this[string], null)",
            "(E.extension(int).Item(string), null)",
            "(E.extension(int).get_Item(string), System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].get)",
            "(E.extension(int).get_Item, System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].get)",
            "(E.extension(int).set_Item(string, int), void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].set)",
            "(E.extension(int).set_Item, void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].set)",
            "(E.get_Item(int, string), System.Int32 E.get_Item(System.Int32 i, System.String s))",
            "(E.get_Item, System.Int32 E.get_Item(System.Int32 i, System.String s))",
            "(E.set_Item(int, string, int), void E.set_Item(System.Int32 i, System.String s, System.Int32 value))",
            "(E.set_Item, void E.set_Item(System.Int32 i, System.String s, System.Int32 value))"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_02()
    {
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
/// <see cref="E.extension(int).Item(string)"/>
/// <see cref="E.extension(int).get_Item(string)"/>
/// <see cref="E.extension(int).get_Item"/>
/// <see cref="E.extension(int).set_Item(string, int)"/>
/// <see cref="E.extension(int).set_Item"/>
/// <see cref="E.get_Item(int, string)"/>
/// <see cref="E.get_Item"/>
/// <see cref="E.set_Item(int, string, int)"/>
/// <see cref="E.set_Item"/>
class C { }
""";
        // PROTOTYPE cref, we should bind `this[string]` and produce LangVer diagnostics
        var comp = CreateCompilation(src, references: [libComp.EmitToImageReference()], parseOptions: TestOptions.Regular14.WithDocumentationMode(DocumentationMode.Diagnose));
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(int).this[string]' that could not be resolved
            // /// <see cref="E.extension(int).this[string]"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int).this[string]").WithArguments("extension(int).this[string]").WithLocation(1, 16),
            // (2,16): warning CS1574: XML comment has cref attribute 'extension(int).Item(string)' that could not be resolved
            // /// <see cref="E.extension(int).Item(string)"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int).Item(string)").WithArguments("extension(int).Item(string)").WithLocation(2, 16));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension(int).this[string], null)",
            "(E.extension(int).Item(string), null)",
            "(E.extension(int).get_Item(string), System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].get)",
            "(E.extension(int).get_Item, System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].get)",
            "(E.extension(int).set_Item(string, int), void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].set)",
            "(E.extension(int).set_Item, void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].set)",
            "(E.get_Item(int, string), System.Int32 E.get_Item(System.Int32 i, System.String s))",
            "(E.get_Item, System.Int32 E.get_Item(System.Int32 i, System.String s))",
            "(E.set_Item(int, string, int), void E.set_Item(System.Int32 i, System.String s, System.Int32 value))",
            "(E.set_Item, void E.set_Item(System.Int32 i, System.String s, System.Int32 value))"],
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
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("[0]=C"), verify: Verification.Skipped).VerifyDiagnostics();
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
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("<Main>$"), verify: Verification.Skipped).VerifyDiagnostics();
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
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("Item"), verify: Verification.Skipped).VerifyDiagnostics();
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
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("MyIndexer"), verify: Verification.Skipped).VerifyDiagnostics();
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
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("<Main>$"), verify: Verification.Skipped).VerifyDiagnostics();
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
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("42"), verify: Verification.Skipped).VerifyDiagnostics();
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
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("Cannot apply indexing with [] to an expression of type 'int'"), verify: Verification.Skipped)
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
        CompileAndVerify(comp, expectedOutput: "4243", verify: Verification.Skipped).VerifyDiagnostics();
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
}
