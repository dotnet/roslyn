// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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
        Assert.True(type is T);
        Assert.False(type.IsExtension);
        Assert.Null(type.ExtensionUnderlyingTypeNoUseSiteDiagnostics);
        Assert.Empty(type.BaseExtensionsNoUseSiteDiagnostics);
    }

    // Verify things that are common for all extension types
    private static void VerifyExtension<T>(TypeSymbol type, bool isManaged = false) where T : TypeSymbol
    {
        Assert.True(type is T);
        Assert.True(type.IsExtension);
        Assert.Null(type.BaseTypeNoUseSiteDiagnostics);
        Assert.False(type.IsSealed); // PROTOTYPE
        Assert.False(type.IsRecord);
        Assert.False(type.IsRecordStruct);
        Assert.False(type.IsReferenceType);
        Assert.False(type.IsValueType);
        Assert.False(type.IsTypeParameter());
        Assert.False(type.IsAnonymousType);
        Assert.False(type.IsEnumType());
        Assert.False(type.IsErrorType());
        Assert.Equal(SpecialType.None, type.SpecialType);
        Assert.False(type.IsObjectType());
        Assert.False(type.IsTupleType);
        Assert.True(type.TupleElements.IsDefault);
        Assert.Empty(type.InterfacesNoUseSiteDiagnostics());
        Assert.Empty(type.AllInterfacesNoUseSiteDiagnostics); // PROTOTYPE
        Assert.False(type.IsReadOnly);
        Assert.False(type.IsRefLikeType);
        Assert.False(type.IsUnsafe());
        Assert.Equal(TypeKind.Extension, type.TypeKind);
        Assert.False(type.IsInterfaceType());
        Assert.False(type.IsAbstract);

        if (type.ExtensionUnderlyingTypeNoUseSiteDiagnostics is { } underlyingType)
        {
            VerifyNotExtension<TypeSymbol>(underlyingType);
        }

        if (type != (object)type.OriginalDefinition)
        {
            VerifyExtension<TypeSymbol>(type.OriginalDefinition);
        }

        foreach (var baseExtension in type.BaseExtensionsNoUseSiteDiagnostics)
        {
            VerifyExtension<TypeSymbol>(baseExtension);
        }

        var managedKindUseSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(type.ContainingAssembly);
        Assert.Equal(isManaged, type.IsManagedType(ref managedKindUseSiteInfo));

        Assert.False(type.IsRestrictedType());
        Assert.True(type.IsType);
        Assert.True(type.CanBeReferencedByName);

        if (type is NamedTypeSymbol namedType)
        {
            Assert.False(namedType.IsCustomTaskType(out _));
            Assert.Null(namedType.DelegateInvokeMethod);
            Assert.False(namedType.HasAnyRequiredMembers);
            Assert.False(namedType.IsNamespace);
        }

        if (type is SourceNamedTypeSymbol sourceNamedType)
        {
            Assert.False(sourceNamedType.IsScriptClass);
            Assert.Null(sourceNamedType.EnumUnderlyingType);
            Assert.False(sourceNamedType.HasStructLayoutAttribute);
            Assert.False(sourceNamedType.IsAnonymousType);
            Assert.False(sourceNamedType.IsSimpleProgram);
            Assert.False(sourceNamedType.IsImplicitlyDeclared);
        }
    }

    [Fact]
    public void ForClass()
    {
        var src = """
interface I { }
class UnderlyingClass : I { }
explicit extension R for UnderlyingClass
{
    public static int StaticField = 0;
    const string Const = "hello";

    void Method() { }
    static void StaticMethod() { }

    int Property { get => throw null; set => throw null; }
    static int StaticProperty { get => throw null; set => throw null; }
    int this[int i] => throw null;

    class NestedType { }
    static class StaticNestedType { }
    explicit extension NestedR for UnderlyingClass { }
    R(int i) { }
    public static implicit operator R(int i) => throw null;
    public static implicit operator R(UnderlyingClass c) => throw null;
    public static implicit operator UnderlyingClass(R r) => throw null;
    public static int operator+(R r, UnderlyingClass c) => throw null;
    public static int operator-(UnderlyingClass c, R r) => throw null;
}
""";
        var comp = CreateCompilation(src);
        // PROTOTYPE need to finalize the rules for operators (conversion and others)
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
        AssertEx.Equal(new[]
            {
                "System.Int32 R.StaticField", "System.String R.Const",
                "void R.Method()", "void R.StaticMethod()",
                "System.Int32 R.Property { get; set; }", "System.Int32 R.Property.get", "void R.Property.set",
                "System.Int32 R.StaticProperty { get; set; }", "System.Int32 R.StaticProperty.get", "void R.StaticProperty.set",
                "System.Int32 R.this[System.Int32 i] { get; }", "System.Int32 R.this[System.Int32 i].get",
                "R.NestedType",
                "R.StaticNestedType",
                "R.NestedR",
                "R..ctor(System.Int32 i)",
                "R R.op_Implicit(System.Int32 i)",
                "R R.op_Implicit(UnderlyingClass c)",
                "UnderlyingClass R.op_Implicit(R r)",
                "System.Int32 R.op_Addition(R r, UnderlyingClass c)",
                "System.Int32 R.op_Subtraction(UnderlyingClass c, R r)",
                "R..cctor()"
            },
            r.GetMembers().ToTestDisplayStrings());

        AssertEx.Equal(new[]
            {
                "StaticField",
                "Const",
                "Method",
                "StaticMethod",
                "Property",
                "StaticProperty",
                "this[]",
                ".ctor",
                "op_Implicit",
                "op_Addition",
                "op_Subtraction"
            },
            r.MemberNames);

        AssertEx.Equal(new[] { "R.NestedType", "R.StaticNestedType", "R.NestedR" },
            r.GetTypeMembers().ToTestDisplayStrings());

        Assert.False(r.IsStatic);
        Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
        Assert.Null(r.ContainingType);
        Assert.False(r.IsGenericType);
        Assert.Empty(r.TypeParameters);
        Assert.True(r.IsDefinition);
        Assert.Equal(0, r.Arity);
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("ContainingType").GetTypeMember("R");
        Assert.Equal("ContainingType.R", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (5,16): error CS9113: 'R.field': cannot declare instance members with state in extension types.
            //     public int field = 0; // 1, 2
            Diagnostic(ErrorCode.ERR_StateInExtension, "field").WithArguments("R.field").WithLocation(5, 16),
            // (5,16): warning CS0649: Field 'R.field' is never assigned to, and will always have its default value 0
            //     public int field = 0; // 1, 2
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("R.field", "0").WithLocation(5, 16),
            // (6,25): error CS9113: 'R.field2': cannot declare instance members with state in extension types.
            //     public volatile int field2 = 0; // 3, 4
            Diagnostic(ErrorCode.ERR_StateInExtension, "field2").WithArguments("R.field2").WithLocation(6, 25),
            // (6,25): warning CS0649: Field 'R.field2' is never assigned to, and will always have its default value 0
            //     public volatile int field2 = 0; // 3, 4
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field2").WithArguments("R.field2", "0").WithLocation(6, 25),
            // (7,9): error CS9113: 'R.AutoProperty': cannot declare instance members with state in extension types.
            //     int AutoProperty { get; set; } // 5
            Diagnostic(ErrorCode.ERR_StateInExtension, "AutoProperty").WithArguments("R.AutoProperty").WithLocation(7, 9),
            // (8,9): error CS9113: 'R.AutoPropertyWithGetAccessor': cannot declare instance members with state in extension types.
            //     int AutoPropertyWithGetAccessor { get; } // 6
            Diagnostic(ErrorCode.ERR_StateInExtension, "AutoPropertyWithGetAccessor").WithArguments("R.AutoPropertyWithGetAccessor").WithLocation(8, 9),
            // (9,42): error CS8051: Auto-implemented properties must have get accessors.
            //     int AutoPropertyWithoutGetAccessor { set; } // 7
            Diagnostic(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, "set").WithLocation(9, 42),
            // (10,32): error CS9113: 'R.Event': cannot declare instance members with state in extension types.
            //     public event System.Action Event; // 8, 9
            Diagnostic(ErrorCode.ERR_StateInExtension, "Event").WithArguments("R.Event").WithLocation(10, 32),
            // (10,32): warning CS0067: The event 'R.Event' is never used
            //     public event System.Action Event; // 8, 9
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event").WithArguments("R.Event").WithLocation(10, 32)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r, isManaged: true);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
        AssertEx.Equal(new[]
            {
                "event System.Action R.Event",
                "void R.Event.add",
                "void R.Event.remove"
            },
            r.GetMembers().ToTestDisplayStrings());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (4,17): error CS0542: 'R': member names cannot be the same as their enclosing type
            //     public void R() { }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "R").WithArguments("R").WithLocation(4, 17)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
        AssertEx.Equal(new[] { "void R.R()" }, r.GetMembers().ToTestDisplayStrings());
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
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
    public void Members_NoDefaultCtor(string type)
    {
        var src = $$"""
{{type}} UnderlyingType { }
explicit extension R for UnderlyingType { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Empty(r.GetMembers());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (4,31): error CS0563: One of the parameters of a binary operator must be the containing type
            //     public static int operator+(UnderlyingClass c1, UnderlyingClass c2) => throw null;
            Diagnostic(ErrorCode.ERR_BadBinaryOperatorSignature, "+").WithLocation(4, 31)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        AssertEx.Equal(new[]
            {
                "System.Int32 R.op_Addition(UnderlyingClass c1, UnderlyingClass c2)"
            },
            r.GetMembers().ToTestDisplayStrings());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (8,10): error CS0708: 'Method': cannot declare instance members in a static type
            //     void Method() { } // 1
            Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "Method").WithArguments("Method").WithLocation(8, 10),
            // (11,9): error CS0708: 'R.Property': cannot declare instance members in a static type
            //     int Property { get => throw null; set => throw null; } // 2
            Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "Property").WithArguments("R.Property").WithLocation(11, 9),
            // (13,9): error CS0720: 'R.this[int]': cannot declare indexers in a static class
            //     int this[int i] => throw null; // 3
            Diagnostic(ErrorCode.ERR_IndexerInStaticClass, "this").WithArguments("R.this[int]").WithLocation(13, 9)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
        AssertEx.Equal(new[]
            {
                "System.Int32 R.StaticField", "System.String R.Const",
                "void R.Method()", "void R.StaticMethod()",
                "System.Int32 R.Property { get; set; }", "System.Int32 R.Property.get", "void R.Property.set",
                "System.Int32 R.StaticProperty { get; set; }", "System.Int32 R.StaticProperty.get", "void R.StaticProperty.set",
                "System.Int32 R.this[System.Int32 i] { get; }", "System.Int32 R.this[System.Int32 i].get",
                "R.NestedType", "R.StaticNestedType", "R.NestedR",
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (11,12): error CS0541: 'R1.M()': explicit interface declaration can only be declared in a class, record, struct or interface
            //     void I.M() { }
            Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M").WithArguments("R1.M()").WithLocation(11, 12),
            // (13,45): error CS9107: 'I' is not an un-annotated extension type.
            // explicit extension R2 for UnderlyingClass : I
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "I").WithArguments("I").WithLocation(13, 45),
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
}
partial explicit extension R for UnderlyingClass
{
    public partial int MethodPartial(int i) => 1;
}
""";
        // PROTOTYPE should warn that `new` isn't required
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net60);
        comp.VerifyDiagnostics(
            // (9,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     int* MethodNotUnsafe(int* i) => i; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(9, 5),
            // (9,26): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     int* MethodNotUnsafe(int* i) => i; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(9, 26),
            // (9,37): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     int* MethodNotUnsafe(int* i) => i; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "i").WithLocation(9, 37),
            // (19,24): warning CS0626: Method, operator, or accessor 'R.MethodExtern()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
            //     static extern void MethodExtern(); // 4
            Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "MethodExtern").WithArguments("R.MethodExtern()").WithLocation(19, 24)
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
        Assert.True(r.GetMethod("MethodAsync").IsAsync);
        Assert.True(r.GetMethod("MethodExtern").IsExtern);
        Assert.True(r.GetMethod("MethodExtern2").IsExtern);
        Assert.True(r.GetMethod("MethodStatic").IsStatic);
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

        // PROTOTYPE should warn about missing `new`
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            );
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
    protected void M2() { } // 4
    readonly void M3() { } // 5
    sealed void M4() { } // 6
    virtual void M5() { } // 7
    required void M6() { } // 8
    scoped System.Span<int> M7() => throw null; // 9
    private protected void M8() { } // 10
    internal protected void M9() { } // 11
    file void M10() { } // 12
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net60);
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
            // (7,20): error CS0106: The modifier 'protected' is not valid for this item
            //     protected void M2() { } // 4
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M2").WithArguments("protected").WithLocation(7, 20),
            // (8,19): error CS0106: The modifier 'readonly' is not valid for this item
            //     readonly void M3() { } // 5
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M3").WithArguments("readonly").WithLocation(8, 19),
            // (9,17): error CS0106: The modifier 'sealed' is not valid for this item
            //     sealed void M4() { } // 6
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M4").WithArguments("sealed").WithLocation(9, 17),
            // (10,18): error CS0106: The modifier 'virtual' is not valid for this item
            //     virtual void M5() { } // 7
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M5").WithArguments("virtual").WithLocation(10, 18),
            // (11,19): error CS0106: The modifier 'required' is not valid for this item
            //     required void M6() { } // 8
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M6").WithArguments("required").WithLocation(11, 19),
            // (12,29): error CS0106: The modifier 'scoped' is not valid for this item
            //     scoped System.Span<int> M7() => throw null; // 9
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M7").WithArguments("scoped").WithLocation(12, 29),
            // (13,28): error CS0106: The modifier 'private protected' is not valid for this item
            //     private protected void M8() { } // 10
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M8").WithArguments("private protected").WithLocation(13, 28),
            // (14,29): error CS0106: The modifier 'protected internal' is not valid for this item
            //     internal protected void M9() { } // 11
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M9").WithArguments("protected internal").WithLocation(14, 29),
            // (15,15): error CS0106: The modifier 'file' is not valid for this item
            //     file void M10() { } // 12
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("file").WithLocation(15, 15)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        MethodSymbol m1 = r.GetMethod("M1");
        Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility);
        Assert.False(m1.IsAbstract);
        Assert.Equal(Accessibility.Private, r.GetMethod("M2").DeclaredAccessibility);
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
    unsafe int* Unsafe => null;
    int* NotUnsafe => null; // 1
    new int NotNew => 0;
    ref int RefInt => throw null;
    static int Static => 0;

    extern int Extern { get; } // 2
    static extern int Extern2 { [System.Runtime.InteropServices.DllImport("test")] get; }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        // PROTOTYPE should warn that `new` isn't required
        comp.VerifyDiagnostics(
            // (9,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     int* NotUnsafe => null; // 1
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(9, 5),
            // (14,25): warning CS0626: Method, operator, or accessor 'R.Extern.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
            //     extern int Extern { get; } // 2
            Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "get").WithArguments("R.Extern.get").WithLocation(14, 25)
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
        Assert.True(r.GetProperty("Static").IsStatic);

        var externProperty = r.GetProperty("Extern");
        Assert.True(externProperty.IsExtern);
        Assert.True(externProperty.GetMethod.IsExtern);

        var externProperty2 = r.GetProperty("Extern2");
        Assert.True(externProperty2.IsExtern);
        Assert.True(externProperty2.IsStatic);
        Assert.True(externProperty2.GetMethod.IsExtern);
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
    public int Property => 0;
}
""";
        var comp = CreateCompilation(src);
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
    protected int Protected => 0; // 8
    readonly int Readonly => 0; // 9
    sealed int Sealed => 0; // 10
    public virtual int Virtual => 0; // 11

    public required int Required { get => throw null; set => throw null; } // 12
    public static required int StaticRequired { get => throw null; set => throw null; } // 13

    private protected int PrivateProtected => 0; // 14
    internal protected int InternalProtected => 0; // 15
    file int File => 0; // 16
}
""";
        // PROTOTYPE confirm spec on `required`
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (4,15): error CS0106: The modifier 'async' is not valid for this item
            //     async int Async => 0; // 1
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Async").WithArguments("async").WithLocation(4, 15),
            // (5,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
            //     partial int Partial { get; } // 2, 3
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(5, 5),
            // (5,17): error CS9113: 'R.Partial': cannot declare instance members with state in extension types.
            //     partial int Partial { get; } // 2, 3
            Diagnostic(ErrorCode.ERR_StateInExtension, "Partial").WithArguments("R.Partial").WithLocation(5, 17),
            // (6,29): error CS0106: The modifier 'scoped' is not valid for this item
            //     scoped System.Span<int> Scoped => throw null; // 4
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Scoped").WithArguments("scoped").WithLocation(6, 29),
            // (7,18): error CS0106: The modifier 'abstract' is not valid for this item
            //     abstract int Abstract { get; } // 5, 6
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Abstract").WithArguments("abstract").WithLocation(7, 18),
            // (7,18): error CS9113: 'R.Abstract': cannot declare instance members with state in extension types.
            //     abstract int Abstract { get; } // 5, 6
            Diagnostic(ErrorCode.ERR_StateInExtension, "Abstract").WithArguments("R.Abstract").WithLocation(7, 18),
            // (8,18): error CS0106: The modifier 'override' is not valid for this item
            //     override int Override => 0; // 7
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Override").WithArguments("override").WithLocation(8, 18),
            // (9,19): error CS0106: The modifier 'protected' is not valid for this item
            //     protected int Protected => 0; // 8
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Protected").WithArguments("protected").WithLocation(9, 19),
            // (10,18): error CS0106: The modifier 'readonly' is not valid for this item
            //     readonly int Readonly => 0; // 9
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Readonly").WithArguments("readonly").WithLocation(10, 18),
            // (11,16): error CS0106: The modifier 'sealed' is not valid for this item
            //     sealed int Sealed => 0; // 10
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Sealed").WithArguments("sealed").WithLocation(11, 16),
            // (12,24): error CS0106: The modifier 'virtual' is not valid for this item
            //     public virtual int Virtual => 0; // 11
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Virtual").WithArguments("virtual").WithLocation(12, 24),
            // (14,25): error CS0106: The modifier 'required' is not valid for this item
            //     public required int Required { get => throw null; set => throw null; } // 12
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Required").WithArguments("required").WithLocation(14, 25),
            // (15,32): error CS0106: The modifier 'required' is not valid for this item
            //     public static required int StaticRequired { get => throw null; set => throw null; } // 13
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "StaticRequired").WithArguments("required").WithLocation(15, 32),
            // (17,27): error CS0106: The modifier 'private protected' is not valid for this item
            //     private protected int PrivateProtected => 0; // 14
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "PrivateProtected").WithArguments("private protected").WithLocation(17, 27),
            // (18,28): error CS0106: The modifier 'protected internal' is not valid for this item
            //     internal protected int InternalProtected => 0; // 15
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "InternalProtected").WithArguments("protected internal").WithLocation(18, 28),
            // (19,14): error CS0106: The modifier 'file' is not valid for this item
            //     file int File => 0; // 16
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "File").WithArguments("file").WithLocation(19, 14)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.False(r.GetProperty("Abstract").IsAbstract);
        Assert.False(r.GetProperty("Override").IsOverride);
        Assert.Equal(Accessibility.Private, r.GetProperty("Protected").DeclaredAccessibility);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (4,16): error CS0106: The modifier 'static' is not valid for this item
            //     static int this[int i] => i;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(4, 16)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        AssertEx.Equal(new[]
            {
               "System.Int32 R.this[System.Int32 i] { get; }", "System.Int32 R.this[System.Int32 i].get"
            },
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
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        // PROTOTYPE should warn that `new` isn't required
        comp.VerifyDiagnostics(
            // (9,43): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     event System.Action NotUnsafe { add { int* i = null; } remove => throw null; } // 1
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(9, 43),
            // (13,41): error CS0179: 'R.Extern.add' cannot be extern and declare a body
            //     extern event System.Action Extern { add => throw null; remove => throw null; } // 2, 3
            Diagnostic(ErrorCode.ERR_ExternHasBody, "add").WithArguments("R.Extern.add").WithLocation(13, 41),
            // (13,60): error CS0179: 'R.Extern.remove' cannot be extern and declare a body
            //     extern event System.Action Extern { add => throw null; remove => throw null; } // 2, 3
            Diagnostic(ErrorCode.ERR_ExternHasBody, "remove").WithArguments("R.Extern.remove").WithLocation(13, 60),
            // (17,63): error CS0073: An add or remove accessor must have a body
            //         [System.Runtime.InteropServices.DllImport("test")] add; // 4
            Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(17, 63),
            // (18,66): error CS0073: An add or remove accessor must have a body
            //         [System.Runtime.InteropServices.DllImport("test")] remove; // 5
            Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(18, 66)
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
    public event System.Action Event { add => throw null; remove => throw null; }
}
""";
        var comp = CreateCompilation(src);
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
    protected event System.Action Protected { add => throw null; remove => throw null; } // 5
    readonly event System.Action Readonly { add => throw null; remove => throw null; } // 6
    sealed event System.Action Sealed { add => throw null; remove => throw null; } // 7
    public virtual event System.Action Virtual { add => throw null; remove => throw null; } // 8
    public required event System.Action Required { add => throw null; remove => throw null; } // 9
    public static required event System.Action StaticRequired { add => throw null; remove => throw null; } // 10
    private protected event System.Action PrivateProtected { add => throw null; remove => throw null; } // 11
    internal protected event System.Action InternalProtected { add => throw null; remove => throw null; } // 12
    file event System.Action File { add => throw null; remove => throw null; } // 13
}
""";
        var comp = CreateCompilation(src);
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
            // (8,35): error CS0106: The modifier 'protected' is not valid for this item
            //     protected event System.Action Protected { add => throw null; remove => throw null; } // 5
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Protected").WithArguments("protected").WithLocation(8, 35),
            // (9,34): error CS0106: The modifier 'readonly' is not valid for this item
            //     readonly event System.Action Readonly { add => throw null; remove => throw null; } // 6
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Readonly").WithArguments("readonly").WithLocation(9, 34),
            // (10,32): error CS0106: The modifier 'sealed' is not valid for this item
            //     sealed event System.Action Sealed { add => throw null; remove => throw null; } // 7
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Sealed").WithArguments("sealed").WithLocation(10, 32),
            // (11,40): error CS0106: The modifier 'virtual' is not valid for this item
            //     public virtual event System.Action Virtual { add => throw null; remove => throw null; } // 8
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Virtual").WithArguments("virtual").WithLocation(11, 40),
            // (12,41): error CS0106: The modifier 'required' is not valid for this item
            //     public required event System.Action Required { add => throw null; remove => throw null; } // 9
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Required").WithArguments("required").WithLocation(12, 41),
            // (13,48): error CS0106: The modifier 'required' is not valid for this item
            //     public static required event System.Action StaticRequired { add => throw null; remove => throw null; } // 10
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "StaticRequired").WithArguments("required").WithLocation(13, 48),
            // (14,43): error CS0106: The modifier 'private protected' is not valid for this item
            //     private protected event System.Action PrivateProtected { add => throw null; remove => throw null; } // 11
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "PrivateProtected").WithArguments("private protected").WithLocation(14, 43),
            // (15,44): error CS0106: The modifier 'protected internal' is not valid for this item
            //     internal protected event System.Action InternalProtected { add => throw null; remove => throw null; } // 12
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "InternalProtected").WithArguments("protected internal").WithLocation(15, 44),
            // (16,30): error CS0106: The modifier 'file' is not valid for this item
            //     file event System.Action File { add => throw null; remove => throw null; } // 13
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "File").WithArguments("file").WithLocation(16, 30)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.False(r.GetEvent("Abstract").IsAbstract);
        Assert.False(r.GetEvent("Override").IsOverride);
        Assert.Equal(Accessibility.Private, r.GetEvent("Protected").DeclaredAccessibility);
        Assert.False(r.GetEvent("Sealed").IsSealed);
        Assert.False(r.GetEvent("Virtual").IsVirtual);
    }

    [Fact]
    public void Members_ExtensionMethod()
    {
        var src = """
class UnderlyingClass { }
explicit extension R1 for UnderlyingClass
{
    public void M(this int i) { } // 1
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,20): error CS1106: Extension method must be defined in a non-generic static class
            // explicit extension R1 for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadExtensionAgg, "R1").WithLocation(2, 20)
            );
    }

    [Fact]
    public void Members_Delegate()
    {
        var src = """
explicit extension R for int
{
    delegate void Delegate();
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var d = comp.GlobalNamespace.GetTypeMember("R").GetTypeMember("Delegate");
        Assert.Equal("R.Delegate", d.ToTestDisplayString());
        Assert.Equal("R", d.ContainingType.ToTestDisplayString());
        Assert.True(d.IsDelegateType());
    }

    [Fact]
    public void Members_Struct()
    {
        var src = """
explicit extension R for int
{
    struct S { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var d = comp.GlobalNamespace.GetTypeMember("R").GetTypeMember("S");
        Assert.Equal("R.S", d.ToTestDisplayString());
        Assert.Equal("R", d.ContainingType.ToTestDisplayString());
    }

    [Fact]
    public void Members_Struct_AllowedModifiers()
    {
        var src = """
explicit extension R for int
{
    struct DefaultAccessibility { }
    public struct Public { }
    private struct Private { }
    internal struct Internal { }
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
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (8,31): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     struct NotUnsafe { void M(int* i) => throw null; } // 1
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(8, 31)
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
        Assert.True(r.GetTypeMember("Readonly").IsReadOnly);
    }

    [Fact]
    public void Members_Struct_DisallowedModifiers()
    {
        var src = """
explicit extension R for int
{
    async struct Async { } // 1
    abstract struct Abstract { } // 2
    override struct Override { } // 3
    protected struct Protected { } // 4
    private protected struct PrivateProtected { } // 5
    internal protected struct InternalProtected { } // 6
    sealed struct Sealed { } // 7
    virtual struct Virtual { } // 8
    required struct Required { } // 9
    file struct File { } // 10
    static struct Static { } // 11
    ref record struct RefRecordStruct { } // 12
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
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
            // (6,22): error CS0106: The modifier 'protected' is not valid for this item
            //     protected struct Protected { } // 4
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Protected").WithArguments("protected").WithLocation(6, 22),
            // (7,30): error CS0106: The modifier 'private protected' is not valid for this item
            //     private protected struct PrivateProtected { } // 5
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "PrivateProtected").WithArguments("private protected").WithLocation(7, 30),
            // (8,31): error CS0106: The modifier 'protected internal' is not valid for this item
            //     internal protected struct InternalProtected { } // 6
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "InternalProtected").WithArguments("protected internal").WithLocation(8, 31),
            // (9,19): error CS0106: The modifier 'sealed' is not valid for this item
            //     sealed struct Sealed { } // 7
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Sealed").WithArguments("sealed").WithLocation(9, 19),
            // (10,20): error CS0106: The modifier 'virtual' is not valid for this item
            //     virtual struct Virtual { } // 8
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Virtual").WithArguments("virtual").WithLocation(10, 20),
            // (11,21): error CS0106: The modifier 'required' is not valid for this item
            //     required struct Required { } // 9
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Required").WithArguments("required").WithLocation(11, 21),
            // (12,17): error CS9054: File-local type 'R.File' must be defined in a top level type; 'R.File' is a nested type.
            //     file struct File { } // 10
            Diagnostic(ErrorCode.ERR_FileTypeNested, "File").WithArguments("R.File").WithLocation(12, 17),
            // (13,19): error CS0106: The modifier 'static' is not valid for this item
            //     static struct Static { } // 11
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Static").WithArguments("static").WithLocation(13, 19),
            // (14,23): error CS0106: The modifier 'ref' is not valid for this item
            //     ref record struct RefRecordStruct { } // 12
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "RefRecordStruct").WithArguments("ref").WithLocation(14, 23)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.False(r.GetTypeMember("Abstract").IsAbstract);
        Assert.False(r.GetTypeMember("Override").IsOverride);
        Assert.Equal(Accessibility.Private, r.GetTypeMember("Protected").DeclaredAccessibility);
        Assert.Equal(Accessibility.Private, r.GetTypeMember("PrivateProtected").DeclaredAccessibility);
        Assert.Equal(Accessibility.Private, r.GetTypeMember("InternalProtected").DeclaredAccessibility);
        Assert.True(r.GetTypeMember("Sealed").IsSealed);
        Assert.False(r.GetTypeMember("Virtual").IsVirtual);
    }

    [Fact]
    public void Members_Class_AllowedModifiers()
    {
        var src = """
explicit extension R for int
{
    class DefaultAccessibility { }
    public class Public { }
    private class Private { }
    internal class Internal { }
    unsafe class Unsafe { void M(int* i) => throw null; }
    class NotUnsafe { void M(int* i) => throw null; } // 1
    new class NotNew { }
    partial class Partial { }
    sealed class Sealed { }
    static class Static { }
}
""";
        // PROTOTYPE should warn that `new` isn't required
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (8,30): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     class NotUnsafe { void M(int* i) => throw null; } // 1
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(8, 30)
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
        Assert.True(r.GetTypeMember("Sealed").IsSealed);
    }

    [Fact]
    public void Members_Class_DisallowedModifiers()
    {
        var src = """
explicit extension R for int
{
    async class Async { } // 1
    abstract class Abstract { } // 2
    override class Override { } // 3
    protected class Protected { } // 4
    private protected class PrivateProtected { } // 5
    internal protected class InternalProtected { } // 6
    virtual class Virtual { } // 8
    required class Required { } // 9
    file class File { } // 10
    readonly class Readonly { } // 11
    static record StaticRecord { } // 12
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (3,17): error CS0106: The modifier 'async' is not valid for this item
            //     async class Async { } // 1
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Async").WithArguments("async").WithLocation(3, 17),
            // (4,20): error CS0106: The modifier 'abstract' is not valid for this item
            //     abstract class Abstract { } // 2
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Abstract").WithArguments("abstract").WithLocation(4, 20),
            // (5,20): error CS0106: The modifier 'override' is not valid for this item
            //     override class Override { } // 3
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Override").WithArguments("override").WithLocation(5, 20),
            // (6,21): error CS0106: The modifier 'protected' is not valid for this item
            //     protected class Protected { } // 4
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Protected").WithArguments("protected").WithLocation(6, 21),
            // (7,29): error CS0106: The modifier 'private protected' is not valid for this item
            //     private protected class PrivateProtected { } // 5
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "PrivateProtected").WithArguments("private protected").WithLocation(7, 29),
            // (8,30): error CS0106: The modifier 'protected internal' is not valid for this item
            //     internal protected class InternalProtected { } // 6
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "InternalProtected").WithArguments("protected internal").WithLocation(8, 30),
            // (9,19): error CS0106: The modifier 'virtual' is not valid for this item
            //     virtual class Virtual { } // 8
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Virtual").WithArguments("virtual").WithLocation(9, 19),
            // (10,20): error CS0106: The modifier 'required' is not valid for this item
            //     required class Required { } // 9
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Required").WithArguments("required").WithLocation(10, 20),
            // (11,16): error CS9054: File-local type 'R.File' must be defined in a top level type; 'R.File' is a nested type.
            //     file class File { } // 10
            Diagnostic(ErrorCode.ERR_FileTypeNested, "File").WithArguments("R.File").WithLocation(11, 16),
            // (12,20): error CS0106: The modifier 'readonly' is not valid for this item
            //     readonly class Readonly { } // 11
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Readonly").WithArguments("readonly").WithLocation(12, 20),
            // (13,19): error CS0106: The modifier 'static' is not valid for this item
            //     static record StaticRecord { } // 12
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "StaticRecord").WithArguments("static").WithLocation(13, 19)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.False(r.GetTypeMember("Abstract").IsAbstract);
        Assert.False(r.GetTypeMember("Override").IsOverride);
        Assert.Equal(Accessibility.Private, r.GetTypeMember("Protected").DeclaredAccessibility);
        Assert.Equal(Accessibility.Private, r.GetTypeMember("PrivateProtected").DeclaredAccessibility);
        Assert.Equal(Accessibility.Private, r.GetTypeMember("InternalProtected").DeclaredAccessibility);
        Assert.False(r.GetTypeMember("Virtual").IsVirtual);
        Assert.False(r.GetTypeMember("Readonly").IsReadOnly);
    }

    [Fact]
    public void Members_Interface_AllowedModifiers()
    {
        var src = """
explicit extension R for int
{
    interface DefaultAccessibility { }
    public interface Public { }
    private interface Private { }
    internal interface Internal { }
    unsafe interface Unsafe { void M(int* i) => throw null; }
    interface NotUnsafe { void M(int* i) => throw null; } // 1
    new interface NotNew { }
    partial interface Partial { }
}
""";
        // PROTOTYPE should warn that `new` isn't required
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // (8,34): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     interface NotUnsafe { void M(int* i) => throw null; } // 1
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(8, 34)
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
    }

    [Fact]
    public void Members_Interface_DisallowedModifiers()
    {
        var src = """
explicit extension R for int
{
    async interface Async { } // 1
    abstract interface Abstract { } // 2
    override interface Override { } // 3
    protected interface Protected { } // 4
    private protected interface PrivateProtected { } // 5
    internal protected interface InternalProtected { } // 6
    virtual interface Virtual { } // 7
    required interface Required { } // 8
    file interface File { } // 9
    readonly interface Readonly { } // 10
    sealed interface Sealed { } // 11
    static interface Static { } // 12
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
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
            // (6,25): error CS0106: The modifier 'protected' is not valid for this item
            //     protected interface Protected { } // 4
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Protected").WithArguments("protected").WithLocation(6, 25),
            // (7,33): error CS0106: The modifier 'private protected' is not valid for this item
            //     private protected interface PrivateProtected { } // 5
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "PrivateProtected").WithArguments("private protected").WithLocation(7, 33),
            // (8,34): error CS0106: The modifier 'protected internal' is not valid for this item
            //     internal protected interface InternalProtected { } // 6
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "InternalProtected").WithArguments("protected internal").WithLocation(8, 34),
            // (9,23): error CS0106: The modifier 'virtual' is not valid for this item
            //     virtual interface Virtual { } // 7
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Virtual").WithArguments("virtual").WithLocation(9, 23),
            // (10,24): error CS0106: The modifier 'required' is not valid for this item
            //     required interface Required { } // 8
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Required").WithArguments("required").WithLocation(10, 24),
            // (11,20): error CS9054: File-local type 'R.File' must be defined in a top level type; 'R.File' is a nested type.
            //     file interface File { } // 9
            Diagnostic(ErrorCode.ERR_FileTypeNested, "File").WithArguments("R.File").WithLocation(11, 20),
            // (12,24): error CS0106: The modifier 'readonly' is not valid for this item
            //     readonly interface Readonly { } // 10
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Readonly").WithArguments("readonly").WithLocation(12, 24),
            // (13,22): error CS0106: The modifier 'sealed' is not valid for this item
            //     sealed interface Sealed { } // 11
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Sealed").WithArguments("sealed").WithLocation(13, 22),
            // (14,22): error CS0106: The modifier 'static' is not valid for this item
            //     static interface Static { } // 12
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Static").WithArguments("static").WithLocation(14, 22)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.True(r.GetTypeMember("Abstract").IsAbstract);
        Assert.False(r.GetTypeMember("Override").IsOverride);
        Assert.Equal(Accessibility.Private, r.GetTypeMember("Protected").DeclaredAccessibility);
        Assert.Equal(Accessibility.Private, r.GetTypeMember("PrivateProtected").DeclaredAccessibility);
        Assert.Equal(Accessibility.Private, r.GetTypeMember("InternalProtected").DeclaredAccessibility);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("UnderlyingStruct", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
        Assert.Empty(r.GetMembers());
        Assert.Null(r.BaseTypeNoUseSiteDiagnostics);
        Assert.False(r.IsStatic);

        Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
        Assert.Null(r.ContainingType);
        Assert.Empty(r.TypeParameters);
        Assert.Empty(r.GetMembers());
    }

    [Fact]
    public void ForTypeParameter()
    {
        var src = """
interface I { }
explicit extension R<T> for T where T : I
{
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R<T>", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("T", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
        Assert.Empty(r.GetMembers());
        Assert.Null(r.BaseTypeNoUseSiteDiagnostics);
        Assert.False(r.IsStatic);

        Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
        Assert.Null(r.ContainingType);
        Assert.Equal(new[] { "T" }, r.TypeParameters.ToTestDisplayStrings());
        Assert.Empty(r.GetMembers());
    }

    [Fact]
    public void ForEnum()
    {
        var src = """
enum E { }
explicit extension R for E
{
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("E", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
        Assert.Empty(r.GetMembers());
        Assert.Null(r.BaseTypeNoUseSiteDiagnostics);
        Assert.False(r.IsStatic);

        Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
        Assert.Null(r.ContainingType);
        Assert.Empty(r.TypeParameters);
        Assert.Empty(r.GetMembers());
    }

    [Fact]
    public void ForObject()
    {
        var src = """
explicit extension R for object
{
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("System.Object", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
        Assert.Empty(r.GetMembers());
        Assert.Null(r.BaseTypeNoUseSiteDiagnostics);
        Assert.False(r.IsStatic);

        Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
        Assert.Null(r.ContainingType);
        Assert.Empty(r.TypeParameters);
        Assert.Empty(r.GetMembers());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R<U>", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("C<U>", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
        Assert.Empty(r.GetMembers());
        Assert.Null(r.BaseTypeNoUseSiteDiagnostics);
        Assert.False(r.IsStatic);

        Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
        Assert.Null(r.ContainingType);
        Assert.Equal(new[] { "U" }, r.TypeParameters.ToTestDisplayStrings());
        Assert.Empty(r.GetMembers());
    }

    [Fact]
    public void ForTuple()
    {
        var src = """
explicit extension R for (int, int)
{
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("(System.Int32, System.Int32)", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
        Assert.Empty(r.GetMembers());
        Assert.Null(r.BaseTypeNoUseSiteDiagnostics);
        Assert.False(r.IsStatic);

        Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
        Assert.Null(r.ContainingType);
        Assert.Empty(r.TypeParameters);
        Assert.Empty(r.GetMembers());
    }

    [Fact]
    public void ForArrayType()
    {
        var src = """
explicit extension R for int[]
{
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("System.Int32[]", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
        Assert.Empty(r.GetMembers());
        Assert.Null(r.BaseTypeNoUseSiteDiagnostics);
        Assert.False(r.IsStatic);

        Assert.Equal(Accessibility.Internal, r.DeclaredAccessibility);
        Assert.Null(r.ContainingType);
        Assert.Empty(r.TypeParameters);
        Assert.Empty(r.GetMembers());
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal(new[] { "T1", "T2" }, r.TypeParameters.ToTestDisplayStrings());
        Assert.True(r.IsGenericType);
        Assert.Equal(2, r.Arity);
    }

    [Fact]
    public void WithTypeParameters_Implicit()
    {
        var src = """
interface I { }
class UnderlyingClass : I { }
implicit extension R<T1, T2> for UnderlyingClass
{
}
""";
        // PROTOTYPE the type parameters of an implicit extension must
        // appear in the underlying type
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal(new[] { "T1", "T2" }, r.TypeParameters.ToTestDisplayStrings());
        Assert.True(r.IsGenericType);
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (3,20): error CS9114: No part of a partial extension 'R' includes an underlying type specification.
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
            // (3,27): error CS1002: ; expected
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(3, 27),
            // (3,27): error CS1022: Type or namespace definition, or end-of-file expected
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_EOFExpected, ")").WithLocation(3, 27),
            // (3,33): error CS1003: Syntax error, '(' expected
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "UnderlyingClass").WithArguments("(").WithLocation(3, 33),
            // (3,33): error CS0119: 'UnderlyingClass' is a type, which is not valid in the given context
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_BadSKunknown, "UnderlyingClass").WithArguments("UnderlyingClass", "type").WithLocation(3, 33),
            // (3,33): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_IllegalStatement, "UnderlyingClass").WithLocation(3, 33),
            // (3,49): error CS1002: ; expected
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(3, 49),
            // (3,49): error CS1525: Invalid expression term '{'
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(3, 49),
            // (3,49): error CS1002: ; expected
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(3, 49),
            // (3,49): error CS1026: ) expected
            // explicit extension R(int i) for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(3, 49)
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
        var comp = CreateCompilation(src);
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.False(r.IsStatic);
        comp.VerifyDiagnostics(
            // (2,26): error CS9106: Instance extension 'R' cannot augment underlying type 'UnderlyingClass' because it is static.
            // explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_StaticBaseTypeOnInstanceExtension, "UnderlyingClass").WithArguments("R", "UnderlyingClass").WithLocation(2, 26)
            );
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
        var comp = CreateCompilation(src);
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R@<tree 0>", r.ToTestDisplayString());
        Assert.Equal("UnderlyingClass@<tree 0>", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,20): error CS9112: File-local type 'UnderlyingClass' cannot be used as a underlying type of non-file-local extension 'R'.
            // explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_FileTypeUnderlying, "R").WithArguments("UnderlyingClass", "R").WithLocation(2, 20)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("UnderlyingClass@<tree 0>", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (5,20): error CS9112: File-local type 'Outer.UnderlyingClass' cannot be used as a underlying type of non-file-local extension 'R'.
            // explicit extension R for Outer.UnderlyingClass
            Diagnostic(ErrorCode.ERR_FileTypeUnderlying, "R").WithArguments("Outer.UnderlyingClass", "R").WithLocation(5, 20)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.Equal("Outer@<tree 0>.UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (4,20): error CS9108: Partial declarations of 'R1' must not specify different underlying types.
            // explicit extension R1 for UnderlyingClass1 { } // 1
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R1").WithArguments("R1").WithLocation(4, 20),
            // (5,20): error CS0101: The namespace '<global namespace>' already contains a definition for 'R1'
            // explicit extension R1 for UnderlyingClass2 { } // 2
            Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "R1").WithArguments("R1", "<global namespace>").WithLocation(5, 20),
            // (7,20): error CS9115: Partial declarations of 'R2' must specify the same extension modifier ('implicit' or 'explicit').
            // explicit extension R2 for UnderlyingClass1 { } // 3, 4
            Diagnostic(ErrorCode.ERR_PartialDifferentExtensionModifiers, "R2").WithArguments("R2").WithLocation(7, 20),
            // (7,20): error CS9108: Partial declarations of 'R2' must not specify different underlying types.
            // explicit extension R2 for UnderlyingClass1 { } // 3, 4
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R2").WithArguments("R2").WithLocation(7, 20),
            // (8,20): error CS0101: The namespace '<global namespace>' already contains a definition for 'R2'
            // implicit extension R2 for UnderlyingClass2 { } // 5
            Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "R2").WithArguments("R2", "<global namespace>").WithLocation(8, 20)
            );

        var r1 = comp.GlobalNamespace.GetTypeMember("R1");
        Assert.Equal("UnderlyingClass1", r1.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        Assert.Equal("UnderlyingClass1", r2.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
partial explicit extension RStruct2 for CStruct<int> { } // 7

partial explicit extension RStruct3<T> for CStruct<T> { } // 8
partial explicit extension RStruct3<T> { }

partial explicit extension RNotNull1 for CNotNull<string> { }

partial explicit extension RNotNull2 for CNotNull<string?> { } // 9
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (9,28): error CS0264: Partial declarations of 'RDefault1<T>' must have the same type parameter names in the same order
            // partial explicit extension RDefault1<T> for CDefault<T> { } // 1, 2
            Diagnostic(ErrorCode.ERR_PartialWrongTypeParams, "RDefault1").WithArguments("RDefault1<T>").WithLocation(9, 28),
            // (9,28): error CS9108: Partial declarations of 'RDefault1<T>' must not specify different underlying types.
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
            // partial explicit extension RStruct3<T> for CStruct<T> { } // 8
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "RStruct3").WithArguments("CStruct<T>", "T", "T").WithLocation(29, 28),
            // (34,28): warning CS8714: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'CNotNull<T>'. Nullability of type argument 'string?' doesn't match 'notnull' constraint.
            // partial explicit extension RNotNull2 for CNotNull<string?> { } // 9
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
        var comp = CreateCompilation(src);
        // PROTOTYPE type parameters of implicit extensions must appear
        // in the underlying type
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void UnderlyingType_MissingTypeSyntax()
    {
        var src = """
explicit extension R for { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (1,26): error CS1031: Type expected
            // explicit extension R for { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "{").WithLocation(1, 26)
            );

        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.True(r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.IsErrorType());
    }

    [Fact]
    public void UnderlyingType_NativeInt()
    {
        var src = """
explicit extension R for nint { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            );
        // PROTOTYPE check that NativeIntegerAttribute is emitted
    }

    [Fact]
    public void UnderlyingType_TypeWithNullability()
    {
        var src = """
#nullable enable
class C<T> { }
explicit extension R for C<object?> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        // PROTOTYPE check that NullableAttribute is emitted
    }

    [Fact]
    public void UnderlyingType_TupleWithElementNames()
    {
        var src = """
explicit extension R for (int a, int b) { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            );
        // PROTOTYPE check that TupleElementNamesAttribute is emitted
    }

    [Fact]
    public void ForPointer()
    {
        var src = """
unsafe explicit extension R for int*
{
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (1,27): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe explicit extension R for int*
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "R").WithLocation(1, 27),
            // (1,33): error CS9105: Extension 'R' cannot augment underlying type 'int*'. The extension underlying type may not be dynamic, a pointer, a nullable reference type, a ref struct or an extension.
            // unsafe explicit extension R for int*
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "int*").WithArguments("R", "int*").WithLocation(1, 33)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Null(r.ExtensionUnderlyingTypeNoUseSiteDiagnostics);
    }

    [Fact]
    public void ForRefType()
    {
        var src = """
explicit extension R for ref int
{
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (1,26): error CS1073: Unexpected token 'ref'
            // explicit extension R for ref int
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(1, 26)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("System.Int32", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void ForPointer_InUnsafeCompilation()
    {
        var src = """
unsafe explicit extension R for int* // 1
{
    int* M(int* i) => i;
}

explicit extension R2 for int* // 2, 3
{
    int* M(int* i) => i; // 4, 5, 6
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (1,33): error CS9105: Extension 'R' cannot augment underlying type 'int*'. The extension underlying type may not be dynamic, a pointer, a nullable reference type, a ref struct or an extension.
            // unsafe explicit extension R for int* // 1
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "int*").WithArguments("R", "int*").WithLocation(1, 33),
            // (6,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // explicit extension R2 for int* // 2, 3
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(6, 27),
            // (6,27): error CS9105: Extension 'R2' cannot augment underlying type 'int*'. The extension underlying type may not be dynamic, a pointer, a nullable reference type, a ref struct or an extension.
            // explicit extension R2 for int* // 2, 3
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "int*").WithArguments("R2", "int*").WithLocation(6, 27),
            // (8,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     int* M(int* i) => i; // 4, 5, 6
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(8, 5),
            // (8,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     int* M(int* i) => i; // 4, 5, 6
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(8, 12),
            // (8,23): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     int* M(int* i) => i; // 4, 5, 6
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "i").WithLocation(8, 23)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Null(r.ExtensionUnderlyingTypeNoUseSiteDiagnostics);
    }

    [Fact]
    public void ForFunctionPointer()
    {
        var src = """
unsafe explicit extension R for delegate*<void>
{
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (1,33): error CS9105: Extension 'R' cannot augment underlying type 'delegate*<void>'. The extension underlying type may not be dynamic, a pointer, a nullable reference type, a ref struct or an extension.
            // unsafe explicit extension R for delegate*<void>
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "delegate*<void>").WithArguments("R", "delegate*<void>").WithLocation(1, 33)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Null(r.ExtensionUnderlyingTypeNoUseSiteDiagnostics);
    }

    [Fact]
    public void ForDynamic()
    {
        var src = """
explicit extension R for dynamic
{
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (1,26): error CS9105: Extension 'R' cannot augment underlying type 'dynamic'. The extension underlying type may not be dynamic, a pointer, a nullable reference type, a ref struct or an extension.
            // explicit extension R for dynamic
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "dynamic").WithArguments("R", "dynamic").WithLocation(1, 26)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Null(r.ExtensionUnderlyingTypeNoUseSiteDiagnostics);
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);
    }

    [Fact]
    public void ForNullableReferenceType()
    {
        var src = """
#nullable enable
class C<T> { }

explicit extension R1 for string { }
explicit extension R2 for string? { } // 1
explicit extension R3 for C<string> { }
explicit extension R4 for C<string?> { }

#nullable disable
explicit extension R5 for string { }
explicit extension R6 for C<string> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (5,27): error CS9105: Extension 'R2' cannot augment underlying type 'string?'. The extension underlying type may not be dynamic, a pointer, a nullable reference type, a ref struct or an extension.
            // explicit extension R2 for string? { } // 1
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "string?").WithArguments("R2", "string?").WithLocation(5, 27)
            );
        NamedTypeSymbol r2 = comp.GlobalNamespace.GetTypeMember("R2");
        Assert.Equal("R2", r2.ToTestDisplayString());
        Assert.True(r2.IsExtension);
        Assert.Null(r2.ExtensionUnderlyingTypeNoUseSiteDiagnostics);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,27): error CS9109: Inconsistent accessibility: underlying type 'UnderlyingStruct' is less accessible than extension 'R'
            // public explicit extension R for UnderlyingStruct
            Diagnostic(ErrorCode.ERR_BadVisUnderlyingType, "R").WithArguments("R", "UnderlyingStruct").WithLocation(2, 27)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal(Accessibility.Public, r.DeclaredAccessibility);
    }

    [Fact]
    public void ForRefLikeType()
    {
        var src = """
ref struct RS {  }
explicit extension R for RS { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,26): error CS9105: Extension 'R' cannot augment underlying type 'RS'. The extension underlying type may not be dynamic, a pointer, a nullable reference type, a ref struct or an extension.
            // explicit extension R for RS { }
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "RS").WithArguments("R", "RS").WithLocation(2, 26)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
    }

    [Fact]
    public void Partial_OnePartWithoutUnderlyingType()
    {
        var src = """
class UnderlyingClass { }
partial explicit extension R for UnderlyingClass { }
partial explicit extension R { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void Partial_OtherPartWithoutUnderlyingType()
    {
        var src = """
class UnderlyingClass { }
partial explicit extension R { }
partial explicit extension R for UnderlyingClass { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (3,28): error CS9108: Partial declarations of 'R' must not specify different underlying types.
            // partial explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(3, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (4,28): error CS9108: Partial declarations of 'R' must not specify different underlying types.
            // partial explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(4, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (3,28): error CS9108: Partial declarations of 'R' must not specify different underlying types.
            // partial explicit extension R for UnderlyingClass { }
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(3, 28),
            // (4,34): error CS0246: The type or namespace name 'ErrorType' could not be found (are you missing a using directive or an assembly reference?)
            // partial explicit extension R for ErrorType { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ErrorType").WithArguments("ErrorType").WithLocation(4, 34)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (3,28): error CS9108: Partial declarations of 'R' must not specify different underlying types.
            // partial explicit extension R for ErrorType { }
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(3, 28),
            // (3,34): error CS0246: The type or namespace name 'ErrorType' could not be found (are you missing a using directive or an assembly reference?)
            // partial explicit extension R for ErrorType { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ErrorType").WithArguments("ErrorType").WithLocation(3, 34)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("ErrorType", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C<System.Object>", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void Partial_PartsWithDifferentUnderlyingTypes_Dynamic()
    {
        var src = """
class C<T> { }
partial explicit extension R for C<object> { }
partial explicit extension R for C<dynamic> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,28): error CS9108: Partial declarations of 'R' must not specify different underlying types.
            // partial explicit extension R for C<object> { }
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(2, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C<>", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.True(r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.IsErrorType());
    }

    [Fact]
    public void Partial_PartsWithDifferentUnderlyingTypes_TupleNames()
    {
        var src = """
class C<T> { }
partial explicit extension R for C<(int x, int b)> { }
partial explicit extension R for C<(int y, int b)> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,28): error CS9108: Partial declarations of 'R' must not specify different underlying types.
            // partial explicit extension R for C<(int x, int b)> { }
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(2, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C<>", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.True(r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.IsErrorType());
    }

    [Fact]
    public void Partial_PartsWithDifferentUnderlyingTypes_Nullability()
    {
        var src = """
#nullable enable

class C<T> { }
partial explicit extension R for C<object> { }
partial explicit extension R for C<object?> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (4,28): error CS9108: Partial declarations of 'R' must not specify different underlying types.
            // partial explicit extension R for C<object> { }
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(4, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C<>", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.True(r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.IsErrorType());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C<System.Object?>", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C<System.Object?>", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (5,28): error CS9108: Partial declarations of 'R' must not specify different underlying types.
            // partial explicit extension R for C<
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(5, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C<, >", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.True(r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.IsErrorType());
    }

    [Fact]
    public void Partial_OnePartIsErrorType()
    {
        var src = """
class UnderlyingClass { }
partial explicit extension R for Error { }
partial explicit extension R for UnderlyingClass { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,28): error CS9108: Partial declarations of 'R' must not specify different underlying types.
            // partial explicit extension R for Error { }
            Diagnostic(ErrorCode.ERR_PartialMultipleUnderlyingTypes, "R").WithArguments("R").WithLocation(2, 28),
            // (2,34): error CS0246: The type or namespace name 'Error' could not be found (are you missing a using directive or an assembly reference?)
            // partial explicit extension R for Error { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error").WithArguments("Error").WithLocation(2, 34)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("Error", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void Partial_OtherPartIsErrorType()
    {
        var src = """
class UnderlyingClass { }
partial explicit extension R for UnderlyingClass { }
partial explicit extension R for Error { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (3,34): error CS0246: The type or namespace name 'Error' could not be found (are you missing a using directive or an assembly reference?)
            // partial explicit extension R for Error { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error").WithArguments("Error").WithLocation(3, 34)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Theory, CombinatorialData]
    public void Partial_NoPartHasUnderlyingType(bool isImplicit)
    {
        var keyword = isImplicit ? "implicit" : "explicit";
        var src = $$"""
partial {{keyword}} extension R { }
partial {{keyword}} extension R { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (1,28): error CS9114: No part of a partial extension 'R' includes an underlying type specification.
            // partial explicit extension R { }
            Diagnostic(ErrorCode.ERR_ExtensionMissingUnderlyingType, "R").WithArguments("R").WithLocation(1, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Null(r.ExtensionUnderlyingTypeNoUseSiteDiagnostics);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var r3 = comp.GlobalNamespace.GetTypeMember("R3");
        Assert.Equal(new[] { "R1", "R2" }, r3.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.True(r3.IsPartial());

        var r4 = comp.GlobalNamespace.GetTypeMember("R4");
        Assert.Equal(new[] { "R1", "R2" }, r4.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.True(r4.IsPartial());

        var r5 = comp.GlobalNamespace.GetTypeMember("R5");
        Assert.Equal(new[] { "R1", "R2" }, r5.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.True(r5.IsPartial());

        var r6 = comp.GlobalNamespace.GetTypeMember("R6");
        Assert.Equal(new[] { "R1", "R2" }, r6.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (3,19): error CS0261: Partial declarations of 'R' must be all classes, all record classes, all structs, all record structs, all interfaces, or all extensions.
            // partial interface R { }
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,35): error CS0262: Partial declarations of 'R1' have conflicting accessibility modifiers
            // public partial explicit extension R1 for C { }
            Diagnostic(ErrorCode.ERR_PartialModifierConflict, "R1").WithArguments("R1").WithLocation(2, 35),
            // (2,35): error CS9109: Inconsistent accessibility: underlying type 'C' is less accessible than extension 'R1'
            // public partial explicit extension R1 for C { }
            Diagnostic(ErrorCode.ERR_BadVisUnderlyingType, "R1").WithArguments("R1", "C").WithLocation(2, 35)
            );
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var r = comp.GlobalNamespace.GetTypeMember("R");
        AssertEx.Equal(new[] { "void R.M1()", "void R.M2()" },
            r.GetMembers().ToTestDisplayStrings());
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

#nullable enable
partial explicit extension R3<T> for C where T : class { }
#nullable disable
partial explicit extension R3<T> for C where T : class { }
#nullable enable

#nullable disable
partial explicit extension R4<T> for C where T : class { }
partial explicit extension R4<T> for C where T : class { }

#nullable enable
explicit extension R5 for C : R1<string?>, R2<string?> , R3<string?>, R4<string?> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (23,20): warning CS8634: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'R1<T>'. Nullability of type argument 'string?' doesn't match 'class' constraint.
            // explicit extension R5 for C : R1<string?>, R2<string?> , R3<string?>, R4<string?> { }
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "R5").WithArguments("R1<T>", "T", "string?").WithLocation(23, 20),
            // (23,20): warning CS8634: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'R2<T>'. Nullability of type argument 'string?' doesn't match 'class' constraint.
            // explicit extension R5 for C : R1<string?>, R2<string?> , R3<string?>, R4<string?> { }
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "R5").WithArguments("R2<T>", "T", "string?").WithLocation(23, 20),
            // (23,20): warning CS8634: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'R3<T>'. Nullability of type argument 'string?' doesn't match 'class' constraint.
            // explicit extension R5 for C : R1<string?>, R2<string?> , R3<string?>, R4<string?> { }
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "R5").WithArguments("R3<T>", "T", "string?").WithLocation(23, 20)
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (1,26): error CS0246: The type or namespace name 'error' could not be found (are you missing a using directive or an assembly reference?)
            // explicit extension R for error
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "error").WithArguments("error").WithLocation(1, 26)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        var underlyingType = r.ExtensionUnderlyingTypeNoUseSiteDiagnostics;
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,28): error CS0246: The type or namespace name 'error' could not be found (are you missing a using directive or an assembly reference?)
            // explicit extension R for C<error>
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "error").WithArguments("error").WithLocation(2, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        var underlyingType = r.ExtensionUnderlyingTypeNoUseSiteDiagnostics;
        Assert.Equal("C<error>", underlyingType.ToTestDisplayString());
    }

    [Fact]
    public void ForTypeWithUseSiteError()
    {
        var lib1_cs = "public class MissingBase { }";
        var comp1 = CreateCompilation(lib1_cs, assemblyName: "missing");
        comp1.VerifyDiagnostics();

        var lib2_cs = "public class UseSiteError : MissingBase { }";
        var comp2 = CreateCompilation(lib2_cs, new[] { comp1.EmitToImageReference() });
        comp2.VerifyDiagnostics();

        var src = """
class C<T> { }
explicit extension R1 for UseSiteError { }
explicit extension R2 for C<UseSiteError> { }
class C1 : UseSiteError { }
class C2 : C<UseSiteError> { }
""";
        var comp = CreateCompilation(src, new[] { comp2.EmitToImageReference() });
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (1,26): error CS9105: Extension 'R' cannot augment underlying type 'R'. The extension underlying type may not be dynamic, a pointer, a nullable reference type, a ref struct or an extension.
            // explicit extension R for R { }
            Diagnostic(ErrorCode.ERR_BadExtensionUnderlyingType, "R").WithArguments("R", "R").WithLocation(1, 26)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Null(r.ExtensionUnderlyingTypeNoUseSiteDiagnostics);
    }

    [Fact]
    public void TypeDepends_SelfReference_WithArray()
    {
        var src = """
explicit extension R for R[] { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R[]", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void TypeDepends_SelfReference_WithTuple()
    {
        var src = """
explicit extension R for (R, R) { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("(R, R)", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void TypeDepends_CircularityViaTypeArgument()
    {
        var src = """
class C<T> { }
explicit extension R for C<R> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C<R>", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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

        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,20): error CS9111: Base extension 'Y' causes a cycle in the extension hierarchy of 'X'.
            // explicit extension X for S : Y { }
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "X").WithArguments("X", "Y").WithLocation(2, 20),
            // (3,20): error CS9111: Base extension 'Z' causes a cycle in the extension hierarchy of 'Y'.
            // explicit extension Y for S : Z { }
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "Y").WithArguments("Y", "Z").WithLocation(3, 20),
            // (4,20): error CS9111: Base extension 'X' causes a cycle in the extension hierarchy of 'Z'.
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
        }
        // PROTOTYPE should also test circularity via metadata
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (1,20): error CS0146: Circular base type dependency involving 'R2.Nested' and 'R'
            // explicit extension R for R2.Nested { }
            Diagnostic(ErrorCode.ERR_CircularBase, "R").WithArguments("R2.Nested", "R").WithLocation(1, 20),
            // (2,20): error CS9111: Base extension 'R' causes a cycle in the extension hierarchy of 'R2'.
            // explicit extension R2 for object : R
            Diagnostic(ErrorCode.ERR_CycleInBaseExtensions, "R2").WithArguments("R2", "R").WithLocation(2, 20)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.True(r.IsExtension);
        Assert.Equal("R2.Nested", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        Assert.True(r2.IsExtension);
        Assert.Equal("System.Object", r2.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(new[] { "R" }, r2.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        Assert.True(r2.BaseExtensionsNoUseSiteDiagnostics.Single().IsErrorType());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.True(r.IsExtension);
        Assert.Equal("R2.Nested[]", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        Assert.True(r2.IsExtension);
        Assert.Equal("R2.Nested[]", r2.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(new[] { "R" }, r2.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("(R2.Nested, System.Int32)", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Empty(r.BaseExtensionsNoUseSiteDiagnostics);

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        Assert.Equal("(R2.Nested, System.Int32)", r2.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(new[] { "R" }, r2.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
    }

    [Fact]
    public void ImplicitVsExplicit()
    {
        var src = """
struct S { }
explicit extension X for S { }
implicit extension X for S { }
""";

        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,20): error CS9115: Partial declarations of 'X' must specify the same extension modifier ('implicit' or 'explicit').
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,28): error CS9115: Partial declarations of 'X' must specify the same extension modifier ('implicit' or 'explicit').
            // partial explicit extension X for S { }
            Diagnostic(ErrorCode.ERR_PartialDifferentExtensionModifiers, "X").WithArguments("X").WithLocation(2, 28)
            );
    }

    [Fact]
    public void ImplicitVsExplicit_PartialImplicitAndExplicit()
    {
        var src = """
struct S { }
partial implicit extension X for S { }
partial explicit extension X for S { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,28): error CS9115: Partial declarations of 'X' must specify the same extension modifier ('implicit' or 'explicit').
            // partial implicit extension X for S { }
            Diagnostic(ErrorCode.ERR_PartialDifferentExtensionModifiers, "X").WithArguments("X").WithLocation(2, 28)
            );
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net60);
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
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net60);
        comp.VerifyDiagnostics(
            // (3,27): error CS0527: Type 'Extension' in interface list is not an interface
            // record struct R2(int j) : Extension { } // 1
            Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "Extension").WithArguments("Extension").WithLocation(3, 27)
            );
    }

    [Fact]
    public void BaseExtensions()
    {
        var src = """
class C { }
explicit extension R1 for C { }
explicit extension R2 for C { }
explicit extension R3 for C : R1, R2 { }

partial explicit extension R4 for C : R1 { }
partial explicit extension R4 for C : R2 { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r3 = comp.GlobalNamespace.GetTypeMember("R3");
        Assert.Equal("C", r3.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(new[] { "R1", "R2" }, r3.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());

        var r4 = comp.GlobalNamespace.GetTypeMember("R4");
        Assert.Equal("C", r4.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(new[] { "R1", "R2" }, r4.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
        // PROTOTYPE verify in compilation reference and metadata
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,30): error CS0246: The type or namespace name 'error' could not be found (are you missing a using directive or an assembly reference?)
            // explicit extension R for C : error
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "error").WithArguments("error").WithLocation(2, 30)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.True(r.IsExtension);
        Assert.Equal("C", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (5,43): error CS9110: Inconsistent accessibility: base extension 'C.R1' is less accessible than extension 'C.R2'
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (3,20): error CS9053: File-local type 'R1' cannot be used as a base type of non-file-local type 'R'.
            // explicit extension R for UnderlyingClass : R1 { }
            Diagnostic(ErrorCode.ERR_FileTypeBase, "R").WithArguments("R1", "R").WithLocation(3, 20)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(new[] { "R1@<tree 0>" }, r.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (4,20): error CS9053: File-local type 'R2' cannot be used as a base type of non-file-local type 'R'.
            // explicit extension R for UnderlyingClass : R1, R2 { }
            Diagnostic(ErrorCode.ERR_FileTypeBase, "R").WithArguments("R2", "R").WithLocation(4, 20)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R", r.ToTestDisplayString());
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (4,20): error CS9053: File-local type 'R1' cannot be used as a base type of non-file-local type 'R'.
            // explicit extension R for UnderlyingClass : R1, R2 { }
            Diagnostic(ErrorCode.ERR_FileTypeBase, "R").WithArguments("R1", "R").WithLocation(4, 20),
            // (4,20): error CS9053: File-local type 'R2' cannot be used as a base type of non-file-local type 'R'.
            // explicit extension R for UnderlyingClass : R1, R2 { }
            Diagnostic(ErrorCode.ERR_FileTypeBase, "R").WithArguments("R2", "R").WithLocation(4, 20)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal(new[] { "R1@<tree 0>", "R2@<tree 0>" }, r.BaseExtensionsNoUseSiteDiagnostics.ToTestDisplayStrings());
    }

    [Theory, CombinatorialData]
    public void BaseExtension_ImplicitVsExplicit(bool baseIsExplicit, bool thisIsExplicit)
    {
        // PROTOTYPE should there be a restriction on implicit/explicit relative
        // to base extensions?
        var src = $$"""
class C { }
{{(baseIsExplicit ? "explicit" : "implicit")}} extension R1 for C { }
{{(thisIsExplicit ? "explicit" : "implicit")}} extension R for C : R1 { }
""";
        var comp = CreateCompilation(src);
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

unsafe explicit extension R7 for C : C* { } // 6
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (5,31): error CS9107: 'I' is not an un-annotated extension type.
            // explicit extension R1 for C : I { } // 1
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "I").WithArguments("I").WithLocation(5, 31),
            // (6,31): error CS9107: 'C' is not an un-annotated extension type.
            // explicit extension R2 for C : C { } // 2
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "C").WithArguments("C").WithLocation(6, 31),
            // (7,31): error CS9107: 'S' is not an un-annotated extension type.
            // explicit extension R3 for C : S { } // 3
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "S").WithArguments("S").WithLocation(7, 31),
            // (8,31): error CS9107: 'E' is not an un-annotated extension type.
            // explicit extension R4 for C : E { } // 4
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "E").WithArguments("E").WithLocation(8, 31),
            // (12,31): error CS9107: 'R5?' is not an un-annotated extension type.
            // explicit extension R6 for C : R5? { } // 5
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "R5?").WithArguments("R5?").WithLocation(12, 31),
            // (14,38): error CS9107: 'C*' is not an un-annotated extension type.
            // unsafe explicit extension R7 for C : C* { } // 6
            Diagnostic(ErrorCode.ERR_OnlyBaseExtensionAllowed, "C*").WithArguments("C*").WithLocation(14, 38)
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
    }

    [Fact]
    public void BaseExtension_Pointer()
    {
        var src = """
class C { }
explicit extension D<T> for C { }
unsafe explicit extension R1 for C : D<int*> { } // 1
explicit extension R2 for C : D<int*> { } // 2
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (3,27): error CS0306: The type 'int*' may not be used as a type argument
            // unsafe explicit extension R1 for C : D<int*> { } // 1
            Diagnostic(ErrorCode.ERR_BadTypeArgument, "R1").WithArguments("int*").WithLocation(3, 27),
            // (4,20): error CS0306: The type 'int*' may not be used as a type argument
            // explicit extension R2 for C : D<int*> { } // 2
            Diagnostic(ErrorCode.ERR_BadTypeArgument, "R2").WithArguments("int*").WithLocation(4, 20)
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (4,35): error CS9120: 'R0' is already listed in the base extension list
            // explicit extension R1 for C : R0, R0 { } // 1
            Diagnostic(ErrorCode.ERR_DuplicateExtensionInBaseList, "R0").WithArguments("R0").WithLocation(4, 35),
            // (7,43): error CS9120: 'R0' is already listed in the base extension list
            // partial explicit extension R2 for C : R0, R0 { } // 2
            Diagnostic(ErrorCode.ERR_DuplicateExtensionInBaseList, "R0").WithArguments("R0").WithLocation(7, 43),
            // (34,5): warning CS9117: 'R3<object?>' is already listed in the base extension list on type 'R5' with different nullability of reference types.
            //     R3<object?> // 3
            Diagnostic(ErrorCode.WRN_DuplicateExtensionWithNullabilityMismatchInBaseList, "R3<object?>").WithArguments("R3<object?>", "R5").WithLocation(34, 5),
            // (38,20): warning CS9117: 'R3<object?>' is already listed in the base extension list on type 'R6' with different nullability of reference types.
            // explicit extension R6 for C : R3<object>, R3<object?> { } // 4
            Diagnostic(ErrorCode.WRN_DuplicateExtensionWithNullabilityMismatchInBaseList, "R6").WithArguments("R3<object?>", "R6").WithLocation(38, 20),
            // (40,20): error CS9119: 'R3<dynamic>' is already listed in the base extension list on type 'R7' as 'R3<object>'.
            // explicit extension R7 for C : R3<object>, R3<dynamic> { } // 5
            Diagnostic(ErrorCode.ERR_DuplicateExtensionWithDifferencesInBaseList, "R7").WithArguments("R3<dynamic>", "R3<object>", "R7").WithLocation(40, 20),
            // (42,20): error CS9118: 'R3<(int, int)>' is already listed in the base extension list on type 'R8' with different tuple element names, as 'R3<(int i, int j)>'.
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
explicit extension R5b for C : R5a, R3<object?> { } // 1

#nullable enable
explicit extension R6a for C : R3<object> { }
explicit extension R6b for C : R6a, R3<object?> { } // 2

explicit extension R7a for C : R3<object> { }
explicit extension R7b for C : R7a, R3<dynamic> { } // 3

explicit extension R8a for C : R3<(int i, int j)> { }
explicit extension R8b for C : R8a, R3<(int, int)> { } // 4
""";
        // PROTOTYPE Missing diagnostics for duplicates from the bases' bases.
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void BaseExtension_CouldUnify()
    {
        var src = """
class C { }
explicit extension R1<T> for C { }
explicit extension R2<T1, T2> for C : R1<T1>, R1<T2> { }

interface I1<T> { }
interface I2<T1, T2> : I1<T1>, I1<T2> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (6,11): error CS0695: 'I2<T1, T2>' cannot implement both 'I1<T1>' and 'I1<T2>' because they may unify for some type parameter substitutions
            // interface I2<T1, T2> : I1<T1>, I1<T2> { }
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,20): error CS9116: Extension 'R2' has underlying type 'long' but a base extension has underlying type 'int'.
            // explicit extension R2 for long : R1 { } // 1
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R2").WithArguments("R2", "long", "int").WithLocation(2, 20),
            // (6,20): error CS9116: Extension 'R4' has underlying type 'C<dynamic>' but a base extension has underlying type 'C<object>'.
            // explicit extension R4 for C<dynamic> : R3 { } // 2
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R4").WithArguments("R4", "C<dynamic>", "C<object>").WithLocation(6, 20),
            // (9,20): error CS9116: Extension 'R6' has underlying type '(int, int)' but a base extension has underlying type '(int i, int j)'.
            // explicit extension R6 for (int, int) : R5 { } // 3
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R6").WithArguments("R6", "(int, int)", "(int i, int j)").WithLocation(9, 20),
            // (19,20): error CS9116: Extension 'R10' has underlying type 'C<string>' but a base extension has underlying type 'C<string>'.
            // explicit extension R10 for C<string> : R9 { } // 4
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R10").WithArguments("R10", "C<string>", "C<string>").WithLocation(19, 20),
            // (21,20): error CS9116: Extension 'R12' has underlying type 'C<string>' but a base extension has underlying type 'C<string>'.
            // explicit extension R12 for C<string> : R11 { } // 5
            Diagnostic(ErrorCode.ERR_UnderlyingTypesMismatch, "R12").WithArguments("R12", "C<string>", "C<string>").WithLocation(21, 20)
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();

        var r2 = comp.GlobalNamespace.GetTypeMember("R2");
        Assert.Equal("U", r2.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal("U", r2.BaseExtensionsNoUseSiteDiagnostics.Single().ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());

        var r4 = comp.GlobalNamespace.GetTypeMember("R4");
        Assert.Equal("C<U>", r4.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
        Assert.Equal("C<U>", r4.BaseExtensionsNoUseSiteDiagnostics.Single().ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
    }

    [Fact]
    public void BaseExtension_StaticType_InstanceExtension()
    {
        var src = """
class UnderlyingClass { }
static explicit extension StaticExtension for UnderlyingClass { }
explicit extension R for UnderlyingClass : StaticExtension { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (3,44): error CS9106: Instance extension 'R' cannot augment underlying type 'StaticExtension' because it is static.
            // explicit extension R for UnderlyingClass : StaticExtension { }
            Diagnostic(ErrorCode.ERR_StaticBaseTypeOnInstanceExtension, "StaticExtension").WithArguments("R", "StaticExtension").WithLocation(3, 44)
            );

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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (3,20): error CS0260: Missing partial modifier on declaration of type 'R'; another partial declaration of this type exists
            // explicit extension R for C { }
            Diagnostic(ErrorCode.ERR_MissingPartial, "R").WithArguments("R").WithLocation(3, 20)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("C", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,27): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "R").WithLocation(2, 27)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("DerivedExtension").GetTypeMember("R");
        Assert.Equal("DerivedExtension.R", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("UnderlyingClass2", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,30): error CS1527: Elements defined in a namespace cannot be explicitly declared as private, protected, protected internal, or private protected
            // protected explicit extension R for UnderlyingStruct
            Diagnostic(ErrorCode.ERR_NoNamespacePrivate, "R").WithLocation(2, 30),
            // (2,30): error CS9109: Inconsistent accessibility: underlying type 'UnderlyingStruct' is less accessible than extension 'R'
            // protected explicit extension R for UnderlyingStruct
            Diagnostic(ErrorCode.ERR_BadVisUnderlyingType, "R").WithArguments("R", "UnderlyingStruct").WithLocation(2, 30)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("C").GetTypeMember("R");
        Assert.Equal("C.R", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,39): error CS1527: Elements defined in a namespace cannot be explicitly declared as private, protected, protected internal, or private protected
            // protected internal explicit extension R for UnderlyingStruct
            Diagnostic(ErrorCode.ERR_NoNamespacePrivate, "R").WithLocation(2, 39),
            // (2,39): error CS9109: Inconsistent accessibility: underlying type 'UnderlyingStruct' is less accessible than extension 'R'
            // protected internal explicit extension R for UnderlyingStruct
            Diagnostic(ErrorCode.ERR_BadVisUnderlyingType, "R").WithArguments("R", "UnderlyingStruct").WithLocation(2, 39)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("C").GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,28): error CS1527: Elements defined in a namespace cannot be explicitly declared as private, protected, protected internal, or private protected
            // private explicit extension R for UnderlyingStruct
            Diagnostic(ErrorCode.ERR_NoNamespacePrivate, "R").WithLocation(2, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("C").GetTypeMember("R");
        Assert.Equal("C.R", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R@<tree 0>", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,34): error CS9052: File-local type 'R' cannot use accessibility modifiers.
            // file internal explicit extension R for UnderlyingStruct
            Diagnostic(ErrorCode.ERR_FileTypeNoExplicitAccessibility, "R").WithArguments("R").WithLocation(2, 34)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        Assert.Equal("R@<tree 0>", r.ToTestDisplayString());
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
        var comp = CreateCompilation(src);
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
    public void Modifiers_Duplicate()
    {
        var src = """
class UnderlyingClass { }
internal internal explicit extension R for UnderlyingClass
{
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,10): error CS1004: Duplicate 'internal' modifier
            // internal internal explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_DuplicateModifier, "internal").WithArguments("internal").WithLocation(2, 10)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,36): error CS0107: More than one protection modifier
            // public internal explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberProtection, "R").WithLocation(2, 36),
            // (2,36): error CS9109: Inconsistent accessibility: underlying type 'UnderlyingClass' is less accessible than extension 'R'
            // public internal explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadVisUnderlyingType, "R").WithArguments("R", "UnderlyingClass").WithLocation(2, 36)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,36): error CS0107: More than one protection modifier
            // internal public explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberProtection, "R").WithLocation(2, 36),
            // (2,36): error CS9109: Inconsistent accessibility: underlying type 'UnderlyingClass' is less accessible than extension 'R'
            // internal public explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadVisUnderlyingType, "R").WithArguments("R", "UnderlyingClass").WithLocation(2, 36)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,29): error CS0106: The modifier 'abstract' is not valid for this item
            // abstract explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("abstract").WithLocation(2, 29)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
        Assert.Equal("UnderlyingClass", r.ExtensionUnderlyingTypeNoUseSiteDiagnostics.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,29): error CS0106: The modifier 'readonly' is not valid for this item
            // readonly explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("readonly").WithLocation(2, 29)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,29): error CS0106: The modifier 'volatile' is not valid for this item
            // volatile explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("volatile").WithLocation(2, 29)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,27): error CS0106: The modifier 'extern' is not valid for this item
            // extern explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("extern").WithLocation(2, 27)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
    }

    [Fact]
    public void Modifiers_Fixed()
    {
        var src = """
class UnderlyingClass { }
fixed explicit extension R for UnderlyingClass { }
""";
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,28): error CS0106: The modifier 'virtual' is not valid for this item
            // virtual explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("virtual").WithLocation(2, 28)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,29): error CS0106: The modifier 'override' is not valid for this item
            // override explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("override").WithLocation(2, 29)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,26): error CS0106: The modifier 'async' is not valid for this item
            // async explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("async").WithLocation(2, 26)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
        var comp = CreateCompilation(src);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,29): error CS0106: The modifier 'required' is not valid for this item
            // required explicit extension R for UnderlyingClass
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("required").WithLocation(2, 29)
            );
        var r = comp.GlobalNamespace.GetTypeMember("R");
        VerifyExtension<SourceNamedTypeSymbol>(r);
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
    public class Exception { }
    public class ValueType { }
    public explicit extension IntPtr for object { }
}
class C
{
    nint M() => throw null;
}
""";
        var comp = CreateEmptyCompilation(src);
        comp.VerifyDiagnostics();
        var m = comp.GetMember<MethodSymbol>("C.M");
        VerifyNotExtension<NativeIntegerTypeSymbol>(m.ReturnType);
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (1,14): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe class C
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "C").WithLocation(1, 14)
            );
        var m = comp.GetMember<MethodSymbol>("C.M");
        VerifyNotExtension<PointerTypeSymbol>(m.ReturnType);
    }

    [Fact]
    public void IsExtension_SubstitutedNamedTypeSymbol()
    {
        var src = $$"""
explicit extension E1<T> for int { }
explicit extension E2 for int : E1<object> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        var e2 = comp.GlobalNamespace.GetTypeMembers("E2").Single();
        var substE1 = e2.BaseExtensionsNoUseSiteDiagnostics.Single();
        Assert.Equal("E1<object>", substE1.ToDisplayString());
        VerifyExtension<SubstitutedNamedTypeSymbol>(substE1);

        Assert.False(substE1.IsDefinition);
        Assert.Equal("E1<T>", substE1.OriginalDefinition.ToTestDisplayString());
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
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (2,22): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
            // class C<T> where T : R
            Diagnostic(ErrorCode.ERR_BadConstraintType, "R").WithLocation(2, 22)
            );
        var m = comp.GetMember<MethodSymbol>("C.M");
        VerifyNotExtension<TypeParameterSymbol>(m.ReturnType);
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
        var comp = CreateCompilation(src);
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
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (3,20): warning CS8860: Types and aliases should not be named 'record'.
            // explicit extension record for C { }
            Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(3, 20)
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
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
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
        var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("R"));
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
        var compilation = CreateCompilation(source);
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
        var comp = CreateCompilation(text);
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
        var comp = CreateCompilation(text, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (5,34): error CS0306: The type 'int*' may not be used as a type argument
            // public unsafe explicit extension R2<T> for C : R1<int*> { }
            Diagnostic(ErrorCode.ERR_BadTypeArgument, "R2").WithArguments("int*").WithLocation(5, 34)
            );
    }
}
