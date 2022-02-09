// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    /* PROTOTYPE(semi-auto-props):
     * Add tests for field attribute target, specially for setter-only
     * nameof(field) should work only when there is a symbol called "field" in scope.
     * nullability tests, ie, make sure we get null dereference warnings when needed.
     */

    /* PROTOTYPE(semi-auto-props):
     * Regarding AbstractFlowPass and DefiniteAssignment, it looks like the logic there requires an adjustment since the behavior of IsAutoPropertyWithGetAccessor has changed.
     * We can no longer guarantee that accessing the property under the checked conditions is always equivalent to accessing a backing field.
     * For example, if the corresponding accessor has a body, we don't really know what it is doing.
     * Also, it looks like usages of AbstractFlowPass.RegularPropertyAccess helper should be reviewed and appropriate adjustments should be made according to it's behavior change.
     */

    // PROTOTYPE(semi-auto-props): All tests should verify that we do not synthesize fields unexpectedly.
    // Success cases can do this by observing emitted metadata.
    // Note that private symbols are filtered out by default, either type IL should be checked, or
    // compilation options should be adjusted to disable the filtering.
    // Error cases should check GetFieldToEmit after checking diagnostics, but before checking NumberOfPerformedAccessorBinding.

    // PROTOTYPE(semi-auto-props): Need to add tests confirming that SemanticModel doesn't bind extra accessors that we ignored for the purpose of syntactic check.

    // PROTOTYPE(semi-auto-props): Need to add tests for when a property accessor have
    // both expression body and block body. We should confirm that SemanticModel doesn't bind an expression body in presence of a block body.

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

        [Fact(Skip = "PROTOTYPE(semi-auto-props): Assigning in constructor is not yet supported.")]
        public void TestFieldOnlyGetter()
        {
            var comp = CreateCompilation(@"
public class C
{
    public C()
    {
        P = 5;
    }

    public int P { get => field; }

    public static void Main()
    {
        System.Console.WriteLine(new C().P);
    }
}
", options: TestOptions.DebugExe);
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());

            CompileAndVerify(comp, expectedOutput: "5");
            VerifyTypeIL(comp, "C", @"
    .class public auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	// Fields
	.field private initonly int32 '<P>k__BackingField'
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	.custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = (
		01 00 00 00 00 00 00 00
	)
	// Methods
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 16 (0x10)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: nop
		IL_0007: nop
		IL_0008: ldarg.0
		IL_0009: ldc.i4.5
		IL_000a: stfld int32 C::'<P>k__BackingField'
		IL_000f: ret
	} // end of method C::.ctor
	.method public hidebysig specialname 
		instance int32 get_P () cil managed 
	{
		// Method begins at RVA 0x2061
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld int32 C::'<P>k__BackingField'
		IL_0006: ret
	} // end of method C::get_P
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2069
		// Code size 18 (0x12)
		.maxstack 8
		.entrypoint
		IL_0000: nop
		IL_0001: newobj instance void C::.ctor()
		IL_0006: call instance int32 C::get_P()
		IL_000b: call void [mscorlib]System.Console::WriteLine(int32)
		IL_0010: nop
		IL_0011: ret
	} // end of method C::Main
	// Properties
	.property instance int32 P()
	{
		.get instance int32 C::get_P()
	}
} // end of class C
");
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        // PROTOTYPE(semi-auto-props): All success scenarios should be executed, expected runtime behavior should be observed.
        // This is waiting until we support assigning to readonly properties in constructor.
        [Fact]
        public void TestExpressionBodiedProperty()
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P => field;
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            VerifyTypeIL(comp, "C", @"
.class public auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	// Fields
	.field private initonly int32 '<P>k__BackingField'
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Methods
	.method public hidebysig specialname 
		instance int32 get_P () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld int32 C::'<P>k__BackingField'
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
	.property instance int32 P()
	{
		.get instance int32 C::get_P()
	}
} // end of class C
");
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact(Skip = "PROTOTYPE(semi-auto-props): Mixing semicolon-only with field not yet working.")]
        public void TestSimpleCase()
        {
            var comp = CreateCompilation(@"
public class C
{
    public string P { get; set => field = value; }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            // PROTOTYPE(semi-auto-props): Should be empty or non-empty? Current behavior is unknown since mixed scenarios not yet supported.
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
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
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact(Skip = "PROTOTYPE(semi-auto-props): Produces error CS8050: Only auto-implemented properties can have initializers.")]
        public void TestSemiAutoPropertyWithInitializer()
        {
            var comp = CreateCompilation(@"
public class C
{
    public string P { get => field; set => field = value; } = ""Hello"";
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
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
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
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
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact(Skip = "PROTOTYPE(semi-auto-props): Mixed scenarios are not yet supported.")]
        public void TestPrefixedWithAt()
        {
            var comp = CreateCompilation(@"
public class C
{
    public string P { get; set => @field = value; }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(
                // (4,35): error CS0103: The name 'field' does not exist in the current context
                //     public string P { get; set => @field = value; }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "@field").WithArguments("field").WithLocation(4, 35));
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestPrefixedWithAt2()
        {
            var comp = CreateCompilation(@"
public class C
{
    public string P { get => @field; }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(
                // (4,30): error CS0103: The name 'field' does not exist in the current context
                //     public string P { get => @field; }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "@field").WithArguments("field").WithLocation(4, 30));
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
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
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact(Skip = "PROTOTYPE(semi-auto-props): Currently has extra diagnostics")]
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(
                // (4,35): error CS8658: Auto-implemented 'set' accessor 'S.P1.set' cannot be marked 'readonly'.
                //     public int P1 { get; readonly set; } // ERR_AutoSetterCantBeReadOnly
                Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P1.set").WithLocation(4, 35),
                // (6,51): error CS1604: Cannot assign to 'field' because it is read-only
                //     public int P3 { get => field; readonly set => field = value; } // No ERR_AutoSetterCantBeReadOnly, but ERR_AssgReadonlyLocal
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(6, 51),
                // (7,42): error CS1604: Cannot assign to 'field' because it is read-only
                //     public int P4 { get; readonly set => field = value; } // No ERR_AutoSetterCantBeReadOnly, but ERR_AssgReadonlyLocal
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(7, 42)
            );
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C2").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(
                // (8,30): error CS0117: 'C' does not contain a definition for 'field'
                //     public int P1 { get => C.field; }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "field").WithArguments("C", "field").WithLocation(8, 30),
                // (9,33): error CS1061: 'C2' does not contain a definition for 'field' and no accessible extension method 'field' accepting a first argument of type 'C2' could be found (are you missing a using directive or an assembly reference?)
                //     public int P2 { get => this.field; }
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "field").WithArguments("C2", "field").WithLocation(9, 33)
            );
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.VerifyDiagnostics(
                // (6,9): error CS0501: 'C.this[int].get' must declare a body because it is not marked abstract, extern, or partial
                //         get; set;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("C.this[int].get").WithLocation(6, 9),
                // (6,14): error CS0501: 'C.this[int].set' must declare a body because it is not marked abstract, extern, or partial
                //         get; set;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("C.this[int].set").WithLocation(6, 14)
            );

            var property = comp.GetTypeByMetadataName("C").GetMembers().OfType<SourcePropertySymbolBase>().Single();
            Assert.Null(property.BackingField);
            Assert.Null(property.FieldKeywordBackingField);

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.VerifyDiagnostics(
                // (6,16): error CS0103: The name 'field' does not exist in the current context
                //         get => field;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(6, 16),
                // (7,20): error CS0103: The name 'field' does not exist in the current context
                //         set => _ = field;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(7, 20)
            );

            var property = comp.GetTypeByMetadataName("C").GetMembers().OfType<SourcePropertySymbolBase>().Single();
            Assert.Null(property.BackingField);
            Assert.Null(property.FieldKeywordBackingField);

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
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
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
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
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        // PROTOTYPE(semi-auto-props): Similar test for when we have an explicit field named `field`, and also for ignored (extra) accessors.
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var fieldToken = tree.GetRoot().DescendantTokens().Where(t => t.ContextualKind() == SyntaxKind.FieldKeyword).Single();
            AssertEx.NotNull(fieldToken.Parent);
            var info = model.GetSymbolInfo(fieldToken.Parent);
            Assert.Empty(info.CandidateSymbols);
            Assert.False(info.IsEmpty);
            Assert.Equal("<P1>k__BackingField", info.Symbol.Name);
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
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
                Assert.Equal("<P1>k__BackingField", info.Symbol.Name);
            }

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
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
                Assert.Equal("<P1>k__BackingField", info.Symbol.Name);
            }
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var fieldToken = tree.GetRoot().DescendantTokens().Where(t => t.ContextualKind() == SyntaxKind.FieldKeyword).Single();
            AssertEx.NotNull(fieldToken.Parent);
            var info = model.GetSymbolInfo(fieldToken.Parent);
            Assert.Empty(info.CandidateSymbols);
            Assert.False(info.IsEmpty);
            Assert.Equal("<P1>k__BackingField", info.Symbol.Name);
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            for (int i = 0; i < 3; i++)
            {
                var fields = ((SourceMemberContainerTypeSymbol)comp.GetTypeByMetadataName("C")!).GetFieldsToEmit().ToArray();
                Assert.Equal(1, fields.Length);
                Assert.Equal("<P>k__BackingField", fields[0].Name);
            }
            Assert.Equal(1, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestGetFieldsToEmit_ExpressionBodied()
        {
            var comp = CreateCompilation(@"
public class C
{
    public string P => field;
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            for (int i = 0; i < 3; i++)
            {
                var fields = ((SourceMemberContainerTypeSymbol)comp.GetTypeByMetadataName("C")!).GetFieldsToEmit().ToArray();
                Assert.Equal(1, fields.Length);
                Assert.Equal("<P>k__BackingField", fields[0].Name);
            }
            Assert.Equal(1, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestGetFieldsToEmit_Initializer()
        {
            var comp = CreateCompilation(@"
public class C
{
    public string P { get => field; } = string.Empty;
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            for (int i = 0; i < 3; i++)
            {
                var fields = ((SourceMemberContainerTypeSymbol)comp.GetTypeByMetadataName("C")!).GetFieldsToEmit().ToArray();
                Assert.Equal(1, fields.Length);
                Assert.Equal("<P>k__BackingField", fields[0].Name);
            }
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("Test").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(
                // PROTOTYPE(semi-auto-props): From review,
                // This error doesn't make sense to me. I understand that the spec requires this field to be read-only, but I don't think this restriction is justified.
                // (8,13): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //             field = 3;
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(8, 13),
                // (9,13): error CS0200: Property or indexer 'Test.X' cannot be assigned to -- it is read only
                //             X = 3;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "X").WithArguments("Test.X").WithLocation(9, 13)
            );
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact(Skip = "PROTOTYPE(semi-auto-props): Assigning in constructor is not yet supported.")]
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("Test").GetMembers().OfType<FieldSymbol>());
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
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void SetOnlyAutoProperty()
        {
            var comp = CreateCompilation(@"
class C
{
    public int P1 { set; }
    public int P2 { set => field = value; }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(
                // (4,21): error CS8051: Auto-implemented properties must have get accessors.
                //     public int P1 { set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, "set").WithArguments("C.P1.set").WithLocation(4, 21)
            );
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact(Skip = "PROTOTYPE(semi-auto-props)")]
        public void InStaticLambda()
        {
            var comp = CreateCompilation(@"
using System;

public class C
{
    public int P
    {
        get
        {
            Func<int> f = static () => field;
            return 0;
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(
                // (10,40): error CS8821: A static anonymous function cannot contain a reference to 'this' or 'base'.
                //             Func<int> f = static () => field;
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureThis, "field").WithLocation(10, 40)
            );
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void Test_ERR_FieldAutoPropCantBeByRefLike()
        {
            var comp = CreateCompilationWithSpan(@"
using System;

public struct S1
{
    public Span<int> P { get => field; }
}

public ref struct S2
{
    public Span<int> P { get => field; }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("S1").GetMembers().OfType<FieldSymbol>());
            Assert.Empty(comp.GetTypeByMetadataName("S2").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(
            // PROTOTYPE(semi-auto-props): This should have ERR_FieldAutoPropCantBeByRefLike
            );
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void Test_ReadOnlyPropertyInStruct()
        {
            var comp = CreateCompilation(@"
public struct S
{
    public readonly string P { set => field = value; }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("S").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(
                // (4,39): error CS1604: Cannot assign to 'field' because it is read-only
                //     public readonly string P { set => field = value; }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(4, 39)
            );
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void Test_ERR_AutoPropertyWithSetterCantBeReadOnly()
        {
            var comp = CreateCompilation(@"
public readonly struct S
{
    public string P { set => field = value; }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("S").GetMembers().OfType<FieldSymbol>());
            // PROTOTYPE(semi-auto-props): An equivalent scenario with explicitly declared field produces a different error:
            // error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
            // Need to confirm why these behave differently and if that's acceptable.
            comp.VerifyDiagnostics(
                // (4,30): error CS1604: Cannot assign to 'field' because it is read-only
                //     public string P { set => field = value; }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(4, 30)
            );
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void Test_NoExtraBindingOccurred()
        {
            var comp = CreateCompilation(@"
public class Point
{
    public int X { get { return field; } set { field = value; } }
    public int Y { get { return field; } set { field = value; } }
}
");
            var data = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = data;
            Assert.Empty(comp.GetTypeByMetadataName("Point").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();
            Assert.Equal(0, data.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void Test_BindingOccurs()
        {
            var comp = CreateCompilation(@"
public class Point
{
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get { return field; } set { field = value; } }
    public int Y { get { return field; } set { field = value; } }
}
");
            var data = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = data;
            Assert.Empty(comp.GetTypeByMetadataName("Point").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();
            Assert.Equal(0, data.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void Test_ContainsFieldKeywordAPI()
        {
            var comp = CreateCompilation(@"
public class C1
{
    public int this[int i]
    {
        get => field;
        set => _ = field;
    }
}

public class C2
{
    public int this[int i] => field;
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var accessorsC1 = comp.GetTypeByMetadataName("C1").GetMembers().OfType<SourcePropertyAccessorSymbol>().ToArray();
            Assert.Equal(2, accessorsC1.Length);
            Assert.False(accessorsC1[0].ContainsFieldKeyword);
            Assert.False(accessorsC1[1].ContainsFieldKeyword);

            var accessorsC2 = comp.GetTypeByMetadataName("C2").GetMembers().OfType<SourcePropertyAccessorSymbol>().ToArray();
            Assert.Equal(1, accessorsC2.Length);
            Assert.False(accessorsC2[0].ContainsFieldKeyword);

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestUnassignedNonNullable()
        {
            var comp = CreateCompilation(@"
#nullable enable

public class C1
{
    public string P1 { get => field; }
    // public string P2 { get => field; } = string.Empty // PROTOTYPE(semi-auto-props): Uncomment when initializers are supported.
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C1").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(); // PROTOTYPE(semi-auto-props): Is this the correct behavior?
            // PROTOTYPE(semi-auto-props): If we're going to have a diagnostic that P1 must be non-null when exiting constructor,
            // then we need another test in constructor like:
            /*
                try
                {
                    x = "";
                }
                catch (Exception)
                {
                    x ??= ""; // No warning, but should have warning if the line is removed.
                }
             */

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        // PROTOTYPE(semi-auto-props): Add more tests related to MethodCompiler._filterOpt
        // - Different syntax trees of a partial file.
        // - Getting the diagnostic for one accessor that doesn't contain field while the other accessor contains field.
        [Fact]
        public void CompileMethods_1()
        {
            var comp = CreateCompilation(@"
public class C1
{
    public int P1 { get => field; }
    public int P2 { get => field; }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C1").GetMembers().OfType<FieldSymbol>());
            var tree = comp.SyntaxTrees[0];

            // Force compiling P1 but not P2
            _ = comp.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, tree, tree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>().First().Span, includeEarlierStages: true);

            comp.VerifyDiagnostics();

            var properties = comp.GetTypeByMetadataName("C1").GetMembers().OfType<SourcePropertySymbolBase>().ToArray();
            Assert.Equal(2, properties.Length);
            Assert.NotNull(properties[0].FieldKeywordBackingField);
            Assert.NotNull(properties[1].FieldKeywordBackingField);

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void InStruct()
        {
            var comp = CreateCompilation(@"
struct S_WithAutoProperty
{
    public int P { get; set; }
}

struct S_WithManualProperty
{
    public int P { get => 0; set => _ = value; }
}

struct S_WithSemiAutoProperty
{
    public int P { get => field; set => field = value; }
}

class C
{
    void M1()
    {
        S_WithAutoProperty s1;
        S_WithAutoProperty s2 = s1;
    }

    void M2()
    {
        S_WithManualProperty s1;
        S_WithManualProperty s2 = s1;
    }

    void M3()
    {
        S_WithSemiAutoProperty s1;
        S_WithSemiAutoProperty s2 = s1;
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Equal("System.Int32 S_WithAutoProperty.<P>k__BackingField", comp.GetTypeByMetadataName("S_WithAutoProperty").GetMembers().OfType<FieldSymbol>().Single().ToTestDisplayString());
            Assert.Empty(comp.GetTypeByMetadataName("S_WithManualProperty").GetMembers().OfType<FieldSymbol>());
            Assert.Empty(comp.GetTypeByMetadataName("S_WithSemiAutoProperty").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(
                // PROTOTYPE(semi-auto-props): Do we expect a similar error for semi auto props?
                // (22,33): error CS0165: Use of unassigned local variable 's1'
                //         S_WithAutoProperty s2 = s1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s1").WithArguments("s1").WithLocation(22, 33)
            );

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void InStruct_02()
        {
            var comp = CreateCompilation(@"
struct S
{
    public int P { get => field; set => field = value; }

    public S(int arg)
    {
        P = 0;
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("S").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void InStruct_03()
        {
            var comp = CreateCompilation(@"
struct S
{
    public int P { get; set; }

    public S(int arg)
    {
        P = 0;
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Equal("System.Int32 S.<P>k__BackingField", comp.GetTypeByMetadataName("S").GetMembers().OfType<FieldSymbol>().Single().ToTestDisplayString());
            comp.VerifyDiagnostics();

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void SpeculativeSemanticModel_FieldInRegularAccessor_BindOriginalFirst()
        {
            var comp = CreateCompilation(@"
class C
{
    public int P { get => 0; }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);
            // PROTOTYPE(semi-auto-props): Should this return a null or non-null?
            Assert.Null(fieldKeywordSymbolInfo.Symbol);
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
        }

        [Fact]
        public void SpeculativeSemanticModel_FieldInRegularAccessor_BindSpeculatedFirst()
        {
            var comp = CreateCompilation(@"
class C
{
    public int P { get => 0; }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);

            // PROTOTYPE(semi-auto-props): Should `GetFieldsToEmit` return empty? Currently it looks like it mutates the original symbol in a bad way.
            Assert.Equal("System.Int32 C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());
            Assert.Equal("System.Int32 C.<P>k__BackingField", fieldKeywordSymbolInfo.Symbol.ToTestDisplayString(includeNonNullable: true));
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void SpeculativeSemanticModel_FieldInAccessorUsingField_BindOriginalFirst()
        {
            var comp = CreateCompilation(@"
using System;

class C
{
    public double P
    {
        get
        {
            if (field == Math.PI)
               return 3.14;
            return field;
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);
            Assert.Equal("System.Double C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);
            Assert.Equal("System.Double C.<P>k__BackingField", fieldKeywordSymbolInfo.Symbol.ToTestDisplayString(includeNonNullable: true));
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
            Assert.Equal("System.Double C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());
        }

        [Fact]
        public void SpeculativeSemanticModel_FieldInAccessorUsingField_BindSpeculatedFirst()
        {
            var comp = CreateCompilation(@"
using System;

class C
{
    public double P
    {
        get
        {
            if (field == Math.PI)
               return 3.14;
            return field;
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);

            Assert.Equal("System.Double C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());
            Assert.Equal("System.Double C.<P>k__BackingField", fieldKeywordSymbolInfo.Symbol.ToTestDisplayString(includeNonNullable: true));
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData("get => 0; set => field = value;")]
        [InlineData("set => field = value; get => 0;")]
        [InlineData("get { return 0; } set { field = value; }")]
        [InlineData("set { field = value; } get { return 0; }")]
        [InlineData("get => 0; set { field = value; }")]
        [InlineData("set { field = value; } get => 0;")]
        [InlineData("get { return 0; } set => field = value;")]
        [InlineData("set => field = value; get { return 0; }")]
        public void SpeculativeSemanticModel_FieldInGetterNotUsingFieldWhereSetterAccessorDoes_BindOriginalFirst(string accessors)
        {
            var comp = CreateCompilation($@"
class C
{{
    public double P
    {{
        {accessors}
    }}
}}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);
            Assert.Equal("System.Double C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);
            Assert.Equal("System.Double C.<P>k__BackingField", fieldKeywordSymbolInfo.Symbol.ToTestDisplayString(includeNonNullable: true));
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
            Assert.Equal("System.Double C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());
        }

        [Theory]
        [InlineData("get => 0; set => field = value;")]
        [InlineData("set => field = value; get => 0;")]
        [InlineData("get { return 0; } set { field = value; }")]
        [InlineData("set { field = value; } get { return 0; }")]
        [InlineData("get => 0; set { field = value; }")]
        [InlineData("set { field = value; } get => 0;")]
        [InlineData("get { return 0; } set => field = value;")]
        [InlineData("set => field = value; get { return 0; }")]
        public void SpeculativeSemanticModel_FieldInGetterNotUsingFieldWhereSetterAccessorDoes_BindSpeculatedFirst(string accessors)
        {
            var comp = CreateCompilation($@"
class C
{{
    public double P
    {{
        {accessors}
    }}
}}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);

            Assert.Equal("System.Double C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());
            Assert.Equal("System.Double C.<P>k__BackingField", fieldKeywordSymbolInfo.Symbol.ToTestDisplayString(includeNonNullable: true));
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData("get => 0; get => field;", 19, false)]
        [InlineData("get => field; get => 0;", 23, true)]
        [InlineData("get { return 0; } get => field;", 27, false)]
        [InlineData("get => field; get { return 0; }", 23, true)]
        [InlineData("get => 0; get { return field; }", 19, false)]
        [InlineData("get { return field; } get => 0;", 31, true)]
        [InlineData("get { return 0; } get { return field; }", 27, false)]
        [InlineData("get { return field; } get { return 0; }", 31, true)]
        public void SpeculativeSemanticModel_FieldInDuplicateAccessorWhereFirstAccessorUsesField_BindOriginalFirst(string accessors, int diagnosticColumn, bool fieldAccessorFirst)
        {
            var comp = CreateCompilation($@"
class C
{{
    public double P
    {{
        {accessors}
    }}
}}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(
                // error CS1007: Property accessor already defined
                Diagnostic(ErrorCode.ERR_DuplicateAccessor, "get").WithLocation(6, diagnosticColumn)
                );

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);

            var fieldsToEmit = comp.GetTypeByMetadataName("C").GetFieldsToEmit();
            if (fieldAccessorFirst)
            {
                Assert.Equal("System.Double C.<P>k__BackingField", fieldsToEmit.Single().ToTestDisplayString());
            }
            else
            {
                Assert.Empty(fieldsToEmit);
            }

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);
            if (fieldAccessorFirst)
            {
                Assert.Equal("System.Double C.<P>k__BackingField", fieldKeywordSymbolInfo.Symbol.ToTestDisplayString(includeNonNullable: true));
            }
            else
            {
                Assert.Null(fieldKeywordSymbolInfo.Symbol);
            }

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);

            if (fieldAccessorFirst)
            {
                Assert.Equal("System.Double C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());
            }
            else
            {
                Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            }
        }

        [Theory]
        [InlineData("get => 0; get => field;")]
        [InlineData("get => field; get => 0;")]
        [InlineData("get { return 0; } get => field;")]
        [InlineData("get => field; get { return 0; }")]
        [InlineData("get => 0; get { return field; }")]
        [InlineData("get { return field; } get => 0;")]
        [InlineData("get { return 0; } get { return field; }")]
        [InlineData("get { return field; } get { return 0; }")]
        public void SpeculativeSemanticModel_FieldInDuplicateAccessorWhereFirstAccessorUsesField_BindSpeculatedFirst(string accessors)
        {
            var comp = CreateCompilation($@"
class C
{{
    public double P
    {{
        {accessors}
    }}
}}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);

            Assert.Equal("System.Double C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());
            Assert.Equal("System.Double C.<P>k__BackingField", fieldKeywordSymbolInfo.Symbol.ToTestDisplayString(includeNonNullable: true));
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData("get => 0; get => 1;", 19, 0)]
        [InlineData("get => 0; get => 1;", 19, 1)]
        [InlineData("get { return 0; } get => 1;", 27, 0)]
        [InlineData("get { return 0; } get => 1;", 27, 1)]
        [InlineData("get => 0; get { return 1; }", 19, 0)]
        [InlineData("get => 0; get { return 1; }", 19, 1)]
        [InlineData("get { return 0; } get { return 1; }", 27, 0)]
        [InlineData("get { return 0; } get { return 1; }", 27, 1)]
        public void SpeculativeSemanticModel_FieldInDuplicateAccessorWhereFirstAccessorDoesNotUseField_BindOriginalFirst(string accessors, int diagnosticColumn, int numericLiteralToSpeculate)
        {
            var comp = CreateCompilation($@"
class C
{{
    public double P
    {{
        {accessors}
    }}
}}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(
                // error CS1007: Property accessor already defined
                Diagnostic(ErrorCode.ERR_DuplicateAccessor, "get").WithLocation(6, diagnosticColumn)
                );

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken) && (int)t.Value == numericLiteralToSpeculate);

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);

            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);
            Assert.Null(fieldKeywordSymbolInfo.Symbol);
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
        }

        [Theory]
        [InlineData("get => 0; get => 1;", 0)]
        [InlineData("get => 0; get => 1;", 1)]
        [InlineData("get { return 0; } get => 1;", 0)]
        [InlineData("get { return 0; } get => 1;", 1)]
        [InlineData("get => 0; get { return 1; }", 0)]
        [InlineData("get => 0; get { return 1; }", 1)]
        [InlineData("get { return 0; } get { return 1; }", 0)]
        [InlineData("get { return 0; } get { return 1; }", 1)]
        public void SpeculativeSemanticModel_FieldInDuplicateAccessorWhereFirstAccessorDoesNotUseField_BindSpeculatedFirst(string accessors, int numericLiteralToSpeculate)
        {
            var comp = CreateCompilation($@"
class C
{{
    public double P
    {{
        {accessors}
    }}
}}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken) && (int)t.Value == numericLiteralToSpeculate);

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);

            Assert.Equal("System.Double C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());
            Assert.Equal("System.Double C.<P>k__BackingField", fieldKeywordSymbolInfo.Symbol.ToTestDisplayString(includeNonNullable: true));
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }
    }
}
