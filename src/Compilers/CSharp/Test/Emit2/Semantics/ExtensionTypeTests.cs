// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public class ExtensionTypeTests : CompilingTestBase
{
    private static void VerifyNotExtension<T>(TypeSymbol type) where T : TypeSymbol
    {
        Assert.True(type is T, $"Found type '{type.GetType()}'");
        Assert.False(type.IsExtension);
        Assert.False(type.IsExplicitExtension);
        Assert.Null(type.ExtendedTypeNoUseSiteDiagnostics);
        Assert.Empty(type.BaseExtensionsNoUseSiteDiagnostics);
        Assert.Empty(type.AllBaseExtensionsNoUseSiteDiagnostics);
    }

    // Verify things that are common for all extension types
    private static void VerifyExtension<T>(TypeSymbol type, bool? isExplicit, SpecialType specialType = SpecialType.None) where T : TypeSymbol
    {
        var namedType = (NamedTypeSymbol)type;
        Assert.True(namedType is T);
        Assert.True(namedType.IsExtension);
        if (isExplicit.HasValue)
        {
            Assert.Equal(isExplicit.Value, namedType.IsExplicitExtension);
        }

        Assert.Null(namedType.BaseTypeNoUseSiteDiagnostics);
        Assert.False(namedType.IsSealed);
        Assert.False(namedType.IsRecord);
        Assert.False(namedType.IsRecordStruct);
        Assert.False(namedType.IsReferenceType);
        Assert.False(namedType.IsValueType);
        Assert.False(namedType.IsTypeParameter());
        Assert.False(namedType.IsAnonymousType);
        Assert.False(namedType.IsEnumType());
        Assert.False(namedType.IsErrorType());
        Assert.Equal(specialType, namedType.SpecialType);
        Assert.False(namedType.IsObjectType());
        Assert.False(namedType.IsTupleType);
        Assert.True(namedType.TupleElements.IsDefault);
        Assert.Empty(namedType.InterfacesNoUseSiteDiagnostics());
        Assert.Empty(namedType.AllInterfacesNoUseSiteDiagnostics); // PROTOTYPE
        Assert.False(namedType.IsReadOnly);
        Assert.False(namedType.IsRefLikeType);
        Assert.False(namedType.IsPointerOrFunctionPointer());
        Assert.Equal(TypeKind.Extension, namedType.TypeKind);
        Assert.False(namedType.IsInterfaceType());
        Assert.False(namedType.IsAbstract);

        if (namedType.ExtendedTypeNoUseSiteDiagnostics is { } underlyingType)
        {
            // PROTOTYPE consider whether we want to expose invalid underlying types
            // in context of public APIs
            VerifyNotExtension<TypeSymbol>(underlyingType);
        }

        if (namedType != (object)namedType.OriginalDefinition)
        {
            VerifyExtension<TypeSymbol>(namedType.OriginalDefinition, isExplicit);
        }

        foreach (var baseExtension in namedType.AllBaseExtensionsNoUseSiteDiagnostics)
        {
            checkBaseExtension(baseExtension);
        }

        var managedKindUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
        Assert.False(namedType.IsManagedType(ref managedKindUseSiteInfo));

        Assert.False(namedType.IsRestrictedType());
        Assert.True(namedType.IsType);
        Assert.True(namedType.CanBeReferencedByName);

        Assert.False(namedType.IsCustomTaskType(out _));
        Assert.Null(namedType.DelegateInvokeMethod);
        Assert.False(namedType.HasAnyRequiredMembers);
        Assert.False(namedType.IsNamespace);
        Assert.True(namedType.IsMetadataSealed);

        if (namedType is SourceNamedTypeSymbol sourceNamedType)
        {
            Assert.False(sourceNamedType.IsScriptClass);
            Assert.Null(sourceNamedType.EnumUnderlyingType);
            Assert.False(sourceNamedType.HasStructLayoutAttribute); // PROTOTYPE revisit when adding support for attributes
            Assert.False(sourceNamedType.IsAnonymousType);
            Assert.False(sourceNamedType.IsSimpleProgram);
            Assert.False(sourceNamedType.IsImplicitlyDeclared);
        }

        static void checkBaseExtension(NamedTypeSymbol baseExtension)
        {
            if (baseExtension.IsExtension)
            {
                VerifyExtension<TypeSymbol>(baseExtension, isExplicit: null);
            }
            else
            {
                Assert.True(baseExtension.IsErrorType());
            }
        }
    }

    [Theory, CombinatorialData]
    public void ForClass(bool useImageReference, bool isExplicit)
    {
        var src = $$"""
interface I { }
public class UnderlyingClass : I { }
public {{(isExplicit ? "explicit" : "implicit")}} extension R for UnderlyingClass
{
    public static int StaticField = 42;
    public const string Const = "hello";

    public void Method() { }
    public static void StaticMethod() { }

    public int Property { get => throw null; set => throw null; }
    public static int StaticProperty { get => throw null; set => throw null; }
    public int this[int i] => throw null;

    class NestedType { }
    static class StaticNestedType { }
    explicit extension NestedR for UnderlyingClass { }
    public R(int i) { }
    public static implicit operator R(int i) => throw null;
    public static implicit operator R(UnderlyingClass c) => throw null;
    public static implicit operator UnderlyingClass(R r) => throw null;
    public static int operator+(R r, UnderlyingClass c) => throw null;
    public static int operator-(UnderlyingClass c, R r) => throw null;
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
            targetFramework: TargetFramework.Net70);
        // PROTOTYPE need to finalize the rules for operators (conversion and others)
        // PROTOTYPE constructor and destructor
        comp.VerifyDiagnostics();

        var verifier = CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        verifier.VerifyIL($$"""R.{{ExtensionMarkerName(isExplicit)}}(UnderlyingClass)""", """
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}
""");

        var src2 = """
explicit extension R2 for UnderlyingClass : R { }
""";
        var comp2 = CreateCompilation(src2, references: new[] { AsReference(comp, useImageReference) }, targetFramework: TargetFramework.Net70);
        comp2.VerifyDiagnostics();
        return;

        void validate(ModuleSymbol module)
        {
            bool inSource = module is SourceModuleSymbol;
            var r = module.GlobalNamespace.GetTypeMember("R");
            Assert.Equal("R", r.ToTestDisplayString());
            if (inSource)
            {
                VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: isExplicit);
            }
            else
            {
                VerifyExtension<PENamedTypeSymbol>(r, isExplicit: isExplicit);
            }

            Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);

            if (!inSource)
            {
                AssertEx.SetEqual(new[]
                    {
                        "System.Runtime.CompilerServices.IsByRefLikeAttribute",
                        """System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute("ExtensionTypes")""",
                        """System.ObsoleteAttribute("Extension types are not supported in this version of your compiler.", true)""",
                        """System.Reflection.DefaultMemberAttribute("Item")"""
                    },
                    GetAttributeStrings(r.GetAttributes()));
            }

            AssertEx.Equal(new[] { "R.NestedType", "R.StaticNestedType", "R.NestedR" },
                r.GetTypeMembers().ToTestDisplayStrings());

            Assert.False(r.IsStatic);
            Assert.Equal(Accessibility.Public, r.DeclaredAccessibility);
            Assert.Null(r.ContainingType);
            Assert.False(r.IsGenericType);
            Assert.Empty(r.TypeParameters);
            Assert.True(r.IsDefinition);
            Assert.Equal(0, r.Arity);
        }
    }

    [Fact]
    public void ForClass_DefaultAccessibility()
    {
        var src = """
interface I { }
public class UnderlyingClass : I { }
explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        static void validate(ModuleSymbol module)
        {
            var r = module.GlobalNamespace.GetTypeMember("R");
            Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
        }
    }

    [Fact]
    public void ForClass_Net7()
    {
        var src = """
class UnderlyingClass { }
explicit extension R for UnderlyingClass { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, symbolValidator: validateAttributes, verify: Verification.FailsPEVerify);
        return;

        static void validateAttributes(ModuleSymbol module)
        {
            var r = module.GlobalNamespace.GetTypeMember("R");
            AssertEx.SetEqual(new[] { "IsByRefLikeAttribute", "ObsoleteAttribute", "CompilerFeatureRequiredAttribute" },
                GetAttributeNames(r.GetAttributes()));
        }
    }

    [Fact]
    public void ForClass_MissingValueType()
    {
        var src = """
class C { }
explicit extension R for C { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.MakeTypeMissing(SpecialType.System_ValueType);
        comp.VerifyDiagnostics(
            // (2,20): error CS0518: Predefined type 'System.ValueType' is not defined or imported
            // explicit extension R for C { }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "R").WithArguments("System.ValueType").WithLocation(2, 20)
            );
    }

    [Fact]
    public void ForClass_MissingVoidType()
    {
        var src = """
class C { }
explicit extension R for C { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.MakeTypeMissing(SpecialType.System_Void);
        comp.VerifyEmitDiagnostics(
            // (2,20): error CS0518: Predefined type 'System.Void' is not defined or imported
            // explicit extension R for C { }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "R").WithArguments("System.Void").WithLocation(2, 20)
            );
    }

    [Fact]
    public void ForClass_Generic()
    {
        var src = """
class UnderlyingClass<T1, T2> { }
class C<T>
{
    explicit extension R<U> for UnderlyingClass<T, U> { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        static void validate(ModuleSymbol module)
        {
            var c = module.GlobalNamespace.GetTypeMember("C");
            var r = c.GetTypeMember("R");
            Assert.Equal(1, r.Arity);
            var underlyingType = (NamedTypeSymbol)r.ExtendedTypeNoUseSiteDiagnostics;
            Assert.Equal("UnderlyingClass<T, U>", underlyingType.ToTestDisplayString());
            Assert.Equal(2, underlyingType.TypeArguments().Length);
            Assert.Same(c.TypeArguments().Single(), underlyingType.TypeArguments()[0]);
            Assert.Same(r.TypeArguments().Single(), underlyingType.TypeArguments()[1]);
        }
    }

    [Fact]
    public void ForClass_Metadata_MissingIsByRefLikeAttribute()
    {
        var src = """
class C { }
explicit extension R for C { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_IsByRefLikeAttribute);

        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        static void validate(ModuleSymbol module)
        {
            var r = module.GlobalNamespace.GetTypeMember("R");
            AssertEx.SetEqual(new[] { "IsByRefLikeAttribute", "CompilerFeatureRequiredAttribute", "ObsoleteAttribute" },
                GetAttributeNames(r.GetAttributes()));

            Assert.NotNull(module.ContainingAssembly.GetTypeByMetadataName("System.Runtime.CompilerServices.IsByRefLikeAttribute"));
        }
    }

    [Fact]
    public void ForClass_Metadata_MissingCompilerFeature()
    {
        var src = """
class C { }
explicit extension R for C { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_CompilerFeatureRequiredAttribute);

        comp.VerifyDiagnostics(
            // (2,20): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute..ctor'
            // explicit extension R for C { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "R").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute", ".ctor").WithLocation(2, 20)
            );
    }

    [Fact]
    public void ForClass_Metadata_MissingObsolete()
    {
        var src = """
class C { }
explicit extension R for C { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.MakeTypeMissing(WellKnownType.System_ObsoleteAttribute);

        comp.VerifyDiagnostics(
            // (2,20): error CS0656: Missing compiler required member 'System.ObsoleteAttribute..ctor'
            // explicit extension R for C { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "R").WithArguments("System.ObsoleteAttribute", ".ctor").WithLocation(2, 20)
            );
    }

    [Fact]
    public void ForClass_Metadata_MissingDynamicAttribute()
    {
        var src = """
class C<T> { }
explicit extension R for C<dynamic> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DynamicAttribute);
        comp.VerifyDiagnostics(
            // (2,28): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
            // explicit extension R for C<dynamic> { }
            Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(2, 28)
            );
    }

    [Fact]
    public void ForClass_WithStaticReadonlyField()
    {
        var src = """
class C { }
static explicit extension R for C
{
    public static readonly int Field = 0;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void ForClass_WithoutStaticField()
    {
        var src = """
interface I { }
class UnderlyingClass : I { }
explicit extension R for UnderlyingClass
{
    void Method() { }
    static void StaticMethod() { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        AssertEx.Equal(new[] { "void R.Method()", "void R.StaticMethod()" },
            r.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void ForClass_Nested()
    {
        var src = """
class UnderlyingClass { }
class ContainingType
{
    explicit extension R for UnderlyingClass
    {
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("ContainingType").GetTypeMember("R");
        Assert.Equal("ContainingType.R", r.ToTestDisplayString());
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("ContainingType", r.ContainingType.ToTestDisplayString());
    }

    [Fact]
    public void Members_DisallowedMembers()
    {
        var src = """
interface I { }
class UnderlyingClass : I { }
explicit extension R for UnderlyingClass
{
    public int field = 0; // 1, 2
    public volatile int field2 = 0; // 3, 4
    int AutoProperty { get; set; } // 5
    int AutoPropertyWithGetAccessor { get; } // 6
    int AutoPropertyWithoutGetAccessor { set; } // 7
    public event System.Action Event; // 8, 9
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,16): error CS9313: 'R.field': cannot declare instance members with state in extension types.
            //     public int field = 0; // 1, 2
            Diagnostic(ErrorCode.ERR_StateInExtension, "field").WithArguments("R.field").WithLocation(5, 16),
            // (5,16): warning CS0649: Field 'R.field' is never assigned to, and will always have its default value 0
            //     public int field = 0; // 1, 2
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("R.field", "0").WithLocation(5, 16),
            // (6,25): error CS9313: 'R.field2': cannot declare instance members with state in extension types.
            //     public volatile int field2 = 0; // 3, 4
            Diagnostic(ErrorCode.ERR_StateInExtension, "field2").WithArguments("R.field2").WithLocation(6, 25),
            // (6,25): warning CS0649: Field 'R.field2' is never assigned to, and will always have its default value 0
            //     public volatile int field2 = 0; // 3, 4
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field2").WithArguments("R.field2", "0").WithLocation(6, 25),
            // (7,9): error CS9313: 'R.AutoProperty': cannot declare instance members with state in extension types.
            //     int AutoProperty { get; set; } // 5
            Diagnostic(ErrorCode.ERR_StateInExtension, "AutoProperty").WithArguments("R.AutoProperty").WithLocation(7, 9),
            // (8,9): error CS9313: 'R.AutoPropertyWithGetAccessor': cannot declare instance members with state in extension types.
            //     int AutoPropertyWithGetAccessor { get; } // 6
            Diagnostic(ErrorCode.ERR_StateInExtension, "AutoPropertyWithGetAccessor").WithArguments("R.AutoPropertyWithGetAccessor").WithLocation(8, 9),
            // (9,42): error CS8051: Auto-implemented properties must have get accessors.
            //     int AutoPropertyWithoutGetAccessor { set; } // 7
            Diagnostic(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, "set").WithLocation(9, 42),
            // (10,32): error CS9313: 'R.Event': cannot declare instance members with state in extension types.
            //     public event System.Action Event; // 8, 9
            Diagnostic(ErrorCode.ERR_StateInExtension, "Event").WithArguments("R.Event").WithLocation(10, 32),
            // (10,32): warning CS0067: The event 'R.Event' is never used
            //     public event System.Action Event; // 8, 9
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event").WithArguments("R.Event").WithLocation(10, 32)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        AssertEx.Equal(new[]
            {
                "System.Int32 R.field",
                "System.Int32 modreq(System.Runtime.CompilerServices.IsVolatile) R.field2",
                "System.Int32 R.<AutoProperty>k__BackingField",
                "System.Int32 R.AutoProperty { get; set; }",
                "System.Int32 R.AutoProperty.get",
                "void R.AutoProperty.set",
                "System.Int32 R.<AutoPropertyWithGetAccessor>k__BackingField",
                "System.Int32 R.AutoPropertyWithGetAccessor { get; }",
                "System.Int32 R.AutoPropertyWithGetAccessor.get",
                "System.Int32 R.AutoPropertyWithoutGetAccessor { set; }",
                "void R.AutoPropertyWithoutGetAccessor.set",
                "void R.Event.add",
                "void R.Event.remove",
                "event System.Action R.Event"
            },
            r.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void Members_EventWithoutField()
    {
        var src = """
class UnderlyingClass { }
explicit extension R for UnderlyingClass
{
    public event System.Action Event { add { throw null; } remove { throw null; } }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        static void validate(ModuleSymbol module)
        {
            bool inSource = module is SourceModuleSymbol;
            var r = module.GlobalNamespace.GetTypeMember("R");
            Assert.Equal("R", r.ToTestDisplayString());
            if (inSource)
            {
                VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
            }
            else
            {
                VerifyExtension<PENamedTypeSymbol>(r, isExplicit: true);
            }

            AssertEx.Equal(new[]
                {
                    "event System.Action R.Event",
                    "void R.Event.add",
                    "void R.Event.remove"
                },
                r.GetMembers().ToTestDisplayStrings().OrderBy(s => s));
        }
    }

    [Fact]
    public void Members_MemberNamedAfterType()
    {
        var src = """
class C { }
explicit extension R for C
{
    public void R() { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,17): error CS0542: 'R': member names cannot be the same as their enclosing type
            //     public void R() { }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "R").WithArguments("R").WithLocation(4, 17)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        AssertEx.Equal(new[] { "void R.R()" }, r.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void Members_MemberNamedAfterUnderlyingType()
    {
        var src = """
class C { }
explicit extension R for C
{
    public void C() { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        AssertEx.Equal(new[] { "void R.C()" }, r.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void Members_DuplicateMemberNames()
    {
        var src = """
class C { }
explicit extension R for C
{
    public void M() { }
    private int M => 0;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,17): error CS0102: The type 'R' already contains a definition for 'M'
            //     private int M => 0;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "M").WithArguments("R", "M").WithLocation(5, 17)
            );
    }

    [Fact]
    public void Members_TypeParameterNameConflict()
    {
        var src = """
class C { }
explicit extension R<M> for C
{
    private int M => 0;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,17): error CS0102: The type 'R<M>' already contains a definition for 'M'
            //     private int M => 0;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "M").WithArguments("R<M>", "M").WithLocation(4, 17)
            );
    }

    [Fact]
    public void Members_UnmatchedOperator()
    {
        var src = """
class C { }
explicit extension R for C
{
    public static bool operator true(R r) => throw null;
    public static bool operator ==(R r1, R r2) => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,20): warning CS0660: 'R' defines operator == or operator != but does not override Object.Equals(object o)
            // explicit extension R for C
            Diagnostic(ErrorCode.WRN_EqualityOpWithoutEquals, "R").WithArguments("R").WithLocation(2, 20),
            // (2,20): warning CS0661: 'R' defines operator == or operator != but does not override Object.GetHashCode()
            // explicit extension R for C
            Diagnostic(ErrorCode.WRN_EqualityOpWithoutGetHashCode, "R").WithArguments("R").WithLocation(2, 20),
            // (4,33): error CS0216: The operator 'R.operator true(R)' requires a matching operator 'false' to also be defined
            //     public static bool operator true(R r) => throw null;
            Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "true").WithArguments("R.operator true(R)", "false").WithLocation(4, 33),
            // (5,33): error CS0216: The operator 'R.operator ==(R, R)' requires a matching operator '!=' to also be defined
            //     public static bool operator ==(R r1, R r2) => throw null;
            Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "==").WithArguments("R.operator ==(R, R)", "!=").WithLocation(5, 33)
            );
        // PROTOTYPE need to finalize rules on operators. Equals and GetHashCode can't be overridden...
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("enum")]
    public void Members_DefaultAccessibility(string type)
    {
        var src = $$"""
{{type}} UnderlyingType { }
explicit extension R for UnderlyingType { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        static void validate(ModuleSymbol module)
        {
            var r = module.GlobalNamespace.GetTypeMember("R");
            Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
        }
    }

    [Fact]
    public void Members_NoDefaultCtor_Delegate()
    {
        var src = $$"""
delegate void UnderlyingType();
explicit extension R for UnderlyingType { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        static void validate(ModuleSymbol module)
        {
            var r = module.GlobalNamespace.GetTypeMember("R");
            Assert.Empty(r.GetMembers());
        }
    }

    [Fact]
    public void Members_Operators_UsingUnderlyingType()
    {
        // PROTOTYPE need to finalize the rules for operators
        var src = """
class UnderlyingClass { }
explicit extension R for UnderlyingClass
{
    public static int operator+(UnderlyingClass c1, UnderlyingClass c2) => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,31): error CS0563: One of the parameters of a binary operator must be the containing type
            //     public static int operator+(UnderlyingClass c1, UnderlyingClass c2) => throw null;
            Diagnostic(ErrorCode.ERR_BadBinaryOperatorSignature, "+").WithLocation(4, 31)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        AssertEx.Equal(new[] { "System.Int32 R.op_Addition(UnderlyingClass c1, UnderlyingClass c2)" }, r.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void Members_StaticExtension()
    {
        var src = """
interface I { }
class UnderlyingClass : I { }
static explicit extension R for UnderlyingClass
{
    public static int StaticField = 0;
    const string Const = "hello";

    void Method() { } // 1
    static void StaticMethod() { }

    int Property { get => throw null; set => throw null; } // 2
    static int StaticProperty { get => throw null; set => throw null; }
    int this[int i] => throw null; // 3

    class NestedType { }
    static class StaticNestedType { }
    explicit extension NestedR for UnderlyingClass { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (8,10): error CS0708: 'Method': cannot declare instance members in a static type
            //     void Method() { } // 1
            Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "Method").WithArguments("Method").WithLocation(8, 10),
            // (11,9): error CS0708: 'R.Property': cannot declare instance members in a static type
            //     int Property { get => throw null; set => throw null; } // 2
            Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "Property").WithArguments("R.Property").WithLocation(11, 9),
            // (13,9): error CS0720: 'R.this[int]': cannot declare indexers in a static type
            //     int this[int i] => throw null; // 3
            Diagnostic(ErrorCode.ERR_IndexerInStaticClass, "this").WithArguments("R.this[int]").WithLocation(13, 9)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
        Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);
        AssertEx.Equal(new[]
            {
                "System.Int32 R.StaticField",
                "System.String R.Const",
                "void R.Method()",
                "void R.StaticMethod()",
                "System.Int32 R.Property { get; set; }",
                "System.Int32 R.Property.get",
                "void R.Property.set",
                "System.Int32 R.StaticProperty { get; set; }",
                "System.Int32 R.StaticProperty.get",
                "void R.StaticProperty.set",
                "System.Int32 R.this[System.Int32 i] { get; }",
                "System.Int32 R.this[System.Int32 i].get",
                "R.NestedType",
                "R.StaticNestedType",
                "R.NestedR",
                "R..cctor()"
            },
            r.GetMembers().ToTestDisplayStrings());

        Assert.True(r.IsStatic);
        Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
        Assert.Null(r.ContainingType);
        Assert.Empty(r.TypeParameters);
        Assert.False(r.IsGenericType);
    }

    [Fact]
    public void Members_ExplicitInterfaceImplementation()
    {
        var src = """
interface I
{
    void M();
}
class UnderlyingClass : I
{
    public void M() { }
}
explicit extension R1 for UnderlyingClass
{
    void I.M() { }
}
explicit extension R2 for UnderlyingClass : I
{
    void I.M() { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (11,12): error CS0541: 'R1.M()': explicit interface declaration can only be declared in a class, record, struct or interface
            //     void I.M() { }
            Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M").WithArguments("R1.M()").WithLocation(11, 12),
            // (13,45): error CS9307: A base extension must be an extension type.
            // explicit extension R2 for UnderlyingClass : I
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "I").WithLocation(13, 45),
            // (15,12): error CS0541: 'R2.M()': explicit interface declaration can only be declared in a class, record, struct or interface
            //     void I.M() { }
            Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M").WithArguments("R2.M()").WithLocation(15, 12)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        AssertEx.Equal(new[] { "void R1.M()" }, r1.GetMembers().ToTestDisplayStrings());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        AssertEx.Equal(new[] { "void R2.M()" }, r2.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void Members_Methods_AllowedModifiers()
    {
        var src = """
class UnderlyingClass { }
partial explicit extension R for UnderlyingClass
{
    void MethodDefaultAccessibility() { }
    public void MethodPublic() { }
    private void MethodPrivate() { }
    internal void MethodInternal() { }
    protected void MethodProtected() { }
    private protected void MethodPrivateProtected() { }
    internal protected void MethodInternalProtected() { }

    unsafe int* MethodUnsafe(int* i) => i;
    int* MethodNotUnsafe(int* i) => i; // 1, 2, 3
    new string ToString() => "";
    new void MethodNotNew() { }

    async System.Threading.Tasks.Task<int> MethodAsync()
    {
        await System.Threading.Tasks.Task.Yield();
        return 1;
    }

    static extern void MethodExtern(); // 4

    [System.Runtime.InteropServices.DllImport("test")]
    static extern void MethodExtern2();

    public partial int MethodPartial(int i);
    ref int MethodRefInt() => throw null;
    static void MethodStatic() { }
    ref readonly int MethodRefReadonly => throw null;
}
partial explicit extension R for UnderlyingClass
{
    public partial int MethodPartial(int i) => 1;
}
""";
        // PROTOTYPE should warn that `new` isn't required
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (13,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     int* MethodNotUnsafe(int* i) => i; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(13, 5),
            // (13,26): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     int* MethodNotUnsafe(int* i) => i; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(13, 26),
            // (13,37): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     int* MethodNotUnsafe(int* i) => i; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "i").WithLocation(13, 37),
            // (23,24): warning CS0626: Method, operator, or accessor 'R.MethodExtern()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
            //     static extern void MethodExtern(); // 4
            Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "MethodExtern").WithArguments("R.MethodExtern()").WithLocation(23, 24)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        MethodSymbol methodDefaultAccessibility = r.GetMethod("MethodDefaultAccessibility");
        Assert.Equal(Accessibility.Private, methodDefaultAccessibility.DeclaredAccessibility);
        Assert.False(methodDefaultAccessibility.IsStatic);
        Assert.False(methodDefaultAccessibility.IsAsync);
        Assert.False(methodDefaultAccessibility.IsExplicitInterfaceImplementation);

        Assert.Equal(Accessibility.Public, r.GetMethod("MethodPublic").DeclaredAccessibility);
        Assert.Equal(Accessibility.Private, r.GetMethod("MethodPrivate").DeclaredAccessibility);
        Assert.Equal(Accessibility.Internal, r.GetMethod("MethodInternal").DeclaredAccessibility);
        Assert.Equal(Accessibility.Protected, r.GetMethod("MethodProtected").DeclaredAccessibility);
        Assert.Equal(Accessibility.ProtectedAndInternal, r.GetMethod("MethodPrivateProtected").DeclaredAccessibility);
        Assert.Equal(Accessibility.ProtectedOrInternal, r.GetMethod("MethodInternalProtected").DeclaredAccessibility);
        Assert.True(r.GetMethod("MethodAsync").IsAsync);
        Assert.True(r.GetMethod("MethodExtern").IsExtern);
        Assert.True(r.GetMethod("MethodExtern2").IsExtern);
        Assert.True(r.GetMethod("MethodStatic").IsStatic);
        Assert.True(r.GetProperty("MethodRefReadonly").ReturnsByRefReadonly);
    }

    [Fact]
    public void Members_Methods_AllowedModifiers_New()
    {
        var src = """
class UnderlyingClass { }
explicit extension R1 for UnderlyingClass
{
    public void Method() { }
}
partial explicit extension R2 for UnderlyingClass : R1
{
    public new void Method() { }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Members_Methods_AllowedModifiers_New_Missing()
    {
        var src = """
class UnderlyingClass { }
explicit extension R1 for UnderlyingClass
{
    public void Method() { }
}
partial explicit extension R2 for UnderlyingClass : R1
{
    public void Method() { }
}
""";

        // PROTOTYPE should warn about missing `new`
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Members_Methods_DisallowedModifiers()
    {
        var src = """
interface I { }
class UnderlyingClass : I { }
explicit extension R for UnderlyingClass
{
    public abstract void M1(); // 1, 2
    override string ToString() => ""; // 3
    readonly void M3() { } // 4
    sealed void M4() { } // 5
    virtual void M5() { } // 6
    required void M6() { } // 7
    scoped System.Span<int> M7() => throw null; // 8
    file void M10() { } // 9
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,26): error CS0106: The modifier 'abstract' is not valid for this item
            //     public abstract void M1(); // 1, 2
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M1").WithArguments("abstract").WithLocation(5, 26),
            // (5,26): error CS0501: 'R.M1()' must declare a body because it is not marked abstract, extern, or partial
            //     public abstract void M1(); // 1, 2
            Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M1").WithArguments("R.M1()").WithLocation(5, 26),
            // (6,21): error CS0106: The modifier 'override' is not valid for this item
            //     override string ToString() => ""; // 3
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "ToString").WithArguments("override").WithLocation(6, 21),
            // (7,19): error CS0106: The modifier 'readonly' is not valid for this item
            //     readonly void M3() { } // 4
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M3").WithArguments("readonly").WithLocation(7, 19),
            // (8,17): error CS0106: The modifier 'sealed' is not valid for this item
            //     sealed void M4() { } // 5
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M4").WithArguments("sealed").WithLocation(8, 17),
            // (9,18): error CS0106: The modifier 'virtual' is not valid for this item
            //     virtual void M5() { } // 6
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M5").WithArguments("virtual").WithLocation(9, 18),
            // (10,19): error CS0106: The modifier 'required' is not valid for this item
            //     required void M6() { } // 7
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M6").WithArguments("required").WithLocation(10, 19),
            // (11,29): error CS0106: The modifier 'scoped' is not valid for this item
            //     scoped System.Span<int> M7() => throw null; // 8
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M7").WithArguments("scoped").WithLocation(11, 29),
            // (12,15): error CS0106: The modifier 'file' is not valid for this item
            //     file void M10() { } // 9
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("file").WithLocation(12, 15)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        MethodSymbol m1 = r.GetMethod("M1");
        Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility);
        Assert.False(m1.IsAbstract);
        Assert.False(r.GetMethod("M4").IsSealed);
        Assert.Equal(Accessibility.Private, r.GetMethod("M5").DeclaredAccessibility);
        Assert.False(r.GetMethod("M6").IsRequired());
    }

    [Fact]
    public void Members_Properties_AllowedModifiers()
    {
        var src = """
class UnderlyingClass { }
explicit extension R for UnderlyingClass
{
    int DefaultAccessibility => 0;
    public int Public => 0;
    private int Private => 0;
    internal int Internal => 0;
    protected int Protected => 0;
    private protected int PrivateProtected => 0;
    internal protected int InternalProtected => 0;
    unsafe int* Unsafe => null;
    int* NotUnsafe => null; // 1
    new int NotNew => 0;
    ref int RefInt => throw null;
    static int Static => 0;

    extern int Extern { get; } // 2
    static extern int Extern2 { [System.Runtime.InteropServices.DllImport("test")] get; }
    ref readonly int RefReadonlyInt => throw null;
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should warn that `new` isn't required
        comp.VerifyDiagnostics(
            // (12,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     int* NotUnsafe => null; // 1
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(12, 5),
            // (17,25): warning CS0626: Method, operator, or accessor 'R.Extern.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
            //     extern int Extern { get; } // 2
            Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "get").WithArguments("R.Extern.get").WithLocation(17, 25)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        var defaultAccessibility = r.GetProperty("DefaultAccessibility");
        Assert.Equal(Accessibility.Private, defaultAccessibility.DeclaredAccessibility);
        Assert.False(defaultAccessibility.IsStatic);
        Assert.False(defaultAccessibility.IsAbstract);
        Assert.False(defaultAccessibility.IsIndexer);
        Assert.False(defaultAccessibility.IsOverride);
        Assert.False(defaultAccessibility.IsVirtual);
        Assert.False(defaultAccessibility.IsSealed);
        Assert.False(defaultAccessibility.IsRequired);
        Assert.False(defaultAccessibility.IsExtern);
        Assert.False(defaultAccessibility.GetMethod.IsExtern);
        Assert.False(defaultAccessibility.IsExplicitInterfaceImplementation);

        Assert.Equal(Accessibility.Public, r.GetProperty("Public").DeclaredAccessibility);
        Assert.Equal(Accessibility.Private, r.GetProperty("Private").DeclaredAccessibility);
        Assert.Equal(Accessibility.Internal, r.GetProperty("Internal").DeclaredAccessibility);
        Assert.Equal(Accessibility.Protected, r.GetProperty("Protected").DeclaredAccessibility);
        Assert.Equal(Accessibility.ProtectedAndInternal, r.GetProperty("PrivateProtected").DeclaredAccessibility);
        Assert.Equal(Accessibility.ProtectedOrInternal, r.GetProperty("InternalProtected").DeclaredAccessibility);
        Assert.True(r.GetProperty("Static").IsStatic);

        var externProperty = r.GetProperty("Extern");
        Assert.True(externProperty.IsExtern);
        Assert.True(externProperty.GetMethod.IsExtern);

        var externProperty2 = r.GetProperty("Extern2");
        Assert.True(externProperty2.IsExtern);
        Assert.True(externProperty2.IsStatic);
        Assert.True(externProperty2.GetMethod.IsExtern);

        Assert.True(r.GetProperty("RefReadonlyInt").ReturnsByRefReadonly);
    }

    [Fact]
    public void Members_Properties_AllowedModifiers_New()
    {
        var src = """
class UnderlyingClass
{
    public int Property => 0;
}
explicit extension R1 for UnderlyingClass
{
    public new int Property => 0;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Members_Properties_AllowedModifiers_New_Missing()
    {
        var src = """
class UnderlyingClass
{
    public int Property => 0;
}
explicit extension R1 for UnderlyingClass
{
    public int Property => 0;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should warn about hiding
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Members_Properties_DisallowedModifiers()
    {
        var src = """
class UnderlyingClass { }
partial explicit extension R for UnderlyingClass
{
    async int Async => 0; // 1
    partial int Partial { get; } // 2, 3
    scoped System.Span<int> Scoped => throw null; // 4
    abstract int Abstract { get; } // 5, 6
    override int Override => 0; // 7
    readonly int Readonly => 0; // 8
    sealed int Sealed => 0; // 9
    public virtual int Virtual => 0; // 10

    public required int Required { get => throw null; set => throw null; } // 11
    public static required int StaticRequired { get => throw null; set => throw null; } // 12

    file int File => 0; // 13
}
""";
        // PROTOTYPE confirm spec on `required`
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,15): error CS0106: The modifier 'async' is not valid for this item
            //     async int Async => 0; // 1
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Async").WithArguments("async").WithLocation(4, 15),
            // (5,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'implicit/explicit extension', or a method return type.
            //     partial int Partial { get; } // 2, 3
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(5, 5),
            // (5,17): error CS9313: 'R.Partial': cannot declare instance members with state in extension types.
            //     partial int Partial { get; } // 2, 3
            Diagnostic(ErrorCode.ERR_StateInExtension, "Partial").WithArguments("R.Partial").WithLocation(5, 17),
            // (6,29): error CS0106: The modifier 'scoped' is not valid for this item
            //     scoped System.Span<int> Scoped => throw null; // 4
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Scoped").WithArguments("scoped").WithLocation(6, 29),
            // (7,18): error CS0106: The modifier 'abstract' is not valid for this item
            //     abstract int Abstract { get; } // 5, 6
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Abstract").WithArguments("abstract").WithLocation(7, 18),
            // (7,18): error CS9313: 'R.Abstract': cannot declare instance members with state in extension types.
            //     abstract int Abstract { get; } // 5, 6
            Diagnostic(ErrorCode.ERR_StateInExtension, "Abstract").WithArguments("R.Abstract").WithLocation(7, 18),
            // (8,18): error CS0106: The modifier 'override' is not valid for this item
            //     override int Override => 0; // 7
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Override").WithArguments("override").WithLocation(8, 18),
            // (9,18): error CS0106: The modifier 'readonly' is not valid for this item
            //     readonly int Readonly => 0; // 8
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Readonly").WithArguments("readonly").WithLocation(9, 18),
            // (10,16): error CS0106: The modifier 'sealed' is not valid for this item
            //     sealed int Sealed => 0; // 9
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Sealed").WithArguments("sealed").WithLocation(10, 16),
            // (11,24): error CS0106: The modifier 'virtual' is not valid for this item
            //     public virtual int Virtual => 0; // 10
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Virtual").WithArguments("virtual").WithLocation(11, 24),
            // (13,25): error CS0106: The modifier 'required' is not valid for this item
            //     public required int Required { get => throw null; set => throw null; } // 11
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Required").WithArguments("required").WithLocation(13, 25),
            // (14,32): error CS0106: The modifier 'required' is not valid for this item
            //     public static required int StaticRequired { get => throw null; set => throw null; } // 12
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "StaticRequired").WithArguments("required").WithLocation(14, 32),
            // (16,14): error CS0106: The modifier 'file' is not valid for this item
            //     file int File => 0; // 13
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "File").WithArguments("file").WithLocation(16, 14)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.False(r.GetProperty("Abstract").IsAbstract);
        Assert.False(r.GetProperty("Override").IsOverride);
        Assert.False(r.GetProperty("Sealed").IsSealed);
        Assert.False(r.GetProperty("Virtual").IsVirtual);
        Assert.False(r.GetProperty("Required").IsRequired);
        Assert.False(r.GetProperty("StaticRequired").IsRequired);
    }

    [Fact]
    public void Members_Indexers_DisallowedModifiers_Static()
    {
        var src = """
class UnderlyingClass { }
partial explicit extension R for UnderlyingClass
{
    static int this[int i] => i;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,16): error CS0106: The modifier 'static' is not valid for this item
            //     static int this[int i] => i;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(4, 16)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        AssertEx.Equal(new[] { "System.Int32 R.this[System.Int32 i] { get; }", "System.Int32 R.this[System.Int32 i].get" },
            r.GetMembers().ToTestDisplayStrings());

        Assert.False(r.GetIndexer<SourcePropertySymbol>("Item").IsStatic);
    }

    [Fact]
    public void Members_Events_AllowedModifiers()
    {
        var src = """
class UnderlyingClass { }
explicit extension R for UnderlyingClass
{
    event System.Action DefaultAccessibility { add => throw null; remove => throw null; }
    public event System.Action Public { add => throw null; remove => throw null; }
    private event System.Action Private { add => throw null; remove => throw null; }
    internal event System.Action Internal { add => throw null; remove => throw null; }
    protected event System.Action Protected { add => throw null; remove => throw null; }
    private protected event System.Action PrivateProtected { add => throw null; remove => throw null; }
    internal protected event System.Action InternalProtected { add => throw null; remove => throw null; }

    unsafe event System.Action Unsafe { add { int* i = null; } remove => throw null; }
    event System.Action NotUnsafe { add { int* i = null; } remove => throw null; } // 1
    new event System.Action NotNew { add => throw null; remove => throw null; }
    static event System.Action Static { add => throw null; remove => throw null; }

    extern event System.Action Extern { add => throw null; remove => throw null; } // 2, 3

    static extern event System.Action Extern2
    {
        [System.Runtime.InteropServices.DllImport("test")] add; // 4
        [System.Runtime.InteropServices.DllImport("test")] remove; // 5
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should warn that `new` isn't required
        comp.VerifyDiagnostics(
            // (13,43): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     event System.Action NotUnsafe { add { int* i = null; } remove => throw null; } // 1
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(13, 43),
            // (17,41): error CS0179: 'R.Extern.add' cannot be extern and declare a body
            //     extern event System.Action Extern { add => throw null; remove => throw null; } // 2, 3
            Diagnostic(ErrorCode.ERR_ExternHasBody, "add").WithArguments("R.Extern.add").WithLocation(17, 41),
            // (17,60): error CS0179: 'R.Extern.remove' cannot be extern and declare a body
            //     extern event System.Action Extern { add => throw null; remove => throw null; } // 2, 3
            Diagnostic(ErrorCode.ERR_ExternHasBody, "remove").WithArguments("R.Extern.remove").WithLocation(17, 60),
            // (21,63): error CS0073: An add or remove accessor must have a body
            //         [System.Runtime.InteropServices.DllImport("test")] add; // 4
            Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(21, 63),
            // (22,66): error CS0073: An add or remove accessor must have a body
            //         [System.Runtime.InteropServices.DllImport("test")] remove; // 5
            Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(22, 66)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        var defaultAccessibility = r.GetEvent("DefaultAccessibility");
        Assert.Equal(Accessibility.Private, defaultAccessibility.DeclaredAccessibility);
        Assert.False(defaultAccessibility.IsStatic);
        Assert.False(defaultAccessibility.IsAbstract);
        Assert.False(defaultAccessibility.IsOverride);
        Assert.False(defaultAccessibility.IsVirtual);
        Assert.False(defaultAccessibility.IsSealed);
        Assert.False(defaultAccessibility.IsExtern);
        Assert.False(defaultAccessibility.AddMethod.IsExtern);
        Assert.False(defaultAccessibility.RemoveMethod.IsExtern);
        Assert.False(defaultAccessibility.IsExplicitInterfaceImplementation);

        Assert.Equal(Accessibility.Public, r.GetEvent("Public").DeclaredAccessibility);
        Assert.Equal(Accessibility.Private, r.GetEvent("Private").DeclaredAccessibility);
        Assert.Equal(Accessibility.Internal, r.GetEvent("Internal").DeclaredAccessibility);
        Assert.Equal(Accessibility.Protected, r.GetEvent("Protected").DeclaredAccessibility);
        Assert.Equal(Accessibility.ProtectedAndInternal, r.GetEvent("PrivateProtected").DeclaredAccessibility);
        Assert.Equal(Accessibility.ProtectedOrInternal, r.GetEvent("InternalProtected").DeclaredAccessibility);
        Assert.True(r.GetEvent("Static").IsStatic);

        var externEvent = r.GetEvent("Extern");
        Assert.True(externEvent.IsExtern);
        Assert.True(externEvent.AddMethod.IsExtern);
        Assert.True(externEvent.RemoveMethod.IsExtern);

        var externEvent2 = r.GetEvent("Extern2");
        Assert.True(externEvent2.IsExtern);
        Assert.True(externEvent2.IsStatic);
        Assert.True(externEvent2.AddMethod.IsExtern);
        Assert.True(externEvent2.RemoveMethod.IsExtern);
    }

    [Fact]
    public void Members_Events_AllowedModifiers_New()
    {
        var src = """
class UnderlyingClass
{
    public event System.Action Event { add => throw null; remove => throw null; }
}
explicit extension R1 for UnderlyingClass
{
    public new event System.Action Event { add => throw null; remove => throw null; }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Members_Events_AllowedModifiers_New_Missing()
    {
        var src = """
class UnderlyingClass
{
    public event System.Action Event { add => throw null; remove => throw null; }
}
explicit extension R1 for UnderlyingClass
{
    public event System.Action Event { add => throw null; remove => throw null; }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should warn about hiding
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Members_Events_DisallowedModifiers()
    {
        var src = """
class UnderlyingClass { }
partial explicit extension R for UnderlyingClass
{
    async event System.Action Async { add => throw null; remove => throw null; } // 1
    partial event System.Action Partial { add => throw null; remove => throw null; } // 2
    abstract event System.Action Abstract { add => throw null; remove => throw null; } // 3
    override event System.Action Override { add => throw null; remove => throw null; } // 4
    readonly event System.Action Readonly { add => throw null; remove => throw null; } // 5
    sealed event System.Action Sealed { add => throw null; remove => throw null; } // 6
    public virtual event System.Action Virtual { add => throw null; remove => throw null; } // 7
    public required event System.Action Required { add => throw null; remove => throw null; } // 8
    public static required event System.Action StaticRequired { add => throw null; remove => throw null; } // 9
    file event System.Action File { add => throw null; remove => throw null; } // 10
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should refine message for ERR_InvalidMemberDecl
        comp.VerifyDiagnostics(
            // (4,31): error CS0106: The modifier 'async' is not valid for this item
            //     async event System.Action Async { add => throw null; remove => throw null; } // 1
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Async").WithArguments("async").WithLocation(4, 31),
            // (5,13): error CS1519: Invalid token 'event' in class, record, struct, or interface member declaration
            //     partial event System.Action Partial { add => throw null; remove => throw null; } // 2
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "event").WithArguments("event").WithLocation(5, 13),
            // (6,34): error CS0106: The modifier 'abstract' is not valid for this item
            //     abstract event System.Action Abstract { add => throw null; remove => throw null; } // 3
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Abstract").WithArguments("abstract").WithLocation(6, 34),
            // (7,34): error CS0106: The modifier 'override' is not valid for this item
            //     override event System.Action Override { add => throw null; remove => throw null; } // 4
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Override").WithArguments("override").WithLocation(7, 34),
            // (8,34): error CS0106: The modifier 'readonly' is not valid for this item
            //     readonly event System.Action Readonly { add => throw null; remove => throw null; } // 5
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Readonly").WithArguments("readonly").WithLocation(8, 34),
            // (9,32): error CS0106: The modifier 'sealed' is not valid for this item
            //     sealed event System.Action Sealed { add => throw null; remove => throw null; } // 6
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Sealed").WithArguments("sealed").WithLocation(9, 32),
            // (10,40): error CS0106: The modifier 'virtual' is not valid for this item
            //     public virtual event System.Action Virtual { add => throw null; remove => throw null; } // 7
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Virtual").WithArguments("virtual").WithLocation(10, 40),
            // (11,41): error CS0106: The modifier 'required' is not valid for this item
            //     public required event System.Action Required { add => throw null; remove => throw null; } // 8
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Required").WithArguments("required").WithLocation(11, 41),
            // (12,48): error CS0106: The modifier 'required' is not valid for this item
            //     public static required event System.Action StaticRequired { add => throw null; remove => throw null; } // 9
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "StaticRequired").WithArguments("required").WithLocation(12, 48),
            // (13,30): error CS0106: The modifier 'file' is not valid for this item
            //     file event System.Action File { add => throw null; remove => throw null; } // 10
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "File").WithArguments("file").WithLocation(13, 30)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.False(r.GetEvent("Abstract").IsAbstract);
        Assert.False(r.GetEvent("Override").IsOverride);
        Assert.False(r.GetEvent("Sealed").IsSealed);
        Assert.False(r.GetEvent("Virtual").IsVirtual);
    }

    [Theory, CombinatorialData]
    public void Members_ExtensionMethod_DisallowedInExtensionTypes(bool isStatic, bool isExplicit)
    {
        var staticKeyword = isStatic ? "static " : "";
        var keyword = isExplicit ? "explicit" : "implicit";

        var text = $$"""
public class C { }

public {{staticKeyword}}{{keyword}} extension R1 for C
{
    static void M(this int i) { } // 1
}
""";
        var comp = CreateCompilation(text, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,17): error CS9321: Extension methods are not allowed in extension types.
            //     static void M(this int i) { } // 1
            Diagnostic(ErrorCode.ERR_ExtensionMethodInExtension, "M").WithLocation(5, 17)
            );
    }

    [Fact]
    public void Members_ExtensionMethod_StaticExtension_StaticMethod_InThisParameter()
    {
        var src = """
class UnderlyingClass { }
static explicit extension R1 for UnderlyingClass
{
    public static void M(in this int i) { } // 1
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,24): error CS9321: Extension methods are not allowed in extension types.
            //     public static void M(in this int i) { } // 1
            Diagnostic(ErrorCode.ERR_ExtensionMethodInExtension, "M").WithLocation(4, 24)
            );
    }

    [Fact]
    public void Members_ExtensionMethod_DisallowExtensionThisParameter()
    {
        var text = $$"""
public class C { }

public implicit extension R for C { }

public static class E
{
    static void M(this R r) { } // 1
}
""";
        var comp = CreateCompilation(text, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (7,24): error CS1103: The first parameter of an extension method cannot be of type 'R'
            //     static void M(this R r) { } // 1
            Diagnostic(ErrorCode.ERR_BadTypeforThis, "R").WithArguments("R").WithLocation(7, 24)
            );
    }

    [Fact]
    public void TypeMembers_Delegate()
    {
        var src = """
explicit extension R for int
{
    delegate void Delegate();
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var d = comp.GlobalNamespace.GetTypeMember("R").GetTypeMember("Delegate");
        Assert.Equal("R.Delegate", d.ToTestDisplayString());
        Assert.Equal("R", d.ContainingType.ToTestDisplayString());
        Assert.True(d.IsDelegateType());
    }

    [Fact]
    public void TypeMembers_Struct()
    {
        var src = """
explicit extension R for int
{
    struct S { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var d = comp.GlobalNamespace.GetTypeMember("R").GetTypeMember("S");
        Assert.Equal("R.S", d.ToTestDisplayString());
        Assert.Equal("R", d.ContainingType.ToTestDisplayString());
    }

    [Fact]
    public void TypeMembers_Struct_AllowedModifiers()
    {
        var src = """
explicit extension R for int
{
    struct DefaultAccessibility { }
    public struct Public { }
    private struct Private { }
    internal struct Internal { }
    protected struct Protected { }
    private protected struct PrivateProtected { }
    internal protected struct InternalProtected { }
    unsafe struct Unsafe { void M(int* i) => throw null; }
    struct NotUnsafe { void M(int* i) => throw null; } // 1
    new struct NotNew { }
    partial struct Partial { }
    readonly struct Readonly { }
    ref struct Ref { }
}
partial explicit extension R2 for int
{
    partial struct Partial { }
}
partial explicit extension R2 for int
{
    partial struct Partial { }
}
""";
        // PROTOTYPE should warn that `new` isn't required
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (11,31): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     struct NotUnsafe { void M(int* i) => throw null; } // 1
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(11, 31)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        var defaultAccessibility = r.GetTypeMember("DefaultAccessibility");
        Assert.Equal(Accessibility.Private, defaultAccessibility.DeclaredAccessibility);
        Assert.False(defaultAccessibility.IsStatic);
        Assert.False(defaultAccessibility.IsAbstract);
        Assert.False(defaultAccessibility.IsOverride);
        Assert.False(defaultAccessibility.IsVirtual);
        Assert.True(defaultAccessibility.IsSealed);
        Assert.False(defaultAccessibility.IsExtern);
        Assert.False(defaultAccessibility.IsReadOnly);

        Assert.Equal(Accessibility.Public, r.GetTypeMember("Public").DeclaredAccessibility);
        Assert.Equal(Accessibility.Private, r.GetTypeMember("Private").DeclaredAccessibility);
        Assert.Equal(Accessibility.Internal, r.GetTypeMember("Internal").DeclaredAccessibility);
        Assert.Equal(Accessibility.Protected, r.GetTypeMember("Protected").DeclaredAccessibility);
        Assert.Equal(Accessibility.ProtectedAndInternal, r.GetTypeMember("PrivateProtected").DeclaredAccessibility);
        Assert.Equal(Accessibility.ProtectedOrInternal, r.GetTypeMember("InternalProtected").DeclaredAccessibility);
        Assert.True(r.GetTypeMember("Readonly").IsReadOnly);
    }

    [Fact]
    public void TypeMembers_Struct_DisallowedModifiers()
    {
        var src = """
explicit extension R for int
{
    async struct Async { } // 1
    abstract struct Abstract { } // 2
    override struct Override { } // 3
    sealed struct Sealed { } // 4
    virtual struct Virtual { } // 5
    required struct Required { } // 6
    file struct File { } // 7
    static struct Static { } // 8
    ref record struct RefRecordStruct { } // 9
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,18): error CS0106: The modifier 'async' is not valid for this item
            //     async struct Async { } // 1
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Async").WithArguments("async").WithLocation(3, 18),
            // (4,21): error CS0106: The modifier 'abstract' is not valid for this item
            //     abstract struct Abstract { } // 2
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Abstract").WithArguments("abstract").WithLocation(4, 21),
            // (5,21): error CS0106: The modifier 'override' is not valid for this item
            //     override struct Override { } // 3
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Override").WithArguments("override").WithLocation(5, 21),
            // (6,19): error CS0106: The modifier 'sealed' is not valid for this item
            //     sealed struct Sealed { } // 4
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Sealed").WithArguments("sealed").WithLocation(6, 19),
            // (7,20): error CS0106: The modifier 'virtual' is not valid for this item
            //     virtual struct Virtual { } // 5
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Virtual").WithArguments("virtual").WithLocation(7, 20),
            // (8,21): error CS0106: The modifier 'required' is not valid for this item
            //     required struct Required { } // 6
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Required").WithArguments("required").WithLocation(8, 21),
            // (9,17): error CS9054: File-local type 'R.File' must be defined in a top level type; 'R.File' is a nested type.
            //     file struct File { } // 7
            Diagnostic(ErrorCode.ERR_FileTypeNested, "File").WithArguments("R.File").WithLocation(9, 17),
            // (10,19): error CS0106: The modifier 'static' is not valid for this item
            //     static struct Static { } // 8
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Static").WithArguments("static").WithLocation(10, 19),
            // (11,23): error CS0106: The modifier 'ref' is not valid for this item
            //     ref record struct RefRecordStruct { } // 9
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "RefRecordStruct").WithArguments("ref").WithLocation(11, 23)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.False(r.GetTypeMember("Abstract").IsAbstract);
        Assert.False(r.GetTypeMember("Override").IsOverride);
        Assert.True(r.GetTypeMember("Sealed").IsSealed);
        Assert.False(r.GetTypeMember("Virtual").IsVirtual);
    }

    [Fact]
    public void TypeMembers_Class_AllowedModifiers()
    {
        var src = """
explicit extension R for int
{
    class DefaultAccessibility { }
    public class Public { }
    private class Private { }
    internal class Internal { }
    protected class Protected { }
    private protected class PrivateProtected { }
    internal protected class InternalProtected { }
    unsafe class Unsafe { void M(int* i) => throw null; }
    class NotUnsafe { void M(int* i) => throw null; } // 1
    new class NotNew { }
    partial class Partial { }
    sealed class Sealed { }
    static class Static { }
    abstract class Abstract { }
}
""";
        // PROTOTYPE should warn that `new` isn't required
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (11,30): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     class NotUnsafe { void M(int* i) => throw null; } // 1
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(11, 30)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        var defaultAccessibility = r.GetTypeMember("DefaultAccessibility");
        Assert.Equal(Accessibility.Private, defaultAccessibility.DeclaredAccessibility);
        Assert.False(defaultAccessibility.IsStatic);
        Assert.False(defaultAccessibility.IsAbstract);
        Assert.False(defaultAccessibility.IsOverride);
        Assert.False(defaultAccessibility.IsVirtual);
        Assert.False(defaultAccessibility.IsSealed);
        Assert.False(defaultAccessibility.IsExtern);
        Assert.False(defaultAccessibility.IsReadOnly);

        Assert.Equal(Accessibility.Public, r.GetTypeMember("Public").DeclaredAccessibility);
        Assert.Equal(Accessibility.Private, r.GetTypeMember("Private").DeclaredAccessibility);
        Assert.Equal(Accessibility.Internal, r.GetTypeMember("Internal").DeclaredAccessibility);
        Assert.Equal(Accessibility.Protected, r.GetTypeMember("Protected").DeclaredAccessibility);
        Assert.Equal(Accessibility.ProtectedAndInternal, r.GetTypeMember("PrivateProtected").DeclaredAccessibility);
        Assert.Equal(Accessibility.ProtectedOrInternal, r.GetTypeMember("InternalProtected").DeclaredAccessibility);
        Assert.True(r.GetTypeMember("Sealed").IsSealed);
        Assert.True(r.GetTypeMember("Abstract").IsAbstract);
    }

    [Fact]
    public void TypeMembers_Class_DisallowedModifiers()
    {
        var src = """
explicit extension R for int
{
    async class Async { } // 1
    override class Override { } // 2
    virtual class Virtual { } // 3
    required class Required { } // 4
    file class File { } // 5
    readonly class Readonly { } // 6
    static record StaticRecord { } // 7
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,17): error CS0106: The modifier 'async' is not valid for this item
            //     async class Async { } // 1
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Async").WithArguments("async").WithLocation(3, 17),
            // (4,20): error CS0106: The modifier 'override' is not valid for this item
            //     override class Override { } // 2
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Override").WithArguments("override").WithLocation(4, 20),
            // (5,19): error CS0106: The modifier 'virtual' is not valid for this item
            //     virtual class Virtual { } // 3
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Virtual").WithArguments("virtual").WithLocation(5, 19),
            // (6,20): error CS0106: The modifier 'required' is not valid for this item
            //     required class Required { } // 4
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Required").WithArguments("required").WithLocation(6, 20),
            // (7,16): error CS9054: File-local type 'R.File' must be defined in a top level type; 'R.File' is a nested type.
            //     file class File { } // 5
            Diagnostic(ErrorCode.ERR_FileTypeNested, "File").WithArguments("R.File").WithLocation(7, 16),
            // (8,20): error CS0106: The modifier 'readonly' is not valid for this item
            //     readonly class Readonly { } // 6
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Readonly").WithArguments("readonly").WithLocation(8, 20),
            // (9,19): error CS0106: The modifier 'static' is not valid for this item
            //     static record StaticRecord { } // 7
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "StaticRecord").WithArguments("static").WithLocation(9, 19)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.False(r.GetTypeMember("Override").IsOverride);
        Assert.False(r.GetTypeMember("Virtual").IsVirtual);
        Assert.False(r.GetTypeMember("Readonly").IsReadOnly);
    }

    [Fact]
    public void TypeMembers_Interface_AllowedModifiers()
    {
        var src = """
explicit extension R for int
{
    interface DefaultAccessibility { }
    public interface Public { }
    private interface Private { }
    internal interface Internal { }
    protected interface Protected { }
    private protected interface PrivateProtected { }
    internal protected interface InternalProtected { }
    unsafe interface Unsafe { void M(int* i) => throw null; }
    interface NotUnsafe { void M(int* i) => throw null; } // 1
    new interface NotNew { }
    partial interface Partial { }
}
""";
        // PROTOTYPE should warn that `new` isn't required
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (11,34): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     interface NotUnsafe { void M(int* i) => throw null; } // 1
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(11, 34)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        var defaultAccessibility = r.GetTypeMember("DefaultAccessibility");
        Assert.Equal(Accessibility.Private, defaultAccessibility.DeclaredAccessibility);
        Assert.False(defaultAccessibility.IsStatic);
        Assert.True(defaultAccessibility.IsAbstract);
        Assert.False(defaultAccessibility.IsOverride);
        Assert.False(defaultAccessibility.IsVirtual);
        Assert.False(defaultAccessibility.IsSealed);
        Assert.False(defaultAccessibility.IsExtern);
        Assert.False(defaultAccessibility.IsReadOnly);

        Assert.Equal(Accessibility.Public, r.GetTypeMember("Public").DeclaredAccessibility);
        Assert.Equal(Accessibility.Private, r.GetTypeMember("Private").DeclaredAccessibility);
        Assert.Equal(Accessibility.Internal, r.GetTypeMember("Internal").DeclaredAccessibility);
        Assert.Equal(Accessibility.Protected, r.GetTypeMember("Protected").DeclaredAccessibility);
        Assert.Equal(Accessibility.ProtectedAndInternal, r.GetTypeMember("PrivateProtected").DeclaredAccessibility);
        Assert.Equal(Accessibility.ProtectedOrInternal, r.GetTypeMember("InternalProtected").DeclaredAccessibility);
    }

    [Fact]
    public void TypeMembers_Interface_DisallowedModifiers()
    {
        var src = """
explicit extension R for int
{
    async interface Async { } // 1
    abstract interface Abstract { } // 2
    override interface Override { } // 3
    virtual interface Virtual { } // 4
    required interface Required { } // 5
    file interface File { } // 6
    readonly interface Readonly { } // 7
    sealed interface Sealed { } // 8
    static interface Static { } // 9
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,21): error CS0106: The modifier 'async' is not valid for this item
            //     async interface Async { } // 1
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Async").WithArguments("async").WithLocation(3, 21),
            // (4,24): error CS0106: The modifier 'abstract' is not valid for this item
            //     abstract interface Abstract { } // 2
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Abstract").WithArguments("abstract").WithLocation(4, 24),
            // (5,24): error CS0106: The modifier 'override' is not valid for this item
            //     override interface Override { } // 3
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Override").WithArguments("override").WithLocation(5, 24),
            // (6,23): error CS0106: The modifier 'virtual' is not valid for this item
            //     virtual interface Virtual { } // 4
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Virtual").WithArguments("virtual").WithLocation(6, 23),
            // (7,24): error CS0106: The modifier 'required' is not valid for this item
            //     required interface Required { } // 5
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Required").WithArguments("required").WithLocation(7, 24),
            // (8,20): error CS9054: File-local type 'R.File' must be defined in a top level type; 'R.File' is a nested type.
            //     file interface File { } // 6
            Diagnostic(ErrorCode.ERR_FileTypeNested, "File").WithArguments("R.File").WithLocation(8, 20),
            // (9,24): error CS0106: The modifier 'readonly' is not valid for this item
            //     readonly interface Readonly { } // 7
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Readonly").WithArguments("readonly").WithLocation(9, 24),
            // (10,22): error CS0106: The modifier 'sealed' is not valid for this item
            //     sealed interface Sealed { } // 8
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Sealed").WithArguments("sealed").WithLocation(10, 22),
            // (11,22): error CS0106: The modifier 'static' is not valid for this item
            //     static interface Static { } // 9
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Static").WithArguments("static").WithLocation(11, 22)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.True(r.GetTypeMember("Abstract").IsAbstract);
        Assert.False(r.GetTypeMember("Override").IsOverride);
        Assert.False(r.GetTypeMember("Virtual").IsVirtual);
        Assert.False(r.GetTypeMember("Readonly").IsReadOnly);
        Assert.False(r.GetTypeMember("Sealed").IsSealed);
        Assert.False(r.GetTypeMember("Static").IsStatic);
    }

    [Fact]
    public void ForStruct()
    {
        var src = """
interface I { }
struct UnderlyingStruct : I { }
explicit extension R for UnderlyingStruct
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        static void validate(ModuleSymbol module)
        {
            bool inSource = module is SourceModuleSymbol;
            var r = module.GlobalNamespace.GetTypeMember("R");

            Assert.Equal("R", r.ToTestDisplayString());
            if (inSource)
            {
                VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
            }
            else
            {
                VerifyExtension<PENamedTypeSymbol>(r, isExplicit: true);
            }

            Assert.Equal("UnderlyingStruct", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.GetMembers());

            Assert.Null(r.BaseTypeNoUseSiteDiagnostics);
            Assert.False(r.IsStatic);

            Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
            Assert.Null(r.ContainingType);
            Assert.Empty(r.TypeParameters);
        }
    }

    [Theory, CombinatorialData]
    public void ForTypeParameter(bool isExplicit)
    {
        var src = $$"""
interface I { }
{{(isExplicit ? "explicit" : "implicit")}} extension R<T> for T where T : I
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        void validate(ModuleSymbol module)
        {
            bool inSource = module is SourceModuleSymbol;
            var r = module.GlobalNamespace.GetTypeMember("R");
            Assert.Equal("R<T>", r.ToTestDisplayString());
            if (inSource)
            {
                VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: isExplicit);
            }
            else
            {
                VerifyExtension<PENamedTypeSymbol>(r, isExplicit: isExplicit);
            }

            Assert.Equal("T", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.GetMembers());

            Assert.Null(r.BaseTypeNoUseSiteDiagnostics);
            Assert.False(r.IsStatic);

            Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
            Assert.Null(r.ContainingType);
            Assert.Equal(new[] { "T" }, r.TypeParameters.ToTestDisplayStrings());
        }
    }

    [Theory, CombinatorialData]
    public void ForEnum(bool isExplicit)
    {
        var src = $$"""
enum E { }
{{(isExplicit ? "explicit" : "implicit")}} extension R for E
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        void validate(ModuleSymbol module)
        {
            bool inSource = module is SourceModuleSymbol;
            var r = module.GlobalNamespace.GetTypeMember("R");
            Assert.Equal("R", r.ToTestDisplayString());
            if (inSource)
            {
                VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: isExplicit);
            }
            else
            {
                VerifyExtension<PENamedTypeSymbol>(r, isExplicit: isExplicit);
            }

            Assert.Equal("E", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.GetMembers());

            Assert.Null(r.BaseTypeNoUseSiteDiagnostics);
            Assert.False(r.IsStatic);

            Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
            Assert.Null(r.ContainingType);
            Assert.Empty(r.TypeParameters);
        }
    }

    [Fact]
    public void ForObject()
    {
        var src = """
explicit extension R for object
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("System.Object", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
        Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);
        Assert.Empty(r.GetMembers());
        Assert.Null(r.BaseTypeNoUseSiteDiagnostics);
        Assert.False(r.IsStatic);

        Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
        Assert.Null(r.ContainingType);
        Assert.Empty(r.TypeParameters);
    }

    [Fact]
    public void ForSubstitutedType()
    {
        var src = """
class C<T> { }
explicit extension R<U> for C<U>
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        static void validate(ModuleSymbol module)
        {
            bool inSource = module is SourceModuleSymbol;
            var r = module.GlobalNamespace.GetTypeMember("R");
            Assert.Equal("R<U>", r.ToTestDisplayString());
            if (inSource)
            {
                VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
            }
            else
            {
                VerifyExtension<PENamedTypeSymbol>(r, isExplicit: true);
            }

            Assert.Equal("C<U>", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.GetMembers());

            Assert.Null(r.BaseTypeNoUseSiteDiagnostics);
            Assert.False(r.IsStatic);

            Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
            Assert.Null(r.ContainingType);
            Assert.Equal(new[] { "U" }, r.TypeParameters.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void ForTuple()
    {
        var src = """
explicit extension R for (int, int)
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        static void validate(ModuleSymbol module)
        {
            bool inSource = module is SourceModuleSymbol;
            var r = module.GlobalNamespace.GetTypeMember("R");
            Assert.Equal("R", r.ToTestDisplayString());
            if (inSource)
            {
                VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
            }
            else
            {
                VerifyExtension<PENamedTypeSymbol>(r, isExplicit: true);
            }

            Assert.Equal("(System.Int32, System.Int32)", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.GetMembers());

            Assert.Null(r.BaseTypeNoUseSiteDiagnostics);
            Assert.False(r.IsStatic);

            Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
            Assert.Null(r.ContainingType);
            Assert.Empty(r.TypeParameters);
        }
    }

    [Fact]
    public void ForArrayType()
    {
        var src = """
explicit extension R for int[]
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        static void validate(ModuleSymbol module)
        {
            bool inSource = module is SourceModuleSymbol;
            var r = module.GlobalNamespace.GetTypeMember("R");
            Assert.Equal("R", r.ToTestDisplayString());
            if (inSource)
            {
                VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
            }
            else
            {
                VerifyExtension<PENamedTypeSymbol>(r, isExplicit: true);
            }

            Assert.Equal("System.Int32[]", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.GetMembers());

            Assert.Null(r.BaseTypeNoUseSiteDiagnostics);
            Assert.False(r.IsStatic);

            Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
            Assert.Null(r.ContainingType);
            Assert.Empty(r.TypeParameters);
        }
    }

    [Fact]
    public void Scopes()
    {
        var src = """
class ContainingType<TContaining>
{
    class UnderlyingClass<T> { }
    explicit extension BaseExtension<T> for UnderlyingClass<T> { }
    explicit extension R1 for UnderlyingClass<TContaining> : BaseExtension<TContaining> { }
    explicit extension R2<TCurrent> for UnderlyingClass<TCurrent> : BaseExtension<TCurrent> { }
    explicit extension R3 for UnderlyingClass<C> : BaseExtension<C>
    {
        class C { }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (7,47): error CS0246: The type or namespace name 'C' could not be found (are you missing a using directive or an assembly reference?)
            //     explicit extension R3 for UnderlyingClass<C> : BaseExtension<C>
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C").WithArguments("C").WithLocation(7, 47),
            // (7,66): error CS0246: The type or namespace name 'C' could not be found (are you missing a using directive or an assembly reference?)
            //     explicit extension R3 for UnderlyingClass<C> : BaseExtension<C>
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C").WithArguments("C").WithLocation(7, 66)
            );
    }

    [Fact]
    public void WithTypeParameters_Explicit()
    {
        var src = """
interface I { }
class UnderlyingClass : I { }
explicit extension R<T1, T2> for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal(new[] { "T1", "T2" }, r.TypeParameters.ToTestDisplayStrings());
        Assert.True(r.IsGenericType);
        Assert.Equal(2, r.Arity);
    }

    [Fact]
    public void WithTypeParameters_Variance()
    {
        var src = """
class C { }
explicit extension R<in T1, out T2> for C
{
    T1 M1(T1 t1) => throw null;
    T2 M2(T2 t2) => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,22): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
            // explicit extension R<in T1, out T2> for UnderlyingClass
            Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "in").WithLocation(2, 22),
            // (2,29): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
            // explicit extension R<in T1, out T2> for UnderlyingClass
            Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(2, 29)
            );
    }

    [Fact]
    public void VarianceInterfaceNesting()
    {
        var src = """
class C { }
interface I<T>
{
    explicit extension R for C { }
}
interface IIn<in T>
{
    explicit extension R for C { }
}
interface IOut<out T>
{
    explicit extension R for C { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (8,24): error CS8427: Enums, classes, structures, and extensions cannot be declared in an interface that has an 'in' or 'out' type parameter.
            //     explicit extension R for C { }
            Diagnostic(ErrorCode.ERR_VarianceInterfaceNesting, "R").WithLocation(8, 24),
            // (12,24): error CS8427: Enums, classes, structures, and extensions cannot be declared in an interface that has an 'in' or 'out' type parameter.
            //     explicit extension R for C { }
            Diagnostic(ErrorCode.ERR_VarianceInterfaceNesting, "R").WithLocation(12, 24)
            );
    }

    [Fact]
    public void WithPrimaryConstructor()
    {
        var src = """
interface I { }
class UnderlyingClass : I { }
explicit extension R(int i) for UnderlyingClass { }
""";
        // PROTOTYPE should parse but remain error
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,20): error CS9314: No part of a partial extension 'R' includes an underlying type specification.
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_ExtensionMissingUnderlyingType, "R").WithArguments("R").WithLocation(3, 20),
            // (3,21): error CS1514: { expected
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(3, 21),
            // (3,21): error CS1513: } expected
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(3, 21),
            // (3,21): error CS8803: Top-level statements must precede namespace and type declarations.
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "(int ").WithLocation(3, 21),
            // (3,22): error CS1525: Invalid expression term 'int'
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(3, 22),
            // (3,22): error CS0119: 'int' is a type, which is not valid in the given context
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_BadSKunknown, "int").WithArguments("int", "type").WithLocation(3, 22),
            // (3,26): error CS1026: ) expected
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "i").WithLocation(3, 26),
            // (3,26): error CS1002: ; expected
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "i").WithLocation(3, 26),
            // (3,26): error CS0246: The type or namespace name 'i' could not be found (are you missing a using directive or an assembly reference?)
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "i").WithArguments("i").WithLocation(3, 26),
            // (3,27): error CS1001: Identifier expected
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(3, 27),
            // (3,27): error CS1003: Syntax error, ',' expected
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(3, 27),
            // (3,51): error CS1002: ; expected
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(3, 51),
            // (3,51): error CS1022: Type or namespace definition, or end-of-file expected
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(3, 51)
            );
    }

    [Fact]
    public void UnderlyingType_InstanceType_StaticExtension()
    {
        var src = """
class UnderlyingClass { }
static explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.True(r.IsStatic);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void UnderlyingType_StaticType_StaticExtension()
    {
        var src = """
static class UnderlyingClass { }
static explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.True(r.IsStatic);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void UnderlyingType_StaticType_InstanceExtension()
    {
        var src = """
static class UnderlyingClass { }
explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.False(r.IsStatic);
        comp.VerifyDiagnostics(
            // (2,26): error CS9306: Instance extension 'R' cannot extend type 'UnderlyingClass' because it is static.
            // explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_StaticBaseTypeOnInstanceExtension, "UnderlyingClass").WithArguments("R", "UnderlyingClass").WithLocation(2, 26)
            );
    }

    [Fact]
    public void UnderlyingType_StaticType_InstanceExtension_PE()
    {
        // static class C { }
        // explicit extension R for C { }
        var ilSource = """
.class public auto ansi abstract sealed beforefieldinit C
    extends [mscorlib]System.Object
{
}

.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '<ExplicitExtension>$'(class C '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R3 for C : R1 { }
public static explicit extension R4 for C : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        // PROTOTYPE: should report use-site diagnostics for R1 instead
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R3' extends 'C' but base extension 'R1' extends 'C'.
            // public explicit extension R3 for C : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R3").WithArguments("R3", "C", "R1", "C").WithLocation(1, 27),
            // (1,34): error CS9306: Instance extension 'R3' cannot extend type 'C' because it is static.
            // public explicit extension R3 for C : R1 { }
            Diagnostic(ErrorCode.ERR_StaticBaseTypeOnInstanceExtension, "C").WithArguments("R3", "C").WithLocation(1, 34),
            // (2,34): error CS9316: Extension 'R4' extends 'C' but base extension 'R1' extends 'C'.
            // public static explicit extension R4 for C : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R4").WithArguments("R4", "C", "R1", "C").WithLocation(2, 34)
            );

        var r1 = (PENamedTypeSymbol)comp.GlobalNamespace.GetTypeMember("R1");
        var r1ExtendedType = r1.ExtendedTypeNoUseSiteDiagnostics;
        Assert.Equal("C", r1ExtendedType.ToTestDisplayString());
        Assert.True(r1ExtendedType.IsErrorType());
    }

    [Fact]
    public void UnderlyingType_StaticType_InstanceExtension_Retargeting()
    {
        var src1 = """
public class C { }
""";
        var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net70,
            assemblyName: "first");
        comp1.VerifyDiagnostics();

        var src2 = """
public explicit extension E1 for C { }
""";
        var comp2 = CreateCompilation(src2, targetFramework: TargetFramework.Net70,
            references: new[] { comp1.EmitToImageReference() });
        comp2.VerifyDiagnostics();

        var src1Updated = """
public static class C { }
""";
        var comp1Updated = CreateCompilation(src1Updated, targetFramework: TargetFramework.Net70,
            assemblyName: "first");
        comp1Updated.VerifyDiagnostics();

        var src = """
public explicit extension E2 for C : E1 { }
""";
        // PROTOTYPE : should report use-site diagnostics for using E1
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70,
            references: new[] { comp2.ToMetadataReference(), comp1Updated.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'E2' extends 'C' but base extension 'E1' extends 'C'.
            // public explicit extension E2 for C : E1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "E2").WithArguments("E2", "C", "E1", "C").WithLocation(1, 27),
            // (1,34): error CS9306: Instance extension 'E2' cannot extend type 'C' because it is static.
            // public explicit extension E2 for C : E1 { }
            Diagnostic(ErrorCode.ERR_StaticBaseTypeOnInstanceExtension, "C").WithArguments("E2", "C").WithLocation(1, 34)
            );

        var e2 = comp.GlobalNamespace.GetTypeMember("E2");
        var e1 = (RetargetingNamedTypeSymbol)e2.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("E1", e1.Name);
        AssertEx.Equal("error CS8090: There is an error in a referenced assembly 'first, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.",
            ((ErrorTypeSymbol)e1.ExtendedTypeNoUseSiteDiagnostics).ErrorInfo.ToString());
    }

    [Theory, CombinatorialData]
    public void UnderlyingType_Extension(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";

        var text = $$"""
public explicit extension E0 for object { }

public {{keyword}} extension E1 for E0
{
}
""";
        var comp = CreateCompilation(text, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,34): error CS9305: The extended type may not be dynamic, a pointer, a ref struct, or an extension.
            // public explicit extension E1 for E0
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "E0").WithLocation(3, 34)
            );
    }

    [Theory, CombinatorialData]
    public void UnderlyingType_Extension_Retargeting(bool isExplicit)
    {
        var src1 = $$"""
public class E0 { }
public {{(isExplicit ? "explicit" : "implicit")}} extension E1 for E0 { }
""";
        var comp1 = CreateCompilation(src1, assemblyName: "first",
            targetFramework: TargetFramework.Net70);
        comp1.VerifyDiagnostics();

        var src2 = """
public explicit extension E2 for E0 : E1 { }
""";

        var comp2 = CreateCompilation(src2, references: new[] { comp1.ToMetadataReference() },
            targetFramework: TargetFramework.Net70);
        comp2.VerifyDiagnostics();

        var src1Updated = $$"""
public {{(isExplicit ? "explicit" : "implicit")}} extension E0 for E1 { }
public class E1 { }
""";
        var comp1Updated = CreateCompilation(src1Updated, assemblyName: "first",
            targetFramework: TargetFramework.Net70);
        comp1Updated.VerifyDiagnostics();

        var src = """
public explicit extension E3 for E1 : E2 { }
public explicit extension E4 for E0 : E2 { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70,
            references: new[] { comp2.ToMetadataReference(), comp1Updated.EmitToImageReference() });
        // PROTOTYPE The diagnostic for using E2 should mention the faulty type
        comp.VerifyDiagnostics(
            // (1,27): error CS8090: There is an error in a referenced assembly 'first, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // public explicit extension E3 for E1 : E2 { }
            Diagnostic(ErrorCode.ERR_ErrorInReferencedAssembly, "E3").WithArguments("first, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 27),
            // (1,27): error CS9316: Extension 'E3' extends 'E1' but base extension 'E2' extends 'E0'.
            // public explicit extension E3 for E1 : E2 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "E3").WithArguments("E3", "E1", "E2", "E0").WithLocation(1, 27),
            // (2,27): error CS8090: There is an error in a referenced assembly 'first, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // public explicit extension E4 for E0 : E2 { }
            Diagnostic(ErrorCode.ERR_ErrorInReferencedAssembly, "E4").WithArguments("first, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 27),
            // (2,34): error CS9305: The extended type may not be dynamic, a pointer, a ref struct, or an extension.
            // public explicit extension E4 for E0 : E2 { }
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "E0").WithLocation(2, 34)
            );

        var e2 = (RetargetingNamedTypeSymbol)comp.GlobalNamespace.GetTypeMember("E2");

        AssertEx.Equal("error CS8090: There is an error in a referenced assembly 'first, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.",
            ((ErrorTypeSymbol)e2.ExtendedTypeNoUseSiteDiagnostics).ErrorInfo.ToString());

        AssertEx.Equal("error CS8090: There is an error in a referenced assembly 'first, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.",
            ((ErrorTypeSymbol)e2.BaseExtensionsNoUseSiteDiagnostics.Single()).ErrorInfo.ToString());
    }

    [Fact]
    public void UnderlyingType_SealedType()
    {
        var src = """
sealed class UnderlyingClass { }
explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.False(r.IsSealed);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void UnderlyingType_FileType_FileExtension()
    {
        var src = """
file class UnderlyingClass { }
file explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R@<tree 0>", r.ToTestDisplayString());
        Assert.Equal("UnderlyingClass@<tree 0>", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void UnderlyingType_FileType_NonFileExtension()
    {
        var src = """
file class UnderlyingClass { }
explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,20): error CS9312: File-local type 'UnderlyingClass' cannot be used as a underlying type of non-file-local extension 'R'.
            // explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_FileTypeUnderlying, "R").WithArguments("UnderlyingClass", "R").WithLocation(2, 20)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("UnderlyingClass@<tree 0>", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void UnderlyingType_FileContainingType_NonFileExtension()
    {
        var src = """
file class Outer
{
    internal class UnderlyingClass { }
}
explicit extension R for Outer.UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,20): error CS9312: File-local type 'Outer.UnderlyingClass' cannot be used as a underlying type of non-file-local extension 'R'.
            // explicit extension R for Outer.UnderlyingClass
            Diagnostic(ErrorCode.ERR_FileTypeUnderlying, "R").WithArguments("Outer.UnderlyingClass", "R").WithLocation(5, 20)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.Equal("Outer@<tree 0>.UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void UnderlyingType_FileContainingType_NonFileExtension_Partial()
    {
        var src = """
file class Outer
{
    internal class UnderlyingClass { }
}
partial explicit extension R for Outer.UnderlyingClass { } // 1
partial explicit extension R for Outer.UnderlyingClass { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,28): error CS9312: File-local type 'Outer.UnderlyingClass' cannot be used as a underlying type of non-file-local extension 'R'.
            // partial explicit extension R for Outer.UnderlyingClass { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeUnderlying, "R").WithArguments("Outer.UnderlyingClass", "R").WithLocation(5, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.Equal("Outer@<tree 0>.UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void UnderlyingType_DuplicatesWithDifferentUnderlyingTypes()
    {
        var src = """
class UnderlyingClass1 { }
class UnderlyingClass2 { }

explicit extension R1 for UnderlyingClass1 { } // 1
explicit extension R1 for UnderlyingClass2 { } // 2

explicit extension R2 for UnderlyingClass1 { } // 3, 4
implicit extension R2 for UnderlyingClass2 { } // 5
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,20): error CS9308: Partial declarations of 'R1' must not extend different types.
            // explicit extension R1 for UnderlyingClass1 { } // 1
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R1").WithArguments("R1").WithLocation(4, 20),
            // (5,20): error CS0101: The namespace '<global namespace>' already contains a definition for 'R1'
            // explicit extension R1 for UnderlyingClass2 { } // 2
            Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "R1").WithArguments("R1", "<global namespace>").WithLocation(5, 20),
            // (7,20): error CS9315: Partial declarations of 'R2' must specify the same extension modifier ('implicit' or 'explicit').
            // explicit extension R2 for UnderlyingClass1 { } // 3, 4
            Diagnostic(ErrorCode.ERR_PartialDifferentExtensionModifiers, "R2").WithArguments("R2").WithLocation(7, 20),
            // (7,20): error CS9308: Partial declarations of 'R2' must not extend different types.
            // explicit extension R2 for UnderlyingClass1 { } // 3, 4
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R2").WithArguments("R2").WithLocation(7, 20),
            // (8,20): error CS0101: The namespace '<global namespace>' already contains a definition for 'R2'
            // implicit extension R2 for UnderlyingClass2 { } // 5
            Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "R2").WithArguments("R2", "<global namespace>").WithLocation(8, 20)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        Assert.Equal("UnderlyingClass1", r1.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        Assert.Equal("UnderlyingClass1", r2.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void UnderlyingType_TypeConstraints()
    {
        var src = """
#nullable enable
interface I { }

class CDefault<T> { }
class CClass<T> where T : class { }
class CStruct<T> where T : struct { }
class CNotNull<T> where T : notnull { }

partial explicit extension RDefault1<T> for CDefault<T> { } // 1, 2
partial explicit extension RDefault1<U> for CDefault<U> { } // 3

explicit extension RDefault2<T> for CDefault<T> { }

partial explicit extension RClass1 { }
partial explicit extension RClass1 for CClass<string> { }

partial explicit extension RClass2 { }
partial explicit extension RClass2 for CClass<int> { } // 4

partial explicit extension RClass3 for CClass<int> { } // 5
partial explicit extension RClass3 { }

partial explicit extension RStruct1 { }
partial explicit extension RStruct1 for CStruct<string> { } // 6

partial explicit extension RStruct2 { }
partial explicit extension RStruct2 for CStruct<int> { }

partial explicit extension RStruct3<T> for CStruct<T> { } // 7
partial explicit extension RStruct3<T> { }

partial explicit extension RNotNull1 for CNotNull<string> { }

partial explicit extension RNotNull2 for CNotNull<string?> { } // 8
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (9,28): error CS0264: Partial declarations of 'RDefault1<T>' must have the same type parameter names in the same order
            // partial explicit extension RDefault1<T> for CDefault<T> { } // 1, 2
            Diagnostic(ErrorCode.ERR_PartialWrongTypeParams, "RDefault1").WithArguments("RDefault1<T>").WithLocation(9, 28),
            // (9,28): error CS9308: Partial declarations of 'RDefault1<T>' must not extend different types.
            // partial explicit extension RDefault1<T> for CDefault<T> { } // 1, 2
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "RDefault1").WithArguments("RDefault1<T>").WithLocation(9, 28),
            // (10,54): error CS0246: The type or namespace name 'U' could not be found (are you missing a using directive or an assembly reference?)
            // partial explicit extension RDefault1<U> for CDefault<U> { } // 3
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "U").WithArguments("U").WithLocation(10, 54),
            // (18,28): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'CClass<T>'
            // partial explicit extension RClass2 for CClass<int> { } // 4
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "RClass2").WithArguments("CClass<T>", "T", "int").WithLocation(18, 28),
            // (20,28): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'CClass<T>'
            // partial explicit extension RClass3 for CClass<int> { } // 5
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "RClass3").WithArguments("CClass<T>", "T", "int").WithLocation(20, 28),
            // (24,28): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'CStruct<T>'
            // partial explicit extension RStruct1 for CStruct<string> { } // 6
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "RStruct1").WithArguments("CStruct<T>", "T", "string").WithLocation(24, 28),
            // (29,28): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'CStruct<T>'
            // partial explicit extension RStruct3<T> for CStruct<T> { } // 7
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "RStruct3").WithArguments("CStruct<T>", "T", "T").WithLocation(29, 28),
            // (34,28): warning CS8714: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'CNotNull<T>'. Nullability of type argument 'string?' doesn't match 'notnull' constraint.
            // partial explicit extension RNotNull2 for CNotNull<string?> { } // 8
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "RNotNull2").WithArguments("CNotNull<T>", "T", "string?").WithLocation(34, 28)
            );
    }

    [Fact]
    public void UnderlyingType_TypeParametersMustBeUsed()
    {
        var src = """
class C { }
class D<T> { }

explicit extension R1<T> for C { }
explicit extension R2<T> for D<T> { }

implicit extension R3<T> for C { } // 1
implicit extension R4<T> for D<T> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (7,20): error CS9328: The underlying type 'C' of implicit extension 'R3<T>' must reference all the type parameters declared by the extension, but type parameter 'T' is missing.
            // implicit extension R3<T> for C { } // 1
            Diagnostic(ErrorCode.ERR_UnderspecifiedImplicitExtension, "R3").WithArguments("C", "R3<T>", "T").WithLocation(7, 20)
            );
    }

    [Fact]
    public void UnderlyingType_TypeParametersMustBeUsed_MissingOne()
    {
        var src = """
string s = C<int>.f;

class C<T> { }

implicit extension E<T, U> for C<T>
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,19): error CS0117: 'C<int>' does not contain a definition for 'f'
            // string s = C<int>.f;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "f").WithArguments("C<int>", "f").WithLocation(1, 19),
            // (5,20): error CS9328: The underlying type 'C<T>' of implicit extension 'E<T, U>' must reference all the type parameters declared by the extension, but type parameter 'U' is missing.
            // implicit extension E<T, U> for C<T>
            Diagnostic(ErrorCode.ERR_UnderspecifiedImplicitExtension, "E").WithArguments("C<T>", "E<T, U>", "U").WithLocation(5, 20)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<int>.f");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
    }

    [Fact]
    public void UnderlyingType_TypeParametersMustBeUsed_Container()
    {
        var src = """
#nullable enable

string s1 = C<int>.f; // 1

class C<T> { }

class Container<T, U>
{
    implicit extension E for C<T>
    {
        public static string f = "hi";
    }

    void M()
    {
        string s2 = C<long>.f; // 2
        string s3 = C<T>.f;

        string s4 = C<T?>.f;

        string s5 = C<
#nullable disable
            T
#nullable enable
            >.f;
    }
}
""";
        // PROTOTYPE handle nullability differences on s4 and s5
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,20): error CS0117: 'C<int>' does not contain a definition for 'f'
            // string s1 = C<int>.f; // 1
            Diagnostic(ErrorCode.ERR_NoSuchMember, "f").WithArguments("C<int>", "f").WithLocation(3, 20),
            // (16,29): error CS0117: 'C<long>' does not contain a definition for 'f'
            //         string s2 = C<long>.f; // 2
            Diagnostic(ErrorCode.ERR_NoSuchMember, "f").WithArguments("C<long>", "f").WithLocation(16, 29)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var s1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<int>.f");
        Assert.Null(model.GetSymbolInfo(s1).Symbol);

        var s2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<long>.f");
        Assert.Null(model.GetSymbolInfo(s2).Symbol);

        var s3 = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<T>.f");
        Assert.Equal("System.String Container<T, U>.E.f", model.GetSymbolInfo(s3).Symbol.ToTestDisplayString());

        var s4 = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<T?>.f");
        Assert.Equal("System.String Container<T, U>.E.f", model.GetSymbolInfo(s4).Symbol.ToTestDisplayString());

        var s5 = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Last();
        Assert.Equal("""
            C<
            #nullable disable
                        T
            #nullable enable
                        >.f
            """, s5.ToString());

        Assert.Equal("System.String Container<T, U>.E.f", model.GetSymbolInfo(s5).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void UnderlyingType_TypeParametersMustBeUsed_ContainerContainer()
    {
        var src = """
#nullable enable

class C<T> { }

class ContainerContainer<T>
{
    class Container<U>
    {
        implicit extension E for C<T>
        {
            public static string f = "hi";
        }

        void M()
        {
            string s2 = C<long>.f; // 1
            string s3 = C<T>.f;

            string s4 = C<T?>.f;

            string s5 = C<
#nullable disable
                T
#nullable enable
                >.f;
        }
    }
}
""";
        // PROTOTYPE handle nullability differences on s4 and s5
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (16,33): error CS0117: 'C<long>' does not contain a definition for 'f'
            //             string s2 = C<long>.f; // 1
            Diagnostic(ErrorCode.ERR_NoSuchMember, "f").WithArguments("C<long>", "f").WithLocation(16, 33)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var s2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<long>.f");
        Assert.Null(model.GetSymbolInfo(s2).Symbol);

        var s3 = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<T>.f");
        Assert.Equal("System.String ContainerContainer<T>.Container<U>.E.f", model.GetSymbolInfo(s3).Symbol.ToTestDisplayString());

        var s4 = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<T?>.f");
        Assert.Equal("System.String ContainerContainer<T>.Container<U>.E.f", model.GetSymbolInfo(s4).Symbol.ToTestDisplayString());

        var s5 = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Last();
        Assert.Equal("""
            C<
            #nullable disable
                            T
            #nullable enable
                            >.f
            """, s5.ToString());

        Assert.Equal("System.String ContainerContainer<T>.Container<U>.E.f", model.GetSymbolInfo(s5).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void UnderlyingType_TypeParametersMustBeUsed_MissingTwo()
    {
        var src = """
string s = C.f;

class C { }

implicit extension E<T, U> for C
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,20): error CS9328: The underlying type 'C' of implicit extension 'E<T, U>' must reference all the type parameters declared by the extension, but type parameter 'T' is missing.
            // implicit extension E<T, U> for C
            Diagnostic(ErrorCode.ERR_UnderspecifiedImplicitExtension, "E").WithArguments("C", "E<T, U>", "T").WithLocation(5, 20),
            // (5,20): error CS9328: The underlying type 'C' of implicit extension 'E<T, U>' must reference all the type parameters declared by the extension, but type parameter 'U' is missing.
            // implicit extension E<T, U> for C
            Diagnostic(ErrorCode.ERR_UnderspecifiedImplicitExtension, "E").WithArguments("C", "E<T, U>", "U").WithLocation(5, 20)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.f");
        Assert.Equal("System.String E<T, U>.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void UnderlyingType_MissingTypeSyntax()
    {
        var src = """
explicit extension R for { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,26): error CS1031: Type expected
            // explicit extension R for { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "{").WithLocation(1, 26)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.True(r.ExtendedTypeNoUseSiteDiagnostics.IsErrorType());
    }

    [Theory, CombinatorialData]
    public void UnderlyingType_NativeInt(bool useImageReference)
    {
        var src = """
public explicit extension R for nint { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        Assert.True(comp.SupportsRuntimeCapability(RuntimeCapability.NumericIntPtr));
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);

        var src2 = """
explicit extension R2 for nint : R { }
""";
        var comp2 = CreateCompilation(src2, references: new[] { AsReference(comp, useImageReference) }, targetFramework: TargetFramework.Net70);
        comp2.VerifyDiagnostics();

        var src3 = """
explicit extension R3 for System.IntPtr : R { }
""";
        var comp3 = CreateCompilation(src3, references: new[] { AsReference(comp, useImageReference) }, targetFramework: TargetFramework.Net70);
        comp3.VerifyDiagnostics();

        return;

        static void validate(ModuleSymbol module)
        {
            var r = module.GlobalNamespace.GetTypeMember("R");
            VerifyExtension<TypeSymbol>(r, isExplicit: true);
            Assert.Equal("nint", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);
        }
    }

    [Theory, CombinatorialData]
    public void UnderlyingType_NativeInt_OlderFramework(bool useImageReference)
    {
        var src = """
public explicit extension R for nint { }
""";
        var comp = CreateCompilation(new[] { src, CompilerFeatureRequiredAttribute });
        Assert.False(comp.SupportsRuntimeCapability(RuntimeCapability.NumericIntPtr));
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate);

        var src2 = """
explicit extension R2 for nint : R { }
""";
        var comp2 = CreateCompilation(src2, references: new[] { AsReference(comp, useImageReference) });
        comp2.VerifyDiagnostics();

        var src3 = """
explicit extension R3 for System.IntPtr : R { }
""";
        var comp3 = CreateCompilation(src3, references: new[] { AsReference(comp, useImageReference) });
        comp3.VerifyDiagnostics(
            // (1,20): error CS9316: Extension 'R3' extends 'IntPtr' but base extension 'R' extends 'nint'.
            // explicit extension R3 for System.IntPtr : R { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R3").WithArguments("R3", "System.IntPtr", "R", "nint").WithLocation(1, 20)
            );

        return;

        static void validate(ModuleSymbol module)
        {
            var r = module.GlobalNamespace.GetTypeMember("R");
            VerifyExtension<TypeSymbol>(r, isExplicit: true);
            Assert.Equal("nint", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);
        }
    }

    [Fact]
    public void UnderlyingType_NativeInt_Nested()
    {
        var src = """
public class C<T> { }
public explicit extension R for C<nint> { }
""";
        var comp = CreateCompilation(new[] { src, CompilerFeatureRequiredAttribute }, targetFramework: TargetFramework.Mscorlib45);
        Assert.False(comp.Assembly.RuntimeSupportsNumericIntPtr);
        Assert.False(comp.SupportsRuntimeCapability(RuntimeCapability.NumericIntPtr));
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate);

        var src2 = """
explicit extension R2 for C<nint> : R { }
""";
        var comp2 = CreateCompilation(src2, references: new[] { comp.EmitToImageReference() }, targetFramework: TargetFramework.Mscorlib45);
        comp2.VerifyDiagnostics();

        var src3 = """
explicit extension R3 for C<System.IntPtr> : R { }
""";
        var comp3 = CreateCompilation(src3, references: new[] { comp.EmitToImageReference() }, targetFramework: TargetFramework.Mscorlib45);
        comp3.VerifyDiagnostics(
            // (1,20): error CS9316: Extension 'R3' extends 'C<IntPtr>' but base extension 'R' extends 'C<nint>'.
            // explicit extension R3 for C<System.IntPtr> : R { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R3").WithArguments("R3", "C<System.IntPtr>", "R", "C<nint>").WithLocation(1, 20)
            );

        return;

        static void validate(ModuleSymbol module)
        {
            var r = module.GlobalNamespace.GetTypeMember("R");
            VerifyExtension<TypeSymbol>(r, isExplicit: true);
            Assert.Equal("C<nint>", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);
        }
    }

    [Fact]
    public void UnderlyingType_Dynamic_Nested()
    {
        var src = """
public class C<T> { }
public explicit extension R for C<dynamic> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);

        var src2 = """
explicit extension R2 for C<dynamic> : R { }
""";
        var comp2 = CreateCompilation(src2, references: new[] { comp.EmitToImageReference() }, targetFramework: TargetFramework.Net70);
        comp2.VerifyDiagnostics();

        var src3 = """
explicit extension R3 for C<object> : R { }
""";
        var comp3 = CreateCompilation(src3, references: new[] { comp.EmitToImageReference() }, targetFramework: TargetFramework.Net70);
        comp3.VerifyDiagnostics(
            // (1,20): error CS9316: Extension 'R3' extends 'C<object>' but base extension 'R' extends 'C<dynamic>'.
            // explicit extension R3 for C<object> : R { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R3").WithArguments("R3", "C<object>", "R", "C<dynamic>").WithLocation(1, 20)
            );

        return;

        static void validate(ModuleSymbol module)
        {
            var r = module.GlobalNamespace.GetTypeMember("R");
            VerifyExtension<TypeSymbol>(r, isExplicit: true);
            Assert.Equal("C<dynamic>", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);
        }
    }

    [Fact]
    public void UnderlyingType_NestedTypeWithNullability_Annotated()
    {
        var src = """
#nullable enable
public class C<T> { }
public explicit extension R for C<object?> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);

        var src2 = """
#nullable enable
explicit extension R2 for C<object?> : R { }
""";
        var comp2 = CreateCompilation(src2, references: new[] { comp.EmitToImageReference() }, targetFramework: TargetFramework.Net70);
        comp2.VerifyDiagnostics();

        var src3 = """
#nullable enable
explicit extension R3 for C<object> : R { }
""";
        // PROTOTYPE this should at most be a nullability warning
        var comp3 = CreateCompilation(src3, references: new[] { comp.EmitToImageReference() }, targetFramework: TargetFramework.Net70);
        comp3.VerifyDiagnostics(
            // (2,20): error CS9316: Extension 'R3' extends 'C<object>' but base extension 'R' extends 'C<object?>'.
            // explicit extension R3 for C<object> : R { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R3").WithArguments("R3", "C<object>", "R", "C<object?>").WithLocation(2, 20)
            );

        var src4 = """
explicit extension R3 for C<object> : R { }
""";
        // PROTOTYPE the nullability warning should be silenced here (oblivious context)
        var comp4 = CreateCompilation(src4, references: new[] { comp.EmitToImageReference() }, targetFramework: TargetFramework.Net70);
        comp4.VerifyDiagnostics(
            // (1,20): error CS9316: Extension 'R3' extends 'C<object>' but base extension 'R' extends 'C<object?>'.
            // explicit extension R3 for C<object> : R { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R3").WithArguments("R3", "C<object>", "R", "C<object?>").WithLocation(1, 20)
            );

        return;

        static void validate(ModuleSymbol module)
        {
            var r = module.GlobalNamespace.GetTypeMember("R");
            VerifyExtension<TypeSymbol>(r, isExplicit: true);
            Assert.Equal("C<System.Object?>", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString(includeNonNullable: true));
            Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);
        }
    }

    [Fact]
    public void UnderlyingType_NestedTypeWithNullability_Unannotated()
    {
        var src = """
#nullable enable
public class C<T> { }
public explicit extension R for C<object> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);

        var src2 = """
#nullable enable
explicit extension R2 for C<object> : R { }
""";
        var comp2 = CreateCompilation(src2, references: new[] { comp.EmitToImageReference() }, targetFramework: TargetFramework.Net70);
        comp2.VerifyDiagnostics();

        var src3 = """
#nullable enable
explicit extension R3 for C<object?> : R { }
""";
        // PROTOTYPE this should at most be a nullability warning
        var comp3 = CreateCompilation(src3, references: new[] { comp.EmitToImageReference() }, targetFramework: TargetFramework.Net70);
        comp3.VerifyDiagnostics(
            // (2,20): error CS9316: Extension 'R3' extends 'C<object?>' but base extension 'R' extends 'C<object>'.
            // explicit extension R3 for C<object?> : R { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R3").WithArguments("R3", "C<object?>", "R", "C<object>").WithLocation(2, 20)
            );

        var src4 = """
explicit extension R3 for C<object> : R { }
""";
        // PROTOTYPE the nullability warning should be silenced here (oblivious context)
        var comp4 = CreateCompilation(src4, references: new[] { comp.EmitToImageReference() }, targetFramework: TargetFramework.Net70);
        comp4.VerifyDiagnostics(
            // (1,20): error CS9316: Extension 'R3' extends 'C<object>' but base extension 'R' extends 'C<object>'.
            // explicit extension R3 for C<object> : R { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R3").WithArguments("R3", "C<object>", "R", "C<object>").WithLocation(1, 20)
            );

        return;

        static void validate(ModuleSymbol module)
        {
            var r = module.GlobalNamespace.GetTypeMember("R");
            VerifyExtension<TypeSymbol>(r, isExplicit: true);
            Assert.Equal("C<System.Object!>", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString(includeNonNullable: true));
            Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);
        }
    }

    [Fact]
    public void UnderlyingType_TupleWithElementNames()
    {
        var src = """
public explicit extension R for (int a, int b) { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);

        var comp1 = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp1.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_TupleElementNamesAttribute);
        comp1.VerifyDiagnostics(
            // (1,33): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
            // public explicit extension R for (int a, int b) { }
            Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int a, int b)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(1, 33)
            );

        var src2 = """
explicit extension R2 for (int a, int b)  : R { }
""";
        var comp2 = CreateCompilation(src2, references: new[] { comp.EmitToImageReference() }, targetFramework: TargetFramework.Net70);
        comp2.VerifyDiagnostics();

        var src3 = """
explicit extension R3 for (int, int) : R { }
""";
        // PROTOTYPE consider warning instead, when revisiting rules for variance of underlying types
        var comp3 = CreateCompilation(src3, references: new[] { comp.EmitToImageReference() }, targetFramework: TargetFramework.Net70);
        comp3.VerifyDiagnostics(
            // (1,20): error CS9316: Extension 'R3' extends '(int, int)' but base extension 'R' extends '(int a, int b)'.
            // explicit extension R3 for (int, int) : R { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R3").WithArguments("R3", "(int, int)", "R", "(int a, int b)").WithLocation(1, 20)
            );

        var src4 = """
explicit extension R4 for (int a, int other) : R { }
""";
        var comp4 = CreateCompilation(src4, references: new[] { comp.EmitToImageReference() }, targetFramework: TargetFramework.Net70);
        comp4.VerifyDiagnostics(
            // (1,20): error CS9316: Extension 'R4' extends '(int a, int other)' but base extension 'R' extends '(int a, int b)'.
            // explicit extension R4 for (int a, int other) : R { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R4").WithArguments("R4", "(int a, int other)", "R", "(int a, int b)").WithLocation(1, 20)
            );

        return;

        static void validate(ModuleSymbol module)
        {
            var r = module.GlobalNamespace.GetTypeMember("R");
            VerifyExtension<TypeSymbol>(r, isExplicit: true);
            Assert.Equal("(System.Int32 a, System.Int32 b)", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);
        }
    }

    [Fact]
    public void ForPointer()
    {
        var src = """
unsafe explicit extension R for int*
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,27): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe explicit extension R for int*
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "R").WithLocation(1, 27),
            // (1,33): error CS9305: The extended type may not be dynamic, a pointer, a ref struct, or an extension.
            // unsafe explicit extension R for int*
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "int*").WithLocation(1, 33)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Null(r.ExtendedTypeNoUseSiteDiagnostics);
        Assert.Empty(r.GetMembers());
    }

    [Fact]
    public void ForRefType()
    {
        var src = """
explicit extension R for ref int
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,26): error CS1073: Unexpected token 'ref'
            // explicit extension R for ref int
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(1, 26)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("System.Int32", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void ForPointer_InUnsafeCompilation()
    {
        var src = """
class C
{
    unsafe void M()
    {
        int* i = null;
        i.M2(); // 1
    }
}

unsafe explicit extension R for int* // 2
{
    int* M(int* i) => i;
}

implicit extension R2 for int* // 3, 4
{
    int* M(int* i) => i; // 5, 6, 7
    public void M2() => throw null;
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (6,11): error CS1061: 'int*' does not contain a definition for 'M2' and no accessible extension method 'M2' accepting a first argument of type 'int*' could be found (are you missing a using directive or an assembly reference?)
            //         i.M2(); // 1
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M2").WithArguments("int*", "M2").WithLocation(6, 11),
            // (10,33): error CS9305: The extended type may not be dynamic, a pointer, a ref struct, or an extension.
            // unsafe explicit extension R for int* // 2
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "int*").WithLocation(10, 33),
            // (15,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // implicit extension R2 for int* // 3, 4
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(15, 27),
            // (15,27): error CS9305: The extended type may not be dynamic, a pointer, a ref struct, or an extension.
            // implicit extension R2 for int* // 3, 4
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "int*").WithLocation(15, 27),
            // (17,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     int* M(int* i) => i; // 5, 6, 7
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(17, 5),
            // (17,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     int* M(int* i) => i; // 5, 6, 7
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(17, 12),
            // (17,23): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     int* M(int* i) => i; // 5, 6, 7
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "i").WithLocation(17, 23)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Null(r.ExtendedTypeNoUseSiteDiagnostics);
    }

    [Fact]
    public void ForFunctionPointer()
    {
        var src = """
unsafe explicit extension R for delegate*<void>
{
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,33): error CS9305: The extended type may not be dynamic, a pointer, a ref struct, or an extension.
            // unsafe explicit extension R for delegate*<void>
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "delegate*<void>").WithLocation(1, 33)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Null(r.ExtendedTypeNoUseSiteDiagnostics);
        Assert.Empty(r.GetMembers());
    }

    [Fact]
    public void ForDynamic()
    {
        var src = """
explicit extension R for dynamic
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,26): error CS9305: The extended type may not be dynamic, a pointer, a ref struct, or an extension.
            // explicit extension R for dynamic
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "dynamic").WithLocation(1, 26)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Null(r.ExtendedTypeNoUseSiteDiagnostics);
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
        Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);
    }

    [Fact]
    public void ForNullableReferenceType()
    {
        var src = """
#nullable enable
class C<T> { }

explicit extension R1 for string { }
explicit extension R2 for string? { }
explicit extension R3 for C<string> { }
explicit extension R4 for C<string?> { }

#nullable disable
explicit extension R5 for string { }
explicit extension R6 for C<string> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void ForLessAccessibleType()
    {
        var src = """
internal struct UnderlyingStruct { }
public explicit extension R for UnderlyingStruct
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,27): error CS9309: Inconsistent accessibility: underlying type 'UnderlyingStruct' is less accessible than extension 'R'
            // public explicit extension R for UnderlyingStruct
            Diagnostic(ErrorCode.ERR_BadVisUnderlyingType, "R").WithArguments("R", "UnderlyingStruct").WithLocation(2, 27)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal(Accessibility.Public, r.DeclaredAccessibility);
    }

    [Fact]
    public void ForRefLikeType()
    {
        var src = """
ref struct RS {  }
explicit extension R for RS { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,26): error CS9305: The extended type may not be dynamic, a pointer, a ref struct, or an extension.
            // explicit extension R for RS { }
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "RS").WithLocation(2, 26)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Empty(r.GetMembers());
    }

    [Fact]
    public void Partial_OnePartWithoutUnderlyingType()
    {
        var src = """
class UnderlyingClass { }
partial explicit extension R for UnderlyingClass { }
partial explicit extension R { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void Partial_OtherPartWithoutUnderlyingType()
    {
        var src = """
class UnderlyingClass { }
partial explicit extension R { }
partial explicit extension R for UnderlyingClass { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void Partial_PartsWithTwoDifferentUnderlyingTypes()
    {
        var src = """
class UnderlyingClass { }
class UnderlyingClass2 { }
partial explicit extension R for UnderlyingClass { }
partial explicit extension R for UnderlyingClass2 { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,28): error CS9308: Partial declarations of 'R' must not extend different types.
            // partial explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(3, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void Partial_PartsWithThreeDifferentUnderlyingTypes()
    {
        var src = """
class UnderlyingClass { }
class UnderlyingClass2 { }
class UnderlyingClass3 { }
partial explicit extension R for UnderlyingClass { }
partial explicit extension R for UnderlyingClass2 { }
partial explicit extension R for UnderlyingClass3 { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,28): error CS9308: Partial declarations of 'R' must not extend different types.
            // partial explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(4, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void Partial_PartsWithThreeDifferentUnderlyingTypes_IncludingErrorType()
    {
        var src = """
class UnderlyingClass { }
class UnderlyingClass3 { }
partial explicit extension R for UnderlyingClass { }
partial explicit extension R for ErrorType { }
partial explicit extension R for UnderlyingClass3 { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,28): error CS9308: Partial declarations of 'R' must not extend different types.
            // partial explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(3, 28),
            // (4,34): error CS0246: The type or namespace name 'ErrorType' could not be found (are you missing a using directive or an assembly reference?)
            // partial explicit extension R for ErrorType { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ErrorType").WithArguments("ErrorType").WithLocation(4, 34)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void Partial_PartsWithThreeDifferentUnderlyingTypes_IncludingErrorType_DifferentOrder()
    {
        var src = """
class UnderlyingClass { }
class UnderlyingClass3 { }
partial explicit extension R for ErrorType { }
partial explicit extension R for UnderlyingClass { }
partial explicit extension R for UnderlyingClass3 { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,28): error CS9308: Partial declarations of 'R' must not extend different types.
            // partial explicit extension R for ErrorType { }
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(3, 28),
            // (3,34): error CS0246: The type or namespace name 'ErrorType' could not be found (are you missing a using directive or an assembly reference?)
            // partial explicit extension R for ErrorType { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ErrorType").WithArguments("ErrorType").WithLocation(3, 34)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("ErrorType", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void Partial_PartsWithMatchingUnderlyingTypes()
    {
        var src = """
#nullable enable

class C<T> { }
partial explicit extension R for C<object> { }
partial explicit extension R for C<object> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C<System.Object>", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void Partial_PartsWithDifferentUnderlyingTypes_Dynamic()
    {
        var src = """
class C<T> { }
partial explicit extension R for C<object> { }
partial explicit extension R for C<dynamic> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,28): error CS9308: Partial declarations of 'R' must not extend different types.
            // partial explicit extension R for C<object> { }
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(2, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C<>", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.True(r.ExtendedTypeNoUseSiteDiagnostics.IsErrorType());
    }

    [Fact]
    public void Partial_PartsWithDifferentUnderlyingTypes_TupleNames()
    {
        var src = """
class C<T> { }
partial explicit extension R for C<(int x, int b)> { }
partial explicit extension R for C<(int y, int b)> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,28): error CS9308: Partial declarations of 'R' must not extend different types.
            // partial explicit extension R for C<(int x, int b)> { }
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(2, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C<>", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.True(r.ExtendedTypeNoUseSiteDiagnostics.IsErrorType());
    }

    [Fact]
    public void Partial_PartsWithDifferentUnderlyingTypes_TopLevelNullability()
    {
        var src = """
#nullable enable

class C<T> { }
partial explicit extension R for object { }
partial explicit extension R for object? { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("System.Object", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString(includeNonNullable: true));
        Assert.False(r.ExtendedTypeNoUseSiteDiagnostics.IsErrorType());
    }

    [Fact]
    public void Partial_PartsWithDifferentUnderlyingTypes_NestedNullability()
    {
        var src = """
#nullable enable

class C<T> { }
partial explicit extension R for C<object> { }
partial explicit extension R for C<object?> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,28): error CS9308: Partial declarations of 'R' must not extend different types.
            // partial explicit extension R for C<object> { }
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(4, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C<>", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.True(r.ExtendedTypeNoUseSiteDiagnostics.IsErrorType());
    }

    [Fact]
    public void Partial_PartsWithDifferentUnderlyingTypes_Nullability_OneIsOblivious()
    {
        var src = """
#nullable enable

class C<T> { }
#nullable disable
partial explicit extension R for C<object> { }
#nullable enable
partial explicit extension R for C<object?> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C<System.Object?>", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void Partial_PartsWithDifferentUnderlyingTypes_Nullability_OtherIsOblivious()
    {
        var src = """
#nullable enable

class C<T> { }
partial explicit extension R for C<object?> { }
#nullable disable
partial explicit extension R for C<object> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C<System.Object?>", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void Partial_PartsWithDifferentUnderlyingTypes_Nullability_MultipleObliviousDifferences()
    {
        var src = """
#nullable enable

class C<T1, T2> { }

partial explicit extension R for C<
#nullable disable
    object,
#nullable enable
    object?> { }

partial explicit extension R for C<
#nullable enable
    object?,
#nullable disable
    object
#nullable enable
    > { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,28): error CS9308: Partial declarations of 'R' must not extend different types.
            // partial explicit extension R for C<
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(5, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C<, >", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.True(r.ExtendedTypeNoUseSiteDiagnostics.IsErrorType());
    }

    [Fact]
    public void Partial_OnePartIsErrorType()
    {
        var src = """
class UnderlyingClass { }
partial explicit extension R for Error { }
partial explicit extension R for UnderlyingClass { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,28): error CS9308: Partial declarations of 'R' must not extend different types.
            // partial explicit extension R for Error { }
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(2, 28),
            // (2,34): error CS0246: The type or namespace name 'Error' could not be found (are you missing a using directive or an assembly reference?)
            // partial explicit extension R for Error { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error").WithArguments("Error").WithLocation(2, 34)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("Error", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void Partial_OtherPartIsErrorType()
    {
        var src = """
class UnderlyingClass { }
partial explicit extension R for UnderlyingClass { }
partial explicit extension R for Error { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,34): error CS0246: The type or namespace name 'Error' could not be found (are you missing a using directive or an assembly reference?)
            // partial explicit extension R for Error { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error").WithArguments("Error").WithLocation(3, 34)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Theory, CombinatorialData]
    public void Partial_NoPartHasUnderlyingType(bool isImplicit)
    {
        var keyword = isImplicit ? "implicit" : "explicit";
        var src = $$"""
partial {{keyword}} extension R { }
partial {{keyword}} extension R { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,28): error CS9314: No part of a partial extension 'R' includes an underlying type specification.
            // partial explicit extension R { }
            Diagnostic(ErrorCode.ERR_ExtensionMissingUnderlyingType, "R").WithArguments("R").WithLocation(1, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Null(r.ExtendedTypeNoUseSiteDiagnostics);
    }

    [Fact]
    public void Partial_DifferentBaseExtensions()
    {
        var src = """
class C { }
explicit extension R1 for C { }
explicit extension R2 for C { }

partial explicit extension R3 for C : R1 { }
partial explicit extension R3 for C : R2 { }

partial explicit extension R4 for C : R1, R2 { }
partial explicit extension R4 for C : R2 { }

partial explicit extension R5 for C : R1 { }
partial explicit extension R5 for C : R1, R2 { }

partial explicit extension R6 for C { }
partial explicit extension R6 for C : R1, R2 { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var r3 = comp.GlobalNamespace.GetTypeMember("R3");
        Assert.Equal(new[] { "R1", "R2" }, r3.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.Equal(new[] { "R1", "R2" }, r3.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.True(r3.IsPartial());

        var r4 = comp.GlobalNamespace.GetTypeMember("R4");
        Assert.Equal(new[] { "R1", "R2" }, r4.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.Equal(new[] { "R1", "R2" }, r4.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.True(r4.IsPartial());

        var r5 = comp.GlobalNamespace.GetTypeMember("R5");
        Assert.Equal(new[] { "R1", "R2" }, r5.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.Equal(new[] { "R1", "R2" }, r5.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.True(r5.IsPartial());

        var r6 = comp.GlobalNamespace.GetTypeMember("R6");
        Assert.Equal(new[] { "R1", "R2" }, r6.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.Equal(new[] { "R1", "R2" }, r6.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.True(r6.IsPartial());
    }

    [Theory]
    [InlineData("class    ")]
    [InlineData("struct   ")]
    [InlineData("interface")]
    public void Partial_PartsWithConflictingTypeKinds(string typeKind)
    {
        var src = $$"""
class C { }
partial explicit extension R for C { }
partial {{typeKind}} R { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,19): error CS0261: Partial declarations of 'R' must all be the same kind of type.
            // partial class     R { }
            Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "R").WithArguments("R").WithLocation(3, 19)
            );
    }

    [Fact]
    public void Partial_PartialModifierConflict()
    {
        var src = """
class C { }
public partial explicit extension R1 for C { }
internal partial explicit extension R1 for C { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,35): error CS0262: Partial declarations of 'R1' have conflicting accessibility modifiers
            // public partial explicit extension R1 for C { }
            Diagnostic(ErrorCode.ERR_PartialModifierConflict, "R1").WithArguments("R1").WithLocation(2, 35),
            // (2,35): error CS9309: Inconsistent accessibility: underlying type 'C' is less accessible than extension 'R1'
            // public partial explicit extension R1 for C { }
            Diagnostic(ErrorCode.ERR_BadVisUnderlyingType, "R1").WithArguments("R1", "C").WithLocation(2, 35)
            );
    }

    [Fact]
    public void Partial_OnePartialModifier_OtherDefault()
    {
        var src = """
class C { }
internal partial explicit extension R1 for C { }
partial explicit extension R1 for C { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        Assert.Equal(Accessibility.Internal, comp.GlobalNamespace.GetTypeMember("R1").DeclaredAccessibility);
    }

    [Fact]
    public void Partial_MergeMembers()
    {
        var src = """
class C { }
partial explicit extension R for C
{
    public void M1() { }
}
partial explicit extension R for C
{
    public void M2() { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        AssertEx.Equal(new[] { "void R.M1()", "void R.M2()" }, r.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void Partial_MergeConstraintsNullability()
    {
        var src = """
class C { }

#nullable enable
partial explicit extension R1<T> for C where T : class { }
partial explicit extension R1<T> for C where T : class { }

#nullable enable
partial explicit extension R2<T> for C where T : class { }
#nullable disable
partial explicit extension R2<T> for C where T : class { }

#nullable disable
partial explicit extension R3<T> for C where T : class { }
#nullable enable
partial explicit extension R3<T> for C where T : class { }

#nullable disable
partial explicit extension R4<T> for C where T : class { }
partial explicit extension R4<T> for C where T : class { }

#nullable enable
explicit extension R5 for C : R1<string?>, R2<string?> , R3<string?>, R4<string?> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (22,20): warning CS8634: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'R1<T>'. Nullability of type argument 'string?' doesn't match 'class' constraint.
            // explicit extension R5 for C : R1<string?>, R2<string?> , R3<string?>, R4<string?> { }
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "R5").WithArguments("R1<T>", "T", "string?").WithLocation(22, 20),
            // (22,20): warning CS8634: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'R2<T>'. Nullability of type argument 'string?' doesn't match 'class' constraint.
            // explicit extension R5 for C : R1<string?>, R2<string?> , R3<string?>, R4<string?> { }
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "R5").WithArguments("R2<T>", "T", "string?").WithLocation(22, 20),
            // (22,20): warning CS8634: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'R3<T>'. Nullability of type argument 'string?' doesn't match 'class' constraint.
            // explicit extension R5 for C : R1<string?>, R2<string?> , R3<string?>, R4<string?> { }
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "R5").WithArguments("R3<T>", "T", "string?").WithLocation(22, 20)
            );
    }

    [Fact]
    public void ForErrorType()
    {
        var src = """
explicit extension R for error
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,26): error CS0246: The type or namespace name 'error' could not be found (are you missing a using directive or an assembly reference?)
            // explicit extension R for error
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "error").WithArguments("error").WithLocation(1, 26)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        var underlyingType = r.ExtendedTypeNoUseSiteDiagnostics;
        Assert.Equal("error", underlyingType.ToTestDisplayString());
        Assert.True(underlyingType.IsErrorType());
    }

    [Fact]
    public void ForErrorType_Nested()
    {
        var src = """
class C<T> { }
explicit extension R for C<error>
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,28): error CS0246: The type or namespace name 'error' could not be found (are you missing a using directive or an assembly reference?)
            // explicit extension R for C<error>
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "error").WithArguments("error").WithLocation(2, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        var underlyingType = r.ExtendedTypeNoUseSiteDiagnostics;
        Assert.Equal("C<error>", underlyingType.ToTestDisplayString());
    }

    [Fact]
    public void ForTypeWithUseSiteError()
    {
        var lib1_cs = "public class MissingBase { }";
        var comp1 = CreateCompilation(lib1_cs, assemblyName: "missing", targetFramework: TargetFramework.Net70);
        comp1.VerifyDiagnostics();

        var lib2_cs = "public class UseSiteError : MissingBase { }";
        var comp2 = CreateCompilation(lib2_cs, new[] { comp1.EmitToImageReference() }, targetFramework: TargetFramework.Net70);
        comp2.VerifyDiagnostics();

        var src = """
class C<T> { }
explicit extension R1 for UseSiteError { }
explicit extension R2 for C<UseSiteError> { }
class C1 : UseSiteError { }
class C2 : C<UseSiteError> { }
""";
        var comp = CreateCompilation(src, new[] { comp2.EmitToImageReference() }, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2, 27): error CS0012: The type 'MissingBase' is defined in an assembly that is not referenced.You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // explicit extension R1 for UseSiteError { }
            Diagnostic(ErrorCode.ERR_NoTypeDef, "UseSiteError").WithArguments("MissingBase", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 27),
            // (4,12): error CS0012: The type 'MissingBase' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // class C1 : UseSiteError { }
            Diagnostic(ErrorCode.ERR_NoTypeDef, "UseSiteError").WithArguments("MissingBase", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(4, 12)
            );
    }

    [Fact]
    public void TypeDepends_SelfReference()
    {
        var src = """
explicit extension R for R { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,26): error CS9305: The extended type may not be dynamic, a pointer, a ref struct, or an extension.
            // explicit extension R for R { }
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "R").WithLocation(1, 26)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Null(r.ExtendedTypeNoUseSiteDiagnostics);
    }

    [Fact]
    public void TypeDepends_SelfReference_AsContainingType()
    {
        var src = """
public explicit extension One<T> for object : One<int>.Two
{
    public explicit extension Two for object { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,27): error CS9311: Base extension 'One<int>.Two' causes a cycle in the extension hierarchy of 'One<T>'.
            // public explicit extension One<T> for object : One<int>.Two
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "One").WithArguments("One<T>", "One<int>.Two").WithLocation(1, 27)
            );

        var one = comp.GlobalNamespace.GetTypeMember("One");
        var two = one.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("One<System.Int32>.Two", two.ToTestDisplayString());
        Assert.True(two.IsErrorType());
    }

    [Fact]
    public void TypeDepends_SelfReference_WithArray()
    {
        var src = """
explicit extension R for R[] { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R[]", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void TypeDepends_SelfReference_WithTuple()
    {
        var src = """
explicit extension R for (R, R) { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("(R, R)", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void TypeDepends_SelfReference_WithStruct()
    {
        var src = """
struct S<T> { }
explicit extension R for S<R> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("S<R>", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void TypeDepends_SelfReference_WithStruct_WithField()
    {
        var src = """
public struct S<T>
{
    public T field;
}

explicit extension R for S<R> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("S<R>", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal("R", r.ExtendedTypeNoUseSiteDiagnostics.GetMember("field").GetTypeOrReturnType().ToTestDisplayString());
    }

    [Fact]
    public void TypeDepends_CircularityViaTypeArgument()
    {
        var src = """
class C<T> { }
explicit extension R for C<R> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C<R>", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void TypeDepends_CircularityViaBaseExtensions()
    {
        var src = """
struct S { }
explicit extension X for S : Y { }
explicit extension Y for S : Z { }
explicit extension Z for S : X { }
""";

        var comp = CreateCompilation(new[] { src, CompilerFeatureRequiredAttribute }, targetFramework: TargetFramework.Mscorlib40);
        comp.VerifyDiagnostics(
            // (2,20): error CS9311: Base extension 'Y' causes a cycle in the extension hierarchy of 'X'.
            // explicit extension X for S : Y { }
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "X").WithArguments("X", "Y").WithLocation(2, 20),
            // (3,20): error CS9311: Base extension 'Z' causes a cycle in the extension hierarchy of 'Y'.
            // explicit extension Y for S : Z { }
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "Y").WithArguments("Y", "Z").WithLocation(3, 20),
            // (4,20): error CS9311: Base extension 'X' causes a cycle in the extension hierarchy of 'Z'.
            // explicit extension Z for S : X { }
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "Z").WithArguments("Z", "X").WithLocation(4, 20)
            );

        var x = comp.GlobalNamespace.GetTypeMember("X");
        verifyBase(x, "Y");

        var y = comp.GlobalNamespace.GetTypeMember("Y");
        verifyBase(y, "Z");

        var z = comp.GlobalNamespace.GetTypeMember("Z");
        verifyBase(z, "X");

        static void verifyBase(NamedTypeSymbol type, string expectedBaseName)
        {
            var baseExtension = type.BaseExtensionsNoUseSiteDiagnostics.Single();
            Assert.Equal(expectedBaseName, baseExtension.ToTestDisplayString());
            Assert.True(baseExtension.IsErrorType());
            Assert.Same(baseExtension, type.AllBaseExtensionsNoUseSiteDiagnostics.Single());
        }
    }

    [Fact]
    public void TypeDepends_CircularityViaBaseExtensions_Metadata()
    {
        var src1 = """
public struct S { }
public explicit extension X for S { }
""";

        var comp1 = CreateCompilation(src1, assemblyName: "first",
            targetFramework: TargetFramework.Net70);
        comp1.VerifyDiagnostics();

        var src2 = """
public explicit extension Y for S : X { }
""";

        var comp2 = CreateCompilation(src2, references: new[] { comp1.EmitToImageReference() },
            assemblyName: "second", targetFramework: TargetFramework.Net70);
        comp2.VerifyDiagnostics();

        var src1Updated = """
public struct S { }
public explicit extension X for S : Y { }
""";

        comp1 = CreateCompilation(src1Updated, references: new[] { comp2.EmitToImageReference() },
            assemblyName: "first", targetFramework: TargetFramework.Net70);
        comp1.VerifyDiagnostics(
            // (2,27): error CS9311: Base extension 'Y' causes a cycle in the extension hierarchy of 'X'.
            // public explicit extension X for S : Y { }
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "X").WithArguments("X", "Y").WithLocation(2, 27)
            );

        var x = comp1.GlobalNamespace.GetTypeMember("X");
        var xBaseExtension = x.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("Y", xBaseExtension.ToTestDisplayString());
        Assert.True(xBaseExtension.IsErrorType());

        var y = comp1.GlobalNamespace.GetTypeMember("Y");
        var yBaseExtension = y.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("X", yBaseExtension.ToTestDisplayString());
        Assert.True(yBaseExtension.IsErrorType());

        var retargetingComp = CreateCompilation(src1Updated, references: new[] { comp2.ToMetadataReference() },
            assemblyName: "first", targetFramework: TargetFramework.Net70);
        retargetingComp.VerifyDiagnostics(
            // (2,27): error CS9311: Base extension 'Y' causes a cycle in the extension hierarchy of 'X'.
            // public explicit extension X for S : Y { }
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "X").WithArguments("X", "Y").WithLocation(2, 27)
            );

        x = retargetingComp.GlobalNamespace.GetTypeMember("X");
        VerifyExtension<SourceExtensionTypeSymbol>(x, isExplicit: true);
        xBaseExtension = x.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("Y", xBaseExtension.ToTestDisplayString());
        Assert.True(xBaseExtension.IsErrorType());

        y = retargetingComp.GlobalNamespace.GetTypeMember("Y");
        VerifyExtension<RetargetingNamedTypeSymbol>(y, isExplicit: true);
        yBaseExtension = y.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("X", yBaseExtension.ToTestDisplayString());
        Assert.True(yBaseExtension.IsErrorType());
    }

    [Fact]
    public void TypeDepends_CircularityViaUnderlyingType()
    {
        var src = """
explicit extension R for R2.Nested { }
explicit extension R2 for object : R
{
    public class Nested { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,20): error CS0146: Circular base type dependency involving 'R2.Nested' and 'R'
            // explicit extension R for R2.Nested { }
            Diagnostic(ErrorCode.ERR_CircularBase, "R").WithArguments("R2.Nested", "R").WithLocation(1, 20),
            // (2,20): error CS9311: Base extension 'R' causes a cycle in the extension hierarchy of 'R2'.
            // explicit extension R2 for object : R
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "R2").WithArguments("R2", "R").WithLocation(2, 20)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.True(r.IsExtension);
        Assert.Equal("R2.Nested", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
        Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        Assert.True(r2.IsExtension);
        Assert.Equal("System.Object", r2.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(new[] { "R" }, r2.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.True(r2.BaseExtensionsNoUseSiteDiagnostics.Single().IsErrorType());
        Assert.Equal(new[] { "R" }, r2.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.True(r2.AllBaseExtensionsNoUseSiteDiagnostics.Single().IsErrorType());
    }

    [Fact]
    public void TypeDepends_CircularityViaUnderlyingType_Metadata()
    {
        var src1 = """
public struct R1 { }
""";
        var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net70, assemblyName: "first");
        comp1.VerifyDiagnostics();

        var src2 = """
public explicit extension R2 for R1 { }
""";
        var comp2 = CreateCompilation(src2, references: new[] { comp1.EmitToImageReference() },
            targetFramework: TargetFramework.Net70, assemblyName: "second");
        comp2.VerifyDiagnostics();

        var src3 = """
public struct R2 { }
""";
        var comp3 = CreateCompilation(src3, targetFramework: TargetFramework.Net70, assemblyName: "second");
        comp1.VerifyDiagnostics();

        var src4 = """
public explicit extension R1 for R2 { }
""";
        var comp4 = CreateCompilation(src4, references: new[] { comp3.EmitToImageReference() },
            targetFramework: TargetFramework.Net70, assemblyName: "first");
        comp4.VerifyDiagnostics();

        var comp5 = CreateCompilation("", references: new[] { comp2.EmitToImageReference(), comp4.EmitToImageReference() },
            targetFramework: TargetFramework.Net70);
        comp5.VerifyDiagnostics();

        var r1 = comp5.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: true);
        var r2FromR1 = (ExtendedErrorTypeSymbol)r1.ExtendedTypeNoUseSiteDiagnostics;
        Assert.Equal("R2", r2FromR1.ToTestDisplayString());
        AssertEx.Equal("error CS9322: Extension marker method on type 'R1' is malformed.", r2FromR1.ErrorInfo.ToString());

        var src6 = """
public explicit extension R6 for object : R1 { }
public explicit extension R7 for object : R2 { }
public explicit extension R8 for R1 { }
public explicit extension R9 for R2 { }
""";
        var comp6 = CreateCompilation(src6, references: new[] { comp2.ToMetadataReference(), comp4.ToMetadataReference() },
            targetFramework: TargetFramework.Net70);
        // PROTOTYPE we should report use-site errors for the uses of R1 and R2
        comp6.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R6' extends 'object' but base extension 'R1' extends 'R2'.
            // public explicit extension R6 for object : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R6").WithArguments("R6", "object", "R1", "R2").WithLocation(1, 27),
            // (2,27): error CS9316: Extension 'R7' extends 'object' but base extension 'R2' extends 'R1'.
            // public explicit extension R7 for object : R2 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R7").WithArguments("R7", "object", "R2", "R1").WithLocation(2, 27),
            // (3,34): error CS9305: The extended type may not be dynamic, a pointer, a ref struct, or an extension.
            // public explicit extension R8 for R1 { }
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "R1").WithLocation(3, 34),
            // (4,34): error CS9305: The extended type may not be dynamic, a pointer, a ref struct, or an extension.
            // public explicit extension R9 for R2 { }
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "R2").WithLocation(4, 34)
            );

        r1 = comp6.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<RetargetingNamedTypeSymbol>(r1, isExplicit: true);
        r2FromR1 = (ExtendedErrorTypeSymbol)r1.ExtendedTypeNoUseSiteDiagnostics;
        Assert.True(r2FromR1.IsErrorType());
        AssertEx.Equal("error CS8090: There is an error in a referenced assembly 'second, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.",
            r2FromR1.ErrorInfo.ToString());
    }

    [Fact]
    public void TypeDepends_CircularityViaUnderlyingType_WithArray()
    {
        var src = """
explicit extension R for R2.Nested[] { }
explicit extension R2 for R2.Nested[] : R
{
    public class Nested { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.True(r.IsExtension);
        Assert.Equal("R2.Nested[]", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
        Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        Assert.True(r2.IsExtension);
        Assert.Equal("R2.Nested[]", r2.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(new[] { "R" }, r2.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.Equal(new[] { "R" }, r2.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
    }

    [Fact]
    public void TypeDepends_CircularityViaUnderlyingType_WithTuple()
    {
        var src = """
explicit extension R for (R2.Nested, int) { }
explicit extension R2 for (R2.Nested, int) : R
{
    public class Nested { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("(R2.Nested, System.Int32)", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
        Assert.Empty(r.AllBaseExtensionsNoUseSiteDiagnostics);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        Assert.Equal("(R2.Nested, System.Int32)", r2.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(new[] { "R" }, r2.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.Equal(new[] { "R" }, r2.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
    }

    [Fact]
    public void TypeDepends_CircularityViaUnderlyingTypeAndBaseExtensions()
    {
        var src = """
explicit extension E1 for object : E2 { }
explicit extension E2 for E2 : E2 { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,20): error CS9311: Base extension 'E2' causes a cycle in the extension hierarchy of 'E2'.
            // explicit extension E2 for E2 : E2 { }
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "E2").WithArguments("E2", "E2").WithLocation(2, 20),
            // (2,27): error CS9305: The extended type may not be dynamic, a pointer, a ref struct, or an extension.
            // explicit extension E2 for E2 : E2 { }
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "E2").WithLocation(2, 27)
            );
        var e2 = comp.GlobalNamespace.GetTypeMember("E2");
        Assert.True(e2.IsExtension);
        Assert.Null(e2.ExtendedTypeNoUseSiteDiagnostics);
        Assert.Equal("E2", e2.BaseExtensionsNoUseSiteDiagnostics.Single().ToTestDisplayString());
    }

    [Fact]
    public void TypeDepends_CircularityAttemptWithUnderylingType()
    {
        var src1 = """
public explicit extension R1 for object
{
    public class Nested1 { }
}
""";
        var comp1 = CreateCompilation(src1, assemblyName: "first", targetFramework: TargetFramework.Net70);
        comp1.VerifyDiagnostics();

        var src2 = """
public explicit extension R2 for R1.Nested1
{
    public class Nested2 { }
}
""";
        var comp2 = CreateCompilation(src2, references: new[] { comp1.EmitToImageReference() },
            assemblyName: "second", targetFramework: TargetFramework.Net70);
        comp2.VerifyDiagnostics();

        var src1_updated = """
public explicit extension R1 for R2.Nested2
{
    public class Nested1 { }
}
""";
        comp1 = CreateCompilation(src1_updated, references: new[] { comp2.EmitToImageReference() },
            assemblyName: "first", targetFramework: TargetFramework.Net70);
        comp1.VerifyDiagnostics();

        var r1 = comp1.GlobalNamespace.GetTypeMember("R1");
        Assert.True(r1.IsExtension);
        Assert.Equal("R2.Nested2", r1.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void TypeDepends_CircularityAttemptWithUnderylingType2()
    {
        var src1 = """
public explicit extension R1 for object
{
    public class Nested1<T> { }
}
""";
        var comp1 = CreateCompilation(src1, assemblyName: "first", targetFramework: TargetFramework.Net70);
        comp1.VerifyDiagnostics();

        var src2 = """
public explicit extension R2 for R1.Nested1<R1>
{
    public class Nested2<T> { }
}
""";
        var comp2 = CreateCompilation(src2, references: new[] { comp1.EmitToImageReference() },
            assemblyName: "second", targetFramework: TargetFramework.Net70);
        comp2.VerifyDiagnostics();

        var src1_updated = """
public explicit extension R1 for R2.Nested2<R2>
{
    public class Nested1<T> { }
}
""";
        comp1 = CreateCompilation(src1_updated, references: new[] { comp2.EmitToImageReference() },
            assemblyName: "first", targetFramework: TargetFramework.Net70);
        comp1.VerifyDiagnostics();
    }

    [Fact]
    public void TypeDepends_CircularityAttemptWithUnderylingType3()
    {
        var ilSource = """
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '<ExplicitExtension>$'(valuetype R2 '') cil managed
    {
        IL_0000: ret
    }
}

.class public sequential ansi sealed beforefieldinit R2
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '<ExplicitExtension>$'(valuetype R1 '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R3 for object : R1 { }
""";

        // PROTOTYPE this test should be updated once we emit erase references to extensions (different metadata format)
        // PROTOTYPE expecting some use-site diagnostics (bad metadata, as underlying type cannot be an extension)
        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R3' extends 'object' but base extension 'R1' extends 'R2'.
            // public explicit extension R3 for object : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R3").WithArguments("R3", "object", "R1", "R2").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        var r1ExtendedType = r1.ExtendedTypeNoUseSiteDiagnostics;
        Assert.Equal("R2", r1ExtendedType.ToTestDisplayString());
        Assert.True(r1ExtendedType.IsErrorType());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        var r2ExtendedType = r2.ExtendedTypeNoUseSiteDiagnostics;
        Assert.Equal("R1", r2ExtendedType.ToTestDisplayString());
        Assert.True(r2ExtendedType.IsErrorType());
    }

    [Fact]
    public void TypeDepends_CircularityWithBaseExtension()
    {
        var ilSource = """
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '<ExplicitExtension>$'(object '', valuetype R2 '') cil managed
    {
        IL_0000: ret
    }
}

.class public sequential ansi sealed beforefieldinit R2
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '<ExplicitExtension>$'(object o, valuetype R1 '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R3 for object : R1 { }
public explicit extension R4 for object : R2 { }
""";

        // PROTOTYPE this test should be updated once we emit erase references to extensions (different metadata format)
        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,27): error CS0268: Imported type 'R2' is invalid. It contains a circular base type dependency.
            // public explicit extension R3 for object : R1 { }
            Diagnostic(ErrorCode.ERR_ImportedCircularBase, "R3").WithArguments("R2").WithLocation(1, 27),
            // (2,27): error CS0268: Imported type 'R1' is invalid. It contains a circular base type dependency.
            // public explicit extension R4 for object : R2 { }
            Diagnostic(ErrorCode.ERR_ImportedCircularBase, "R4").WithArguments("R1").WithLocation(2, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        var r1BaseExtension = r1.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("R2", r1BaseExtension.ToTestDisplayString());
        Assert.True(r1BaseExtension.IsErrorType());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        var r2BaseExtension = r2.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("R1", r2BaseExtension.ToTestDisplayString());
        Assert.True(r2BaseExtension.IsErrorType());
    }

    [Fact]
    public void ImplicitVsExplicit()
    {
        var src = """
struct S { }
explicit extension X for S { }
implicit extension X for S { }
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,20): error CS9315: Partial declarations of 'X' must specify the same extension modifier ('implicit' or 'explicit').
            // explicit extension X for S { }
            Diagnostic(ErrorCode.ERR_PartialDifferentExtensionModifiers, "X").WithArguments("X").WithLocation(2, 20),
            // (3,20): error CS0101: The namespace '<global namespace>' already contains a definition for 'X'
            // implicit extension X for S { }
            Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "X").WithArguments("X", "<global namespace>").WithLocation(3, 20)
            );
    }

    [Fact]
    public void ImplicitVsExplicit_PartialExplicitAndImplicit()
    {
        var src = """
struct S { }
partial explicit extension X for S { }
partial implicit extension X for S { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,28): error CS9315: Partial declarations of 'X' must specify the same extension modifier ('implicit' or 'explicit').
            // partial explicit extension X for S { }
            Diagnostic(ErrorCode.ERR_PartialDifferentExtensionModifiers, "X").WithArguments("X").WithLocation(2, 28)
            );
        // PROTOTYPE add and verify an IsExplicit API on the symbol
    }

    [Fact]
    public void ImplicitVsExplicit_PartialImplicitAndExplicit()
    {
        var src = """
struct S { }
partial implicit extension X for S { }
partial explicit extension X for S { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,28): error CS9315: Partial declarations of 'X' must specify the same extension modifier ('implicit' or 'explicit').
            // partial implicit extension X for S { }
            Diagnostic(ErrorCode.ERR_PartialDifferentExtensionModifiers, "X").WithArguments("X").WithLocation(2, 28)
            );
        // PROTOTYPE add and verify an IsExplicit API on the symbol
    }

    [Fact]
    public void ImplicitVsExplicit_PartialImplicitAndExplicitAndExplicit()
    {
        var src = """
struct S { }
partial implicit extension X for S { }
partial explicit extension X for S { }
partial explicit extension X for S { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,28): error CS9315: Partial declarations of 'X' must specify the same extension modifier ('implicit' or 'explicit').
            // partial implicit extension X for S { }
            Diagnostic(ErrorCode.ERR_PartialDifferentExtensionModifiers, "X").WithArguments("X").WithLocation(2, 28),
            // (2,28): error CS9315: Partial declarations of 'X' must specify the same extension modifier ('implicit' or 'explicit').
            // partial implicit extension X for S { }
            Diagnostic(ErrorCode.ERR_PartialDifferentExtensionModifiers, "X").WithArguments("X").WithLocation(2, 28)
            );
        // PROTOTYPE add and verify an IsExplicit API on the symbol
    }

    [Theory, CombinatorialData, WorkItem(67050, "https://github.com/dotnet/roslyn/issues/67050")]
    public void ImplicitVsExplicit_PartialAndMissingImplicitOrExplicit(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var src = $$"""
struct S { }
partial {{keyword}} extension X for S { }
partial extension X for S { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,39): error CS1031: Type expected
            // partial explicit extension X for S { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(2, 39),
            // (2,39): error CS1525: Invalid expression term 'partial'
            // partial explicit extension X for S { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("partial").WithLocation(2, 39),
            // (2,39): error CS1003: Syntax error, ',' expected
            // partial explicit extension X for S { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(2, 39),
            // (3,1): error CS8803: Top-level statements must precede namespace and type declarations.
            // partial extension X for S { }
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "p").WithLocation(3, 1),
            // (3,29): error CS1002: ; expected
            // partial extension X for S { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(3, 29),
            // (3,29): error CS1022: Type or namespace definition, or end-of-file expected
            // partial extension X for S { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(3, 29)
            );
    }

    [Fact]
    public void ExtensionAsBase_ForClass()
    {
        var src = """
class C1 { }
explicit extension R for C1 { }
class C2 : R { } // 1
class C3 : C1, R { } // 2
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,12): error CS0527: Type 'R' in interface list is not an interface
            // class C2 : R { } // 1
            Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "R").WithArguments("R").WithLocation(3, 12),
            // (4,16): error CS0527: Type 'R' in interface list is not an interface
            // class C3 : C1, R { } // 2
            Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "R").WithArguments("R").WithLocation(4, 16)
            );
    }

    [Fact]
    public void ExtensionAsBase_ForStruct()
    {
        var src = """
struct S1 { }
explicit extension R for S1 { }
struct S2 : R { } // 1
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,13): error CS0527: Type 'R' in interface list is not an interface
            // struct S2 : R { } // 1
            Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "R").WithArguments("R").WithLocation(3, 13)
            );
    }

    [Fact]
    public void ExtensionAsBase_ForInterface()
    {
        var src = """
interface I1 { }
explicit extension R for I1 { }
interface I2 : R { } // 1
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,16): error CS0527: Type 'R' in interface list is not an interface
            // interface I2 : R { } // 1
            Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "R").WithArguments("R").WithLocation(3, 16)
            );
    }

    [Fact]
    public void ExtensionAsBase_ForEnum()
    {
        var src = """
enum E1 { }
explicit extension R for E1 { }
enum E2 : R { } // 1
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,11): error CS1008: Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
            // enum E2 : R { } // 1
            Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "R").WithLocation(3, 11)
            );
    }

    [Fact]
    public void ExtensionAsBase_ForRecordClass()
    {
        var src = """
record R1(int i) { }
explicit extension Extension for R1 { }
record R2(int j) : Extension { } // 1
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,20): error CS0527: Type 'Extension' in interface list is not an interface
            // record R2(int j) : Extension { } // 1
            Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "Extension").WithArguments("Extension").WithLocation(3, 20)
            );
    }

    [Fact]
    public void ExtensionAsBase_ForRecordStruct()
    {
        var src = """
record struct R1(int i) { }
explicit extension Extension for R1 { }
record struct R2(int j) : Extension { } // 1
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,27): error CS0527: Type 'Extension' in interface list is not an interface
            // record struct R2(int j) : Extension { } // 1
            Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "Extension").WithArguments("Extension").WithLocation(3, 27)
            );
    }

    [Fact]
    public void BaseExtension()
    {
        var src = """
class C { }
explicit extension R1 for C { }
explicit extension R2 for C { }
explicit extension R3 for C : R1, R2 { }

partial explicit extension R4 for C : R1 { }
partial explicit extension R4 for C : R2 { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);

        return;

        static void validate(ModuleSymbol module)
        {
            var r3 = module.GlobalNamespace.GetTypeMember("R3");
            Assert.Equal("C", r3.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Equal(new[] { "R1", "R2" }, r3.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
            Assert.Equal(new[] { "R1", "R2" }, r3.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());

            var r4 = module.GlobalNamespace.GetTypeMember("R4");
            Assert.Equal("C", r4.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Equal(new[] { "R1", "R2" }, r4.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
            Assert.Equal(new[] { "R1", "R2" }, r4.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        }
    }

    [Fact]
    public void BaseExtension_Generic()
    {
        var src = """
class C<T1, T2> { }
class D<U>
{
    explicit extension R1<T1, T2> for C<T1, T2> { }
    explicit extension R2<T1, T2> for C<T1, T2> { }
    explicit extension R3<V> for C<U, V> : R1<U, V>, R2<U, V> { }

    partial explicit extension R4<V> for C<U, V> : R1<U, V> { }
    partial explicit extension R4<V> for C<U, V> : R2<U, V> { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        static void validate(ModuleSymbol module)
        {
            var d = module.GlobalNamespace.GetTypeMember("D");
            var r1 = d.GetTypeMember("R1");
            Assert.Equal(2, r1.Arity);

            var r3 = d.GetTypeMember("R3");
            Assert.Equal(1, r3.Arity);
            Assert.Equal("C<U, V>", r3.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Equal(new[] { "D<U>.R1<U, V>", "D<U>.R2<U, V>" }, r3.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
            Assert.Equal(new[] { "D<U>.R1<U, V>", "D<U>.R2<U, V>" }, r3.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());

            var r4 = d.GetTypeMember("R4");
            Assert.Equal(1, r4.Arity);
            Assert.Equal("C<U, V>", r4.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Equal(new[] { "D<U>.R1<U, V>", "D<U>.R2<U, V>" }, r4.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
            Assert.Equal(new[] { "D<U>.R1<U, V>", "D<U>.R2<U, V>" }, r4.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());

            var r4FirstBase = r4.BaseExtensionsNoUseSiteDiagnostics.First();
            Assert.Equal(2, r4FirstBase.TypeArguments().Length);
            Assert.Same(d.TypeArguments().Single(), r4FirstBase.TypeArguments()[0]);
            Assert.Same(r4.TypeArguments().Single(), r4FirstBase.TypeArguments()[1]);
        }
    }

    [Fact]
    public void BaseExtensions_ErrorType()
    {
        var src = """
class C { }
explicit extension R for C : error
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,30): error CS0246: The type or namespace name 'error' could not be found (are you missing a using directive or an assembly reference?)
            // explicit extension R for C : error
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "error").WithArguments("error").WithLocation(2, 30)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        var baseExtensions = r.BaseExtensionsNoUseSiteDiagnostics;
        Assert.Equal(new[] { "error" }, baseExtensions.ToTestDisplayStrings());
        Assert.True(baseExtensions.Single().IsErrorType());
    }

    [Fact]
    public void BaseExtensions_ErrorType_Nested()
    {
        var src = """
class C { }
explicit extension R1<T> for C { }
explicit extension R2 for C : R1<error>
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,34): error CS0246: The type or namespace name 'error' could not be found (are you missing a using directive or an assembly reference?)
            // explicit extension R2 for C : R1<error>
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "error").WithArguments("error").WithLocation(3, 34)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R2");
        var baseExtensions = r.BaseExtensionsNoUseSiteDiagnostics;
        Assert.Equal(new[] { "R1<error>" }, baseExtensions.ToTestDisplayStrings());
    }

    [Theory]
    [InlineData("internal", "public")]
    [InlineData("protected", "public")]
    [InlineData("private protected", "public")]
    [InlineData("internal protected", "public")]
    [InlineData("private protected", "protected")]
    [InlineData("private", "public")]
    [InlineData("internal", "protected")]
    [InlineData("private protected", "internal")]
    [InlineData("internal", "internal protected")]
    [InlineData("private", "internal")]
    [InlineData("protected", "internal")]
    public void BaseExtensions_LessAccessibleBaseExtension(string baseAccessibility, string thisAccessibility)
    {
        var src = $$"""
public struct UnderlyingStruct { }
public class C
{
    {{baseAccessibility}} explicit extension R1 for UnderlyingStruct { }
    {{thisAccessibility}} explicit extension R2 for UnderlyingStruct : R1 { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,43): error CS9310: Inconsistent accessibility: base extension 'C.R1' is less accessible than extension 'C.R2'
            //     internal protected explicit extension R2 for UnderlyingStruct : R1 { }
            Diagnostic(ErrorCode.ERR_BadVisBaseExtension, "R2").WithArguments("C.R2", "C.R1")
            );
    }

    [Theory]
    [InlineData("public", "public")]
    [InlineData("public", "internal")]
    [InlineData("public", "protected")]
    [InlineData("public", "private protected")]
    [InlineData("public", "internal protected")]
    [InlineData("public", "private")]
    [InlineData("internal", "internal")]
    [InlineData("internal", "private protected")]
    [InlineData("internal protected", "internal")]
    [InlineData("protected", "protected")]
    [InlineData("protected", "private protected")]
    [InlineData("private", "private")]
    public void BaseExtensions_AtLeastAsAccessibleBaseExtension(string baseAccessibility, string thisAccessibility)
    {
        var src = $$"""
public class C
{
    public struct UnderlyingStruct { }
    {{baseAccessibility}} explicit extension R1 for UnderlyingStruct { }
    {{thisAccessibility}} explicit extension R2 for UnderlyingStruct : R1 { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void BaseExtension_FileType_NonFileExtension()
    {
        var src = """
class UnderlyingClass { }
file explicit extension R1 for UnderlyingClass { }
explicit extension R for UnderlyingClass : R1 { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,20): error CS9053: File-local type 'R1' cannot be used as a base type of non-file-local type 'R'.
            // explicit extension R for UnderlyingClass : R1 { }
            Diagnostic(ErrorCode.ERR_FileTypeBase, "R").WithArguments("R1", "R").WithLocation(3, 20)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(new[] { "R1@<tree 0>" }, r.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.Equal(new[] { "R1@<tree 0>" }, r.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
    }

    [Fact]
    public void BaseExtension_FileType_NonFileExtension_SecondPosition()
    {
        var src = """
class UnderlyingClass { }
explicit extension R1 for UnderlyingClass { }
file explicit extension R2 for UnderlyingClass { }
explicit extension R for UnderlyingClass : R1, R2 { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,20): error CS9053: File-local type 'R2' cannot be used as a base type of non-file-local type 'R'.
            // explicit extension R for UnderlyingClass : R1, R2 { }
            Diagnostic(ErrorCode.ERR_FileTypeBase, "R").WithArguments("R2", "R").WithLocation(4, 20)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(new[] { "R1", "R2@<tree 0>" }, r.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
    }

    [Fact]
    public void BaseExtension_FileType_NonFileExtension_Both()
    {
        var src = """
class UnderlyingClass { }
file explicit extension R1 for UnderlyingClass { }
file explicit extension R2 for UnderlyingClass { }
explicit extension R for UnderlyingClass : R1, R2 { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,20): error CS9053: File-local type 'R1' cannot be used as a base type of non-file-local type 'R'.
            // explicit extension R for UnderlyingClass : R1, R2 { }
            Diagnostic(ErrorCode.ERR_FileTypeBase, "R").WithArguments("R1", "R").WithLocation(4, 20),
            // (4,20): error CS9053: File-local type 'R2' cannot be used as a base type of non-file-local type 'R'.
            // explicit extension R for UnderlyingClass : R1, R2 { }
            Diagnostic(ErrorCode.ERR_FileTypeBase, "R").WithArguments("R2", "R").WithLocation(4, 20)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(new[] { "R1@<tree 0>", "R2@<tree 0>" }, r.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
    }

    [Theory, CombinatorialData]
    public void BaseExtension_ImplicitVsExplicit(bool baseIsExplicit, bool thisIsExplicit)
    {
        var src = $$"""
class C { }
{{(baseIsExplicit ? "explicit" : "implicit")}} extension R1 for C { }
{{(thisIsExplicit ? "explicit" : "implicit")}} extension R for C : R1 { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void BaseExtension_MiscTypes()
    {
        var src = """
class C { }
interface I { }
struct S { }
enum E { }
explicit extension R1 for C : I { } // 1
explicit extension R2 for C : C { } // 2
explicit extension R3 for C : S { } // 3
explicit extension R4 for C : E { } // 4

#nullable enable
explicit extension R5 for C { }
explicit extension R6 for C : R5? { } // 5

explicit extension R7 for S { }
explicit extension R8 for S : R7? { } // PROTOTYPE

unsafe explicit extension R9 for C : C* { } // 6
""";
        // PROTOTYPE need to revisit binding of annotated types to account for extension types
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,31): error CS9307: A base extension must be an extension type.
            // explicit extension R1 for C : I { } // 1
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "I").WithLocation(5, 31),
            // (6,31): error CS9307: A base extension must be an extension type.
            // explicit extension R2 for C : C { } // 2
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "C").WithLocation(6, 31),
            // (7,31): error CS9307: A base extension must be an extension type.
            // explicit extension R3 for C : S { } // 3
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "S").WithLocation(7, 31),
            // (8,31): error CS9307: A base extension must be an extension type.
            // explicit extension R4 for C : E { } // 4
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "E").WithLocation(8, 31),
            // (12,31): error CS9307: A base extension must be an extension type.
            // explicit extension R6 for C : R5? { } // 5
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "R5?").WithLocation(12, 31),
            // (15,31): error CS9307: A base extension must be an extension type.
            // explicit extension R8 for S : R7? { } // PROTOTYPE
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "R7?").WithLocation(15, 31),
            // (17,38): error CS9307: A base extension must be an extension type.
            // unsafe explicit extension R9 for C : C* { } // 6
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "C*").WithLocation(17, 38)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        Assert.Empty(r1.BaseExtensionsNoUseSiteDiagnostics);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        Assert.Empty(r2.BaseExtensionsNoUseSiteDiagnostics);

        var r3 = comp.GlobalNamespace.GetTypeMember("R3");
        Assert.Empty(r3.BaseExtensionsNoUseSiteDiagnostics);

        var r4 = comp.GlobalNamespace.GetTypeMember("R4");
        Assert.Empty(r4.BaseExtensionsNoUseSiteDiagnostics);

        var r5 = comp.GlobalNamespace.GetTypeMember("R5");
        Assert.Empty(r5.BaseExtensionsNoUseSiteDiagnostics);

        var r6 = comp.GlobalNamespace.GetTypeMember("R6");
        Assert.Equal("R5", r6.BaseExtensionsNoUseSiteDiagnostics.Single().ToTestDisplayString());

        var r8 = comp.GlobalNamespace.GetTypeMember("R8");
        Assert.Equal("R7", r8.BaseExtensionsNoUseSiteDiagnostics.Single().ToTestDisplayString());
    }

    [Fact]
    public void BaseExtension_Pointer()
    {
        var src = """
class C { }
explicit extension D<T> for C { }
unsafe explicit extension R1 for C : D<int*> { } // 1
explicit extension R2 for C : D<int*> { } // 2, 3
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,27): error CS0306: The type 'int*' may not be used as a type argument
            // unsafe explicit extension R1 for C : D<int*> { } // 1
            Diagnostic(ErrorCode.ERR_BadTypeArgument, "R1").WithArguments("int*").WithLocation(3, 27),
            // (4,20): error CS0306: The type 'int*' may not be used as a type argument
            // explicit extension R2 for C : D<int*> { } // 2, 3
            Diagnostic(ErrorCode.ERR_BadTypeArgument, "R2").WithArguments("int*").WithLocation(4, 20),
            // (4,33): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // explicit extension R2 for C : D<int*> { } // 2, 3
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(4, 33)
            );
    }

    [Fact]
    public void BaseExtension_Constraints()
    {
        var src = """
class C { }
explicit extension D<T> for C where T : class { }

explicit extension R1 for C : D<string> { }
explicit extension R2 for C : D<int> { } // 1
explicit extension R3<T> for C : D<T> where T : class { }
explicit extension R4<T> for C : D<T> where T : struct { } // 2
explicit extension R5<T> for C : D<T> { } // 3
explicit extension R6 for C : D<R1> { } // 4
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,20): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'D<T>'
            // explicit extension R2 for C : D<int> { } // 1
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "R2").WithArguments("D<T>", "T", "int").WithLocation(5, 20),
            // (7,20): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'D<T>'
            // explicit extension R4<T> for C : D<T> where T : struct { } // 2
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "R4").WithArguments("D<T>", "T", "T").WithLocation(7, 20),
            // (8,20): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'D<T>'
            // explicit extension R5<T> for C : D<T> { } // 3
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "R5").WithArguments("D<T>", "T", "T").WithLocation(8, 20),
            // (9,20): error CS0452: The type 'R1' must be a reference type in order to use it as parameter 'T' in the generic type or method 'D<T>'
            // explicit extension R6 for C : D<R1> { } // 4
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "R6").WithArguments("D<T>", "T", "R1").WithLocation(9, 20)
            );
    }

    [Fact]
    public void BaseExtension_DeriveWithWeakerConstraints()
    {
        var src = """
class C { }
interface I1 { }
interface I2 { }
explicit extension R1<T> for C where T : I1, I2 { }
explicit extension R2<T> for C : R1<T> where T : I2 { } // 1
explicit extension R3<T> for C : R2<T>, R1<T> { } // 2, 3, 4
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,20): error CS0314: The type 'T' cannot be used as type parameter 'T' in the generic type or method 'R1<T>'. There is no boxing conversion or type parameter conversion from 'T' to 'I1'.
            // explicit extension R2<T> for C : R1<T> where T : I2 { } // 1
            Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "R2").WithArguments("R1<T>", "I1", "T", "T").WithLocation(5, 20),
            // (6,20): error CS0314: The type 'T' cannot be used as type parameter 'T' in the generic type or method 'R2<T>'. There is no boxing conversion or type parameter conversion from 'T' to 'I2'.
            // explicit extension R3<T> for C : R2<T>, R1<T> { } // 2, 3, 4
            Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "R3").WithArguments("R2<T>", "I2", "T", "T").WithLocation(6, 20),
            // (6,20): error CS0314: The type 'T' cannot be used as type parameter 'T' in the generic type or method 'R1<T>'. There is no boxing conversion or type parameter conversion from 'T' to 'I1'.
            // explicit extension R3<T> for C : R2<T>, R1<T> { } // 2, 3, 4
            Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "R3").WithArguments("R1<T>", "I1", "T", "T").WithLocation(6, 20),
            // (6,20): error CS0314: The type 'T' cannot be used as type parameter 'T' in the generic type or method 'R1<T>'. There is no boxing conversion or type parameter conversion from 'T' to 'I2'.
            // explicit extension R3<T> for C : R2<T>, R1<T> { } // 2, 3, 4
            Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "R3").WithArguments("R1<T>", "I2", "T", "T").WithLocation(6, 20)
            );
    }

    [Fact]
    public void BaseExtension_DuplicatesAndVariations()
    {
        var src = """
class C { }
explicit extension R0 for C { }

explicit extension R1 for C : R0, R0 { } // 1

partial explicit extension R2 for C : R0 { }
partial explicit extension R2 for C : R0, R0 { } // 2

explicit extension R3<T> for C { }

explicit extension R4 for C :
#nullable enable
    R3<object?>,
#nullable disable
    R3<object> // no diagnostic since reported on oblivious location
#nullable enable
{
}

interface I<T> { }
class D :
#nullable enable
    I<object?>,
#nullable disable
    I<object> // no diagnostic since reported on oblivious location
#nullable enable
{
}

explicit extension R5 for C :
#nullable disable
    R3<object>,
#nullable enable
    R3<object?> // 3
    { }

#nullable enable
explicit extension R6 for C : R3<object>, R3<object?> { } // 4

explicit extension R7 for C : R3<object>, R3<dynamic> { } // 5

explicit extension R8 for C : R3<(int i, int j)>, R3<(int, int)> { } // 6
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,35): error CS9320: 'R0' is already listed in the base extension list
            // explicit extension R1 for C : R0, R0 { } // 1
            Diagnostic(ErrorCode.ERR_DuplicateExtensionInBaseList, "R0").WithArguments("R0").WithLocation(4, 35),
            // (7,43): error CS9320: 'R0' is already listed in the base extension list
            // partial explicit extension R2 for C : R0, R0 { } // 2
            Diagnostic(ErrorCode.ERR_DuplicateExtensionInBaseList, "R0").WithArguments("R0").WithLocation(7, 43),
            // (34,5): warning CS9317: 'R3<object?>' is already listed in the base extension list on type 'R5' with different nullability of reference types.
            //     R3<object?> // 3
            Diagnostic(ErrorCode.WRN_DuplicateExtensionWithNullabilityMismatchInBaseList, "R3<object?>").WithArguments("R3<object?>", "R5").WithLocation(34, 5),
            // (38,20): warning CS9317: 'R3<object?>' is already listed in the base extension list on type 'R6' with different nullability of reference types.
            // explicit extension R6 for C : R3<object>, R3<object?> { } // 4
            Diagnostic(ErrorCode.WRN_DuplicateExtensionWithNullabilityMismatchInBaseList, "R6").WithArguments("R3<object?>", "R6").WithLocation(38, 20),
            // (40,20): error CS9319: 'R3<dynamic>' is already listed in the base extension list on type 'R7' as 'R3<object>'.
            // explicit extension R7 for C : R3<object>, R3<dynamic> { } // 5
            Diagnostic(ErrorCode.ERR_DuplicateExtensionWithDifferencesInBaseList, "R7").WithArguments("R3<dynamic>", "R3<object>", "R7").WithLocation(40, 20),
            // (42,20): error CS9318: 'R3<(int, int)>' is already listed in the base extension list on type 'R8' with different tuple element names, as 'R3<(int i, int j)>'.
            // explicit extension R8 for C : R3<(int i, int j)>, R3<(int, int)> { } // 6
            Diagnostic(ErrorCode.ERR_DuplicateExtensionWithTupleNamesInBaseList, "R8").WithArguments("R3<(int, int)>", "R3<(int i, int j)>", "R8").WithLocation(42, 20)
            );
    }

    [Fact]
    public void BaseExtension_DuplicatesAndVariations_FromBase()
    {
        var src = """
class C { }
explicit extension R0 for C { }

explicit extension R1a for C : R0 { }
explicit extension R1b for C : R0, R1a { }

partial explicit extension R2 for C : R0 { }
partial explicit extension R2 for C : R1a, R0 { }

explicit extension R3<T> for C { }

#nullable disable
explicit extension R5a for C : R3<object> { }
#nullable enable
explicit extension R5b for C : R5a, R3<object?> { }

#nullable enable
explicit extension R6a for C : R3<object> { }
explicit extension R6b for C : R6a, R3<object?> { } // 1

explicit extension R7a for C : R3<object> { }
explicit extension R7b for C : R7a, R3<dynamic> { } // 2

explicit extension R8a for C : R3<(int i, int j)> { }
explicit extension R8b for C : R8a, R3<(int, int)> { } // 3
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (19,20): warning CS9317: 'R3<object?>' is already listed in the base extension list on type 'R6b' with different nullability of reference types.
            // explicit extension R6b for C : R6a, R3<object?> { } // 1
            Diagnostic(ErrorCode.WRN_DuplicateExtensionWithNullabilityMismatchInBaseList, "R6b").WithArguments("R3<object?>", "R6b").WithLocation(19, 20),
            // (22,20): error CS9319: 'R3<dynamic>' is already listed in the base extension list on type 'R7b' as 'R3<object>'.
            // explicit extension R7b for C : R7a, R3<dynamic> { } // 2
            Diagnostic(ErrorCode.ERR_DuplicateExtensionWithDifferencesInBaseList, "R7b").WithArguments("R3<dynamic>", "R3<object>", "R7b").WithLocation(22, 20),
            // (25,20): error CS9318: 'R3<(int, int)>' is already listed in the base extension list on type 'R8b' with different tuple element names, as 'R3<(int i, int j)>'.
            // explicit extension R8b for C : R8a, R3<(int, int)> { } // 3
            Diagnostic(ErrorCode.ERR_DuplicateExtensionWithTupleNamesInBaseList, "R8b").WithArguments("R3<(int, int)>", "R3<(int i, int j)>", "R8b").WithLocation(25, 20)
            );
    }

    [Fact]
    public void BaseExtension_CouldUnify()
    {
        var src = """
class C { }
explicit extension R1<T> for C { }
explicit extension R2<T1, T2> for C : R1<T1>, R1<T2> { }

interface I1<T> { }
interface I2<T1, T2> : I1<T1>, I1<T2> { } // 1
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (6,11): error CS0695: 'I2<T1, T2>' cannot implement both 'I1<T1>' and 'I1<T2>' because they may unify for some type parameter substitutions
            // interface I2<T1, T2> : I1<T1>, I1<T2> { } // 1
            Diagnostic(ErrorCode.ERR_UnifyingInterfaceInstantiations, "I2").WithArguments("I2<T1, T2>", "I1<T1>", "I1<T2>").WithLocation(6, 11)
            );
    }

    [Fact]
    public void BaseExtension_UnderlyingTypeMismatch()
    {
        var src = """
explicit extension R1 for int { }
explicit extension R2 for long : R1 { } // 1

class C<T> { }
explicit extension R3 for C<object> { }
explicit extension R4 for C<dynamic> : R3 { } // 2

explicit extension R5 for (int i, int j) { }
explicit extension R6 for (int, int) : R5 { } // 3

#nullable enable
explicit extension R7 for string { }
#nullable disable
explicit extension R8 for string : R7 { }

#nullable enable
explicit extension R9 for C<string> { }
#nullable disable
explicit extension R10 for C<string> : R9 { } // 4

explicit extension R12 for C<string> : R11 { } // 5
#nullable enable
explicit extension R11 for C<string> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,20): error CS9316: Extension 'R2' extends 'long' but base extension 'R1' extends 'int'.
            // explicit extension R2 for long : R1 { } // 1
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R2").WithArguments("R2", "long", "R1", "int").WithLocation(2, 20),
            // (6,20): error CS9316: Extension 'R4' extends 'C<dynamic>' but base extension 'R3' extends 'C<object>'.
            // explicit extension R4 for C<dynamic> : R3 { } // 2
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R4").WithArguments("R4", "C<dynamic>", "R3", "C<object>").WithLocation(6, 20),
            // (9,20): error CS9316: Extension 'R6' extends '(int, int)' but base extension 'R5' extends '(int i, int j)'.
            // explicit extension R6 for (int, int) : R5 { } // 3
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R6").WithArguments("R6", "(int, int)", "R5", "(int i, int j)").WithLocation(9, 20),
            // (19,20): error CS9316: Extension 'R10' extends 'C<string>' but base extension 'R9' extends 'C<string>'.
            // explicit extension R10 for C<string> : R9 { } // 4
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R10").WithArguments("R10", "C<string>", "R9", "C<string>").WithLocation(19, 20),
            // (21,20): error CS9316: Extension 'R12' extends 'C<string>' but base extension 'R11' extends 'C<string>'.
            // explicit extension R12 for C<string> : R11 { } // 5
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R12").WithArguments("R12", "C<string>", "R11", "C<string>").WithLocation(21, 20)
            );
    }

    [Fact]
    public void BaseExtension_UnderlyingTypeMismatch_Generic()
    {
        var src = """
explicit extension R1<T> for T { }
explicit extension R2<U> for U : R1<U> { }

class C<T> { }
explicit extension R3<T> for C<T> { }
explicit extension R4<U> for C<U> : R3<U> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        Assert.Equal("U", r2.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal("U", r2.BaseExtensionsNoUseSiteDiagnostics.Single().ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());

        var r4 = comp.GlobalNamespace.GetTypeMember("R4");
        Assert.Equal("C<U>", r4.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal("C<U>", r4.BaseExtensionsNoUseSiteDiagnostics.Single().ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void BaseExtension_UnderlyingTypeMismatch_PE()
    {
        // class C { }
        // explicit extension R1 for object { }
        // explicit extension R2 for C : R1 { }
        var ilSource = """
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}

.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '<ExplicitExtension>$'(object '') cil managed
    {
        IL_0000: ret
    }
}

.class public sequential ansi sealed beforefieldinit R2
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '<ExplicitExtension>$'(class C '', valuetype R1 '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R3 for object : R2 { }
public explicit extension R4 for C : R2 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R3' extends 'object' but base extension 'R2' extends 'C'.
            // public explicit extension R3 for object : R2 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R3").WithArguments("R3", "object", "R2", "C").WithLocation(1, 27),
            // (2,27): error CS9316: Extension 'R4' extends 'C' but base extension 'R1' extends 'object'.
            // public explicit extension R4 for C : R2 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R4").WithArguments("R4", "C", "R1", "object").WithLocation(2, 27)
            );

        var r2 = (PENamedTypeSymbol)comp.GlobalNamespace.GetTypeMember("R2");
        var r2BaseExtension = r2.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("R1", r2BaseExtension.ToTestDisplayString());
        Assert.False(r2BaseExtension.IsErrorType());
    }

    [Fact]
    public void BaseExtension_UnderlyingTypeMismatch_Retargeting()
    {
        var src1 = """
public class C { }
public explicit extension E1 for C { }
""";
        var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net70,
            assemblyName: "first");
        comp1.VerifyDiagnostics();

        var src2 = """
public explicit extension E2 for C : E1 { }
""";
        var comp2 = CreateCompilation(src2, targetFramework: TargetFramework.Net70,
            references: new[] { comp1.EmitToImageReference() });
        comp2.VerifyDiagnostics();

        var src1Updated = """
public class C { }
public explicit extension E1 for object { }
""";
        var comp1Updated = CreateCompilation(src1Updated, targetFramework: TargetFramework.Net70,
            assemblyName: "first");
        comp1Updated.VerifyDiagnostics();

        var src = """
explicit extension E3 for C : E2 { }
explicit extension E4 for object : E2 { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70,
            references: new[] { comp1Updated.EmitToImageReference(), comp2.ToMetadataReference() });
        comp.VerifyDiagnostics(
            // (1,20): error CS8090: There is an error in a referenced assembly 'first, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // explicit extension E3 for C : E2 { }
            Diagnostic(ErrorCode.ERR_ErrorInReferencedAssembly, "E3").WithArguments("first, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 20),
            // (2,20): error CS8090: There is an error in a referenced assembly 'first, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // explicit extension E4 for object : E2 { }
            Diagnostic(ErrorCode.ERR_ErrorInReferencedAssembly, "E4").WithArguments("first, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 20),
            // (2,20): error CS9316: Extension 'E4' extends 'object' but base extension 'E2' extends 'C'.
            // explicit extension E4 for object : E2 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "E4").WithArguments("E4", "object", "E2", "C").WithLocation(2, 20)
            );

        var e2 = (RetargetingNamedTypeSymbol)comp.GlobalNamespace.GetTypeMember("E2");
        var e2BaseExtension = e2.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("E1", e2BaseExtension.ToTestDisplayString());

        AssertEx.Equal("error CS8090: There is an error in a referenced assembly 'first, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.",
            ((ErrorTypeSymbol)e2BaseExtension).ErrorInfo.ToString());
    }

    [Fact]
    public void BaseExtension_StaticType_InstanceExtension()
    {
        var src = """
class UnderlyingClass { }
static explicit extension StaticExtension for UnderlyingClass { }
explicit extension R for UnderlyingClass : StaticExtension { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("StaticExtension", r.BaseExtensionsNoUseSiteDiagnostics.Single().ToTestDisplayString());
        Assert.False(r.IsStatic);

        var staticExtension = comp.GlobalNamespace.GetTypeMember("StaticExtension");
        Assert.True(staticExtension.IsStatic);
    }

    [Fact]
    public void Modifiers_Partial()
    {
        var src = """
class UnderlyingClass { }
partial explicit extension R for UnderlyingClass
{
}
partial explicit extension R for UnderlyingClass
{
}
partial explicit extension R
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.False(r.IsStatic);
    }

    [Fact]
    public void Modifiers_Partial_UnpartialDeclaration()
    {
        var src = """
class C { }
partial explicit extension R for C { }
explicit extension R for C { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,20): error CS0260: Missing partial modifier on declaration of type 'R'; another partial declaration of this type exists
            // explicit extension R for C { }
            Diagnostic(ErrorCode.ERR_MissingPartial, "R").WithArguments("R").WithLocation(3, 20)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("C", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void Modifiers_Unsafe()
    {
        var src = """
class UnderlyingClass { }
unsafe explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,27): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "R").WithLocation(2, 27)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.False(r.IsStatic);
    }

    [Fact]
    public void Modifiers_Unsafe_InUnsafeCompilation()
    {
        var src = """
class UnderlyingClass { }
unsafe explicit extension R for UnderlyingClass
{
    int* M(int* i) => i;
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.False(r.IsStatic);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Modifiers_New()
    {
        var src = """
class UnderlyingClass { }
explicit extension BaseExtension for UnderlyingClass
{
    class R { }
}
class UnderlyingClass2 { }
explicit extension DerivedExtension for UnderlyingClass : BaseExtension
{
    new explicit extension R for UnderlyingClass2 { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("DerivedExtension").GetTypeMember("R");
        Assert.Equal("DerivedExtension.R", r.ToTestDisplayString());
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("UnderlyingClass2", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        // PROTOTYPE verify hiding in usages/lookups
    }

    [Fact]
    public void Modifiers_Public()
    {
        var src = """
public struct UnderlyingStruct { }
public explicit extension R for UnderlyingStruct
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal(Accessibility.Public, r.DeclaredAccessibility);
        // PROTOTYPE verify accessibility from source and metadata references
    }

    [Fact]
    public void Modifiers_Protected()
    {
        var src = """
struct UnderlyingStruct { }
protected explicit extension R for UnderlyingStruct
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,30): error CS1527: Elements defined in a namespace cannot be explicitly declared as private, protected, protected internal, or private protected
            // protected explicit extension R for UnderlyingStruct
            Diagnostic(ErrorCode.ERR_NoNamespacePrivate, "R").WithLocation(2, 30),
            // (2,30): error CS9309: Inconsistent accessibility: underlying type 'UnderlyingStruct' is less accessible than extension 'R'
            // protected explicit extension R for UnderlyingStruct
            Diagnostic(ErrorCode.ERR_BadVisUnderlyingType, "R").WithArguments("R", "UnderlyingStruct").WithLocation(2, 30)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal(Accessibility.Protected, r.DeclaredAccessibility);
    }

    [Fact]
    public void Modifiers_Protected_InSealedContainingType()
    {
        var src = """
struct S { }
sealed class C
{
    protected explicit extension R for S { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,34): warning CS0628: 'C.R': new protected member declared in sealed type
            //     protected explicit extension R for S { }
            Diagnostic(ErrorCode.WRN_ProtectedInSealed, "R").WithArguments("C.R").WithLocation(4, 34)
            );
    }

    [Fact]
    public void Modifiers_Protected_Nested()
    {
        var src = """
struct UnderlyingStruct { }
class C
{
    protected explicit extension R for UnderlyingStruct
    {
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("C").GetTypeMember("R");
        Assert.Equal("C.R", r.ToTestDisplayString());
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal(Accessibility.Protected, r.DeclaredAccessibility);
        Assert.Equal("C", r.ContainingType.ToTestDisplayString());
        // PROTOTYPE verify accessibility from source and metadata references
    }

    [Fact]
    public void Modifiers_Internal()
    {
        var src = """
struct UnderlyingStruct { }
internal explicit extension R for UnderlyingStruct
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
        // PROTOTYPE verify accessibility from source and metadata references
    }

    [Fact]
    public void Modifiers_ProtectedInternal()
    {
        var src = """
struct UnderlyingStruct { }
protected internal explicit extension R for UnderlyingStruct
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,39): error CS1527: Elements defined in a namespace cannot be explicitly declared as private, protected, protected internal, or private protected
            // protected internal explicit extension R for UnderlyingStruct
            Diagnostic(ErrorCode.ERR_NoNamespacePrivate, "R").WithLocation(2, 39),
            // (2,39): error CS9309: Inconsistent accessibility: underlying type 'UnderlyingStruct' is less accessible than extension 'R'
            // protected internal explicit extension R for UnderlyingStruct
            Diagnostic(ErrorCode.ERR_BadVisUnderlyingType, "R").WithArguments("R", "UnderlyingStruct").WithLocation(2, 39)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal(Accessibility.ProtectedOrInternal, r.DeclaredAccessibility);
    }

    [Fact]
    public void Modifiers_ProtectedInternal_Nested()
    {
        var src = """
struct UnderlyingStruct { }
class C
{
    protected internal explicit extension R for UnderlyingStruct
    {
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("C").GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal(Accessibility.ProtectedOrInternal, r.DeclaredAccessibility);
        // PROTOTYPE verify accessibility from source and metadata references
    }

    [Fact]
    public void Modifiers_Private()
    {
        var src = """
struct UnderlyingStruct { }
private explicit extension R for UnderlyingStruct
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,28): error CS1527: Elements defined in a namespace cannot be explicitly declared as private, protected, protected internal, or private protected
            // private explicit extension R for UnderlyingStruct
            Diagnostic(ErrorCode.ERR_NoNamespacePrivate, "R").WithLocation(2, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal(Accessibility.Private, r.DeclaredAccessibility);
    }

    [Fact]
    public void Modifiers_Private_Nested()
    {
        var src = """
struct UnderlyingStruct { }
class C
{
    private explicit extension R for UnderlyingStruct
    {
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("C").GetTypeMember("R");
        Assert.Equal("C.R", r.ToTestDisplayString());
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal(Accessibility.Private, r.DeclaredAccessibility);
    }

    [Fact]
    public void Modifiers_File()
    {
        var src = """
struct UnderlyingStruct { }
file explicit extension R for UnderlyingStruct
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R@<tree 0>", r.ToTestDisplayString());
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
        // PROTOTYPE verify visibility from source references, in name or extension lookup
    }

    [Fact]
    public void Modifiers_File_WithAccessibility()
    {
        var src = """
struct UnderlyingStruct { }
file internal explicit extension R for UnderlyingStruct
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,34): error CS9052: File-local type 'R' cannot use accessibility modifiers.
            // file internal explicit extension R for UnderlyingStruct
            Diagnostic(ErrorCode.ERR_FileTypeNoExplicitAccessibility, "R").WithArguments("R").WithLocation(2, 34)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R@<tree 0>", r.ToTestDisplayString());
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
    }

    [Fact]
    public void Modifiers_File_DuplicateName()
    {
        var src = """
struct S { }
file explicit extension R for S { }
file explicit extension R for S { }

class C
{
    explicit extension R2 for S { }
    explicit extension R2 for S { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,25): error CS9071: The namespace '<global namespace>' already contains a definition for 'R' in this file.
            // file explicit extension R for S { }
            Diagnostic(ErrorCode.ERR_FileLocalDuplicateNameInNS, "R").WithArguments("R", "<global namespace>").WithLocation(3, 25),
            // (8,24): error CS0102: The type 'C' already contains a definition for 'R2'
            //     explicit extension R2 for S { }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "R2").WithArguments("C", "R2").WithLocation(8, 24)
            );
    }

    [Fact]
    public void Modifiers_File_DuplicateName_SeparateFiles()
    {
        var src1 = """
struct S { }
file explicit extension R for S { }

partial class C
{
    explicit extension R2 for S { }
}
""";
        var src2 = """
file explicit extension R for S { }

partial class C
{
    explicit extension R2 for S { }
}
""";
        var comp = CreateCompilation(new[] { (src1, "1.cs"), (src2, "2.cs") }, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // 2.cs(5,24): error CS0102: The type 'C' already contains a definition for 'R2'
            //     explicit extension R2 for S { }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "R2").WithArguments("C", "R2").WithLocation(5, 24)
            );
    }

    [Fact]
    public void Modifiers_Duplicate()
    {
        var src = """
class UnderlyingClass { }
internal internal explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,10): error CS1004: Duplicate 'internal' modifier
            // internal internal explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_DuplicateModifier, "internal").WithArguments("internal").WithLocation(2, 10)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
    }

    [Fact]
    public void Modifiers_IncompatibleAccessibilities()
    {
        var src = """
class UnderlyingClass { }
public internal explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,36): error CS0107: More than one protection modifier
            // public internal explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberProtection, "R").WithLocation(2, 36),
            // (2,36): error CS9309: Inconsistent accessibility: underlying type 'UnderlyingClass' is less accessible than extension 'R'
            // public internal explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadVisUnderlyingType, "R").WithArguments("R", "UnderlyingClass").WithLocation(2, 36)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(Accessibility.Public, r.DeclaredAccessibility);
    }

    [Fact]
    public void Modifiers_IncompatibleAccessibilities_ReverseOrder()
    {
        var src = """
class UnderlyingClass { }
internal public explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,36): error CS0107: More than one protection modifier
            // internal public explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberProtection, "R").WithLocation(2, 36),
            // (2,36): error CS9309: Inconsistent accessibility: underlying type 'UnderlyingClass' is less accessible than extension 'R'
            // internal public explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadVisUnderlyingType, "R").WithArguments("R", "UnderlyingClass").WithLocation(2, 36)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(Accessibility.Public, r.DeclaredAccessibility);
    }

    [Fact]
    public void Modifiers_Abstract()
    {
        var src = """
class UnderlyingClass { }
abstract explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,29): error CS0106: The modifier 'abstract' is not valid for this item
            // abstract explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("abstract").WithLocation(2, 29)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
        Assert.Equal("UnderlyingClass", r.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void Modifiers_Readonly()
    {
        var src = """
class UnderlyingClass { }
readonly explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,29): error CS0106: The modifier 'readonly' is not valid for this item
            // readonly explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("readonly").WithLocation(2, 29)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
    }

    [Fact]
    public void Modifiers_Const()
    {
        var src = """
class UnderlyingClass { }
const explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,1): error CS8803: Top-level statements must precede namespace and type declarations.
            // const explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "const ").WithLocation(2, 1),
            // (2,7): error CS1031: Type expected
            // const explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_TypeExpected, "explicit").WithLocation(2, 7),
            // (2,7): error CS1001: Identifier expected
            // const explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "explicit").WithLocation(2, 7),
            // (2,7): error CS0145: A const field requires a value to be provided
            // const explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_ConstValueRequired, "explicit").WithLocation(2, 7),
            // (2,7): error CS1002: ; expected
            // const explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "explicit").WithLocation(2, 7)
            );
    }

    [Fact]
    public void Modifiers_Volatile()
    {
        var src = """
class UnderlyingClass { }
volatile explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,29): error CS0106: The modifier 'volatile' is not valid for this item
            // volatile explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("volatile").WithLocation(2, 29)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
    }

    [Fact]
    public void Modifiers_Extern()
    {
        var src = """
class UnderlyingClass { }
extern explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,27): error CS0106: The modifier 'extern' is not valid for this item
            // extern explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("extern").WithLocation(2, 27)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
    }

    [Fact]
    public void Modifiers_Fixed()
    {
        var src = """
class UnderlyingClass { }
fixed explicit extension R for UnderlyingClass { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,7): error CS1031: Type expected
            // fixed explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "explicit").WithLocation(2, 7),
            // (2,7): error CS1001: Identifier expected
            // fixed explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "explicit").WithLocation(2, 7),
            // (2,7): error CS1003: Syntax error, '[' expected
            // fixed explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("[").WithLocation(2, 7),
            // (2,7): error CS1003: Syntax error, ']' expected
            // fixed explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("]").WithLocation(2, 7),
            // (2,7): error CS0443: Syntax error; value expected
            // fixed explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_ValueExpected, "explicit").WithLocation(2, 7),
            // (2,7): error CS1002: ; expected
            // fixed explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "explicit").WithLocation(2, 7),
            // (2,7): error CS1642: Fixed size buffer fields may only be members of structs
            // fixed explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_FixedNotInStruct, "").WithLocation(2, 7),
            // (2,7): error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
            // fixed explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_IllegalFixedType, "").WithLocation(2, 7),
            // (2,7): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // fixed explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "").WithLocation(2, 7)
            );
    }

    [Fact]
    public void Modifiers_Virtual()
    {
        var src = """
class UnderlyingClass { }
virtual explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,28): error CS0106: The modifier 'virtual' is not valid for this item
            // virtual explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("virtual").WithLocation(2, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
    }

    [Fact]
    public void Modifiers_Override()
    {
        var src = """
class UnderlyingClass { }
override explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,29): error CS0106: The modifier 'override' is not valid for this item
            // override explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("override").WithLocation(2, 29)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
    }

    [Fact]
    public void Modifiers_Async()
    {
        var src = """
class UnderlyingClass { }
async explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,26): error CS0106: The modifier 'async' is not valid for this item
            // async explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("async").WithLocation(2, 26)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
    }

    [Fact]
    public void Modifiers_Ref()
    {
        var src = """
class UnderlyingClass { }
ref explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,5): error CS1031: Type expected
            // ref explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_TypeExpected, "explicit").WithLocation(2, 5)
            );
    }

    [Fact]
    public void Modifiers_Required()
    {
        var src = """
class UnderlyingClass { }
required explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,29): error CS0106: The modifier 'required' is not valid for this item
            // required explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("required").WithLocation(2, 29)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceExtensionTypeSymbol>(r, isExplicit: true);
    }

    [Fact]
    public void Modifiers_Scoped()
    {
        var src = """
class UnderlyingClass { }
scoped explicit extension R for UnderlyingClass { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,1): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
            // scoped explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "scoped").WithArguments("+").WithLocation(2, 1),
            // (2,1): error CS0246: The type or namespace name 'scoped' could not be found (are you missing a using directive or an assembly reference?)
            // scoped explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "scoped").WithArguments("scoped").WithLocation(2, 1),
            // (2,8): error CS1003: Syntax error, 'operator' expected
            // scoped explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(2, 8),
            // (2,8): error CS1020: Overloadable binary operator expected
            // scoped explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, "explicit").WithLocation(2, 8),
            // (2,8): error CS0558: User-defined operator '<invalid-global-code>.operator +(extension, UnderlyingClass)' must be declared static and public
            // scoped explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_OperatorsMustBeStatic, "").WithArguments("<invalid-global-code>.operator +(extension, UnderlyingClass)").WithLocation(2, 8),
            // (2,8): error CS0501: '<invalid-global-code>.operator +(extension, UnderlyingClass)' must declare a body because it is not marked abstract, extern, or partial
            // scoped explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "").WithArguments("<invalid-global-code>.operator +(extension, UnderlyingClass)").WithLocation(2, 8),
            // (2,8): error CS0563: One of the parameters of a binary operator must be the containing type
            // scoped explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_BadBinaryOperatorSignature, "").WithLocation(2, 8),
            // (2,17): error CS1003: Syntax error, '(' expected
            // scoped explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "extension").WithArguments("(").WithLocation(2, 17),
            // (2,17): error CS0246: The type or namespace name 'extension' could not be found (are you missing a using directive or an assembly reference?)
            // scoped explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "extension").WithArguments("extension").WithLocation(2, 17),
            // (2,29): error CS1003: Syntax error, ',' expected
            // scoped explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "for").WithArguments(",").WithLocation(2, 29),
            // (2,33): error CS1003: Syntax error, ',' expected
            // scoped explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "UnderlyingClass").WithArguments(",").WithLocation(2, 33),
            // (2,49): error CS1001: Identifier expected
            // scoped explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(2, 49),
            // (2,49): error CS1003: Syntax error, ',' expected
            // scoped explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(2, 49),
            // (2,51): error CS1026: ) expected
            // scoped explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "}").WithLocation(2, 51),
            // (2,51): error CS1002: ; expected
            // scoped explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(2, 51),
            // (2,51): error CS1022: Type or namespace definition, or end-of-file expected
            // scoped explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(2, 51)
            );
    }

    [Theory]
    [InlineData("int")]
    [InlineData("string")]
    [InlineData("T")]
    public void NotExtension_ArrayTypeSymbol(string type)
    {
        var src = $$"""
class C<T>
{
    {{type}}[] M() => throw null;
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var m = comp.GetMember<MethodSymbol>("C.M");
        VerifyNotExtension<ArrayTypeSymbol>(m.ReturnType);
    }

    [Fact]
    public void NotExtension_DynamicTypeSymbol()
    {
        var src = $$"""
class C
{
    dynamic M() => throw null;
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var m = comp.GetMember<MethodSymbol>("C.M");
        VerifyNotExtension<DynamicTypeSymbol>(m.ReturnType);
    }

    [Fact]
    public void NotExtension_AnonymousTypeSymbol()
    {
        var src = $$"""
class C
{
    void M()
    {
        var a = new { A = 1 };
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var variableDeclarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        var variable = model.GetDeclaredSymbol(variableDeclarator);
        var publicType = (Symbols.PublicModel.NonErrorNamedTypeSymbol)variable.GetTypeOrReturnType();
        VerifyNotExtension<AnonymousTypeManager.AnonymousTypePublicSymbol>(publicType.UnderlyingNamedTypeSymbol);
    }

    [Fact]
    public void NotExtension_ErrorTypeSymbol()
    {
        var src = $$"""
class C
{
    error M() => throw null;
}
""";
        var comp = CreateCompilation(src);
        var m = comp.GetMember<MethodSymbol>("C.M");
        VerifyNotExtension<ErrorTypeSymbol>(m.ReturnType);
    }

    [Fact]
    public void NotExtension_FunctionTypeSymbol()
    {
        var src = $$"""
delegate void M();
""";
        var comp = CreateCompilation(src);
        var m = comp.GetMember<NamedTypeSymbol>("M");
        var functionType = new FunctionTypeSymbol(m);
        VerifyNotExtension<FunctionTypeSymbol>(functionType);
    }

    [Fact]
    public void NotExtension_NativeIntegerTypeSymbol()
    {
        var src = $$"""
class C
{
    nint M() => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net60);
        var m = comp.GetMember<MethodSymbol>("C.M");
        VerifyNotExtension<NativeIntegerTypeSymbol>(m.ReturnType);
    }

    [Fact]
    public void NotExtension_NativeIntegerTypeSymbol_Custom()
    {
        var src = $$"""
namespace System
{
    public class Object { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public class String { }
    public class Exception { }
    public class ValueType { }
    public class Enum { }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public enum AttributeTargets { All = 32767, }
    public class ObsoleteAttribute : Attribute
    {
        public ObsoleteAttribute() { }
        public ObsoleteAttribute(string message) { }
        public ObsoleteAttribute(string message, bool error) { }

        public string DiagnosticId { get; set; }
        public string UrlFormat { get; set; }
    }
    public explicit extension IntPtr for object { }
}
class C
{
    nint M() => throw null;
}
""";
        var comp = CreateEmptyCompilation(new[] { src, CompilerFeatureRequiredAttribute });
        comp.VerifyDiagnostics(
            // 0.cs(32,5): error CS0518: Predefined type 'IntPtr' is not defined or imported
            //     nint M() => throw null;
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "nint").WithArguments("IntPtr").WithLocation(32, 5)
            );
        var m = comp.GetMember<MethodSymbol>("C.M");
        VerifyNotExtension<MissingMetadataTypeSymbol.TopLevel>(m.ReturnType);
        Assert.True(comp.GetSpecialType(SpecialType.System_IntPtr).IsErrorType());

        var intPtr = comp.GetTypeByMetadataName("System.IntPtr");
        VerifyExtension<SourceExtensionTypeSymbol>(intPtr, isExplicit: true, SpecialType.System_IntPtr);
    }

    [Fact]
    public void NotExtension_Tuple_Custom()
    {
        var src = $$"""
namespace System
{
    public class Object { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public class String { }
    public class Exception { }
    public class ValueType { }
    public explicit extension ValueTuple<T1, T2> for object { }
    public class Enum { }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public enum AttributeTargets { All = 32767, }
    public class ObsoleteAttribute : Attribute
    {
        public ObsoleteAttribute() { }
        public ObsoleteAttribute(string message) { }
        public ObsoleteAttribute(string message, bool error) { }

        public string DiagnosticId { get; set; }
        public string UrlFormat { get; set; }
    }
}
class C
{
    (object, object) M() => throw null;
}
""";
        var comp = CreateEmptyCompilation(new[] { src, CompilerFeatureRequiredAttribute });
        comp.VerifyDiagnostics(
            // 0.cs(32,5): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
            //     (object, object) M() => throw null;
            Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(object, object)").WithArguments("System.ValueTuple`2").WithLocation(32, 5)
            );
        var m = comp.GetMember<MethodSymbol>("C.M");
        VerifyNotExtension<ConstructedErrorTypeSymbol>(m.ReturnType);
        Assert.True(comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).IsErrorType());

        var valueTuple = comp.GetTypeByMetadataName("System.ValueTuple`2");
        VerifyExtension<SourceExtensionTypeSymbol>(valueTuple, isExplicit: true);
    }

    [Fact]
    public void NotExtension_PointerTypeSymbol()
    {
        var src = $$"""
unsafe class C
{
    int* M() => throw null;
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics();
        var m = comp.GetMember<MethodSymbol>("C.M");
        VerifyNotExtension<PointerTypeSymbol>(m.ReturnType);
    }

    [Fact]
    public void NotExtension_FunctionPointerTypeSymbol()
    {
        var src = $$"""
unsafe class C
{
    delegate*<void> M() => throw null;
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics();
        var m = comp.GetMember<MethodSymbol>("C.M");
        VerifyNotExtension<FunctionPointerTypeSymbol>(m.ReturnType);
    }

    [Theory, CombinatorialData]
    public void IsExtension_SubstitutedNamedTypeSymbol(bool isExplicit)
    {
        var src = $$"""
{{(isExplicit ? "explicit" : "implicit")}} extension E1<T> for int { }
explicit extension E2 for int : E1<object> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        if (isExplicit)
        {
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        }
        else
        {
            comp.VerifyDiagnostics(
                // (1,20): error CS9328: The underlying type 'int' of implicit extension 'E1<T>' must reference all the type parameters declared by the extension, but type parameter 'T' is missing.
                // implicit extension E1<T> for int { }
                Diagnostic(ErrorCode.ERR_UnderspecifiedImplicitExtension, "E1").WithArguments("int", "E1<T>", "T").WithLocation(1, 20)
                );

            validate(comp.SourceModule);
        }

        return;

        void validate(ModuleSymbol module)
        {
            var e2 = module.GlobalNamespace.GetTypeMember("E2");
            var substE1 = e2.BaseExtensionsNoUseSiteDiagnostics.Single();
            Assert.Equal("E1<object>", substE1.ToDisplayString());
            VerifyExtension<SubstitutedNamedTypeSymbol>(substE1, isExplicit: isExplicit);

            Assert.False(substE1.IsDefinition);
            Assert.Equal("E1<T>", substE1.OriginalDefinition.ToTestDisplayString());
        }
    }

    [Theory, CombinatorialData]
    public void IsExtension_NestedNamedTypeSymbol(bool isExplicit, bool isExplicit2)
    {
        var src = $$"""
{{(isExplicit ? "explicit" : "implicit")}} extension E1 for int
{
    {{(isExplicit2 ? "explicit" : "implicit")}} extension E2 for int { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        void validate(ModuleSymbol module)
        {
            var e2 = module.GlobalNamespace.GetTypeMember("E1").GetTypeMember("E2");
            VerifyExtension<NamedTypeSymbol>(e2.ContainingType, isExplicit: isExplicit);
            VerifyExtension<NamedTypeSymbol>(e2, isExplicit: isExplicit2);
        }
    }

    [Theory, CombinatorialData]
    public void IsExtension_Retargeting(bool isExplicit)
    {
        var src = $$"""
public explicit extension E0 for int { }
public {{(isExplicit ? "explicit" : "implicit")}} extension E1 for int : E0 { }
""";
        var comp1 = CreateCompilation(new[] { src, CompilerFeatureRequiredAttribute }, targetFramework: TargetFramework.Mscorlib40);
        comp1.VerifyDiagnostics();

        var src2 = """
explicit extension E2 for int : E1 { }
""";

        var comp2 = CreateCompilation(src2, references: new[] { comp1.ToMetadataReference() }, targetFramework: TargetFramework.Mscorlib46);
        comp2.VerifyDiagnostics();

        var e1 = comp2.GlobalNamespace.GetTypeMember("E1");
        VerifyExtension<RetargetingNamedTypeSymbol>(e1, isExplicit: isExplicit);
        Assert.Equal("System.Int32", e1.ExtendedTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(new[] { "E0" }, e1.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
    }

    [Fact]
    public void NotExtension_Retargeting()
    {
        var src = $$"""
class C { }
""";
        var comp1 = CreateCompilation(new[] { src, CompilerFeatureRequiredAttribute }, targetFramework: TargetFramework.Mscorlib40);
        comp1.VerifyDiagnostics();

        var comp2 = CreateCompilation("", references: new[] { comp1.ToMetadataReference() }, targetFramework: TargetFramework.Mscorlib46);
        var c = comp2.GlobalNamespace.GetTypeMember("C");
        VerifyNotExtension<RetargetingNamedTypeSymbol>(c);
        Assert.Null(c.ExtendedTypeNoUseSiteDiagnostics);
        Assert.Empty(c.BaseExtensionsNoUseSiteDiagnostics);
    }

    [Fact]
    public void NotExtension_TypeParameterSymbol()
    {
        var src = $$"""
class C<T>
{
    T M() => throw null;
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var m = comp.GetMember<MethodSymbol>("C.M");
        VerifyNotExtension<TypeParameterSymbol>(m.ReturnType);
    }

    [Fact]
    public void NotExtension_TypeParameterSymbol_ExtensionConstraintDisallowed()
    {
        var src = $$"""
explicit extension R for string { }
class C<T> where T : R
{
    T M() => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,22): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
            // class C<T> where T : R
            Diagnostic(ErrorCode.ERR_BadConstraintType, "R").WithLocation(2, 22)
            );
        var m = comp.GetMember<MethodSymbol>("C.M");
        VerifyNotExtension<TypeParameterSymbol>(m.ReturnType);
    }

    [Fact]
    public void NotExtension_TypeParameterSymbol_ViaSubstitution()
    {
        var src = $$"""
public explicit extension R for string { }
public explicit extension R2 for string : R { }

public class Container<T>
{
    public class C<U> where U : T
    {
        T M() => throw null;
    }
}
class C2
{
    Container<R> M2() => throw null;
    void M3(Container<R>.C<R> cr, Container<R>.C<R2> cr2) { }
}
""";
        // PROTOTYPE the diagnostic will disappear once we have an identity between R and R2
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (14,54): error CS0315: The type 'R2' cannot be used as type parameter 'U' in the generic type or method 'Container<R>.C<U>'. There is no boxing conversion from 'R2' to 'R'.
            //     void M3(Container<R>.C<R> cr, Container<R>.C<R2> cr2) { }
            Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "cr2").WithArguments("Container<R>.C<U>", "R", "U", "R2").WithLocation(14, 54)
            );
        var containerR = comp.GetMember<MethodSymbol>("C2.M2").ReturnType;
        var c = containerR.GetTypeMembers().Single();
        Assert.Equal("Container<R>.C<U>", c.ToTestDisplayString());
        VerifyNotExtension<SubstitutedTypeParameterSymbol>(c.TypeParameters.Single());
    }

    [Fact]
    public void ArrayTypeConstraintViaSubstitution()
    {
        var src = $$"""
public class Container<T>
{
    public class C<U> where U : T
    {
        T M() => throw null;
    }
}
class C2
{
    Container<int[]> M2() => throw null;
    void M3(Container<int[]>.C<int[]> c, Container<int[]>.C<byte[]> c2, Container<int[]>.C<long[]> c3) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (11,69): error CS0311: The type 'byte[]' cannot be used as type parameter 'U' in the generic type or method 'Container<int[]>.C<U>'. There is no implicit reference conversion from 'byte[]' to 'int[]'.
            //     void M3(Container<int[]>.C<int[]> c, Container<int[]>.C<byte[]> c2, Container<int[]>.C<long[]> c3) { }
            Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "c2").WithArguments("Container<int[]>.C<U>", "int[]", "U", "byte[]").WithLocation(11, 69),
            // (11,100): error CS0311: The type 'long[]' cannot be used as type parameter 'U' in the generic type or method 'Container<int[]>.C<U>'. There is no implicit reference conversion from 'long[]' to 'int[]'.
            //     void M3(Container<int[]>.C<int[]> c, Container<int[]>.C<byte[]> c2, Container<int[]>.C<long[]> c3) { }
            Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "c3").WithArguments("Container<int[]>.C<U>", "int[]", "U", "long[]").WithLocation(11, 100)
            );
        var containerR = comp.GetMember<MethodSymbol>("C2.M2").ReturnType;
        var c = containerR.GetTypeMembers().Single();
        Assert.Equal("Container<System.Int32[]>.C<U>", c.ToTestDisplayString());
        var substitutedTypeParameter = c.TypeParameters.Single();
        Assert.False(substitutedTypeParameter.IsArray());
    }

    [Fact]
    public void Attributes()
    {
        var src = """
class MyAttribute : System.Attribute { }
class C { }

[My]
explicit extension R for C
{
    [My] void M() { }
}
""";
        // PROTOTYPE what attribute target should be used for extensions?
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,2): error CS0592: Attribute 'My' is not valid on this declaration type. It is only valid on 'assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter' declarations.
            // [My]
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "My").WithArguments("My", "assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter").WithLocation(4, 2)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("MyAttribute", r.GetAttributes().Single().ToString());

        var m = r.GetMethod("M");
        Assert.Equal("MyAttribute", m.GetAttributes().Single().ToString());
    }

    [Fact]
    public void ReservedTypeNames()
    {
        var src = $$"""
class C { }

explicit extension record for C { }
explicit extension file for C { }
explicit extension required for C { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,20): warning CS8860: Types and aliases should not be named 'record'.
            // explicit extension record for C { }
            Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(3, 20),
            // (4,20): error CS9056: Types and aliases cannot be named 'file'.
            // explicit extension file for C { }
            Diagnostic(ErrorCode.ERR_FileTypeNameDisallowed, "file").WithLocation(4, 20),
            // (5,20): error CS9029: Types and aliases cannot be named 'required'.
            // explicit extension required for C { }
            Diagnostic(ErrorCode.ERR_RequiredNameDisallowed, "required").WithLocation(5, 20)
            );
    }

    [Fact]
    public void ReservedTypeNames_Keyword()
    {
        var text = """explicit extension unsafe for var { }""";
        var comp = CreateCompilation(text, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,20): error CS1001: Identifier expected
            // explicit extension unsafe for var { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "unsafe").WithLocation(1, 20),
            // (1,20): error CS1514: { expected
            // explicit extension unsafe for var { }
            Diagnostic(ErrorCode.ERR_LbraceExpected, "unsafe").WithLocation(1, 20),
            // (1,20): error CS1513: } expected
            // explicit extension unsafe for var { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "unsafe").WithLocation(1, 20),
            // (1,20): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
            // explicit extension unsafe for var { }
            Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "unsafe").WithLocation(1, 20),
            // (1,20): error CS9314: No part of a partial extension '' includes an underlying type specification.
            // explicit extension unsafe for var { }
            Diagnostic(ErrorCode.ERR_ExtensionMissingUnderlyingType, "").WithArguments("").WithLocation(1, 20),
            // (1,27): error CS8803: Top-level statements must precede namespace and type declarations.
            // explicit extension unsafe for var { }
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "for var { }").WithLocation(1, 27),
            // (1,31): error CS1003: Syntax error, '(' expected
            // explicit extension unsafe for var { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "var").WithArguments("(").WithLocation(1, 31),
            // (1,31): error CS0103: The name 'var' does not exist in the current context
            // explicit extension unsafe for var { }
            Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(1, 31),
            // (1,31): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
            // explicit extension unsafe for var { }
            Diagnostic(ErrorCode.ERR_IllegalStatement, "var").WithLocation(1, 31),
            // (1,35): error CS1002: ; expected
            // explicit extension unsafe for var { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 35),
            // (1,35): error CS1525: Invalid expression term '{'
            // explicit extension unsafe for var { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(1, 35),
            // (1,35): error CS1002: ; expected
            // explicit extension unsafe for var { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 35),
            // (1,35): error CS1026: ) expected
            // explicit extension unsafe for var { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(1, 35)
            );
    }

    [Fact]
    public void Entrypoint_Implicit()
    {
        var src = $$"""
class C { }

explicit extension R for C
{
    public static void Main()
    {
        System.Console.Write("hello");
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        Assert.Equal("void R.Main()", comp.GetEntryPoint(cancellationToken: default).ToTestDisplayString());
        // PROTOTYPE confirm we want this and verify execution
    }

    [Fact]
    public void Entrypoint_Explicit()
    {
        string source = @"
class C { }

explicit extension R for C
{
    static void Main() { }
}
";
        var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("R"), targetFramework: TargetFramework.Net70);
        compilation.VerifyDiagnostics(
            // (4,20): error CS1556: 'R' specified for Main method must be a non-generic class, record, struct, or interface
            // explicit extension R for C
            Diagnostic(ErrorCode.ERR_MainClassNotClass, "R").WithArguments("R").WithLocation(4, 20)
            );
        // PROTOTYPE confirm whether we want this
    }

    [Fact]
    public void Partial_PartialMisplaced()
    {
        string source = @"
public class C { }

partial public explicit extension R for C { }
";
        var compilation = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        compilation.VerifyDiagnostics(
            // (4,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'implicit/explicit extension', or a method return type.
            // partial public explicit extension R for C { }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 1)
            );
    }

    [Fact]
    public void SuppressConstraintChecksInitially()
    {
        var text = @"
public class C { }

public explicit extension R1<T> for C where T : C { }
public explicit extension R2<T> for C : R1<R2<T>> { }
";
        var comp = CreateCompilation(text, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,27): error CS0315: The type 'R2<T>' cannot be used as type parameter 'T' in the generic type or method 'R1<T>'. There is no boxing conversion from 'R2<T>' to 'C'.
            // public explicit extension R2<T> for C : R1<R2<T>> { }
            Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "R2").WithArguments("R1<T>", "C", "T", "R2<T>").WithLocation(5, 27)
            );
    }

    [Fact]
    public void SuppressConstraintChecksInitially_PointerAsTypeArgument()
    {
        var text = @"
public class C { }

public explicit extension R1<T> for C { }
public unsafe explicit extension R2<T> for C : R1<int*> { }
";
        var comp = CreateCompilation(text, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,34): error CS0306: The type 'int*' may not be used as a type argument
            // public unsafe explicit extension R2<T> for C : R1<int*> { }
            Diagnostic(ErrorCode.ERR_BadTypeArgument, "R2").WithArguments("int*").WithLocation(5, 34)
            );
    }

    [Fact]
    public void ImplicitAndExplicit()
    {
        var text = """implicit explicit extension R for var { }""";

        var comp = CreateCompilation(text, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,10): error CS1003: Syntax error, 'operator' expected
            // implicit explicit extension R for var { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(1, 10),
            // (1,10): error CS1031: Type expected
            // implicit explicit extension R for var { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "explicit").WithLocation(1, 10),
            // (1,10): error CS1003: Syntax error, '(' expected
            // implicit explicit extension R for var { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("(").WithLocation(1, 10),
            // (1,10): error CS1026: ) expected
            // implicit explicit extension R for var { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "explicit").WithLocation(1, 10),
            // (1,10): error CS1002: ; expected
            // implicit explicit extension R for var { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "explicit").WithLocation(1, 10),
            // (1,10): error CS0558: User-defined operator '<invalid-global-code>.implicit operator ?()' must be declared static and public
            // implicit explicit extension R for var { }
            Diagnostic(ErrorCode.ERR_OperatorsMustBeStatic, "").WithArguments("<invalid-global-code>.implicit operator ?()").WithLocation(1, 10),
            // (1,10): error CS0501: '<invalid-global-code>.implicit operator ?()' must declare a body because it is not marked abstract, extern, or partial
            // implicit explicit extension R for var { }
            Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "").WithArguments("<invalid-global-code>.implicit operator ?()").WithLocation(1, 10),
            // (1,10): error CS1019: Overloadable unary operator expected
            // implicit explicit extension R for var { }
            Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "").WithLocation(1, 10),
            // (1,35): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
            // implicit explicit extension R for var { }
            Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(1, 35)
            );
    }

    private string ExtensionMarkerName(bool isExplicit)
    {
        return isExplicit ? "<ExplicitExtension>$" : "<ImplicitExtension>$";
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_Baseline(bool isExplicit)
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string) = (
        01 00 43 45 78 74 65 6e 73 69 6f 6e 20 74 79 70
        65 73 20 61 72 65 20 6e 6f 74 20 73 75 70 70 6f
        72 74 65 64 20 69 6e 20 74 68 69 73 20 76 65 72
        73 69 6f 6e 20 6f 66 20 79 6f 75 72 20 63 6f 6d
        70 69 6c 65 72 2e 00 00
    )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit)}}'(object '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: isExplicit);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_NotByValueParameter(bool isExplicit)
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit)}}'(object& '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should have an error like "Extension marker method on type '...' is malformed" instead
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R2' extends 'object' but base extension 'R1' extends 'Object'.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R2").WithArguments("R2", "object", "R1", "System.Object").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: isExplicit);
        var r1ExtendedType = r1.ExtendedTypeNoUseSiteDiagnostics;
        Assert.Equal("System.Object", r1ExtendedType.ToTestDisplayString());
        Assert.True(r1ExtendedType.IsErrorType());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_WithModoptOnReturn(bool isExplicit)
    {
        // PROTOTYPE consider allowing modopts in extension marker methods
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void modopt(object) '{{ExtensionMarkerName(isExplicit)}}'(object '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should have an error like "Extension marker method on type '...' is malformed" instead (if we keep an error)
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R2' extends 'object' but base extension 'R1' extends '?'.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R2").WithArguments("R2", "object", "R1", "?").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: isExplicit);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_WithModreqOnReturn(bool isExplicit)
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void modreq(object) '{{ExtensionMarkerName(isExplicit)}}'(object '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should have an error like "Extension marker method on type '...' is malformed" instead
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R2' extends 'object' but base extension 'R1' extends '?'.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R2").WithArguments("R2", "object", "R1", "?").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: isExplicit);
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_WithModoptOnFirstParameter(bool isExplicit)
    {
        // PROTOTYPE consider allowing modopts in extension marker methods
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit)}}'(object modopt(object) '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should have an error like "Extension marker method on type '...' is malformed" instead (if we keep an error)
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R2' extends 'object' but base extension 'R1' extends 'Object'.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R2").WithArguments("R2", "object", "R1", "System.Object").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: isExplicit);
        var r1ExtendedType = r1.ExtendedTypeNoUseSiteDiagnostics;
        Assert.Equal("System.Object", r1ExtendedType.ToTestDisplayString());
        Assert.True(r1ExtendedType.IsErrorType());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_WithModreqOnFirstParameter(bool isExplicit)
    {
        // PROTOTYPE consider allowing modopts in extension marker methods
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit)}}'(object modreq(object) '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should have an error like "Extension marker method on type '...' is malformed" instead
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R2' extends 'object' but base extension 'R1' extends 'Object'.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R2").WithArguments("R2", "object", "R1", "System.Object").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: isExplicit);
        var r1ExtendedType = (ExtendedErrorTypeSymbol)r1.ExtendedTypeNoUseSiteDiagnostics;
        Assert.Equal("System.Object", r1ExtendedType.ToTestDisplayString());
        Assert.True(r1ExtendedType.IsErrorType());
        AssertEx.Equal("error CS9322: Extension marker method on type 'R1' is malformed.", r1ExtendedType.ErrorInfo.ToString());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_WithModoptOnSecondParameter(bool isExplicit)
    {
        // PROTOTYPE consider allowing modopts in extension marker methods
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit)}}'(object '') cil managed
    {
        IL_0000: ret
    }
}

.class public sequential ansi sealed beforefieldinit R2
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit)}}'(object '', valuetype R2 modopt(object) '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R3 for object : R2 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,27): error CS9322: Extension marker method on type 'R2' is malformed.
            // public explicit extension R3 for object : R2 { }
            Diagnostic(ErrorCode.ERR_MalformedExtensionInMetadata, "R3").WithArguments("R2").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: isExplicit);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<PENamedTypeSymbol>(r2, isExplicit: isExplicit);
        var r2BaseExtension = r2.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("R2", r2BaseExtension.ToTestDisplayString());
        Assert.True(r2BaseExtension.IsErrorType());

        var r3 = comp.GlobalNamespace.GetTypeMember("R3");
        VerifyExtension<SourceExtensionTypeSymbol>(r3, isExplicit: true);
    }

    [Fact]
    public void ExtensionMarkerMethod_Missing()
    {
        var ilSource = """
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,43): error CS9307: A base extension must be an extension type.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "R1").WithLocation(1, 43)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyNotExtension<PENamedTypeSymbol>(r1);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_Overloaded(bool isExplicit)
    {
        // A mix between `extension R1 for object { }` and `extension R1 for string { }`
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
        01 00 43 45 78 74 65 6e 73 69 6f 6e 20 74 79 70
        65 73 20 61 72 65 20 6e 6f 74 20 73 75 70 70 6f
        72 74 65 64 20 69 6e 20 74 68 69 73 20 76 65 72
        73 69 6f 6e 20 6f 66 20 79 6f 75 72 20 63 6f 6d
        70 69 6c 65 72 2e 01 00 00
    )

    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit)}}'(object '') cil managed
    {
        IL_0000: ret
    }
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit)}}'(string '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,43): error CS0619: 'R1' is obsolete: 'Extension type are not supported in this version of your compiler.'
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "R1").WithArguments("R1", PEModule.ExtensionMarker).WithLocation(1, 43),
            // (1,43): error CS9307: A base extension must be an extension type.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "R1").WithLocation(1, 43)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyNotExtension<PENamedTypeSymbol>(r1);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);

        comp = CreateCompilationWithIL(src, ilSource,
            options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

        Assert.Equal(new[]
            {
                "R1..ctor()",
                $$"""void R1.{{ExtensionMarkerName(isExplicit)}}(System.Object A_0)""",
                $$"""void R1.{{ExtensionMarkerName(isExplicit)}}(System.String A_0)"""
            },
            comp.GlobalNamespace.GetTypeMember("R1").GetMembers().ToTestDisplayStrings());
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_Overloaded_DifferentExplicitness(bool isExplicit)
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
        01 00 43 45 78 74 65 6e 73 69 6f 6e 20 74 79 70
        65 73 20 61 72 65 20 6e 6f 74 20 73 75 70 70 6f
        72 74 65 64 20 69 6e 20 74 68 69 73 20 76 65 72
        73 69 6f 6e 20 6f 66 20 79 6f 75 72 20 63 6f 6d
        70 69 6c 65 72 2e 01 00 00
    )

    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit)}}'(object '') cil managed
    {
        IL_0000: ret
    }
    .method private hidebysig static void '{{ExtensionMarkerName(!isExplicit)}}'(string '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,43): error CS0619: 'R1' is obsolete: 'Extension type are not supported in this version of your compiler.'
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "R1").WithArguments("R1", PEModule.ExtensionMarker).WithLocation(1, 43),
            // (1,43): error CS9307: A base extension must be an extension type.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "R1").WithLocation(1, 43)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyNotExtension<PENamedTypeSymbol>(r1);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);

        comp = CreateCompilationWithIL(src, ilSource,
            options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

        Assert.Equal(new[]
            {
                "R1..ctor()",
                $$"""void R1.{{ExtensionMarkerName(isExplicit)}}(System.Object A_0)""",
                $$"""void R1.{{ExtensionMarkerName(!isExplicit)}}(System.String A_0)"""
            },
            comp.GlobalNamespace.GetTypeMember("R1").GetMembers().ToTestDisplayStrings());
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_ExtensionMethod(bool isExplicit)
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00)
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit)}}'(object '') cil managed
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
static class OtherExtension
{
    public static void M(this object o) { }
}
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: isExplicit);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_WithThisParameter(bool isExplicit)
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit)}}'(object '') cil managed
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 01 )
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: isExplicit);
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_WithDynamicFirstParameter(bool isExplicit)
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit)}}'(object '') cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.DynamicAttribute::.ctor() = ( 01 00 00 00 )
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        // PROTOTYPE should have a use-site error too
        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R2' extends 'object' but base extension 'R1' extends 'dynamic'.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R2").WithArguments("R2", "object", "R1", "dynamic").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: isExplicit);
        var r1Underyling = r1.ExtendedTypeNoUseSiteDiagnostics;
        Assert.Equal("dynamic", r1Underyling.ToTestDisplayString());
        Assert.True(r1Underyling.IsErrorType());
        Assert.Empty(r1.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_WithExtensionFirstParameter(bool isExplicit)
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R0
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit)}}'(object '') cil managed
    {
        IL_0000: ret
    }
}

.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit)}}'(valuetype R0 '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        // PROTOTYPE this test should be updated once we emit erase references to extensions (different metadata format)
        // PROTOTYPE should have an error like "Extension marker method on type '...' is malformed" instead
        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R2' extends 'object' but base extension 'R1' extends 'R0'.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R2").WithArguments("R2", "object", "R1", "R0").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: isExplicit);
        var r1Underyling = (ExtendedErrorTypeSymbol)r1.ExtendedTypeNoUseSiteDiagnostics;
        Assert.Equal("R0", r1Underyling.ToTestDisplayString());
        Assert.True(r1Underyling.IsErrorType());
        AssertEx.Equal("error CS9322: Extension marker method on type 'R1' is malformed.", r1Underyling.ErrorInfo.ToString());
        Assert.Empty(r1.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Fact]
    public void ExtensionMarkerMethod_WithSelfExtensionFirstParameter()
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit: true)}}'(valuetype R1 '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        // PROTOTYPE this test should be updated once we emit erase references to extensions (different metadata format)
        // PROTOTYPE should have a use-site error too
        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R2' extends 'object' but base extension 'R1' extends 'R1'.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R2").WithArguments("R2", "object", "R1", "R1").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: true);
        var r1Underyling = r1.ExtendedTypeNoUseSiteDiagnostics;
        Assert.Equal("R1", r1Underyling.ToTestDisplayString());
        Assert.True(r1Underyling.IsErrorType());
        Assert.Empty(r1.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Fact]
    public void ExtensionMarkerMethod_WithNonExtensionSecondParameter()
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit: true)}}'(object '', object '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,27): error CS9322: Extension marker method on type 'R1' is malformed.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_MalformedExtensionInMetadata, "R2").WithArguments("R1").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: true);
        var r1BaseExtension = r1.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("System.Object", r1BaseExtension.ToTestDisplayString());
        Assert.True(r1BaseExtension.IsErrorType());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceNamedTypeSymbol>(r2, isExplicit: true);
        var r2BaseExtension = r2.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("R1", r2BaseExtension.ToTestDisplayString());
        Assert.False(r2BaseExtension.IsErrorType());
    }

    [Fact]
    public void ExtensionMarkerMethod_WithSelfReferentialSecondParameter()
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit: true)}}'(object '', valuetype R1 '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        // PROTOTYPE this test should be updated once we emit erase references to extensions (different metadata format)
        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        // PROTOTYPE consider a dedicated error message
        comp.VerifyDiagnostics(
            // (1,27): error CS0268: Imported type 'R1' is invalid. It contains a circular base type dependency.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_ImportedCircularBase, "R2").WithArguments("R1").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: true);
        var r1BaseExtension = r1.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("R1", r1BaseExtension.ToTestDisplayString());
        Assert.True(r1BaseExtension.IsErrorType());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
        var r2BaseExtension = r2.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("R1", r2BaseExtension.ToTestDisplayString());
        Assert.False(r2BaseExtension.IsErrorType());
    }

    [Fact]
    public void ExtensionMarkerMethod_WithDuplicateSecondAndThirdParameters()
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit: true)}}'(object '') cil managed
    {
        IL_0000: ret
    }
}

.class public sequential ansi sealed beforefieldinit R2
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit: true)}}'(object '', valuetype R1 '', valuetype R1 '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R3 for object : R2 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should we consider duplicate base extensions to be bad metadata?
        // PROTOTYPE this test should be updated once we emit erase references to extensions (different metadata format)
        comp.VerifyDiagnostics();

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: true);
        Assert.Empty(r1.BaseExtensionsNoUseSiteDiagnostics);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<PENamedTypeSymbol>(r2, isExplicit: true);
        Assert.Equal(new[] { "R1", "R1" }, r2.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.True(r2.BaseExtensionsNoUseSiteDiagnostics.All(b => !b.IsErrorType()));

        var r3 = comp.GlobalNamespace.GetTypeMember("R3");
        VerifyExtension<SourceExtensionTypeSymbol>(r3, isExplicit: true);
        var r3BaseExtensions = r3.BaseExtensionsNoUseSiteDiagnostics;
        Assert.Equal("R2", r3BaseExtensions.Single().ToTestDisplayString());
        Assert.False(r3BaseExtensions.Single().IsErrorType());
    }

    [Fact]
    public void ExtensionMarkerMethod_MissingIsByRefLikeAttribute()
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
        01 00 43 45 78 74 65 6e 73 69 6f 6e 20 74 79 70
        65 73 20 61 72 65 20 6e 6f 74 20 73 75 70 70 6f
        72 74 65 64 20 69 6e 20 74 68 69 73 20 76 65 72
        73 69 6f 6e 20 6f 66 20 79 6f 75 72 20 63 6f 6d
        70 69 6c 65 72 2e 01 00 00
    )

    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit: true)}}'(object '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,43): error CS0619: 'R1' is obsolete: 'Extension types are not supported in this version of your compiler.'
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "R1").WithArguments("R1", PEModule.ExtensionMarker).WithLocation(1, 43),
            // (1,43): error CS9307: A base extension must be an extension type.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "R1").WithLocation(1, 43)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyNotExtension<PENamedTypeSymbol>(r1);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Fact]
    public void ObsoleteExtensionMarker_OnMethod()
    {
        // public class C
        // {
        //     [Obsolete(ExtensionMarker)]
        //     public void M() { }
        // }
        var ilSource = """
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .method public hidebysig instance void M() cil managed
    {
        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
            01 00 43 45 78 74 65 6e 73 69 6f 6e 20 74 79 70
            65 73 20 61 72 65 20 6e 6f 74 20 73 75 70 70 6f
            72 74 65 64 20 69 6e 20 74 68 69 73 20 76 65 72
            73 69 6f 6e 20 6f 66 20 79 6f 75 72 20 63 6f 6d
            70 69 6c 65 72 2e 01 00 00
        )

        IL_0000: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}
""";

        var src = """
class C2 : C
{
    void M2()
    {
        M();
    }
}
""";

        var comp = CreateCompilationWithIL(src, ilSource);
        comp.VerifyDiagnostics(
            // (5,9): error CS0619: 'C.M()' is obsolete: 'Extension types are not supported in this version of your compiler.'
            //         M();
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "M()").WithArguments("C.M()", PEModule.ExtensionMarker).WithLocation(5, 9)
            );
    }

    [Fact]
    public void ObsoleteExtensionMarker_OnField()
    {
        // public class C
        // {
        //     [Obsolete(ExtensionMarker)]
        //     public int field;
        // }
        var ilSource = """
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .field public int32 'field'
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
        01 00 43 45 78 74 65 6e 73 69 6f 6e 20 74 79 70
        65 73 20 61 72 65 20 6e 6f 74 20 73 75 70 70 6f
        72 74 65 64 20 69 6e 20 74 68 69 73 20 76 65 72
        73 69 6f 6e 20 6f 66 20 79 6f 75 72 20 63 6f 6d
        70 69 6c 65 72 2e 01 00 00
    )

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}
""";

        var src = """
class C2 : C
{
    void M2()
    {
        _ = field;
    }
}
""";

        var comp = CreateCompilationWithIL(src, ilSource);
        comp.VerifyDiagnostics(
            // (5,13): error CS0619: 'C.field' is obsolete: 'Extension type are not supported in this version of your compiler.'
            //         _ = field;
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "field").WithArguments("C.field", PEModule.ExtensionMarker).WithLocation(5, 13)
            );
    }

    [Fact]
    public void ObsoleteExtensionMarker_OnProperty()
    {
        // public class C
        // {
        //     [Obsolete(ExtensionMarker)]
        //     public int Property => 0;
        // }
        var ilSource = """
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .method public hidebysig specialname instance int32 get_Property() cil managed
    {
        IL_0000: ldc.i4.0
        IL_0001: ret
    }

    .property instance int32 Property()
    {
        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
            01 00 43 45 78 74 65 6e 73 69 6f 6e 20 74 79 70
            65 73 20 61 72 65 20 6e 6f 74 20 73 75 70 70 6f
            72 74 65 64 20 69 6e 20 74 68 69 73 20 76 65 72
            73 69 6f 6e 20 6f 66 20 79 6f 75 72 20 63 6f 6d
            70 69 6c 65 72 2e 01 00 00
        )

        .get instance int32 C::get_Property()
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}
""";

        var src = """
class C2 : C
{
    void M2()
    {
        _ = Property;
    }
}
""";

        var comp = CreateCompilationWithIL(src, ilSource);
        comp.VerifyDiagnostics(
            // (5,13): error CS0619: 'C.Property' is obsolete: 'Extension type are not supported in this version of your compiler.'
            //         _ = Property;
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Property").WithArguments("C.Property", PEModule.ExtensionMarker).WithLocation(5, 13)
            );
    }

    [Fact]
    public void ObsoleteExtensionMarker_OnEvent()
    {
        // public class C
        // {
        //     [Obsolete(ExtensionMarker)]
        //     public event System.Action Event { add => throw null; remove => throw null; }
        // }
        var ilSource = """
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .method public hidebysig specialname instance void add_Event(class [mscorlib]System.Action 'value') cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname instance void remove_Event(class [mscorlib]System.Action 'value') cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .event [mscorlib]System.Action Event
    {
        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
            01 00 43 45 78 74 65 6e 73 69 6f 6e 20 74 79 70
            65 73 20 61 72 65 20 6e 6f 74 20 73 75 70 70 6f
            72 74 65 64 20 69 6e 20 74 68 69 73 20 76 65 72
            73 69 6f 6e 20 6f 66 20 79 6f 75 72 20 63 6f 6d
            70 69 6c 65 72 2e 01 00 00
        )

        .addon instance void C::add_Event(class [mscorlib]System.Action)
        .removeon instance void C::remove_Event(class [mscorlib]System.Action)
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}
""";

        var src = """
class C2 : C
{
    void M2()
    {
        Event += null;
    }
}
""";

        var comp = CreateCompilationWithIL(src, ilSource);
        comp.VerifyDiagnostics(
            // (5,9): error CS0619: 'C.Event' is obsolete: 'Extension type are not supported in this version of your compiler.'
            //         Event += null;
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Event").WithArguments("C.Event", PEModule.ExtensionMarker).WithLocation(5, 9)
            );
    }

    [Fact]
    public void ObsoleteExtensionMarker_WrongString()
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = ( 01 00 02 68 69 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit: true)}}'(object '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,43): warning CS0618: 'R1' is obsolete: 'hi'
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "R1").WithArguments("R1", "hi").WithLocation(1, 43)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: true);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_NotPrivate(bool isExplicit)
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method public hidebysig static void '{{ExtensionMarkerName(isExplicit)}}'(object '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should have a use-site error too
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R2' extends 'object' but base extension 'R1' extends '?'.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R2").WithArguments("R2", "object", "R1", "?").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: isExplicit);
        Assert.True(r1.ExtendedTypeNoUseSiteDiagnostics.IsErrorType());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_NotStatic(bool isExplicit)
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig void '{{ExtensionMarkerName(isExplicit)}}'(object '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should have a use-site error too
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R2' extends 'object' but base extension 'R1' extends '?'.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R2").WithArguments("R2", "object", "R1", "?").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: isExplicit);
        Assert.True(r1.ExtendedTypeNoUseSiteDiagnostics.IsErrorType());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Fact]
    public void ExtensionMarkerMethod_NotHideBySig()
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private static void '{{ExtensionMarkerName(isExplicit: true)}}'(object '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: true);
        Assert.False(r1.ExtendedTypeNoUseSiteDiagnostics.IsErrorType());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_NoParameters(bool isExplicit)
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit)}}'() cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should have a use-site error too
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R2' extends 'object' but base extension 'R1' extends '?'.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R2").WithArguments("R2", "object", "R1", "?").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: isExplicit);
        Assert.True(r1.ExtendedTypeNoUseSiteDiagnostics.IsErrorType());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Theory, CombinatorialData]
    public void ExtensionMarkerMethod_NotVoidReturn(bool isExplicit)
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static int32 '{{ExtensionMarkerName(isExplicit)}}'(object '') cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R2' extends 'object' but base extension 'R1' extends '?'.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R2").WithArguments("R2", "object", "R1", "?").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: isExplicit);
        Assert.True(r1.ExtendedTypeNoUseSiteDiagnostics.IsErrorType());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Fact]
    public void ExtensionMarkerMethod_GenericMethod()
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit: true)}}'<T>(object '') cil managed
    {
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should have a use-site error too
        comp.VerifyDiagnostics(
            // (1,27): error CS9316: Extension 'R2' extends 'object' but base extension 'R1' extends '?'.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R2").WithArguments("R2", "object", "R1", "?").WithLocation(1, 27)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyExtension<PENamedTypeSymbol>(r1, isExplicit: true);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        VerifyExtension<SourceExtensionTypeSymbol>(r2, isExplicit: true);
    }

    [Fact]
    public void BaseExtension_Dynamic_Nested()
    {
        // PROTOTYPE type references to extensions should be emitted with erasure
        var src1 = """
public explicit extension R1<T> for object { }
public explicit extension R2 for object : R1<dynamic> { }
""";

        var comp = CreateCompilation(src1, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        static void validate(ModuleSymbol module)
        {
            var r2 = module.GlobalNamespace.GetTypeMember("R2");
            Assert.Equal("R1<dynamic>", r2.BaseExtensionsNoUseSiteDiagnostics.Single().ToTestDisplayString());
            Assert.Equal("R1<dynamic>", r2.AllBaseExtensionsNoUseSiteDiagnostics.Single().ToTestDisplayString());
        }
    }

    [Fact]
    public void BaseExtension_Dynamic_Nested_MissingDynamicAttribute()
    {
        var src = """
explicit extension R1<T> for object { }
explicit extension R2 for object : R1<dynamic> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DynamicAttribute);
        comp.VerifyDiagnostics(
            // (2,39): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
            // explicit extension R2 for object : R1<dynamic> { }
            Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(2, 39)
            );
    }

    [Fact]
    public void BaseExtension_Tuple_Nested()
    {
        // PROTOTYPE type references to extensions should be emitted with erasure
        var src1 = """
public explicit extension R1<T> for object { }
public explicit extension R2 for object : R1<(int a, int b)> { }
""";

        var comp = CreateCompilation(src1, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        static void validate(ModuleSymbol module)
        {
            var r2 = module.GlobalNamespace.GetTypeMember("R2");
            Assert.Equal("R1<(System.Int32 a, System.Int32 b)>", r2.BaseExtensionsNoUseSiteDiagnostics.Single().ToTestDisplayString());
            Assert.Equal("R1<(System.Int32 a, System.Int32 b)>", r2.AllBaseExtensionsNoUseSiteDiagnostics.Single().ToTestDisplayString());
        }
    }

    [Fact]
    public void BaseExtension_Nullability_Nested()
    {
        // PROTOTYPE type references to extensions should be emitted with erasure
        var src1 = """
#nullable enable
public explicit extension R1<T> for object { }
public explicit extension R2 for object : R1<object?> { }
""";

        var comp = CreateCompilation(src1, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        static void validate(ModuleSymbol module)
        {
            var r2 = module.GlobalNamespace.GetTypeMember("R2");
            Assert.Equal("R1<System.Object?>", r2.BaseExtensionsNoUseSiteDiagnostics.Single().ToTestDisplayString());
        }
    }

    [Fact]
    public void BaseExtension_Nullability_Nested_MissingNullableAttribute()
    {
        var lib_cs = """
public explicit extension R1<T> for object { }
""";
        var libComp = CreateCompilation(lib_cs, targetFramework: TargetFramework.Net70);
        libComp.VerifyDiagnostics();

        var src = """
#nullable enable
public explicit extension R2 for object : R1<object?> { }
""";

        var comp = CreateCompilation(src, references: new[] { libComp.EmitToImageReference() }, targetFramework: TargetFramework.Net70);
        comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_NullableAttribute);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        static void validate(ModuleSymbol module)
        {
            bool inSource = module is SourceModuleSymbol;

            var r2 = module.GlobalNamespace.GetTypeMember("R2");
            Assert.Equal("R1<System.Object?>", r2.BaseExtensionsNoUseSiteDiagnostics.Single().ToTestDisplayString());

            var nullableAttribute = module.ContainingAssembly.GetTypeByMetadataName("System.Runtime.CompilerServices.NullableAttribute");
            Assert.Equal(inSource, nullableAttribute is null);
        }
    }

    [Fact]
    public void BaseExtension_Nullability_Nested_MissingNullableContextAttribute()
    {
        var src1 = """
#nullable enable
public explicit extension R1<T> for object { }
public explicit extension R2 for object : R1<object?> { }
""";

        var comp = CreateCompilation(src1, targetFramework: TargetFramework.Net70);
        comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_NullableContextAttribute);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);
        return;

        static void validate(ModuleSymbol module)
        {
            bool inSource = module is SourceModuleSymbol;

            var r2 = module.GlobalNamespace.GetTypeMember("R2");
            Assert.Equal("R1<System.Object?>", r2.BaseExtensionsNoUseSiteDiagnostics.Single().ToTestDisplayString());

            var nullableContextAttribute = module.ContainingAssembly.GetTypeByMetadataName("System.Runtime.CompilerServices.NullableContextAttribute");
            Assert.Equal(inSource, nullableContextAttribute is null);
        }
    }

    [Fact]
    public void GenerateNullableContextAttribute()
    {
        var source = @"
public explicit extension R1 for object
{
#nullable enable
    private object M1() => null!;
    private object M2() => null!;
    private object M3() => null!;
}";
        var comp = CreateCompilation(source, assemblyName: "comp", targetFramework: TargetFramework.Net70);
        comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_NullableContextAttribute);

        CompileAndVerify(comp, verify: Verification.FailsPEVerify, symbolValidator: module =>
        {
            var attributeType = module.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Runtime.CompilerServices.NullableContextAttribute");
            Assert.NotNull(attributeType);
            Assert.Equal("comp", attributeType.ContainingAssembly.Name);
            AttributeUsageInfo attributeUsage = attributeType.GetAttributeUsageInfo();
            Assert.False(attributeUsage.Inherited);
            Assert.False(attributeUsage.AllowMultiple);
            Assert.True(attributeUsage.HasValidAttributeTargets);
        });
    }

    [Fact]
    public void BaseExtension_NativeInteger_Nested()
    {
        // PROTOTYPE type references to extensions should be emitted with erasure
        var src1 = """
public explicit extension R1<T> for object { }
public explicit extension R2 for object : R1<nint> { }
""";

        var comp = CreateCompilation(new[] { src1, CompilerFeatureRequiredAttribute });
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate);
        return;

        static void validate(ModuleSymbol module)
        {
            bool inSource = module is SourceModuleSymbol;

            var r2 = module.GlobalNamespace.GetTypeMember("R2");
            Assert.Equal("R1<nint>", r2.BaseExtensionsNoUseSiteDiagnostics.Single().ToTestDisplayString());

            var nativeIntegerAttribute = module.ContainingAssembly.GetTypeByMetadataName("System.Runtime.CompilerServices.NativeIntegerAttribute");
            Assert.Equal(inSource, nativeIntegerAttribute is null);
        }
    }

    [Fact]
    public void BadMember_ExtensionMethod()
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00)
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit: true)}}'(object '') cil managed
    {
        IL_0000: ret
    }
    .method public hidebysig static void M(object '') cil managed
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        IL_0000: ret
    }
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
static class OtherExtension
{
    public static void M(this object o) { }
}
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var m = comp.GlobalNamespace.GetTypeMember("R1").GetMethod("M");
        Assert.False(m.IsExtensionMethod);
    }

    [Fact]
    public void BadMember_InstanceField()
    {
        var ilSource = $$"""
.class public sequential ansi sealed beforefieldinit R1
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00)
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
        01 00 43 45 78 74 65 6e 73 69 6f 6e 20 74 79 70
        65 73 20 61 72 65 20 6e 6f 74 20 73 75 70 70 6f
        72 74 65 64 20 69 6e 20 74 68 69 73 20 76 65 72
        73 69 6f 6e 20 6f 66 20 79 6f 75 72 20 63 6f 6d
        70 69 6c 65 72 2e 01 00 00
    )

    .method private hidebysig static void '{{ExtensionMarkerName(isExplicit: true)}}'(object '') cil managed
    {
        IL_0000: ret
    }
    .field public int32 'field'
}
""";

        var src = """
public explicit extension R2 for object : R1 { }
""";

        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,43): error CS0619: 'R1' is obsolete: 'Extension type are not supported in this version of your compiler.'
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "R1").WithArguments("R1", PEModule.ExtensionMarker).WithLocation(1, 43),
            // (1,43): error CS9307: A base extension must be an extension type.
            // public explicit extension R2 for object : R1 { }
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "R1").WithLocation(1, 43)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        VerifyNotExtension<PENamedTypeSymbol>(r1);
    }

    [Fact]
    public void ExtensionMarkerMethodHiddenInMetadata()
    {
        var src = """
public explicit extension R for object { }
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var comp2 = CreateCompilation("", references: new[] { comp.EmitToImageReference() },
            options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

        Assert.Empty(comp2.GlobalNamespace.GetTypeMember("R").GetMembers());
    }

    [Fact]
    public void ObsoleteOnType()
    {
        var src1 = """
[System.Obsolete("message", true)]
public explicit extension R for object { }
""";

        var comp = CreateCompilation(src1, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,2): error CS0592: Attribute 'System.Obsolete' is not valid on this declaration type. It is only valid on 'class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate' declarations.
            // [System.Obsolete("message")]
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "System.Obsolete").WithArguments("System.Obsolete", "class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate").WithLocation(1, 2)
            );
        // PROTOTYPE revisit once attributes are allowed on extension types.
        // The Obsolete poison attribute should not be emitted when the user marked the type as obsolete.
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void MemberLookup()
    {
        var src = """
E.M();
_ = E.Property;

public explicit extension E for object
{
    public static void M()
    {
        System.Console.Write("Method ");
    }

    public static int Property
    {
        get
        {
            System.Console.Write("Property");
            return 0;
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, expectedOutput: "Method Property",
            symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        Assert.Equal("E.M()", invocation.ToString());
        Assert.Equal("void E.M()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());

        var property = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Skip(1).First();
        Assert.Equal("E.Property", property.ToString());
        Assert.Equal("System.Int32 E.Property { get; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());

        void validate(ModuleSymbol module)
        {
            bool inSource = module is SourceModuleSymbol;
            var e = module.GlobalNamespace.GetTypeMember("E");
            Assert.Empty(e.BaseExtensionsNoUseSiteDiagnostics);
            Assert.Empty(e.AllBaseExtensionsNoUseSiteDiagnostics);
        }
    }

    [ConditionalFact(typeof(CoreClrOnly), AlwaysSkip = "fails in emit stage in used assemblies leg")]
    public void MemberLookup_Instance()
    {
        var src = """
void M(E e)
{
    e.M();
    _ = e.Property;
}

public explicit extension E for object
{
    public void M()
    {
        System.Console.Write("Method ");
    }

    public int Property
    {
        get
        {
            System.Console.Write("Property");
            return 0;
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,6): warning CS8321: The local function 'M' is declared but never used
            // void M(E e)
            Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "M").WithArguments("M").WithLocation(1, 6)
            );
        // PROTOTYPE execute once instance invocation is implemented

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        Assert.Equal("e.M()", invocation.ToString());
        Assert.Equal("void E.M()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());

        var property = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Skip(1).First();
        Assert.Equal("e.Property", property.ToString());
        Assert.Equal("System.Int32 E.Property { get; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void MemberLookup_BaseExtension()
    {
        var src = """
E.M();
_ = E.Property;

public explicit extension Base for object
{
    public static void M()
    {
        System.Console.Write("Method ");
    }

    public static int Property
    {
        get
        {
            System.Console.Write("Property");
            return 0;
        }
    }
}

public explicit extension E for object : Base { }
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, expectedOutput: "Method Property",
            symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        Assert.Equal("E.M()", invocation.ToString());
        Assert.Equal("void Base.M()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());

        var property = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Skip(1).First();
        Assert.Equal("E.Property", property.ToString());
        Assert.Equal("System.Int32 Base.Property { get; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());

        static void validate(ModuleSymbol module)
        {
            bool inSource = module is SourceModuleSymbol;
            var e = module.GlobalNamespace.GetTypeMember("E");
            AssertEx.Equal(new[] { "Base" }, e.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
            AssertEx.Equal(new[] { "Base" }, e.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        }
    }

    [ConditionalFact(typeof(CoreClrOnly), AlwaysSkip = "fails in emit stage in used assemblies leg")]
    public void MemberLookup_BaseExtension_Instance()
    {
        var src = """
#pragma warning disable CS8321 // unused local function
void M(E e)
{
    e.M();
    _ = e.Property;
}

public explicit extension Base for object
{
    public void M()
    {
        System.Console.Write("Method ");
    }

    public int Property
    {
        get
        {
            System.Console.Write("Property");
            return 0;
        }
    }
}

public explicit extension E for object : Base { }
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        // PROTOTYPE execute once instance invocation is implemented

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        Assert.Equal("e.M()", invocation.ToString());
        Assert.Equal("void Base.M()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());

        var property = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Skip(1).First();
        Assert.Equal("e.Property", property.ToString());
        Assert.Equal("System.Int32 Base.Property { get; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void MemberLookup_BaseExtension_DifferentArities()
    {
        var src = """
E.M<int>();

public explicit extension Base for object
{
    public static void M<T>()
    {
        System.Console.Write("Method ");
    }
    public static void M() => throw null;
}

public explicit extension E for object : Base
{
    public static void M() => throw null;
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, expectedOutput: "Method", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        Assert.Equal("E.M<int>()", invocation.ToString());
        Assert.Equal("void Base.M<System.Int32>()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void MemberLookup_BaseExtension_Ambiguous()
    {
        var src = """
E.M();
_ = E.Property;

public explicit extension Base1 for object
{
    public static void M() => throw null;
    public static int Property => throw null;
}

public explicit extension Base2 for object
{
    public static void M() => throw null;
    public static int Property => throw null;
}

public explicit extension E for object : Base1, Base2 { }
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
             // (1,3): error CS0121: The call is ambiguous between the following methods or properties: 'Base1.M()' and 'Base2.M()'
             // E.M();
             Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Base1.M()", "Base2.M()").WithLocation(1, 3),
             // (2,7): error CS0229: Ambiguity between 'Base1.Property' and 'Base2.Property'
             // _ = E.Property;
             Diagnostic(ErrorCode.ERR_AmbigMember, "Property").WithArguments("Base1.Property", "Base2.Property").WithLocation(2, 7)
             );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        Assert.Equal("E.M()", invocation.ToString());
        Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        Assert.Equal(new[] { "void Base1.M()", "void Base2.M()" }, model.GetSymbolInfo(invocation).CandidateSymbols.ToTestDisplayStrings());

        var property = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Skip(1).First();
        Assert.Equal("E.Property", property.ToString());
        Assert.Null(model.GetSymbolInfo(property).Symbol);
        Assert.Equal(new[] { "System.Int32 Base1.Property { get; }", "System.Int32 Base2.Property { get; }" },
            model.GetSymbolInfo(property).CandidateSymbols.ToTestDisplayStrings());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void MemberLookup_NestedType()
    {
        var src = """
public explicit extension E2 for object : E.Nested, E.BaseNested, E.HidingNested { }

public explicit extension Base for object
{
    public explicit extension BaseNested for object { }
    public explicit extension HidingNested for object { }
}

public explicit extension E for object : Base
{
    public explicit extension Nested for object { }
    public explicit extension HidingNested for object { }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var e2 = comp.GlobalNamespace.GetTypeMember("E2");
        AssertEx.Equal(new[] { "E.Nested", "Base.BaseNested", "E.HidingNested" },
            e2.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void MemberLookup_NestedType_DifferentArities()
    {
        var src = """
public explicit extension E2 for object : E.Nested, E.Nested<int> { }

public explicit extension Base for object
{
    public explicit extension Nested<T> for object { }
    public explicit extension Nested for object { }
}

public explicit extension E for object : Base
{
    public explicit extension Nested for object { }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var e2 = comp.GlobalNamespace.GetTypeMember("E2");
        AssertEx.Equal(new[] { "E.Nested", "Base.Nested<System.Int32>" },
            e2.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void MemberLookup_NestedType_Ambiguous()
    {
        var src = """
public explicit extension E2 for object : E.Ambiguous { }

public explicit extension Base1 for object
{
    public explicit extension Ambiguous for object { }
}

public explicit extension Base2 for object
{
    public explicit extension Ambiguous for object { }
}

public explicit extension E for object : Base1, Base2
{
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,45): error CS0104: 'Ambiguous' is an ambiguous reference between 'Base1.Ambiguous' and 'Base2.Ambiguous'
            // public explicit extension E2 for object : E.Ambiguous { }
            Diagnostic(ErrorCode.ERR_AmbigContext, "Ambiguous").WithArguments("Ambiguous", "Base1.Ambiguous", "Base2.Ambiguous").WithLocation(1, 45)
            );

        var e2 = comp.GlobalNamespace.GetTypeMember("E2");
        AssertEx.Equal(new[] { "Base1.Ambiguous" }, e2.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void MemberLookup_BaseExtension_Hiding()
    {
        var src = """
E.M();
_ = E.Property;

public explicit extension Base for object
{
    public static void M() => throw null;
    public static int Property => throw null;
}

public explicit extension E for object : Base
{
    public static void M()
    {
        System.Console.Write("Method ");
    }

    public static int Property
    {
        get
        {
            System.Console.Write("Property");
            return 0;
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, expectedOutput: "Method Property",
            symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        Assert.Equal("E.M()", invocation.ToString());
        Assert.Equal("void E.M()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());

        var property = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Skip(1).First();
        Assert.Equal("E.Property", property.ToString());
        Assert.Equal("System.Int32 E.Property { get; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());

        static void validate(ModuleSymbol module)
        {
            bool inSource = module is SourceModuleSymbol;
            var e = module.GlobalNamespace.GetTypeMember("E");
            AssertEx.Equal(new[] { "Base" }, e.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
            AssertEx.Equal(new[] { "Base" }, e.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        }
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void MemberLookup_BaseExtension_MethodHidesProperty()
    {
        var src = """
E.Member();

var m = E.Member;
m();

public explicit extension Base for object
{
    public static int Member => throw null;
}

public explicit extension E for object : Base
{
    public static void Member()
    {
        System.Console.Write("Member ");
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, expectedOutput: "Member Member",
            symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        Assert.Equal("E.Member()", invocation.ToString());
        Assert.Equal("void E.Member()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());

        var property = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Skip(1).First();
        Assert.Equal("E.Member", property.ToString());
        Assert.Equal("void E.Member()", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());

        static void validate(ModuleSymbol module)
        {
            bool inSource = module is SourceModuleSymbol;
            var e = module.GlobalNamespace.GetTypeMember("E");
            AssertEx.Equal(new[] { "Base" }, e.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
            AssertEx.Equal(new[] { "Base" }, e.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        }
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void MemberLookup_MultipleCandidates()
    {
        var src = """
E.M((int)0);
E.M("hi");
E.M((long)0);

public explicit extension Base for object
{
    public static void M(int i)
    {
        System.Console.Write("Method(int) ");
    }
    public static void M(string i)
    {
        System.Console.Write("Method(string) ");
    }
}

public explicit extension E for object : Base
{
    public static void M(long l)
    {
        System.Console.Write("Method(long) ");
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, expectedOutput: "Method(long) Method(string) Method(long)", verify: Verification.FailsPEVerify);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void MemberLookup_BaseExtension_Diamond()
    {
        var src = """
_ = E2.Prop;
_ = E4.Prop;

public explicit extension E1 for object
{
    public static int Prop => throw null;
}

public explicit extension E2 for object : E1
{
    public static long Prop
    {
        get
        {
            System.Console.Write("E2.Prop ");
            return 42;
        }
    }
}

public explicit extension E3 for object : E1 { }

public explicit extension E4 for object : E2, E3 { }
""";

        // PROTOTYPE should warn about hiding
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, expectedOutput: "E2.Prop E2.Prop", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var property = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().First();
        Assert.Equal("E2.Prop", property.ToString());
        Assert.Equal("System.Int64 E2.Prop { get; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());

        var property2 = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Skip(1).First();
        Assert.Equal("E4.Prop", property2.ToString());
        Assert.Equal("System.Int64 E2.Prop { get; }", model.GetSymbolInfo(property2).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(ClrOnly))]
    public void MemberLookup_BaseExtension_Inaccessible()
    {
        var src = """
E.M();
_ = E.Property;

public explicit extension Base for object
{
    private static void M() => throw null;
    private static int Property => throw null;
}

public explicit extension E for object : Base
{
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,3): error CS0122: 'Base.M()' is inaccessible due to its protection level
            // E.M();
            Diagnostic(ErrorCode.ERR_BadAccess, "M").WithArguments("Base.M()").WithLocation(1, 3),
            // (2,7): error CS0122: 'Base.Property' is inaccessible due to its protection level
            // _ = E.Property;
            Diagnostic(ErrorCode.ERR_BadAccess, "Property").WithArguments("Base.Property").WithLocation(2, 7)
            );
    }

    [ConditionalFact(typeof(ClrOnly))]
    public void MemberLookup_BaseExtension_Circular()
    {
        var src = """
E2.M();
_ = E2.Property;

public explicit extension E1 for object : E2
{
    public static void M() { }
    public static int Property => 0;
}

public explicit extension E2 for object : E1 { }
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,4): error CS0117: 'E2' does not contain a definition for 'M'
            // E2.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("E2", "M").WithLocation(1, 4),
            // (2,8): error CS0117: 'E2' does not contain a definition for 'Property'
            // _ = E2.Property;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("E2", "Property").WithLocation(2, 8),
            // (4,27): error CS9311: Base extension 'E2' causes a cycle in the extension hierarchy of 'E1'.
            // public explicit extension E1 for object : E2
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "E1").WithArguments("E1", "E2").WithLocation(4, 27),
            // (10,27): error CS9311: Base extension 'E1' causes a cycle in the extension hierarchy of 'E2'.
            // public explicit extension E2 for object : E1 { }
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "E2").WithArguments("E2", "E1").WithLocation(10, 27)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        Assert.Equal("E2.M()", invocation.ToString());
        Assert.Null(model.GetSymbolInfo(invocation).Symbol);

        var property = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Skip(1).First();
        Assert.Equal("E2.Property", property.ToString());
        Assert.Null(model.GetSymbolInfo(property).Symbol);
    }

    [ConditionalTheory(typeof(CoreClrOnly))]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void MemberLookup_GrandBaseExtension(int offset)
    {
        var src = $$"""
E.M();
_ = E.Property;

public explicit extension GrandBase{{offset}} for object
{
    public static void M()
    {
        System.Console.Write("Method ");
    }

    public static int Property
    {
        get
        {
            System.Console.Write("Property");
            return 0;
        }
    }
}
public explicit extension GrandBase{{(offset + 1) % 4}} for object
{
}
public explicit extension GrandBase{{(offset + 2) % 4}} for object
{
}
public explicit extension GrandBase{{(offset + 3) % 4}} for object
{
}

public explicit extension Base1 for object : GrandBase0, GrandBase1
{
}
public explicit extension Base2 for object : GrandBase2, GrandBase3
{
}

public explicit extension E for object : Base1, Base2 { }
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, expectedOutput: "Method Property",
            symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        Assert.Equal("E.M()", invocation.ToString());
        Assert.Equal($$"""void GrandBase{{offset}}.M()""", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());

        var property = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Skip(1).First();
        Assert.Equal("E.Property", property.ToString());
        Assert.Equal($$"""System.Int32 GrandBase{{offset}}.Property { get; }""", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());

        static void validate(ModuleSymbol module)
        {
            bool inSource = module is SourceModuleSymbol;
            var e = module.GlobalNamespace.GetTypeMember("E");
            AssertEx.Equal(new[] { "Base1", "Base2" }, e.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());

            AssertEx.Equal(new[] { "Base1", "GrandBase0", "GrandBase1", "Base2", "GrandBase2", "GrandBase3" },
                e.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        }
    }

    [ConditionalFact(typeof(ClrOnly))]
    public void MemberLookup_BaseExtension_CircularityAcrossNestedExtensions()
    {
        var src = """
D.E.M();
_ = D.E.Property;

public explicit extension C for object : D.E
{
    public explicit extension Base for object
    {
        public static void M()
        {
            System.Console.Write("Method ");
        }

        public static int Property
        {
            get
            {
                System.Console.Write("Property");
                return 0;
            }
        }
    }
}

public explicit extension D for object : C.Base
{
    public explicit extension E for object : C.Base { }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,5): error CS0117: 'D.E' does not contain a definition for 'M'
            // D.E.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("D.E", "M").WithLocation(1, 5),
            // (2,9): error CS0117: 'D.E' does not contain a definition for 'Property'
            // _ = D.E.Property;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("D.E", "Property").WithLocation(2, 9),
            // (4,27): error CS9311: Base extension 'D.E' causes a cycle in the extension hierarchy of 'C'.
            // public explicit extension C for object : D.E
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "C").WithArguments("C", "D.E").WithLocation(4, 27),
            // (24,27): error CS9311: Base extension 'C.Base' causes a cycle in the extension hierarchy of 'D'.
            // public explicit extension D for object : C.Base
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "D").WithArguments("D", "C.Base").WithLocation(24, 27),
            // (26,31): error CS9311: Base extension 'C.Base' causes a cycle in the extension hierarchy of 'D.E'.
            //     public explicit extension E for object : C.Base { }
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "E").WithArguments("D.E", "C.Base").WithLocation(26, 31)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        Assert.Equal("D.E.M()", invocation.ToString());
        Assert.Null(model.GetSymbolInfo(invocation).Symbol);

        var property = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Skip(2).First();
        Assert.Equal("D.E.Property", property.ToString());
        Assert.Null(model.GetSymbolInfo(property).Symbol);
    }

    [ConditionalFact(typeof(ClrOnly))]
    public void MemberLookup_BaseExtension_CircularityAcrossNestedExtensions_MissingBase()
    {
        var src1 = """
public explicit extension A for object { }
""";
        var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net70, assemblyName: "missing");

        var src2 = """
public explicit extension B for object : A { }
""";
        var comp2 = CreateCompilation(src2, targetFramework: TargetFramework.Net70,
            references: new[] { comp1.ToMetadataReference() });

        var src3 = """
D.E.M();
_ = D.E.Property;

public explicit extension C for object : D.E, B
{
    public explicit extension Base for object
    {
        public static void M()
        {
            System.Console.Write("Method ");
        }

        public static int Property
        {
            get
            {
                System.Console.Write("Property");
                return 0;
            }
        }
    }
}

public explicit extension D for object : C.Base
{
    public explicit extension E for object : C.Base { }
}
""";

        var comp3 = CreateCompilation(src3, targetFramework: TargetFramework.Net70,
            references: new[] { comp2.ToMetadataReference() });

        comp3.VerifyDiagnostics(
            // (1,5): error CS0117: 'D.E' does not contain a definition for 'M'
            // D.E.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("D.E", "M").WithLocation(1, 5),
            // (2,9): error CS0117: 'D.E' does not contain a definition for 'Property'
            // _ = D.E.Property;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("D.E", "Property").WithLocation(2, 9),
            // (4,27): error CS9311: Base extension 'D.E' causes a cycle in the extension hierarchy of 'C'.
            // public explicit extension C for object : D.E, B
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "C").WithArguments("C", "D.E").WithLocation(4, 27),
            // (4,27): error CS8090: There is an error in a referenced assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // public explicit extension C for object : D.E, B
            Diagnostic(ErrorCode.ERR_ErrorInReferencedAssembly, "C").WithArguments("missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(4, 27),
            // (10,13): error CS8090: There is an error in a referenced assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //             System.Console.Write("Method ");
            Diagnostic(ErrorCode.ERR_ErrorInReferencedAssembly, "System").WithArguments("missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(10, 13),
            // (17,17): error CS8090: There is an error in a referenced assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //                 System.Console.Write("Property");
            Diagnostic(ErrorCode.ERR_ErrorInReferencedAssembly, "System").WithArguments("missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(17, 17),
            // (24,27): error CS9311: Base extension 'C.Base' causes a cycle in the extension hierarchy of 'D'.
            // public explicit extension D for object : C.Base
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "D").WithArguments("D", "C.Base").WithLocation(24, 27),
            // (26,31): error CS9311: Base extension 'C.Base' causes a cycle in the extension hierarchy of 'D.E'.
            //     public explicit extension E for object : C.Base { }
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "E").WithArguments("D.E", "C.Base").WithLocation(26, 31)
            );

        var tree = comp3.SyntaxTrees.Single();
        var model = comp3.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        Assert.Equal("D.E.M()", invocation.ToString());
        Assert.Null(model.GetSymbolInfo(invocation).Symbol);

        var property = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Skip(2).First();
        Assert.Equal("D.E.Property", property.ToString());
        Assert.Null(model.GetSymbolInfo(property).Symbol);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void MemberLookup_WithTypeArgumentInvolvingMissingBase()
    {
        var src1 = """
public explicit extension Missing for object { }
""";
        var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net70, assemblyName: "missing");

        var src2 = """
public explicit extension B for object : Missing
{
    public class C { }
}
""";
        var comp2 = CreateCompilation(src2, targetFramework: TargetFramework.Net70,
            references: new[] { comp1.ToMetadataReference() });

        var src3 = """
E1.Method();

public explicit extension E2<T> for object
{
}
public explicit extension E1 for object : E2<B.C>
{
    public static void Method()
    {
        System.Console.Write("Method");
    }
}
""";

        var comp3 = CreateCompilation(src3, targetFramework: TargetFramework.Net70,
            references: new[] { comp2.ToMetadataReference() });

        // PROTOTYPE should we have a diagnostic for using B (whose base extension is missing)?
        comp3.VerifyDiagnostics();
        CompileAndVerify(comp3, expectedOutput: "Method");
    }

    [Fact]
    public void RecursiveExtensionLookup()
    {
        var src = """
explicit extension A<T> for object { }
explicit extension B for object : A<B.Garbage> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,39): error CS0426: The type name 'Garbage' does not exist in the type 'B'
            // explicit extension B for object : A<B.Garbage> { }
            Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "Garbage").WithArguments("Garbage", "B").WithLocation(2, 39)
            );
    }

    [Fact]
    public void UseSiteErrorReporting_MissingGrandBase()
    {
        var source1 = """
public explicit extension A for object { }
""";
        var compilation1 = CreateCompilation(source1, assemblyName: "missing",
            targetFramework: TargetFramework.Net70);

        compilation1.VerifyDiagnostics();

        var source2 = """
public explicit extension B for object : A { }
public explicit extension C<T> for object : B { }
""";
        var compilation2 = CreateCompilation(source2, references: new[] { compilation1.ToMetadataReference() },
            targetFramework: TargetFramework.Net70);

        compilation2.VerifyDiagnostics();

        var source3 = """
D.M();

public explicit extension D for object : C<string>
{
    public static void M() { }
}
""";
        var compilation3 = CreateCompilation(source3, references: new[] { compilation2.ToMetadataReference() },
            targetFramework: TargetFramework.Net70);

        compilation3.VerifyDiagnostics(
            // (1,3): error CS8090: There is an error in a referenced assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // D.M();
            Diagnostic(ErrorCode.ERR_ErrorInReferencedAssembly, "M").WithArguments("missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 3),
            // (3,27): error CS8090: There is an error in a referenced assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // public explicit extension D for object : C<string>
            Diagnostic(ErrorCode.ERR_ErrorInReferencedAssembly, "D").WithArguments("missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(3, 27)
            );
    }

    [Fact]
    public void MemberLookup_MethodsFromObject()
    {
        var src = """
#pragma warning disable CS8321 // unused local function
void M(A a)
{
    A.ToString();
    a.ToString();
}

public explicit extension A for object { }
""";
        // PROTOTYPE methods from System.Object will come from member lookup (once updated to consider the underlying type part of the base types)
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,7): error CS0117: 'A' does not contain a definition for 'ToString'
            //     A.ToString();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "ToString").WithArguments("A", "ToString").WithLocation(4, 7),
            // (5,7): error CS1061: 'A' does not contain a definition for 'ToString' and no accessible extension method 'ToString' accepting a first argument of type 'A' could be found (are you missing a using directive or an assembly reference?)
            //     a.ToString();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "ToString").WithArguments("A", "ToString").WithLocation(5, 7)
            );
    }

    [Fact]
    public void MemberLookup_InUsing()
    {
        var src = """
explicit extension Base for object
{
    public class BaseMember { }
    public class HidingMember { }
}

explicit extension E for object : Base
{
    public class Member { }
    public class HidingMember { }
}

namespace N
{
#pragma warning disable CS8019 // unnecessary using
    using Alias1 = E.Member;
    using Alias2 = E.BaseMember;
    using Alias3 = E.HidingMember;
}
""";
        // PROTOTYPE should warn for hiding
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var alias1 = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().First();
        Assert.Equal("Alias1=E.Member", model.GetDeclaredSymbol(alias1).ToTestDisplayString());

        var alias2 = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Skip(1).First();
        Assert.Equal("Alias2=Base.BaseMember", model.GetDeclaredSymbol(alias2).ToTestDisplayString());

        var alias3 = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Skip(2).First();
        Assert.Equal("Alias3=E.HidingMember", model.GetDeclaredSymbol(alias3).ToTestDisplayString());
    }

    [Fact]
    public void MemberLookup_InGlobalAlias()
    {
        var src = """
_ = typeof(global::E.Member);
_ = typeof(global::E.BaseMember);
_ = typeof(global::E.HidingMember);

explicit extension Base for object
{
    public class BaseMember { }
    public class HidingMember { }
}

explicit extension E for object : Base
{
    public class Member { }
    public class HidingMember { }
}

""";
        // PROTOTYPE should warn for hiding
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var member = tree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().First().Type;
        Assert.Equal("E.Member", model.GetSymbolInfo(member).Symbol.ToTestDisplayString());

        var baseMember = tree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().Skip(1).First().Type;
        Assert.Equal("Base.BaseMember", model.GetSymbolInfo(baseMember).Symbol.ToTestDisplayString());

        var hidingMember = tree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().Skip(2).First().Type;
        Assert.Equal("E.HidingMember", model.GetSymbolInfo(hidingMember).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void MemberLookup_InAlias()
    {
        var src = """
using Alias = E;

_ = typeof(Alias.Member);
_ = typeof(Alias.BaseMember);
_ = typeof(Alias.HidingMember);

explicit extension Base for object
{
    public class BaseMember { }
    public class HidingMember { }
}

explicit extension E for object : Base
{
    public class Member { }
    public class HidingMember { }
}
""";
        // PROTOTYPE should warn for hiding
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var member = tree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().First().Type;
        Assert.Equal("E.Member", model.GetSymbolInfo(member).Symbol.ToTestDisplayString());

        var baseMember = tree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().Skip(1).First().Type;
        Assert.Equal("Base.BaseMember", model.GetSymbolInfo(baseMember).Symbol.ToTestDisplayString());

        var hidingMember = tree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().Skip(2).First().Type;
        Assert.Equal("E.HidingMember", model.GetSymbolInfo(hidingMember).Symbol.ToTestDisplayString());
    }

    [ConditionalTheory(typeof(ClrOnly))]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void AllBaseExtensions_Retargeting(int offset)
    {
        var src1 = """
public explicit extension E1 for object { }
""";

        var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net70, assemblyName: "first");
        comp1.VerifyDiagnostics();

        var src2 = """
public explicit extension E2 for object : E1 { }
""";
        var comp2 = CreateCompilation(src2, targetFramework: TargetFramework.Net70,
            references: new[] { comp1.EmitToImageReference() });

        comp2.VerifyDiagnostics();

        var src1Updated = $$"""
public explicit extension GrandBase{{offset}} for object
{
}
public explicit extension GrandBase{{(offset + 1) % 4}} for object
{
}
public explicit extension GrandBase{{(offset + 2) % 4}} for object
{
}
public explicit extension GrandBase{{(offset + 3) % 4}} for object
{
}

public explicit extension Base1 for object : GrandBase0, GrandBase1
{
}
public explicit extension Base2 for object : GrandBase2, GrandBase3
{
}

public explicit extension E1 for object : Base1, Base2 { }
""";

        var comp1Updated = CreateCompilation(src1Updated, targetFramework: TargetFramework.Net70, assemblyName: "first");
        comp1Updated.VerifyDiagnostics();

        var src = """
public explicit extension E3 for object : E2 { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70,
            references: new[] { comp2.ToMetadataReference(), comp1Updated.EmitToImageReference() });
        comp.VerifyDiagnostics();

        var e3 = comp.GlobalNamespace.GetTypeMember("E3");
        var e2 = e3.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("E2", e2.ToTestDisplayString());
        VerifyExtension<RetargetingNamedTypeSymbol>(e2, isExplicit: true);

        AssertEx.Equal(new[] { "E1", "Base1", "GrandBase0", "GrandBase1", "Base2", "GrandBase2", "GrandBase3" },
            e2.AllBaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Simple_Static_InConversion()
    {
        var src = """
int p = object.Property;
System.Action p2 = object.Property2;

int f = object.Field;
System.Console.Write($"Field({f}) ");

System.Action m = object.Method;
m();

implicit extension E for object
{
    public static int Property
    {
        get
        {
            System.Console.Write("Property ");
            return 0;
        }
    }

    public static System.Action Property2
    {
        get
        {
            System.Console.Write("Property2 ");
            return () => { };
        }
    }

    public static int Field = 42;

    public static void Method()
    {
        System.Console.Write("Method ");
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, expectedOutput: "Property Property2 Field(42) Method");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        ITypeSymbol int32 = comp.GetSpecialType(SpecialType.System_Int32).GetPublicSymbol();
        ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String).GetPublicSymbol();
        ITypeSymbol action = comp.GetWellKnownType(WellKnownType.System_Action).GetPublicSymbol();

        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property");
        Assert.Equal("System.Int32 E.Property { get; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(property));
        Assert.Equal(ConversionKind.Identity, model.GetConversion(property).Kind);
        Assert.Equal(ConversionKind.Identity, model.ClassifyConversion(property, int32).Kind);
        Assert.Equal(ConversionKind.NoConversion, model.ClassifyConversion(property, stringType).Kind);
        Assert.Equal(ConversionKind.NoConversion, model.ClassifyConversion(property, action).Kind);

        var property2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property2");
        Assert.Equal("System.Action E.Property2 { get; }", model.GetSymbolInfo(property2).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(property2));
        Assert.Equal(ConversionKind.Identity, model.GetConversion(property2).Kind);
        Assert.Equal(ConversionKind.Identity, model.ClassifyConversion(property2, action).Kind);
        Assert.Equal(ConversionKind.NoConversion, model.ClassifyConversion(property2, stringType).Kind);
        Assert.Equal(ConversionKind.NoConversion, model.ClassifyConversion(property2, int32).Kind);

        var field = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Field");
        Assert.Equal("System.Int32 E.Field", model.GetSymbolInfo(field).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(field));
        Assert.Equal(ConversionKind.Identity, model.GetConversion(field).Kind);
        Assert.Equal(ConversionKind.Identity, model.ClassifyConversion(field, int32).Kind);

        var method = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Method");
        Assert.Equal("void E.Method()", model.GetSymbolInfo(method).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(method)); // PROTOTYPE need to fix the semantic model
        Assert.Equal(ConversionKind.MethodGroup, model.GetConversion(method).Kind);
        Assert.Equal(ConversionKind.MethodGroup, model.ClassifyConversion(method, action).Kind);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Simple_Static_InNumericConversion()
    {
        var src = """
long p = object.Property;

long f = object.Field;
System.Console.Write($"Field({f}) ");

implicit extension E for object
{
    public static int Property
    {
        get
        {
            System.Console.Write("Property ");
            return 0;
        }
    }

    public static int Field = 42;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, expectedOutput: "Property Field(42)");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property");
        Assert.Equal("System.Int32 E.Property { get; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(property));
        Assert.Equal(ConversionKind.ImplicitNumeric, model.GetConversion(property).Kind);
        ITypeSymbol int64 = comp.GetSpecialType(SpecialType.System_Int64).GetPublicSymbol();
        Assert.Equal(ConversionKind.ImplicitNumeric, model.ClassifyConversion(property, int64).Kind);

        var field = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Field");
        Assert.Equal("System.Int32 E.Field", model.GetSymbolInfo(field).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(field));
        Assert.Equal(ConversionKind.ImplicitNumeric, model.GetConversion(field).Kind);
        Assert.Equal(ConversionKind.ImplicitNumeric, model.ClassifyConversion(field, int64).Kind);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Simple_Static_InFailedConversion()
    {
        var src = """
System.Action p = object.Property;
int p2 = object.Property2;

implicit extension E for object
{
    public static int Property => throw null;
    public static System.Action Property2 => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,26): error CS0117: 'object' does not contain a definition for 'Property'
            // System.Action p = object.Property;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("object", "Property").WithLocation(1, 26),
            // (2,17): error CS0428: Cannot convert method group 'Property2' to non-delegate type 'int'. Did you intend to invoke the method?
            // int p2 = object.Property2;
            Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Property2").WithArguments("Property2", "int").WithLocation(2, 17)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property");
        Assert.Equal("System.Int32 E.Property { get; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(property));
        Assert.Equal(ConversionKind.NoConversion, model.GetConversion(property).Kind);
        ITypeSymbol int64 = comp.GetSpecialType(SpecialType.System_Int64).GetPublicSymbol();
        Assert.Equal(ConversionKind.ImplicitNumeric, model.ClassifyConversion(property, int64).Kind);

        var property2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property2");
        Assert.Equal("System.Action E.Property2 { get; }", model.GetSymbolInfo(property2).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(property2));
        Assert.Equal(ConversionKind.NoConversion, model.GetConversion(property2).Kind);
        ITypeSymbol action = comp.GetWellKnownType(WellKnownType.System_Action).GetPublicSymbol();
        Assert.Equal(ConversionKind.Identity, model.ClassifyConversion(property2, action).Kind);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Simple_Static_InMemberAccess()
    {
        var src = """
object.Property.ToString();
System.Console.Write($"Field({object.Field.ToString()}) ");
object.Type.M();
object.StaticType.M();

implicit extension E for object
{
    public static int Property
    {
        get
        {
            System.Console.Write("Property ");
            return 0;
        }
    }

    public static int Field = 42;

    public class Type
    {
        public static void M()
        {
            System.Console.Write("Type ");
        }
    }

    public class StaticType
    {
        public static void M()
        {
            System.Console.Write("StaticType ");
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, expectedOutput: "Property Field(42) Type StaticType");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property");
        Assert.Equal("System.Int32 E.Property { get; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());

        var field = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Field");
        Assert.Equal("System.Int32 E.Field", model.GetSymbolInfo(field).Symbol.ToTestDisplayString());

        var type = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Type");
        Assert.Equal("E.Type", model.GetSymbolInfo(type).Symbol.ToTestDisplayString());

        var staticType = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.StaticType");
        Assert.Equal("E.StaticType", model.GetSymbolInfo(staticType).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Simple_Static_Discards()
    {
        var src = """
_ = object.Property;
_ = object.Field;

implicit extension E for object
{
    public static int Property { get { System.Console.Write("Property"); return 42; } }
    public static int Field = 42;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "Property").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property");
        Assert.Equal("System.Int32 E.Property { get; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(property));

        var field = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Field");
        Assert.Equal("System.Int32 E.Field", model.GetSymbolInfo(field).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(field));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Simple_Static_Method()
    {
        var src = """
object.Method();

implicit extension E for object
{
    public static void Method()
    {
        System.Console.Write("Method ");
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "Method").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var method = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Method");
        Assert.Equal("void E.Method()", model.GetSymbolInfo(method).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Simple_Static_FromBaseExtension()
    {
        var src = """
_ = object.Property;
System.Console.Write($"Field({object.Field}) ");
object.Type.M();
object.StaticType.M();

implicit extension Derived for object : Base { }

implicit extension Base for object
{
    public static int Property
    {
        get
        {
            System.Console.Write("Property ");
            return 0;
        }
    }

    public static int Field = 42;

    public class Type
    {
        public static void M()
        {
            System.Console.Write("Type ");
        }
    }

    public class StaticType
    {
        public static void M()
        {
            System.Console.Write("StaticType ");
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "Property Field(42) Type StaticType").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property");
        Assert.Equal("System.Int32 Base.Property { get; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(property));

        var field = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Field");
        Assert.Equal("System.Int32 Base.Field", model.GetSymbolInfo(field).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(field));

        var type = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Type");
        Assert.Equal("Base.Type", model.GetSymbolInfo(type).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(type));

        var staticType = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.StaticType");
        Assert.Equal("Base.StaticType", model.GetSymbolInfo(staticType).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(staticType));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Simple_Static_FromBaseExtension_OnlyDerivedInScope()
    {
        var src = """
_ = object.Property;
System.Console.Write($"Field({object.Field}) ");
object.Type.M();
object.StaticType.M();

implicit extension Derived for object : N.Base { }

namespace N
{
    implicit extension Base for object
    {
        public static int Property
        {
            get
            {
                System.Console.Write("Property ");
                return 0;
            }
        }

        public static int Field = 42;

        public class Type
        {
            public static void M()
            {
                System.Console.Write("Type ");
            }
        }

        public class StaticType
        {
            public static void M()
            {
                System.Console.Write("StaticType ");
            }
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "Property Field(42) Type StaticType").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property");
        Assert.Equal("System.Int32 N.Base.Property { get; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(property));

        var field = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Field");
        Assert.Equal("System.Int32 N.Base.Field", model.GetSymbolInfo(field).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(field));

        var type = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Type");
        Assert.Equal("N.Base.Type", model.GetSymbolInfo(type).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(type));

        var staticType = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.StaticType");
        Assert.Equal("N.Base.StaticType", model.GetSymbolInfo(staticType).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(staticType));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Simple_Static_FromBaseExtension_OnlyDerivedInScope_Inaccessible()
    {
        var src = """
_ = object.Property;
System.Console.Write($"Field({object.Field}) ");
object.Type.M();

implicit extension Derived for object : N.Base { }

namespace N
{
    implicit extension Base for object
    {
        private static int Property
        {
            get
            {
                System.Console.Write("Property ");
                return 0;
            }
        }

        private static int Field = 42;

        private class Type
        {
            public static void M()
            {
                System.Console.Write("Type ");
            }
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,12): error CS0117: 'object' does not contain a definition for 'Property'
            // _ = object.Property;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("object", "Property").WithLocation(1, 12),
            // (2,38): error CS0117: 'object' does not contain a definition for 'Field'
            // System.Console.Write($"Field({object.Field}) ");
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Field").WithArguments("object", "Field").WithLocation(2, 38),
            // (3,8): error CS0117: 'object' does not contain a definition for 'Type'
            // object.Type.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Type").WithArguments("object", "Type").WithLocation(3, 8),
            // (20,28): warning CS0414: The field 'Base.Field' is assigned but its value is never used
            //         private static int Field = 42;
            Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "Field").WithArguments("N.Base.Field").WithLocation(20, 28)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property");
        Assert.Null(model.GetSymbolInfo(property).Symbol);
        Assert.Empty(model.GetMemberGroup(property));

        var field = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Field");
        Assert.Null(model.GetSymbolInfo(field).Symbol);
        Assert.Empty(model.GetMemberGroup(field));

        var type = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Type");
        Assert.Null(model.GetSymbolInfo(type).Symbol);
        Assert.Empty(model.GetMemberGroup(type));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Simple_Static_FromBaseExtension_Method()
    {
        var src = """
object.Method();

implicit extension Derived for object : Base { }

implicit extension Base for object
{
    public static void Method()
    {
        System.Console.Write("Method ");
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "Method").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var method = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Method");
        Assert.Equal("void Base.Method()", model.GetSymbolInfo(method).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(method)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Simple_Static_FromBaseExtension_Method_OnlyDerivedInScope()
    {
        var src = """
object.Method();

implicit extension Derived for object : N.Base { }

namespace N
{
    implicit extension Base for object
    {
        public static void Method()
        {
            System.Console.Write("Method ");
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "Method").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var method = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Method");
        Assert.Equal("void N.Base.Method()", model.GetSymbolInfo(method).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(method)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Simple_Static_Protected()
    {
        var src = """
_ = object.Property;
System.Console.Write($"Field({object.Field}) ");
object.Type.M();

implicit extension E for object
{
    protected static int Property
    {
        get
        {
            System.Console.Write("Property ");
            return 0;
        }
    }

    protected static int Field = 42;

    protected class Type
    {
        public static void M()
        {
            System.Console.Write("Type ");
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,12): error CS0117: 'object' does not contain a definition for 'Property'
            // _ = object.Property;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("object", "Property").WithLocation(1, 12),
            // (2,38): error CS0117: 'object' does not contain a definition for 'Field'
            // System.Console.Write($"Field({object.Field}) ");
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Field").WithArguments("object", "Field").WithLocation(2, 38),
            // (3,8): error CS0117: 'object' does not contain a definition for 'Type'
            // object.Type.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Type").WithArguments("object", "Type").WithLocation(3, 8)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property");
        Assert.Null(model.GetSymbolInfo(property).Symbol);
        Assert.Empty(model.GetMemberGroup(property));

        var field = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Field");
        Assert.Null(model.GetSymbolInfo(field).Symbol);
        Assert.Empty(model.GetMemberGroup(field));

        var type = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Type");
        Assert.Null(model.GetSymbolInfo(type).Symbol);
        Assert.Empty(model.GetMemberGroup(type));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Simple_Static_MethodGroupExists()
    {
        var src = """
C.M();

class C
{
    public static void M(int i) { }
    public static void M(string s) { }
}

implicit extension E for C
{
    public static void M()
    {
        System.Console.Write("M");
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "M").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var method = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Equal("void E.M()", model.GetSymbolInfo(method).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.M(System.Int32 i)", "void C.M(System.String s)" }, model.GetMemberGroup(method).ToTestDisplayStrings());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Simple_Static_Shadowing()
    {
        var src = """
_ = object.Property;
System.Console.Write($"Field({object.Field}) ");
object.Type.M();

implicit extension Derived for object : Base
{
    public static int Property
    {
        get
        {
            System.Console.Write("Property ");
            return 0;
        }
    }

    public static int Field = 42;

    public class Type
    {
        public static void M()
        {
            System.Console.Write("Type ");
        }
    }

    public class StaticType
    {
        public static void M()
        {
            System.Console.Write("StaticType ");
        }
    }
}

implicit extension Base for object
{
    public static int Property
    {
        get => throw null;
    }

    public static int Field = 42;

    public class Type { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should warn about hiding
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, expectedOutput: "Property Field(42) Type");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property");
        Assert.Equal("System.Int32 Derived.Property { get; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(property));

        var field = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Field");
        Assert.Equal("System.Int32 Derived.Field", model.GetSymbolInfo(field).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(field));

        var type = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Type");
        Assert.Equal("Derived.Type", model.GetSymbolInfo(type).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(type));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Simple_Static_Shadowing_OnlyDerivedInScope()
    {
        var src = """
_ = object.Property;
System.Console.Write($"Field({object.Field}) ");
object.Type.M();

implicit extension Derived for object : N.Base
{
    public static int Property
    {
        get
        {
            System.Console.Write("Property ");
            return 0;
        }
    }

    public static int Field = 42;

    public class Type
    {
        public static void M()
        {
            System.Console.Write("Type ");
        }
    }

    public class StaticType
    {
        public static void M()
        {
            System.Console.Write("StaticType ");
        }
    }
}

namespace N
{
    implicit extension Base for object
    {
        public static int Property
        {
            get => throw null;
        }

        public static int Field = 42;

        public class Type { }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should warn about hiding
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, expectedOutput: "Property Field(42) Type");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property");
        Assert.Equal("System.Int32 Derived.Property { get; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(property));

        var field = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Field");
        Assert.Equal("System.Int32 Derived.Field", model.GetSymbolInfo(field).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(field));

        var type = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Type");
        Assert.Equal("Derived.Type", model.GetSymbolInfo(type).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(type));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_VariousScopes()
    {
        var cSrc = """
class C
{
    public static void Main()
    {
        _ = object.Property;
        System.Console.Write($"Field({object.Field}) ");
        object.Type.M();
        object.StaticType.M();
    }
}
""";

        var eSrc = """
implicit extension E for object
{
    public static int Property
    {
        get
        {
            System.Console.Write("Property ");
            return 0;
        }
    }

    public static int Field = 42;

    public class Type
    {
        public static void M()
        {
            System.Console.Write("Type ");
        }
    }

    public class StaticType
    {
        public static void M()
        {
            System.Console.Write("StaticType ");
        }
    }
}
""";

        var src1 = $$"""
namespace N
{
    {{cSrc}}
    {{eSrc}}
}
""";
        verify(src1, "N.E");

        var src2 = $$"""
namespace N
{
    namespace N2
    {
        {{cSrc}}
    }

    {{eSrc}}
}
""";
        verify(src2, "N.E");

        var src3 = $$"""
{{eSrc}}
namespace N
{
    {{cSrc}}
}
""";
        verify(src3, extensionName: "E");

        var src4 = $$"""
file {{eSrc}}
{{cSrc}}
""";
        verify(src4, extensionName: "E@<tree 0>");

        var src5 = $$"""
class Container
{
    {{eSrc}}
    {{cSrc}}
}
""";
        verify(src5, extensionName: "Container.E");

        void verify(string src, string extensionName)
        {
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Property Field(42) Type StaticType").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property");
            Assert.Equal($$"""System.Int32 {{extensionName}}.Property { get; }""", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());
            Assert.Empty(model.GetMemberGroup(property));

            var field = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Field");
            Assert.Equal($$"""System.Int32 {{extensionName}}.Field""", model.GetSymbolInfo(field).Symbol.ToTestDisplayString());
            Assert.Empty(model.GetMemberGroup(field));

            var type = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Type");
            Assert.Equal($$"""{{extensionName}}.Type""", model.GetSymbolInfo(type).Symbol.ToTestDisplayString());
            Assert.Empty(model.GetMemberGroup(type));

            var staticType = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.StaticType");
            Assert.Equal($$"""{{extensionName}}.StaticType""", model.GetSymbolInfo(staticType).Symbol.ToTestDisplayString());
            Assert.Empty(model.GetMemberGroup(staticType));
        }
    }

    [Fact]
    public void ExtensionMemberLookup_VariousScopes_Errors()
    {
        var cSrc = """
class C
{
    public static void Main()
    {
        object.Method();
        _ = object.Property;
        System.Console.Write($"Field({object.Field}) ");
        object.StaticType.M();
    }
}
""";

        var eSrc = """
implicit extension E for object
{
    public static void Method() => throw null;
    public static int Property => throw null;
    public static int Field = 42;

    public static class StaticType
    {
        public static void M() => throw null;
    }
}
""";

        var src1 = $$"""
        namespace N
        {
            {{cSrc}}
            namespace N2
            {
                {{eSrc}}
            }
        }
        """;
        verify(src1,
            // (7,16): error CS0117: 'object' does not contain a definition for 'Method'
            //         object.Method();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Method").WithArguments("object", "Method").WithLocation(7, 16),
            // (8,20): error CS0117: 'object' does not contain a definition for 'Property'
            //         _ = object.Property;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("object", "Property").WithLocation(8, 20),
            // (9,46): error CS0117: 'object' does not contain a definition for 'Field'
            //         System.Console.Write($"Field({object.Field}) ");
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Field").WithArguments("object", "Field").WithLocation(9, 46),
            // (10,16): error CS0117: 'object' does not contain a definition for 'StaticType'
            //         object.StaticType.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "StaticType").WithArguments("object", "StaticType").WithLocation(10, 16)
            );

        var src2 = $$"""
file {{eSrc}}
""";
        verify(new[] { cSrc, src2 },
            // 0.cs(5,16): error CS0117: 'object' does not contain a definition for 'Method'
            //         object.Method();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Method").WithArguments("object", "Method").WithLocation(5, 16),
            // 0.cs(6,20): error CS0117: 'object' does not contain a definition for 'Property'
            //         _ = object.Property;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("object", "Property").WithLocation(6, 20),
            // 0.cs(7,46): error CS0117: 'object' does not contain a definition for 'Field'
            //         System.Console.Write($"Field({object.Field}) ");
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Field").WithArguments("object", "Field").WithLocation(7, 46),
            // 0.cs(8,16): error CS0117: 'object' does not contain a definition for 'StaticType'
            //         object.StaticType.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "StaticType").WithArguments("object", "StaticType").WithLocation(8, 16)
            );

        static void verify(CSharpTestSource src, params DiagnosticDescription[] expected)
        {
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(expected);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var method = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Method");
            Assert.Null(model.GetSymbolInfo(method).Symbol);
            Assert.Empty(model.GetMemberGroup(method));

            var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property");
            Assert.Null(model.GetSymbolInfo(property).Symbol);
            Assert.Empty(model.GetMemberGroup(property));

            var field = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Field");
            Assert.Null(model.GetSymbolInfo(field).Symbol);
            Assert.Empty(model.GetMemberGroup(field));

            var staticType = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.StaticType");
            Assert.Null(model.GetSymbolInfo(staticType).Symbol);
            Assert.Empty(model.GetMemberGroup(staticType));
        }
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_FromUsingNamespace()
    {
        var cSrc = """
class C
{
    public static void Main()
    {
        _ = object.Property;
        System.Console.Write($"Field({object.Field}) ");
        object.Type.M();
        object.StaticType.M();
    }
}
""";

        var eSrc = """
namespace N2
{
    implicit extension E for object
    {
        public static int Property
        {
            get
            {
                System.Console.Write("Property ");
                return 0;
            }
        }

        public static int Field = 42;

        public class Type
        {
            public static void M()
            {
                System.Console.Write("Type ");
            }
        }

        public class StaticType
        {
            public static void M()
            {
                System.Console.Write("StaticType ");
            }
        }
    }
}
""";

        var src1 = $$"""
using N2;
{{cSrc}}

{{eSrc}}
""";
        verify(src1, "N2.E");

        var src2 = $$"""
using N2;
using N2; // 1, 2
{{cSrc}}

{{eSrc}}
""";

        var comp = CreateCompilation(src2, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics(
            // (2,1): hidden CS8019: Unnecessary using directive.
            // using N2; // 1, 2
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N2;").WithLocation(2, 1),
            // (2,7): warning CS0105: The using directive for 'N2' appeared previously in this namespace
            // using N2; // 1, 2
            Diagnostic(ErrorCode.WRN_DuplicateUsing, "N2").WithArguments("N2").WithLocation(2, 7)
            );

        var src3 = $$"""
namespace N3
{
    using N2;

    namespace N4
    {
        {{cSrc}}
    }

    {{eSrc}}
}
""";
        verify(src3, "N3.N2.E");

        void verify(string src, string extensionName)
        {
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Property Field(42) Type StaticType").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property");
            Assert.Equal($$"""System.Int32 {{extensionName}}.Property { get; }""", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());
            Assert.Empty(model.GetMemberGroup(property));

            var field = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Field");
            Assert.Equal($$"""System.Int32 {{extensionName}}.Field""", model.GetSymbolInfo(field).Symbol.ToTestDisplayString());
            Assert.Empty(model.GetMemberGroup(field));

            var type = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Type");
            Assert.Equal($$"""{{extensionName}}.Type""", model.GetSymbolInfo(type).Symbol.ToTestDisplayString());
            Assert.Empty(model.GetMemberGroup(type));

            var staticType = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.StaticType");
            Assert.Equal($$"""{{extensionName}}.StaticType""", model.GetSymbolInfo(staticType).Symbol.ToTestDisplayString());
            Assert.Empty(model.GetMemberGroup(staticType));
        }
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_UsingNamespaceNecessity_Property()
    {
        var src = """
using N1;
using N2;

class C
{
    public static void Main()
    {
        _ = object.Property;
    }
}

namespace N1
{
    class D { }
}

namespace N2
{
    implicit extension E for object
    {
        public static int Property { get { System.Console.Write("property"); return 0; } }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using N1;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N1;").WithLocation(1, 1)
            );

        CompileAndVerify(comp, expectedOutput: "property");
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_UsingNamespaceNecessity_Method()
    {
        var src = """
using N1;
using N2;

class C
{
    public static void Main()
    {
        object.Method();
    }
}

namespace N1
{
    class D { }
}

namespace N2
{
    implicit extension E for object
    {
        public static void Method()
        {
            System.Console.Write("method");
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using N1;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N1;").WithLocation(1, 1)
            );

        CompileAndVerify(comp, expectedOutput: "method");
    }

    [Fact]
    public void ExtensionMemberLookup_UsingNamespaceNecessity_UnusedImplicitExtension_Property()
    {
        var src = """
using N1;
using N2;

class C
{
    public static void Main()
    {
        _ = object.Property;
    }
}

namespace N1
{
    implicit extension D for string { }
}

namespace N2
{
    implicit extension E for object
    {
        public static int Property => throw null;
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void ExtensionMemberLookup_UsingNamespaceNecessity_UnusedImplicitExtension_Field()
    {
        var src = """
using N1;
using N2;

class C
{
    public static void Main()
    {
        _ = object.Field;
    }
}

namespace N1
{
    implicit extension D for string { }
}

namespace N2
{
    implicit extension E for object
    {
        public static int Field = 0;
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_UsingNamespaceNecessity_UnusedExplicitExtension_Method()
    {
        var src = """
using N1;
using N2;

class C
{
    public static void Main()
    {
        object.Method();
    }
}

namespace N1
{
    explicit extension D for string { }
}

namespace N2
{
    implicit extension E for object
    {
        public static void Method()
        {
            System.Console.Write("method");
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using N1;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N1;").WithLocation(1, 1)
            );

        CompileAndVerify(comp, expectedOutput: "method");
    }

    [Fact]
    public void ExtensionMemberLookup_ExplicitExtension()
    {
        var src = """
object.Method();
_ = object.Property;
System.Console.Write($"Field({object.Field}) ");
object.StaticType.M();

explicit extension E for object
{
    public static void Method() => throw null;
    public static int Property => throw null;
    public static int Field = 42;

    public class StaticType
    {
        public static void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,8): error CS0117: 'object' does not contain a definition for 'Method'
            // object.Method();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Method").WithArguments("object", "Method").WithLocation(1, 8),
            // (2,12): error CS0117: 'object' does not contain a definition for 'Property'
            // _ = object.Property;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("object", "Property").WithLocation(2, 12),
            // (3,38): error CS0117: 'object' does not contain a definition for 'Field'
            // System.Console.Write($"Field({object.Field}) ");
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Field").WithArguments("object", "Field").WithLocation(3, 38),
            // (4,8): error CS0117: 'object' does not contain a definition for 'StaticType'
            // object.StaticType.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "StaticType").WithArguments("object", "StaticType").WithLocation(4, 8)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_InaccessibleMembers()
    {
        var src = """
object.Method();
_ = object.Property;
System.Console.Write($"Field({object.Field}) ");
object.StaticType.M();

implicit extension E for object
{
    private static void Method() => throw null;
    private static int Property => throw null;
    private static int Field = 42;

    private class StaticType
    {
        public static void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,8): error CS0117: 'object' does not contain a definition for 'Method'
            // object.Method();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Method").WithArguments("object", "Method").WithLocation(1, 8),
            // (2,12): error CS0117: 'object' does not contain a definition for 'Property'
            // _ = object.Property;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("object", "Property").WithLocation(2, 12),
            // (3,38): error CS0117: 'object' does not contain a definition for 'Field'
            // System.Console.Write($"Field({object.Field}) ");
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Field").WithArguments("object", "Field").WithLocation(3, 38),
            // (4,8): error CS0117: 'object' does not contain a definition for 'StaticType'
            // object.StaticType.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "StaticType").WithArguments("object", "StaticType").WithLocation(4, 8),
            // (10,24): warning CS0414: The field 'E.Field' is assigned but its value is never used
            //     private static int Field = 42;
            Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "Field").WithArguments("E.Field").WithLocation(10, 24)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_Ambiguity()
    {
        var src = """
object.Method();
_ = object.Property;
_ = object.Field;
_ = object.Type.M();

implicit extension E1 for object
{
    public static void Method() => throw null;
    public static int Property => throw null;
    public static int Field = 42;
    public class Type
    {
        public static void M() => throw null;
    }
}

implicit extension E2 for object
{
    public static void Method() => throw null;
    public static int Property => throw null;
    public static int Field = 42;
    public class Type
    {
        public static void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,8): error CS0121: The call is ambiguous between the following methods or properties: 'E1.Method()' and 'E2.Method()'
            // object.Method();
            Diagnostic(ErrorCode.ERR_AmbigCall, "Method").WithArguments("E1.Method()", "E2.Method()").WithLocation(1, 8),
            // (2,12): error CS0229: Ambiguity between 'E1.Property' and 'E2.Property'
            // _ = object.Property;
            Diagnostic(ErrorCode.ERR_AmbigMember, "Property").WithArguments("E1.Property", "E2.Property").WithLocation(2, 12),
            // (3,12): error CS0229: Ambiguity between 'E1.Field' and 'E2.Field'
            // _ = object.Field;
            Diagnostic(ErrorCode.ERR_AmbigMember, "Field").WithArguments("E1.Field", "E2.Field").WithLocation(3, 12),
            // (4,12): error CS0104: 'Type' is an ambiguous reference between 'E1.Type' and 'E2.Type'
            // _ = object.Type.M();
            Diagnostic(ErrorCode.ERR_AmbigContext, "Type").WithArguments("Type", "E1.Type", "E2.Type").WithLocation(4, 12)
            );
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Overloads()
    {
        var src = """
object.Method(42);
object.Method("hello");

implicit extension E1 for object
{
    public static void Method(int i)
    {
        System.Console.Write($"E1.Method({i}) ");
    }
}

implicit extension E2 for object
{
    public static void Method(string s)
    {
        System.Console.Write($"E2.Method({s}) ");
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "E1.Method(42) E2.Method(hello)").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Overloads_DifferentScopes_NestedNamespace()
    {
        var src = """
namespace N1
{
    implicit extension E1 for object
    {
        public static void Method(int i)
        {
            System.Console.Write($"E1.Method({i}) ");
        }
    }

    namespace N2
    {
        implicit extension E2 for object
        {
            public static void Method(string s)
            {
                System.Console.Write($"E2.Method({s}) ");
            }
        }

        class C
        {
            public static void Main()
            {
                object.Method(42);
                object.Method("hello");
            }
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "E1.Method(42) E2.Method(hello)").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Overloads_DifferentScopes_NestedType()
    {
        var src = """
namespace N1
{
    implicit extension E1 for object
    {
        public static void Method(int i)
        {
            System.Console.Write($"E1.Method({i}) ");
        }
    }

    class Nested
    {
        implicit extension E2 for object
        {
            public static void Method(string s)
            {
                System.Console.Write($"E2.Method({s})");
            }
        }

        class C
        {
            public static void Main()
            {
                object.Method(42);
                object.Method("hello");
            }
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "E1.Method(42) E2.Method(hello)").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_NamespaceVsUsing_FromNamespace_Property()
    {
        var src = """
using N2; // 1

class C
{
    public static void Main()
    {
        _ = object.Property;
    }
}

implicit extension E1 for object
{
    public static int Property
    {
        get
        {
            System.Console.Write("E1.Property");
            return 0;
        }
    }
}

namespace N2
{
    implicit extension E2 for object
    {
        public static int Property => throw null;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using N2; // 1
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N2;").WithLocation(1, 1)
            );
        CompileAndVerify(comp, expectedOutput: "E1.Property");
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_NamespaceVsUsing_FromNamespace_Method()
    {
        var src = """
using N2;

object.Method(42);
object.Method("hello");

implicit extension E1 for object
{
    public static void Method(int i)
    {
        System.Console.Write("E1.Method ");
    }
}

namespace N2
{
    implicit extension E2 for object
    {
        public static void Method(string s)
        {
            System.Console.Write("E2.Method");
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "E1.Method E2.Method").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_NamespaceVsUsing_FromUsing_Method()
    {
        var src = """
using N2;

object.Method("hello");

implicit extension E1 for object
{
    public static void Method2(int i) => throw null;
}

namespace N2
{
    implicit extension E2 for object
    {
        public static void Method(string s)
        {
            System.Console.Write("Method");
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "Method").VerifyDiagnostics();
    }

    [Fact]
    public void ExtensionMemberLookup_MatchingExtendedType_DerivedType()
    {
        var src = """
string.StaticType.M();

implicit extension E for object
{
    public static class StaticType
    {
        public static void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var staticType = GetSyntax<MemberAccessExpressionSyntax>(tree, "string.StaticType");
        Assert.Equal("E.StaticType", model.GetSymbolInfo(staticType).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(staticType));
    }

    [Fact]
    public void ExtensionMemberLookup_MatchingExtendedType_DerivedDerivedType()
    {
        var src = """
Derived.StaticType.M();

class Base { }
class Derived : Base { }

implicit extension E for object
{
    public static class StaticType
    {
        public static void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var staticType = GetSyntax<MemberAccessExpressionSyntax>(tree, "Derived.StaticType");
        Assert.Equal("E.StaticType", model.GetSymbolInfo(staticType).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(staticType));
    }

    [Fact]
    public void ExtensionMemberLookup_MatchingExtendedType_ImplementedInterface()
    {
        var src = """
C.StaticType.M();

interface I { }
class C : I { }

implicit extension E for I
{
    public static class StaticType
    {
        public static void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var staticType = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.StaticType");
        Assert.Equal("E.StaticType", model.GetSymbolInfo(staticType).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(staticType));
    }

    [Fact]
    public void ExtensionMemberLookup_MatchingExtendedType_IndirectlyImplementedInterface()
    {
        var src = """
C.StaticType.M();

interface I { }
interface Indirect : I { }
class C : Indirect { }

implicit extension E for I
{
    public static class StaticType
    {
        public static void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var staticType = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.StaticType");
        Assert.Equal("E.StaticType", model.GetSymbolInfo(staticType).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(staticType));
    }

    [Fact]
    public void ExtensionMemberLookup_MatchingExtendedType_TypeParameterImplementedInterface()
    {
        var src = """
class C
{
    void M<T>() where T : I
    {
        T.StaticType.M(); // 1
        _ = T.Property; // 2
    }
}

interface I
{
    int Property => 0;
}

implicit extension E for I
{
    public static class StaticType
    {
        public static void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,9): error CS0704: Cannot do non-virtual member lookup in 'T' because it is a type parameter
            //         T.StaticType.M(); // 1
            Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T").WithArguments("T").WithLocation(5, 9),
            // (6,13): error CS0704: Cannot do non-virtual member lookup in 'T' because it is a type parameter
            //         _ = T.Property; // 2
            Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T").WithArguments("T").WithLocation(6, 13)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_TypeParameterWithBaseClass()
    {
        var src = $$"""
class C<T> { }
implicit extension R<T> for C<T> where T : C<T>
{
    void M()
    {
        T.M(); // 1
        T.Type.M2(); // 2
    }

    class Type
    {
        public static void M2() { }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (6,9): error CS0704: Cannot do non-virtual member lookup in 'T' because it is a type parameter
            //         T.M(); // 1
            Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T").WithArguments("T").WithLocation(6, 9),
            // (7,9): error CS0704: Cannot do non-virtual member lookup in 'T' because it is a type parameter
            //         T.Type.M2(); // 2
            Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T").WithArguments("T").WithLocation(7, 9)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_MatchingExtendedType_BaseType()
    {
        var src = """
object.StaticType.M();

implicit extension E for string
{
    public static class StaticType
    {
        public static void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics(
            // (1,8): error CS0117: 'object' does not contain a definition for 'StaticType'
            // object.StaticType.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "StaticType").WithArguments("object", "StaticType").WithLocation(1, 8)
            );
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_MatchingExtendedType_GenericType()
    {
        var src = """
C<int>.StaticType.M();

class C<T> { }

implicit extension E<T> for C<T>
{
    public static class StaticType
    {
        public static void M() { System.Console.Write("ran"); }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "ran", verify: Verification.FailsPEVerify)
           .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<int>.StaticType");
        Assert.Equal("E<System.Int32>.StaticType", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_MatchingExtendedType_GenericType_Nested()
    {
        var src = """
C<int>.D.StaticType.M();

class C<T>
{
    public class D { }
}

implicit extension E<T> for C<T>.D
{
    public static class StaticType
    {
        public static void M() { System.Console.Write("ran"); }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "ran", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<int>.D.StaticType");
        Assert.Equal("E<System.Int32>.StaticType", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_MatchingExtendedType_DynamicDifference()
    {
        // Note: no dynamic resolution of implicit extensions
        var src = """
dynamic.StaticType.M(); // 1
dynamic.Method(); // 2
_ = dynamic.Property; // 3
_ = dynamic.Field; // 4

dynamic d = new object();
d.StaticType.M(); // This will fail at runtime

object o = new object();
o.StaticType.M(); // 5

implicit extension E for object
{
    public static class StaticType
    {
        public static void M() => throw null;
    }

    public static void Method() => throw null;
    public static int Property => throw null;
    public static int Field = 42;
}
""";
        // PROTOTYPE see if we can improve the diagnostic location for 5 (should be on "StaticType", not "o.StaticType")
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics(
            // (1,1): error CS0103: The name 'dynamic' does not exist in the current context
            // dynamic.StaticType.M(); // 1
            Diagnostic(ErrorCode.ERR_NameNotInContext, "dynamic").WithArguments("dynamic").WithLocation(1, 1),
            // (2,1): error CS0103: The name 'dynamic' does not exist in the current context
            // dynamic.Method(); // 2
            Diagnostic(ErrorCode.ERR_NameNotInContext, "dynamic").WithArguments("dynamic").WithLocation(2, 1),
            // (3,5): error CS0103: The name 'dynamic' does not exist in the current context
            // _ = dynamic.Property; // 3
            Diagnostic(ErrorCode.ERR_NameNotInContext, "dynamic").WithArguments("dynamic").WithLocation(3, 5),
            // (4,5): error CS0103: The name 'dynamic' does not exist in the current context
            // _ = dynamic.Field; // 4
            Diagnostic(ErrorCode.ERR_NameNotInContext, "dynamic").WithArguments("dynamic").WithLocation(4, 5),
            // (10,1): error CS0572: 'StaticType': cannot reference a type through an expression; try 'E.StaticType' instead
            // o.StaticType.M(); // 5
            Diagnostic(ErrorCode.ERR_BadTypeReference, "o.StaticType").WithArguments("StaticType", "E.StaticType").WithLocation(10, 1)
            );
    }

    [Fact]
    public void DynamicArgument()
    {
        // No extension members in dynamic invocation
        var src = """
dynamic d = null;
object.M(d);

implicit extension E for object
{
    public static void Method(object o) => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,8): error CS0117: 'object' does not contain a definition for 'M'
            // object.M(d);
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("object", "M").WithLocation(2, 8)
            );
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_MatchingExtendedType_DynamicDifference_Nested()
    {
        var src = """
C<dynamic>.StaticType.M();

class C<T> { }

implicit extension E for C<object>
{
    public static class StaticType
    {
        public static void M()
        {
            System.Console.Write("M");
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "M").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_MatchingExtendedType_DynamicDifference_InBase()
    {
        var src = """
D.StaticType.M();

class C<T> { }
class D : C<dynamic> { }

implicit extension E for C<object>
{
    public static class StaticType
    {
        public static void M()
        {
            System.Console.Write("M");
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "M").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_MatchingExtendedType_DynamicDifference_InInterface()
    {
        var src = """
D.StaticType.M();

interface I<T> { }
class D : I<dynamic> { }

implicit extension E for I<object>
{
    public static class StaticType
    {
        public static void M()
        {
            System.Console.Write("M");
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,11): error CS1966: 'D': cannot implement a dynamic interface 'I<dynamic>'
            // class D : I<dynamic> { }
            Diagnostic(ErrorCode.ERR_DeriveFromConstructedDynamic, "I<dynamic>").WithArguments("D", "I<dynamic>").WithLocation(4, 11)
            );
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_MatchingExtendedType_TupleNamesDifference()
    {
        var src = """
C<(int a, int b)>.StaticType.M();
C<(int, int)>.StaticType.M();
C<(int other, int)>.StaticType.M();

class C<T> { }

implicit extension E for C<(int a, int b)>
{
    public static class StaticType
    {
        public static void M()
        {
            System.Console.Write("M");
        }
    }
}
""";
        // PROTOTYPE consider warning for certain tuple name differences
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "MMM").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_MatchingExtendedType_TupleNamesDifference_InBase()
    {
        var src = """
D1.StaticType.M();
D2.StaticType.M();
D3.StaticType.M();

class C<T> { }
class D1 : C<(int a, int b)> { }
class D2 : C<(int, int)> { }
class D3 : C<(int other, int)> { }

implicit extension E for C<(int a, int b)>
{
    public static class StaticType
    {
        public static void M()
        {
            System.Console.Write("M");
        }
    }
}
""";
        // PROTOTYPE consider warning for certain tuple name differences
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "MMM").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_MatchingExtendedType_TupleNamesDifference_InInterface()
    {
        var src = """
D1.StaticType.M();
D2.StaticType.M();
D3.StaticType.M();

class I<T> { }
class D1 : I<(int a, int b)> { }
class D2 : I<(int, int)> { }
class D3 : I<(int other, int)> { }

implicit extension E for I<(int a, int b)>
{
    public static class StaticType
    {
        public static void M()
        {
            System.Console.Write("M");
        }
    }
}
""";
        // PROTOTYPE consider warning for certain tuple name differences
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "MMM").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_NullabilityDifference_AnnotatedInExtendedType()
    {
        var src = """
#nullable enable
C<object>.StaticType.M();
C<object?>.StaticType.M();

C<
#nullable disable
    object
#nullable enable
    >.StaticType.M();

class C<T> { }

implicit extension E for C<object?>
{
    public static class StaticType
    {
        public static void M()
        {
            System.Console.Write("M");
        }
    }
}
""";
        // PROTOTYPE consider warning for certain nullability differences
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "MMM").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_NullabilityDifference_AnnotatedInExtendedType_InBase()
    {
        var src = """
#nullable enable

D1.StaticType.M();
D2.StaticType.M();
D3.StaticType.M();

class C<T> { }

class D1 : C<object> { }
class D2 : C<object?> { }

class D3 : C<
#nullable disable
    object
#nullable enable
    > { }

implicit extension E for C<object?>
{
    public static class StaticType
    {
        public static void M()
        {
            System.Console.Write("M");
        }
    }
}
""";
        // PROTOTYPE consider warning for certain nullability differences
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "MMM").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_NullabilityDifference_AnnotatedInExtendedType_InInterface()
    {
        var src = """
#nullable enable

D1.StaticType.M();
D2.StaticType.M();
D3.StaticType.M();

interface I<T> { }

class D1 : I<object> { }
class D2 : I<object?> { }

class D3 : I<
#nullable disable
    object
#nullable enable
    > { }

implicit extension E for I<object?>
{
    public static class StaticType
    {
        public static void M()
        {
            System.Console.Write("M");
        }
    }
}
""";
        // PROTOTYPE consider warning for certain nullability differences
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "MMM").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_NullabilityDifference_UnannotatedInExtendedType()
    {
        var src = """
#nullable enable
C<object>.StaticType.M();
C<object?>.StaticType.M();

C<
#nullable disable
    object
#nullable enable
    >.StaticType.M();

class C<T> { }

implicit extension E for C<object>
{
    public static class StaticType
    {
        public static void M()
        {
            System.Console.Write("M");
        }
    }
}
""";
        // PROTOTYPE consider warning for certain nullability differences
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "MMM").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_NullabilityDifference_UnannotatedInExtendedType_InBase()
    {
        var src = """
#nullable enable
D1.StaticType.M();
D2.StaticType.M();
D3.StaticType.M();

class C<T> { }

class D1 : C<object> { }
class D2 : C<object?> { }

class D3 : C<
#nullable disable
    object
#nullable enable
    > { }

implicit extension E for C<object>
{
    public static class StaticType
    {
        public static void M() { }
    }
}
""";
        // PROTOTYPE consider warning for certain nullability differences
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_NullabilityDifference_UnannotatedInExtendedType_InInterface()
    {
        var src = """
#nullable enable
D1.StaticType.M();
D2.StaticType.M();
D3.StaticType.M();

interface I<T> { }

class D1 : I<object> { }
class D2 : I<object?> { }

class D3 : I<
#nullable disable
    object
#nullable enable
    > { }

implicit extension E for I<object>
{
    public static class StaticType
    {
        public static void M()
        {
            System.Console.Write("M");
        }
    }
}
""";
        // PROTOTYPE consider warning for certain nullability differences
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "MMM").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_NullabilityDifference_ObliviousInExtendedType()
    {
        var src = """
#nullable enable
C<object>.StaticType.M();
C<object?>.StaticType.M();

C<
#nullable disable
    object
#nullable enable
    >.StaticType.M();

class C<T> { }

implicit extension E for C<
#nullable disable
    object
#nullable enable
    >
{
    public static class StaticType
    {
        public static void M()
        {
            System.Console.Write("M");
        }
    }
}
""";
        // PROTOTYPE consider warning for certain nullability differences
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "MMM").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_NullabilityDifference_ObliviousInExtendedType_InBase()
    {
        var src = """
#nullable enable
D1.StaticType.M();
D2.StaticType.M();
D3.StaticType.M();

class C<T> { }

class D1 : C<object> { }
class D2 : C<object?> { }

class D3 : C<
#nullable disable
    object
#nullable enable
    > { }

implicit extension E for C<
#nullable disable
    object
#nullable enable
    >
{
    public static class StaticType
    {
        public static void M()
        {
            System.Console.Write("M");
        }
    }
}
""";
        // PROTOTYPE consider warning for certain nullability differences
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "MMM").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_NullabilityDifference_AnnotatedTypeParameterInExtendedType()
    {
        var src = """
#nullable enable
C<object>.StaticType.M();
C<object?>.StaticType.M();

C<
#nullable disable
    object
#nullable enable
    >.StaticType.M();

class C<T> { }

implicit extension E<T> for C<T?>
{
    public static class StaticType
    {
        public static void M() { System.Console.Write("ran "); }
    }
}
""";
        // PROTOTYPE consider warning for certain nullability differences
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "ran ran ran", verify: Verification.FailsPEVerify)
          .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<object>.StaticType");
        Assert.Equal("E<System.Object>.StaticType", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());

        var memberAccess2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<object?>.StaticType");
        Assert.Equal("E<System.Object?>.StaticType", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_NullabilityDifference_AnnotatedTypeParameterInExtendedType_Constrained()
    {
        var src = """
#nullable enable
C<object>.StaticType.M();
C<object?>.StaticType.M();

C<
#nullable disable
    object
#nullable enable
    >.StaticType.M();

class C<T> { }

implicit extension E<T> for C<T?> where T : class
{
    public static class StaticType
    {
        public static void M() { System.Console.Write("ran "); }
    }
}
""";
        // PROTOTYPE consider warning for certain nullability differences
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, expectedOutput: "ran ran ran", verify: Verification.FailsPEVerify)
           .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<object>.StaticType");
        Assert.Equal("E<System.Object>.StaticType", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());

        var memberAccess2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<object?>.StaticType");
        Assert.Equal("E<System.Object?>.StaticType", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Nameof()
    {
        var src = """
System.Console.Write($"{nameof(object.M)} ");
System.Console.Write($"{nameof(object.StaticType)}");

implicit extension E for object
{
    public static void M() { }
    public static class StaticType { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "M StaticType").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Nameof_Overloads()
    {
        var src = """
System.Console.Write($"{nameof(object.M)} ");

implicit extension E for object
{
    public static void M() { }
    public static void M(int i) { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "M").VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Nameof_SimpleName()
    {
        var src = """
class C
{
    void M()
    {
        _ = nameof(Method);
        _ = nameof(StaticType);
    }
}

implicit extension E for object
{
    public static void Method() { }
    public static class StaticType { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,20): error CS0103: The name 'Method' does not exist in the current context
            //         _ = nameof(Method);
            Diagnostic(ErrorCode.ERR_NameNotInContext, "Method").WithArguments("Method").WithLocation(5, 20),
            // (6,20): error CS0103: The name 'StaticType' does not exist in the current context
            //         _ = nameof(StaticType);
            Diagnostic(ErrorCode.ERR_NameNotInContext, "StaticType").WithArguments("StaticType").WithLocation(6, 20)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_Indexer_Static()
    {
        var src = """
implicit extension E for object
{
    public static int this[int i]
    {
        get
        {
            return 0;
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,23): error CS0106: The modifier 'static' is not valid for this item
            //     public static int this[int i]
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(3, 23)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_Indexer_Instance_Getter()
    {
        var src = """
object o = new object();

/*<bind>*/
_ = o[42];
/*</bind>*/

implicit extension E for object
{
    public int this[int i]
    {
        get
        {
            System.Console.Write("indexer");
            return 0;
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE revisit as part of "indexer access" section
        comp.VerifyDiagnostics(
            // (4,5): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
            // _ = o[42];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "o[42]").WithArguments("object").WithLocation(4, 5)
            );
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "indexer");

        //        string expectedOperationTree = """
        //ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '_ = o[42]')
        //Left:
        //  IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
        //Right:
        //  IPropertyReferenceOperation: System.Int32 E.this[System.Int32 i] { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'o[42]')
        //    Instance Receiver:
        //      ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
        //    Arguments(1):
        //        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '42')
        //          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
        //          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        //          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        //""";
        //        var expectedDiagnostics = DiagnosticDescription.None;

        //        VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(src,
        //            expectedOperationTree, expectedDiagnostics, targetFramework: TargetFramework.Net70);
    }

    [Fact]
    public void ExtensionMemberLookup_Indexer_Instance_Setter()
    {
        var src = """
object o = new object();

/*<bind>*/
o[42] = 0;
/*</bind>*/

implicit extension E for object
{
    public int this[int i]
    {
        set
        {
            System.Console.Write("indexer");
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE revisit as part of the "indexer access" section
        comp.VerifyDiagnostics(
            // (4,1): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
            // o[42] = 0;
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "o[42]").WithArguments("object").WithLocation(4, 1)
            );
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "indexer");

        //        string expectedOperationTree = """
        //ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'o[42] = 0')
        //Left:
        //  IPropertyReferenceOperation: System.Int32 E.this[System.Int32 i] { set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'o[42]')
        //    Instance Receiver:
        //      ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
        //    Arguments(1):
        //        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '42')
        //          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
        //          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        //          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        //Right:
        //  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        //""";
        //        var expectedDiagnostics = DiagnosticDescription.None;

        //        VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(src,
        //            expectedOperationTree, expectedDiagnostics, targetFramework: TargetFramework.Net70);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Instance_Simple_Property()
    {
        var src = """
object o = new object();
_ = o.Property;

implicit extension E for object
{
    public int Property
    {
        get
        {
            System.Console.Write("Property ");
            return 0;
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        // PROTOTYPE Revisit when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "Property");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "o.Property");
        Assert.Equal("System.Int32 E.Property { get; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(property));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Instance_Simple_Method()
    {
        var src = """
object o = new object();
o.Method();

implicit extension E for object
{
    public void Method()
    {
        System.Console.Write("Method");
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        // PROTOTYPE Revisit when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "Method");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var method = GetSyntax<MemberAccessExpressionSyntax>(tree, "o.Method");
        Assert.Equal("void E.Method()", model.GetSymbolInfo(method).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(method)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Instance_Null_Property()
    {
        var src = """
#nullable enable

object? o2 = null;
_ = o2.Property;

implicit extension E for object
{
    public int Property
    {
        get
        {
            System.Console.Write("Property ");
            return 0;
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,5): warning CS8602: Dereference of a possibly null reference.
            // _ = o2.Property;
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o2").WithLocation(4, 5)
            );

        // PROTOTYPE What is the expected runtime behavior? NRE? The nullability checks should be adjusted correspondingly.
        // PROTOTYPE Revisit when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "Property");
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_Instance_Null_Method()
    {
        var src = """
#nullable enable

object? o = null;
o.Method();

implicit extension E for object
{
    public void Method()
    {
        System.Console.Write("Method ");
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,1): warning CS8602: Dereference of a possibly null reference.
            // o.Method();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(4, 1)
            );

        // PROTOTYPE What is the expected runtime behavior? NRE? The nullability checks should be adjusted correspondingly.
        // PROTOTYPE Revisit when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "Method");
    }

    [Fact]
    public void ExtensionMemberLookup_InstanceVsStatic()
    {
        var src = """
object o = new object();
o.Method();
_ = o.Property;

implicit extension E for object
{
    public static void Method() => throw null;
    public static int Property => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,1): error CS0176: Member 'E.Method()' cannot be accessed with an instance reference; qualify it with a type name instead
            // o.Method();
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "o.Method").WithArguments("E.Method()").WithLocation(2, 1),
            // (3,5): error CS0176: Member 'E.Property' cannot be accessed with an instance reference; qualify it with a type name instead
            // _ = o.Property;
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "o.Property").WithArguments("E.Property").WithLocation(3, 5)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var method = GetSyntax<MemberAccessExpressionSyntax>(tree, "o.Method");
        Assert.Null(model.GetSymbolInfo(method).Symbol);
        Assert.Empty(model.GetMemberGroup(method));

        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "o.Property");
        Assert.Null(model.GetSymbolInfo(property).Symbol);
        Assert.Empty(model.GetMemberGroup(property));
    }

    [Fact]
    public void ExtensionMemberLookup_StaticVsInstance()
    {
        var src = """
_ = object.Property;
_ = object.Field;

public implicit extension E for object
{
    public int Property => 0;
    public int Field = 0;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,5): error CS0120: An object reference is required for the non-static field, method, or property 'E.Property'
            // _ = object.Property;
            Diagnostic(ErrorCode.ERR_ObjectRequired, "object.Property").WithArguments("E.Property").WithLocation(1, 5),
            // (2,5): error CS0120: An object reference is required for the non-static field, method, or property 'E.Field'
            // _ = object.Field;
            Diagnostic(ErrorCode.ERR_ObjectRequired, "object.Field").WithArguments("E.Field").WithLocation(2, 5),
            // (7,16): error CS9313: 'E.Field': cannot declare instance members with state in extension types.
            //     public int Field = 0;
            Diagnostic(ErrorCode.ERR_StateInExtension, "Field").WithArguments("E.Field").WithLocation(7, 16)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Property");
        Assert.Null(model.GetSymbolInfo(property).Symbol);
        Assert.Empty(model.GetMemberGroup(property));

        var field = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Field");
        Assert.Null(model.GetSymbolInfo(field).Symbol);
        Assert.Empty(model.GetMemberGroup(field));
    }

    [Fact]
    public void ExtensionMemberLookup_StaticVsInstance_Method()
    {
        var src = """
object.Method();

implicit extension E for object
{
    public void Method() => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,1): error CS0120: An object reference is required for the non-static field, method, or property 'E.Method()'
            // object.Method();
            Diagnostic(ErrorCode.ERR_ObjectRequired, "object.Method").WithArguments("E.Method()").WithLocation(1, 1)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var method = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Method");
        Assert.Null(model.GetSymbolInfo(method).Symbol);
        Assert.Empty(model.GetMemberGroup(method));
    }

    [ConditionalFact(typeof(CoreClrOnly), AlwaysSkip = "fails in emit stage in used assemblies leg")]
    public void ExtensionMemberLookup_ColorColor_Property()
    {
        var src = """
class C
{
    static void M(C C)
    {
        C.Property = 42;
    }
}

implicit extension E for C
{
    public int Property
    {
        set
        {
            System.Console.Write("Property");
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "Property");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Property");
        Assert.Equal("System.Int32 E.Property { set; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(property));
    }

    [Fact]
    public void ExtensionMemberLookup_ColorColor_Method()
    {
        var src = """
class C
{
    static void M(C C)
    {
        C.Method();
    }
}

implicit extension E for C
{
    public void Method()
    {
        System.Console.Write("Method ");
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "Method");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var method = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Method");
        Assert.Equal("void E.Method()", model.GetSymbolInfo(method).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(method)); // PROTOTYPE need to fix the semantic model
    }

    [Fact]
    public void ExtensionMemberLookup_ColorColor_Static_Property()
    {
        var src = """
class C
{
    void M(C C)
    {
        _ = C.Property;
    }
}

implicit extension E for C
{
    public static int Property => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,13): error CS0176: Member 'E.Property' cannot be accessed with an instance reference; qualify it with a type name instead
            //         _ = C.Property;
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "C.Property").WithArguments("E.Property").WithLocation(5, 13)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Property");
        Assert.Null(model.GetSymbolInfo(property).Symbol);
        Assert.Empty(model.GetMemberGroup(property));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_ColorColor_Static_Method()
    {
        var src = """
C.M(null);

class C
{
    public static void M(C C)
    {
        C.Method();
    }
}

implicit extension E for C
{
    public static void Method()
    {
        System.Console.Write("Method");
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "Method").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var method = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Method");
        Assert.Equal("void E.Method()", model.GetSymbolInfo(method).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(method)); // PROTOTYPE need to fix the semantic model
    }

    [Fact]
    public void ExtensionMemberLookup_AttributeProperty()
    {
        var src = """
[My(Property = 0)]
class MyAttribute : System.Attribute
{
}

[My(Property = 1)]
implicit extension E for MyAttribute
{
    [My(Property = 2)]
    public int Property
    {
        [My(Property = 3)]
        get => throw null;

        [My(Property = 4)]
        set => throw null;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics(
            // (1,5): error CS0246: The type or namespace name 'Property' could not be found (are you missing a using directive or an assembly reference?)
            // [My(Property = 0)]
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Property").WithArguments("Property").WithLocation(1, 5),
            // (6,5): error CS0246: The type or namespace name 'Property' could not be found (are you missing a using directive or an assembly reference?)
            // [My(Property = 1)]
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Property").WithArguments("Property").WithLocation(6, 5),
            // (9,9): error CS0246: The type or namespace name 'Property' could not be found (are you missing a using directive or an assembly reference?)
            //     [My(Property = 2)]
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Property").WithArguments("Property").WithLocation(9, 9),
            // (12,13): error CS0246: The type or namespace name 'Property' could not be found (are you missing a using directive or an assembly reference?)
            //         [My(Property = 3)]
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Property").WithArguments("Property").WithLocation(12, 13),
            // (15,13): error CS0246: The type or namespace name 'Property' could not be found (are you missing a using directive or an assembly reference?)
            //         [My(Property = 4)]
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Property").WithArguments("Property").WithLocation(15, 13)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_AttributeValue_SimpleName()
    {
        var src = """
[My(Property = Constant)]
class MyAttribute : System.Attribute
{
    public int Property { get; set; }
}

implicit extension E for MyAttribute
{
    public const int Constant = 42;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics(
            // (1,16): error CS0103: The name 'Constant' does not exist in the current context
            // [My(Property = Constant)]
            Diagnostic(ErrorCode.ERR_NameNotInContext, "Constant").WithArguments("Constant").WithLocation(1, 16)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_AttributeValue_MemberAccess()
    {
        var src = """
[My(Property = MyAttribute.Constant)]
class MyAttribute : System.Attribute
{
    public int Property { get; set; }
}

implicit extension E for MyAttribute
{
    public const int Constant = 42;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, verify: Verification.FailsPEVerify,
            sourceSymbolValidator: attributeValidator, symbolValidator: attributeValidator);

        return;

        static void attributeValidator(ModuleSymbol m)
        {
            var attributeType = m.GlobalNamespace.GetTypeMember("MyAttribute");
            var attributes = attributeType.GetAttributes();
            Assert.Equal(1, attributes.Length);
            attributes[0].VerifyNamedArgumentValue(0, "Property", TypedConstantKind.Primitive, 42);
            Assert.Equal("MyAttribute(Property = 42)", attributes[0].ToString());
        };
    }

    [Fact]
    public void ExtensionMemberLookup_AttributeValue_MemberAccess_NotAConstant()
    {
        var src = """
[My(Property = MyAttribute.NotConstant)]
class MyAttribute : System.Attribute
{
    public int Property { get; set; }
}

implicit extension E for MyAttribute
{
    public static int NotConstant = 42;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,16): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
            // [My(Property = MyAttribute.NotConstant)]
            Diagnostic(ErrorCode.ERR_BadAttributeArgument, "MyAttribute.NotConstant").WithLocation(1, 16)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_Literals_Methods()
    {
        var src = """
1.M();
2L.M();
"hello".M();

implicit extension E1 for int
{
    public void M()
    {
        System.Console.Write("M(int) ");
    }
}

implicit extension E2 for long
{
    public void M()
    {
        System.Console.Write("M(long) ");
    }
}

implicit extension E3 for string
{
    public void M()
    {
        System.Console.Write("M(string) ");
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "M(int) M(long) M(string)");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var intM = GetSyntax<MemberAccessExpressionSyntax>(tree, "1.M");
        Assert.Equal("void E1.M()", model.GetSymbolInfo(intM).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(intM)); // PROTOTYPE need to fix the semantic model

        var longM = GetSyntax<MemberAccessExpressionSyntax>(tree, "2L.M");
        Assert.Equal("void E2.M()", model.GetSymbolInfo(longM).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(longM)); // PROTOTYPE need to fix the semantic model

        var stringM = GetSyntax<MemberAccessExpressionSyntax>(tree, "\"hello\".M");
        Assert.Equal("void E3.M()", model.GetSymbolInfo(stringM).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(stringM)); // PROTOTYPE need to fix the semantic model
    }

    [Fact]
    public void ExtensionMemberLookup_Literals_Properties()
    {
        var src = """
_ = 1.Property;
_ = 2L.Property;
_ = "hello".Property;

implicit extension E1 for int
{
    public int Property
    {
        get
        {
            System.Console.Write("Property(int) ");
            return 42;
        }
    }
}

implicit extension E2 for long
{
    public int Property
    {
        get
        {
            System.Console.Write("Property(long) ");
            return 42;
        }
    }
}

implicit extension E3 for string
{
    public int Property
    {
        get
        {
            System.Console.Write("Property(string) ");
            return 42;
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "Property(int) Property(long) Property(string)");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var intM = GetSyntax<MemberAccessExpressionSyntax>(tree, "1.Property");
        Assert.Equal("System.Int32 E1.Property { get; }", model.GetSymbolInfo(intM).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(intM)); // PROTOTYPE need to fix the semantic model

        var longM = GetSyntax<MemberAccessExpressionSyntax>(tree, "2L.Property");
        Assert.Equal("System.Int32 E2.Property { get; }", model.GetSymbolInfo(longM).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(longM)); // PROTOTYPE need to fix the semantic model

        var stringM = GetSyntax<MemberAccessExpressionSyntax>(tree, "\"hello\".Property");
        Assert.Equal("System.Int32 E3.Property { get; }", model.GetSymbolInfo(stringM).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(stringM)); // PROTOTYPE need to fix the semantic model
    }

    [Fact]
    public void ExtensionMemberLookup_InTypeConstraint_Generic()
    {
        var src = """
class C { }

implicit extension E for C
{
    public class Nested<T> where T : C.Nested<T> { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var nested = comp.GlobalNamespace.GetTypeMember("E").GetTypeMember("Nested");
        var t = nested.TypeParameters.Single();
        Assert.Equal("E.Nested<T>", t.ConstraintTypes().Single().ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_InTypeConstraint_Generic_OuterScope()
    {
        var src = """
public class C { }

public implicit extension E1 for C
{
    public class Nested<T> { }
}

namespace N
{
    public class D<T> where T : C.Nested<T> { }

    public static class E2
    {
        public static void Nested(this C c) { }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var d = comp.GetTypeByMetadataName("N.D`1");
        Assert.Equal("E1.Nested<T>", d.TypeParameters.Single().ConstraintTypes().Single().ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_InTypeConstraint_NonGeneric()
    {
        var src = """
class C { }
class D<T> where T : C.Nested { }

implicit extension E for C
{
    public class Nested { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var d = comp.GlobalNamespace.GetTypeMember("D");
        var t = d.TypeParameters.Single();
        Assert.Equal("E.Nested", t.ConstraintTypes().Single().ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_InTypeConstraint_NonGeneric_OnExtensionType()
    {
        var src = """
class C<T> { }

implicit extension E<U> for C<U> where U : C<U>.Nested
{
    public class Nested { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var e = comp.GlobalNamespace.GetTypeMember("E");
        var u = e.TypeParameters.Single();
        Assert.Equal("E<U>.Nested", u.ConstraintTypes().Single().ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_InTypeConstraint_WithInaccessibleNestedType()
    {
        var src = """
class C
{
    protected class Nested { } // inaccessible
}

class D<T> where T : C.Nested { }

implicit extension E for C
{
    public class Nested { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var d = comp.GlobalNamespace.GetTypeMember("D");
        var t = d.TypeParameters.Single();
        Assert.Equal("E.Nested", t.ConstraintTypes().Single().ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_InTypeConstraint_Generic_WithInaccessibleNestedType()
    {
        var src = """
class C
{
    protected class Nested<T> { } // inaccessible
}

class D<T> where T : C.Nested<int> { }

implicit extension E for C
{
    public class Nested<T> { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var d = comp.GlobalNamespace.GetTypeMember("D");
        var t = d.TypeParameters.Single();
        Assert.Equal("E.Nested<System.Int32>", t.ConstraintTypes().Single().ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_InTypeConstraint_AttemptingAmbiguousNestedType()
    {
        var ilSrc = """
.class public auto ansi beforefieldinit C extends [mscorlib]System.Object
{
    .class nested public auto ansi beforefieldinit Nested extends [mscorlib]System.Object
    {
    }

    .class nested public auto ansi beforefieldinit Nested extends [mscorlib]System.Object
    {
    }
}
""";

        var src = """
class D<T> where T : C.Nested { }

implicit extension E for C
{
    public class Nested { }
}
""";
        var comp = CreateCompilationWithIL(src, ilSrc, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var d = comp.GlobalNamespace.GetTypeMember("D");
        var t = d.TypeParameters.Single();
        Assert.Equal("C.Nested", t.ConstraintTypes().Single().ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_InTypeConstraint_Generic_AttemptingAmbiguousNestedType()
    {
        var ilSrc = """
.class public auto ansi beforefieldinit C extends [mscorlib]System.Object
{
    .class nested public auto ansi beforefieldinit Nested`1<T> extends [mscorlib]System.Object
    {
    }

    .class nested public auto ansi beforefieldinit Nested`1<valuetype .ctor ([mscorlib]System.ValueType) T> extends [mscorlib]System.Object
    {
    }
}
""";

        var src = """
class D<T> where T : C.Nested<int> { }

implicit extension E for C
{
    public class Nested<T> { }
}
""";
        var comp = CreateCompilationWithIL(src, ilSrc, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var d = comp.GlobalNamespace.GetTypeMember("D");
        var t = d.TypeParameters.Single();
        Assert.Equal("C.Nested<System.Int32>", t.ConstraintTypes().Single().ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_InTypeConstraint_AsTypeParameter()
    {
        var src = """
class C { }

implicit extension E for C
{
    public class Nested<T> where T : C.Nested<C.Nested<T>> { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,25): error CS0311: The type 'E.Nested<T>' cannot be used as type parameter 'T' in the generic type or method 'E.Nested<T>'. There is no implicit reference conversion from 'E.Nested<T>' to 'E.Nested<E.Nested<E.Nested<T>>>'.
            //     public class Nested<T> where T : C.Nested<C.Nested<T>> { }
            Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "T").WithArguments("E.Nested<T>", "E.Nested<E.Nested<E.Nested<T>>>", "T", "E.Nested<T>").WithLocation(5, 25)
            );

        var nested = comp.GlobalNamespace.GetTypeMember("E").GetTypeMember("Nested");
        var t = nested.TypeParameters.Single();
        Assert.Equal("E.Nested<E.Nested<T>>", t.ConstraintTypes().Single().ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_SimpleName()
    {
        var src = """
class C
{
    void M()
    {
        Method(); // 1
        StaticMethod(); // 2
        _ = Property; // 3
        _ = StaticProperty; // 4
        _ = Field; // 5
        Type.M2(); // 6
    }
}

implicit extension E for C
{
    public void Method() { }
    public static void StaticMethod() { }
    public int Property => 0;
    public static int StaticProperty => 0;
    public static int Field = 42;
    public class Type
    {
        public void M2() { }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,9): error CS0103: The name 'Method' does not exist in the current context
            //         Method(); // 1
            Diagnostic(ErrorCode.ERR_NameNotInContext, "Method").WithArguments("Method").WithLocation(5, 9),
            // (6,9): error CS0103: The name 'StaticMethod' does not exist in the current context
            //         StaticMethod(); // 2
            Diagnostic(ErrorCode.ERR_NameNotInContext, "StaticMethod").WithArguments("StaticMethod").WithLocation(6, 9),
            // (7,13): error CS0103: The name 'Property' does not exist in the current context
            //         _ = Property; // 3
            Diagnostic(ErrorCode.ERR_NameNotInContext, "Property").WithArguments("Property").WithLocation(7, 13),
            // (8,13): error CS0103: The name 'StaticProperty' does not exist in the current context
            //         _ = StaticProperty; // 4
            Diagnostic(ErrorCode.ERR_NameNotInContext, "StaticProperty").WithArguments("StaticProperty").WithLocation(8, 13),
            // (9,13): error CS0103: The name 'Field' does not exist in the current context
            //         _ = Field; // 5
            Diagnostic(ErrorCode.ERR_NameNotInContext, "Field").WithArguments("Field").WithLocation(9, 13),
            // (10,9): error CS0103: The name 'Type' does not exist in the current context
            //         Type.M2(); // 6
            Diagnostic(ErrorCode.ERR_NameNotInContext, "Type").WithArguments("Type").WithLocation(10, 9)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_AsFunctionType_StaticMethod()
    {
        var src = """
class C
{
    void M()
    {
        var x = C.Method;
    }
}

implicit extension E for C
{
    public static string Method() => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE need to infer delegate type
        comp.VerifyDiagnostics(
            // (5,17): error CS8917: The delegate type could not be inferred.
            //         var x = C.Method;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "C.Method").WithLocation(5, 17)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var x = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        //Assert.Equal("System.Func<System.String> x", model.GetDeclaredSymbol(x).ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_AsFunctionType_StaticMethod_Overloads()
    {
        var src = """
class C
{
    void M()
    {
        var x = C.Method;
    }
}

implicit extension E for C
{
    public static string Method() => throw null;
    public static string Method(int i) => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,17): error CS8917: The delegate type could not be inferred.
            //         var x = C.Method;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "C.Method").WithLocation(5, 17)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var x = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        Assert.Equal("? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_AsFunctionType_StaticMethod_Duplicate()
    {
        var src = """
class C
{
    void M()
    {
        var x = C.Method;
    }
}

implicit extension E1 for C
{
    public static string Method() => throw null;
}
implicit extension E2 for C
{
    public static string Method() => throw null;
}
""";
        // PROTOTYPE we should be able to determine the function type, but this should fail (ambiguous method reference)
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,17): error CS8917: The delegate type could not be inferred.
            //         var x = C.Method;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "C.Method").WithLocation(5, 17)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var x = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        Assert.Equal("? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_AsFunctionType_StaticMethod_Difference()
    {
        var src = """
class C
{
    void M()
    {
        var x = C.Method;
    }
}

implicit extension E1 for C
{
    public static string Method() => throw null;
}
implicit extension E2 for C
{
    public static int Method(int i) => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,17): error CS8917: The delegate type could not be inferred.
            //         var x = C.Method;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "C.Method").WithLocation(5, 17)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var x = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        Assert.Equal("? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_AsFunctionType_StaticMethod_Duplicate_FromDifferentScopes()
    {
        var src = """
using N;

class C
{
    void M()
    {
        var x = C.Method;
    }
}

implicit extension E1 for C
{
    public static string Method() => throw null;
}

namespace N
{
    implicit extension E2 for C
    {
        public static string Method() => throw null;
    }
}
""";
        // PROTOTYPE we should be able to determine the function type, but this should fail (ambiguous method reference)
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using N;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1),
            // (7,17): error CS8917: The delegate type could not be inferred.
            //         var x = C.Method;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "C.Method").WithLocation(7, 17)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var x = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        Assert.Equal("? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_AsFunctionType_StaticMethod_Difference_FromDifferentScopes()
    {
        var src = """
using N;

class C
{
    void M()
    {
        var x = C.Method;
    }
}

implicit extension E1 for C
{
    public static int Method(int i) => throw null;
}

namespace N
{
    implicit extension E2 for C
    {
        public static string Method() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using N;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1),
            // (7,17): error CS8917: The delegate type could not be inferred.
            //         var x = C.Method;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "C.Method").WithLocation(7, 17)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var x = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        Assert.Equal("? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_AsFunctionType_StaticMethod_FromStaticUsing()
    {
        // PROTOTYPE should a static using of extended type bring the static extension members in scope?
        var src = """
using static C;

var x = Method;

class C { }

implicit extension E for C
{
    public static string Method() => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using static C;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static C;").WithLocation(1, 1),
            // (3,9): error CS0103: The name 'Method' does not exist in the current context
            // var x = Method;
            Diagnostic(ErrorCode.ERR_NameNotInContext, "Method").WithArguments("Method").WithLocation(3, 9)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var x = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        Assert.Equal("? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_AsFunctionType_StaticMethod_WithTypeArgument()
    {
        var src = """
class C
{
    void M()
    {
        var x = C.Method<int>;
    }
}

implicit extension E for C
{
    public static T Method<T>() => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE need to infer delegate type
        comp.VerifyDiagnostics(
            // (5,17): error CS8917: The delegate type could not be inferred.
            //         var x = C.Method<int>;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "C.Method<int>").WithLocation(5, 17)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var x = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        Assert.Equal("? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_AsFunctionType_StaticMethod_WithTypeArgument_WrongArity()
    {
        var src = """
class C
{
    void M()
    {
        var x = C.Method<int>;
    }
}

implicit extension E for C
{
    public static T Method<T, U>(U u) => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,19): error CS0117: 'C' does not contain a definition for 'Method'
            //         var x = C.Method<int>;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Method<int>").WithArguments("C", "Method").WithLocation(5, 19)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var x = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        Assert.Equal("? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_AsFunctionType_InstanceMethod()
    {
        var src = """
class C
{
    void M()
    {
        var x = this.Method;
    }
}

implicit extension E for C
{
    public string Method() => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE need to infer delegate type
        comp.VerifyDiagnostics(
            // (5,17): error CS8917: The delegate type could not be inferred.
            //         var x = this.Method;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "this.Method").WithLocation(5, 17)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var x = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        //Assert.Equal("System.Func<System.String> x", model.GetDeclaredSymbol(x).ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_AsFunctionType_StaticMethod_OnExtension()
    {
        var src = """
class C
{
    void M()
    {
        var x = E.Method;
    }
}

implicit extension E for C
{
    public static string Method() => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var x = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        Assert.Equal("System.Func<System.String> x", model.GetDeclaredSymbol(x).ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_AsFunctionType_StaticMethod_OnExtension_FromStaticUsing()
    {
        var src = """
using static E;

class C
{
    void M()
    {
        var x = Method;
    }
}

implicit extension E for C
{
    public static string Method() => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var x = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        Assert.Equal("System.Func<System.String> x", model.GetDeclaredSymbol(x).ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_AsFunctionType_InstanceMethod_OnExtension()
    {
        var src = """
class C { }

implicit extension E for C
{
    public string Method() => throw null;

    void M()
    {
        var x = this.Method;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var x = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        Assert.Equal("System.Func<System.String> x", model.GetDeclaredSymbol(x).ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly), AlwaysSkip = "PROTOTYPE: crash when binding foreach")]
    public void ExtensionMemberLookup_PatternBasedForEach_NoMethod()
    {
        var src = """
foreach (var x in new C())
{
    System.Console.Write(x);
    break;
}

class C { }
class D { }

implicit extension E1 for C
{
    public D GetEnumerator() => new D();
}
implicit extension E2 for D
{
    public bool MoveNext() => true;
    public int Current => 42;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,19): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
            // foreach (var x in new C())
            Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(1, 19)
            );
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "42");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var loop = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
        Assert.Null(model.GetForEachStatementInfo(loop).GetEnumeratorMethod);
        Assert.Null(model.GetForEachStatementInfo(loop).MoveNextMethod);
        Assert.Null(model.GetForEachStatementInfo(loop).CurrentProperty);
    }

    [ConditionalFact(typeof(CoreClrOnly), AlwaysSkip = "PROTOTYPE: crash when binding foreach")]
    public void ExtensionMemberLookup_PatternBasedForEach_NoApplicableMethod()
    {
        var src = """
foreach (var x in new C())
{
    System.Console.Write(x);
    break;
}

class C
{
    public void GetEnumerator(int notApplicable) { } // not applicable
}
class D { }

implicit extension E1 for C
{
    public D GetEnumerator() => new D();
}
implicit extension E2 for D
{
    public bool MoveNext() => true;
    public int Current => 42;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,19): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
            // foreach (var x in new C())
            Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(1, 19)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var loop = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
        Assert.Null(model.GetForEachStatementInfo(loop).GetEnumeratorMethod);
        Assert.Null(model.GetForEachStatementInfo(loop).MoveNextMethod);
        Assert.Null(model.GetForEachStatementInfo(loop).CurrentProperty);
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBasedForEach_WrongArity()
    {
        var src = """
using System.Collections;

foreach (var x in new C()) { }

class C { }

implicit extension E for C
{
    public IEnumerator GetEnumerator<T>() => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,19): error CS0411: The type arguments for method 'E.GetEnumerator<T>()' cannot be inferred from the usage. Try specifying the type arguments explicitly.
            // foreach (var x in new C()) { }
            Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "new C()").WithArguments("E.GetEnumerator<T>()").WithLocation(3, 19),
            // (3,19): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
            // foreach (var x in new C()) { }
            Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(3, 19)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var loop = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
        Assert.Null(model.GetForEachStatementInfo(loop).GetEnumeratorMethod);
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBasedForEach_NonInvocable()
    {
        var src = """
using System.Collections;

foreach (var x in new C()) { }

class C { }

implicit extension E for C
{
    public IEnumerator GetEnumerator => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,19): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
            // foreach (var x in new C()) { }
            Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(3, 19)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var loop = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
        Assert.Null(model.GetForEachStatementInfo(loop).GetEnumeratorMethod);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_PatternBasedDeconstruct_NoMethod()
    {
        var src = """
var (x, y) = new C();
System.Console.Write((x, y));

class C { }

implicit extension E for C
{
    public void Deconstruct(out int i, out int j)
    {
        i = 42;
        j = 43;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE confirm when spec'ing pattern-based deconstruction
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "(42, 43)");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var deconstruction = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().First();

        Assert.Equal("void E.Deconstruct(out System.Int32 i, out System.Int32 j)",
            model.GetDeconstructionInfo(deconstruction).Method.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_PatternBasedDeconstruct_FallbackToExtensionMethod()
    {
        // If the method from the extension type is not applicable, we fall back
        // to a Deconstruct extension method
        var src = """
var (x, y) = new C();
System.Console.Write((x, y));

public class C { }

implicit extension E1 for C
{
    public void Deconstruct(int inapplicable) => throw null;
}

public static class E2
{
    public static void Deconstruct(this C c, out int i, out int j)
    {
        i = 42;
        j = 43;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE confirm when spec'ing pattern-based deconstruction
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "(42, 43)");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var deconstruction = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().First();

        Assert.Equal("void E2.Deconstruct(this C c, out System.Int32 i, out System.Int32 j)",
            model.GetDeconstructionInfo(deconstruction).Method.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_PatternBasedDeconstruct_DelegateTypeProperty()
    {
        var src = """
var (x, y) = new C();

class C { }

delegate void D(out int i, out int j);

implicit extension E for C
{
    public D Deconstruct => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE revisit pattern-based deconstruction
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "(42, 43)");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var deconstruction = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().First();

        Assert.Equal("void D.Invoke(out System.Int32 i, out System.Int32 j)",
            model.GetDeconstructionInfo(deconstruction).Method.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly), AlwaysSkip = "PROTOTYPE Asserts in BindDynamicInvocation")]
    public void ExtensionMemberLookup_PatternBasedDeconstruct_DynamicProperty()
    {
        var src = """
var (x, y) = new C();

class C { }

implicit extension E for C
{
    public dynamic Deconstruct => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE revisit pattern-based deconstruction
        comp.VerifyDiagnostics(
            // (1,6): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x'.
            // var (x, y) = new C();
            Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x").WithArguments("x").WithLocation(1, 6),
            // (1,9): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'y'.
            // var (x, y) = new C();
            Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "y").WithArguments("y").WithLocation(1, 9),
            // (1,14): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'C', with 2 out parameters and a void return type.
            // var (x, y) = new C();
            Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(1, 14)
            );
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "(42, 43)");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var deconstruction = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().First();

        Assert.Null(model.GetDeconstructionInfo(deconstruction).Method);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_PatternBasedDeconstruct_NoApplicableMethod()
    {
        var src = """
var (x, y) = new C();
System.Console.Write((x, y));

class C
{
    public void Deconstruct() { } // not applicable
}

implicit extension E for C
{
    public void Deconstruct(out int i, out int j)
    {
        i = 42;
        j = 43;
    }
}
""";
        // PROTOTYPE confirm when spec'ing pattern-based deconstruction
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "(42, 43)");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var deconstruction = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().First();

        Assert.Equal("void E.Deconstruct(out System.Int32 i, out System.Int32 j)",
            model.GetDeconstructionInfo(deconstruction).Method.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_PatternBasedDispose_Async_NoMethod()
    {
        var src = """
using System.Threading.Tasks;

/*<bind>*/
await using var x = new C();
/*</bind>*/

class C { }

implicit extension E for C
{
    public async Task DisposeAsync()
    {
        System.Console.Write("RAN");
        await Task.Yield();
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE confirm when spec'ing pattern-based disposal
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "RAN");

        //        string expectedOperationTree = """
        //IUsingDeclarationOperation(IsAsynchronous: True, DisposeMethod: System.Threading.Tasks.Task E.DisposeAsync()) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'await using ...  = new C();')
        //DeclarationGroup:
        //  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'await using ...  = new C();')
        //    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var x = new C()')
        //      Declarators:
        //          IVariableDeclaratorOperation (Symbol: C x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x = new C()')
        //            Initializer:
        //              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
        //                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
        //                  Arguments(0)
        //                  Initializer:
        //                    null
        //      Initializer:
        //        null
        //""";
        //        var expectedDiagnostics = DiagnosticDescription.None;

        //        VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(src,
        //            expectedOperationTree, expectedDiagnostics, targetFramework: TargetFramework.Net70);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_PatternBasedDispose_Async_NoApplicableMethod()
    {
        var src = """
using System.Threading.Tasks;

/*<bind>*/
await using var x = new C();
/*</bind>*/

class C
{
    public Task DisposeAsync(int notApplicable) => throw null; // not applicable
}

implicit extension E for C
{
    public async Task DisposeAsync()
    {
        System.Console.Write("RAN");
        await Task.Yield();
    }
}
""";
        // PROTOTYPE confirm when spec'ing pattern-based disposal
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "RAN");

        // PROTOTYPE verify IOperation
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBasedDispose_RefStruct()
    {
        var src = """
using var x = new S();

ref struct S { }

implicit extension E for S
{
    public void Dispose()
    {
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,1): error CS1674: 'S': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
            // using var x = new S();
            Diagnostic(ErrorCode.ERR_NoConvToIDisp, "using var x = new S();").WithArguments("S").WithLocation(1, 1),
            // (5,26): error CS9305: The extended type may not be dynamic, a pointer, a ref struct, or an extension.
            // implicit extension E for S
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "S").WithLocation(5, 26)
            );
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_PatternBasedFixed_NoMethod()
    {
        var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable())
        {
            System.Console.WriteLine(p[1]);
        }
    }
}

class Fixable { }

implicit extension E for Fixable
{
    public ref int GetPinnableReference()
    {
        return ref (new int[] { 1, 2, 3 })[0];
    }
}
";
        var comp = CreateCompilation(text, targetFramework: TargetFramework.Net70, options: TestOptions.UnsafeReleaseExe);
        // PROTOTYPE confirm when spec'ing pattern-based fixed
        comp.VerifyDiagnostics();

        // PROTOTYPE Execute when adding support for emitting non-static members
        //        var compVerifier = CompileAndVerify(comp, expectedOutput: @"2", verify: Verification.Fails);

        //        compVerifier.VerifyIL("C.Main", """
        //{
        //  // Code size       33 (0x21)
        //  .maxstack  2
        //  .locals init (pinned int& V_0)
        //  IL_0000:  newobj     "Fixable..ctor()"
        //  IL_0005:  dup
        //  IL_0006:  brtrue.s   IL_000d
        //  IL_0008:  pop
        //  IL_0009:  ldc.i4.0
        //  IL_000a:  conv.u
        //  IL_000b:  br.s       IL_0015
        //  IL_000d:  call       "ref int E.GetPinnableReference()"
        //  IL_0012:  stloc.0
        //  IL_0013:  ldloc.0
        //  IL_0014:  conv.u
        //  IL_0015:  ldc.i4.4
        //  IL_0016:  add
        //  IL_0017:  ldind.i4
        //  IL_0018:  call       "void System.Console.WriteLine(int)"
        //  IL_001d:  ldc.i4.0
        //  IL_001e:  conv.u
        //  IL_001f:  stloc.0
        //  IL_0020:  ret
        //}
        //""");
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_PatternBasedFixed_NoApplicableMethod()
    {
        var src = """
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable())
        {
            System.Console.WriteLine(p[1]);
        }
    }
}

class Fixable
{
    public ref int GetPinnableReference(int notApplicable) => throw null; // not applicable
}

implicit extension E for Fixable
{
    public ref int GetPinnableReference()
    {
        return ref (new int[] { 1, 2, 3 })[0];
    }
}
""";

        // PROTOTYPE confirm when spec'ing pattern-based fixed
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.UnsafeReleaseExe);
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "2");

        // PROTOTYPE verify IOperation
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBasedFixed_Static()
    {
        var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable())
        {
        }
    }
}

class Fixable { }

implicit extension E for Fixable
{
    public static ref int GetPinnableReference() => throw null;
}
";

        var comp = CreateCompilation(text, targetFramework: TargetFramework.Net70, options: TestOptions.UnsafeReleaseExe);
        // PROTOTYPE confirm when spec'ing pattern-based fixed
        comp.VerifyDiagnostics(
            // (6,25): error CS0176: Member 'E.GetPinnableReference()' cannot be accessed with an instance reference; qualify it with a type name instead
            //         fixed (int* p = new Fixable())
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "new Fixable()").WithArguments("E.GetPinnableReference()").WithLocation(6, 25),
            // (6,25): error CS8385: The given expression cannot be used in a fixed statement
            //         fixed (int* p = new Fixable())
            Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "new Fixable()").WithLocation(6, 25)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBasedAwait_NoMethod()
    {
        var text = @"
using System;
using System.Runtime.CompilerServices;

int i = await new C();
System.Console.Write(i);

class C { }
class D { }

implicit extension E1 for C
{
    public D GetAwaiter() => new D();
}

implicit extension E2 for D : INotifyCompletion
{
    public bool IsCompleted => true;
    public int GetResult() => 42;
    public void OnCompleted(Action continuation) => throw null;
}
";

        var comp = CreateCompilation(text, targetFramework: TargetFramework.Net70);
        // PROTOTYPE confirm when spec'ing pattern-based await
        comp.VerifyDiagnostics(
            // (5,9): error CS0117: 'D' does not contain a definition for 'IsCompleted'
            // int i = await new C();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "await new C()").WithArguments("D", "IsCompleted").WithLocation(5, 9),
            // (16,31): error CS9307: A base extension must be an extension type.
            // implicit extension E2 for D : INotifyCompletion
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "INotifyCompletion").WithLocation(16, 31)
            );
        // PROTOTYPE Execute when adding support for emitting non-static members and adding interfaces
        //CompileAndVerify(comp, expectedOutput: "42");
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBasedAwait_NoApplicableGetAwaiterMethod()
    {
        var text = @"
using System;
using System.Runtime.CompilerServices;

int i = await new C();
System.Console.Write(i);

class C
{
    public D GetAwaiter(int notApplicable) => throw null; // not applicable
}
class D { }

implicit extension E1 for C
{
    public D GetAwaiter() => new D();
}

implicit extension E2 for D : INotifyCompletion
{
    public bool IsCompleted => true;
    public int GetResult() => 42;
    public void OnCompleted(Action continuation) => throw null;
}
";

        // PROTOTYPE confirm when spec'ing pattern-based await
        // PROTOTYPE Revisit when adding support for emitting non-static members and adding interfaces
        var comp = CreateCompilation(text, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,9): error CS0117: 'D' does not contain a definition for 'IsCompleted'
            // int i = await new C();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "await new C()").WithArguments("D", "IsCompleted").WithLocation(5, 9),
            // (19,31): error CS9307: A base extension must be an extension type.
            // implicit extension E2 for D : INotifyCompletion
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "INotifyCompletion").WithLocation(19, 31)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBasedIndexIndexer_NoIndexer()
    {
        var src = """
var c = new C();

/*<bind>*/
_ = c[^1];
/*</bind>*/

class C { }

implicit extension E for C
{
    public int this[int i]
    {
        get
        {
            System.Console.Write("indexer ");
            return 0;
        }
    }

    public int Length
    {
        get
        {
            System.Console.Write("length ");
            return 42;
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE revisit as part of "indexer access" section
        comp.VerifyDiagnostics(
            // (4,5): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            // _ = c[^1];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "c[^1]").WithArguments("C").WithLocation(4, 5)
            );
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "length indexer");

        //        string expectedOperationTree = """
        //ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '_ = c[^1]')
        //Left:
        //  IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
        //Right:
        //  IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: 'c[^1]')
        //    Instance:
        //      ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
        //    Argument:
        //      IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index) (Syntax: '^1')
        //        Operand:
        //          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        //    LengthSymbol: System.Int32 E.Length { get; }
        //    IndexerSymbol: System.Int32 E.this[System.Int32 i] { get; }
        //""";
        //        var expectedDiagnostics = DiagnosticDescription.None;

        //        VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(src,
        //            expectedOperationTree, expectedDiagnostics, targetFramework: TargetFramework.Net70);
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBasedIndexIndexer_NoApplicableIndexer()
    {
        var src = """
var c = new C();

/*<bind>*/
_ = c[^1];
/*</bind>*/

class C
{
    public int this[string notApplicable] { } // not applicable
}

implicit extension E for C
{
    public int this[int i]
    {
        get
        {
            System.Console.Write("indexer ");
            return 0;
        }
    }

    public int Length
    {
        get
        {
            System.Console.Write("length ");
            return 42;
        }
    }
}
""";

        // PROTOTYPE this scenario should work (based on updated "indexer access" rules)
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,7): error CS1503: Argument 1: cannot convert from 'System.Index' to 'string'
            // _ = c[^1];
            Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "string").WithLocation(4, 7),
            // (9,16): error CS0548: 'C.this[string]': property or indexer must have at least one accessor
            //     public int this[string notApplicable] { } // not applicable
            Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "this").WithArguments("C.this[string]").WithLocation(9, 16)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBasedRangeIndexer_NoMethod()
    {
        var src = """
var c = new C();

/*<bind>*/
_ = c[1..^1];
/*</bind>*/

class C { }

implicit extension E for C
{
    public int Slice(int i, int j)
    {
        System.Console.Write("slice ");
        return 0;
    }

    public int Length
    {
        get
        {
            System.Console.Write("length ");
            return 42;
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE revisit pattern-based implicit indexing
        comp.VerifyDiagnostics(
            // (4,5): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            // _ = c[1..^1];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "c[1..^1]").WithArguments("C").WithLocation(4, 5)
            );
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "length slice");

        //        string expectedOperationTree = """
        //ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '_ = c[1..^1]')
        //Left:
        //  IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
        //Right:
        //  IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: 'c[1..^1]')
        //    Instance:
        //      ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
        //    Argument:
        //      IRangeOperation (OperationKind.Range, Type: System.Range) (Syntax: '1..^1')
        //        LeftOperand:
        //          IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Index System.Index.op_Implicit(System.Int32 value)) (OperationKind.Conversion, Type: System.Index, IsImplicit) (Syntax: '1')
        //            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Index System.Index.op_Implicit(System.Int32 value))
        //            Operand:
        //              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        //        RightOperand:
        //          IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index) (Syntax: '^1')
        //            Operand:
        //              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        //    LengthSymbol: System.Int32 E.Length { get; }
        //    IndexerSymbol: System.Int32 E.Slice(System.Int32 i, System.Int32 j)
        //""";
        //        var expectedDiagnostics = DiagnosticDescription.None;

        //        VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(src,
        //            expectedOperationTree, expectedDiagnostics, targetFramework: TargetFramework.Net70);
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBasedRangeIndexer_NoApplicableMethod()
    {
        var src = """
var c = new C();

/*<bind>*/
_ = c[1..^1];
/*</bind>*/

class C
{
    public int Slice(int notApplicable) => throw null; // not applicable
}

implicit extension E for C
{
    public int Slice(int i, int j)
    {
        System.Console.Write("slice ");
        return 0;
    }

    public int Length
    {
        get
        {
            System.Console.Write("length ");
            return 42;
        }
    }
}
""";

        // PROTOTYPE this scenario should work (based on updated "indexer access" rules)
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,5): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            // _ = c[1..^1];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "c[1..^1]").WithArguments("C").WithLocation(4, 5)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_Patterns()
    {
        var src = """
var c = new C();

/*<bind>*/
_ = c is { Property: 42 };
/*</bind>*/

class C { }

implicit extension E for C
{
    public int Property
    {
        get
        {
            System.Console.Write("property");
            return 42;
        }
    }
}
""";
        // PROTOTYPE need to decide whether extensions apply here
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,12): error CS0117: 'C' does not contain a definition for 'Property'
            // _ = c is { Property: 42 };
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("C", "Property").WithLocation(4, 12)
            );
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "property");
    }

    [ConditionalFact(typeof(NoUsedAssembliesValidation))] // PROTOTYPE enable once we can lower/emit for non-static scenarios
    public void ExtensionMemberLookup_ObjectInitializer()
    {
        var src = """
/*<bind>*/
_ = new C() { Property = 42 };
/*</bind>*/

class C { }

implicit extension E for C
{
    public int Property
    {
        set
        {
            System.Console.Write("property");
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE need to decide whether extensions apply here
        comp.VerifyDiagnostics(
            // (2,15): error CS0117: 'C' does not contain a definition for 'Property'
            // _ = new C() { Property = 42 };
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("C", "Property").WithLocation(2, 15)
            );
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "property");
    }

    [ConditionalFact(typeof(NoUsedAssembliesValidation))] // PROTOTYPE enable once we can lower/emit for non-static scenarios
    public void ExtensionMemberLookup_With()
    {
        var src = """
/*<bind>*/
_ = new S() with { Property = 42 };
/*</bind>*/

struct S { }

implicit extension E for S
{
    public int Property
    {
        set
        {
            System.Console.Write("property");
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE need to decide whether extensions apply here
        comp.VerifyDiagnostics(
            // (2,20): error CS0117: 'S' does not contain a definition for 'Property'
            // _ = new S() with { Property = 42 };
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("S", "Property").WithLocation(2, 20)
            );
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "property");

    }

    [Fact]
    public void ExtensionMemberLookup_CollectionInitializer_NoMethod()
    {
        var src = """
using System.Collections;
using System.Collections.Generic;

/*<bind>*/
_ = new C() { 42 };
/*</bind>*/

class C : IEnumerable<int>, IEnumerable
{
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
}

implicit extension E for C
{
    public void Add(int i)
    {
        System.Console.Write("add");
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE confirm when spec'ing pattern-based collection initializer
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "add");

        //        string expectedOperationTree = """
        //ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: '_ = new C() { 42 }')
        //Left:
        //  IDiscardOperation (Symbol: C _) (OperationKind.Discard, Type: C) (Syntax: '_')
        //Right:
        //  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { 42 }')
        //    Arguments(0)
        //    Initializer:
        //      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ 42 }')
        //        Initializers(1):
        //            IInvocationOperation ( void E.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '42')
        //              Instance Receiver:
        //                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
        //              Arguments(1):
        //                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '42')
        //                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
        //                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        //                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        //""";
        //        var expectedDiagnostics = DiagnosticDescription.None;

        //        VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(src,
        //            expectedOperationTree, expectedDiagnostics, targetFramework: TargetFramework.Net70);
    }

    [Fact]
    public void ExtensionMemberLookup_CollectionInitializer_NoApplicableMethod()
    {
        var src = """
using System.Collections;
using System.Collections.Generic;

/*<bind>*/
_ = new C() { 42 };
/*</bind>*/

class C : IEnumerable<int>, IEnumerable
{
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(string notApplicable) => throw null;
}

implicit extension E for C
{
    public void Add(int i)
    {
        System.Console.Write("add");
    }
}
""";

        // PROTOTYPE confirm when spec'ing pattern-based collection initializer
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "add");
    }

    [Fact]
    public void ExtensionMemberLookup_InterpolationHandler_Simple()
    {
        var src = """
/*<bind>*/
_ = f($"{(object)1} {f2()}");
/*</bind>*/

static int f(InterpolationHandler s) => 0;
static string f2() => "hello";

[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount) => throw null;
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void ExtensionMemberLookup_InterpolationHandler_AppendLiteralExtensionMethod()
    {
        var src = """
/*<bind>*/
_ = f($"{(object)1} {f2()}");
/*</bind>*/

static int f(InterpolationHandler s) => 0;
static string f2() => "hello";

[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount) => throw null;
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class Extensions
{
    public static void AppendLiteral(this InterpolationHandler ih, string value) { }
}
""";

        // Interpolation handlers don't allow extension methods
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,20): error CS1061: 'InterpolationHandler' does not contain a definition for 'AppendLiteral' and no accessible extension method 'AppendLiteral' accepting a first argument of type 'InterpolationHandler' could be found (are you missing a using directive or an assembly reference?)
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, " ").WithArguments("InterpolationHandler", "AppendLiteral").WithLocation(2, 20),
            // (2,20): error CS8941: Interpolated string handler method '?.()' is malformed. It does not return 'void' or 'bool'.
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, " ").WithArguments("?.()").WithLocation(2, 20)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_InterpolationHandler_AppendLiteralExtensionTypeMethod()
    {
        var src = """
/*<bind>*/
_ = f($"{(object)1} {f2()}");
/*</bind>*/

static int f(InterpolationHandler s) => 0;
static string f2() => "hello";

[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount) => throw null;
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public implicit extension E for InterpolationHandler
{
    public void AppendLiteral(string value) { }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,20): error CS1061: 'InterpolationHandler' does not contain a definition for 'AppendLiteral' and no accessible extension method 'AppendLiteral' accepting a first argument of type 'InterpolationHandler' could be found (are you missing a using directive or an assembly reference?)
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, " ").WithArguments("InterpolationHandler", "AppendLiteral").WithLocation(2, 20),
            // (2,20): error CS8941: Interpolated string handler method '?.()' is malformed. It does not return 'void' or 'bool'.
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, " ").WithArguments("?.()").WithLocation(2, 20)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_InterpolationHandler_AppendFormattedExtensionMethod()
    {
        var src = """
/*<bind>*/
_ = f($"{(object)1} {f2()}");
/*</bind>*/

static int f(InterpolationHandler s) => 0;
static string f2() => "hello";

[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount) => throw null;
    public void AppendLiteral(string value) { }
}

public static class Extensions
{
    public static void AppendFormatted<T>(this InterpolationHandler ih, T hole, int alignment = 0, string format = null) { }
}
""";

        // Interpolation handlers don't allow extension methods
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,9): error CS1061: 'InterpolationHandler' does not contain a definition for 'AppendFormatted' and no accessible extension method 'AppendFormatted' accepting a first argument of type 'InterpolationHandler' could be found (are you missing a using directive or an assembly reference?)
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "{(object)1}").WithArguments("InterpolationHandler", "AppendFormatted").WithLocation(2, 9),
            // (2,9): error CS8941: Interpolated string handler method '?.()' is malformed. It does not return 'void' or 'bool'.
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, "{(object)1}").WithArguments("?.()").WithLocation(2, 9),
            // (2,21): error CS1061: 'InterpolationHandler' does not contain a definition for 'AppendFormatted' and no accessible extension method 'AppendFormatted' accepting a first argument of type 'InterpolationHandler' could be found (are you missing a using directive or an assembly reference?)
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "{f2()}").WithArguments("InterpolationHandler", "AppendFormatted").WithLocation(2, 21),
            // (2,21): error CS8941: Interpolated string handler method '?.()' is malformed. It does not return 'void' or 'bool'.
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, "{f2()}").WithArguments("?.()").WithLocation(2, 21)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_InterpolationHandler_AppendFormattedExtensionTypeMethod()
    {
        var src = """
/*<bind>*/
_ = f($"{(object)1} {f2()}");
/*</bind>*/

static int f(InterpolationHandler s) => 0;
static string f2() => "hello";

[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount) => throw null;
    public void AppendLiteral(string value) { }
}

public implicit extension E for InterpolationHandler
{
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) { }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,9): error CS1061: 'InterpolationHandler' does not contain a definition for 'AppendFormatted' and no accessible extension method 'AppendFormatted' accepting a first argument of type 'InterpolationHandler' could be found (are you missing a using directive or an assembly reference?)
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "{(object)1}").WithArguments("InterpolationHandler", "AppendFormatted").WithLocation(2, 9),
            // (2,9): error CS8941: Interpolated string handler method '?.()' is malformed. It does not return 'void' or 'bool'.
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, "{(object)1}").WithArguments("?.()").WithLocation(2, 9),
            // (2,21): error CS1061: 'InterpolationHandler' does not contain a definition for 'AppendFormatted' and no accessible extension method 'AppendFormatted' accepting a first argument of type 'InterpolationHandler' could be found (are you missing a using directive or an assembly reference?)
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "{f2()}").WithArguments("InterpolationHandler", "AppendFormatted").WithLocation(2, 21),
            // (2,21): error CS8941: Interpolated string handler method '?.()' is malformed. It does not return 'void' or 'bool'.
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, "{f2()}").WithArguments("?.()").WithLocation(2, 21)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_Query_NoMethod()
    {
        var src = """
/*<bind>*/
string query = from x in new C() select x;
/*</bind>*/

System.Console.Write(query);

class C { }

implicit extension E for C
{
    public string Select(System.Func<C, C> selector) => "hello";
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "hello");

        //        string expectedOperationTree = """
        //IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'string quer ... ) select x;')
        //IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'string quer ... () select x')
        //  Declarators:
        //      IVariableDeclaratorOperation (Symbol: System.String query) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'query = fro ... () select x')
        //        Initializer:
        //          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= from x in ... () select x')
        //            ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.String) (Syntax: 'from x in n ... () select x')
        //              Expression:
        //                IInvocationOperation ( System.String E.Select(System.Func<C, C> selector)) (OperationKind.Invocation, Type: System.String, IsImplicit) (Syntax: 'select x')
        //                  Instance Receiver:
        //                    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
        //                      Arguments(0)
        //                      Initializer:
        //                        null
        //                  Arguments(1):
        //                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
        //                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<C, C>, IsImplicit) (Syntax: 'x')
        //                          Target:
        //                            IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'x')
        //                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x')
        //                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x')
        //                                  ReturnedValue:
        //                                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C) (Syntax: 'x')
        //                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        //                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        //  Initializer:
        //    null
        //""";
        //        VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(src,
        //            expectedOperationTree, DiagnosticDescription.None, targetFramework: TargetFramework.Net70);
    }

    [Fact]
    public void ExtensionMemberLookup_Query_NoApplicableMethod()
    {
        var src = """
/*<bind>*/
string query = from x in new C() select x;
/*</bind>*/

System.Console.Write(query);

class C
{
    public string Select(int notApplicable) => throw null; // not applicable
}

implicit extension E for C
{
    public string Select(System.Func<C, C> selector) => "hello";
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "hello");

        // PROTOTYPE verify IOperation
    }

    [Fact]
    public void ExtensionMemberLookup_NameOf_NoParameter()
    {
        var src = """
class C
{
    void M()
    {
        System.Console.Write(nameof());
    }
}

implicit extension E for C
{
    public string nameof() => throw null;
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,30): error CS0103: The name 'nameof' does not exist in the current context
            //         System.Console.Write(nameof());
            Diagnostic(ErrorCode.ERR_NameNotInContext, "nameof").WithArguments("nameof").WithLocation(5, 30)
            );
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMemberLookup_NameOf_SingleParameter()
    {
        var src = """
class C
{
    public static void Main()
    {
        string x = "";
        System.Console.Write(nameof(x));
    }
}

implicit extension E for C
{
    public string nameof(string s) => throw null;
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "x").VerifyDiagnostics();
    }

    [Fact]
    public void ExtensionMemberLookup_GotoLabel()
    {
        var src = """
class C
{
    void M()
    {
        goto label;
    }
}

implicit extension E for C
{
    public const int label = 42;
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,14): error CS0159: No such label 'label' within the scope of the goto statement
            //         goto label;
            Diagnostic(ErrorCode.ERR_LabelNotFound, "label").WithArguments("label").WithLocation(5, 14)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_LabelDeclaration()
    {
        var src = """
class C
{
    void M()
    {
label:;
    }
}

implicit extension E for C
{
    public const int label = 42;
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,1): warning CS0164: This label has not been referenced
            // label:;
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "label").WithLocation(5, 1)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_SimpleTypeNameAsTypeConstraint()
    {
        var src = """
class C<T> where T : ExtensionType
{
}

implicit extension E<T> for C<T> where T : ExtensionType
{
    public class ExtensionType { }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,22): error CS0246: The type or namespace name 'ExtensionType' could not be found (are you missing a using directive or an assembly reference?)
            // class C<T> where T : ExtensionType
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ExtensionType").WithArguments("ExtensionType").WithLocation(1, 22),
            // (5,20): error CS0314: The type 'T' cannot be used as type parameter 'T' in the generic type or method 'C<T>'. There is no boxing conversion or type parameter conversion from 'T' to 'ExtensionType'.
            // implicit extension E<T> for C<T> where T : ExtensionType
            Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "E").WithArguments("C<T>", "ExtensionType", "T", "T").WithLocation(5, 20),
            // (5,44): error CS0246: The type or namespace name 'ExtensionType' could not be found (are you missing a using directive or an assembly reference?)
            // implicit extension E<T> for C<T> where T : ExtensionType
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ExtensionType").WithArguments("ExtensionType").WithLocation(5, 44)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_AliasQualifiedName()
    {
        var src = """
using C1 = C;

C1::ExtensionType.M(); // 1
C1.ExtensionType.M();

class C { }

implicit extension E for C
{
    public class ExtensionType
    {
        public static void M() { }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (3,1): error CS0431: Cannot use alias 'C1' with '::' since the alias references a type. Use '.' instead.
            // C1::ExtensionType.M(); // 1
            Diagnostic(ErrorCode.ERR_ColColWithTypeAlias, "C1").WithArguments("C1").WithLocation(3, 1)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_TypeParameter()
    {
        var src = """
class C<T>
{
    explicit extension R for T
    {
        void M1() { }

        class Type
        {
            static void M2() { }
        }
    }

    void M3()
    {
        T.M1();
        T.Type.M2();
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (15,9): error CS0704: Cannot do non-virtual member lookup in 'T' because it is a type parameter
            //         T.M1();
            Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T").WithArguments("T").WithLocation(15, 9),
            // (16,9): error CS0704: Cannot do non-virtual member lookup in 'T' because it is a type parameter
            //         T.Type.M2();
            Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T").WithArguments("T").WithLocation(16, 9)
            );

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var invocation = GetSyntax<MemberAccessExpressionSyntax>(tree, "T.M1");
        Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        Assert.Empty(model.GetMemberGroup(invocation));

        var type = GetSyntax<MemberAccessExpressionSyntax>(tree, "T.Type");
        Assert.Null(model.GetSymbolInfo(type).Symbol);
        Assert.Empty(model.GetMemberGroup(type));
    }

    [Fact]
    public void ExtensionMemberLookup_DuplicateFieldNamesInUnderlyingType()
    {
        var ilSource = """
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .field public int32 'member'
    .field public string 'member'
}
""";

        var src = """
class D
{
    void M(C c)
    {
        _ = c.member;
    }
}

implicit extension E for C
{
    public string member => throw null;
}
""";
        var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,15): error CS0229: Ambiguity between 'C.member' and 'C.member'
            //         _ = c.member;
            Diagnostic(ErrorCode.ERR_AmbigMember, "member").WithArguments("C.member", "C.member").WithLocation(5, 15)
            );
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ParameterCapturing_054_ColorColor_QualifiedName_Type_WithExtension()
    {
        var source = @"
class Color
{
    public class C1(Color Color)
    {
        public object M1(object input)
        {
            if (input is Color.Red)
            {
                return ""Red"";
            }

            return ""Blue"";
        }
    }
}

implicit extension E for Color
{
    public class Red;
}

class Program
{
    static void Main()
    {
        var c1 = new Color.C1(default);
        object val = c1.M1(new Color.Red());
        System.Console.Write(val);
    }
}
";
        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net70);

        // PROTOTYPE missing WRN_UnreadPrimaryConstructorParameter
        CompileAndVerify(comp, expectedOutput: @"Red", verify: Verification.FailsPEVerify).VerifyDiagnostics(
            //// (4,27): warning CS9113: Parameter 'Color' is unread.
            ////     public class C1(Color Color)
            //Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(4, 27)
            );

        //Assert.Empty(comp.GetTypeByMetadataName("Color+C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_TypeReceiver_Simple()
    {
        // We look at methods in extension types if the method group on type has no applicable candidates
        var source = """
C.M(42);

class C
{
    public static void M() => throw null;
}

implicit extension E for C
{
    public static void M(int i)
    {
        System.Console.Write("E.M");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);

        CompileAndVerify(comp, expectedOutput: "E.M", verify: Verification.FailsPEVerify).VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Equal("void E.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.M()" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_InstanceReceiver_Simple()
    {
        // We look at methods in extension types if the method group on type has no applicable candidates
        var source = """
new C().M(42);

class C
{
    public void M() => throw null;
}

implicit extension E for C
{
    public void M(int i)
    {
        System.Console.Write("E.M");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);

        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "E.M", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void E.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.M()" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_TypeReceiver_Overloads()
    {
        // When we look at methods in extension types, we perform overload resolution
        var source = """
C.M(42);

class C
{
    public static void M() => throw null;
}

implicit extension E for C
{
    public static void M(int i)
    {
        System.Console.Write($"E.M({i})");
    }

    public static void M(string s) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);

        CompileAndVerify(comp, expectedOutput: "E.M(42)", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Equal("void E.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.M()" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_InstanceReceiver_Overloads()
    {
        // When we look at methods in extension types, we perform overload resolution
        var source = """
new C().M(42);

class C
{
    public void M() => throw null;
}

implicit extension E for C
{
    public void M(int i)
    {
        System.Console.Write($"E.M({i})");
    }

    public static void M(string s) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);

        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "E.M(42)", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void E.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.M()" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_TypeReceiver_NextScope()
    {
        // If overload resolution on extension type methods yields no applicable candidates,
        // we look in the next scope.
        var source = """
using N;

C.M(42);

class C
{
    public static void M() => throw null;
}

implicit extension E1 for C
{
    public static void M(string s) => throw null;
}

namespace N
{
    implicit extension E2 for C
    {
        public static void M(int i)
        {
            System.Console.Write($"E2.M({i})");
        }
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);

        CompileAndVerify(comp, expectedOutput: "E2.M(42)", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Equal("void N.E2.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.M()" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_InstanceReceiver_NextScope()
    {
        // If overload resolution on extension type methods yields no applicable candidates,
        // we look in the next scope.
        var source = """
using N;

new C().M(42);

class C
{
    public void M() => throw null;
}

implicit extension E1 for C
{
    public void M(string s) => throw null;
}

namespace N
{
    implicit extension E2 for C
    {
        public void M(int i)
        {
            System.Console.Write($"E2.M({i})");
        }
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);

        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "E2.M(42)", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void N.E2.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.M()" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_ExtensionTypePriority()
    {
        // The method from the extension type comes before the extension method
        var source = """
new C().M(42);

class C
{
    public void M() => throw null;
}

implicit extension E1 for C
{
    public void M(int i)
    {
        System.Console.Write($"E1.M({i})");
    }
}

static class E2
{
    public void M(this C c, int i) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);

        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "E1.M(42)", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void E1.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.M()", "void C.M(System.Int32 i)" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_FallbackToExtensionMethod()
    {
        // The extension method is picked up if extension type candidates were not applicable
        var source = """
new C().M(42);

class C
{
    public static void M() => throw null;
}

implicit extension E1 for C
{
    public void M(string s) => throw null;
    public void M(char c) => throw null;
}

static class E2
{
    public static void M(this C c, int i)
    {
        System.Console.Write($"E2.M({i})");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);

        CompileAndVerify(comp, expectedOutput: "E2.M(42)", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void C.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.M()", "void C.M(System.Int32 i)" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [Fact]
    public void ExtensionInvocation_InaccessibleExtensionTypeMember()
    {
        var source = """
C.M(42);

class C
{
    public static void M() => throw null;
}

implicit extension E1 for C
{
    protected static void M(int i) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // 0.cs(1,3): error CS1501: No overload for method 'M' takes 1 arguments
            // C.M(42);
            Diagnostic(ErrorCode.ERR_BadArgCount, "M").WithArguments("M", "1").WithLocation(1, 3)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal(new[] { "void C.M()" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_InaccessibleExtensionTypeMember_FallbackToExtensionMethod()
    {
        // Extension method is picked up after inaccessible extension type member was found
        var source = """
new C().M(42);

class C
{
    public void M() => throw null;
}

implicit extension E1 for C
{
    protected void M(int i) => throw null;
}

static class E2
{
    public static void M(this C c, int i)
    {
        System.Console.Write($"E2.M({i})");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);

        CompileAndVerify(comp, expectedOutput: "E2.M(42)", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void C.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.M()", "void C.M(System.Int32 i)" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [Fact]
    public void ExtensionInvocation_SimpleName()
    {
        // Extension invocation comes into play on an invocation on a member access but not an invocation on a simple name
        var source = """
class C
{
    public void M() => throw null;

    void M2()
    {
        M(42); // 1
    }
}

implicit extension E for C
{
    public void M(int i) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // 0.cs(7,9): error CS1501: No overload for method 'M' takes 1 arguments
            //         M(42); // 1
            Diagnostic(ErrorCode.ERR_BadArgCount, "M").WithArguments("M", "1").WithLocation(7, 9)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "M(42)");
        Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        Assert.Empty(model.GetMemberGroup(invocation));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_OnlyDelegateFieldExists()
    {
        // Invocable fields are considered during extension invocation
        var source = """
E.Field = (i) => { System.Console.Write($"ran({i})"); };
C.Field(42);

class C { }

delegate void D(int i);

implicit extension E for C
{
    public static D Field;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "ran(42)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Field");
        Assert.Equal("D E.Field", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_OnlyIntegerFieldExists()
    {
        var source = """
C.Field(42);

public class C { }

public implicit extension E for C
{
    public static int Field = 0;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,3): error CS0117: 'C' does not contain a definition for 'Field'
            // C.Field(42);
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Field").WithArguments("C", "Field").WithLocation(1, 3)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Field");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_OnlyDelegatePropertyExists()
    {
        // Invocable properties are considered during extension invocation
        var source = """
C.Property(42);

class C { }

delegate void D(int i);

implicit extension E for C
{
    public static D Property
    {
        get
        {
            return (i) => { System.Console.Write($"ran({i})"); };
        }
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "ran(42)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Property");
        Assert.Equal("D E.Property { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_OnlyDynamicPropertyExists()
    {
        // Invocable properties are considered during extension invocation
        var source = """
C.Property(42);

class C { }

implicit extension E for C
{
    public static dynamic Property
    {
        get
        {
            return (System.Action<int>)((i) => { System.Console.Write($"ran({i})"); });
        }
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "ran(42)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Property");
        Assert.Equal("dynamic E.Property { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [ConditionalFact(typeof(CoreClrOnly), AlwaysSkip = "fails in emit stage in used assemblies leg")]
    public void ExtensionInvocation_OnlyEventExists()
    {
        // Events are considered during extension invocation
        var source = """
new C().Event(42);

class C { }

delegate void D(int i);

implicit extension E for C
{
    public event D Event { add => throw null; remove => throw null; }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        // PROTOTYPE Execute when adding support for emitting non-static members
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().Event");
        Assert.Equal("event D E.Event", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_ArgumentName()
    {
        // Instance method with incompatible parameter name is skipped in favor of extension type method
        var source = """
C.M(b: 42);

class C
{
    public static void M(int a) => throw null;
}

implicit extension E1 for C
{
    public static void M(int b)
    {
        System.Console.Write($"E1.M({b})");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, expectedOutput: "E1.M(42)", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Equal("void E1.M(System.Int32 b)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.M(System.Int32 a)" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_ArgumentName_02()
    {
        // Extension type method with incompatible parameter name is skipped in favor of extension method
        var source = """
new C().M(c: 42);

public class C
{
    public static void M(int a) => throw null;
}

implicit extension E1 for C
{
    public static void M(int b) => throw null;
}

public static class E2
{
    public static void M(this C self, int c)
    {
        System.Console.Write($"E2.M({c})");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        CompileAndVerify(comp, expectedOutput: "E2.M(42)", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void C.M(System.Int32 c)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());

        Assert.Equal(new[] { "void C.M(System.Int32 a)", "void C.M(System.Int32 c)" },
            model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void DelegateConversion_TypeReceiver()
    {
        var source = """
D d = C.M;
d(42);

delegate void D(int i);

class C { }

implicit extension E for C
{
    public static void M(int i)
    {
        System.Console.Write("E.M");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);

        CompileAndVerify(comp, expectedOutput: "E.M", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Equal("void E.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void DelegateConversion_InstanceReceiver()
    {
        var source = """
D d = new C().M;
d(42);

delegate void D(int i);

class C
{
    public void M() => throw null;
}

implicit extension E for C
{
    public void M(int i)
    {
        System.Console.Write("E.M");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);

        CompileAndVerify(comp, expectedOutput: "E.M", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void E.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.M()" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void DelegateConversion_SimpleName()
    {
        var source = """
delegate void D(int i);

class C
{
    public void M() => throw null;

    void M2()
    {
        D d = M;
        d(42);
    }
}

implicit extension E for C
{
    public void M(int i)
    {
        System.Console.Write("E.M");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // 0.cs(9,15): error CS0123: No overload for 'M' matches delegate 'D'
            //         D d = M;
            Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "M").WithArguments("M", "D").WithLocation(9, 15)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var simpleName = GetSyntax<EqualsValueClauseSyntax>(tree, "= M").Value;
        Assert.Equal(new[] { "void C.M()" }, model.GetMemberGroup(simpleName).ToTestDisplayStrings());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_TypeReceiver_TypeArguments()
    {
        var source = """
C.M<object>(42);

class C { }

implicit extension E for C
{
    public static void M(int i) => throw null;
    public static void M<T>(int i)
    {
        System.Console.Write("ran");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "ran", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M<object>");
        Assert.Equal("void E.M<System.Object>(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_InstanceReceiver_TypeArguments()
    {
        var source = """
new C().M<object>(42);

class C { }

implicit extension E for C
{
    public void M(int i) => throw null;
    public void M<T>(int i)
    {
        System.Console.Write("ran");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);

        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "ran", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M<object>");
        Assert.Equal("void E.M<System.Object>(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void DelegateConversion_TypeReceiver_Overloads()
    {
        var source = """
D d = C.M;
d(42);

C.M(42);

delegate void D(int i);

class C { }

implicit extension E for C
{
    public static void M(int i)
    {
        System.Console.Write("ran ");
    }

    public static void M(string s) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "ran ran", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "C.M").First();
        Assert.Equal("void E.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void DelegateConversion_ValueReceiver_Overloads()
    {
        var source = """
D d = new C().M;
d(42);

new C().M(42);

delegate void D(int i);

class C { }

implicit extension E for C
{
    public void M(int i)
    {
        System.Console.Write("ran ");
    }

    public void M(string s) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "ran ran", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").First();
        Assert.Equal("void E.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void DelegateConversion_TypeReceiver_Overloads_DifferentExtensions()
    {
        var source = """
D d = C.M;
d(42);

C.M(42);

delegate void D(int i);

class C { }

implicit extension E1 for C
{
    public static void M(int i)
    {
        System.Console.Write("ran ");
    }
}

implicit extension E2 for C
{
    public static void M(string s) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "ran ran", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "C.M").First();
        Assert.Equal("void E1.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void DelegateConversion_ValueReceiver_Overloads_DifferentExtensions()
    {
        var source = """
D d = new C().M;
d(42);

new C().M(42);

delegate void D(int i);

class C { }

implicit extension E1 for C
{
    public void M(int i)
    {
        System.Console.Write("ran ");
    }
}

implicit extension E2 for C
{
    public void M(string s) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "ran ran", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").First();
        Assert.Equal("void E1.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void DelegateConversion_TypeReceiver_Overloads_OuterScope()
    {
        var source = """
using N;

D d = C.M; // 1
d(42);

C.M(42);

delegate void D(int i);

class C { }

namespace N
{
    implicit extension E1 for C
    {
        public static void M(int i)
        {
            System.Console.Write("ran ");
        }
    }
}

implicit extension E2 for C
{
    public static void M(string s) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "ran ran", verify: Verification.FailsPEVerify).VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "C.M").First();
        Assert.Equal("void N.E1.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void DelegateConversion_ValueReceiver_Overloads_OuterScope()
    {
        var source = """
using N;

D d = new C().M;
d(42);

new C().M(42);

delegate void D(int i);

class C { }

namespace N
{
    implicit extension E1 for C
    {
        public void M(int i)
        {
            System.Console.Write("ran");
        }
    }
}

implicit extension E2 for C
{
    public void M(string s) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "ran", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").First();
        Assert.Equal("void N.E1.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void DelegateConversion_TypeReceiver_Overloads_OuterScope_WithInapplicableInstanceMember()
    {
        var source = """
using N;

D d = C.M;
d(42);

C.M(42);

delegate void D(int i);

class C
{
    public static void M(char c) { }
}

namespace N
{
    implicit extension E1 for C
    {
        public static void M(int i)
        {
            System.Console.Write("ran ");
        }
    }
}

implicit extension E2 for C
{
    public static void M(string s) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "ran ran", verify: Verification.FailsPEVerify).VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "C.M").First();
        Assert.Equal("void N.E1.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.M(System.Char c)" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void DelegateConversion_ValueReceiver_Overloads_OuterScope_WithInapplicableInstanceMember()
    {
        var source = """
using N;

D d = new C().M;
d(42);

new C().M(42);

delegate void D(int i);

class C
{
    public void M(char c) { }
}

namespace N
{
    implicit extension E1 for C
    {
        public void M(int i)
        {
            System.Console.Write("ran ran");
        }
    }
}

implicit extension E2 for C
{
    public void M(string s) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "ran ran", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").First();
        Assert.Equal("void N.E1.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.M(System.Char c)" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void DelegateConversion_TypeReceiver_Overloads_InnerScope()
    {
        var source = """
using N;

D d = C.M;
d(42);

delegate void D(int i);

class C { }

implicit extension E1 for C
{
    public static void M(int i)
    {
        System.Console.Write("ran");
    }
}

namespace N
{
    implicit extension E2 for C
    {
        public static void M(int i) => throw null;
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // 0.cs(1,1): hidden CS8019: Unnecessary using directive.
            // using N;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1)
            );

        CompileAndVerify(comp, expectedOutput: "ran", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Equal("void E1.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void DelegateConversion_TypeReceiver_TypeArguments()
    {
        var source = """
D d = C.M<object>;
d(42);

delegate void D(int i);
class C { }

implicit extension E for C
{
    public static void M(int i) => throw null;
    public static void M<T>(int i)
    {
        System.Console.Write("ran");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "ran", verify: Verification.FailsPEVerify).VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M<object>");
        Assert.Equal("void E.M<System.Object>(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void DelegateConversion_InstanceReceiver_TypeArguments()
    {
        var source = """
D d = new C().M<object>;
d(42);

delegate void D(int i);
class C { }

implicit extension E for C
{
    public void M(int i) => throw null;
    public void M<T>(int i)
    {
        System.Console.Write("ran");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "ran", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M<object>");
        Assert.Equal("void E.M<System.Object>(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_TypeReceiver_TypeArguments_WrongNumber()
    {
        var source = """
C.M<object, object>(42);

class C { }

implicit extension E for C
{
    public static void M(int i) => throw null;
    public static void M<T>(int i) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should give an updated error message (possibly ERR_NoSuchMemberOrExtension)
        comp.VerifyDiagnostics(
            // 0.cs(1,3): error CS0117: 'C' does not contain a definition for 'M'
            // C.M<object, object>(42);
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M<object, object>").WithArguments("C", "M").WithLocation(1, 3)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M<object, object>");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_InstanceReceiver_TypeArguments_WrongNumber()
    {
        var source = """
new C().M<object, object>(42);

class C { }

implicit extension E for C
{
    public void M(int i) => throw null;
    public void M<T>(int i) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // 0.cs(1,9): error CS1061: 'C' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
            // new C().M<object, object>(42);
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M<object, object>").WithArguments("C", "M").WithLocation(1, 9)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M<object, object>");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_TypeReceiver_TypeArguments_Omitted()
    {
        var source = """
C.M<>(42);

class C { }

implicit extension E for C
{
    public static void M<T>(int i) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // 0.cs(1,1): error CS8389: Omitting the type argument is not allowed in the current context
            // C.M<>(42);
            Diagnostic(ErrorCode.ERR_OmittedTypeArgument, "C.M<>").WithLocation(1, 1)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M<>");
        Assert.Equal("void E.M<?>(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_InstanceReceiver_TypeArguments_Omitted()
    {
        var source = """
new C().M<>(42);

class C { }

implicit extension E for C
{
    public void M<T>(int i) => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,1): error CS8389: Omitting the type argument is not allowed in the current context
            // new C().M<>(42);
            Diagnostic(ErrorCode.ERR_OmittedTypeArgument, "new C().M<>").WithLocation(1, 1)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M<>");
        Assert.Equal("void E.M<?>(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_TypeReceiver_TypeArguments_Inferred()
    {
        // No type arguments passed, but the extension type method is found and the type parameter inferred
        var source = """
C.M(42);

class C { }

implicit extension E for C
{
    public static void M<T>(T t)
    {
        System.Console.Write($"M({t})");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "M(42)", verify: Verification.FailsPEVerify).VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Equal("void E.M<System.Int32>(System.Int32 t)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_InstanceReceiver_TypeArguments_Inferred()
    {
        // No type arguments passed, but the extension type method is found and the type parameter inferred
        var source = """
new C().M(42);

class C { }

implicit extension E for C
{
    public void M<T>(T t)
    {
        System.Console.Write($"M({t})");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "M(42)", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void E.M<System.Int32>(System.Int32 t)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_StaticReceiver_InstanceExtensionMethod()
    {
        // The extension method is not static, but the receiver is a type
        var source = """
C.M();

class C { }

implicit extension E for C
{
    public void M() => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // 0.cs(1,1): error CS0120: An object reference is required for the non-static field, method, or property 'E.M()'
            // C.M();
            Diagnostic(ErrorCode.ERR_ObjectRequired, "C.M").WithArguments("E.M()").WithLocation(1, 1)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_InstanceReceiver_StaticExtensionMethod()
    {
        // The extension method is static but the receiver is a value
        var source = """
new C().M();

class C { }

implicit extension E for C
{
    public static void M() => throw null;
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // 0.cs(1,1): error CS0176: Member 'E.M()' cannot be accessed with an instance reference; qualify it with a type name instead
            // new C().M();
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "new C().M").WithArguments("E.M()").WithLocation(1, 1)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void IndexerAccess_Simple()
    {
        var source = """
_ = (new C())[42];

class C
{
    public int this[string s]
    {
        get => throw null;
    }
}

implicit extension E for C
{
    public int this[int i]
    {
        get
        {
            System.Console.Write("ran");
            return 0;
        }
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        // PROTOTYPE revisit when implementing "extension indexer access"
        comp.VerifyDiagnostics(
            // 0.cs(1,15): error CS1503: Argument 1: cannot convert from 'int' to 'string'
            // _ = (new C())[42];
            Diagnostic(ErrorCode.ERR_BadArgType, "42").WithArguments("1", "int", "string").WithLocation(1, 15)
            );
    }

    [Fact]
    public void ExtensionMethodOnTypeReceiver()
    {
        var source = """
C.Method();

public class C { }

public static class E
{
    public static void Method(this C c) { }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // 0.cs(1,1): error CS0120: An object reference is required for the non-static field, method, or property 'E.Method(C)'
            // C.Method();
            Diagnostic(ErrorCode.ERR_ObjectRequired, "C.Method").WithArguments("E.Method(C)").WithLocation(1, 1)
            );
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_TypeReceiver_WithExtensionMethod()
    {
        var source = """
C.Method();

public class C { }

public static class E1
{
    public static void Method(this C c) { }
}

implicit extension E2 for C
{
    public static void Method() { System.Console.Write("ran"); }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Method");
        Assert.Equal("void E2.Method()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.Method()" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_TypeReceiver_TypeParameter()
    {
        var source = """
public class C<T>
{
    void M()
    {
        T.Method();
    }

    implicit extension E for T
    {
        public static void Method() { }
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // 0.cs(5,9): error CS0704: Cannot do non-virtual member lookup in 'T' because it is a type parameter
            //         T.Method();
            Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T").WithArguments("T").WithLocation(5, 9)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "T.Method");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_TypeReceiver_TypeParameter_InNameof()
    {
        var source = """
public class C<T>
{
    void M()
    {
        _ = nameof(T.Method);
    }

    implicit extension E for T
    {
        public static void Method() { }
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // 0.cs(5,20): error CS0704: Cannot do non-virtual member lookup in 'T' because it is a type parameter
            //         _ = nameof(T.Method);
            Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T").WithArguments("T").WithLocation(5, 20)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "T.Method");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_TypeReceiver_TypeParameterWithStaticMethod()
    {
        var source = """
public interface I
{
    public static void Method() { }
}

public class C<T> where T : I
{
    void M()
    {
        T.Method();
    }

    implicit extension E for T
    {
        public static void Method() { }
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // 0.cs(10,9): error CS0704: Cannot do non-virtual member lookup in 'T' because it is a type parameter
            //         T.Method();
            Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T").WithArguments("T").WithLocation(10, 9)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "T.Method");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_TypeReceiver_TypeParameterWithStaticAbstractMethod()
    {
        var source = """
public interface I
{
    public static abstract void Method();
}

public class C<T> where T : I
{
    void M()
    {
        T.Method();
    }

    implicit extension E for T
    {
        public static void Method() { }
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "T.Method");
        Assert.Equal("void I.Method()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void I.Method()" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_TypeReceiver_TypeParameterWithStaticAbstractMethod_Overloads()
    {
        var source = """
public interface I
{
    public static abstract void Method();
}

public class C<T> where T : I
{
    void M()
    {
        T.Method(42);
    }

    implicit extension E for T
    {
        public static void Method(int i) { }
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // 0.cs(10,11): error CS1501: No overload for method 'Method' takes 1 arguments
            //         T.Method(42);
            Diagnostic(ErrorCode.ERR_BadArgCount, "Method").WithArguments("Method", "1").WithLocation(10, 11)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "T.Method");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal(new[] { "void I.Method()" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_ValueReceiver_TypeParameter()
    {
        var source = """
public class C<T>
{
    void M(T t)
    {
        t.Method();
    }

    implicit extension E for T
    {
        public void Method() { }
    }
}
""";
        // PROTOTYPE need to confirm what we want for extension invocations on type parameters
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,11): error CS1061: 'T' does not contain a definition for 'Method' and no accessible extension method 'Method' accepting a first argument of type 'T' could be found (are you missing a using directive or an assembly reference?)
            //         t.Method();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Method").WithArguments("T", "Method").WithLocation(5, 11)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "t.Method");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [ConditionalFact(typeof(CoreClrOnly), AlwaysSkip = "fails in emit stage in used assemblies leg")]
    public void RefOmittedComCall()
    {
        // For COM import type, omitting the ref is allowed
        string source = @"
using System;
using System.Runtime.InteropServices;

[ComImport, Guid(""1234C65D-1234-447A-B786-64682CBEF136"")]
class C { }

implicit extension E for C
{
    public void M(ref short p) { }
    public void M(sbyte p) { }
    public void I(ref int p) { }
}

class X
{
    public static void Goo()
    {
        short x = 123;
        C c = new C();
        c.M(x);
        c.I(123);
    }
}
";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void FunctionType_TypeReceiver_StaticExtension()
    {
        // PROTOTYPE The unique signature from method group should account for methods from extension types
        var source = """
var d = C.M;
d(42);

class C { }

implicit extension E for C
{
    public static void M(int i)
    {
        System.Console.Write("E.M");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        // PROTOTYPE need to infer delegate type
        comp.VerifyDiagnostics(
            // (1,9): error CS8917: The delegate type could not be inferred.
            // var d = C.M;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "C.M").WithLocation(1, 9)
            );
        //CompileAndVerify(comp, expectedOutput: "E.M", verify: Verification.FailsPEVerify);

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void FunctionType_ValueReceiver_StaticExtension()
    {
        // The method from extension is static
        var source = """
var d = new C().M; // 1

class C { }

implicit extension E for C
{
    public static void M(int i)
    {
        System.Console.Write("E.M");
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // 0.cs(1,9): error CS8917: The delegate type could not be inferred.
            // var d = new C().M; // 1
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().M").WithLocation(1, 9)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void FunctionType_TypeReceiver_InstanceAndExtension_DifferentSignatures()
    {
        // The instance method and the method from extension have different signatures
        var source = """
var d = C.M; // 1

class C
{
    public static void M() { }
}

implicit extension E for C
{
    public static void M(int i)
    {
        System.Console.Write("E.M");
    }
}
""";
        // PROTOTYPE revisit when implementing GetUniqueSignatureFromMethodGroup, should be an error
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Equal("void C.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.M()" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void FunctionType_ValueReceiver_ExtensionAndExtensionMethod_DifferentSignatures()
    {
        // The extension method and the method from extension have different signatures
        var source = """
var d = new C().M; // 1

public class C { }

implicit extension E1 for C
{
    public void M(int i) { }
}

public static class E2
{
    public static void M(this C c, string s) { }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        // PROTOTYPE revisit when implementing GetUniqueSignatureFromMethodGroup, should be an error
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void C.M(System.String s)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());

        Assert.Equal(new[] { "void C.M(System.String s)" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void FunctionType_TypeReceiver_InstanceAndExtension_SameSignatures()
    {
        var source = """
var d = C.M;

class C
{
    public static void M() { }
}

implicit extension E for C
{
    public static void M() { }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyEmitDiagnostics();

        // PROTOTYPE revisit when implementing GetUniqueSignatureFromMethodGroup, should be an error
        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Equal("void C.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void C.M()" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMethodAsReceiverOfMemberAccess()
    {
        var source = """
C.M.ToString();

class C { }

implicit extension E for C
{
    public static void M() { }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // 0.cs(1,3): error CS0119: 'E.M()' is a method, which is not valid in the given context
            // C.M.ToString();
            Diagnostic(ErrorCode.ERR_BadSKunknown, "M").WithArguments("E.M()", "method").WithLocation(1, 3)
            );

        // PROTOTYPE could improve the semantic model to return symbol `E.M()`
        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMethodAsReceiverOfMemberAccess_FindIndexer()
    {
        var source = """
C c = null;
c.Item.ToString();
c.get_Item.ToString();

class C { }

implicit extension E for C
{
    public int this[int i] { get { throw null; } set { throw null; } }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,3): error CS1061: 'C' does not contain a definition for 'Item' and no accessible extension method 'Item' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
            // c.Item.ToString();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item").WithArguments("C", "Item").WithLocation(2, 3),
            // (3,3): error CS1061: 'C' does not contain a definition for 'get_Item' and no accessible extension method 'get_Item' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
            // c.get_Item.ToString();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "get_Item").WithArguments("C", "get_Item").WithLocation(3, 3)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "c.Item");
        Assert.Null(model.GetSymbolInfo(memberAccess1).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess1));

        var memberAccess2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "c.get_Item");
        Assert.Null(model.GetSymbolInfo(memberAccess2).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess2));
    }

    [Fact]
    public void SymbolInfoForMethodGroup03()
    {
        var source = """
public class A { }

implicit extension E for A
{
    public string Extension() { return null; }
}
public class Program
{
    public static void Main(string[] args)
    {
        A a = null;
        _ = nameof(a.Extension);
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        // PROTOTYPE should we produce ERR_NameofExtensionMethod (Extension method groups are not allowed as an argument to 'nameof') or something similar?
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "a.Extension");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [Fact]
    public void NotInvocable_TypeReceiver()
    {
        var source = """
C.f();

public class C
{
    public int f;
}
""";

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (1,3): error CS1955: Non-invocable member 'C.f' cannot be used like a method.
            // C.f();
            Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "f").WithArguments("C.f").WithLocation(1, 3)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.f");
        var symbolInfo = model.GetSymbolInfo(memberAccess);
        Assert.Null(symbolInfo.Symbol);
        Assert.Empty(symbolInfo.CandidateSymbols);
        Assert.Null(model.GetTypeInfo(memberAccess).Type);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [Fact]
    public void NotInvocable_InstanceReceiver()
    {
        var source = """
new C().f();

public class C
{
    public int f;
}
""";

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (1,9): error CS1955: Non-invocable member 'C.f' cannot be used like a method.
            // new C().f();
            Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "f").WithArguments("C.f").WithLocation(1, 9)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().f");
        var symbolInfo = model.GetSymbolInfo(memberAccess);
        Assert.Null(symbolInfo.Symbol);
        Assert.Empty(symbolInfo.CandidateSymbols);
        Assert.Null(model.GetTypeInfo(memberAccess).Type);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void NotInvocable_TypeReceiver_WithExtensionType()
    {
        var source = """
C.f();

public class C
{
    public int f;
}

implicit extension E for C
{
    public static void f()
    {
        System.Console.Write("f");
    }
}
""";

        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "f").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.f");
        Assert.Equal("void E.f()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Null(model.GetTypeInfo(memberAccess).Type);
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [Fact]
    public void NotInvocable_InstanceReceiver_WithExtensionType()
    {
        var source = """
new C().f();

public class C
{
    public int f;
}

implicit extension E for C
{
    public void f() { System.Console.Write("ran"); }
}
""";

        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().f");
        Assert.Equal("void E.f()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Null(model.GetTypeInfo(memberAccess).Type);
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_InstanceReceiver_InTypeInference()
    {
        // We resolve the method group for `M` as part of overload resolution for `Select`
        var source = """
class D
{
    public static void Main()
    {
        int i = new D().Select(new C().M);
    }

    T Select<T>(System.Func<T> t)
    {
        t();
        return default;
    }
}

public class C { }

implicit extension E for C
{
    public int M() { System.Console.Write("ran"); return 0; }
}
""";

        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("System.Int32 E.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionInvocation_TypeReceiver_TypeInference()
    {
        // We resolve the method group for `M` as part of overload resolution for `Select`
        var source = """
class D
{
    public static void Main()
    {
        new D().Select(C.M);
    }

    T Select<T>(System.Func<T> t)
    {
        t();
        return default;
    }
}

public class C { }

implicit extension E for C
{
    public static int M() { System.Console.Write("ran"); return 0; }
}
""";

        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Equal("System.Int32 E.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [Fact]
    public void AddressOf_AmbiguousBestMethod()
    {
        var src = """
unsafe class C
{
    static void M1()
    {
        delegate*<string, string, void> ptr = &C.M;
    }
}

implicit extension E for C
{
    public static void M(string s, object o) {}
    public static void M(object o, string s) {}
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (5,48): error CS0121: The call is ambiguous between the following methods or properties: 'E.M(string, object)' and 'E.M(object, string)'
            //         delegate*<string, string, void> ptr = &C.M;
            Diagnostic(ErrorCode.ERR_AmbigCall, "C.M").WithArguments("E.M(string, object)", "E.M(object, string)").WithLocation(5, 48)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [Fact]
    public void ExtensionMethodsInNonInvocationLookup()
    {
        var src = """
public implicit extension E1 for object
{
    public int Member => throw null;
}

namespace N
{
    public static class E2
    {
        public static void Member(this object o)
        {
            System.Console.Write("ran ");
        }
    }

    class C
    {
        public static void Main()
        {
            var o = new object();

            var x = o.Member;
            x();

            System.Action y = o.Member;
            y();

            o.Member();
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //CompileAndVerify(comp, expectedOutput: "ran ran ran");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").First();
        Assert.Equal("void System.Object.Member()", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void System.Object.Member()" }, model.GetMemberGroup(memberAccess1).ToTestDisplayStrings());

        var memberAccess2 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").Skip(1).First();
        Assert.Equal("void System.Object.Member()", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void System.Object.Member()" }, model.GetMemberGroup(memberAccess2).ToTestDisplayStrings());

        var memberAccess3 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").Skip(2).Single();
        Assert.Equal("void System.Object.Member()", model.GetSymbolInfo(memberAccess3).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void System.Object.Member()" }, model.GetMemberGroup(memberAccess3).ToTestDisplayStrings());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMethodsInNonInvocationLookup_Generic()
    {
        var src = """
public implicit extension E1 for object
{
    public int Member => throw null;
}

namespace N
{
    public static class E2
    {
        public static void Member<T>(this T o)
        {
            System.Console.Write("ran ");
        }
    }

    class C
    {
        public static void Main()
        {
            var o = new object();

            var x = o.Member;
            x();

            System.Action y = o.Member;
            y();

            o.Member();
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "ran ran ran", verify: Verification.Fails).VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").First();
        Assert.Equal("void System.Object.Member<System.Object>()", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void System.Object.Member<System.Object>()" }, model.GetMemberGroup(memberAccess1).ToTestDisplayStrings());

        var memberAccess2 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").Skip(1).First();
        Assert.Equal("void System.Object.Member<System.Object>()", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void System.Object.Member<System.Object>()" }, model.GetMemberGroup(memberAccess2).ToTestDisplayStrings());

        var memberAccess3 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").Skip(2).Single();
        Assert.Equal("void System.Object.Member<System.Object>()", model.GetSymbolInfo(memberAccess3).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void System.Object.Member<System.Object>()" }, model.GetMemberGroup(memberAccess3).ToTestDisplayStrings());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMethodsInNonInvocationLookup_InapplicableGeneric()
    {
        var src = """
public implicit extension E1 for object
{
    public int Member { get { System.Console.Write("ran "); return 42; } }
}

namespace N
{
    public static class E2
    {
        public static void Member<T>(this T o) where T : struct { }
    }

    class C
    {
        public static void Main()
        {
            var o = new object();
            var x = o.Member;
            System.Console.Write(x);
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics(
            // (18,21): error CS8917: The delegate type could not be inferred.
            //             var x = o.Member;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "o.Member").WithLocation(18, 21)
            );
        // PROTOTYPE Execute when adding support for emitting non-static members

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "o.Member");
        Assert.Equal("System.Int32 E1.Member { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal([], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMethodsInInvocationLookup_AfterExtensionType()
    {
        var src = """
public implicit extension E1 for object
{
    public int Member => throw null;
}

public static class E2
{
    public static void Member(this object o)
    {
        System.Console.Write("ran ");
    }
}

class C
{
    public static void Main()
    {
        var o = new object();
        var x = o.Member;
        x();

        System.Action y = o.Member;
        y();

        o.Member();
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "ran ran ran", verify: Verification.Fails).VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").First();
        Assert.Equal("void System.Object.Member()", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void System.Object.Member()" }, model.GetMemberGroup(memberAccess1).ToTestDisplayStrings());

        var memberAccess2 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").Skip(1).First();
        Assert.Equal("void System.Object.Member()", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void System.Object.Member()" }, model.GetMemberGroup(memberAccess2).ToTestDisplayStrings());

        var memberAccess3 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").Skip(2).Single();
        Assert.Equal("void System.Object.Member()", model.GetSymbolInfo(memberAccess3).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void System.Object.Member()" }, model.GetMemberGroup(memberAccess3).ToTestDisplayStrings());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMethodsInInvocationLookup_AfterExtensionType_Import()
    {
        var src = """
using N;

var o = new object();
var x = o.Member;
x();

System.Action y = o.Member;
y();

o.Member();

namespace N
{
    public implicit extension E1 for object
    {
        public int Member => throw null;
    }

    public static class E2
    {
        public static void Member(this object o)
        {
            System.Console.Write("ran ");
        }
    }
}
""";
        // PROTOTYPE revisit when implementing GetUniqueSignatureFromMethodGroup, the `var` case should bind to the property
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "ran ran ran", verify: Verification.Fails).VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").First();
        Assert.Equal("void System.Object.Member()", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void System.Object.Member()" }, model.GetMemberGroup(memberAccess1).ToTestDisplayStrings());

        var memberAccess2 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").Skip(1).First();
        Assert.Equal("void System.Object.Member()", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void System.Object.Member()" }, model.GetMemberGroup(memberAccess2).ToTestDisplayStrings());

        var memberAccess3 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").Skip(2).Single();
        Assert.Equal("void System.Object.Member()", model.GetSymbolInfo(memberAccess3).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void System.Object.Member()" }, model.GetMemberGroup(memberAccess3).ToTestDisplayStrings());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMethodsInNonInvocationLookup_ComesBeforeExtensionTypeInNextScope()
    {
        // Imported extension methods come before extension type members in outer scope
        var src = """
public implicit extension E1 for object
{
    public int Member => throw null;
}

namespace N1
{
    using N2;

    class C
    {
        public static void Main()
        {
            var o = new object();
            var x = o.Member;
            x();

            System.Action y = o.Member;
            y();

            o.Member();
        }
    }
}

namespace N2
{
    public static class E2
    {
        public static void Member(this object o)
        {
            System.Console.Write("ran ");
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics();
        // Existing ILVerification failure tracked by https://github.com/dotnet/roslyn/issues/68749
        var verifier = CompileAndVerify(comp, expectedOutput: "ran ran ran", verify: Verification.Fails);

        verifier.VerifyIL("N1.C.Main", """
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (object V_0, //o
                System.Action V_1, //x
                System.Action V_2) //y
  IL_0000:  nop
  IL_0001:  newobj     "object..ctor()"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldftn      "void N2.E2.Member(object)"
  IL_000e:  newobj     "System.Action..ctor(object, nint)"
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  callvirt   "void System.Action.Invoke()"
  IL_001a:  nop
  IL_001b:  ldloc.0
  IL_001c:  ldftn      "void N2.E2.Member(object)"
  IL_0022:  newobj     "System.Action..ctor(object, nint)"
  IL_0027:  stloc.2
  IL_0028:  ldloc.2
  IL_0029:  callvirt   "void System.Action.Invoke()"
  IL_002e:  nop
  IL_002f:  ldloc.0
  IL_0030:  call       "void N2.E2.Member(object)"
  IL_0035:  nop
  IL_0036:  ret
}
""");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").First();
        Assert.Equal("void System.Object.Member()", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void System.Object.Member()" }, model.GetMemberGroup(memberAccess1).ToTestDisplayStrings());

        var memberAccess2 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").Skip(1).First();
        Assert.Equal("void System.Object.Member()", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void System.Object.Member()" }, model.GetMemberGroup(memberAccess2).ToTestDisplayStrings());

        var memberAccess3 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").Skip(2).Single();
        Assert.Equal("void System.Object.Member()", model.GetSymbolInfo(memberAccess3).Symbol.ToTestDisplayString());
        Assert.Equal(new[] { "void System.Object.Member()" }, model.GetMemberGroup(memberAccess3).ToTestDisplayStrings());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMethodsInInvocationLookup_GenericExtension_ThisParameterNotCompatible()
    {
        var src = """
using N;

C<int> c = new C<int>();
int i = c.Member;
System.Console.Write(i);

public class C<T> { }

public static class E1
{
    public static void Member<T>(this C<T> o) where T : class => throw null;
}

namespace N
{
    public implicit extension E2 for object
    {
        public int Member => 42;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "42", verify: Verification.Fails).VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "c.Member").First();
        Assert.Equal("System.Int32 N.E2.Member { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMethodsInInvocationLookup_GenericExtension_ExtraParameter()
    {
        var src = """
using N;

C<int> c = new C<int>();
int i = c.Member;
System.Console.Write(i);

public class C<T> { }

public static class E1
{
    public static void Member<T>(this C<T> o, string s) => throw null;
}

namespace N
{
    public implicit extension E2 for object
    {
        public int Member => 42;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using N;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1),
            // (4,11): error CS0428: Cannot convert method group 'Member' to non-delegate type 'int'. Did you intend to invoke the method?
            // int i = c.Member;
            Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Member").WithArguments("Member", "int").WithLocation(4, 11)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "c.Member").First();
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);

        Assert.Equal(new[] { "void C<System.Int32>.Member<System.Int32>(System.String s)" },
            model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ExtensionMethodsInInvocationLookup_GenericExtension_UndeterminedTypeParameter()
    {
        var src = """
using N;

C<int> c = new C<int>();
int i = c.Member;
System.Console.Write(i);

public class C<T> { }

public static class E1
{
    public static void Member<T, U>(this C<T> o, U u) => throw null;
}

namespace N
{
    public implicit extension E2 for object
    {
        public int Member => 42;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using N;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1),
            // (4,11): error CS0428: Cannot convert method group 'Member' to non-delegate type 'int'. Did you intend to invoke the method?
            // int i = c.Member;
            Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Member").WithArguments("Member", "int").WithLocation(4, 11)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "c.Member").First();
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);

        Assert.Equal(new[] { "void C<System.Int32>.Member<System.Int32, U>(U u)" },
            model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
    }

    [Fact]
    public void ExtensionMethodsInNonInvocationLookup_Inaccessible()
    {
        // An inaccessible extension method is skipped in favor of an extension type member in outer scope
        var src = """
public implicit extension E1 for object
{
    public int Member { get { System.Console.Write("ran "); return 0; } }
}

namespace N
{
    public static class E2
    {
        private static void Member(this object o) => throw null;
    }

    class C
    {
        public static void Main()
        {
            var o = new object();
            int x = o.Member;
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics();
        // PROTOTYPE Execute when adding support for emitting non-static members
        //        var verifier = CompileAndVerify(comp, expectedOutput: "ran");

        //        verifier.VerifyIL("N.C.Main", """
        //{
        //  // Code size       15 (0xf)
        //  .maxstack  1
        //  .locals init (object V_0, //o
        //                int V_1) //x
        //  IL_0000:  nop
        //  IL_0001:  newobj     "object..ctor()"
        //  IL_0006:  stloc.0
        //  IL_0007:  ldloc.0
        //  IL_0008:  callvirt   "int E1.Member.get"
        //  IL_000d:  stloc.1
        //  IL_000e:  ret
        //}
        //""");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").Single();
        Assert.Equal("System.Int32 E1.Member { get; }", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMethodsInNonInvocationLookup_Ineligible()
    {
        // An ineligible extension method comes before an extension type member in outer scope
        var src = """
public implicit extension E1 for object
{
    public int Member { get { System.Console.Write("ran "); return 0; } }
}

namespace N
{
    public static class E2
    {
        public static void Member(this string o) => throw null;
    }

    class C
    {
        public static void Main()
        {
            var o = new object();
            int x = o.Member;
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").Single();
        Assert.Equal("System.Int32 E1.Member { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [Fact]
    public void ExtensionMethodsInNonInvocationLookup_ExtraParameter()
    {
        var src = """
public implicit extension E1 for object
{
    public int Member { get { System.Console.Write("ran "); return 0; } }
}

namespace N
{
    public static class E2
    {
        public static void Member(this object o, int i) => throw null;
    }

    class C
    {
        public static void Main()
        {
            var o = new object();
            int x = o.Member;
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics(
            // (18,23): error CS0428: Cannot convert method group 'Member' to non-delegate type 'int'. Did you intend to invoke the method?
            //             int x = o.Member;
            Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Member").WithArguments("Member", "int").WithLocation(18, 23)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "o.Member").Single();
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal(new[] { "void System.Object.Member(System.Int32 i)" }, model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
    }

    [Fact]
    public void ExtensionMethodsInNonInvocationLookup_FieldBlocksInstanceMethod()
    {
        // A non-invocable field blocks an instance method from base type in non-invocation scenario
        var src = """
C c = null;
System.Action a = c.M; // 1
c.M();

public class Base
{
    public void M() { }
}

public class C : Base
{
    public new int M;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,19): error CS0029: Cannot implicitly convert type 'int' to 'System.Action'
            // System.Action a = c.M; // 1
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "c.M").WithArguments("int", "System.Action").WithLocation(2, 19)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "c.M").First();
        Assert.Equal("System.Int32 C.M", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());

        var memberAccess2 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "c.M").Skip(1).Single();
        Assert.Equal("void Base.M()", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMethodsInNonInvocationLookup_FieldBlocksExtensionMethod()
    {
        // A non-invocable field blocks an extension method in non-invocation scenario
        var src = """
C c = null;
System.Action a = c.M; // 1
c.M();

public static class E
{
    public static void M(this C c) { }
}

public class C
{
    public int M;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,19): error CS0029: Cannot implicitly convert type 'int' to 'System.Action'
            // System.Action a = c.M; // 1
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "c.M").WithArguments("int", "System.Action").WithLocation(2, 19)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "c.M").First();
        Assert.Equal("System.Int32 C.M", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());

        var memberAccess2 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "c.M").Skip(1).Single();
        Assert.Equal("void C.M()", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMethodsInNonInvocationLookup_PropertyBlocksExtensionMethod()
    {
        // A non-invocable property blocks an extension method in non-invocation scenario
        var src = """
C c = null;
System.Action a = c.M; // 1
c.M();

public static class E
{
    public static void M(this C c) { }
}

public class C
{
    public int M => 0;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,19): error CS0029: Cannot implicitly convert type 'int' to 'System.Action'
            // System.Action a = c.M; // 1
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "c.M").WithArguments("int", "System.Action").WithLocation(2, 19)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "c.M").First();
        Assert.Equal("System.Int32 C.M { get; }", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());

        var memberAccess2 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "c.M").Skip(1).Single();
        Assert.Equal("void C.M()", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMethodsInNonInvocationLookup_PropertyBlocksExtensionTypeMethod()
    {
        // A non-invocable property blocks an extension type method in non-invocation scenario
        var src = """
C c = null;
System.Action a = c.M; // 1
c.M();

implicit extension E for C
{
    public void M() { }
}

public class C
{
    public int M => 0;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (2,19): error CS0029: Cannot implicitly convert type 'int' to 'System.Action'
            // System.Action a = c.M; // 1
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "c.M").WithArguments("int", "System.Action").WithLocation(2, 19)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "c.M").First();
        Assert.Equal("System.Int32 C.M { get; }", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());

        var memberAccess2 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "c.M").Skip(1).Single();
        Assert.Equal("void E.M()", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ParameterCapturing_023_ColorColor_MemberAccess_InstanceAndStatic_ExtensionTypeMethods()
    {
        // See ParameterCapturing_023_ColorColor_MemberAccess_InstanceAndStatic_Method
        var source = """
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
    }
}

class Color { }

implicit extension E for Color
{
    public void M1(S1 x, int y = 0)
    {
        System.Console.WriteLine("instance");
    }

    public static void M1<T>(T x) where T : unmanaged
    {
        System.Console.WriteLine("static");
    }
}
""";
        // PROTOTYPE missing ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver
        var comp = CreateCompilation(source, options: TestOptions.ReleaseDll, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            //// (5,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
            ////         Color.M1(this);
            //Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(5, 9)
            );

        Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "Color.M1");
        Assert.Equal("void E.M1(S1 x, [System.Int32 y = 0])", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ParameterCapturing_023_ColorColor_MemberAccess_InstanceAndStatic_ExtensionTypeProperties()
    {
        var source = """
struct S1(Color Color)
{
    public void Test()
    {
        _ = Color.P1;
    }
}

class Color { }

implicit extension E1 for Color
{
    public int P1 => 0;
}

implicit extension E2 for Color
{
    public static int P1 => 0;
}
""";
        var comp = CreateCompilation(source, options: TestOptions.ReleaseDll, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (5,19): error CS0229: Ambiguity between 'E1.P1' and 'E2.P1'
            //         _ = Color.P1;
            Diagnostic(ErrorCode.ERR_AmbigMember, "P1").WithArguments("E1.P1", "E2.P1").WithLocation(5, 19)
            );

        Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "Color.P1");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
    }

    [Fact]
    public void ParameterCapturing_023_ColorColor_MemberAccess_InstanceAndStatic_ExtensionTypeMembersVsExtensionMethod()
    {
        var source = """
public struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
    }
}

public class Color { }

public static class E1
{
    public static void M1(this Color c, S1 x, int y = 0)
    {
        System.Console.WriteLine("instance");
    }
}

implicit extension E2 for Color
{
    public static void M1<T>(T x) where T : unmanaged
    {
        System.Console.WriteLine("static");
    }
}
""";
        // PROTOTYPE missing ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver
        var comp = CreateCompilation(source, options: TestOptions.ReleaseDll, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            //// (5,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
            ////         Color.M1(this);
            //Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(5, 9)
            );

        Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "Color.M1");
        Assert.Equal("void Color.M1(S1 x, [System.Int32 y = 0])", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void NotOnBase()
    {
        // Unlike `this`, `base` is not an expression in itself.
        // "Extension invocation" and "extension member lookup" do not apply to `base_access` syntax.
        var src = """
class Base { }

class Derived : Base
{
    void Main()
    {
        M(); // 1
        this.M();
        base.M(); // 2
        _ = P; // 3
        _ = this.P;
        _ = base.P; // 4
    }
}

implicit extension E for Base
{
    public void M() { }
    public int P => 0;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (7,9): error CS0103: The name 'M' does not exist in the current context
            //         M(); // 1
            Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(7, 9),
            // (9,14): error CS0117: 'Base' does not contain a definition for 'M'
            //         base.M(); // 2
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("Base", "M").WithLocation(9, 14),
            // (10,13): error CS0103: The name 'P' does not exist in the current context
            //         _ = P; // 3
            Diagnostic(ErrorCode.ERR_NameNotInContext, "P").WithArguments("P").WithLocation(10, 13),
            // (12,18): error CS0117: 'Base' does not contain a definition for 'P'
            //         _ = base.P; // 4
            Diagnostic(ErrorCode.ERR_NoSuchMember, "P").WithArguments("Base", "P").WithLocation(12, 18)
            );
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void CompoundAssignment()
    {
        var src = """
object.P += 1;
System.Console.Write($"Property({object.P}) ");

object.Field += 1;
System.Console.Write($"Field({object.Field}) ");

implicit extension E for object
{
    public static int P { get; set; } = 42;
    public static int Field = 42;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "Property(43) Field(43)", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void LookupKind_DelegateConversion()
    {
        // Non-invcable extension member in inner scope is skipped in favor of invocable one from outer scope
        var src = """
using N;

System.Action a = object.Member;
a();

implicit extension E1 for object
{
    public class Member { }
}

namespace N
{
    implicit extension E2 for object
    {
        public static void Member()
        {
            System.Console.Write("ran ");
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "ran", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Member");
        Assert.Equal("void N.E2.Member()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void LookupKind_Invocation()
    {
        // Non-invcable extension member in inner scope is skipped in favor of invocable one from outer scope
        var src = """
using N;

object.Member();

implicit extension E1 for object
{
    public class Member { }
}

namespace N
{
    implicit extension E2 for object
    {
        public static void Member()
        {
            System.Console.Write("ran ");
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "ran", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Member");
        Assert.Equal("void N.E2.Member()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void LookupKind_OtherConversion()
    {
        // Non-invocable member in inner scope is skipped
        var src = """
using N;

int a = object.Member;

implicit extension E1 for object
{
    public class Member { }
}

namespace N
{
    implicit extension E2 for object
    {
        public static void Member() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using N;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1),
            // (3,16): error CS0428: Cannot convert method group 'Member' to non-delegate type 'int'. Did you intend to invoke the method?
            // int a = object.Member;
            Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Member").WithArguments("Member", "int").WithLocation(3, 16)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Member");
        Assert.Equal("E1.Member", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ReturnType_FindExtensionTypeMethod()
    {
        var src = """
System.Console.Write(local(object.M));

T local<T>(System.Func<T> f) => f();

implicit extension E for object
{
    public static int M() => 42;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);

        CompileAndVerify(comp, expectedOutput: "42", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.Int32 E.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ReturnType_FindExtensionTypeMethod_PickBestOverload()
    {
        var src = """
System.Console.Write(local(object.M));

T local<T>(System.Func<int, T> f) => f(42);

implicit extension E for object
{
    public static int M(int i) => i;
    public static string M(string s) => throw null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);

        CompileAndVerify(comp, expectedOutput: "42", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.Int32 E.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ReturnType_FindType()
    {
        var src = """
int i = object.M;

implicit extension E for object
{
    public class M { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,16): error CS0428: Cannot convert method group 'M' to non-delegate type 'int'. Did you intend to invoke the method?
            // int i = object.M;
            Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "M").WithArguments("M", "int").WithLocation(1, 16)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("E.M", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ReturnType_FindType_DelegateType()
    {
        // In invocation context, the type resolution is skipped
        var src = """
System.Console.Write(local(object.M));

T local<T>(System.Func<T> f) => f();

implicit extension E1 for object
{
    public class M { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,22): error CS0411: The type arguments for method 'local<T>(Func<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
            // System.Console.Write(local(object.M));
            Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "local").WithArguments("local<T>(System.Func<T>)").WithLocation(1, 22),
            // (3,3): warning CS8321: The local function 'local' is declared but never used
            // T local<T>(System.Func<T> f) => f();
            Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(3, 3)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void IndexExpression()
    {
        var src = """
var x = new[] { 1, 2, 3 };
System.Console.Write(x[^object.f]);

implicit extension E for object
{
    public static int f = 2;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "2", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.f");
        Assert.Equal("System.Int32 E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void RangeExpression()
    {
        var src = """
var x = new[] { 1, 2, 3 };
System.Console.Write(x[0..object.f].Length);

implicit extension E for object
{
    public static int f = 2;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "2", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.f");
        Assert.Equal("System.Int32 E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ArrayIndexer()
    {
        var src = """
var x = new[] { 1, 2, 3 };
System.Console.Write(x[object.f]);

implicit extension E for object
{
    public static int f = 2;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "3", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.f");
        Assert.Equal("System.Int32 E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void CollectionInitializer()
    {
        var src = """
var x = new System.Collections.Generic.List<int> { object.f };
System.Console.Write(x[0]);

implicit extension E for object
{
    public static int f = 2;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "2", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.f");
        Assert.Equal("System.Int32 E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ArrayInitializer()
    {
        var src = """
var x = new[] { object.f };
System.Console.Write(x[0]);

implicit extension E for object
{
    public static int f = 2;
}
""";
        // PROTOTYPE this should work
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,9): error CS0826: No best type found for implicitly-typed array
            // var x = new[] { object.f };
            Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { object.f }").WithLocation(1, 9)
            );
        //CompileAndVerify(comp, expectedOutput: "2", verify: Verification.FailsPEVerify)
        //    .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.f");
        Assert.Equal("System.Int32 E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ArrayInitializer_Null()
    {
        var src = """
var x = new[] { object.f, null };
System.Console.Write(x[0]);

implicit extension E for object
{
    public static string f = "hi";
}
""";
        // PROTOTYPE this should work
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,9): error CS0826: No best type found for implicitly-typed array
            // var x = new[] { object.f, null };
            Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { object.f, null }").WithLocation(1, 9)
            );
        //CompileAndVerify(comp, expectedOutput: "2", verify: Verification.FailsPEVerify)
        //    .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.f");
        Assert.Equal("System.String E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void BinaryOperator_Default()
    {
        var src = """
int i = default + object.f;

implicit extension E for object
{
    public static int f = 2;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,9): error CS8310: Operator '+' cannot be applied to operand 'default'
            // int i = default + object.f;
            Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default + object.f").WithArguments("+", "default").WithLocation(1, 9)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.f");
        Assert.Equal("System.Int32 E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void BinaryOperator()
    {
        var src = """
int i = object.f + object.f;
System.Console.Write(i);

implicit extension E for object
{
    public static int f = 2;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "4", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.f").First();
        Assert.Equal("System.Int32 E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void NullCoalescingAssignment_LHS()
    {
        var src = """
object.f ??= "hi";
System.Console.Write(object.f);

implicit extension E for object
{
    public static string f = null;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "hi", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.f").First();
        Assert.Equal("System.String E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void NullCoalescingAssignment_RHS()
    {
        var src = """
string s = null;
s ??= object.f;
System.Console.Write(s);

implicit extension E for object
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "hi", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.f").First();
        Assert.Equal("System.String E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void Ternary()
    {
        var src = """
bool b = true;
var x = b ? object.f : object.f;
System.Console.Write(object.f);

implicit extension E for object
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE this should work
        comp.VerifyDiagnostics(
            // (2,9): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'method group' and 'method group'
            // var x = b ? object.f : object.f;
            Diagnostic(ErrorCode.ERR_InvalidQM, "b ? object.f : object.f").WithArguments("method group", "method group").WithLocation(2, 9)
            );
        //CompileAndVerify(comp, expectedOutput: "hi", verify: Verification.FailsPEVerify)
        //    .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.f").First();
        Assert.Equal("System.String E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void Ternary_Null()
    {
        var src = """
bool b = true;
var x = b ? object.f : null;
System.Console.Write(object.f);

implicit extension E for object
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE this should work
        comp.VerifyDiagnostics(
            // (2,9): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'method group' and '<null>'
            // var x = b ? object.f : null;
            Diagnostic(ErrorCode.ERR_InvalidQM, "b ? object.f : null").WithArguments("method group", "<null>").WithLocation(2, 9)
            );
        //CompileAndVerify(comp, expectedOutput: "hi", verify: Verification.FailsPEVerify)
        //    .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.f").First();
        Assert.Equal("System.String E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void RefExpression()
    {
        var src = """
ref var x = ref object.f;
System.Console.Write(object.f);

implicit extension E for object
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "hi", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.f").First();
        Assert.Equal("System.String E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ReturnStatement()
    {
        var src = """
System.Console.Write(local());

string local()
{
    return object.f;
}

implicit extension E for object
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "hi", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.f").First();
        Assert.Equal("System.String E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void LambdaReturn()
    {
        var src = """
var l = () => object.f;
System.Console.Write(l());

implicit extension E for object
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE need to infer delegate type
        comp.VerifyDiagnostics(
            // (1,9): error CS8917: The delegate type could not be inferred.
            // var l = () => object.f;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "() => object.f").WithLocation(1, 9)
            );
        //CompileAndVerify(comp, expectedOutput: "hi", verify: Verification.FailsPEVerify)
        //    .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.f").First();
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void IfStatement()
    {
        var src = """
if (object.f)
    System.Console.Write("hi");
else
    throw null;

implicit extension E for object
{
    public static bool f = true;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "hi", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.f").First();
        Assert.Equal("System.Boolean E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void ForeachStatement()
    {
        var src = """
foreach (var x in object.f)
{
    System.Console.Write(x);
}

implicit extension E for object
{
    public static string[] f = new[] { "hi" };
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "hi", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.f").First();
        Assert.Equal("System.String[] E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GotoStatement()
    {
        var src = """
goto object.f;

implicit extension E for object
{
    public static string[] f = new[] { "hi" };
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,6): error CS1001: Identifier expected
            // goto object.f;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "object").WithLocation(1, 6),
            // (1,6): error CS1002: ; expected
            // goto object.f;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "object").WithLocation(1, 6),
            // (1,6): error CS0159: No such label '' within the scope of the goto statement
            // goto object.f;
            Diagnostic(ErrorCode.ERR_LabelNotFound, "").WithArguments("").WithLocation(1, 6),
            // (1,6): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
            // goto object.f;
            Diagnostic(ErrorCode.ERR_IllegalStatement, "object.f").WithLocation(1, 6)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.f").First();
        Assert.Equal("System.String[] E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void SwitchExpression()
    {
        var src = """
var s = 0 switch { _ => object.f };
System.Console.Write(s);

implicit extension E for object
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE this should work
        comp.VerifyDiagnostics(
            // (1,11): error CS8506: No best type was found for the switch expression.
            // var s = 0 switch { _ => object.f };
            Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(1, 11)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.f").First();
        Assert.Equal("System.String E.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void RefArgument()
    {
        var src = """
write(ref object.P);

void write(ref object o)
{
    System.Console.Write(o.ToString());
}

implicit extension E for object
{
    static object o = "hi";
    public static ref object P => ref o;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "hi", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.P");
        Assert.Equal("ref System.Object E.P { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GenericExtension_Simple()
    {
        var src = """
string s = C<int>.f;
System.Console.Write(s);

class C<T> { }

implicit extension E<T> for C<T>
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);

        CompileAndVerify(comp, expectedOutput: "hi", verify: Verification.FailsPEVerify)
           .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<int>.f");
        Assert.Equal("System.String E<System.Int32>.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GenericExtension_NotUnique()
    {
        var src = """
string s1 = C<object, dynamic>.f;
string s2 = C<dynamic, object>.f;

class C<T ,U> { }

implicit extension E<T> for C<T ,T>
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<object, dynamic>.f");
        Assert.Equal("System.String E<System.Object>.f", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());

        // PROTOTYPE we could refine the algorithm to "merge" options
        var memberAccess2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<dynamic, object>.f");
        Assert.Equal("System.String E<dynamic>.f", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GenericExtension_GenericNestedType()
    {
        var src = """
string s = C<string>.Nested<int>.f;
System.Console.Write(s);

internal class C<T>
{
    internal class Nested<U> { }
}

implicit extension E<T1, T2> for C<T1>.Nested<T2>
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        var verifier = CompileAndVerify(comp, expectedOutput: "hi", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<string>.Nested<int>.f");
        Assert.Equal("System.String E<System.String, System.Int32>.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GenericExtension_InGenericContainer()
    {
        var src = """
string s = C<string>.Nested1<bool>.Nested2<int>.f;
System.Console.Write(s);

internal class C<T>
{
    internal class Nested1<U>
    {
        internal class Nested2<V> { }
    }
}

implicit extension E<T1, T2, T3> for C<T1>.Nested1<T2>.Nested2<T3>
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: "hi", verify: Verification.FailsPEVerify)
            .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<string>.Nested1<bool>.Nested2<int>.f");
        Assert.Equal("System.String E<System.String, System.Boolean, System.Int32>.f",
            model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GenericExtension_InaccessibleNestedType()
    {
        var src = """
string s = C<string>.Nested<int>.f;
System.Console.Write(s);

class C<T>
{
    private class Nested<U> { }
}

implicit extension E<T1, T2> for C<T1>.Nested<T2>
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,22): error CS0122: 'C<string>.Nested<U>' is inaccessible due to its protection level
            // string s = C<string>.Nested<int>.f;
            Diagnostic(ErrorCode.ERR_BadAccess, "Nested<int>").WithArguments("C<string>.Nested<U>").WithLocation(1, 22),
            // (9,40): error CS0122: 'C<T1>.Nested<U>' is inaccessible due to its protection level
            // implicit extension E<T1, T2> for C<T1>.Nested<T2>
            Diagnostic(ErrorCode.ERR_BadAccess, "Nested<T2>").WithArguments("C<T1>.Nested<U>").WithLocation(9, 40)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<string>.Nested<int>");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
    }

    [Fact]
    public void GenericExtension_Tuples()
    {
        var src = """
string s = (string, string).Nested.f;
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,13): error CS1525: Invalid expression term 'string'
            // string s = (string, string).Nested.f;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "string").WithArguments("string").WithLocation(1, 13),
            // (1,21): error CS1525: Invalid expression term 'string'
            // string s = (string, string).Nested.f;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "string").WithArguments("string").WithLocation(1, 21)
            );
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GenericExtension_NestedTuples()
    {
        var src = """
string s = C<(string, string)>.Nested<(int, int)>.f;
System.Console.Write(s);

class C<T>
{
    internal class Nested<U> { }
}

implicit extension E<T1, T2> for C<(T1, T1)>.Nested<(T2, T2)>
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<(string, string)>.Nested<(int, int)>.f");
        Assert.Equal("System.String E<System.String, System.Int32>.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GenericExtension_PointerArray()
    {
        var src = """
unsafe
{
    string s = C<long*[]>.Nested<int*[]>.f;
    System.Console.Write(s);
}

unsafe class C<T>
{
    internal class Nested<U> { }
}

unsafe implicit extension E<T1, T2> for C<T1*[]>.Nested<T2*[]>
    where T1 : unmanaged
    where T2 : unmanaged
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.UnsafeDebugExe);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<long*[]>.Nested<int*[]>.f");
        Assert.Equal("System.String E<System.Int64, System.Int32>.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GenericExtension_Pointer()
    {
        // A type parameter cannot unify with a pointer type
        var src = """
unsafe
{
    string s = C<long*[]>.Nested<int*[]>.f;
    System.Console.Write(s);
}

unsafe class C<T>
{
    internal class Nested<U> { }
}

unsafe implicit extension E<T1, T2> for C<T1[]>.Nested<T2[]>
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.UnsafeDebugExe);
        comp.VerifyDiagnostics(
            // (3,42): error CS0117: 'C<long*[]>.Nested<int*[]>' does not contain a definition for 'f'
            //     string s = C<long*[]>.Nested<int*[]>.f;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "f").WithArguments("C<long*[]>.Nested<int*[]>", "f").WithLocation(3, 42)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<long*[]>.Nested<int*[]>.f");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GenericExtension_FunctionPointer()
    {
        // PROTOTYPE type unification should handle function pointer types
        var src = """
unsafe
{
    string s = C<delegate*<int>[]>.Nested<delegate*<long>[]>.f;
    System.Console.Write(s);
}

unsafe class C<T>
{
    internal class Nested<U> { }
}

unsafe implicit extension E<T1, T2> for C<delegate*<T1>[]>.Nested<delegate*<T2>[]>
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70, options: TestOptions.UnsafeDebugExe);
        comp.VerifyDiagnostics(
            // (3,62): error CS0117: 'C<delegate*<int>[]>.Nested<delegate*<long>[]>' does not contain a definition for 'f'
            //     string s = C<delegate*<int>[]>.Nested<delegate*<long>[]>.f;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "f").WithArguments("C<delegate*<int>[]>.Nested<delegate*<long>[]>", "f").WithLocation(3, 62)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<delegate*<int>[]>.Nested<delegate*<long>[]>.f");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GenericExtension_OccursCheck()
    {
        var src = """
class C<T>
{
    internal class Nested<U> { }
}

implicit extension E<T1, T2> for C<T1>.Nested<T2>
{
    public static string f = "hi";

    public static void M()
    {
        string s = C<C<T1>>.Nested<int>.f;
        System.Console.Write(s);
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (12,41): error CS0117: 'C<C<T1>>.Nested<int>' does not contain a definition for 'f'
            //         string s = C<C<T1>>.Nested<int>.f;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "f").WithArguments("C<C<T1>>.Nested<int>", "f").WithLocation(12, 41)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<C<T1>>.Nested<int>.f");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GenericExtension_ForInterface()
    {
        var src = """
string s = C<int>.f;
System.Console.Write(s);

class C<T> : I<T> { }
interface I<T> { }

implicit extension E<T> for I<T>
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);

        CompileAndVerify(comp, expectedOutput: "hi", verify: Verification.FailsPEVerify)
           .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<int>.f");
        Assert.Equal("System.String E<System.Int32>.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GenericExtension_ForBaseInterface()
    {
        var src = """
string s = C<int>.f;
System.Console.Write(s);

class C<T> : I<T> { }
interface I<T> : I2<T> { }
interface I2<T> { }

implicit extension E<T> for I2<T>
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);

        CompileAndVerify(comp, expectedOutput: "hi", verify: Verification.FailsPEVerify)
           .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<int>.f");
        Assert.Equal("System.String E<System.Int32>.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GenericExtension_ForBase()
    {
        var src = """
string s = C<int, string>.f;
System.Console.Write(s);

class Base<T, U> { }
class C<T, U> : Base<U, T> { }

implicit extension E<T, U> for Base<T, U>
{
    public static string f = "hi";
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);

        CompileAndVerify(comp, expectedOutput: "hi", verify: Verification.FailsPEVerify)
           .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<int, string>.f");
        Assert.Equal("System.String E<System.String, System.Int32>.f", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void UnderlyingType_SelfReference_Generic()
    {
        // While binding the underlying type of an extension type,
        // we cannot afford to involve the extension type (that would be a cycle)
        var src = """
string s = C<string>.Nested<int>.f;
System.Console.Write(s);

class C<T>
{
}

implicit extension E<T1, T2> for C<T1>.Nested<T2>
{
    public static string f = "hi";
    public class Nested<U> { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE could give a better diagnostic, such as the underlying type of an extension type may not rely on the extension declaration
        comp.VerifyDiagnostics(
            // (1,22): error CS0117: 'C<string>' does not contain a definition for 'Nested'
            // string s = C<string>.Nested<int>.f;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Nested<int>").WithArguments("C<string>", "Nested").WithLocation(1, 22),
            // (8,40): error CS0426: The type name 'Nested<>' does not exist in the type 'C<T1>'
            // implicit extension E<T1, T2> for C<T1>.Nested<T2>
            Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "Nested<T2>").WithArguments("Nested<>", "C<T1>").WithLocation(8, 40)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C<string>.Nested<int>");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
    }

    [Fact]
    public void UnderlyingType_SelfReference()
    {
        // While binding the underlying type of an extension type,
        // we cannot afford to involve the extension type (that would be a cycle)
        var src = """
string s = C.Nested.f;
System.Console.Write(s);

class C
{
}

implicit extension E for C.Nested
{
    public static string f = "hi";
    public class Nested { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (1,14): error CS0117: 'C' does not contain a definition for 'Nested'
            // string s = C.Nested.f;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Nested").WithArguments("C", "Nested").WithLocation(1, 14),
            // (8,28): error CS0426: The type name 'Nested' does not exist in the type 'C'
            // implicit extension E for C.Nested
            Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "Nested").WithArguments("Nested", "C").WithLocation(8, 28)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Nested");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
    }
}

