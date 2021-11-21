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
    }
}
