﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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

    // PROTOTYPE(semi-auto-props): Add ENC tests.
    public class PropertyFieldKeywordTests : CompilingTestBase
    {
        /// <summary>
        /// Represents the state of "field" identifier for speculative semantic model tests.
        /// </summary>
        public enum FieldBindingTestState
        {
            /// <summary>
            /// The field identifier isn't a backing field or a local.
            /// </summary>
            /// <remarks>
            /// If the original property is not semi-auto property. We don't
            /// bind field identifier as a backing field.
            /// </remarks>
            None,

            /// <summary>
            /// The field identifier is bound as a backing field.
            /// </summary>
            /// <remarks>
            /// This only happens if the original property is a semi-auto property.
            /// </remarks>
            BecomesBackingField,

            /// <summary>
            /// The field identifier is bound as a local.
            /// </summary>
            BecomesLocal,
        }

        private void VerifyTypeIL(CSharpCompilation compilation, string typeName, string expected)
        {
            if (!ExecutionConditionUtil.IsDesktop)
            {
                // Hacky. We could otherwise run the tests only when IsDesktop is true, similar to what's done in CodeGenDisplayClassOptimizationTests
                expected = expected.Replace("[mscorlib]", "[netstandard]");
            }

            CompileAndVerify(compilation).VerifyTypeIL(typeName, expected);
        }

        [Theory, CombinatorialData]
        public void TestVirtualPropertyOverride(bool callGetFieldsToEmit, bool callSemanticModel)
        {
            var comp = CreateCompilation(@"
public class Base
{
    public virtual int P1 { get; set; }
    public virtual int P2 { get => 0; set { } }
}

public class Derived1 : Base
{
    public override int P1 { get { _ = field; return field; } }
    public override int P2 { get => field; }
}

public class Derived2 : Base
{
    public override int P1 { set => _ = field; }
    public override int P2 { set => _ = field; }
}

public class Derived3 : Base
{
    // PROTOTYPE(semi-auto-props):
    // This should produce ERR_AutoPropertyMustOverrideSet ""Auto-implemented properties must override all accessors of the overridden property.""
    // instead of ERR_AutoPropertyMustHaveGetAccessor, unless https://github.com/dotnet/csharplang/issues/6089 is accepted.
    public override int P1 { set; }
}

public class Derived4 : Base
{
    public override int P1 { get => field; set => field = value; }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            if (callGetFieldsToEmit)
            {
                var baseFields = comp.GetTypeByMetadataName("Base").GetFieldsToEmit().ToArray();
                Assert.Equal(1, baseFields.Length);
                Assert.Equal("System.Int32 Base.<P1>k__BackingField", baseFields[0].ToTestDisplayString());

                var derived1Fields = comp.GetTypeByMetadataName("Derived1").GetFieldsToEmit().ToArray();
                Assert.Equal(2, derived1Fields.Length);
                Assert.Equal("System.Int32 Derived1.<P1>k__BackingField", derived1Fields[0].ToTestDisplayString());
                Assert.Equal("System.Int32 Derived1.<P2>k__BackingField", derived1Fields[1].ToTestDisplayString());

                var derived2Fields = comp.GetTypeByMetadataName("Derived2").GetFieldsToEmit().ToArray();
                Assert.Equal(2, derived2Fields.Length);
                Assert.Equal("System.Int32 Derived2.<P1>k__BackingField", derived2Fields[0].ToTestDisplayString());
                Assert.Equal("System.Int32 Derived2.<P2>k__BackingField", derived2Fields[1].ToTestDisplayString());

                var derived3Fields = comp.GetTypeByMetadataName("Derived3").GetFieldsToEmit().ToArray();
                Assert.Equal(0, derived3Fields.Length);

                var derived4Fields = comp.GetTypeByMetadataName("Derived4").GetFieldsToEmit().ToArray();
                Assert.Equal(1, derived4Fields.Length);
                Assert.Equal("System.Int32 Derived4.<P1>k__BackingField", derived4Fields[0].ToTestDisplayString());
            }

            if (callSemanticModel)
            {
                var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
                var nodes = comp.SyntaxTrees.Single().GetRoot().DescendantNodes().Where(n => n is IdentifierNameSyntax identifier && identifier.Identifier.ContextualKind() == SyntaxKind.FieldKeyword);
                foreach (var node in nodes)
                {
                    var typeInfo = model.GetTypeInfo(node);
                    Assert.Equal(SpecialType.System_Int32, typeInfo.Type.SpecialType);
                }
            }

            comp.VerifyDiagnostics(
                // (10,25): error CS8080: Auto-implemented properties must override all accessors of the overridden property.
                //     public override int P1 { get { _ = field; return field; } }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P1").WithLocation(10, 25),
                // (11,25): error CS8080: Auto-implemented properties must override all accessors of the overridden property.
                //     public override int P2 { get => field; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P2").WithLocation(11, 25),
                // (16,25): error CS8080: Auto-implemented properties must override all accessors of the overridden property.
                //     public override int P1 { set => _ = field; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P1").WithLocation(16, 25),
                // (17,25): error CS8080: Auto-implemented properties must override all accessors of the overridden property.
                //     public override int P2 { set => _ = field; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P2").WithLocation(17, 25),
                // (25,30): error CS8051: Auto-implemented properties must have get accessors.
                //     public override int P1 { set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, "set").WithArguments("Derived3.P1.set").WithLocation(25, 30)
                );
            Assert.Equal(callGetFieldsToEmit ? 5 : 0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestInInterface()
        {
            var comp = CreateCompilation(@"
public interface I
{
    public int P1 { get => field; }

    public int P2 { get => field; set => field = value; }

    public int P3 { get { _ = field; return field; } set => field = value; }

    public int P4 { get => field; } = 0;

    public int P5 { get => field; set => field = value; } = 0;

    public int P6 { get { _ = field; return field; } set => field = value; } = 0;
}
", targetFramework: TargetFramework.NetCoreApp); // setting TargetFramework for DefaultImplementationsOfInterfaces to exist.

            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.VerifyDiagnostics(
                // (4,28): error CS0525: Interfaces cannot contain instance fields
                //     public int P1 { get => field; } = 0;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "field").WithLocation(4, 28),
                // (6,28): error CS0525: Interfaces cannot contain instance fields
                //     public int P2 { get => field; set => field = value; } = 0;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "field").WithLocation(6, 28),
                // (6,42): error CS0525: Interfaces cannot contain instance fields
                //     public int P2 { get => field; set => field = value; } = 0;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "field").WithLocation(6, 42),
                // (8,31): error CS0525: Interfaces cannot contain instance fields
                //     public int P3 { get { _ = field; return field; } set => field = value; } = 0;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "field").WithLocation(8, 31),
                // (8,45): error CS0525: Interfaces cannot contain instance fields
                //     public int P3 { get { _ = field; return field; } set => field = value; } = 0;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "field").WithLocation(8, 45),
                // (8,61): error CS0525: Interfaces cannot contain instance fields
                //     public int P3 { get { _ = field; return field; } set => field = value; } = 0;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "field").WithLocation(8, 61),
                // (10,16): error CS8053: Instance properties in interfaces cannot have initializers.
                //     public int P4 { get => field; } = 0;
                Diagnostic(ErrorCode.ERR_InstancePropertyInitializerInInterface, "P4").WithArguments("I.P4").WithLocation(10, 16),
                // (10,28): error CS0525: Interfaces cannot contain instance fields
                //     public int P4 { get => field; } = 0;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "field").WithLocation(10, 28),
                // (12,16): error CS8053: Instance properties in interfaces cannot have initializers.
                //     public int P5 { get => field; set => field = value; } = 0;
                Diagnostic(ErrorCode.ERR_InstancePropertyInitializerInInterface, "P5").WithArguments("I.P5").WithLocation(12, 16),
                // (12,28): error CS0525: Interfaces cannot contain instance fields
                //     public int P5 { get => field; set => field = value; } = 0;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "field").WithLocation(12, 28),
                // (12,42): error CS0525: Interfaces cannot contain instance fields
                //     public int P5 { get => field; set => field = value; } = 0;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "field").WithLocation(12, 42),
                // (14,16): error CS8053: Instance properties in interfaces cannot have initializers.
                //     public int P6 { get { _ = field; return field; } set => field = value; } = 0;
                Diagnostic(ErrorCode.ERR_InstancePropertyInitializerInInterface, "P6").WithArguments("I.P6").WithLocation(14, 16),
                // (14,31): error CS0525: Interfaces cannot contain instance fields
                //     public int P6 { get { _ = field; return field; } set => field = value; } = 0;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "field").WithLocation(14, 31),
                // (14,45): error CS0525: Interfaces cannot contain instance fields
                //     public int P6 { get { _ = field; return field; } set => field = value; } = 0;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "field").WithLocation(14, 45),
                // (14,61): error CS0525: Interfaces cannot contain instance fields
                //     public int P6 { get { _ = field; return field; } set => field = value; } = 0;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "field").WithLocation(14, 61)
                );
            var @interface = comp.GetTypeByMetadataName("I");
            Assert.Empty(@interface.GetMembers().OfType<FieldSymbol>());
            var fieldsToEmit = @interface.GetFieldsToEmit().ToArray();
            Assert.Equal(6, fieldsToEmit.Length);
            Assert.Equal("System.Int32 I.<P1>k__BackingField", fieldsToEmit[0].ToTestDisplayString());
            Assert.Equal("System.Int32 I.<P2>k__BackingField", fieldsToEmit[1].ToTestDisplayString());
            Assert.Equal("System.Int32 I.<P3>k__BackingField", fieldsToEmit[2].ToTestDisplayString());
            Assert.Equal("System.Int32 I.<P4>k__BackingField", fieldsToEmit[3].ToTestDisplayString());
            Assert.Equal("System.Int32 I.<P5>k__BackingField", fieldsToEmit[4].ToTestDisplayString());
            Assert.Equal("System.Int32 I.<P6>k__BackingField", fieldsToEmit[5].ToTestDisplayString());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestStaticInInterface()
        {
            var comp = CreateCompilation(@"
public interface I
{
    public static int P { get => field; }
}
", targetFramework: TargetFramework.NetCoreApp); // setting TargetFramework for DefaultImplementationsOfInterfaces to exist.

            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.VerifyDiagnostics();
            VerifyTypeIL(comp, "I", @"
.class interface public auto ansi abstract I
{
    // Fields
    .field private static initonly int32 '<P>k__BackingField'
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    	01 00 00 00
    )
    // Methods
    .method public hidebysig specialname static 
    	int32 get_P () cil managed 
    {
    	// Method begins at RVA 0x2050
    	// Code size 6 (0x6)
    	.maxstack 8
    	IL_0000: ldsfld int32 I::'<P>k__BackingField'
    	IL_0005: ret
    } // end of method I::get_P
    // Properties
    .property int32 P()
    {
    	.get int32 I::get_P()
    }
} // end of class I
");
            var @interface = comp.GetTypeByMetadataName("I");
            Assert.Equal("System.Int32 I.<P>k__BackingField", @interface.GetFieldsToEmit().Single().ToTestDisplayString());
            Assert.Empty(@interface.GetMembers().OfType<FieldSymbol>());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestStaticInInterface_DefaultImplementationsOfInterfacesIsMissing()
        {
            var comp = CreateCompilation(@"
public interface I
{
    public static int P { get => field; }
}
");
            comp.MakeMemberMissing(SpecialMember.System_Runtime_CompilerServices_RuntimeFeature__DefaultImplementationsOfInterfaces);
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.VerifyDiagnostics(
                // (4,27): error CS8701: Target runtime doesn't support default interface implementation.
                //     public static int P { get => field; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(4, 27)
                );
            var @interface = comp.GetTypeByMetadataName("I");
            Assert.Equal("System.Int32 I.<P>k__BackingField", @interface.GetFieldsToEmit().Single().ToTestDisplayString());
            Assert.Empty(@interface.GetMembers().OfType<FieldSymbol>());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestAbstract()
        {
            var comp = CreateCompilation(@"
public abstract class C
{
    public abstract int P { get => field; }
}
"); // setting TargetFramework for DefaultImplementationsOfInterfaces to exist.

            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.VerifyDiagnostics(
                // (4,29): error CS0500: 'C.P.get' cannot declare a body because it is marked abstract
                //     public abstract int P { get => field; }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("C.P.get").WithLocation(4, 29)
                );
            var @class = comp.GetTypeByMetadataName("C");
            Assert.Empty(@class.GetMembers().OfType<FieldSymbol>());
            Assert.Empty(@class.GetFieldsToEmit());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestExtern()
        {
            var comp = CreateCompilation(@"
public class C
{
    public extern int P { get => field; }
}
");

            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.VerifyDiagnostics(
                // (4,27): error CS0179: 'C.P.get' cannot be extern and declare a body
                //     public extern int P { get => field; }
                Diagnostic(ErrorCode.ERR_ExternHasBody, "get").WithArguments("C.P.get").WithLocation(4, 27)
                );
            var @class = comp.GetTypeByMetadataName("C");
            Assert.Empty(@class.GetMembers().OfType<FieldSymbol>());
            Assert.Empty(@class.GetFieldsToEmit());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestNameOfFieldInAttribute()
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            return local();

            [My(nameof(field))]
            int local() => 0;
        }
    }
}

public class MyAttribute : System.Attribute
{
    public MyAttribute(string s) { }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.VerifyDiagnostics(
                // (10,24): error CS9028: Cannot use 'field' keyword inside 'nameof' expressions.
                //             [My(nameof(field))]
                Diagnostic(ErrorCode.ERR_FieldKeywordInsideNameOf, "field").WithLocation(10, 24)
            );
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestNameOfField()
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            return nameof(field);
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.VerifyDiagnostics(
                // (8,20): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //             return nameof(field);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "nameof(field)").WithArguments("string", "int").WithLocation(8, 20),
                // (8,27): error CS9028: Cannot use 'field' keyword inside 'nameof' expressions.
                //             return nameof(field);
                Diagnostic(ErrorCode.ERR_FieldKeywordInsideNameOf, "field").WithLocation(8, 27)
            );
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestNameOfField_NameofIsMethodInvocation()
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            return nameof(field);
        }
    }

    public int nameof(int x) => 0;
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.VerifyDiagnostics();
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
		// Code size 13 (0xd)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldarg.0
		IL_0002: ldfld int32 C::'<P>k__BackingField'
		IL_0007: call instance int32 C::nameof(int32)
		IL_000c: ret
	} // end of method C::get_P
	.method public hidebysig 
		instance int32 nameof (
			int32 x
		) cil managed 
	{
		// Method begins at RVA 0x205e
		// Code size 2 (0x2)
		.maxstack 8
		IL_0000: ldc.i4.0
		IL_0001: ret
	} // end of method C::nameof
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
	.property instance int32 P()
	{
		.get instance int32 C::get_P()
	}
} // end of class C
");
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestPropertyNotAssignedInStructConstructor()
        {
            var comp = CreateCompilation(@"
public struct C
{
    public static void Main()
    {
        var x = new C();
        x.P = 10;
        x = new C();
    }

    public C()
    {
        System.Console.WriteLine(""In C..ctor: "" + P);
    }

    public int P { get => field; set => field = value; }
}
", options: TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            CompileAndVerify(comp, expectedOutput: @"In C..ctor: 0
In C..ctor: 0").VerifyIL("C..ctor", @"
{
    // Code size       37 (0x25)
    .maxstack  2
    .locals init (int V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  stfld      ""int C.<P>k__BackingField""
    IL_0007:  ldstr      ""In C..ctor: ""
    IL_000c:  ldarg.0
    IL_000d:  call       ""int C.P.get""
    IL_0012:  stloc.0
    IL_0013:  ldloca.s   V_0
    IL_0015:  call       ""string int.ToString()""
    IL_001a:  call       ""string string.Concat(string, string)""
    IL_001f:  call       ""void System.Console.WriteLine(string)""
    IL_0024:  ret
}
").VerifyDiagnostics(
    // (13,51): warning CS9020: The 'this' object is read before all of its fields have been assigned, causing preceding implicit assignments of 'default' to non-explicitly assigned fields.
    //         System.Console.WriteLine("In C..ctor: " + P);
    Diagnostic(ErrorCode.WRN_UseDefViolationThisSupportedVersion, "P").WithLocation(13, 51)
    );
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestPropertyIsReadThenAssignedInStructConstructor()
        {
            var comp = CreateCompilation(@"
public struct C
{
    public static void Main()
    {
        var x = new C();
        x.P = 10;
        x = new C();
    }

    public C()
    {
        System.Console.WriteLine(""In C..ctor before assignment: "" + P);
        P = 5;
        System.Console.WriteLine(""In C..ctor after assignment: "" + P);
    }

    public int P { get => field; set => field = value; }
}
", options: TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            CompileAndVerify(comp, expectedOutput: @"In C..ctor before assignment: 0
In C..ctor after assignment: 5
In C..ctor before assignment: 0
In C..ctor after assignment: 5").VerifyIL("C..ctor", @"
{
  // Code size       73 (0x49)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int C.<P>k__BackingField""
  IL_0007:  ldstr      ""In C..ctor before assignment: ""
  IL_000c:  ldarg.0
  IL_000d:  call       ""int C.P.get""
  IL_0012:  stloc.0
  IL_0013:  ldloca.s   V_0
  IL_0015:  call       ""string int.ToString()""
  IL_001a:  call       ""string string.Concat(string, string)""
  IL_001f:  call       ""void System.Console.WriteLine(string)""
  IL_0024:  ldarg.0
  IL_0025:  ldc.i4.5
  IL_0026:  call       ""void C.P.set""
  IL_002b:  ldstr      ""In C..ctor after assignment: ""
  IL_0030:  ldarg.0
  IL_0031:  call       ""int C.P.get""
  IL_0036:  stloc.0
  IL_0037:  ldloca.s   V_0
  IL_0039:  call       ""string int.ToString()""
  IL_003e:  call       ""string string.Concat(string, string)""
  IL_0043:  call       ""void System.Console.WriteLine(string)""
  IL_0048:  ret
}
").VerifyDiagnostics(
    // (13,69): warning CS9020: The 'this' object is read before all of its fields have been assigned, causing preceding implicit assignments of 'default' to non-explicitly assigned fields.
    //         System.Console.WriteLine("In C..ctor before assignment: " + P);
    Diagnostic(ErrorCode.WRN_UseDefViolationThisSupportedVersion, "P").WithLocation(13, 69)
    );
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact] // PROTOTYPE(semi-auto-props): Add test with semi-colon setter when mixed scenarios are supported.
        public void TestPropertyIsReadThenAssignedInStructConstructor_ReadOnlyProperty()
        {
            var comp = CreateCompilation(@"
public struct C
{
    public static void Main()
    {
        var x = new C();
        System.Console.Write(x.P);
    }

    public C()
    {
        System.Console.WriteLine(""In C..ctor before assignment: "" + P);
        P = 5;
        System.Console.WriteLine(""In C..ctor after assignment: "" + P);
    }

    public int P { get => field; }
}
", options: TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            CompileAndVerify(comp, expectedOutput: @"In C..ctor before assignment: 0
In C..ctor after assignment: 5
5").VerifyIL("C..ctor", @"
{
    // Code size       73 (0x49)
    .maxstack  2
    .locals init (int V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  stfld      ""int C.<P>k__BackingField""
    IL_0007:  ldstr      ""In C..ctor before assignment: ""
    IL_000c:  ldarg.0
    IL_000d:  call       ""int C.P.get""
    IL_0012:  stloc.0
    IL_0013:  ldloca.s   V_0
    IL_0015:  call       ""string int.ToString()""
    IL_001a:  call       ""string string.Concat(string, string)""
    IL_001f:  call       ""void System.Console.WriteLine(string)""
    IL_0024:  ldarg.0
    IL_0025:  ldc.i4.5
    IL_0026:  stfld      ""int C.<P>k__BackingField""
    IL_002b:  ldstr      ""In C..ctor after assignment: ""
    IL_0030:  ldarg.0
    IL_0031:  call       ""int C.P.get""
    IL_0036:  stloc.0
    IL_0037:  ldloca.s   V_0
    IL_0039:  call       ""string int.ToString()""
    IL_003e:  call       ""string string.Concat(string, string)""
    IL_0043:  call       ""void System.Console.WriteLine(string)""
    IL_0048:  ret
}
").VerifyDiagnostics(
    // (12,69): warning CS9020: The 'this' object is read before all of its fields have been assigned, causing preceding implicit assignments of 'default' to non-explicitly assigned fields.
    //         System.Console.WriteLine("In C..ctor before assignment: " + P);
    Diagnostic(ErrorCode.WRN_UseDefViolationThisSupportedVersion, "P").WithLocation(12, 69)
    );
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestPropertyIsAssignedInStructConstructor_PropertyHasSetter()
        {
            var comp = CreateCompilation(@"
public struct C
{
    public static void Main()
    {
        var x = new C();
        System.Console.Write(x.P + "" ""); // 5
        x.P = 10;
        System.Console.Write(x.P + "" ""); // 10
        x = new C();
        System.Console.Write(x.P); // 5
    }

    public C()
    {
        P = 5;
    }

    public int P { get => field; set => field = value; }
}
", options: TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            CompileAndVerify(comp, expectedOutput: "5 10 5").VerifyIL("C..ctor", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int C.<P>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.5
  IL_0009:  call       ""void C.P.set""
  IL_000e:  ret
}
").VerifyDiagnostics(
    // (16,9): warning CS9020: The 'this' object is read before all of its fields have been assigned, causing preceding implicit assignments of 'default' to non-explicitly assigned fields.
    //         P = 5;
    Diagnostic(ErrorCode.WRN_UseDefViolationThisSupportedVersion, "P").WithLocation(16, 9)
    );
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestPropertyIsAssignedInStructConstructor_ReadOnlyProperty()
        {
            var comp = CreateCompilation(@"
public struct C
{
    public static void Main()
    {
        var x = new C();
        System.Console.Write(x.P);
    }

    public C()
    {
        P = 5;
    }

    public int P { get => field; }
}
", options: TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            CompileAndVerify(comp, expectedOutput: "5").VerifyIL("C..ctor", @"
    {
      // Code size        8 (0x8)
      .maxstack  2
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.5
      IL_0002:  stfld      ""int C.<P>k__BackingField""
      IL_0007:  ret
    }
").VerifyDiagnostics();
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestFieldInLocalFunction()
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            return local1() + local2() + local3();

            [My(field)]
            int local1() => 0;

            int local2(int i = field) => 0;

            static int local3(int i = field) => 0;
        }
    }
}

public class MyAttribute : System.Attribute
{
    public MyAttribute(int i) { }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.VerifyDiagnostics(
                // (10,17): error CS0120: An object reference is required for the non-static field, method, or property 'C.<P>k__BackingField'
                //             [My(field)]
                Diagnostic(ErrorCode.ERR_ObjectRequired, "field").WithArguments("C.<P>k__BackingField").WithLocation(10, 17),
                // (13,32): error CS1736: Default parameter value for 'i' must be a compile-time constant
                //             int local2(int i = field) => 0;
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "field").WithArguments("i").WithLocation(13, 32),
                // (15,39): error CS1736: Default parameter value for 'i' must be a compile-time constant
                //             static int local3(int i = field) => 0;
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "field").WithArguments("i").WithLocation(15, 39)
            );
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestFieldInLocalFunction_ShadowedByConstant()
        {
            var comp = CreateCompilation(@"
System.Console.WriteLine(new C().P);

public class C
{
    public int P
    {
        get
        {
            const int field = 5;
            return local1() + local2() + local3();

            [My(field)]
            int local1() => 0;

            int local2(int i = field) => i;

            static int local3(int i = field) => i;
        }
    }
}

public class MyAttribute : System.Attribute
{
    public MyAttribute(int i) { }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            CompileAndVerify(comp, expectedOutput: "10").VerifyDiagnostics(
                // Looks like an incorrect diagnostic. Tracked by https://github.com/dotnet/roslyn/issues/60645
                // (10,23): warning CS0219: The variable 'field' is assigned but its value is never used
                //             const int field = 5;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "field").WithArguments("field").WithLocation(10, 23)
                );
            VerifyTypeIL(comp, "C", @"
.class public auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	// Methods
	.method public hidebysig specialname 
		instance int32 get_P () cil managed 
	{
		// Method begins at RVA 0x2069
		// Code size 20 (0x14)
		.maxstack 8
		IL_0000: call int32 C::'<get_P>g__local1|1_0'()
		IL_0005: ldc.i4.5
		IL_0006: call int32 C::'<get_P>g__local2|1_1'(int32)
		IL_000b: add
		IL_000c: ldc.i4.5
		IL_000d: call int32 C::'<get_P>g__local3|1_2'(int32)
		IL_0012: add
		IL_0013: ret
	} // end of method C::get_P
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
	.method assembly hidebysig static 
		int32 '<get_P>g__local1|1_0' () cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		.custom instance void MyAttribute::.ctor(int32) = (
			01 00 05 00 00 00 00 00
		)
		// Method begins at RVA 0x207e
		// Code size 2 (0x2)
		.maxstack 8
		IL_0000: ldc.i4.0
		IL_0001: ret
	} // end of method C::'<get_P>g__local1|1_0'
	.method assembly hidebysig static 
		int32 '<get_P>g__local2|1_1' (
			[opt] int32 i
		) cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		.param [1] = int32(5)
		// Method begins at RVA 0x2081
		// Code size 2 (0x2)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ret
	} // end of method C::'<get_P>g__local2|1_1'
	.method assembly hidebysig static 
		int32 '<get_P>g__local3|1_2' (
			[opt] int32 i
		) cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		.param [1] = int32(5)
		// Method begins at RVA 0x2081
		// Code size 2 (0x2)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ret
	} // end of method C::'<get_P>g__local3|1_2'
	// Properties
	.property instance int32 P()
	{
		.get instance int32 C::get_P()
	}
} // end of class C
");
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestFieldInLocalFunction_PropertyIsStatic()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static int P
    {
        get
        {
            return local1() + local2() + local3();

            [My(field)]
            int local1() => 0;

            int local2(int i = field) => 0;

            static int local3(int i = field) => 0;
        }
    }
}

public class MyAttribute : System.Attribute
{
    public MyAttribute(int i) { }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.VerifyDiagnostics(
                // (10,17): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //             [My(field)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(10, 17),
                // (13,32): error CS1736: Default parameter value for 'i' must be a compile-time constant
                //             int local2(int i = field) => 0;
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "field").WithArguments("i").WithLocation(13, 32),
                // (15,39): error CS1736: Default parameter value for 'i' must be a compile-time constant
                //             static int local3(int i = field) => 0;
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "field").WithArguments("i").WithLocation(15, 39)
            );
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestFieldIsShadowed_ReferencedFromRegularLocalFunction()
        {
            var comp = CreateCompilation(@"
System.Console.WriteLine(new C().P);

public class C
{
    public int P
    {
        get
        {
            int field = 5;
            return local();

            int local() => field;
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            CompileAndVerify(comp, expectedOutput: "5").VerifyDiagnostics();
            VerifyTypeIL(comp, "C", @"
.class public auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass1_0'
		extends [mscorlib]System.ValueType
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 'field'
	} // end of class <>c__DisplayClass1_0
	// Methods
	.method public hidebysig specialname 
		instance int32 get_P () cil managed 
	{
		// Method begins at RVA 0x206c
		// Code size 16 (0x10)
		.maxstack 2
		.locals init (
			[0] valuetype C/'<>c__DisplayClass1_0'
		)
		IL_0000: ldloca.s 0
		IL_0002: ldc.i4.5
		IL_0003: stfld int32 C/'<>c__DisplayClass1_0'::'field'
		IL_0008: ldloca.s 0
		IL_000a: call int32 C::'<get_P>g__local|1_0'(valuetype C/'<>c__DisplayClass1_0'&)
		IL_000f: ret
	} // end of method C::get_P
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
	.method assembly hidebysig static 
		int32 '<get_P>g__local|1_0' (
			valuetype C/'<>c__DisplayClass1_0'& ''
		) cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x2088
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld int32 C/'<>c__DisplayClass1_0'::'field'
		IL_0006: ret
	} // end of method C::'<get_P>g__local|1_0'
	// Properties
	.property instance int32 P()
	{
		.get instance int32 C::get_P()
	}
} // end of class C
");
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestFieldIsShadowed_ReferencedFromRegularLambda()
        {
            var comp = CreateCompilation(@"
System.Console.WriteLine(new C().P);
public class C
{
    public int P
    {
        get
        {
            int field = 5;
            System.Func<int> func = () => field;
            return func();
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            CompileAndVerify(comp, expectedOutput: "5").VerifyDiagnostics();
            VerifyTypeIL(comp, "C", @"
.class public auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass1_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 'field'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2061
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass1_0'::.ctor
		.method assembly hidebysig 
			instance int32 '<get_P>b__0' () cil managed 
		{
			// Method begins at RVA 0x2087
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 C/'<>c__DisplayClass1_0'::'field'
			IL_0006: ret
		} // end of method '<>c__DisplayClass1_0'::'<get_P>b__0'
	} // end of class <>c__DisplayClass1_0
	// Methods
	.method public hidebysig specialname 
		instance int32 get_P () cil managed 
	{
		// Method begins at RVA 0x2069
		// Code size 29 (0x1d)
		.maxstack 8
		IL_0000: newobj instance void C/'<>c__DisplayClass1_0'::.ctor()
		IL_0005: dup
		IL_0006: ldc.i4.5
		IL_0007: stfld int32 C/'<>c__DisplayClass1_0'::'field'
		IL_000c: ldftn instance int32 C/'<>c__DisplayClass1_0'::'<get_P>b__0'()
		IL_0012: newobj instance void class [mscorlib]System.Func`1<int32>::.ctor(object, native int)
		IL_0017: callvirt instance !0 class [mscorlib]System.Func`1<int32>::Invoke()
		IL_001c: ret
	} // end of method C::get_P
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
	.property instance int32 P()
	{
		.get instance int32 C::get_P()
	}
} // end of class C
");
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory, CombinatorialData]
        public void TestFieldIsShadowed_ReferencedFromStaticLocalFunction(bool isStatic, bool isLocal)
        {
            var local = isLocal ? "int field = 5;" : "";
            var field = !isLocal ? $"private {(isStatic ? "static" : "")} int field = 5;" : "";
            var comp = CreateCompilation($@"
public class C
{{
    {field}
    public {(isStatic ? "static" : "")} int P
    {{
        get
        {{
            {local}
            return local();

            static int local() => field;
        }}
    }}
}}
");
            var tree = comp.SyntaxTrees[0];
            var root = tree.GetRoot();
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            if (isLocal)
            {
                comp.VerifyDiagnostics(
                    // (12,35): error CS8421: A static local function cannot contain a reference to 'field'.
                    //             static int local() => field;
                    Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "field").WithArguments("field").WithLocation(12, 35)
                );
            }
            else if (!isStatic && !isLocal)
            {
                comp.VerifyDiagnostics(
                    // (12,35): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                    //             static int local() => field;
                    Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "field").WithLocation(12, 35)
                );
            }
            else
            {
                Assert.True(isStatic && !isLocal);
                comp.VerifyDiagnostics();
            }

            var symbolInfo = comp.GetSemanticModel(tree).GetSymbolInfo(root.DescendantNodes().Single(n => n is IdentifierNameSyntax { Parent: ArrowExpressionClauseSyntax }));
            if (isLocal)
            {
                Assert.Equal(SymbolKind.Local, symbolInfo.Symbol.Kind);
                Assert.Equal("System.Int32 field", symbolInfo.Symbol.GetSymbol().ToTestDisplayString());
            }
            else
            {
                Assert.Equal(SymbolKind.Field, symbolInfo.Symbol.Kind);
                Assert.Equal("System.Int32 C.field", symbolInfo.Symbol.GetSymbol().ToTestDisplayString());
            }

            if (isLocal)
            {
                Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
                Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            }
            else
            {
                Assert.Equal(symbolInfo.Symbol.GetSymbol(), comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>().Single());
                Assert.Equal(symbolInfo.Symbol.GetSymbol(), comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single());
            }

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory, CombinatorialData]
        public void TestFieldIsShadowed_ReferencedFromStaticLambda(bool isLocal)
        {
            var local = isLocal ? "int field = 5;" : "";
            var field = !isLocal ? "private int field = 5;" : "";
            var comp = CreateCompilation($@"
public class C
{{
    {field}
    public int P
    {{
        get
        {{
            {local}
            System.Func<int> func = static () => field;
            return func();
        }}
    }}
}}
");
            var tree = comp.SyntaxTrees[0];
            var root = tree.GetRoot();
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            if (isLocal)
            {
                comp.VerifyDiagnostics(
                    // (10,50): error CS8820: A static anonymous function cannot contain a reference to 'field'.
                    //             System.Func<int> func = static () => field;
                    Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable, "field").WithArguments("field").WithLocation(10, 50)
                );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (10,50): error CS8821: A static anonymous function cannot contain a reference to 'this' or 'base'.
                    //             System.Func<int> func = static () => field;
                    Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureThis, "field").WithLocation(10, 50)
                );
            }

            var symbolInfo = comp.GetSemanticModel(tree).GetSymbolInfo(root.DescendantNodes().Single(n => n is IdentifierNameSyntax { Parent: ParenthesizedLambdaExpressionSyntax }));
            if (isLocal)
            {
                Assert.Equal("System.Int32 field", symbolInfo.Symbol.GetSymbol().ToTestDisplayString());
                Assert.Equal(SymbolKind.Local, symbolInfo.Symbol.Kind);
                Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
                Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            }
            else
            {
                Assert.Equal("System.Int32 C.field", symbolInfo.Symbol.GetSymbol().ToTestDisplayString());
                Assert.Equal(SymbolKind.Field, symbolInfo.Symbol.Kind);
                Assert.Equal(symbolInfo.Symbol.GetSymbol(), comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>().Single());
                Assert.Equal(symbolInfo.Symbol.GetSymbol(), comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single());
            }

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestFieldIsShadowedByField_ReferencedFromRegularLocalFunction()
        {
            var comp = CreateCompilation(@"
System.Console.WriteLine(new C().P);

public class C
{
    private int field = 5;

    public int P
    {
        get
        {
            return local();

            int local() => field;
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            CompileAndVerify(comp, expectedOutput: "5").VerifyDiagnostics();
            VerifyTypeIL(comp, "C", @"
.class public auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	// Fields
	.field private int32 'field'
	// Methods
	.method public hidebysig specialname 
		instance int32 get_P () cil managed 
	{
		// Method begins at RVA 0x2069
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance int32 C::'<get_P>g__local|2_0'()
		IL_0006: ret
	} // end of method C::get_P
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x2071
		// Code size 14 (0xe)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldc.i4.5
		IL_0002: stfld int32 C::'field'
		IL_0007: ldarg.0
		IL_0008: call instance void [mscorlib]System.Object::.ctor()
		IL_000d: ret
	} // end of method C::.ctor
	.method private hidebysig 
		instance int32 '<get_P>g__local|2_0' () cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x2080
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld int32 C::'field'
		IL_0006: ret
	} // end of method C::'<get_P>g__local|2_0'
	// Properties
	.property instance int32 P()
	{
		.get instance int32 C::get_P()
	}
} // end of class C
");
            var field = comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>().Single();
            Assert.Equal("System.Int32 C.field", field.ToTestDisplayString());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestNameOfField_NameofIsLocalFunctionInvocation()
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            return nameof(field);

            int nameof(int x) => 0;
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.VerifyDiagnostics();
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
		// Code size 12 (0xc)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld int32 C::'<P>k__BackingField'
		IL_0006: call int32 C::'<get_P>g__nameof|1_0'(int32)
		IL_000b: ret
	} // end of method C::get_P
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x205d
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method C::.ctor
	.method assembly hidebysig static 
		int32 '<get_P>g__nameof|1_0' (
			int32 x
		) cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x2065
		// Code size 2 (0x2)
		.maxstack 8
		IL_0000: ldc.i4.0
		IL_0001: ret
	} // end of method C::'<get_P>g__nameof|1_0'
	// Properties
	.property instance int32 P()
	{
		.get instance int32 C::get_P()
	}
} // end of class C
");
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestFieldIsShadowedByField_ReferencedFromRegularLambda()
        {
            var comp = CreateCompilation(@"
System.Console.WriteLine(new C().P);
public class C
{
    private int field = 5;

    public int P
    {
        get
        {
            System.Func<int> func = () => field;
            return func();
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            CompileAndVerify(comp, expectedOutput: "5").VerifyDiagnostics();
            VerifyTypeIL(comp, "C", @"
.class public auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	// Fields
	.field private int32 'field'
	// Methods
	.method public hidebysig specialname 
		instance int32 get_P () cil managed 
	{
		// Method begins at RVA 0x2069
		// Code size 18 (0x12)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldftn instance int32 C::'<get_P>b__2_0'()
		IL_0007: newobj instance void class [mscorlib]System.Func`1<int32>::.ctor(object, native int)
		IL_000c: callvirt instance !0 class [mscorlib]System.Func`1<int32>::Invoke()
		IL_0011: ret
	} // end of method C::get_P
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x207c
		// Code size 14 (0xe)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldc.i4.5
		IL_0002: stfld int32 C::'field'
		IL_0007: ldarg.0
		IL_0008: call instance void [mscorlib]System.Object::.ctor()
		IL_000d: ret
	} // end of method C::.ctor
	.method private hidebysig 
		instance int32 '<get_P>b__2_0' () cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x208b
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld int32 C::'field'
		IL_0006: ret
	} // end of method C::'<get_P>b__2_0'
	// Properties
	.property instance int32 P()
	{
		.get instance int32 C::get_P()
	}
} // end of class C
");
            var field = comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>().Single();
            Assert.Equal("System.Int32 C.field", field.ToTestDisplayString());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData("private void field() { }")]
        [InlineData("private int field { get => 0; }")]
        [InlineData("private int field;")]
        public void TestNameOfField_FieldIsMember(string member)
        {
            var comp = CreateCompilation($@"
System.Console.WriteLine(new C().P);

public class C
{{
#pragma warning disable CS0649 // Field 'C.field' is never assigned to, and will always have its default value 0
    {member}
#pragma warning restore CS0649

    public string P
    {{
        get
        {{
            return nameof(field);
        }}
    }}
}}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            CompileAndVerify(comp, expectedOutput: "field").VerifyDiagnostics();
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
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
            CompileAndVerify(comp, expectedOutput: "5").VerifyDiagnostics();
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

        [Fact]
        public void TestAssigningFromConstructorNoAccessors()
        {
            var comp = CreateCompilation(@"
public class C
{
    public C()
    {
        P = 5;
    }

    public int P { }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(
                // (6,9): error CS0200: Property or indexer 'C.P' cannot be assigned to -- it is read only
                //         P = 5;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "P").WithArguments("C.P").WithLocation(6, 9),
                // (9,16): error CS0548: 'C.P': property or indexer must have at least one accessor
                //     public int P { }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "P").WithArguments("C.P").WithLocation(9, 16)
            );
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData("get;", false)]
        [InlineData("get; set;", true)]
        // [InlineData("set;")] PROTOTYPE(semi-auto-props): Not yet supported.
        [InlineData("get => field;", false)]
        // [InlineData("get => field; set;")] PROTOTYPE(semi-auto-props): Not yet supported
        public void TestAssigningFromConstructorThroughBackingField(string accessors, bool callsSynthesizedSetter)
        {
            var comp = CreateCompilation($@"
public class C
{{
    public C()
    {{
        P = 5;
    }}

    public int P {{ {accessors} }}

    public static void Main()
    {{
        System.Console.WriteLine(new C().P);
    }}
}}
", options: TestOptions.DebugExe);
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            string expectedCtorIL;
            if (callsSynthesizedSetter)
            {

                expectedCtorIL = @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  nop
  IL_0007:  nop
  IL_0008:  ldarg.0
  IL_0009:  ldc.i4.5
  IL_000a:  call       ""void C.P.set""
  IL_000f:  nop
  IL_0010:  ret
}
";
            }
            else
            {
                expectedCtorIL = @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  nop
  IL_0007:  nop
  IL_0008:  ldarg.0
  IL_0009:  ldc.i4.5
  IL_000a:  stfld      ""int C.<P>k__BackingField""
  IL_000f:  ret
}
";
            }

            CompileAndVerify(comp, expectedOutput: "5").VerifyDiagnostics().VerifyIL("C.P.get", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<P>k__BackingField""
  IL_0006:  ret
}
").VerifyIL("C..ctor", expectedCtorIL);
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        public void TestAssigningFromConstructorThroughSetterWithFieldKeyword_NoGetter(string type)
        {
            var comp = CreateCompilation($@"
public {type} C
{{
    public C()
    {{
        P = 5;
    }}

    public int P {{ set => field = value * 2; }}
}}
");
            string ctorExpectedIL;
            if (type == "struct")
            {
                ctorExpectedIL = @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int C.<P>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.5
  IL_0009:  call       ""void C.P.set""
  IL_000e:  ret
}
";
            }
            else
            {
                ctorExpectedIL = @"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldc.i4.5
  IL_0008:  call       ""void C.P.set""
  IL_000d:  ret
}
";
            }
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            CompileAndVerify(comp).VerifyDiagnostics().VerifyIL("C.P.set", @"
{
  // Code size       10 (0xa)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldc.i4.2
  IL_0003:  mul
  IL_0004:  stfld      ""int C.<P>k__BackingField""
  IL_0009:  ret
}
").VerifyIL("C..ctor", ctorExpectedIL);
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        //[InlineData("get; set => field = value * 2;")] PROTOTYPE(semi-auto-props): Not yet supported.
        [InlineData("get => field; set => field = value * 2;")]
        public void TestAssigningFromConstructorThroughSetterWithFieldKeyword_HasGetter(string accessors)
        {
            var comp = CreateCompilation($@"
public class C
{{
    public C()
    {{
        P = 5;
    }}

    public int P {{ {accessors} }}

    public static void Main()
    {{
        System.Console.WriteLine(new C().P);
    }}
}}
", options: TestOptions.DebugExe);
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            CompileAndVerify(comp, expectedOutput: "10").VerifyDiagnostics().VerifyIL("C.P.get", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<P>k__BackingField""
  IL_0006:  ret
}
").VerifyIL("C..ctor", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  nop
  IL_0007:  nop
  IL_0008:  ldarg.0
  IL_0009:  ldc.i4.5
  IL_000a:  call       ""void C.P.set""
  IL_000f:  nop
  IL_0010:  ret
}
");
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory(Skip = "PROTOTYPE(semi-auto-props): Not supported yet.")]
        [InlineData("get; set => _ = value;")]
        [InlineData("set => _ = value;")]
        [InlineData("get => field; set => _ = value;")]
        public void TestAssigningFromConstructorThroughSetter_RegularSetter(string accessors)
        {
            var comp = CreateCompilation($@"
public class C
{{
    public C()
    {{
        P = 5;
    }}

    public int P {{ {accessors} }}

    public static void Main()
    {{
        System.Console.WriteLine(new C().P);
    }}
}}
", options: TestOptions.DebugExe);
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            CompileAndVerify(comp, expectedOutput: "0").VerifyDiagnostics().VerifyIL("C.P.get", @"
").VerifyIL("C..ctor", @"
");
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        // [InlineData("get => 0; set;")] PROTOTYPE(semi-auto-props): Not yet supported.
        [InlineData("get => 0; set => field = value;")]
        public void TestAssigningFromConstructorThroughSetter_RegularGetter_CanAssignInCtor(string accessors)
        {
            var comp = CreateCompilation($@"
public class C
{{
    public C()
    {{
        P = 5;
    }}

    public int P {{ {accessors} }}

    public static void Main()
    {{
        System.Console.WriteLine(new C().P);
    }}
}}
", options: TestOptions.DebugExe);
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            CompileAndVerify(comp, expectedOutput: "0").VerifyDiagnostics().VerifyIL("C.P.get", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}
").VerifyIL("C.P.set", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int C.<P>k__BackingField""
  IL_0007:  ret
}
").VerifyIL("C..ctor", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  nop
  IL_0007:  nop
  IL_0008:  ldarg.0
  IL_0009:  ldc.i4.5
  IL_000a:  call       ""void C.P.set""
  IL_000f:  nop
  IL_0010:  ret
}
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
        public void TestFieldLocalEqualsField()
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P { get { int field = field; return field; } }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(
                // (4,38): error CS0165: Use of unassigned local variable 'field'
                //     public int P { get { int field = field; return field; } }
                Diagnostic(ErrorCode.ERR_UseDefViolation, "field").WithArguments("field").WithLocation(4, 38)
            );

            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestFieldWhenLocalFieldExistsButNotInScope()
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            if (GetBoolValue())
            {
                int field = 10;
                return field;
            }

            return field;
        }
    }

    public bool GetBoolValue() => true;
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();
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
		// Code size 18 (0x12)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance bool C::GetBoolValue()
		IL_0006: brfalse.s IL_000b
		IL_0008: ldc.i4.s 10
		IL_000a: ret
		IL_000b: ldarg.0
		IL_000c: ldfld int32 C::'<P>k__BackingField'
		IL_0011: ret
	} // end of method C::get_P
	.method public hidebysig 
		instance bool GetBoolValue () cil managed 
	{
		// Method begins at RVA 0x2063
		// Code size 2 (0x2)
		.maxstack 8
		IL_0000: ldc.i4.1
		IL_0001: ret
	} // end of method C::GetBoolValue
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x2066
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
            Assert.Equal("System.Int32 C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Equal("System.Int32 S.<P1>k__BackingField", comp.GetTypeByMetadataName("S").GetMembers().OfType<FieldSymbol>().Single().ToTestDisplayString());
            comp.VerifyDiagnostics(
                // (4,35): error CS8658: Auto-implemented 'set' accessor 'S.P1.set' cannot be marked 'readonly'.
                //     public int P1 { get; readonly set; } // ERR_AutoSetterCantBeReadOnly
                Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P1.set").WithLocation(4, 35),
                // (6,51): error CS1604: Cannot assign to 'field' because it is read-only
                //     public int P3 { get => field; readonly set => field = value; } // No ERR_AutoSetterCantBeReadOnly, but ERR_AssgReadonlyLocal
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(6, 51),
                // (7,42): error CS1604: Cannot assign to 'field' because it is read-only
                //     public int P4 { get; readonly set => field = value; } // No ERR_AutoSetterCantBeReadOnly, but ERR_AssgReadonlyLocal
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(7, 42),
                // PROTOTYPE(semi-auto-props): The following diagnostic shouldn't exist. It should go away when mixed scenarios are supported.
                // (7,21): error CS0501: 'S.P4.get' must declare a body because it is not marked abstract, extern, or partial
                //     public int P4 { get; readonly set => field = value; } // No ERR_AutoSetterCantBeReadOnly, but ERR_AssgReadonlyLocal
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("S.P4.get").WithLocation(7, 21)
            );
            Assert.Equal(1, accessorBindingData.NumberOfPerformedAccessorBinding);
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
        public void AssignReadOnlyOnlyPropertyOutsideConstructor_FieldAssignedFirst()
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

        [Fact]
        public void AssignReadOnlyOnlyPropertyOutsideConstructor_FieldAssignedAfterProperty()
        {
            var comp = CreateCompilation(@"
class Test
{
    int X
    {
        get
        {
            X = 3;
            field = 3;
            return 0;
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("Test").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(
                // (8,13): error CS0200: Property or indexer 'Test.X' cannot be assigned to -- it is read only
                //             X = 3;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "X").WithArguments("Test.X").WithLocation(8, 13),
                // PROTOTYPE(semi-auto-props):
                // Should the generated field not be readonly?
                // (9,13): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //             field = 3;
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(9, 13)
            );
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void AssignReadOnlyOnlyPropertyOutsideConstructor_FieldNotAssigned()
        {
            var comp = CreateCompilation(@"
class Test
{
    int X
    {
        get
        {
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
                // (8,13): error CS0200: Property or indexer 'Test.X' cannot be assigned to -- it is read only
                //             X = 3;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "X").WithArguments("Test.X").WithLocation(8, 13)
            );
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
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
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("Test").GetMembers().OfType<FieldSymbol>());
            CompileAndVerify(comp, expectedOutput: "3").VerifyDiagnostics();
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

        [Fact]
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
        public void InStaticLocalFunction()
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            return localFunc();

            static int localFunc() => field;
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics(
                // (10,39): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                //             static int localFunc() => field;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "field").WithLocation(10, 39)
            );
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void InLocalFunction()
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            return localFunc();
            int localFunc() => field;
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();
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
		IL_0001: call instance int32 C::'<get_P>g__localFunc|1_0'()
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
	.method private hidebysig 
		instance int32 '<get_P>g__localFunc|1_0' () cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x2060
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld int32 C::'<P>k__BackingField'
		IL_0006: ret
	} // end of method C::'<get_P>g__localFunc|1_0'
	// Properties
	.property instance int32 P()
	{
		.get instance int32 C::get_P()
	}
} // end of class C
");
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void InStaticLambda_PropertyIsStatic()
        {
            var comp = CreateCompilation(@"
using System;
public class C
{
    public static int P
    {
        get
        {
            Func<int> f = static () => field;
            Console.WriteLine(f());
            return 0;
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();
            VerifyTypeIL(comp, "C", @"
.class public auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed serializable beforefieldinit '<>c'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public static initonly class C/'<>c' '<>9'
		.field public static class [mscorlib]System.Func`1<int32> '<>9__1_0'
		// Methods
		.method private hidebysig specialname rtspecialname static 
			void .cctor () cil managed 
		{
			// Method begins at RVA 0x2084
			// Code size 11 (0xb)
			.maxstack 8
			IL_0000: newobj instance void C/'<>c'::.ctor()
			IL_0005: stsfld class C/'<>c' C/'<>c'::'<>9'
			IL_000a: ret
		} // end of method '<>c'::.cctor
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x207c
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c'::.ctor
		.method assembly hidebysig 
			instance int32 '<get_P>b__1_0' () cil managed 
		{
			// Method begins at RVA 0x2090
			// Code size 6 (0x6)
			.maxstack 8
			IL_0000: ldsfld int32 C::'<P>k__BackingField'
			IL_0005: ret
		} // end of method '<>c'::'<get_P>b__1_0'
	} // end of class <>c
	// Fields
	.field private static initonly int32 '<P>k__BackingField'
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Methods
	.method public hidebysig specialname static 
		int32 get_P () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 43 (0x2b)
		.maxstack 8
		IL_0000: ldsfld class [mscorlib]System.Func`1<int32> C/'<>c'::'<>9__1_0'
		IL_0005: dup
		IL_0006: brtrue.s IL_001f
		IL_0008: pop
		IL_0009: ldsfld class C/'<>c' C/'<>c'::'<>9'
		IL_000e: ldftn instance int32 C/'<>c'::'<get_P>b__1_0'()
		IL_0014: newobj instance void class [mscorlib]System.Func`1<int32>::.ctor(object, native int)
		IL_0019: dup
		IL_001a: stsfld class [mscorlib]System.Func`1<int32> C/'<>c'::'<>9__1_0'
		IL_001f: callvirt instance !0 class [mscorlib]System.Func`1<int32>::Invoke()
		IL_0024: call void [mscorlib]System.Console::WriteLine(int32)
		IL_0029: ldc.i4.0
		IL_002a: ret
	} // end of method C::get_P
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x207c
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method C::.ctor
	// Properties
	.property int32 P()
	{
		.get int32 C::get_P()
	}
} // end of class C
");
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void InLambda()
        {
            var comp = CreateCompilation(@"
using System;
public class C
{
    public int P
    {
        get
        {
            Func<int> f = () => field;
            Console.WriteLine(f());
            return 0;
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();
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
		// Code size 24 (0x18)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldftn instance int32 C::'<get_P>b__1_0'()
		IL_0007: newobj instance void class [mscorlib]System.Func`1<int32>::.ctor(object, native int)
		IL_000c: callvirt instance !0 class [mscorlib]System.Func`1<int32>::Invoke()
		IL_0011: call void [mscorlib]System.Console::WriteLine(int32)
		IL_0016: ldc.i4.0
		IL_0017: ret
	} // end of method C::get_P
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x2069
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method C::.ctor
	.method private hidebysig 
		instance int32 '<get_P>b__1_0' () cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x2071
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld int32 C::'<P>k__BackingField'
		IL_0006: ret
	} // end of method C::'<get_P>b__1_0'
	// Properties
	.property instance int32 P()
	{
		.get instance int32 C::get_P()
	}
} // end of class C
");
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
        public void FieldLocal_PropertyAssignedInConstructor()
        {
            var comp = CreateCompilation(@"
public class C
{
    public C()
    {
        P = 10;
    }

    public int P
    {
        get
        {
            int field = 0;
            return field;
        }
    }
}");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.VerifyDiagnostics(
                // (6,9): error CS0200: Property or indexer 'C.P' cannot be assigned to -- it is read only
                //         P = 10;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "P").WithArguments("C.P").WithLocation(6, 9)
                );
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory, CombinatorialData]
        public void InStruct(bool structTreeFirst)
        {
            var source1 = @"
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
";

            var source2 = @"
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
";
            var comp = CreateCompilation(new[] { source1, source2 });
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Equal("System.Int32 S_WithAutoProperty.<P>k__BackingField", comp.GetTypeByMetadataName("S_WithAutoProperty").GetMembers().OfType<FieldSymbol>().Single().ToTestDisplayString());
            Assert.Empty(comp.GetTypeByMetadataName("S_WithManualProperty").GetMembers().OfType<FieldSymbol>());
            Assert.Empty(comp.GetTypeByMetadataName("S_WithSemiAutoProperty").GetMembers().OfType<FieldSymbol>());

            if (structTreeFirst)
            {
                comp.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, comp.SyntaxTrees[0], filterSpanWithinTree: null, includeEarlierStages: true).Verify();
            }

            comp.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, comp.SyntaxTrees[1], filterSpanWithinTree: null, includeEarlierStages: true).Verify(
                // (7,33): error CS0165: Use of unassigned local variable 's1'
                //         S_WithAutoProperty s2 = s1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s1").WithArguments("s1").WithLocation(7, 33),
                // (19,37): error CS0165: Use of unassigned local variable 's1'
                //         S_WithSemiAutoProperty s2 = s1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s1").WithArguments("s1").WithLocation(19, 37)
            );

            if (!structTreeFirst)
            {
                comp.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, comp.SyntaxTrees[0], filterSpanWithinTree: null, includeEarlierStages: true).Verify();
            }

            comp.VerifyDiagnostics(
                // (7,33): error CS0165: Use of unassigned local variable 's1'
                //         S_WithAutoProperty s2 = s1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s1").WithArguments("s1").WithLocation(7, 33),
                // (19,37): error CS0165: Use of unassigned local variable 's1'
                //         S_WithSemiAutoProperty s2 = s1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s1").WithArguments("s1").WithLocation(19, 37)
            );

            Assert.Equal(structTreeFirst ? 0 : 1, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory, CombinatorialData]
        public void InStruct_AssignStructToDefault(bool structTreeFirst)
        {
            var source1 = @"
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

";

            var source2 = @"
class C
{
    void M1()
    {
        S_WithAutoProperty s1 = default;
        S_WithAutoProperty s2 = s1;
    }

    void M2()
    {
        S_WithManualProperty s1 = default;
        S_WithManualProperty s2 = s1;
    }

    void M3()
    {
        S_WithSemiAutoProperty s1 = default;
        S_WithSemiAutoProperty s2 = s1;
    }
}
";
            var comp = CreateCompilation(new[] { source1, source2 });
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Equal("System.Int32 S_WithAutoProperty.<P>k__BackingField", comp.GetTypeByMetadataName("S_WithAutoProperty").GetMembers().OfType<FieldSymbol>().Single().ToTestDisplayString());
            Assert.Empty(comp.GetTypeByMetadataName("S_WithManualProperty").GetMembers().OfType<FieldSymbol>());
            Assert.Empty(comp.GetTypeByMetadataName("S_WithSemiAutoProperty").GetMembers().OfType<FieldSymbol>());

            if (structTreeFirst)
            {
                comp.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, comp.SyntaxTrees[0], filterSpanWithinTree: null, includeEarlierStages: true).Verify();
            }

            comp.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, comp.SyntaxTrees[1], filterSpanWithinTree: null, includeEarlierStages: true).Verify();

            if (!structTreeFirst)
            {
                comp.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, comp.SyntaxTrees[0], filterSpanWithinTree: null, includeEarlierStages: true).Verify();
            }

            comp.VerifyDiagnostics();

            Assert.Equal(structTreeFirst ? 0 : 1, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory, CombinatorialData]
        public void InStruct_AssignStructViaConstructor(bool structTreeFirst)
        {
            var source1 = @"
struct S_WithAutoProperty
{
    public int P { get; set; }

    public S_WithAutoProperty() { }
}

struct S_WithManualProperty
{
    public int P { get => 0; set => _ = value; }

    public S_WithManualProperty() { }
}

struct S_WithSemiAutoProperty
{
    public int P { get => field; set => field = value; }

    public S_WithSemiAutoProperty() { }
}
";

            var source2 = @"
class C
{
    void M1()
    {
        S_WithAutoProperty s1 = new S_WithAutoProperty();
        S_WithAutoProperty s2 = s1;
    }

    void M2()
    {
        S_WithManualProperty s1 = new S_WithManualProperty();
        S_WithManualProperty s2 = s1;
    }

    void M3()
    {
        S_WithSemiAutoProperty s1 = new S_WithSemiAutoProperty();
        S_WithSemiAutoProperty s2 = s1;
    }
}
";
            var comp = CreateCompilation(new[] { source1, source2 });
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Equal("System.Int32 S_WithAutoProperty.<P>k__BackingField", comp.GetTypeByMetadataName("S_WithAutoProperty").GetMembers().OfType<FieldSymbol>().Single().ToTestDisplayString());
            Assert.Empty(comp.GetTypeByMetadataName("S_WithManualProperty").GetMembers().OfType<FieldSymbol>());
            Assert.Empty(comp.GetTypeByMetadataName("S_WithSemiAutoProperty").GetMembers().OfType<FieldSymbol>());

            if (structTreeFirst)
            {
                comp.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, comp.SyntaxTrees[0], filterSpanWithinTree: null, includeEarlierStages: true).Verify();
            }

            comp.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, comp.SyntaxTrees[1], filterSpanWithinTree: null, includeEarlierStages: true).Verify();

            if (!structTreeFirst)
            {
                comp.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, comp.SyntaxTrees[0], filterSpanWithinTree: null, includeEarlierStages: true).Verify();
            }

            comp.VerifyDiagnostics();

            Assert.Equal(structTreeFirst ? 0 : 1, accessorBindingData.NumberOfPerformedAccessorBinding);
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

        [Theory, CombinatorialData]
        public void InStruct_Cycle(bool bindS1First)
        {
            var source1 = @"
struct S1
{
    public S1() { }
    public S2 P { get => field; }
}
";
            var source2 = @"
struct S2
{
    public S2() { }
    public S1 P { get; }
}
";
            var comp = CreateCompilation(new[] { source1, source2 });
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, bindS1First ? comp.SyntaxTrees[0] : comp.SyntaxTrees[1], filterSpanWithinTree: null, includeEarlierStages: true).Verify(
                // (5,15): error CS0523: Struct member 'S1.P' of type 'S2' causes a cycle in the struct layout
                //     public S2 P { get => field; }
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "P").WithArguments(bindS1First ? "S1.P" : "S2.P", bindS1First ? "S2" : "S1").WithLocation(5, 15));

            Assert.Equal(bindS1First ? 0 : 1, accessorBindingData.NumberOfPerformedAccessorBinding);

            comp.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, bindS1First ? comp.SyntaxTrees[1] : comp.SyntaxTrees[0], filterSpanWithinTree: null, includeEarlierStages: true).Verify(
                // (5,15): error CS0523: Struct member 'S2.P' of type 'S1' causes a cycle in the struct layout
                //     public S1 P { get => field; }
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "P").WithArguments(bindS1First ? "S2.P" : "S1.P", bindS1First ? "S1" : "S2").WithLocation(5, 15));

            comp.VerifyDiagnostics(
                // (5,15): error CS0523: Struct member 'S1.P' of type 'S2' causes a cycle in the struct layout
                //     public S2 P { get; }
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "P").WithArguments("S1.P", "S2").WithLocation(5, 15),
                // (5,15): error CS0523: Struct member 'S2.P' of type 'S1' causes a cycle in the struct layout
                //     public S1 P { get; }
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "P").WithArguments("S2.P", "S1").WithLocation(5, 15)
                );
            Assert.Equal(bindS1First ? 0 : 1, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void InStruct_Cycle2()
        {
            var source1 = @"
struct S1
{
    public S1() { }
    public S2 P { get => field; }
}
";
            var source2 = @"
struct S2
{
    public S2() { }
    public S1 P { get => field; }
}
";
            var comp = CreateCompilation(new[] { source1, source2 });
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            comp.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, comp.SyntaxTrees[0], filterSpanWithinTree: null, includeEarlierStages: true).Verify(
                // (5,15): error CS0523: Struct member 'S1.P' of type 'S2' causes a cycle in the struct layout
                //     public S2 P { get; }
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "P").WithArguments("S1.P", "S2").WithLocation(5, 15)
                );

            comp.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, comp.SyntaxTrees[1], filterSpanWithinTree: null, includeEarlierStages: true).Verify(
                // (5,15): error CS0523: Struct member 'S2.P' of type 'S1' causes a cycle in the struct layout
                //     public S1 P { get; }
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "P").WithArguments("S2.P", "S1").WithLocation(5, 15)
                );

            comp.VerifyDiagnostics(
                // (5,15): error CS0523: Struct member 'S1.P' of type 'S2' causes a cycle in the struct layout
                //     public S2 P { get; }
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "P").WithArguments("S1.P", "S2").WithLocation(5, 15),
                // (5,15): error CS0523: Struct member 'S2.P' of type 'S1' causes a cycle in the struct layout
                //     public S1 P { get; }
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "P").WithArguments("S2.P", "S1").WithLocation(5, 15)
                );
            Assert.Equal(1, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void InStruct_NoCycle()
        {
            var source1 = @"
struct S1
{
    public S1() { }

    public int P
    {
        get
        {
            _ = field;
            var x = new S2();
            return field;
        }
    }
}";

            var source2 = @"
struct S2
{
    public S2() { }

    public int P
    {
        get
        {
            _ = field;
            var x = new S1();
            return field;
        }
    }
}";
            var comp = CreateCompilation(new[] { source1, source2 });
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, comp.SyntaxTrees[0], filterSpanWithinTree: null, includeEarlierStages: true).Verify();
            Assert.Equal(1, accessorBindingData.NumberOfPerformedAccessorBinding);
            comp.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, comp.SyntaxTrees[1], filterSpanWithinTree: null, includeEarlierStages: true).Verify();
            Assert.Equal(1, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory, CombinatorialData]
        public void InStruct_NoCycleBecauseStatic(bool firstIsSemi, bool secondIsSemi)
        {
            var firstAccessor = firstIsSemi ? "get => field;" : "get;";
            var secondAccessor = secondIsSemi ? "get => field;" : "get;";
            var comp = CreateCompilation($@"
struct S1
{{
    public S1() {{ }}
    public static S2 P {{ {firstAccessor} }}
}}

struct S2
{{
    public S2() {{ }}
    public static S1 P {{ {secondAccessor} }}
}}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.VerifyDiagnostics();
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void InStruct_SelfCycle()
        {
            var comp = CreateCompilation(@"
struct S
{
    public S() { }
    public S P { get => field; }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            comp.VerifyDiagnostics(
                // (5,14): error CS0523: Struct member 'S.P' of type 'S' causes a cycle in the struct layout
                //     public S P { get => field; }
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "P").WithArguments("S.P", "S").WithLocation(5, 14)
                );
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "never")]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "always")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "never")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "always")]
        public void SpeculativeSemanticModel_FieldInRegularAccessor_BindOriginalFirst(SpeculativeBindingOption bindingOption, string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
class C
{
    public int P { get => 0; }
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
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
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, bindingOption);

            var typeInfo = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "?" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);
            Assert.Null(fieldKeywordSymbolInfo.Symbol);
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
        }

        [Theory]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "never")]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "always")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "never")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "always")]
        public void SpeculativeSemanticModel_FieldInRegularAccessor_BindSpeculatedFirst(SpeculativeBindingOption bindingOption, string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
class C
{
    public int P { get => 0; }
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, bindingOption);
            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);

            var typeInfo = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "?" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            Assert.Null(fieldKeywordSymbolInfo.Symbol);
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory, CombinatorialData]
        public void SpeculativeSemanticModel_FieldInRegularAccessor_LocalFunctionOrLambda_BindOriginalFirst(
            SpeculativeBindingOption bindingOption,
            [CombinatorialValues("int f() => 0;", "System.Func<int> f = () => 0;")] string localFunctionOrLambda,
            [CombinatorialValues("always", "never")] string runNullableAnalysis)
        {
            var comp = CreateCompilation($@"
class C
{{
    public int P
    {{
        get
        {{
            {localFunctionOrLambda}
            return f();
        }}
    }}
}}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
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
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, bindingOption);
            Assert.True(fieldKeywordSymbolInfo2.IsEmpty);
            Assert.Null(fieldKeywordSymbolInfo2.Symbol);

            var typeInfo = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "?" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
        }

        [Theory, CombinatorialData]
        public void SpeculativeSemanticModel_FieldInRegularAccessor_LocalFunctionOrLambda_BindSpeculatedFirst(
            SpeculativeBindingOption bindingOption,
            [CombinatorialValues("int f() => 0;", "System.Func<int> f = () => 0;")] string localFunctionOrLambda,
            [CombinatorialValues("always", "never")] string runNullableAnalysis)
        {
            var comp = CreateCompilation($@"
class C
{{
    public int P
    {{
        get
        {{
            {localFunctionOrLambda}
            return f();
        }}
    }}
}}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, bindingOption);
            Assert.True(fieldKeywordSymbolInfo2.IsEmpty);
            Assert.Null(fieldKeywordSymbolInfo2.Symbol);

            var typeInfo = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "?" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            Assert.Null(fieldKeywordSymbolInfo.Symbol);
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory, CombinatorialData]
        public void SpeculativeSemanticModel_FieldInSemiAutoPropertyAccessor_LocalFunctionOrLambda_BindOriginalFirst(
            SpeculativeBindingOption bindingOption,
            [CombinatorialValues("int f() => 0;", "System.Func<int> f = () => 0;")] string localFunctionOrLambda,
            [CombinatorialValues("always", "never")] string runNullableAnalysis)
        {
            var comp = CreateCompilation($@"
class C
{{
    public int P
    {{
        get
        {{
            {localFunctionOrLambda}
            return field + f();
        }}
    }}
}}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);
            Assert.Equal("System.Int32 C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, bindingOption);
            if (bindingOption == SpeculativeBindingOption.BindAsTypeOrNamespace)
            {
                Assert.True(fieldKeywordSymbolInfo2.IsEmpty);
                Assert.Null(fieldKeywordSymbolInfo2.Symbol);
            }
            else
            {
                Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);
            }

            Assert.Equal(comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single(), fieldKeywordSymbolInfo.Symbol.GetSymbol());

            var typeInfo = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "System.Int32" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? TypeKind.Struct : TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
            Assert.Equal(comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single(), fieldKeywordSymbolInfo.Symbol.GetSymbol());
        }

        [Theory, CombinatorialData]
        public void SpeculativeSemanticModel_FieldInSemiAutoPropertyAccessor_LocalFunctionOrLambda_BindSpeculatedFirst(
            SpeculativeBindingOption bindingOption,
            [CombinatorialValues("int f() => 0;", "System.Func<int> f = () => 0;")] string localFunctionOrLambda,
            [CombinatorialValues("always", "never")] string runNullableAnalysis)
        {
            var comp = CreateCompilation($@"
class C
{{
    public int P
    {{
        get
        {{
            {localFunctionOrLambda}
            return field + f();
        }}
    }}
}}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, bindingOption);
            if (bindingOption == SpeculativeBindingOption.BindAsTypeOrNamespace)
            {
                Assert.True(fieldKeywordSymbolInfo2.IsEmpty);
                Assert.Null(fieldKeywordSymbolInfo2.Symbol);
            }
            else
            {
                Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);
            }

            Assert.Equal(comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single(), fieldKeywordSymbolInfo.Symbol.GetSymbol());

            var typeInfo = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "System.Int32" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? TypeKind.Struct : TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Equal("System.Int32 C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());
            Assert.Equal(comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single(), fieldKeywordSymbolInfo.Symbol.GetSymbol());
            Assert.Equal(runNullableAnalysis == "never" ? 1 : 0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "never")]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "always")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "never")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "always")]
        public void SpeculativeSemanticModel_FieldInExpressionBodied_BindOriginalFirst(SpeculativeBindingOption bindingOption, string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
class C
{
    public int P => 0;
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var arrowClause = SyntaxFactory.ArrowExpressionClause(SyntaxFactory.ParseExpression("field"));
            model.TryGetSpeculativeSemanticModel(token.SpanStart, arrowClause, out var speculativeModel);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(arrowClause.Expression);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(token.SpanStart, arrowClause.Expression, bindingOption);
            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);
            Assert.Null(fieldKeywordSymbolInfo.Symbol);

            var typeInfo = model.GetSpeculativeTypeInfo(token.SpanStart, arrowClause.Expression, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "?" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(token.SpanStart, arrowClause.Expression, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
        }

        [Theory]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "never")]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "always")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "never")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "always")]
        public void SpeculativeSemanticModel_FieldInExpressionBodied_BindSpeculatedFirst(SpeculativeBindingOption bindingOption, string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
class C
{
    public int P => 0;
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var arrowClause = SyntaxFactory.ArrowExpressionClause(SyntaxFactory.ParseExpression("field"));
            model.TryGetSpeculativeSemanticModel(token.SpanStart, arrowClause, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(arrowClause.Expression);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(token.SpanStart, arrowClause.Expression, bindingOption);
            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);

            var typeInfo = model.GetSpeculativeTypeInfo(token.SpanStart, arrowClause.Expression, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "?" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(token.SpanStart, arrowClause.Expression, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            Assert.Null(fieldKeywordSymbolInfo.Symbol);
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "never")]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "always")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "never")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "always")]
        public void SpeculativeSemanticModel_FieldInExpressionBodiedProperty_BindOriginalFirst(SpeculativeBindingOption bindingOption, string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
class C
{
    public int P => 0;
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
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
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, bindingOption);
            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);
            Assert.Null(fieldKeywordSymbolInfo.Symbol);

            var typeInfo = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "?" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
        }

        [Theory]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "never")]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "always")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "never")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "always")]
        public void SpeculativeSemanticModel_FieldInExpressionBodiedProperty_BindSpeculatedFirst(SpeculativeBindingOption bindingOption, string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
class C
{
    public int P => 0;
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, bindingOption);
            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);

            var typeInfo = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "?" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            Assert.Null(fieldKeywordSymbolInfo.Symbol);
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void SpeculativeSemanticModel_FieldIdentifierInRegularAccessor_ReplaceBlock_BindOriginalFirst()
        {
            var comp = CreateCompilation(@"
class C
{
    public int P
    {
        get
        {
            return 0;
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
            var block = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    public int P
    {
        get
        {
            int field = 0;
            return field;
        }
    }
}").GetRoot().DescendantNodes().OfType<BlockSyntax>().Single();
            model.TryGetSpeculativeSemanticModel(token.SpanStart, block, out var speculativeModel);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());

            var fieldIdentifierSymbolInfo = speculativeModel.GetSymbolInfo(block.DescendantNodes().OfType<IdentifierNameSyntax>().Single());
            Assert.Equal(SymbolKind.Local, fieldIdentifierSymbolInfo.Symbol.Kind);

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
        }

        [Fact]
        public void SpeculativeSemanticModel_FieldIdentifierInRegularAccessor_ReplaceBlock_BindSpeculatedFirst()
        {
            var comp = CreateCompilation(@"
class C
{
    public int P
    {
        get
        {
            return 0;
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var block = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    public int P
    {
        get
        {
            int field = 0;
            return field;
        }
    }
}").GetRoot().DescendantNodes().OfType<BlockSyntax>().Single();
            model.TryGetSpeculativeSemanticModel(token.SpanStart, block, out var speculativeModel);
            var fieldIdentifierSymbolInfo = speculativeModel.GetSymbolInfo(block.DescendantNodes().OfType<IdentifierNameSyntax>().Single());
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            Assert.Equal(SymbolKind.Local, fieldIdentifierSymbolInfo.Symbol.Kind);
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void SpeculativeSemanticModel_FieldKeywordInRegularAccessor_ReplaceBlock_BindOriginalFirst()
        {
            var comp = CreateCompilation(@"
class C
{
    public int P
    {
        get
        {
            return 0;
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
            var block = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    public int P
    {
        get
        {
            return field;
        }
    }
}").GetRoot().DescendantNodes().OfType<BlockSyntax>().Single();
            model.TryGetSpeculativeSemanticModel(token.SpanStart, block, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(block.DescendantNodes().OfType<IdentifierNameSyntax>().Single());
            Assert.Null(fieldKeywordSymbolInfo.Symbol);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void SpeculativeSemanticModel_FieldKeywordInRegularAccessor_ReplaceBlock_BindSpeculatedFirst()
        {
            var comp = CreateCompilation(@"
class C
{
    public int P
    {
        get
        {
            return 0;
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var block = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    public int P
    {
        get
        {
            return field;
        }
    }
}").GetRoot().DescendantNodes().OfType<BlockSyntax>().Single();
            model.TryGetSpeculativeSemanticModel(token.SpanStart, block, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(block.DescendantNodes().OfType<IdentifierNameSyntax>().Single());
            Assert.Null(fieldKeywordSymbolInfo.Symbol);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory, CombinatorialData]
        public void SpeculativeSemanticModel_FieldKeywordInRegularAccessor_ReplaceBlock_LocalFunction_BindOriginalFirst([CombinatorialValues("never", "always")] string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
class C
{
    public int P
    {
        get
        {
            return localFunction();

            int localFunction() { return 0; }
        }
    }
}
", parseOptions: TestOptions.RegularPreview.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var block = (BlockSyntax)SyntaxFactory.ParseSyntaxTree(@"
class C
{
    public int P
    {
        get
        {
            return localFunction();

            int localFunction() { return field; }
        }
    }
}").GetRoot().DescendantNodes().Single(s => s is BlockSyntax && s.Parent is LocalFunctionStatementSyntax);
            model.TryGetSpeculativeSemanticModel(token.SpanStart, block, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(block.DescendantNodes().OfType<IdentifierNameSyntax>().Single());
            Assert.Null(fieldKeywordSymbolInfo.Symbol);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory, CombinatorialData]
        public void SpeculativeSemanticModel_FieldKeywordInRegularAccessor_ReplaceBlock_LocalFunction_BindSpeculatedFirst([CombinatorialValues("never", "always")] string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
class C
{
    public int P
    {
        get
        {
            return localFunction();

            int localFunction() { return 0; }
        }
    }
}
", parseOptions: TestOptions.RegularPreview.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var block = (BlockSyntax)SyntaxFactory.ParseSyntaxTree(@"
class C
{
    public int P
    {
        get
        {
            return localFunction();

            int localFunction() { return field; }
        }
    }
}").GetRoot().DescendantNodes().Single(s => s is BlockSyntax && s.Parent is LocalFunctionStatementSyntax);
            model.TryGetSpeculativeSemanticModel(token.SpanStart, block, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(block.DescendantNodes().OfType<IdentifierNameSyntax>().Single());
            Assert.Null(fieldKeywordSymbolInfo.Symbol);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "never")]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "always")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "never")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "always")]
        public void SpeculativeSemanticModel_FieldInAccessorUsingField_BindOriginalFirst(SpeculativeBindingOption bindingOption, string runNullableAnalysis)
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
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
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
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, bindingOption);
            if (bindingOption == SpeculativeBindingOption.BindAsExpression)
            {
                Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);
            }
            else
            {
                Assert.Equal(SpeculativeBindingOption.BindAsTypeOrNamespace, bindingOption);
                Assert.Null(fieldKeywordSymbolInfo2.Symbol);
                Assert.True(fieldKeywordSymbolInfo2.IsEmpty);
            }

            var typeInfo = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "System.Double" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? TypeKind.Struct : TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Equal("System.Double C.<P>k__BackingField", fieldKeywordSymbolInfo.Symbol.ToTestDisplayString(includeNonNullable: true));
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
            Assert.Same(comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single(), fieldKeywordSymbolInfo.Symbol.GetSymbol());
        }

        [Theory]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "never")]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "always")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "never")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "always")]
        public void SpeculativeSemanticModel_FieldInAccessorUsingField_BindSpeculatedFirst(SpeculativeBindingOption bindingOption, string runNullableAnalysis)
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
", parseOptions: TestOptions.RegularPreview.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, bindingOption);
            if (bindingOption == SpeculativeBindingOption.BindAsExpression)
            {
                Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);
            }
            else
            {
                Assert.Equal(SpeculativeBindingOption.BindAsTypeOrNamespace, bindingOption);
                Assert.Null(fieldKeywordSymbolInfo2.Symbol);
                Assert.True(fieldKeywordSymbolInfo2.IsEmpty);
            }

            var typeInfo = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "System.Double" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? TypeKind.Struct : TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Equal("System.Double C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());
            Assert.Same(comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single(), fieldKeywordSymbolInfo.Symbol.GetSymbol());
            Assert.Equal(runNullableAnalysis == "always" ? 0 : 1, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void SpeculativeSemanticModel_FieldKeywordInRegularAccessor_ReplaceBlock_MethodBodySemanticModel_BindOriginalFirst()
        {
            var comp = CreateCompilation(@"
class C
{
    public int P
    {
        get
        {
            return 0;
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
            var accessor = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    public int P
    {
        get
        {
            return field;
        }
    }
}").GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Single();
            model.TryGetSpeculativeSemanticModelForMethodBody(token.SpanStart, accessor, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(accessor.DescendantNodes().OfType<IdentifierNameSyntax>().Single());
            Assert.Null(fieldKeywordSymbolInfo.Symbol);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void SpeculativeSemanticModel_FieldKeywordInRegularAccessor_ReplaceBlock_MethodBodySemanticModel_BindSpeculatedFirst()
        {
            var comp = CreateCompilation(@"
class C
{
    public int P
    {
        get
        {
            return 0;
        }
    }
}
");
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken));

            var model = comp.GetSemanticModel(tree);
            var accessor = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    public int P
    {
        get
        {
            return field;
        }
    }
}").GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Single();
            model.TryGetSpeculativeSemanticModelForMethodBody(token.SpanStart, accessor, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(accessor.DescendantNodes().OfType<IdentifierNameSyntax>().Single());
            Assert.Null(fieldKeywordSymbolInfo.Symbol);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData("get => 0; set => field = value;", FieldBindingTestState.BecomesBackingField)]
        [InlineData("set => field = value; get => 0;", FieldBindingTestState.BecomesBackingField)]
        [InlineData("get { return 0; } set { field = value; }", FieldBindingTestState.BecomesBackingField)]
        [InlineData("set { field = value; } get { return 0; }", FieldBindingTestState.BecomesBackingField)]
        [InlineData("get => 0; set { field = value; }", FieldBindingTestState.BecomesBackingField)]
        [InlineData("set { field = value; } get => 0;", FieldBindingTestState.BecomesBackingField)]
        [InlineData("get { return 0; } set => field = value;", FieldBindingTestState.BecomesBackingField)]
        [InlineData("set => field = value; get { return 0; }", FieldBindingTestState.BecomesBackingField)]
        [InlineData("get { double field = 0; return field; } set => field = value;", FieldBindingTestState.BecomesLocal)]
        [InlineData("set => field = value; get { double field = 0; return field; }", FieldBindingTestState.BecomesLocal)]
        public void SpeculativeSemanticModel_FieldInGetterNotUsingFieldWhereSetterAccessorDoes_BindOriginalFirst(string accessors, FieldBindingTestState bindingState)
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
            var fieldKeywordSymbolInfoAsExpression = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfoAsExpression);

            var fieldKeywordSymbolInfoAsType = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(fieldKeywordSymbolInfoAsType.Symbol);
            if (bindingState == FieldBindingTestState.BecomesLocal)
            {
                Assert.Equal(CandidateReason.NotATypeOrNamespace, fieldKeywordSymbolInfoAsType.CandidateReason);
                Assert.Equal("System.Double field", fieldKeywordSymbolInfoAsType.CandidateSymbols.Single().GetSymbol().ToTestDisplayString());
            }
            else
            {
                Assert.True(fieldKeywordSymbolInfoAsType.IsEmpty);
            }

            var typeInfoAsExpression = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal("System.Double", typeInfoAsExpression.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, typeInfoAsExpression.Type.TypeKind);
            Assert.Equal(typeInfoAsExpression.Type, typeInfoAsExpression.ConvertedType);

            var typeInfoAsType = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Equal("field", typeInfoAsType.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfoAsType.Type.TypeKind);
            Assert.Equal(typeInfoAsType.Type, typeInfoAsType.ConvertedType);

            var aliasInfoAsExpression = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            var aliasInfoAsType = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(aliasInfoAsExpression);
            Assert.Null(aliasInfoAsType);

            if (bindingState == FieldBindingTestState.BecomesLocal)
            {
                Assert.Equal(SymbolKind.Local, fieldKeywordSymbolInfo.Symbol.Kind);
                Assert.Equal("System.Double field", fieldKeywordSymbolInfo.Symbol.ToTestDisplayString(includeNonNullable: true));
                Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
                Assert.Equal("System.Double C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());
            }
            else
            {
                Assert.Equal(FieldBindingTestState.BecomesBackingField, bindingState);
                Assert.Equal("System.Double C.<P>k__BackingField", fieldKeywordSymbolInfo.Symbol.ToTestDisplayString(includeNonNullable: true));
                Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
                Assert.Same(comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single(), fieldKeywordSymbolInfo.Symbol.GetSymbol());
            }
        }

        [Theory]
        [InlineData("get => 0; set => field = value;", FieldBindingTestState.BecomesBackingField)]
        [InlineData("set => field = value; get => 0;", FieldBindingTestState.BecomesBackingField)]
        [InlineData("get { return 0; } set { field = value; }", FieldBindingTestState.BecomesBackingField)]
        [InlineData("set { field = value; } get { return 0; }", FieldBindingTestState.BecomesBackingField)]
        [InlineData("get => 0; set { field = value; }", FieldBindingTestState.BecomesBackingField)]
        [InlineData("set { field = value; } get => 0;", FieldBindingTestState.BecomesBackingField)]
        [InlineData("get { return 0; } set => field = value;", FieldBindingTestState.BecomesBackingField)]
        [InlineData("set => field = value; get { return 0; }", FieldBindingTestState.BecomesBackingField)]
        [InlineData("get { int field = 0; return field; } set => field = value;", FieldBindingTestState.BecomesLocal)]
        [InlineData("set => field = value; get { int field = 0; return field; }", FieldBindingTestState.BecomesLocal)]
        public void SpeculativeSemanticModel_FieldInGetterNotUsingFieldWhereSetterAccessorDoes_BindSpeculatedFirst(string accessors, FieldBindingTestState bindingState)
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
            var fieldKeywordSymbolInfoAsExpression = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfoAsExpression);

            var fieldKeywordSymbolInfoAsType = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(fieldKeywordSymbolInfoAsType.Symbol);
            if (bindingState == FieldBindingTestState.BecomesLocal)
            {
                Assert.Equal(CandidateReason.NotATypeOrNamespace, fieldKeywordSymbolInfoAsType.CandidateReason);
                Assert.Equal("System.Int32 field", fieldKeywordSymbolInfoAsType.CandidateSymbols.Single().GetSymbol().ToTestDisplayString());
            }
            else
            {
                Assert.True(fieldKeywordSymbolInfoAsType.IsEmpty);
            }

            var typeInfoAsExpression = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(bindingState == FieldBindingTestState.BecomesLocal ? "System.Int32" : "System.Double", typeInfoAsExpression.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, typeInfoAsExpression.Type.TypeKind);
            Assert.Equal(typeInfoAsExpression.Type, typeInfoAsExpression.ConvertedType);

            var typeInfoAsType = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Equal("field", typeInfoAsType.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfoAsType.Type.TypeKind);
            Assert.Equal(typeInfoAsType.Type, typeInfoAsType.ConvertedType);

            var aliasInfoAsExpression = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            var aliasInfoAsType = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(aliasInfoAsExpression);
            Assert.Null(aliasInfoAsType);

            if (bindingState == FieldBindingTestState.BecomesLocal)
            {
                Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
            }
            else
            {
                Assert.Equal(FieldBindingTestState.BecomesBackingField, bindingState);
                Assert.Equal(1, accessorBindingData.NumberOfPerformedAccessorBinding);
            }

            Assert.Equal("System.Double C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());
            if (bindingState == FieldBindingTestState.BecomesLocal)
            {
                Assert.Equal("System.Int32 field", fieldKeywordSymbolInfo.Symbol.GetSymbol().ToTestDisplayString());
                Assert.Equal(SymbolKind.Local, fieldKeywordSymbolInfo.Symbol.Kind);
                Assert.Equal(2, accessorBindingData.NumberOfPerformedAccessorBinding);
            }
            else
            {
                Assert.Same(comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single(), fieldKeywordSymbolInfo.Symbol.GetSymbol());
                Assert.Equal(1, accessorBindingData.NumberOfPerformedAccessorBinding);
            }
        }

        [Theory]
        [InlineData("get => 0; get => field;", 19, FieldBindingTestState.None)]
        [InlineData("get => field; get => 0;", 23, FieldBindingTestState.BecomesBackingField)]
        [InlineData("get { return 0; } get => field;", 27, FieldBindingTestState.None)]
        [InlineData("get => field; get { return 0; }", 23, FieldBindingTestState.BecomesBackingField)]
        [InlineData("get => 0; get { return field; }", 19, FieldBindingTestState.None)]
        [InlineData("get { return field; } get => 0;", 31, FieldBindingTestState.BecomesBackingField)]
        [InlineData("get { return 0; } get { return field; }", 27, FieldBindingTestState.None)]
        [InlineData("get { return field; } get { return 0; }", 31, FieldBindingTestState.BecomesBackingField)]
        [InlineData("get { double field = 0; return field; } get { return field; }", 49, FieldBindingTestState.BecomesLocal)]
        [InlineData("get { return field; } get { double field = 0; return field; }", 31, FieldBindingTestState.BecomesBackingField)]
        public void SpeculativeSemanticModel_FieldInDuplicateAccessorWhereOneAccessorUsesField_BindOriginalFirst(string accessors, int diagnosticColumn, FieldBindingTestState bindingState)
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
            if (bindingState == FieldBindingTestState.BecomesBackingField)
            {
                Assert.Equal("System.Double C.<P>k__BackingField", fieldsToEmit.Single().ToTestDisplayString());
            }
            else
            {
                Assert.Empty(fieldsToEmit);
            }

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);
            var fieldKeywordSymbolInfoAsExpression = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfoAsExpression);
            if (bindingState == FieldBindingTestState.BecomesBackingField)
            {
                Assert.Equal("System.Double C.<P>k__BackingField", fieldKeywordSymbolInfo.Symbol.ToTestDisplayString(includeNonNullable: true));
            }
            else if (bindingState == FieldBindingTestState.BecomesLocal)
            {
                Assert.Equal(SymbolKind.Local, fieldKeywordSymbolInfo.Symbol.Kind);
                Assert.Equal("System.Double field", fieldKeywordSymbolInfo.Symbol.GetSymbol().ToTestDisplayString());
            }
            else
            {
                Assert.Equal(FieldBindingTestState.None, bindingState);
                Assert.Null(fieldKeywordSymbolInfo.Symbol);
            }

            var fieldKeywordSymbolInfoAsType = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(fieldKeywordSymbolInfoAsType.Symbol);
            if (bindingState == FieldBindingTestState.BecomesLocal)
            {
                Assert.Equal(CandidateReason.NotATypeOrNamespace, fieldKeywordSymbolInfoAsType.CandidateReason);
                Assert.Equal("System.Double field", fieldKeywordSymbolInfoAsType.CandidateSymbols.Single().GetSymbol().ToTestDisplayString());
            }
            else
            {
                Assert.True(fieldKeywordSymbolInfoAsType.IsEmpty);
            }

            var typeInfoAsExpression = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(bindingState == FieldBindingTestState.None ? "?" : "System.Double", typeInfoAsExpression.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(bindingState == FieldBindingTestState.None ? TypeKind.Error : TypeKind.Struct, typeInfoAsExpression.Type.TypeKind);
            Assert.Equal(typeInfoAsExpression.Type, typeInfoAsExpression.ConvertedType);

            var typeInfoAsType = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Equal("field", typeInfoAsType.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfoAsType.Type.TypeKind);
            Assert.Equal(typeInfoAsType.Type, typeInfoAsType.ConvertedType);

            var aliasInfoAsExpression = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            var aliasInfoAsType = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(aliasInfoAsExpression);
            Assert.Null(aliasInfoAsType);

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);

            if (bindingState == FieldBindingTestState.BecomesBackingField)
            {
                Assert.Same(comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single(), fieldKeywordSymbolInfo.Symbol.GetSymbol());
            }
            else
            {
                Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            }
        }

        [Theory]
        [InlineData("get => 0; get => field;", FieldBindingTestState.None)]
        [InlineData("get => field; get => 0;", FieldBindingTestState.BecomesBackingField)]
        [InlineData("get { return 0; } get => field;", FieldBindingTestState.None)]
        [InlineData("get => field; get { return 0; }", FieldBindingTestState.BecomesBackingField)]
        [InlineData("get => 0; get { return field; }", FieldBindingTestState.None)]
        [InlineData("get { return field; } get => 0;", FieldBindingTestState.BecomesBackingField)]
        [InlineData("get { return 0; } get { return field; }", FieldBindingTestState.None)]
        [InlineData("get { return field; } get { return 0; }", FieldBindingTestState.BecomesBackingField)]
        [InlineData("get { int field = 0; return field; } get { return field; }", FieldBindingTestState.BecomesLocal)]
        [InlineData("get { return field; } get { int field = 0; return field; }", FieldBindingTestState.BecomesBackingField)]
        public void SpeculativeSemanticModel_FieldInDuplicateAccessorWhereOneAccessorUsesField_BindSpeculatedFirst(string accessors, FieldBindingTestState bindingState)
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
            var fieldKeywordSymbolInfoAsExpression = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfoAsExpression);

            var fieldKeywordSymbolInfoAsType = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(fieldKeywordSymbolInfoAsType.Symbol);
            if (bindingState == FieldBindingTestState.BecomesLocal)
            {
                Assert.Equal(CandidateReason.NotATypeOrNamespace, fieldKeywordSymbolInfoAsType.CandidateReason);
                Assert.Equal("System.Int32 field", fieldKeywordSymbolInfoAsType.CandidateSymbols.Single().GetSymbol().ToTestDisplayString());
            }
            else
            {
                Assert.True(fieldKeywordSymbolInfoAsType.IsEmpty);
            }

            var typeInfoAsExpression = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            var displayString = bindingState switch
            {
                FieldBindingTestState.None => "?",
                FieldBindingTestState.BecomesLocal => "System.Int32",
                FieldBindingTestState.BecomesBackingField => "System.Double",
                _ => throw new ArgumentException("Unexpected value.", nameof(bindingState))
            };
            Assert.Equal(displayString, typeInfoAsExpression.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(bindingState == FieldBindingTestState.None ? TypeKind.Error : TypeKind.Struct, typeInfoAsExpression.Type.TypeKind);
            Assert.Equal(typeInfoAsExpression.Type, typeInfoAsExpression.ConvertedType);

            var typeInfoAsType = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Equal("field", typeInfoAsType.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfoAsType.Type.TypeKind);
            Assert.Equal(typeInfoAsType.Type, typeInfoAsType.ConvertedType);

            var aliasInfoAsExpression = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            var aliasInfoAsType = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(aliasInfoAsExpression);
            Assert.Null(aliasInfoAsType);

            if (bindingState == FieldBindingTestState.BecomesLocal)
            {
                Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
                Assert.Equal(SymbolKind.Local, fieldKeywordSymbolInfo.Symbol.Kind);
                Assert.Equal(1, accessorBindingData.NumberOfPerformedAccessorBinding);
            }
            else if (bindingState == FieldBindingTestState.BecomesBackingField)
            {
                Assert.Equal("System.Double C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());
                Assert.Same(comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single(), fieldKeywordSymbolInfo.Symbol.GetSymbol());
                Assert.Equal(1, accessorBindingData.NumberOfPerformedAccessorBinding);
            }
            else
            {
                Assert.Equal(FieldBindingTestState.None, bindingState);
                Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
                Assert.Null(fieldKeywordSymbolInfo.Symbol);
                Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
            }
        }

        [Theory]
        [InlineData("get => 0; get => 1;", 19, 0, FieldBindingTestState.None)]
        [InlineData("get => 0; get => 1;", 19, 1, FieldBindingTestState.None)]
        [InlineData("get { return 0; } get => 1;", 27, 0, FieldBindingTestState.None)]
        [InlineData("get { return 0; } get => 1;", 27, 1, FieldBindingTestState.None)]
        [InlineData("get => 0; get { return 1; }", 19, 0, FieldBindingTestState.None)]
        [InlineData("get => 0; get { return 1; }", 19, 1, FieldBindingTestState.None)]
        [InlineData("get { return 0; } get { return 1; }", 27, 0, FieldBindingTestState.None)]
        [InlineData("get { return 0; } get { return 1; }", 27, 1, FieldBindingTestState.None)]
        [InlineData("get { double field = 0; return field; } get { return 1; }", 49, 0, FieldBindingTestState.BecomesLocal)]
        [InlineData("get { double field = 0; return field; } get { return 1; }", 49, 1, FieldBindingTestState.None)]
        public void SpeculativeSemanticModel_TwoGettersNotUsingFieldKeyword_BindOriginalFirst(string accessors, int diagnosticColumn, int numericLiteralToSpeculate, FieldBindingTestState bindingState)
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
            var fieldKeywordSymbolInfoAsExpression = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfoAsExpression);

            var fieldKeywordSymbolInfoAsType = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(fieldKeywordSymbolInfoAsType.Symbol);
            if (bindingState == FieldBindingTestState.BecomesLocal)
            {
                Assert.Equal(CandidateReason.NotATypeOrNamespace, fieldKeywordSymbolInfoAsType.CandidateReason);
                Assert.Equal("System.Double field", fieldKeywordSymbolInfoAsType.CandidateSymbols.Single().GetSymbol().ToTestDisplayString());
            }
            else
            {
                Assert.True(fieldKeywordSymbolInfoAsType.IsEmpty);
            }

            var typeInfoAsExpression = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(bindingState == FieldBindingTestState.None ? "?" : "System.Double", typeInfoAsExpression.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(bindingState == FieldBindingTestState.None ? TypeKind.Error : TypeKind.Struct, typeInfoAsExpression.Type.TypeKind);
            Assert.Equal(typeInfoAsExpression.Type, typeInfoAsExpression.ConvertedType);

            var typeInfoAsType = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Equal("field", typeInfoAsType.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfoAsType.Type.TypeKind);
            Assert.Equal(typeInfoAsType.Type, typeInfoAsType.ConvertedType);

            var aliasInfoAsExpression = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            var aliasInfoAsType = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(aliasInfoAsExpression);
            Assert.Null(aliasInfoAsType);

            if (bindingState == FieldBindingTestState.BecomesLocal)
            {
                Assert.Equal(SymbolKind.Local, fieldKeywordSymbolInfo.Symbol.Kind);
                Assert.Equal("System.Double field", fieldKeywordSymbolInfo.Symbol.GetSymbol().ToTestDisplayString());
            }
            else
            {
                Assert.Equal(FieldBindingTestState.None, bindingState);
                Assert.Null(fieldKeywordSymbolInfo.Symbol);
            }

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
        }

        [Theory]
        [InlineData("get => 0; get => 1;", 0, FieldBindingTestState.None, 0)]
        [InlineData("get => 0; get => 1;", 1, FieldBindingTestState.None, 0)]
        [InlineData("get { return 0; } get => 1;", 0, FieldBindingTestState.None, 0)]
        [InlineData("get { return 0; } get => 1;", 1, FieldBindingTestState.None, 0)]
        [InlineData("get => 0; get { return 1; }", 0, FieldBindingTestState.None, 0)]
        [InlineData("get => 0; get { return 1; }", 1, FieldBindingTestState.None, 0)]
        [InlineData("get { return 0; } get { return 1; }", 0, FieldBindingTestState.None, 0)]
        [InlineData("get { return 0; } get { return 1; }", 1, FieldBindingTestState.None, 0)]
        [InlineData("get { int field = 0; return field; } get { return 1; }", 0, FieldBindingTestState.BecomesLocal, 1)]
        [InlineData("get { int field = 0; return field; } get { return 1; }", 1, FieldBindingTestState.None, 1)]
        public void SpeculativeSemanticModel_TwoGettersNotUsingFieldKeyword_BindSpeculatedFirst(string accessors, int numericLiteralToSpeculate, FieldBindingTestState bindingState, int numberOfAccessorBinding)
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
            var fieldKeywordSymbolInfoAsExpression = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfoAsExpression);

            var fieldKeywordSymbolInfoAsType = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(fieldKeywordSymbolInfoAsType.Symbol);
            if (bindingState == FieldBindingTestState.BecomesLocal)
            {
                Assert.Equal(CandidateReason.NotATypeOrNamespace, fieldKeywordSymbolInfoAsType.CandidateReason);
                Assert.Equal("System.Int32 field", fieldKeywordSymbolInfoAsType.CandidateSymbols.Single().GetSymbol().ToTestDisplayString());
            }
            else
            {
                Assert.True(fieldKeywordSymbolInfoAsType.IsEmpty);
            }

            var typeInfoAsExpression = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(bindingState == FieldBindingTestState.None ? "?" : "System.Int32", typeInfoAsExpression.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(bindingState == FieldBindingTestState.None ? TypeKind.Error : TypeKind.Struct, typeInfoAsExpression.Type.TypeKind);
            Assert.Equal(typeInfoAsExpression.Type, typeInfoAsExpression.ConvertedType);

            var typeInfoAsType = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Equal("field", typeInfoAsType.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfoAsType.Type.TypeKind);
            Assert.Equal(typeInfoAsType.Type, typeInfoAsType.ConvertedType);

            var aliasInfoAsExpression = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            var aliasInfoAsType = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(aliasInfoAsExpression);
            Assert.Null(aliasInfoAsType);

            if (bindingState == FieldBindingTestState.BecomesLocal)
            {
                Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
                Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
                Assert.Equal("System.Int32 field", fieldKeywordSymbolInfo.Symbol.GetSymbol().ToTestDisplayString());
                Assert.Equal(SymbolKind.Local, fieldKeywordSymbolInfo.Symbol.Kind);
            }
            else
            {
                Assert.Equal(FieldBindingTestState.None, bindingState);
                Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
                Assert.Null(fieldKeywordSymbolInfo.Symbol);
            }

            Assert.Equal(numberOfAccessorBinding, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "never")]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "always")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "never")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "always")]
        public void SpeculativeSemanticModel_FieldLocalNotInScope_BindOriginalFirst(SpeculativeBindingOption bindingOption, string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            if (GetBoolValue())
            {
                int field = 10;
                return field;
            }

            return 0;
        }
    }

    public bool GetBoolValue() => true;
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken) && t.ValueText == "0");

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);

            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, bindingOption);
            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);
            Assert.Null(fieldKeywordSymbolInfo.Symbol);

            var typeInfoAsExpression = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal("?", typeInfoAsExpression.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfoAsExpression.Type.TypeKind);
            Assert.Equal(typeInfoAsExpression.Type, typeInfoAsExpression.ConvertedType);

            var typeInfoAsType = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Equal("field", typeInfoAsType.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfoAsType.Type.TypeKind);
            Assert.Equal(typeInfoAsType.Type, typeInfoAsType.ConvertedType);

            var aliasInfoAsExpression = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            var aliasInfoAsType = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(aliasInfoAsExpression);
            Assert.Null(aliasInfoAsType);

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "never")]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "always")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "never")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "always")]
        public void SpeculativeSemanticModel_FieldLocalNotInScope_BindSpeculatedFirst(SpeculativeBindingOption bindingOption, string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            if (GetBoolValue())
            {
                int field = 10;
                return field;
            }

            return 0;
        }
    }

    public bool GetBoolValue() => true;
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var token = tree.GetRoot().DescendantTokens().Single(t => t.IsKind(SyntaxKind.NumericLiteralToken) && t.ValueText == "0");

            var model = comp.GetSemanticModel(tree);
            var identifier = SyntaxFactory.ParseExpression("field");
            model.TryGetSpeculativeSemanticModel(token.SpanStart, (IdentifierNameSyntax)identifier, out var speculativeModel);
            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(identifier);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(token.SpanStart, identifier, bindingOption);
            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);

            var typeInfoAsExpression = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal("?", typeInfoAsExpression.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfoAsExpression.Type.TypeKind);
            Assert.Equal(typeInfoAsExpression.Type, typeInfoAsExpression.ConvertedType);

            var typeInfoAsType = model.GetSpeculativeTypeInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Equal("field", typeInfoAsType.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfoAsType.Type.TypeKind);
            Assert.Equal(typeInfoAsType.Type, typeInfoAsType.ConvertedType);

            var aliasInfoAsExpression = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
            var aliasInfoAsType = model.GetSpeculativeAliasInfo(token.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(aliasInfoAsExpression);
            Assert.Null(aliasInfoAsType);

            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            Assert.Null(fieldKeywordSymbolInfo.Symbol);
            Assert.Equal(1, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "never")]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "always")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "never")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "always")]
        public void SpeculativeSemanticModel_EqualsValueClauseSyntax_OriginalIsRegularProperty_BindOriginalFirst(SpeculativeBindingOption bindingOption, string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            return local();

            int local(int x = 0) => x;
        }
    }
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var equalsValueClauseSyntax = (EqualsValueClauseSyntax)tree.GetRoot().DescendantNodes().Single(t => t is EqualsValueClauseSyntax);

            var model = comp.GetSemanticModel(tree);
            var newEqualsValueClause = equalsValueClauseSyntax.WithValue(SyntaxFactory.ParseExpression("field"));
            model.TryGetSpeculativeSemanticModel(equalsValueClauseSyntax.SpanStart, newEqualsValueClause, out var speculativeModel);

            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(newEqualsValueClause.Value);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(equalsValueClauseSyntax.SpanStart, newEqualsValueClause.Value, bindingOption);
            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);
            Assert.Null(fieldKeywordSymbolInfo.Symbol);

            var typeInfoAsExpression = model.GetSpeculativeTypeInfo(equalsValueClauseSyntax.SpanStart, newEqualsValueClause.Value, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal("?", typeInfoAsExpression.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfoAsExpression.Type.TypeKind);
            Assert.Equal(typeInfoAsExpression.Type, typeInfoAsExpression.ConvertedType);

            var typeInfoAsType = model.GetSpeculativeTypeInfo(equalsValueClauseSyntax.SpanStart, newEqualsValueClause.Value, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Equal("field", typeInfoAsType.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfoAsType.Type.TypeKind);
            Assert.Equal(typeInfoAsType.Type, typeInfoAsType.ConvertedType);

            var aliasInfoAsExpression = model.GetSpeculativeAliasInfo(equalsValueClauseSyntax.SpanStart, newEqualsValueClause.Value, SpeculativeBindingOption.BindAsExpression);
            var aliasInfoAsType = model.GetSpeculativeAliasInfo(equalsValueClauseSyntax.SpanStart, newEqualsValueClause.Value, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(aliasInfoAsExpression);
            Assert.Null(aliasInfoAsType);

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "never")]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "always")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "never")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "always")]
        public void SpeculativeSemanticModel_EqualsValueClauseSyntax_OriginalIsRegularProperty_BindSpeculatedFirst(SpeculativeBindingOption bindingOption, string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            return local();

            int local(int x = 0) => x;
        }
    }
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var equalsValueClauseSyntax = (EqualsValueClauseSyntax)tree.GetRoot().DescendantNodes().Single(t => t is EqualsValueClauseSyntax);

            var model = comp.GetSemanticModel(tree);
            var newEqualsValueClause = equalsValueClauseSyntax.WithValue(SyntaxFactory.ParseExpression("field"));
            model.TryGetSpeculativeSemanticModel(equalsValueClauseSyntax.SpanStart, newEqualsValueClause, out var speculativeModel);

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(newEqualsValueClause.Value);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(equalsValueClauseSyntax.SpanStart, newEqualsValueClause.Value, bindingOption);
            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);

            var typeInfoAsExpression = model.GetSpeculativeTypeInfo(equalsValueClauseSyntax.SpanStart, newEqualsValueClause.Value, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal("?", typeInfoAsExpression.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfoAsExpression.Type.TypeKind);
            Assert.Equal(typeInfoAsExpression.Type, typeInfoAsExpression.ConvertedType);

            var typeInfoAsType = model.GetSpeculativeTypeInfo(equalsValueClauseSyntax.SpanStart, newEqualsValueClause.Value, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Equal("field", typeInfoAsType.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfoAsType.Type.TypeKind);
            Assert.Equal(typeInfoAsType.Type, typeInfoAsType.ConvertedType);

            var aliasInfoAsExpression = model.GetSpeculativeAliasInfo(equalsValueClauseSyntax.SpanStart, newEqualsValueClause.Value, SpeculativeBindingOption.BindAsExpression);
            var aliasInfoAsType = model.GetSpeculativeAliasInfo(equalsValueClauseSyntax.SpanStart, newEqualsValueClause.Value, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(aliasInfoAsExpression);
            Assert.Null(aliasInfoAsType);

            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            Assert.Null(fieldKeywordSymbolInfo.Symbol);
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "never")]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "always")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "never")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "always")]
        public void SpeculativeSemanticModel_EqualsValueClauseSyntax_OriginalIsSemiAutoProperty_BindOriginalFirst(SpeculativeBindingOption bindingOption, string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            return field + local();

            int local(int x = 0) => x;
        }
    }
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var equalsValueClauseSyntax = (EqualsValueClauseSyntax)tree.GetRoot().DescendantNodes().Single(t => t is EqualsValueClauseSyntax);

            var model = comp.GetSemanticModel(tree);
            var newEqualsValueClause = equalsValueClauseSyntax.WithValue(SyntaxFactory.ParseExpression("field"));
            model.TryGetSpeculativeSemanticModel(equalsValueClauseSyntax.SpanStart, newEqualsValueClause, out var speculativeModel);

            Assert.Equal("System.Int32 C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(newEqualsValueClause.Value);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(equalsValueClauseSyntax.SpanStart, newEqualsValueClause.Value, bindingOption);
            if (bindingOption == SpeculativeBindingOption.BindAsTypeOrNamespace)
            {
                Assert.Null(fieldKeywordSymbolInfo2.Symbol);
                Assert.True(fieldKeywordSymbolInfo2.IsEmpty);
            }
            else
            {
                Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);
            }

            Assert.Equal(comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single(), fieldKeywordSymbolInfo.Symbol.GetSymbol());

            var typeInfo = model.GetSpeculativeTypeInfo(equalsValueClauseSyntax.SpanStart, newEqualsValueClause.Value, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "System.Int32" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? TypeKind.Struct : TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(equalsValueClauseSyntax.SpanStart, newEqualsValueClause.Value, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "never")]
        [InlineData(SpeculativeBindingOption.BindAsExpression, "always")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "never")]
        [InlineData(SpeculativeBindingOption.BindAsTypeOrNamespace, "always")]
        public void SpeculativeSemanticModel_EqualsValueClauseSyntax_OriginalIsSemiAutoProperty_BindSpeculatedFirst(SpeculativeBindingOption bindingOption, string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            return field + local();

            int local(int x = 0) => x;
        }
    }
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var equalsValueClauseSyntax = (EqualsValueClauseSyntax)tree.GetRoot().DescendantNodes().Single(t => t is EqualsValueClauseSyntax);

            var model = comp.GetSemanticModel(tree);
            var newEqualsValueClause = equalsValueClauseSyntax.WithValue(SyntaxFactory.ParseExpression("field"));
            model.TryGetSpeculativeSemanticModel(equalsValueClauseSyntax.SpanStart, newEqualsValueClause, out var speculativeModel);

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(newEqualsValueClause.Value);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(equalsValueClauseSyntax.SpanStart, newEqualsValueClause.Value, bindingOption);
            if (bindingOption == SpeculativeBindingOption.BindAsExpression)
            {
                Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);
            }
            else
            {
                Assert.Null(fieldKeywordSymbolInfo2.Symbol);
                Assert.True(fieldKeywordSymbolInfo2.IsEmpty);
            }

            var typeInfo = model.GetSpeculativeTypeInfo(equalsValueClauseSyntax.SpanStart, newEqualsValueClause.Value, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "System.Int32" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? TypeKind.Struct : TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(equalsValueClauseSyntax.SpanStart, newEqualsValueClause.Value, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Equal("System.Int32 C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());
            Assert.Equal(comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single(), fieldKeywordSymbolInfo.Symbol.GetSymbol());
            Assert.Equal(runNullableAnalysis == "always" ? 0 : 1, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory, CombinatorialData]
        public void SpeculativeSemanticModel_AttributeSyntax_OriginalIsRegularProperty_BindOriginalFirst(SpeculativeBindingOption bindingOption, [CombinatorialValues("never", "always")] string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            return local();

            [My("""")]
            int local(int x = 0) => x;
        }
    }
}

public class MyAttribute : System.Attribute
{
    public MyAttribute(string s) { }
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var attributeSyntax = (AttributeSyntax)tree.GetRoot().DescendantNodes().Single(t => t is AttributeSyntax);

            var model = comp.GetSemanticModel(tree);

            var newAttributeSyntax = SyntaxFactory.Attribute(
                attributeSyntax.Name,
                SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>().Add(SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression("nameof(field)")))));

            var fieldNode = (IdentifierNameSyntax)((InvocationExpressionSyntax)newAttributeSyntax.ArgumentList.Arguments[0].Expression).ArgumentList.Arguments[0].Expression;
            Assert.Equal(SyntaxKind.FieldKeyword, fieldNode.Identifier.ContextualKind());
            model.TryGetSpeculativeSemanticModel(attributeSyntax.SpanStart, newAttributeSyntax, out var speculativeModel);

            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(fieldNode);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(attributeSyntax.SpanStart, fieldNode, bindingOption);
            Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);
            Assert.Null(fieldKeywordSymbolInfo.Symbol);

            var typeInfo = model.GetSpeculativeTypeInfo(attributeSyntax.SpanStart, fieldNode, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "?" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(attributeSyntax.SpanStart, fieldNode, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory, CombinatorialData]
        public void SpeculativeSemanticModel_AttributeSyntax_OriginalIsRegularProperty_BindSpeculatedFirst(SpeculativeBindingOption bindingOption, [CombinatorialValues("never", "always")] string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            return local();

            [My("""")]
            int local(int x = 0) => x;
        }
    }
}

public class MyAttribute : System.Attribute
{
    public MyAttribute(string s) { }
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];

            var attributeSyntax = (AttributeSyntax)tree.GetRoot().DescendantNodes().Single(t => t is AttributeSyntax);
            var model = comp.GetSemanticModel(tree);

            var newAttributeSyntax = SyntaxFactory.Attribute(
                attributeSyntax.Name,
                SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>().Add(SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression("nameof(field)")))));

            var fieldNode = (IdentifierNameSyntax)((InvocationExpressionSyntax)newAttributeSyntax.ArgumentList.Arguments[0].Expression).ArgumentList.Arguments[0].Expression;
            Assert.Equal(SyntaxKind.FieldKeyword, fieldNode.Identifier.ContextualKind());
            model.TryGetSpeculativeSemanticModel(attributeSyntax.SpanStart, newAttributeSyntax, out var speculativeModel);

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(fieldNode);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(attributeSyntax.SpanStart, fieldNode, bindingOption);
            if (bindingOption == SpeculativeBindingOption.BindAsExpression)
            {
                Assert.Equal(fieldKeywordSymbolInfo, fieldKeywordSymbolInfo2);
            }
            else
            {
                Assert.True(fieldKeywordSymbolInfo2.IsEmpty);
                Assert.Null(fieldKeywordSymbolInfo2.Symbol);
            }

            var typeInfo = model.GetSpeculativeTypeInfo(attributeSyntax.SpanStart, fieldNode, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "?" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(attributeSyntax.SpanStart, fieldNode, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Empty(comp.GetTypeByMetadataName("C").GetFieldsToEmit());
            Assert.Null(fieldKeywordSymbolInfo.Symbol);
            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory, CombinatorialData]
        public void SpeculativeSemanticModel_AttributeSyntax_OriginalIsSemiAutoProperty_BindOriginalFirst(SpeculativeBindingOption bindingOption, [CombinatorialValues("never", "always")] string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            return field + local();

            [My("""")]
            int local(int x = 0) => x;
        }
    }
}

public class MyAttribute : System.Attribute
{
    public MyAttribute(string s) { }
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var attributeSyntax = (AttributeSyntax)tree.GetRoot().DescendantNodes().Single(t => t is AttributeSyntax);
            var model = comp.GetSemanticModel(tree);

            var newAttributeSyntax = SyntaxFactory.Attribute(
                attributeSyntax.Name,
                SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>().Add(SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression("nameof(field)")))));

            var fieldNode = (IdentifierNameSyntax)((InvocationExpressionSyntax)newAttributeSyntax.ArgumentList.Arguments[0].Expression).ArgumentList.Arguments[0].Expression;
            Assert.Equal(SyntaxKind.FieldKeyword, fieldNode.Identifier.ContextualKind());
            model.TryGetSpeculativeSemanticModel(attributeSyntax.SpanStart, newAttributeSyntax, out var speculativeModel);

            Assert.Equal("System.Int32 C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(fieldNode);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(attributeSyntax.SpanStart, fieldNode, bindingOption);
            Assert.Equal(comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single(), fieldKeywordSymbolInfo.Symbol.GetSymbol());

            if (bindingOption == SpeculativeBindingOption.BindAsTypeOrNamespace)
            {
                Assert.True(fieldKeywordSymbolInfo2.IsEmpty);
                Assert.Null(fieldKeywordSymbolInfo2.Symbol);
            }
            else
            {
                Assert.Equal(fieldKeywordSymbolInfo2, fieldKeywordSymbolInfo);
            }

            var typeInfo = model.GetSpeculativeTypeInfo(attributeSyntax.SpanStart, fieldNode, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "System.Int32" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? TypeKind.Struct : TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(attributeSyntax.SpanStart, fieldNode, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Theory, CombinatorialData]
        public void SpeculativeSemanticModel_AttributeSyntax_OriginalIsSemiAutoProperty_BindSpeculatedFirst(SpeculativeBindingOption bindingOption, [CombinatorialValues("never", "always")] string runNullableAnalysis)
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P
    {
        get
        {
            return field + local();

            [My("""")]
            int local(int x = 0) => x;
        }
    }
}

public class MyAttribute : System.Attribute
{
    public MyAttribute(string s) { }
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", runNullableAnalysis));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var attributeSyntax = (AttributeSyntax)tree.GetRoot().DescendantNodes().Single(t => t is AttributeSyntax);
            var model = comp.GetSemanticModel(tree);

            var newAttributeSyntax = SyntaxFactory.Attribute(
                attributeSyntax.Name,
                SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>().Add(SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression("nameof(field)")))));

            var fieldNode = (IdentifierNameSyntax)((InvocationExpressionSyntax)newAttributeSyntax.ArgumentList.Arguments[0].Expression).ArgumentList.Arguments[0].Expression;
            Assert.Equal(SyntaxKind.FieldKeyword, fieldNode.Identifier.ContextualKind());
            model.TryGetSpeculativeSemanticModel(attributeSyntax.SpanStart, newAttributeSyntax, out var speculativeModel);

            var fieldKeywordSymbolInfo = speculativeModel.GetSymbolInfo(fieldNode);
            var fieldKeywordSymbolInfo2 = model.GetSpeculativeSymbolInfo(attributeSyntax.SpanStart, fieldNode, bindingOption);
            Assert.Equal(comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single(), fieldKeywordSymbolInfo.Symbol.GetSymbol());

            if (bindingOption == SpeculativeBindingOption.BindAsTypeOrNamespace)
            {
                Assert.True(fieldKeywordSymbolInfo2.IsEmpty);
                Assert.Null(fieldKeywordSymbolInfo2.Symbol);
            }
            else
            {
                Assert.Equal(fieldKeywordSymbolInfo2, fieldKeywordSymbolInfo);
            }

            var typeInfo = model.GetSpeculativeTypeInfo(attributeSyntax.SpanStart, fieldNode, bindingOption);
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? "System.Int32" : "field", typeInfo.Type.GetSymbol().ToTestDisplayString());
            Assert.Equal(bindingOption == SpeculativeBindingOption.BindAsExpression ? TypeKind.Struct : TypeKind.Error, typeInfo.Type.TypeKind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var aliasInfo = model.GetSpeculativeAliasInfo(attributeSyntax.SpanStart, fieldNode, bindingOption);
            Assert.Null(aliasInfo);

            Assert.Equal("System.Int32 C.<P>k__BackingField", comp.GetTypeByMetadataName("C").GetFieldsToEmit().Single().ToTestDisplayString());
            Assert.Equal(runNullableAnalysis == "always" ? 0 : 1, accessorBindingData.NumberOfPerformedAccessorBinding);
        }

        [Fact]
        public void TestFromSemanticModelBinder()
        {
            var comp = CreateCompilation(@"
public class C
{
    public int P1 { get => field; }
}
", parseOptions: TestOptions.RegularNext.WithFeature("run-nullable-analysis", "never"));
            var accessorBindingData = new SourcePropertySymbolBase.AccessorBindingData();
            comp.TestOnlyCompilationData = accessorBindingData;

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var info = model.GetSymbolInfo(tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Single());
            Assert.Empty(info.CandidateSymbols);
            Assert.False(info.IsEmpty);
            Assert.Equal("System.Int32 C.<P1>k__BackingField", info.Symbol.GetSymbol().ToTestDisplayString());
            Assert.Empty(comp.GetTypeByMetadataName("C").GetMembers().OfType<FieldSymbol>());
            comp.VerifyDiagnostics();

            Assert.Equal(0, accessorBindingData.NumberOfPerformedAccessorBinding);
        }
    }
}
