// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.InitOnly)]
    public class InitOnlyMemberTests : CompilingTestBase
    {
        // Spec: https://github.com/jaredpar/csharplang/blob/init/proposals/init.md

        // PROTOTYPE(init-only): test allowed from 'with' expression
        // PROTOTYPE(init-only): public API, confirm behavior of IsReadOnly and IsInitOnly

        // PROTOTYPE(init-only): open issues:
        // PROTOTYPE(init-only): queue discussion on init methods (`init void Init()`) and collection initializers (`init void Add()`)

        // PROTOTYPE(init-only): test dynamic scenario
        // PROTOTYPE(init-only): test whether reflection use property despite modreq?
        // PROTOTYPE(init-only): test behavior of old compiler with modreq. For example VB
        // PROTOTYPE(init-only): test with ambiguous IsInitOnly types

        [Fact]
        public void TestCSharp8()
        {
            string source = @"
public class C
{
    public string Property { get; init; }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,35): error CS8652: The feature 'init-only setters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public string Property { get; init; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "init").WithArguments("init-only setters").WithLocation(4, 35)
                );
        }

        [Fact]
        public void TestInitNotModifier()
        {
            string source = @"
public class C
{
    public string Property { get; init set; }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,40): error CS8180: { or ; or => expected
                //     public string Property { get; init set; }
                Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "set").WithLocation(4, 40),
                // (4,40): error CS1007: Property accessor already defined
                //     public string Property { get; init set; }
                Diagnostic(ErrorCode.ERR_DuplicateAccessor, "set").WithLocation(4, 40)
                );
        }

        [Fact]
        public void TestWithDuplicateAccessor()
        {
            string source = @"
public class C
{
    public string Property { set => throw null; init => throw null; }
    public string Property2 { init => throw null; set => throw null; }
    public string Property3 { init => throw null; init => throw null; }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,49): error CS1007: Property accessor already defined
                //     public string Property { set => throw null; init => throw null; }
                Diagnostic(ErrorCode.ERR_DuplicateAccessor, "init").WithLocation(4, 49),
                // (5,51): error CS1007: Property accessor already defined
                //     public string Property2 { init => throw null; set => throw null; }
                Diagnostic(ErrorCode.ERR_DuplicateAccessor, "set").WithLocation(5, 51),
                // (6,51): error CS1007: Property accessor already defined
                //     public string Property3 { init => throw null; init => throw null; }
                Diagnostic(ErrorCode.ERR_DuplicateAccessor, "init").WithLocation(6, 51)
                );
        }

        [Fact]
        public void OverrideScenarioWithSubstitutions()
        {
            string source = @"
public class C<T>
{
    public string Property { get; init; }
}
public class Derived : C<string>
{
    void M()
    {
        Property = null; // 1
    }

    Derived()
    {
        Property = null;
    }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (10,9): error CS8802: Init-only member 'C<string>.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         Property = null; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C<string>.Property").WithLocation(10, 9)
                );
        }

        [Fact]
        public void ImplementationScenarioWithSubstitutions()
        {
            string source = @"
public interface I<T>
{
    public string Property { get; init; }
}
public class CWithInit : I<string>
{
    public string Property { get; init; }
}
public class CWithoutInit : I<string> // 1
{
    public string Property { get; set; }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (10,29): error CS8804: 'CWithoutInit' does not implement interface member 'I<string>.Property.set'. 'CWithoutInit.Property.set' cannot implement 'I<string>.Property.set' because it does not match by init-only.
                // public class CWithoutInit : I<string> // 1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I<string>").WithArguments("CWithoutInit", "I<string>.Property.set", "CWithoutInit.Property.set").WithLocation(10, 29)
                );
        }

        [Fact]
        public void InLambdaOrLocalFunction()
        {
            string source = @"
public class C<T>
{
    public string Property { get; init; }
}
public class Derived : C<string>
{
    void M()
    {
        System.Action a = () =>
        {
            Property = null; // 1
        };

        local();
        void local()
        {
            Property = null; // 2
        }
    }

    Derived()
    {
        System.Action a = () =>
        {
            Property = null; // 3
        };

        local();
        void local()
        {
            Property = null; // 4
        }
    }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (12,13): error CS8802: Init-only member 'C<string>.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             Property = null; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C<string>.Property").WithLocation(12, 13),
                // (18,13): error CS8802: Init-only member 'C<string>.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             Property = null; // 2
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C<string>.Property").WithLocation(18, 13),
                // (26,13): error CS8802: Init-only member 'C<string>.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             Property = null; // 3
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C<string>.Property").WithLocation(26, 13),
                // (32,13): error CS8802: Init-only member 'C<string>.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             Property = null; // 4
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C<string>.Property").WithLocation(32, 13)
                );
        }

        [Fact]
        public void MissingIsInitOnlyType_Property()
        {
            string source = @"
public class C
{
    public string Property { get => throw null; init { } }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,49): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsInitOnly' is not defined or imported
                //     public string Property { get => throw null; init { } }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "init").WithArguments("System.Runtime.CompilerServices.IsInitOnly").WithLocation(4, 49)
                );
        }

        [Fact]
        public void InitOnlyPropertyAssignmentDisallowed()
        {
            string source = @"
public class C
{
    public string Property { get; init; }

    void M()
    {
        Property = null; // 1
        _ = new C() { Property = null };
    }

    public C()
    {
        Property = null;
    }

    public string InitOnlyProperty
    {
        get
        {
            Property = null; // 2
            return null;
        }
        init
        {
            Property = null;
        }
    }

    public string RegularProperty
    {
        get
        {
            Property = null; // 3
            return null;
        }
        set
        {
            Property = null; // 4
        }
    }

    public string otherField = (Property = null); // 5
}

class Derived : C
{
}

class Derived2 : Derived
{
    void M()
    {
        Property = null; // 6
    }

    Derived2()
    {
        Property = null;
    }

    public string InitOnlyProperty2
    {
        get
        {
            Property = null; // 7
            return null;
        }
        init
        {
            Property = null;
        }
    }

    public string RegularProperty2
    {
        get
        {
            Property = null; // 8
            return null;
        }
        set
        {
            Property = null; // 9
        }
    }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,9): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         Property = null; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(8, 9),
                // (21,13): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             Property = null; // 2
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(21, 13),
                // (34,13): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             Property = null; // 3
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(34, 13),
                // (39,13): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             Property = null; // 4
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(39, 13),
                // (43,33): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C.Property'
                //     public string otherField = (Property = null); // 5
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "Property").WithArguments("C.Property").WithLocation(43, 33),
                // (54,9): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         Property = null; // 6
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(54, 9),
                // (66,13): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             Property = null; // 7
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(66, 13),
                // (79,13): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             Property = null; // 8
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(79, 13),
                // (84,13): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             Property = null; // 9
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(84, 13)
                );
        }

        [Fact]
        public void DisallowedOnStaticMembers()
        {
            string source = @"
public class C
{
    public static string Property { get; init; }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,42): error CS8806: The 'init' accessor is not valid on static members
                //     public static string Property { get; init; }
                Diagnostic(ErrorCode.ERR_BadInitAccessor, "init").WithLocation(4, 42)
                );
        }

        [Fact]
        public void DisallowedOnOtherInstances()
        {
            string source = @"
public class C
{
    public string Property { get; init; }
    public C c;

    public C()
    {
        c.Property = null; // 1
    }

    public string InitOnlyProperty
    {
        init
        {
            c.Property = null; // 2
        }
    }
}
public class Derived : C
{
    Derived()
    {
        c.Property = null; // 3
    }

    public string InitOnlyProperty2
    {
        init
        {
            c.Property = null; // 4
        }
    }
}
public class Caller
{
    void M(C c)
    {
        _ = new C() {
            Property =
                (c.Property = null)  // 5
        };
    }
}
";

            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (9,9): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         c.Property = null; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(9, 9),
                // (16,13): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             c.Property = null; // 2
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(16, 13),
                // (24,9): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         c.Property = null; // 3
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(24, 9),
                // (31,13): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             c.Property = null; // 4
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(31, 13),
                // (41,18): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //                 (c.Property = null)  // 5
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(41, 18)
                );
        }

        [Fact]
        public void DeconstructionAssignmentDisallowed()
        {
            string source = @"
public class C
{
    public string Property { get; init; }

    void M()
    {
        (Property, (Property, Property)) = (null, (null, null)); // 1, 2, 3
    }

    C()
    {
        (Property, (Property, Property)) = (null, (null, null));
    }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,10): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         (Property, (Property, Property)) = (null, (null, null)); // 1, 2, 3
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(8, 10),
                // (8,21): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         (Property, (Property, Property)) = (null, (null, null)); // 1, 2, 3
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(8, 21),
                // (8,31): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         (Property, (Property, Property)) = (null, (null, null)); // 1, 2, 3
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(8, 31)
                );
        }

        [Fact]
        public void OutParameterAssignmentDisallowed()
        {
            string source = @"
public class C
{
    public string Property { get; init; }

    void M()
    {
        M2(out Property); // 1
    }

    void M2(out string s) => throw null;
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,16): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         M2(out Property); // 1
                Diagnostic(ErrorCode.ERR_RefProperty, "Property").WithArguments("C.Property").WithLocation(8, 16)
                );
        }

        [Fact]
        public void CompoundAssignmentDisallowed()
        {
            string source = @"
public class C
{
    public int Property { get; init; }

    void M()
    {
        Property += 42; // 1
    }

    C()
    {
        Property += 42;
    }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,9): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         Property += 42; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(8, 9)
                );
        }

        [Fact]
        public void CompoundAssignmentDisallowed_OrAssignment()
        {
            string source = @"
public class C
{
    public bool Property { get; init; }

    void M()
    {
        Property |= true; // 1
    }

    C()
    {
        Property |= true;
    }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,9): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         Property |= true; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(8, 9)
                );
        }

        [Fact]
        public void CompoundAssignmentDisallowed_NullCoalescingAssignment()
        {
            string source = @"
public class C
{
    public string Property { get; init; }

    void M()
    {
        Property ??= null; // 1
    }

    C()
    {
        Property ??= null;
    }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,9): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         Property ??= null; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(8, 9)
                );
        }

        [Fact]
        public void CompoundAssignmentDisallowed_Increment()
        {
            string source = @"
public class C
{
    public int Property { get; init; }

    void M()
    {
        Property++; // 1
    }

    C()
    {
        Property++;
    }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,9): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         Property++; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(8, 9)
                );
        }

        [Fact]
        public void RefProperty()
        {
            string source = @"
public class C
{
    ref int Property1 { get; init; }
    ref int Property3 { init; }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,13): error CS8145: Auto-implemented properties cannot return by reference
                //     ref int Property1 { get; init; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "Property1").WithArguments("C.Property1").WithLocation(4, 13),
                // (4,30): error CS8147: Properties which return by reference cannot have set accessors
                //     ref int Property1 { get; init; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "init").WithArguments("C.Property1.set").WithLocation(4, 30),
                // (5,13): error CS8146: Properties which return by reference must have a get accessor
                //     ref int Property3 { init; }
                Diagnostic(ErrorCode.ERR_RefPropertyMustHaveGetAccessor, "Property3").WithArguments("C.Property3").WithLocation(5, 13)
                );
        }

        [Fact]
        public void VerifyPESymbols_Property()
        {
            string source = @"
public class C
{
    public string Property { get; init; }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition },
                parseOptions: TestOptions.RegularPreview,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            comp.VerifyDiagnostics();

            if (ExecutionConditionUtil.IsCoreClr)
            {
                CompileAndVerify(comp, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator);
            }
            else
            {
                // PE verification fails:  [ : C::set_Property] Cannot change initonly field outside its .ctor.
                CompileAndVerify(comp, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator, verify: Verification.Fails);
            }

            void symbolValidator(ModuleSymbol m)
            {
                bool isSource = !(m is PEModuleSymbol);
                var c = (NamedTypeSymbol)m.GlobalNamespace.GetMember("C");

                var property = (PropertySymbol)c.GetMembers("Property").Single();
                // PROTOTYPE(records): adjust SymbolDisplayVisitor once we have a public IsInitOnly API
                Assert.Equal("System.String C.Property { get; set; }", property.ToTestDisplayString());
                var propertyAttributes = property.GetAttributes().Select(a => a.ToString());
                AssertEx.Empty(propertyAttributes);

                var getterAttributes = property.GetMethod.GetAttributes().Select(a => a.ToString());
                if (isSource)
                {
                    AssertEx.Empty(getterAttributes);
                }
                else
                {
                    AssertEx.Equal(new[] { "System.Runtime.CompilerServices.CompilerGeneratedAttribute" }, getterAttributes);
                }

                var setterAttributes = property.SetMethod.GetAttributes().Select(a => a.ToString());
                var modifier = property.SetMethod.ReturnTypeWithAnnotations.CustomModifiers.Single();
                Assert.Equal("System.Runtime.CompilerServices.IsInitOnly", modifier.Modifier.ToTestDisplayString());
                Assert.False(modifier.IsOptional);

                if (isSource)
                {
                    AssertEx.Empty(setterAttributes);
                }
                else
                {
                    AssertEx.Equal(new[] { "System.Runtime.CompilerServices.CompilerGeneratedAttribute" }, setterAttributes);
                }

                var backingField = (FieldSymbol)c.GetMembers("<Property>k__BackingField").Single();
                var backingFieldAttributes = backingField.GetAttributes().Select(a => a.ToString());
                Assert.True(backingField.IsReadOnly);
                if (isSource)
                {
                    AssertEx.Empty(backingFieldAttributes);
                }
                else
                {
                    AssertEx.Equal(
                        new[] { "System.Runtime.CompilerServices.CompilerGeneratedAttribute",
                            "System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)" },
                        backingFieldAttributes);

                    var peBackingField = (PEFieldSymbol)backingField;
                    Assert.Equal(System.Reflection.FieldAttributes.InitOnly | System.Reflection.FieldAttributes.Private, peBackingField.Flags);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AssignmentDisallowed_PE(bool emitImage)
        {
            string lib_cs = @"
public class C
{
    public string Property { get; init; }
}
";
            var libComp = CreateCompilation(new[] { lib_cs, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            libComp.VerifyDiagnostics();

            string source = @"
public class Other
{
    public C c;

    void M()
    {
        c.Property = null; // 1
    }

    public Other()
    {
        c.Property = null; // 2
    }

    public string InitOnlyProperty
    {
        get
        {
            c.Property = null; // 3
            return null;
        }
        init
        {
            c.Property = null; // 4
        }
    }

    public string RegularProperty
    {
        get
        {
            c.Property = null; // 5
            return null;
        }
        set
        {
            c.Property = null; // 6
        }
    }
}

class Derived : C
{
}

class Derived2 : Derived
{
    void M()
    {
        Property = null; // 7
    }

    Derived2()
    {
        Property = null;
    }

    public string InitOnlyProperty2
    {
        get
        {
            Property = null; // 8
            return null;
        }
        init
        {
            Property = null;
        }
    }

    public string RegularProperty2
    {
        get
        {
            Property = null; // 9
            return null;
        }
        set
        {
            Property = null; // 10
        }
    }
}
";
            var comp = CreateCompilation(source,
                references: new[] { emitImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() },
                parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics(
                // (8,9): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         c.Property = null; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(8, 9),
                // (13,9): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         c.Property = null; // 2
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(13, 9),
                // (20,13): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             c.Property = null; // 3
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(20, 13),
                // (25,13): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             c.Property = null; // 4
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(25, 13),
                // (33,13): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             c.Property = null; // 5
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(33, 13),
                // (38,13): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             c.Property = null; // 6
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(38, 13),
                // (51,9): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         Property = null; // 7
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(51, 9),
                // (63,13): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             Property = null; // 8
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(63, 13),
                // (76,13): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             Property = null; // 9
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(76, 13),
                // (81,13): error CS8802: Init-only member 'C.Property' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //             Property = null; // 10
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(81, 13)
                );
        }

        [Fact]
        public void OverridingInitOnlyProperty()
        {
            string source = @"
public class Base
{
    public virtual string Property { get; init; }
}

public class DerivedWithInit : Base
{
    public override string Property { get; init; }
}
public class DerivedWithoutInit : Base
{
    public override string Property { get; set; } // 1
}

public class DerivedWithInitSetterOnly : Base
{
    public override string Property { init { } }
}
public class DerivedWithoutInitSetterOnly : Base
{
    public override string Property { set { } } // 2
}

public class DerivedGetterOnly : Base
{
    public override string Property { get => null; }
}
public class DerivedDerivedWithInit : DerivedGetterOnly
{
    public override string Property { init { } }
}
public class DerivedDerivedWithoutInit : DerivedGetterOnly
{
    public override string Property { set { } } // 3
}
";

            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (13,28): error CS8803: 'DerivedWithoutInit.Property' must match by init-only of overridden member 'Base.Property'
                //     public override string Property { get; set; } // 1
                Diagnostic(ErrorCode.ERR_CantChangeInitOnlyOnOverride, "Property").WithArguments("DerivedWithoutInit.Property", "Base.Property").WithLocation(13, 28),
                // (22,28): error CS8803: 'DerivedWithoutInitSetterOnly.Property' must match by init-only of overridden member 'Base.Property'
                //     public override string Property { set { } } // 2
                Diagnostic(ErrorCode.ERR_CantChangeInitOnlyOnOverride, "Property").WithArguments("DerivedWithoutInitSetterOnly.Property", "Base.Property").WithLocation(22, 28),
                // (35,28): error CS8803: 'DerivedDerivedWithoutInit.Property' must match by init-only of overridden member 'DerivedGetterOnly.Property'
                //     public override string Property { set { } } // 3
                Diagnostic(ErrorCode.ERR_CantChangeInitOnlyOnOverride, "Property").WithArguments("DerivedDerivedWithoutInit.Property", "DerivedGetterOnly.Property").WithLocation(35, 28)
                );
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void OverridingInitOnlyProperty_Metadata(bool emitAsImage)
        {
            string lib_cs = @"
public class Base
{
    public virtual string Property { get; init; }
}";
            var libComp = CreateCompilation(new[] { lib_cs, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            libComp.VerifyDiagnostics();

            string source = @"
public class DerivedWithInit : Base
{
    public override string Property { get; init; }
}
public class DerivedWithoutInit : Base
{
    public override string Property { get; set; } // 1
}
public class DerivedWithInitSetterOnly : Base
{
    public override string Property { init { } }
}
public class DerivedWithoutInitSetterOnly : Base
{
    public override string Property { set { } } // 2
}
public class DerivedGetterOnly : Base
{
    public override string Property { get => null; }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview,
                references: new[] { emitAsImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() });
            comp.VerifyDiagnostics(
                // (8,28): error CS8803: 'DerivedWithoutInit.Property' must match by init-only of overridden member 'Base.Property'
                //     public override string Property { get; set; } // 1
                Diagnostic(ErrorCode.ERR_CantChangeInitOnlyOnOverride, "Property").WithArguments("DerivedWithoutInit.Property", "Base.Property").WithLocation(8, 28),
                // (16,28): error CS8803: 'DerivedWithoutInitSetterOnly.Property' must match by init-only of overridden member 'Base.Property'
                //     public override string Property { set { } } // 2
                Diagnostic(ErrorCode.ERR_CantChangeInitOnlyOnOverride, "Property").WithArguments("DerivedWithoutInitSetterOnly.Property", "Base.Property").WithLocation(16, 28)
                );
        }

        [Fact]
        public void OverridingRegularProperty()
        {
            string source = @"
public class Base
{
    public virtual string Property { get; set; }
}
public class DerivedWithInit : Base
{
    public override string Property { get; init; } // 1
}
public class DerivedWithoutInit : Base
{
    public override string Property { get; set; }
}
public class DerivedWithInitSetterOnly : Base
{
    public override string Property { init { } } // 2
}
public class DerivedWithoutInitSetterOnly : Base
{
    public override string Property { set { } }
}
public class DerivedGetterOnly : Base
{
    public override string Property { get => null; }
}
";

            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,28): error CS8803: 'DerivedWithInit.Property' must match by init-only of overridden member 'Base.Property'
                //     public override string Property { get; init; } // 1
                Diagnostic(ErrorCode.ERR_CantChangeInitOnlyOnOverride, "Property").WithArguments("DerivedWithInit.Property", "Base.Property").WithLocation(8, 28),
                // (16,28): error CS8803: 'DerivedWithInitSetterOnly.Property' must match by init-only of overridden member 'Base.Property'
                //     public override string Property { init { } } // 2
                Diagnostic(ErrorCode.ERR_CantChangeInitOnlyOnOverride, "Property").WithArguments("DerivedWithInitSetterOnly.Property", "Base.Property").WithLocation(16, 28)
                );
        }

        [Fact]
        public void OverridingGetterOnlyProperty()
        {
            string source = @"
public class Base
{
    public virtual string Property { get => null; }
}
public class DerivedWithInit : Base
{
    public override string Property { get; init; } // 1
}
public class DerivedWithoutInit : Base
{
    public override string Property { get; set; } // 2
}
public class DerivedWithInitSetterOnly : Base
{
    public override string Property { init { } } // 3
}
public class DerivedWithoutInitSetterOnly : Base
{
    public override string Property { set { } } // 4
}
";

            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,44): error CS0546: 'DerivedWithInit.Property.set': cannot override because 'Base.Property' does not have an overridable set accessor
                //     public override string Property { get; init; } // 1
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "init").WithArguments("DerivedWithInit.Property.set", "Base.Property").WithLocation(8, 44),
                // (12,44): error CS0546: 'DerivedWithoutInit.Property.set': cannot override because 'Base.Property' does not have an overridable set accessor
                //     public override string Property { get; set; } // 2
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("DerivedWithoutInit.Property.set", "Base.Property").WithLocation(12, 44),
                // (16,39): error CS0546: 'DerivedWithInitSetterOnly.Property.set': cannot override because 'Base.Property' does not have an overridable set accessor
                //     public override string Property { init { } } // 3
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "init").WithArguments("DerivedWithInitSetterOnly.Property.set", "Base.Property").WithLocation(16, 39),
                // (20,39): error CS0546: 'DerivedWithoutInitSetterOnly.Property.set': cannot override because 'Base.Property' does not have an overridable set accessor
                //     public override string Property { set { } } // 4
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("DerivedWithoutInitSetterOnly.Property.set", "Base.Property").WithLocation(20, 39)
                );
        }

        [Fact]
        public void OverridingSetterOnlyProperty()
        {
            string source = @"
public class Base
{
    public virtual string Property { set { } }
}
public class DerivedWithInit : Base
{
    public override string Property { get; init; } // 1, 2
}
public class DerivedWithoutInit : Base
{
    public override string Property { get; set; } // 3
}
public class DerivedWithInitSetterOnly : Base
{
    public override string Property { init { } } // 4
}
public class DerivedWithoutInitGetterOnly : Base
{
    public override string Property { get => null; } // 5
}
";

            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,28): error CS8803: 'DerivedWithInit.Property' must match by init-only of overridden member 'Base.Property'
                //     public override string Property { get; init; } // 1, 2
                Diagnostic(ErrorCode.ERR_CantChangeInitOnlyOnOverride, "Property").WithArguments("DerivedWithInit.Property", "Base.Property").WithLocation(8, 28),
                // (8,39): error CS0545: 'DerivedWithInit.Property.get': cannot override because 'Base.Property' does not have an overridable get accessor
                //     public override string Property { get; init; } // 1, 2
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("DerivedWithInit.Property.get", "Base.Property").WithLocation(8, 39),
                // (12,39): error CS0545: 'DerivedWithoutInit.Property.get': cannot override because 'Base.Property' does not have an overridable get accessor
                //     public override string Property { get; set; } // 3
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("DerivedWithoutInit.Property.get", "Base.Property").WithLocation(12, 39),
                // (16,28): error CS8803: 'DerivedWithInitSetterOnly.Property' must match by init-only of overridden member 'Base.Property'
                //     public override string Property { init { } } // 4
                Diagnostic(ErrorCode.ERR_CantChangeInitOnlyOnOverride, "Property").WithArguments("DerivedWithInitSetterOnly.Property", "Base.Property").WithLocation(16, 28),
                // (20,39): error CS0545: 'DerivedWithoutInitGetterOnly.Property.get': cannot override because 'Base.Property' does not have an overridable get accessor
                //     public override string Property { get => null; } // 5
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("DerivedWithoutInitGetterOnly.Property.get", "Base.Property").WithLocation(20, 39)
                );
        }

        [Fact]
        public void ImplementingInitOnlyProperty()
        {
            string source = @"
public interface I
{
    string Property { get; init; }
}
public class DerivedWithInit : I
{
    public string Property { get; init; }
}
public class DerivedWithoutInit : I // 1
{
    public string Property { get; set; }
}
public class DerivedWithInitSetterOnly : I // 2
{
    public string Property { init { } }
}
public class DerivedWithoutInitGetterOnly : I // 3
{
    public string Property { get => null; }
}
";

            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (10,35): error CS8804: 'DerivedWithoutInit' does not implement interface member 'I.Property.set'. 'DerivedWithoutInit.Property.set' cannot implement 'I.Property.set' because it does not match by init-only.
                // public class DerivedWithoutInit : I // 1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I").WithArguments("DerivedWithoutInit", "I.Property.set", "DerivedWithoutInit.Property.set").WithLocation(10, 35),
                // (14,42): error CS0535: 'DerivedWithInitSetterOnly' does not implement interface member 'I.Property.get'
                // public class DerivedWithInitSetterOnly : I // 2
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I").WithArguments("DerivedWithInitSetterOnly", "I.Property.get").WithLocation(14, 42),
                // (18,45): error CS0535: 'DerivedWithoutInitGetterOnly' does not implement interface member 'I.Property.set'
                // public class DerivedWithoutInitGetterOnly : I // 3
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I").WithArguments("DerivedWithoutInitGetterOnly", "I.Property.set").WithLocation(18, 45)
                );
        }

        [Fact]
        public void ImplementingSetterOnlyProperty()
        {
            string source = @"
public interface I
{
    string Property { set; }
}
public class DerivedWithInit : I // 1
{
    public string Property { get; init; }
}
public class DerivedWithoutInit : I
{
    public string Property { get; set; }
}
public class DerivedWithInitSetterOnly : I // 2
{
    public string Property { init { } }
}
public class DerivedWithoutInitGetterOnly : I // 3
{
    public string Property { get => null; }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,32): error CS8804: 'DerivedWithInit' does not implement interface member 'I.Property.set'. 'DerivedWithInit.Property.set' cannot implement 'I.Property.set' because it does not match by init-only.
                // public class DerivedWithInit : I // 1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I").WithArguments("DerivedWithInit", "I.Property.set", "DerivedWithInit.Property.set").WithLocation(6, 32),
                // (14,42): error CS8804: 'DerivedWithInitSetterOnly' does not implement interface member 'I.Property.set'. 'DerivedWithInitSetterOnly.Property.set' cannot implement 'I.Property.set' because it does not match by init-only.
                // public class DerivedWithInitSetterOnly : I // 2
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I").WithArguments("DerivedWithInitSetterOnly", "I.Property.set", "DerivedWithInitSetterOnly.Property.set").WithLocation(14, 42),
                // (18,45): error CS0535: 'DerivedWithoutInitGetterOnly' does not implement interface member 'I.Property.set'
                // public class DerivedWithoutInitGetterOnly : I // 3
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I").WithArguments("DerivedWithoutInitGetterOnly", "I.Property.set").WithLocation(18, 45)
                );
        }

        [Fact]
        public void ObjectCreationOnInterface()
        {
            string source = @"
public interface I
{
    string Property { set; }
    string InitProperty { init; }
}
public class C
{
    void M<T>() where T: I, new()
    {
        _ = new T() { Property = null, InitProperty = null };
    }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void HidingInitOnlySetterOnlyProperty()
        {
            string source = @"
public class Base
{
    public string Property { init { } }
}
public class Derived : Base
{
    public string Property { init { } } // 1
}
public class DerivedWithNew : Base
{
    public new string Property { init { } }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,19): warning CS0108: 'Derived.Property' hides inherited member 'Base.Property'. Use the new keyword if hiding was intended.
                //     public string Property { init { } } // 1
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("Derived.Property", "Base.Property").WithLocation(8, 19)
                );
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ImplementingSetterOnlyProperty_Metadata(bool emitAsImage)
        {
            string lib_cs = @"
public interface I
{
    string Property { set; }
}";
            var libComp = CreateCompilation(new[] { lib_cs, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            libComp.VerifyDiagnostics();

            string source = @"
public class DerivedWithInit : I // 1
{
    public string Property { get; init; }
}
public class DerivedWithoutInit : I
{
    public string Property { get; set; }
}
public class DerivedWithInitSetterOnly : I // 2
{
    public string Property { init { } }
}
public class DerivedWithoutInitGetterOnly : I // 3
{
    public string Property { get => null; }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview,
                references: new[] { emitAsImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() });
            comp.VerifyDiagnostics(
                // (2,32): error CS8804: 'DerivedWithInit' does not implement interface member 'I.Property.set'. 'DerivedWithInit.Property.set' cannot implement 'I.Property.set' because it does not match by init-only.
                // public class DerivedWithInit : I // 1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I").WithArguments("DerivedWithInit", "I.Property.set", "DerivedWithInit.Property.set").WithLocation(2, 32),
                // (10,42): error CS8804: 'DerivedWithInitSetterOnly' does not implement interface member 'I.Property.set'. 'DerivedWithInitSetterOnly.Property.set' cannot implement 'I.Property.set' because it does not match by init-only.
                // public class DerivedWithInitSetterOnly : I // 2
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I").WithArguments("DerivedWithInitSetterOnly", "I.Property.set", "DerivedWithInitSetterOnly.Property.set").WithLocation(10, 42),
                // (14,45): error CS0535: 'DerivedWithoutInitGetterOnly' does not implement interface member 'I.Property.set'
                // public class DerivedWithoutInitGetterOnly : I // 3
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I").WithArguments("DerivedWithoutInitGetterOnly", "I.Property.set").WithLocation(14, 45)
                );
        }

        [Fact]
        public void ImplementingSetterOnlyProperty_Explicitly()
        {
            string source = @"
public interface I
{
    string Property { set; }
}
public class DerivedWithInit : I
{
    string I.Property { init { } } // 1
}
public class DerivedWithInitAndGetter : I
{
    string I.Property { get; init; } // 2, 3
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,25): error CS8805: Accessors 'DerivedWithInit.I.Property.set' and 'I.Property.set' should both be init-only or neither
                //     string I.Property { init { } } // 1
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMismatchInitOnly, "init").WithArguments("DerivedWithInit.I.Property.set", "I.Property.set").WithLocation(8, 25),
                // (12,25): error CS0550: 'DerivedWithInitAndGetter.I.Property.get' adds an accessor not found in interface member 'I.Property'
                //     string I.Property { get; init; } // 2, 3
                Diagnostic(ErrorCode.ERR_ExplicitPropertyAddingAccessor, "get").WithArguments("DerivedWithInitAndGetter.I.Property.get", "I.Property").WithLocation(12, 25),
                // (12,30): error CS8805: Accessors 'DerivedWithInitAndGetter.I.Property.set' and 'I.Property.set' should both be init-only or neither
                //     string I.Property { get; init; } // 2, 3
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMismatchInitOnly, "init").WithArguments("DerivedWithInitAndGetter.I.Property.set", "I.Property.set").WithLocation(12, 30)
                );
        }

        [Fact]
        public void ImplementingSetterOnlyInitOnlyProperty_Explicitly()
        {
            string source = @"
public interface I
{
    string Property { init; }
}
public class DerivedWithoutInit : I
{
    string I.Property { set { } } // 1
}
public class DerivedWithInit : I
{
    string I.Property { init { } }
}
public class DerivedWithInitAndGetter : I
{
    string I.Property { get; init; } // 2
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,25): error CS8805: Accessors 'DerivedWithoutInit.I.Property.set' and 'I.Property.set' should both be init-only or neither
                //     string I.Property { set { } } // 1
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMismatchInitOnly, "set").WithArguments("DerivedWithoutInit.I.Property.set", "I.Property.set").WithLocation(8, 25),
                // (16,25): error CS0550: 'DerivedWithInitAndGetter.I.Property.get' adds an accessor not found in interface member 'I.Property'
                //     string I.Property { get; init; } // 2
                Diagnostic(ErrorCode.ERR_ExplicitPropertyAddingAccessor, "get").WithArguments("DerivedWithInitAndGetter.I.Property.get", "I.Property").WithLocation(16, 25)
                );
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ImplementingSetterOnlyInitOnlyProperty_Metadata_Explicitly(bool emitAsImage)
        {
            string lib_cs = @"
public interface I
{
    string Property { init; }
}";
            var libComp = CreateCompilation(new[] { lib_cs, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            libComp.VerifyDiagnostics();

            string source = @"
public class DerivedWithoutInit : I
{
    string I.Property { set { } } // 1
}
public class DerivedWithInit : I
{
    string I.Property { init { } }
}
public class DerivedGetterOnly : I // 2
{
    string I.Property { get => null; } // 3, 4
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview,
                references: new[] { emitAsImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() });
            comp.VerifyDiagnostics(
                // (4,25): error CS8805: Accessors 'DerivedWithoutInit.I.Property.set' and 'I.Property.set' should both be init-only or neither
                //     string I.Property { set { } } // 1
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMismatchInitOnly, "set").WithArguments("DerivedWithoutInit.I.Property.set", "I.Property.set").WithLocation(4, 25),
                // (10,34): error CS0535: 'DerivedGetterOnly' does not implement interface member 'I.Property.set'
                // public class DerivedGetterOnly : I // 2
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I").WithArguments("DerivedGetterOnly", "I.Property.set").WithLocation(10, 34),
                // (12,14): error CS0551: Explicit interface implementation 'DerivedGetterOnly.I.Property' is missing accessor 'I.Property.set'
                //     string I.Property { get => null; } // 3, 4
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMissingAccessor, "Property").WithArguments("DerivedGetterOnly.I.Property", "I.Property.set").WithLocation(12, 14),
                // (12,25): error CS0550: 'DerivedGetterOnly.I.Property.get' adds an accessor not found in interface member 'I.Property'
                //     string I.Property { get => null; } // 3, 4
                Diagnostic(ErrorCode.ERR_ExplicitPropertyAddingAccessor, "get").WithArguments("DerivedGetterOnly.I.Property.get", "I.Property").WithLocation(12, 25)
                );
        }

        [Fact]
        public void DIM_TwoInitOnlySetters()
        {
            string source = @"
public interface I1
{
    string Property { init; }
}
public interface I2
{
    string Property { init; }
}
public interface IWithoutInit : I1, I2
{
    string Property { set; } // 1
}
public interface IWithInit : I1, I2
{
    string Property { init; } // 2
}
public interface IWithInitWithNew : I1, I2
{
    new string Property { init; }
}
public interface IWithInitWithDefaultImplementation : I1, I2
{
    string Property { init { } } // 3
}
public interface IWithInitWithExplicitImplementation : I1, I2
{
    string I1.Property { init { } }
}
";

            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition },
                targetFramework: TargetFramework.NetStandardLatest,
                parseOptions: TestOptions.RegularPreview);
            Assert.True(comp.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            comp.VerifyDiagnostics(
                // (12,12): warning CS0108: 'IWithoutInit.Property' hides inherited member 'I1.Property'. Use the new keyword if hiding was intended.
                //     string Property { set; } // 1
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("IWithoutInit.Property", "I1.Property").WithLocation(12, 12),
                // (16,12): warning CS0108: 'IWithInit.Property' hides inherited member 'I1.Property'. Use the new keyword if hiding was intended.
                //     string Property { init; } // 2
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("IWithInit.Property", "I1.Property").WithLocation(16, 12),
                // (24,12): warning CS0108: 'IWithInitWithDefaultImplementation.Property' hides inherited member 'I1.Property'. Use the new keyword if hiding was intended.
                //     string Property { init { } } // 3
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("IWithInitWithDefaultImplementation.Property", "I1.Property").WithLocation(24, 12)
                );
        }

        [Fact]
        public void DIM_OneInitOnlySetter()
        {
            string source = @"
public interface I1
{
    string Property { init; }
}
public interface I2
{
    string Property { set; }
}

public interface IWithoutInit : I1, I2
{
    string Property { set; } // 1
}
public interface IWithInit : I1, I2
{
    string Property { init; } // 2
}

public interface IWithInitWithNew : I1, I2
{
    new string Property { init; }
}
public interface IWithoutInitWithNew : I1, I2
{
    new string Property { set; }
}

public interface IWithInitWithImplementation : I1, I2
{
    string Property { init { } } // 3
}

public interface IWithInitWithExplicitImplementationOfI1 : I1, I2
{
    string I1.Property { init { } }
}
public interface IWithInitWithExplicitImplementationOfI2 : I1, I2
{
    string I2.Property { init { } } // 4
}

public interface IWithoutInitWithExplicitImplementationOfI1 : I1, I2
{
    string I1.Property { set { } } // 5
}
public interface IWithoutInitWithExplicitImplementationOfI2 : I1, I2
{
    string I2.Property { set { } }
}
public interface IWithoutInitWithExplicitImplementationOfBoth : I1, I2
{
    string I1.Property { init { } }
    string I2.Property { set { } }
}

public class CWithExplicitImplementation : I1, I2
{
    string I1.Property { init { } }
    string I2.Property { set { } }
}
public class CWithImplementationWithInitOnly : I1, I2 // 6
{
    public string Property { init { } }
}
public class CWithImplementationWithoutInitOnly : I1, I2 // 7
{
    public string Property { set { } }
}
";

            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition },
                targetFramework: TargetFramework.NetStandardLatest,
                parseOptions: TestOptions.RegularPreview);
            Assert.True(comp.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            comp.VerifyDiagnostics(
                // (13,12): warning CS0108: 'IWithoutInit.Property' hides inherited member 'I1.Property'. Use the new keyword if hiding was intended.
                //     string Property { set; } // 1
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("IWithoutInit.Property", "I1.Property").WithLocation(13, 12),
                // (17,12): warning CS0108: 'IWithInit.Property' hides inherited member 'I1.Property'. Use the new keyword if hiding was intended.
                //     string Property { init; } // 2
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("IWithInit.Property", "I1.Property").WithLocation(17, 12),
                // (31,12): warning CS0108: 'IWithInitWithImplementation.Property' hides inherited member 'I1.Property'. Use the new keyword if hiding was intended.
                //     string Property { init { } } // 3
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("IWithInitWithImplementation.Property", "I1.Property").WithLocation(31, 12),
                // (40,26): error CS8805: Accessors 'IWithInitWithExplicitImplementationOfI2.I2.Property.set' and 'I2.Property.set' should both be init-only or neither
                //     string I2.Property { init { } } // 4
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMismatchInitOnly, "init").WithArguments("IWithInitWithExplicitImplementationOfI2.I2.Property.set", "I2.Property.set").WithLocation(40, 26),
                // (45,26): error CS8805: Accessors 'IWithoutInitWithExplicitImplementationOfI1.I1.Property.set' and 'I1.Property.set' should both be init-only or neither
                //     string I1.Property { set { } } // 5
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMismatchInitOnly, "set").WithArguments("IWithoutInitWithExplicitImplementationOfI1.I1.Property.set", "I1.Property.set").WithLocation(45, 26),
                // (62,52): error CS8804: 'CWithImplementationWithInitOnly' does not implement interface member 'I2.Property.set'. 'CWithImplementationWithInitOnly.Property.set' cannot implement 'I2.Property.set' because it does not match by init-only.
                // public class CWithImplementationWithInitOnly : I1, I2 // 6
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I2").WithArguments("CWithImplementationWithInitOnly", "I2.Property.set", "CWithImplementationWithInitOnly.Property.set").WithLocation(62, 52),
                // (66,51): error CS8804: 'CWithImplementationWithoutInitOnly' does not implement interface member 'I1.Property.set'. 'CWithImplementationWithoutInitOnly.Property.set' cannot implement 'I1.Property.set' because it does not match by init-only.
                // public class CWithImplementationWithoutInitOnly : I1, I2 // 7
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I1").WithArguments("CWithImplementationWithoutInitOnly", "I1.Property.set", "CWithImplementationWithoutInitOnly.Property.set").WithLocation(66, 51)
                );
        }

        [Fact]
        public void EventWithInitOnly()
        {
            string source = @"
public class C
{
    public event System.Action Event
    {
        init { }
    }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,32): error CS0065: 'C.Event': event property must have both add and remove accessors
                //     public event System.Action Event
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "Event").WithArguments("C.Event").WithLocation(4, 32),
                // (6,9): error CS1055: An add or remove accessor expected
                //         init { }
                Diagnostic(ErrorCode.ERR_AddOrRemoveExpected, "init").WithLocation(6, 9)
                );
        }

        [Fact]
        public void IndexerWithInitOnly()
        {
            string source = @"
public class C
{
    public string this[int i]
    {
        init { }
    }
    public C()
    {
        this[42] = null;
    }
    public void M1()
    {
        this[43] = null; // 1
    }
}
public class Derived : C
{
    public Derived()
    {
        this[44] = null;
    }
    public void M2()
    {
        this[45] = null; // 2
    }
}
public class D
{
    void M3(C c2)
    {
        _ = new C() { [46] = null };
        c2[47] = null; // 3
    }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (14,9): error CS8802: Init-only member 'C.this[int]' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         this[43] = null; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "this[43]").WithArguments("C.this[int]").WithLocation(14, 9),
                // (25,9): error CS8802: Init-only member 'C.this[int]' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         this[45] = null; // 2
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "this[45]").WithArguments("C.this[int]").WithLocation(25, 9),
                // (33,9): error CS8802: Init-only member 'C.this[int]' can only be assigned from a constructor, object initialization or 'with' expression of that type.
                //         c2[47] = null; // 3
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c2[47]").WithArguments("C.this[int]").WithLocation(33, 9)
                );
        }

        [Fact]
        public void ReadonlyFields()
        {
            string source = @"
public class C
{
    public readonly string field;
    public C()
    {
        field = null;
    }
    public void M1()
    {
        field = null; // 1
        _ = new C() { field = null }; // 2
    }
    public int InitOnlyProperty1
    {
        init
        {
            field = null;
        }
    }
    public int RegularProperty
    {
        get
        {
            field = null; // 3
            throw null;
        }
        set
        {
            field = null; // 4
        }
    }
}
public class Derived : C
{
    public Derived()
    {
        field = null; // 5
    }
    public void M2()
    {
        field = null; // 6
    }
    public int InitOnlyProperty2
    {
        init
        {
            field = null; // 7
        }
    }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (11,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //         field = null; // 1
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(11, 9),
                // (12,23): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //         _ = new C() { field = null }; // 2
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(12, 23),
                // (25,13): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //             field = null; // 3
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(25, 13),
                // (30,13): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //             field = null; // 4
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(30, 13),
                // (38,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //         field = null; // 5
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(38, 9),
                // (42,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //         field = null; // 6
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(42, 9),
                // (48,13): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //             field = null; // 7
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(48, 13)
                );
        }

        [Fact]
        public void StaticReadonlyFieldInitializedByAnother()
        {
            string source = @"
public class C
{
    public static readonly int field;
    public static readonly int field2 = (field = 42);
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadonlyFields_DisallowedOnOtherInstances()
        {
            string source = @"
public class C
{
    public readonly string field;
    public C c;

    public C()
    {
        c.field = null; // 1
    }

    public string InitOnlyProperty
    {
        init
        {
            c.field = null; // 2
        }
    }
}
public class Derived : C
{
    Derived()
    {
        c.field = null; // 3
    }

    public string InitOnlyProperty2
    {
        init
        {
            c.field = null; // 4
        }
    }
}
public class Caller
{
    void M(C c)
    {
        _ = new C() {
            field = // 5
                (c.field = null)  // 6 
        };
    }
}
";

            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (9,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //         c.field = null; // 1
                Diagnostic(ErrorCode.ERR_AssgReadonly, "c.field").WithLocation(9, 9),
                // (16,13): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //             c.field = null; // 2
                Diagnostic(ErrorCode.ERR_AssgReadonly, "c.field").WithLocation(16, 13),
                // (24,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //         c.field = null; // 3
                Diagnostic(ErrorCode.ERR_AssgReadonly, "c.field").WithLocation(24, 9),
                // (31,13): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //             c.field = null; // 4
                Diagnostic(ErrorCode.ERR_AssgReadonly, "c.field").WithLocation(31, 13),
                // (40,13): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //             field = // 5
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(40, 13),
                // (41,18): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //                 (c.field = null)  // 6 
                Diagnostic(ErrorCode.ERR_AssgReadonly, "c.field").WithLocation(41, 18)
                );
        }

        [Fact]
        public void ReadonlyFieldsMembers()
        {
            string source = @"
public struct Container
{
    public string content;
}
public class C
{
    public readonly Container field;
    public C()
    {
        field.content = null;
    }
    public void M1()
    {
        field.content = null; // 1
    }
    public int InitOnlyProperty1
    {
        init
        {
            field.content = null;
        }
    }
    public int RegularProperty
    {
        get
        {
            field.content = null; // 2
            throw null;
        }
        set
        {
            field.content = null; // 3
        }
    }
}
public class Derived : C
{
    public Derived()
    {
        field.content = null; // 4
    }
    public void M2()
    {
        field.content = null; // 5
    }
    public int InitOnlyProperty2
    {
        init
        {
            field.content = null; // 6
        }
    }
}
";
            var comp = CreateCompilation(new[] { source, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (15,9): error CS1648: Members of readonly field 'C.field' cannot be modified (except in a constructor, an init-only member or a variable initializer)
                //         field.content = null; // 1
                Diagnostic(ErrorCode.ERR_AssgReadonly2, "field.content").WithArguments("C.field").WithLocation(15, 9),
                // (28,13): error CS1648: Members of readonly field 'C.field' cannot be modified (except in a constructor, an init-only member or a variable initializer)
                //             field.content = null; // 2
                Diagnostic(ErrorCode.ERR_AssgReadonly2, "field.content").WithArguments("C.field").WithLocation(28, 13),
                // (33,13): error CS1648: Members of readonly field 'C.field' cannot be modified (except in a constructor, an init-only member or a variable initializer)
                //             field.content = null; // 3
                Diagnostic(ErrorCode.ERR_AssgReadonly2, "field.content").WithArguments("C.field").WithLocation(33, 13),
                // (41,9): error CS1648: Members of readonly field 'C.field' cannot be modified (except in a constructor, an init-only member or a variable initializer)
                //         field.content = null; // 4
                Diagnostic(ErrorCode.ERR_AssgReadonly2, "field.content").WithArguments("C.field").WithLocation(41, 9),
                // (45,9): error CS1648: Members of readonly field 'C.field' cannot be modified (except in a constructor, an init-only member or a variable initializer)
                //         field.content = null; // 5
                Diagnostic(ErrorCode.ERR_AssgReadonly2, "field.content").WithArguments("C.field").WithLocation(45, 9),
                // (51,13): error CS1648: Members of readonly field 'C.field' cannot be modified (except in a constructor, an init-only member or a variable initializer)
                //             field.content = null; // 6
                Diagnostic(ErrorCode.ERR_AssgReadonly2, "field.content").WithArguments("C.field").WithLocation(51, 13)
                );
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadonlyFields_Metadata(bool emitAsImage)
        {
            string lib_cs = @"
public class C
{
    public readonly string field;
}
";
            var libComp = CreateCompilation(new[] { lib_cs, IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);

            string source = @"
public class Derived : C
{
    public Derived()
    {
        field = null; // 1
        _ = new C() { field = null }; // 2

    }
    public void M2()
    {
        field = null; // 3
        _ = new C() { field = null }; // 4

    }
    public int InitOnlyProperty2
    {
        init
        {
            field = null; // 5
        }
    }
}
";
            var comp = CreateCompilation(source,
                references: new[] { emitAsImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() },
                parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //         field = null; // 1
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(6, 9),
                // (7,23): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //         _ = new C() { field = null }; // 2
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(7, 23),
                // (12,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //         field = null; // 3
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(12, 9),
                // (13,23): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //         _ = new C() { field = null }; // 4
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(13, 23),
                // (20,13): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the class in which the field is defined or a variable initializer))
                //             field = null; // 5
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(20, 13)
                );
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForPropertyAccessorBody()
        {
            var compilation = CreateCompilation(@"
class R
{
    private int _p;
}

class C : R 
{
    
    private int M
    {
        init
        {
            int y = 1000;
        }
    }
}
");

            var blockStatement = (BlockSyntax)SyntaxFactory.ParseStatement(@"
{ 
   int z = 0; 

   _p = 123L;
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            AccessorDeclarationSyntax accessorDecl = root.DescendantNodes().OfType<AccessorDeclarationSyntax>().Single();

            var speculatedMethod = accessorDecl.ReplaceNode(accessorDecl.Body, blockStatement);

            SemanticModel speculativeModel;
            var success =
                model.TryGetSpeculativeSemanticModelForMethodBody(
                    accessorDecl.Body.Statements[0].SpanStart, speculatedMethod, out speculativeModel);

            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var p =
                speculativeModel.SyntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Single(s => s.Identifier.ValueText == "_p");

            var symbolSpeculation =
                speculativeModel.GetSpeculativeSymbolInfo(p.FullSpan.Start, p, SpeculativeBindingOption.BindAsExpression);

            Assert.Equal("_p", symbolSpeculation.Symbol.Name);

            var typeSpeculation =
                speculativeModel.GetSpeculativeTypeInfo(p.FullSpan.Start, p, SpeculativeBindingOption.BindAsExpression);

            Assert.Equal("Int32", typeSpeculation.Type.Name);
        }

        [Fact]
        public void BlockBodyAndExpressionBody_14()
        {
            var comp = CreateCompilation(new[] { @"
public class C
{
    static int P1 { get; set; }
    int P2
    {
        init { P1 = 1; } => P1 = 1;
    }
}
", IsInitOnlyTypeDefinition }, parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics(
                // (7,9): error CS8057: Block bodies and expression bodies cannot both be provided.
                //         init { P1 = 1; } => P1 = 1;
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, "init { P1 = 1; } => P1 = 1;").WithLocation(7, 9)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var nodes = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>();
            Assert.Equal(2, nodes.Count());

            foreach (var assign in nodes)
            {
                var node = assign.Left;
                Assert.Equal("P1", node.ToString());
                Assert.Equal("System.Int32 C.P1 { get; set; }", model.GetSymbolInfo(node).Symbol.ToTestDisplayString());
            }
        }

        [Fact]
        public void TestSyntaxFacts()
        {
            Assert.True(SyntaxFacts.IsAccessorDeclaration(SyntaxKind.InitAccessorDeclaration));
            Assert.True(SyntaxFacts.IsAccessorDeclarationKeyword(SyntaxKind.InitKeyword));
        }
    }
}
