// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    /*TODO:
     * Any concerns regarding field keyword as attribute target vs field keyword in property accessors? I can't think of any.
     * Backcompat tests (field or variable in scope)
     * nameof(field) should work only when there is a symbol called "field" in scope.
     * nullability tests, ie, make sure we get null dereference warnings when needed.
     * 
    */
    public class PropertyFieldKeywordTests : CompilingTestBase
    {
        private void VerifyTypeIL(CSharpCompilation compilation, string typeName, string expected)
        {
            if (!ExecutionConditionUtil.IsDesktop)
            {
                // Hacky. We could otherwise run the tests only when IsDesktop is true, similar to what's done in CodeGenDisplayClassOptimizationTests
                expected = expected.Replace("[mscorlib]", "[netstandard]");
            }

            CompileAndVerify(compilation).VerifyTypeIL(typeName, expected);
        }

        [Fact]
        public void TestSimpleCase()
        {
            var comp = CreateCompilation(@"
public class C
{
    public string P { get; set => field = value; }
}
");
            VerifyTypeIL(comp, "C", @"
.class public auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	// Fields
	.field private string '<P>k__BackingField'
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Methods
	.method public hidebysig specialname 
		instance string get_P () cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x2050
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld string C::'<P>k__BackingField'
		IL_0006: ret
	} // end of method C::get_P
	.method public hidebysig specialname 
		instance void set_P (
			string 'value'
		) cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x2058
		// Code size 8 (0x8)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldarg.1
		IL_0002: stfld string C::'<P>k__BackingField'
		IL_0007: ret
	} // end of method C::set_P
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x2061
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method C::.ctor
	// Properties
	.property instance string P()
	{
		.get instance string C::get_P()
		.set instance void C::set_P(string)
	}
} // end of class C
");
        }

        [Fact(Skip = "PROTOTYPE(semi-auto-props): Initializer isn't added and no code is generated for it. This is because we use NonFieldBackingField when we add initializers. A cycle needs to be fixed.")]
        public void TestSemiAutoPropertyWithInitializer()
        {
            var comp = CreateCompilation(@"
public class C
{
    public string P { get => field; set => field = value; } = ""Hello"";
}
");
            VerifyTypeIL(comp, "C", @"
.class public auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	// Fields
	.field private string '<P>k__BackingField'
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Methods
	.method public hidebysig specialname 
		instance string get_P () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld string C::'<P>k__BackingField'
		IL_0006: ret
	} // end of method C::get_P
	.method public hidebysig specialname 
		instance void set_P (
			string 'value'
		) cil managed 
	{
		// Method begins at RVA 0x2058
		// Code size 8 (0x8)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldarg.1
		IL_0002: stfld string C::'<P>k__BackingField'
		IL_0007: ret
	} // end of method C::set_P
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x2061
		// Code size 18 (0x12)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldstr ""Hello""
		IL_0006: stfld string C::'<P>k__BackingField'
		IL_000b: ldarg.0
		IL_000c: call instance void [mscorlib]System.Object::.ctor()
		IL_0011: ret
	} // end of method C::.ctor
	// Properties
	.property instance string P()
	{
		.get instance string C::get_P()
		.set instance void C::set_P(string)
	}
} // end of class C
");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void FieldKeywordInReadOnlyProperty()
        {
            var comp = CreateCompilation(@"
public class C
{
    public string P { get => field; }
}
");
            VerifyTypeIL(comp, "C", @"
.class public auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	// Fields
	.field private initonly string '<P>k__BackingField'
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Methods
	.method public hidebysig specialname 
		instance string get_P () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld string C::'<P>k__BackingField'
		IL_0006: ret
	} // end of method C::get_P
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x2058
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method C::.ctor
	// Properties
	.property instance string P()
	{
		.get instance string C::get_P()
	}
} // end of class C
");
        }

        [Fact]
        public void TestPrefixedWithAt()
        {
            var comp = CreateCompilation(@"
public class C
{
    public string P { get; set => @field = value; }
}
");
            comp.VerifyDiagnostics(
                // (4,35): error CS0103: The name 'field' does not exist in the current context
                //     public string P { get; set => @field = value; }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "@field").WithArguments("field").WithLocation(4, 35));
        }

        [Fact]
        public void TestHasFieldMemberInScope()
        {
            var comp = CreateCompilation(@"
public class B
{
    protected string field;
}
public class C : B
{
    public string P { get => field; }
}
");
            VerifyTypeIL(comp, "C", @"
.class public auto ansi beforefieldinit C
	extends B
{
	// Methods
	.method public hidebysig specialname 
		instance string get_P () cil managed 
	{
		// Method begins at RVA 0x2058
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld string B::'field'
		IL_0006: ret
	} // end of method C::get_P
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x2060
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void B::.ctor()
		IL_0006: ret
	} // end of method C::.ctor
	// Properties
	.property instance string P()
	{
		.get instance string C::get_P()
	}
} // end of class C
");
        }

        [Fact]
        public void Test_ERR_AutoSetterCantBeReadOnly()
        {
            var comp = CreateCompilation(@"
public struct S
{
    public int P1 { get; readonly set; } // ERR_AutoSetterCantBeReadOnly
    public int P2 { get { return 0; } readonly set { } } // No ERR_AutoSetterCantBeReadOnly
    public int P3 { get => field; readonly set => field = value; } // No ERR_AutoSetterCantBeReadOnly, but ERR_AssgReadonlyLocal
    public int P4 { get; readonly set => field = value; } // No ERR_AutoSetterCantBeReadOnly, but ERR_AssgReadonlyLocal
}
");
            comp.VerifyDiagnostics(
                // (4,35): error CS8658: Auto-implemented 'set' accessor 'S.P1.set' cannot be marked 'readonly'.
                //     public int P1 { get; readonly set; } // ERR_AutoSetterCantBeReadOnly
                Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P1.set").WithLocation(4, 35),
                // (6,51): error CS1604: Cannot assign to 'field' because it is read-only
                //     public int P3 { get => field; readonly set => field = value; } // No ERR_AutoSetterCantBeReadOnly, but ERR_AssgReadonlyLocal
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(6, 51),

                // PROTOTYPE(semi-auto-props): Should not be reported.
                // (7,35): error CS8658: Auto-implemented 'set' accessor 'S.P4.set' cannot be marked 'readonly'.
                //     public int P4 { get; readonly set => field = value; } // No ERR_AutoSetterCantBeReadOnly, but ERR_AssgReadonlyLocal
                Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P4.set").WithLocation(7, 35),

                // (7,42): error CS1604: Cannot assign to 'field' because it is read-only
                //     public int P4 { get; readonly set => field = value; } // No ERR_AutoSetterCantBeReadOnly, but ERR_AssgReadonlyLocal
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(7, 42)
            );
        }

        [Fact]
        public void TestAccessingNonExistingFieldMember()
        {
            var comp = CreateCompilation(@"
public class C
{
}

public class C2
{
    public int P1 { get => C.field; }
    public int P2 { get => this.field; }
}
");
            comp.VerifyDiagnostics(
                // (8,30): error CS0117: 'C' does not contain a definition for 'field'
                //     public int P1 { get => C.field; }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "field").WithArguments("C", "field").WithLocation(8, 30),
                // (9,33): error CS1061: 'C2' does not contain a definition for 'field' and no accessible extension method 'field' accepting a first argument of type 'C2' could be found (are you missing a using directive or an assembly reference?)
                //     public int P2 { get => this.field; }
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "field").WithArguments("C2", "field").WithLocation(9, 33)
            );
        }

        [Fact]
        public void TestIndexer_LikeAutoProperty_ErrorCase()
        {
            var comp = CreateCompilation(@"
public class C
{
    public int this[int i]
    {
        get; set;
    }
}
");
            comp.VerifyDiagnostics(
                // (6,9): error CS0501: 'C.this[int].get' must declare a body because it is not marked abstract, extern, or partial
                //         get; set;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("C.this[int].get").WithLocation(6, 9),
                // (6,14): error CS0501: 'C.this[int].set' must declare a body because it is not marked abstract, extern, or partial
                //         get; set;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("C.this[int].set").WithLocation(6, 14)
            );
        }

        [Fact]
        public void TestIndexer_ContainsFieldIdentifier_Error()
        {
            var comp = CreateCompilation(@"
public class C
{
    public int this[int i]
    {
        get => field;
        set => _ = field;
    }
}
");
            comp.VerifyDiagnostics(
                // (6,16): error CS0103: The name 'field' does not exist in the current context
                //         get => field;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(6, 16),
                // (7,20): error CS0103: The name 'field' does not exist in the current context
                //         set => _ = field;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(7, 20)
            );
        }

        [Fact]
        public void TestIndexer_ContainsFieldIdentifier()
        {
            var comp = CreateCompilation(@"
public class C
{
    private int field;

    public int this[int i]
    {
        get => field;
        set => _ = field;
    }
}
");
            VerifyTypeIL(comp, "C", @"
.class public auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
		01 00 04 49 74 65 6d 00 00
	)
	// Fields
	.field private int32 'field'
	// Methods
	.method public hidebysig specialname 
		instance int32 get_Item (
			int32 i
		) cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld int32 C::'field'
		IL_0006: ret
	} // end of method C::get_Item
	.method public hidebysig specialname 
		instance void set_Item (
			int32 i,
			int32 'value'
		) cil managed 
	{
		// Method begins at RVA 0x2058
		// Code size 8 (0x8)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld int32 C::'field'
		IL_0006: pop
		IL_0007: ret
	} // end of method C::set_Item
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x2061
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method C::.ctor
	// Properties
	.property instance int32 Item(
		int32 i
	)
	{
		.get instance int32 C::get_Item(int32)
		.set instance void C::set_Item(int32, int32)
	}
} // end of class C
");
        }

        [Fact]
        public void TestIndexer()
        {
            var comp = CreateCompilation(@"
public class C
{
    public int this[int i]
    {
        get => i;
        set => _ = i;
    }
}
", options: TestOptions.ReleaseDll);
            VerifyTypeIL(comp, "C", @"

.class public auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
		01 00 04 49 74 65 6d 00 00
	)
	// Methods
	.method public hidebysig specialname 
		instance int32 get_Item (
			int32 i
		) cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 2 (0x2)
		.maxstack 8
		IL_0000: ldarg.1
		IL_0001: ret
	} // end of method C::get_Item
	.method public hidebysig specialname 
		instance void set_Item (
			int32 i,
			int32 'value'
		) cil managed 
	{
		// Method begins at RVA 0x2053
		// Code size 1 (0x1)
		.maxstack 8
		IL_0000: ret
	} // end of method C::set_Item
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x2055
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method C::.ctor
	// Properties
	.property instance int32 Item(
		int32 i
	)
	{
		.get instance int32 C::get_Item(int32)
		.set instance void C::set_Item(int32, int32)
	}
} // end of class C
");
        }

        [Theory]
        [InlineData("get => field;")]
        [InlineData("get { return field; }")]
        public void TestSemanticModel_GetterOnly(string accessor)
        {
            var comp = CreateCompilation($@"
public class C
{{
    public int P1 {{ {accessor} }}
}}
");
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var fieldToken = tree.GetRoot().DescendantTokens().Where(t => t.ContextualKind() == SyntaxKind.FieldKeyword).Single();
            AssertEx.NotNull(fieldToken.Parent);
            var info = model.GetSymbolInfo(fieldToken.Parent);
            Assert.Empty(info.CandidateSymbols);
            Assert.False(info.IsEmpty);
            Assert.Equal("<P1>k__BackingField", info.Symbol!.Name);
        }

        [Theory]
        [InlineData("get => field; set => field = value;")]
        [InlineData("set => field = value; get => field;")]
        public void TestSemanticModel_GetterAndSetter_BothAreExpressionBodied(string accessor)
        {
            var comp = CreateCompilation($@"
public class C
{{
    public int P1 {{ {accessor} }}
}}
");
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var fieldTokens = tree.GetRoot().DescendantTokens().Where(t => t.ContextualKind() == SyntaxKind.FieldKeyword).ToArray();
            Assert.Equal(2, fieldTokens.Length);
            foreach (var fieldToken in fieldTokens)
            {
                AssertEx.NotNull(fieldToken.Parent);
                var info = model.GetSymbolInfo(fieldToken.Parent);
                Assert.Empty(info.CandidateSymbols);
                Assert.False(info.IsEmpty);
                Assert.Equal("<P1>k__BackingField", info.Symbol!.Name);
            }
        }

        [Theory]
        [InlineData("get => field; set { field = value; }")]
        [InlineData("get { return field; } set => field = value;")]
        [InlineData("set { field = value; } get => field;")]
        [InlineData("set => field = value; get { return field; }")]
        public void TestSemanticModel_GetterAndSetter_MixBodyAndExpressionBoded(string accessor)
        {
            var comp = CreateCompilation($@"
public class C
{{
    public int P1 {{ {accessor} }}
}}
");
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var fieldTokens = tree.GetRoot().DescendantTokens().Where(t => t.ContextualKind() == SyntaxKind.FieldKeyword).ToArray();
            Assert.Equal(2, fieldTokens.Length);
            foreach (var fieldToken in fieldTokens)
            {
                AssertEx.NotNull(fieldToken.Parent);
                var info = model.GetSymbolInfo(fieldToken.Parent);
                Assert.Empty(info.CandidateSymbols);
                Assert.False(info.IsEmpty);
                Assert.Equal("<P1>k__BackingField", info.Symbol!.Name);
            }
        }

        [Theory]
        [InlineData("get { return field; } set => _ = value;")] // body getter
        [InlineData("set => _ = value; get { return field; }")] // body getter
        [InlineData("get => field; set { _ = value; }")] // expression bodied getter
        [InlineData("set { _ = value; } get => field;")] // expression bodied getter
        [InlineData("get => 0; set { field = value; }")] // body setter
        [InlineData("set { field = value; } get => 0;")] // body setter
        [InlineData("get { return 0; } set => field = value;")] // expression bodied setter
        [InlineData("set => field = value; get { return 0; }")] // expression bodied setter
        public void TestSemanticModel_GetterAndSetter_OnlyOneAccessorUsesKeyword(string accessor)
        {
            var comp = CreateCompilation($@"
public class C
{{
    public int P1 {{ {accessor} }}
}}
");
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var fieldToken = tree.GetRoot().DescendantTokens().Where(t => t.ContextualKind() == SyntaxKind.FieldKeyword).Single();
            AssertEx.NotNull(fieldToken.Parent);
            var info = model.GetSymbolInfo(fieldToken.Parent);
            Assert.Empty(info.CandidateSymbols);
            Assert.False(info.IsEmpty);
            Assert.Equal("<P1>k__BackingField", info.Symbol!.Name);
        }

        [Fact]
        public void TestGetFieldsToEmit()
        {
            var comp = CreateCompilation(@"
public class C
{
    public string P { set => field = value; }
}
");
            for (int i = 0; i < 3; i++)
            {
                var fields = ((SourceMemberContainerTypeSymbol)comp.GetTypeByMetadataName("C")!).GetFieldsToEmit().ToArray();
                Assert.Equal(1, fields.Length);
                Assert.Equal("<P>k__BackingField", fields[0].Name);
            }
        }

        [Fact(Skip = "PROTOTYPE(semi-auto-props): Cycle..")]
        public void AssignReadOnlyOnlyPropertyOutsideConstructor()
        {
            var comp = CreateCompilation(@"
class Test
{
    int X
    {
        get
        {
            field = 3;
            X = 3;
            return 0;
        }
    }
}
");
            comp.VerifyDiagnostics(
                // (9,13): error CS0200: Property or indexer 'Test.X' cannot be assigned to -- it is read only
                //             X = 3;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "X").WithArguments("Test.X").WithLocation(9, 13)
            );
        }

        [Fact]
        public void AssignReadOnlyOnlyPropertyInConstructor()
        {
            var comp = CreateCompilation(@"
using System;

_ = new Test();

class Test
{
    public Test()
    {
        X = 3;
        Console.WriteLine(X);
    }

    int X
    {
        get { return field; }
    }
}
");
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "3");
            VerifyTypeIL(comp, "Test", @"
.class private auto ansi beforefieldinit Test
    extends [mscorlib]System.Object
{
    // Fields
    .field private initonly int32 '<X>k__BackingField'
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    	01 00 00 00
    )
    // Methods
    .method public hidebysig specialname rtspecialname 
    	instance void .ctor () cil managed 
    {
    	// Method begins at RVA 0x2060
    	// Code size 25 (0x19)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: call instance void [mscorlib]System.Object::.ctor()
    	IL_0006: ldarg.0
    	IL_0007: ldc.i4.3
    	IL_0008: stfld int32 Test::'<X>k__BackingField'
    	IL_000d: ldarg.0
    	IL_000e: call instance int32 Test::get_X()
    	IL_0013: call void [mscorlib]System.Console::WriteLine(int32)
    	IL_0018: ret
    } // end of method Test::.ctor
    .method private hidebysig specialname 
    	instance int32 get_X () cil managed 
    {
    	// Method begins at RVA 0x207a
    	// Code size 7 (0x7)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldfld int32 Test::'<X>k__BackingField'
    	IL_0006: ret
    } // end of method Test::get_X
    // Properties
    .property instance int32 X()
    {
    	.get instance int32 Test::get_X()
    }
} // end of class Test
");

        }
    }
}
