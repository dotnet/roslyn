// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.InitOnlySetters)]
    public class InitOnlyMemberTests : CompilingTestBase
    {
        // Spec: https://github.com/dotnet/csharplang/blob/master/proposals/init.md

        // https://github.com/dotnet/roslyn/issues/44685
        // test allowed from 'with' expression
        // test dynamic scenario
        // test whether reflection use property despite modreq?
        // test behavior of old compiler with modreq. For example VB
        // test with ambiguous IsExternalInit types

        [Fact]
        public void TestCSharp8()
        {
            string source = @"
public class C
{
    public string Property { get; init; }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,35): error CS8652: The feature 'init-only setters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public string Property { get; init; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "init").WithArguments("init-only setters").WithLocation(4, 35)
                );

            var property = (PropertySymbol)comp.GlobalNamespace.GetMember("C.Property");
            Assert.False(property.GetMethod.IsInitOnly);
            Assert.True(property.SetMethod.IsInitOnly);
            IPropertySymbol publicProperty = property.GetPublicSymbol();
            Assert.False(publicProperty.GetMethod.IsInitOnly);
            Assert.True(publicProperty.SetMethod.IsInitOnly);
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
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

            var members = ((NamedTypeSymbol)comp.GlobalNamespace.GetMember("C")).GetMembers();
            AssertEx.SetEqual(members.ToTestDisplayStrings(),
                new[] {
                    "System.String C.Property { set; }",
                    "void C.Property.set",
                    "System.String C.Property2 { init; }",
                    "void modreq(System.Runtime.CompilerServices.IsExternalInit) C.Property2.init",
                    "System.String C.Property3 { init; }",
                    "void modreq(System.Runtime.CompilerServices.IsExternalInit) C.Property3.init",
                    "C..ctor()"
                });

            var property = (PropertySymbol)comp.GlobalNamespace.GetMember("C.Property");
            Assert.False(property.SetMethod.IsInitOnly);
            Assert.False(property.GetPublicSymbol().SetMethod.IsInitOnly);

            var property2 = (PropertySymbol)comp.GlobalNamespace.GetMember("C.Property2");
            Assert.True(property2.SetMethod.IsInitOnly);
            Assert.True(property2.GetPublicSymbol().SetMethod.IsInitOnly);

            var property3 = (PropertySymbol)comp.GlobalNamespace.GetMember("C.Property3");
            Assert.True(property3.SetMethod.IsInitOnly);
            Assert.True(property3.GetPublicSymbol().SetMethod.IsInitOnly);
        }

        [Fact]
        public void InThisOrBaseConstructorInitializer()
        {
            string source = @"
public class C
{
    public string Property { init { throw null; } }
    public C() : this(Property = null) // 1
    {
    }

    public C(string s)
    {
    }
}
public class Derived : C
{
    public Derived() : base(Property = null) // 2
    {
    }

    public Derived(int i) : base(base.Property = null) // 3
    {
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,23): error CS0120: An object reference is required for the non-static field, method, or property 'C.Property'
                //     public C() : this(Property = null) // 1
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Property").WithArguments("C.Property").WithLocation(5, 23),
                // (15,29): error CS0120: An object reference is required for the non-static field, method, or property 'C.Property'
                //     public Derived() : base(Property = null) // 2
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Property").WithArguments("C.Property").WithLocation(15, 29),
                // (19,34): error CS1512: Keyword 'base' is not available in the current context
                //     public Derived(int i) : base(base.Property = null) // 3
                Diagnostic(ErrorCode.ERR_BaseInBadContext, "base").WithLocation(19, 34)
                );
        }

        [Fact]
        public void TestWithAccessModifiers_Private()
        {
            string source = @"
public class C
{
    public string Property { get { throw null; } private init { throw null; } }
    void M()
    {
        _ = new C() { Property = null };
        Property = null; // 1
    }

    C()
    {
        Property = null;
    }
}
public class Other
{
    void M(C c)
    {
        _ = new C() { Property = null }; // 2, 3
        c.Property = null; // 4
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         Property = null; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(8, 9),
                // (20,17): error CS0122: 'C.C()' is inaccessible due to its protection level
                //         _ = new C() { Property = null }; // 2, 3
                Diagnostic(ErrorCode.ERR_BadAccess, "C").WithArguments("C.C()").WithLocation(20, 17),
                // (20,23): error CS0272: The property or indexer 'C.Property' cannot be used in this context because the set accessor is inaccessible
                //         _ = new C() { Property = null }; // 2, 3
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "Property").WithArguments("C.Property").WithLocation(20, 23),
                // (21,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         c.Property = null; // 4
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(21, 9)
                );
        }

        [Fact]
        public void TestWithAccessModifiers_Protected()
        {
            string source = @"
public class C
{
    public string Property { get { throw null; } protected init { throw null; } }
    void M()
    {
        _ = new C() { Property = null };
        Property = null; // 1
    }

    public C()
    {
        Property = null;
    }
}
public class Derived : C
{
    void M(C c)
    {
        _ = new C() { Property = null }; // 2
        c.Property = null; // 3, 4
        Property = null; // 5
    }

    Derived()
    {
        _ = new C() { Property = null }; // 6
        _ = new Derived() { Property = null };
        Property = null;
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         Property = null; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(8, 9),
                // (20,23): error CS1540: Cannot access protected member 'C.Property' via a qualifier of type 'C'; the qualifier must be of type 'Derived' (or derived from it)
                //         _ = new C() { Property = null }; // 2
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "Property").WithArguments("C.Property", "C", "Derived").WithLocation(20, 23),
                // (21,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         c.Property = null; // 3, 4
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(21, 9),
                // (22,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         Property = null; // 5
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(22, 9),
                // (27,23): error CS1540: Cannot access protected member 'C.Property' via a qualifier of type 'C'; the qualifier must be of type 'Derived' (or derived from it)
                //         _ = new C() { Property = null }; // 6
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "Property").WithArguments("C.Property", "C", "Derived").WithLocation(27, 23)
                );
        }

        [Fact]
        public void TestWithAccessModifiers_Protected_WithoutGetter()
        {
            string source = @"
public class C
{
    public string Property { protected init { throw null; } }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,19): error CS0276: 'C.Property': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                //     public string Property { protected init { throw null; } }
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "Property").WithArguments("C.Property").WithLocation(4, 19)
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (10,9): error CS8802: Init-only property or indexer 'C<string>.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         Property = null; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C<string>.Property").WithLocation(10, 9)
                );

            var property = (PropertySymbol)comp.GlobalNamespace.GetTypeMember("Derived").BaseTypeNoUseSiteDiagnostics.GetMember("Property");
            Assert.False(property.GetMethod.IsInitOnly);
            Assert.False(property.GetPublicSymbol().GetMethod.IsInitOnly);
            Assert.True(property.SetMethod.IsInitOnly);
            Assert.True(property.GetPublicSymbol().SetMethod.IsInitOnly);
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (10,29): error CS8804: 'CWithoutInit' does not implement interface member 'I<string>.Property.init'. 'CWithoutInit.Property.set' cannot implement 'I<string>.Property.init'.
                // public class CWithoutInit : I<string> // 1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I<string>").WithArguments("CWithoutInit", "I<string>.Property.init", "CWithoutInit.Property.set").WithLocation(10, 29)
                );

            var property = (PropertySymbol)comp.GlobalNamespace.GetMember("I.Property");
            Assert.False(property.GetMethod.IsInitOnly);
            Assert.False(property.GetPublicSymbol().GetMethod.IsInitOnly);
            Assert.True(property.SetMethod.IsInitOnly);
            Assert.True(property.GetPublicSymbol().SetMethod.IsInitOnly);
        }

        [Fact]
        public void InLambdaOrLocalFunction_InMethodOrDerivedConstructor()
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (12,13): error CS8802: Init-only property or indexer 'C<string>.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             Property = null; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C<string>.Property").WithLocation(12, 13),
                // (18,13): error CS8802: Init-only property or indexer 'C<string>.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             Property = null; // 2
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C<string>.Property").WithLocation(18, 13),
                // (26,13): error CS8802: Init-only property or indexer 'C<string>.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             Property = null; // 3
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C<string>.Property").WithLocation(26, 13),
                // (32,13): error CS8802: Init-only property or indexer 'C<string>.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             Property = null; // 4
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C<string>.Property").WithLocation(32, 13)
                );
        }

        [Fact]
        public void InLambdaOrLocalFunction_InConstructorOrInit()
        {
            string source = @"
public class C<T>
{
    public string Property { get; init; }

    C()
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

    public string Other
    {
        init
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
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (10,13): error CS8802: Init-only property or indexer 'C<T>.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             Property = null; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C<T>.Property").WithLocation(10, 13),
                // (16,13): error CS8802: Init-only property or indexer 'C<T>.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             Property = null; // 2
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C<T>.Property").WithLocation(16, 13),
                // (26,17): error CS8802: Init-only property or indexer 'C<T>.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //                 Property = null; // 3
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C<T>.Property").WithLocation(26, 17),
                // (32,17): error CS8802: Init-only property or indexer 'C<T>.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //                 Property = null; // 4
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C<T>.Property").WithLocation(32, 17)
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
                // (4,49): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
                //     public string Property { get => throw null; init { } }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "init").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(4, 49)
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         Property = null; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(8, 9),
                // (21,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             Property = null; // 2
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(21, 13),
                // (34,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             Property = null; // 3
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(34, 13),
                // (39,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             Property = null; // 4
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(39, 13),
                // (43,33): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C.Property'
                //     public string otherField = (Property = null); // 5
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "Property").WithArguments("C.Property").WithLocation(43, 33),
                // (54,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         Property = null; // 6
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(54, 9),
                // (66,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             Property = null; // 7
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(66, 13),
                // (79,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             Property = null; // 8
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(79, 13),
                // (84,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             Property = null; // 9
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(84, 13)
                );

            var property = (PropertySymbol)comp.GlobalNamespace.GetMember("C.Property");
            Assert.False(property.GetMethod.IsInitOnly);
            Assert.False(property.GetPublicSymbol().GetMethod.IsInitOnly);
            Assert.True(property.SetMethod.IsInitOnly);
            Assert.True(property.GetPublicSymbol().SetMethod.IsInitOnly);
        }

        [Fact]
        public void InitOnlyPropertyAssignmentAllowedInWithInitializer()
        {
            string source = @"
record C
{
    public int Property { get; init; }

    void M(C c)
    {
        _ = c with { Property = 1 };
    }

}

record Derived : C
{
}

record Derived2 : Derived
{
    void M(C c)
    {
        _ = c with { Property = 1 };
        _ = this with { Property = 1 };
    }
}

class Other
{
    void M()
    {
        var c = new C() with { Property = 42 };
        System.Console.Write($""{c.Property}"");
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void InitOnlyPropertyAssignmentAllowedInWithInitializer_Evaluation()
        {
            string source = @"
record C
{
    private int field;
    public int Property { get { return field; } init { field = value; System.Console.Write(""set ""); } }

    public C Clone() { System.Console.Write(""clone ""); return this; }
}

class Other
{
    public static void Main()
    {
        var c = new C() with { Property = 42 };
        System.Console.Write($""{c.Property}"");
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "clone set 42");
        }

        [Fact]
        public void EvaluationInitOnlySetter()
        {
            string source = @"
public class C
{
    public int Property
    {
        init { System.Console.Write(value + "" ""); }
    }

    public int Property2
    {
        init { System.Console.Write(value); }
    }

    C()
    {
        System.Console.Write(""Main "");
    }

    static void Main()
    {
        _ = new C() { Property = 42, Property2 = 43};
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Main 42 43");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EvaluationInitOnlySetter_OverrideAutoProp(bool emitImage)
        {
            string parent = @"
public class Base
{
    public virtual int Property { get; init; }
}";

            string source = @"
public class C : Base
{
    int field;
    public override int Property
    {
        get { System.Console.Write(""get:"" + field + "" ""); return field; }
        init { field = value; System.Console.Write(""set:"" + value + "" ""); }
    }

    public C()
    {
        System.Console.Write(""Main "");
    }
}";

            string main = @"
public class D
{
    static void Main()
    {
        var c = new C() { Property = 42 };
        _ = c.Property;
    }
}
";
            var libComp = CreateCompilation(new[] { parent, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            var comp = CreateCompilation(new[] { source, main }, references: new[] { emitImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() },
                parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Main set:42 get:42");

            libComp = CreateCompilation(new[] { parent, source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp = CreateCompilation(new[] { main }, references: new[] { emitImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() },
                parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Main set:42 get:42");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EvaluationInitOnlySetter_AutoProp(bool emitImage)
        {
            string source = @"
public class C
{
    public int Property { get; init; }

    public C()
    {
        System.Console.Write(""Main "");
    }
}";
            string main = @"
public class D
{
    static void Main()
    {
        var c = new C() { Property = 42 };
        System.Console.Write($""{c.Property}"");
    }
}
";
            var libComp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            var comp = CreateCompilation(new[] { main }, references: new[] { emitImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() },
                parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Main 42");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EvaluationInitOnlySetter_Implementation(bool emitImage)
        {
            string parent = @"
public interface I
{
    int Property { get; init; }
}";

            string source = @"
public class C : I
{
    int field;
    public int Property
    {
        get { System.Console.Write(""get:"" + field + "" ""); return field; }
        init { field = value; System.Console.Write(""set:"" + value + "" ""); }
    }

    public C()
    {
        System.Console.Write(""Main "");
    }
}";

            string main = @"
public class D
{
    static void Main()
    {
        M<C>();
    }

    static void M<T>() where T : I, new()
    {
        var t = new T() { Property = 42 };
        _ = t.Property;
    }
}
";
            var libComp = CreateCompilation(new[] { parent, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            var comp = CreateCompilation(new[] { source, main }, references: new[] { emitImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() },
                parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Main set:42 get:42");

            libComp = CreateCompilation(new[] { parent, source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp = CreateCompilation(new[] { main }, references: new[] { emitImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() },
                parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Main set:42 get:42");
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,42): error CS8806: The 'init' accessor is not valid on static members
                //     public static string Property { get; init; }
                Diagnostic(ErrorCode.ERR_BadInitAccessor, "init").WithLocation(4, 42)
                );

            var property = (PropertySymbol)comp.GlobalNamespace.GetMember("C.Property");
            Assert.False(property.GetMethod.IsInitOnly);
            Assert.False(property.GetPublicSymbol().GetMethod.IsInitOnly);
            Assert.False(property.SetMethod.IsInitOnly);
            Assert.False(property.GetPublicSymbol().SetMethod.IsInitOnly);
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

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (9,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         c.Property = null; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(9, 9),
                // (16,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             c.Property = null; // 2
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(16, 13),
                // (24,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         c.Property = null; // 3
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(24, 9),
                // (31,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             c.Property = null; // 4
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(31, 13),
                // (41,18): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,10): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         (Property, (Property, Property)) = (null, (null, null)); // 1, 2, 3
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(8, 10),
                // (8,21): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         (Property, (Property, Property)) = (null, (null, null)); // 1, 2, 3
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(8, 21),
                // (8,31): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
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
    ref int Property2 { init; }
    ref int Property3 { get => throw null; init => throw null; }
    ref int Property4 { init => throw null; }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,13): error CS8145: Auto-implemented properties cannot return by reference
                //     ref int Property1 { get; init; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "Property1").WithArguments("C.Property1").WithLocation(4, 13),
                // (4,30): error CS8147: Properties which return by reference cannot have set accessors
                //     ref int Property1 { get; init; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "init").WithArguments("C.Property1.init").WithLocation(4, 30),
                // (5,13): error CS8146: Properties which return by reference must have a get accessor
                //     ref int Property2 { init; }
                Diagnostic(ErrorCode.ERR_RefPropertyMustHaveGetAccessor, "Property2").WithArguments("C.Property2").WithLocation(5, 13),
                // (6,44): error CS8147: Properties which return by reference cannot have set accessors
                //     ref int Property3 { get => throw null; init => throw null; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "init").WithArguments("C.Property3.init").WithLocation(6, 44),
                // (7,13): error CS8146: Properties which return by reference must have a get accessor
                //     ref int Property4 { init => throw null; }
                Diagnostic(ErrorCode.ERR_RefPropertyMustHaveGetAccessor, "Property4").WithArguments("C.Property4").WithLocation(7, 13)
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition },
                parseOptions: TestOptions.RegularPreview,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            comp.VerifyDiagnostics();

            // PE verification fails:  [ : C::set_Property] Cannot change initonly field outside its .ctor.
            CompileAndVerify(comp, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator,
                verify: ExecutionConditionUtil.IsCoreClr ? Verification.Passes : Verification.Fails);

            void symbolValidator(ModuleSymbol m)
            {
                bool isSource = !(m is PEModuleSymbol);
                var c = (NamedTypeSymbol)m.GlobalNamespace.GetMember("C");

                var property = (PropertySymbol)c.GetMembers("Property").Single();
                Assert.Equal("System.String C.Property { get; init; }", property.ToTestDisplayString());
                Assert.Equal(0, property.CustomModifierCount());
                var propertyAttributes = property.GetAttributes().Select(a => a.ToString());
                AssertEx.Empty(propertyAttributes);

                var getter = property.GetMethod;
                Assert.Empty(property.GetMethod.ReturnTypeWithAnnotations.CustomModifiers);
                Assert.False(getter.IsInitOnly);
                Assert.False(getter.GetPublicSymbol().IsInitOnly);
                var getterAttributes = getter.GetAttributes().Select(a => a.ToString());
                if (isSource)
                {
                    AssertEx.Empty(getterAttributes);
                }
                else
                {
                    AssertEx.Equal(new[] { "System.Runtime.CompilerServices.CompilerGeneratedAttribute" }, getterAttributes);
                }

                var setter = property.SetMethod;
                Assert.True(setter.IsInitOnly);
                Assert.True(setter.GetPublicSymbol().IsInitOnly);
                var setterAttributes = property.SetMethod.GetAttributes().Select(a => a.ToString());
                var modifier = property.SetMethod.ReturnTypeWithAnnotations.CustomModifiers.Single();
                Assert.Equal("System.Runtime.CompilerServices.IsExternalInit", modifier.Modifier.ToTestDisplayString());
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
            var libComp = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
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
        base.Property = null; // 8
    }

    Derived2()
    {
        Property = null;
        base.Property = null;
    }

    public string InitOnlyProperty2
    {
        get
        {
            Property = null; // 9
            base.Property = null; // 10
            return null;
        }
        init
        {
            Property = null;
            base.Property = null;
        }
    }

    public string RegularProperty2
    {
        get
        {
            Property = null; // 11
            base.Property = null; // 12
            return null;
        }
        set
        {
            Property = null; // 13
            base.Property = null; // 14
        }
    }
}
";
            var comp = CreateCompilation(source,
                references: new[] { emitImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() },
                parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics(
                // (8,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         c.Property = null; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(8, 9),
                // (13,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         c.Property = null; // 2
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(13, 9),
                // (20,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             c.Property = null; // 3
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(20, 13),
                // (25,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             c.Property = null; // 4
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(25, 13),
                // (33,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             c.Property = null; // 5
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(33, 13),
                // (38,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             c.Property = null; // 6
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(38, 13),
                // (51,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         Property = null; // 7
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(51, 9),
                // (52,9): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         base.Property = null; // 8
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "base.Property").WithArguments("C.Property").WithLocation(52, 9),
                // (65,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             Property = null; // 9
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(65, 13),
                // (66,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             base.Property = null; // 10
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "base.Property").WithArguments("C.Property").WithLocation(66, 13),
                // (80,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             Property = null; // 11
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(80, 13),
                // (81,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             base.Property = null; // 12
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "base.Property").WithArguments("C.Property").WithLocation(81, 13),
                // (86,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             Property = null; // 13
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(86, 13),
                // (87,13): error CS8802: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             base.Property = null; // 14
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "base.Property").WithArguments("C.Property").WithLocation(87, 13)
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

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
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
            var libComp = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
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
public class DerivedDerivedWithInit : DerivedGetterOnly
{
    public override string Property { init { } } // 3
}
public class DerivedDerivedWithoutInit : DerivedGetterOnly
{
    public override string Property { set { } }
}
";

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (9,28): error CS8803: 'DerivedWithInit.Property' must match by init-only of overridden member 'Base.Property'
                //     public override string Property { get; init; } // 1
                Diagnostic(ErrorCode.ERR_CantChangeInitOnlyOnOverride, "Property").WithArguments("DerivedWithInit.Property", "Base.Property").WithLocation(9, 28),
                // (18,28): error CS8803: 'DerivedWithInitSetterOnly.Property' must match by init-only of overridden member 'Base.Property'
                //     public override string Property { init { } } // 2
                Diagnostic(ErrorCode.ERR_CantChangeInitOnlyOnOverride, "Property").WithArguments("DerivedWithInitSetterOnly.Property", "Base.Property").WithLocation(18, 28),
                // (31,28): error CS8803: 'DerivedDerivedWithInit.Property' must match by init-only of overridden member 'DerivedGetterOnly.Property'
                //     public override string Property { init { } } // 3
                Diagnostic(ErrorCode.ERR_CantChangeInitOnlyOnOverride, "Property").WithArguments("DerivedDerivedWithInit.Property", "DerivedGetterOnly.Property").WithLocation(31, 28)
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

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,44): error CS0546: 'DerivedWithInit.Property.init': cannot override because 'Base.Property' does not have an overridable set accessor
                //     public override string Property { get; init; } // 1
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "init").WithArguments("DerivedWithInit.Property.init", "Base.Property").WithLocation(8, 44),
                // (12,44): error CS0546: 'DerivedWithoutInit.Property.set': cannot override because 'Base.Property' does not have an overridable set accessor
                //     public override string Property { get; set; } // 2
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("DerivedWithoutInit.Property.set", "Base.Property").WithLocation(12, 44),
                // (16,39): error CS0546: 'DerivedWithInitSetterOnly.Property.init': cannot override because 'Base.Property' does not have an overridable set accessor
                //     public override string Property { init { } } // 3
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "init").WithArguments("DerivedWithInitSetterOnly.Property.init", "Base.Property").WithLocation(16, 39),
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

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
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

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (10,35): error CS8804: 'DerivedWithoutInit' does not implement interface member 'I.Property.init'. 'DerivedWithoutInit.Property.set' cannot implement 'I.Property.init'.
                // public class DerivedWithoutInit : I // 1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I").WithArguments("DerivedWithoutInit", "I.Property.init", "DerivedWithoutInit.Property.set").WithLocation(10, 35),
                // (14,42): error CS0535: 'DerivedWithInitSetterOnly' does not implement interface member 'I.Property.get'
                // public class DerivedWithInitSetterOnly : I // 2
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I").WithArguments("DerivedWithInitSetterOnly", "I.Property.get").WithLocation(14, 42),
                // (18,45): error CS0535: 'DerivedWithoutInitGetterOnly' does not implement interface member 'I.Property.init'
                // public class DerivedWithoutInitGetterOnly : I // 3
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I").WithArguments("DerivedWithoutInitGetterOnly", "I.Property.init").WithLocation(18, 45)
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,32): error CS8804: 'DerivedWithInit' does not implement interface member 'I.Property.set'. 'DerivedWithInit.Property.init' cannot implement 'I.Property.set'.
                // public class DerivedWithInit : I // 1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I").WithArguments("DerivedWithInit", "I.Property.set", "DerivedWithInit.Property.init").WithLocation(6, 32),
                // (14,42): error CS8804: 'DerivedWithInitSetterOnly' does not implement interface member 'I.Property.set'. 'DerivedWithInitSetterOnly.Property.init' cannot implement 'I.Property.set'.
                // public class DerivedWithInitSetterOnly : I // 2
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I").WithArguments("DerivedWithInitSetterOnly", "I.Property.set", "DerivedWithInitSetterOnly.Property.init").WithLocation(14, 42),
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
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
            var libComp = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
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
                // (2,32): error CS8804: 'DerivedWithInit' does not implement interface member 'I.Property.set'. 'DerivedWithInit.Property.init' cannot implement 'I.Property.set'.
                // public class DerivedWithInit : I // 1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I").WithArguments("DerivedWithInit", "I.Property.set", "DerivedWithInit.Property.init").WithLocation(2, 32),
                // (10,42): error CS8804: 'DerivedWithInitSetterOnly' does not implement interface member 'I.Property.set'. 'DerivedWithInitSetterOnly.Property.init' cannot implement 'I.Property.set'.
                // public class DerivedWithInitSetterOnly : I // 2
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I").WithArguments("DerivedWithInitSetterOnly", "I.Property.set", "DerivedWithInitSetterOnly.Property.init").WithLocation(10, 42),
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,25): error CS8805: Accessors 'DerivedWithInit.I.Property.init' and 'I.Property.set' should both be init-only or neither
                //     string I.Property { init { } } // 1
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMismatchInitOnly, "init").WithArguments("DerivedWithInit.I.Property.init", "I.Property.set").WithLocation(8, 25),
                // (12,25): error CS0550: 'DerivedWithInitAndGetter.I.Property.get' adds an accessor not found in interface member 'I.Property'
                //     string I.Property { get; init; } // 2, 3
                Diagnostic(ErrorCode.ERR_ExplicitPropertyAddingAccessor, "get").WithArguments("DerivedWithInitAndGetter.I.Property.get", "I.Property").WithLocation(12, 25),
                // (12,30): error CS8805: Accessors 'DerivedWithInitAndGetter.I.Property.init' and 'I.Property.set' should both be init-only or neither
                //     string I.Property { get; init; } // 2, 3
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMismatchInitOnly, "init").WithArguments("DerivedWithInitAndGetter.I.Property.init", "I.Property.set").WithLocation(12, 30)
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,25): error CS8805: Accessors 'DerivedWithoutInit.I.Property.set' and 'I.Property.init' should both be init-only or neither
                //     string I.Property { set { } } // 1
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMismatchInitOnly, "set").WithArguments("DerivedWithoutInit.I.Property.set", "I.Property.init").WithLocation(8, 25),
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
            var libComp = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
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
                // (4,25): error CS8805: Accessors 'DerivedWithoutInit.I.Property.set' and 'I.Property.init' should both be init-only or neither
                //     string I.Property { set { } } // 1
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMismatchInitOnly, "set").WithArguments("DerivedWithoutInit.I.Property.set", "I.Property.init").WithLocation(4, 25),
                // (10,34): error CS0535: 'DerivedGetterOnly' does not implement interface member 'I.Property.init'
                // public class DerivedGetterOnly : I // 2
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I").WithArguments("DerivedGetterOnly", "I.Property.init").WithLocation(10, 34),
                // (12,14): error CS0551: Explicit interface implementation 'DerivedGetterOnly.I.Property' is missing accessor 'I.Property.init'
                //     string I.Property { get => null; } // 3, 4
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMissingAccessor, "Property").WithArguments("DerivedGetterOnly.I.Property", "I.Property.init").WithLocation(12, 14),
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

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition },
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

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition },
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
                // (40,26): error CS8805: Accessors 'IWithInitWithExplicitImplementationOfI2.I2.Property.init' and 'I2.Property.set' should both be init-only or neither
                //     string I2.Property { init { } } // 4
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMismatchInitOnly, "init").WithArguments("IWithInitWithExplicitImplementationOfI2.I2.Property.init", "I2.Property.set").WithLocation(40, 26),
                // (45,26): error CS8805: Accessors 'IWithoutInitWithExplicitImplementationOfI1.I1.Property.set' and 'I1.Property.init' should both be init-only or neither
                //     string I1.Property { set { } } // 5
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMismatchInitOnly, "set").WithArguments("IWithoutInitWithExplicitImplementationOfI1.I1.Property.set", "I1.Property.init").WithLocation(45, 26),
                // (62,52): error CS8804: 'CWithImplementationWithInitOnly' does not implement interface member 'I2.Property.set'. 'CWithImplementationWithInitOnly.Property.init' cannot implement 'I2.Property.set'.
                // public class CWithImplementationWithInitOnly : I1, I2 // 6
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I2").WithArguments("CWithImplementationWithInitOnly", "I2.Property.set", "CWithImplementationWithInitOnly.Property.init").WithLocation(62, 52),
                // (66,51): error CS8804: 'CWithImplementationWithoutInitOnly' does not implement interface member 'I1.Property.init'. 'CWithImplementationWithoutInitOnly.Property.set' cannot implement 'I1.Property.init'.
                // public class CWithImplementationWithoutInitOnly : I1, I2 // 7
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I1").WithArguments("CWithImplementationWithoutInitOnly", "I1.Property.init", "CWithImplementationWithoutInitOnly.Property.set").WithLocation(66, 51)
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,32): error CS0065: 'C.Event': event property must have both add and remove accessors
                //     public event System.Action Event
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "Event").WithArguments("C.Event").WithLocation(4, 32),
                // (6,9): error CS1055: An add or remove accessor expected
                //         init { }
                Diagnostic(ErrorCode.ERR_AddOrRemoveExpected, "init").WithLocation(6, 9)
                );

            var members = ((NamedTypeSymbol)comp.GlobalNamespace.GetMember("C")).GetMembers();
            AssertEx.SetEqual(members.ToTestDisplayStrings(), new[] {
                "event System.Action C.Event",
                "C..ctor()"
            });
        }

        [Fact]
        public void EventAccessorsAreNotInitOnly()
        {
            string source = @"
public class C
{
    public event System.Action Event
    {
        add { }
        remove { }
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var eventSymbol = comp.GlobalNamespace.GetMember<EventSymbol>("C.Event");
            Assert.False(eventSymbol.AddMethod.IsInitOnly);
            Assert.False(eventSymbol.GetPublicSymbol().AddMethod.IsInitOnly);
            Assert.False(eventSymbol.RemoveMethod.IsInitOnly);
            Assert.False(eventSymbol.GetPublicSymbol().RemoveMethod.IsInitOnly);
        }

        [Fact]
        public void ConstructorAndDestructorAreNotInitOnly()
        {
            string source = @"
public class C
{
    public C() { }
    ~C() { }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var constructor = comp.GlobalNamespace.GetMember<SourceConstructorSymbol>("C..ctor");
            Assert.False(constructor.IsInitOnly);
            Assert.False(constructor.GetPublicSymbol().IsInitOnly);

            var destructor = comp.GlobalNamespace.GetMember<SourceDestructorSymbol>("C.Finalize");
            Assert.False(destructor.IsInitOnly);
            Assert.False(destructor.GetPublicSymbol().IsInitOnly);
        }

        [Fact]
        public void OperatorsAreNotInitOnly()
        {
            string source = @"
public class C
{
    public static implicit operator int(C c) => throw null;
    public static bool operator +(C c1, C c2) => throw null;
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var conversion = comp.GlobalNamespace.GetMember<SourceUserDefinedConversionSymbol>("C.op_Implicit");
            Assert.False(conversion.IsInitOnly);
            Assert.False(conversion.GetPublicSymbol().IsInitOnly);

            var addition = comp.GlobalNamespace.GetMember<SourceUserDefinedOperatorSymbol>("C.op_Addition");
            Assert.False(addition.IsInitOnly);
            Assert.False(addition.GetPublicSymbol().IsInitOnly);
        }

        [Fact]
        public void ConstructedMethodsAreNotInitOnly()
        {
            string source = @"
public class C
{
    void M<T>()
    {
        M<string>();
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: true);

            var invocation = root.DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            var method = (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol;
            Assert.Equal("void C.M<System.String>()", method.ToTestDisplayString());
            Assert.False(method.IsInitOnly);
        }

        [Fact]
        public void InitOnlyOnMembersOfRecords()
        {
            string source = @"
public record C(int i)
{
    void M() { }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var cMembers = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers();
            AssertEx.SetEqual(new[] {
                "C C." + WellKnownMemberNames.CloneMethodName + "()",
                "System.Type C.EqualityContract.get",
                "System.Type C.EqualityContract { get; }",
                "C..ctor(System.Int32 i)",
                "System.Int32 C.<i>k__BackingField",
                "System.Int32 C.i.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) C.i.init",
                "System.Int32 C.i { get; init; }",
                "void C.M()",
                "System.Int32 C.GetHashCode()",
                "System.Boolean C.Equals(System.Object? obj)",
                "System.Boolean C.Equals(C? other)",
                "C..ctor(C original)",
                "void C.Deconstruct(out System.Int32 i)",
                }, cMembers.ToTestDisplayStrings());

            foreach (var member in cMembers)
            {
                if (member is MethodSymbol method)
                {
                    bool isSetter = method.MethodKind == MethodKind.PropertySet;
                    Assert.Equal(isSetter, method.IsInitOnly);
                    Assert.Equal(isSetter, method.GetPublicSymbol().IsInitOnly);
                }
            }
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (14,9): error CS8802: Init-only property or indexer 'C.this[int]' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         this[43] = null; // 1
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "this[43]").WithArguments("C.this[int]").WithLocation(14, 9),
                // (25,9): error CS8802: Init-only property or indexer 'C.this[int]' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         this[45] = null; // 2
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "this[45]").WithArguments("C.this[int]").WithLocation(25, 9),
                // (33,9): error CS8802: Init-only property or indexer 'C.this[int]' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
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
        public void ReadonlyFields_Evaluation()
        {
            string source = @"
public class C
{
    public readonly int field;
    public static void Main()
    {
        var c1 = new C();
        System.Console.Write($""{c1.field} "");

        var c2 = new C() { Property = 43 };
        System.Console.Write($""{c2.field}"");
    }

    public C()
    {
        field = 42;
    }

    public int Property
    {
        init
        {
            field = value;
        }
    }
}
";

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition },
                options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            // [ : C::set_Property] Cannot change initonly field outside its .ctor.
            CompileAndVerify(comp, expectedOutput: "42 43",
                verify: ExecutionConditionUtil.IsCoreClr ? Verification.Passes : Verification.Fails);
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
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

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
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
            var libComp = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);

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
", IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);

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
        public void ModReqOnSetAccessorParameter()
        {
            string il = @"
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig specialname newslot virtual instance void set_Property ( int32 modreq(System.Runtime.CompilerServices.IsExternalInit) 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance int32 Property()
    {
        .set instance void C::set_Property(int32 modreq(System.Runtime.CompilerServices.IsExternalInit))
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";

            string source = @"
public class Derived : C
{
    public override int Property { set { throw null; } }
}
public class Derived2 : C
{
    public override int Property { init { throw null; } }
}
public class D
{
    void M(C c)
    {
        c.Property = 42;
        c.set_Property(42);
    }
}
";

            var reference = CreateMetadataReferenceFromIlSource(il);
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,36): error CS0570: 'C.Property.set' is not supported by the language
                //     public override int Property { set { throw null; } }
                Diagnostic(ErrorCode.ERR_BindToBogus, "set").WithArguments("C.Property.set").WithLocation(4, 36),
                // (8,25): error CS8853: 'Derived2.Property' must match by init-only of overridden member 'C.Property'
                //     public override int Property { init { throw null; } }
                Diagnostic(ErrorCode.ERR_CantChangeInitOnlyOnOverride, "Property").WithArguments("Derived2.Property", "C.Property").WithLocation(8, 25),
                // (8,36): error CS0570: 'C.Property.set' is not supported by the language
                //     public override int Property { init { throw null; } }
                Diagnostic(ErrorCode.ERR_BindToBogus, "init").WithArguments("C.Property.set").WithLocation(8, 36),
                // (14,11): error CS0570: 'C.Property.set' is not supported by the language
                //         c.Property = 42;
                Diagnostic(ErrorCode.ERR_BindToBogus, "Property").WithArguments("C.Property.set").WithLocation(14, 11),
                // (15,11): error CS0571: 'C.Property.set': cannot explicitly call operator or accessor
                //         c.set_Property(42);
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "set_Property").WithArguments("C.Property.set").WithLocation(15, 11)
                );

            var property0 = (PEPropertySymbol)comp.GlobalNamespace.GetMember("C.Property");
            Assert.Null(property0.GetMethod);
            Assert.False(property0.MustCallMethodsDirectly);
            Assert.True(property0.SetMethod.HasUseSiteError);
            Assert.True(property0.SetMethod.HasUnsupportedMetadata);
            Assert.True(property0.SetMethod.Parameters[0].HasUnsupportedMetadata);

            var property1 = (PropertySymbol)comp.GlobalNamespace.GetMember("Derived.Property");
            Assert.Null(property1.GetMethod);
            Assert.False(property1.SetMethod.HasUseSiteError);
            Assert.False(property1.SetMethod.Parameters[0].Type.IsErrorType());

            var property2 = (PropertySymbol)comp.GlobalNamespace.GetMember("Derived2.Property");
            Assert.Null(property2.GetMethod);
            Assert.False(property2.SetMethod.HasUseSiteError);
            Assert.False(property2.SetMethod.Parameters[0].Type.IsErrorType());
        }

        [Fact]
        public void ModReqOnSetAccessorParameter_AndProperty()
        {
            string il = @"
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig specialname newslot virtual instance void set_Property ( int32 modreq(System.Runtime.CompilerServices.IsExternalInit) 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance int32 modreq(System.Runtime.CompilerServices.IsExternalInit) Property()
    {
        .set instance void C::set_Property(int32 modreq(System.Runtime.CompilerServices.IsExternalInit))
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";

            string source = @"
public class Derived : C
{
    public override int Property { set { throw null; } }
}
public class Derived2 : C
{
    public override int Property { init { throw null; } }
}
public class D
{
    void M(C c)
    {
        c.Property = 42;
        c.set_Property(42);
    }
}
";

            var reference = CreateMetadataReferenceFromIlSource(il);
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,25): error CS0569: 'Derived.Property': cannot override 'C.Property' because it is not supported by the language
                //     public override int Property { set { throw null; } }
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "Property").WithArguments("Derived.Property", "C.Property").WithLocation(4, 25),
                // (8,25): error CS0569: 'Derived2.Property': cannot override 'C.Property' because it is not supported by the language
                //     public override int Property { init { throw null; } }
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "Property").WithArguments("Derived2.Property", "C.Property").WithLocation(8, 25),
                // (14,11): error CS1546: Property, indexer, or event 'C.Property' is not supported by the language; try directly calling accessor method 'C.set_Property(int)'
                //         c.Property = 42;
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "Property").WithArguments("C.Property", "C.set_Property(int)").WithLocation(14, 11),
                // (15,11): error CS0570: 'C.set_Property(int)' is not supported by the language
                //         c.set_Property(42);
                Diagnostic(ErrorCode.ERR_BindToBogus, "set_Property").WithArguments("C.set_Property(int)").WithLocation(15, 11)
                );

            var property0 = (PEPropertySymbol)comp.GlobalNamespace.GetMember("C.Property");
            Assert.True(property0.HasUseSiteError);
            Assert.True(property0.HasUnsupportedMetadata);
            Assert.True(property0.MustCallMethodsDirectly);
            Assert.Equal("System.Int32", property0.Type.ToTestDisplayString());
            Assert.Null(property0.GetMethod);
            Assert.True(property0.SetMethod.HasUseSiteError);
            Assert.True(property0.SetMethod.HasUnsupportedMetadata);
            Assert.True(property0.SetMethod.Parameters[0].HasUnsupportedMetadata);

            var property1 = (PropertySymbol)comp.GlobalNamespace.GetMember("Derived.Property");
            Assert.False(property1.HasUseSiteError);
            Assert.Null(property1.GetMethod);
            Assert.False(property1.SetMethod.HasUseSiteError);
            Assert.False(property1.SetMethod.Parameters[0].Type.IsErrorType());

            var property2 = (PropertySymbol)comp.GlobalNamespace.GetMember("Derived2.Property");
            Assert.False(property2.HasUseSiteError);
            Assert.Null(property2.GetMethod);
            Assert.False(property2.SetMethod.HasUseSiteError);
            Assert.False(property2.SetMethod.Parameters[0].Type.IsErrorType());
        }

        [Fact]
        public void ModReqOnSetAccessorParameter_IndexerParameter()
        {
            string il = @"
.class public auto ansi beforefieldinit C extends System.Object
{
    .custom instance void System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6d 00 00 )
    .method public hidebysig specialname newslot virtual instance void set_Item ( int32 modreq(System.Runtime.CompilerServices.IsExternalInit)  i, int32 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance int32 Item(int32 modreq(System.Runtime.CompilerServices.IsExternalInit) i)
    {
        .set instance void C::set_Item(int32 modreq(System.Runtime.CompilerServices.IsExternalInit), int32)
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}

.class public auto ansi sealed beforefieldinit System.Reflection.DefaultMemberAttribute extends System.Attribute
{
    .method public hidebysig specialname rtspecialname instance void .ctor ( string memberName ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}

.class public auto ansi sealed beforefieldinit System.Attribute extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";

            string source = @"
public class Derived : C
{
    public override int this[int i] { set { throw null; } }
}
public class Derived2 : C
{
    public override int this[int i] { init { throw null; } }
}
public class D
{
    void M(C c)
    {
        c[42] = 43;
        c.set_Item(42, 43);
    }
}
";

            var reference = CreateMetadataReferenceFromIlSource(il);
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,25): error CS0569: 'Derived.this[int]': cannot override 'C.this[int]' because it is not supported by the language
                //     public override int this[int i] { set { throw null; } }
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "this").WithArguments("Derived.this[int]", "C.this[int]").WithLocation(4, 25),
                // (8,25): error CS0569: 'Derived2.this[int]': cannot override 'C.this[int]' because it is not supported by the language
                //     public override int this[int i] { init { throw null; } }
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "this").WithArguments("Derived2.this[int]", "C.this[int]").WithLocation(8, 25),
                // (14,9): error CS1546: Property, indexer, or event 'C.this[int]' is not supported by the language; try directly calling accessor method 'C.set_Item(int, int)'
                //         c[42] = 43;
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "c[42]").WithArguments("C.this[int]", "C.set_Item(int, int)").WithLocation(14, 9),
                // (15,11): error CS0570: 'C.set_Item(int, int)' is not supported by the language
                //         c.set_Item(42, 43);
                Diagnostic(ErrorCode.ERR_BindToBogus, "set_Item").WithArguments("C.set_Item(int, int)").WithLocation(15, 11)
                );

            var property0 = (PEPropertySymbol)comp.GlobalNamespace.GetMember("C.this[]");
            Assert.True(property0.HasUseSiteError);
            Assert.True(property0.MustCallMethodsDirectly);
            Assert.Null(property0.GetMethod);
            Assert.True(property0.SetMethod.HasUseSiteError);
            Assert.True(property0.SetMethod.HasUnsupportedMetadata);
            Assert.True(property0.SetMethod.Parameters[0].HasUnsupportedMetadata);
        }

        [Fact]
        public void ModReqOnIndexerValue()
        {
            string il = @"
.class public auto ansi beforefieldinit C extends System.Object
{
    .custom instance void System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6d 00 00 )
    .method public hidebysig specialname newslot virtual instance void set_Item ( int32 i, int32 modreq(System.Runtime.CompilerServices.IsExternalInit) 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance int32 Item(int32 i)
    {
        .set instance void C::set_Item(int32, int32 modreq(System.Runtime.CompilerServices.IsExternalInit))
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}

.class public auto ansi sealed beforefieldinit System.Reflection.DefaultMemberAttribute extends System.Attribute
{
    .method public hidebysig specialname rtspecialname instance void .ctor ( string memberName ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}

.class public auto ansi sealed beforefieldinit System.Attribute extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";

            string source = @"
public class Derived : C
{
    public override int this[int i] { set { throw null; } }
}
public class Derived2 : C
{
    public override int this[int i] { init { throw null; } }
}
";

            var reference = CreateMetadataReferenceFromIlSource(il);
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,39): error CS0570: 'C.this[int].set' is not supported by the language
                //     public override int this[int i] { set { throw null; } }
                Diagnostic(ErrorCode.ERR_BindToBogus, "set").WithArguments("C.this[int].set").WithLocation(4, 39),
                // (8,25): error CS8853: 'Derived2.this[int]' must match by init-only of overridden member 'C.this[int]'
                //     public override int this[int i] { init { throw null; } }
                Diagnostic(ErrorCode.ERR_CantChangeInitOnlyOnOverride, "this").WithArguments("Derived2.this[int]", "C.this[int]").WithLocation(8, 25),
                // (8,39): error CS0570: 'C.this[int].set' is not supported by the language
                //     public override int this[int i] { init { throw null; } }
                Diagnostic(ErrorCode.ERR_BindToBogus, "init").WithArguments("C.this[int].set").WithLocation(8, 39)
                );

            var property0 = (PEPropertySymbol)comp.GlobalNamespace.GetMember("C.this[]");
            Assert.False(property0.HasUseSiteError);
            Assert.False(property0.MustCallMethodsDirectly);
            Assert.Null(property0.GetMethod);
            Assert.True(property0.SetMethod.HasUseSiteError);
            Assert.True(property0.SetMethod.HasUnsupportedMetadata);
            Assert.True(property0.SetMethod.Parameters[1].HasUnsupportedMetadata);
        }

        [Fact]
        public void ModReqOnStaticMethod()
        {
            string il = @"
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig static void modreq(System.Runtime.CompilerServices.IsExternalInit) M () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";
            string source = @"
public class D
{
    void M2()
    {
        C.M();
    }
}
";

            var reference = CreateMetadataReferenceFromIlSource(il);
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,11): error CS0570: 'C.M()' is not supported by the language
                //         C.M();
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("C.M()").WithLocation(6, 11)
                );

            var method = (PEMethodSymbol)comp.GlobalNamespace.GetMember("C.M");
            Assert.False(method.IsInitOnly);
            Assert.False(method.GetPublicSymbol().IsInitOnly);
            Assert.True(method.HasUseSiteError);
            Assert.True(method.HasUnsupportedMetadata);
        }

        [Fact]
        public void ModReqOnMethodParameter()
        {
            string il = @"
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig newslot virtual instance void M ( int32 modreq(System.Runtime.CompilerServices.IsExternalInit) i ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";
            string source = @"
public class Derived : C
{
    public override void M() { }
}
";

            var reference = CreateMetadataReferenceFromIlSource(il);
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,26): error CS0115: 'Derived.M()': no suitable method found to override
                //     public override void M() { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M").WithArguments("Derived.M()").WithLocation(4, 26)
                );

            var method0 = (PEMethodSymbol)comp.GlobalNamespace.GetMember("C.M");
            Assert.True(method0.HasUseSiteError);
            Assert.True(method0.HasUnsupportedMetadata);
            Assert.True(method0.Parameters[0].HasUnsupportedMetadata);
        }

        [Fact]
        public void ModReqOnInitOnlySetterOfRefProperty()
        {
            string il = @"
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig specialname newslot virtual instance int32& get_Property () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname newslot virtual instance void set_Property ( int32& modreq(System.Runtime.CompilerServices.IsExternalInit) 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance int32& Property()
    {
        .get instance int32& C::get_Property()
        .set instance void C::set_Property(int32& modreq(System.Runtime.CompilerServices.IsExternalInit))
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";
            string source = @"
public class D
{
    void M(C c, ref int i)
    {
        _ = c.get_Property();
        c.set_Property(i); // 1

        _ = c.Property; // 2
        c.Property = i; // 3
    }
}
";

            var reference = CreateMetadataReferenceFromIlSource(il);
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (7,11): error CS0570: 'C.set_Property(ref int)' is not supported by the language
                //         c.set_Property(i); // 1
                Diagnostic(ErrorCode.ERR_BindToBogus, "set_Property").WithArguments("C.set_Property(ref int)").WithLocation(7, 11),
                // (9,15): error CS1545: Property, indexer, or event 'C.Property' is not supported by the language; try directly calling accessor methods 'C.get_Property()' or 'C.set_Property(ref int)'
                //         _ = c.Property; // 2
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Property").WithArguments("C.Property", "C.get_Property()", "C.set_Property(ref int)").WithLocation(9, 15),
                // (10,11): error CS1545: Property, indexer, or event 'C.Property' is not supported by the language; try directly calling accessor methods 'C.get_Property()' or 'C.set_Property(ref int)'
                //         c.Property = i; // 3
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Property").WithArguments("C.Property", "C.get_Property()", "C.set_Property(ref int)").WithLocation(10, 11)
                );

            var property0 = (PEPropertySymbol)comp.GlobalNamespace.GetMember("C.Property");
            Assert.False(property0.HasUseSiteError);
            Assert.True(property0.MustCallMethodsDirectly);
            Assert.False(property0.GetMethod.HasUseSiteError);
            Assert.True(property0.SetMethod.HasUseSiteError);
            Assert.True(property0.SetMethod.Parameters[0].HasUnsupportedMetadata);
            Assert.False(property0.SetMethod.IsInitOnly);
            Assert.False(property0.GetPublicSymbol().SetMethod.IsInitOnly);
        }

        [Fact]
        public void ModReqOnRefProperty_OnRefReturn()
        {
            string il = @"
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig specialname newslot virtual instance int32& modreq(System.Runtime.CompilerServices.IsExternalInit) get_Property () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname newslot virtual instance void set_Property ( int32& modreq(System.Runtime.CompilerServices.IsExternalInit) 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance int32& modreq(System.Runtime.CompilerServices.IsExternalInit) Property()
    {
        .get instance int32& modreq(System.Runtime.CompilerServices.IsExternalInit) C::get_Property()
        .set instance void C::set_Property(int32& modreq(System.Runtime.CompilerServices.IsExternalInit))
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";
            string source = @"
public class D
{
    void M(C c, ref int i)
    {
        _ = c.get_Property(); // 1
        c.set_Property(i); // 2

        _ = c.Property; // 3
        c.Property = i; // 4
    }
}
";

            var reference = CreateMetadataReferenceFromIlSource(il);
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,15): error CS0570: 'C.get_Property()' is not supported by the language
                //         _ = c.get_Property(); // 1
                Diagnostic(ErrorCode.ERR_BindToBogus, "get_Property").WithArguments("C.get_Property()").WithLocation(6, 15),
                // (7,11): error CS0570: 'C.set_Property(ref int)' is not supported by the language
                //         c.set_Property(i); // 2
                Diagnostic(ErrorCode.ERR_BindToBogus, "set_Property").WithArguments("C.set_Property(ref int)").WithLocation(7, 11),
                // (9,15): error CS1545: Property, indexer, or event 'C.Property' is not supported by the language; try directly calling accessor methods 'C.get_Property()' or 'C.set_Property(ref int)'
                //         _ = c.Property; // 3
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Property").WithArguments("C.Property", "C.get_Property()", "C.set_Property(ref int)").WithLocation(9, 15),
                // (10,11): error CS1545: Property, indexer, or event 'C.Property' is not supported by the language; try directly calling accessor methods 'C.get_Property()' or 'C.set_Property(ref int)'
                //         c.Property = i; // 4
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Property").WithArguments("C.Property", "C.get_Property()", "C.set_Property(ref int)").WithLocation(10, 11)
                );

            var property0 = (PEPropertySymbol)comp.GlobalNamespace.GetMember("C.Property");
            Assert.True(property0.HasUseSiteError);
            Assert.True(property0.MustCallMethodsDirectly);
            Assert.Equal("System.Runtime.CompilerServices.IsExternalInit", property0.RefCustomModifiers.Single().Modifier.ToTestDisplayString());
            Assert.Empty(property0.TypeWithAnnotations.CustomModifiers);

            Assert.True(property0.GetMethod.HasUseSiteError);
            Assert.True(property0.GetMethod.HasUnsupportedMetadata);
            Assert.True(property0.GetMethod.ReturnsByRef);

            Assert.True(property0.SetMethod.HasUseSiteError);
            Assert.True(property0.SetMethod.HasUnsupportedMetadata);
            Assert.True(property0.SetMethod.Parameters[0].HasUnsupportedMetadata);
            Assert.False(property0.SetMethod.IsInitOnly);
            Assert.False(property0.GetPublicSymbol().SetMethod.IsInitOnly);
        }

        [Fact]
        public void ModReqOnRefProperty_OnReturn()
        {
            string il = @"
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig specialname newslot virtual instance int32 modreq(System.Runtime.CompilerServices.IsExternalInit)& get_Property () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname newslot virtual instance void set_Property ( int32 modreq(System.Runtime.CompilerServices.IsExternalInit)& 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance int32 modreq(System.Runtime.CompilerServices.IsExternalInit)& Property()
    {
        .get instance int32 modreq(System.Runtime.CompilerServices.IsExternalInit)& C::get_Property()
        .set instance void C::set_Property(int32 modreq(System.Runtime.CompilerServices.IsExternalInit)&)
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";
            string source = @"
public class D
{
    void M(C c, ref int i)
    {
        _ = c.get_Property(); // 1
        c.set_Property(i); // 2

        _ = c.Property; // 3
        c.Property = i; // 4
    }
}
";

            var reference = CreateMetadataReferenceFromIlSource(il);
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,15): error CS0570: 'C.get_Property()' is not supported by the language
                //         _ = c.get_Property(); // 1
                Diagnostic(ErrorCode.ERR_BindToBogus, "get_Property").WithArguments("C.get_Property()").WithLocation(6, 15),
                // (7,11): error CS0570: 'C.set_Property(ref int)' is not supported by the language
                //         c.set_Property(i); // 2
                Diagnostic(ErrorCode.ERR_BindToBogus, "set_Property").WithArguments("C.set_Property(ref int)").WithLocation(7, 11),
                // (9,15): error CS1545: Property, indexer, or event 'C.Property' is not supported by the language; try directly calling accessor methods 'C.get_Property()' or 'C.set_Property(ref int)'
                //         _ = c.Property; // 3
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Property").WithArguments("C.Property", "C.get_Property()", "C.set_Property(ref int)").WithLocation(9, 15),
                // (10,11): error CS1545: Property, indexer, or event 'C.Property' is not supported by the language; try directly calling accessor methods 'C.get_Property()' or 'C.set_Property(ref int)'
                //         c.Property = i; // 4
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Property").WithArguments("C.Property", "C.get_Property()", "C.set_Property(ref int)").WithLocation(10, 11)
                );

            var property0 = (PEPropertySymbol)comp.GlobalNamespace.GetMember("C.Property");
            Assert.True(property0.HasUseSiteError);
            Assert.True(property0.MustCallMethodsDirectly);
            Assert.Empty(property0.RefCustomModifiers);
            Assert.Equal("System.Runtime.CompilerServices.IsExternalInit", property0.TypeWithAnnotations.CustomModifiers.Single().Modifier.ToTestDisplayString());
            Assert.Equal("System.Int32", property0.TypeWithAnnotations.Type.ToTestDisplayString());

            Assert.True(property0.GetMethod.HasUseSiteError);
            Assert.True(property0.GetMethod.HasUnsupportedMetadata);
            Assert.True(property0.GetMethod.ReturnsByRef);

            Assert.True(property0.SetMethod.HasUseSiteError);
            Assert.True(property0.SetMethod.HasUnsupportedMetadata);
            Assert.True(property0.SetMethod.Parameters[0].HasUnsupportedMetadata);
            Assert.False(property0.SetMethod.IsInitOnly);
            Assert.False(property0.GetPublicSymbol().SetMethod.IsInitOnly);
        }

        [Fact]
        public void ModReqOnGetAccessorReturnValue()
        {
            string il = @"
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig specialname newslot virtual instance int32 modreq(System.Runtime.CompilerServices.IsExternalInit) get_Property () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname newslot virtual instance void set_Property ( int32 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance int32 Property()
    {
        .get instance int32 modreq(System.Runtime.CompilerServices.IsExternalInit) C::get_Property()
        .set instance void C::set_Property(int32)
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";

            string source = @"
public class Derived : C
{
    public override int Property { get { throw null; } }
}
";

            var reference = CreateMetadataReferenceFromIlSource(il);
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,36): error CS0570: 'C.Property.get' is not supported by the language
                //     public override int Property { get { throw null; } }
                Diagnostic(ErrorCode.ERR_BindToBogus, "get").WithArguments("C.Property.get").WithLocation(4, 36)
                );

            var property = (PEPropertySymbol)comp.GlobalNamespace.GetMember("C.Property");
            Assert.False(property.GetMethod.IsInitOnly);
            Assert.False(property.GetPublicSymbol().GetMethod.IsInitOnly);
            Assert.True(property.GetMethod.HasUseSiteError);
            Assert.True(property.GetMethod.HasUnsupportedMetadata);
            Assert.False(property.SetMethod.IsInitOnly);
            Assert.False(property.GetPublicSymbol().SetMethod.IsInitOnly);
            Assert.False(property.SetMethod.HasUseSiteError);
        }

        [Fact]
        public void TestSyntaxFacts()
        {
            Assert.True(SyntaxFacts.IsAccessorDeclaration(SyntaxKind.InitAccessorDeclaration));
            Assert.True(SyntaxFacts.IsAccessorDeclarationKeyword(SyntaxKind.InitKeyword));
        }

        [Fact]
        public void NoCascadingErrorsInStaticConstructor()
        {
            string source = @"
public class C
{
    public string Property { get { throw null; } init { throw null; } }
    static C()
    {
        Property = null; // 1
        this.Property = null; // 2
    }
}
public class D : C
{
    static D()
    {
        Property = null; // 3
        this.Property = null; // 4
        base.Property = null; // 5
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (7,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.Property'
                //         Property = null; // 1
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Property").WithArguments("C.Property").WithLocation(7, 9),
                // (8,9): error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer
                //         this.Property = null; // 2
                Diagnostic(ErrorCode.ERR_ThisInStaticMeth, "this").WithLocation(8, 9),
                // (15,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.Property'
                //         Property = null; // 3
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Property").WithArguments("C.Property").WithLocation(15, 9),
                // (16,9): error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer
                //         this.Property = null; // 4
                Diagnostic(ErrorCode.ERR_ThisInStaticMeth, "this").WithLocation(16, 9),
                // (17,9): error CS1511: Keyword 'base' is not available in a static method
                //         base.Property = null; // 5
                Diagnostic(ErrorCode.ERR_BaseInStaticMeth, "base").WithLocation(17, 9)
                );
        }

        [Fact]
        public void LocalFunctionsAreNotInitOnly()
        {
            var comp = CreateCompilation(new[] { @"
public class C
{
    delegate void Delegate();

    void M()
    {
        local();
        void local() { }
    }
}
", IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var localFunctionSyntax = tree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var localFunctionSymbol = model.GetDeclaredSymbol(localFunctionSyntax).GetSymbol<LocalFunctionSymbol>();
            Assert.False(localFunctionSymbol.IsInitOnly);
            Assert.False(localFunctionSymbol.GetPublicSymbol().IsInitOnly);

            var delegateSyntax = tree.GetRoot().DescendantNodes().OfType<DelegateDeclarationSyntax>().Single();
            var delegateMemberSymbols = model.GetDeclaredSymbol(delegateSyntax).GetSymbol<SourceNamedTypeSymbol>().GetMembers();
            Assert.True(delegateMemberSymbols.All(m => m is SourceDelegateMethodSymbol));
            foreach (var member in delegateMemberSymbols)
            {
                if (member is MethodSymbol method)
                {
                    Assert.False(method.IsInitOnly);
                    Assert.False(method.GetPublicSymbol().IsInitOnly);
                }
            }
        }

        [Fact]
        public void RetargetProperties_WithInitOnlySetter()
        {
            var source0 = @"
public struct S
{
    public int Property { get; init; }
}
";

            var source1 = @"
class Program
{
    public static void Main()
    {
        var s = new S() { Property = 42 };
        System.Console.WriteLine(s.Property);
    }
}
";

            var source2 = @"
class Program
{
    public static void Main()
    {
        var s = new S() { Property = 43 };
        System.Console.WriteLine(s.Property);
    }
}
";

            var comp1 = CreateCompilation(new[] { source0, source1, IsExternalInitTypeDefinition },
                targetFramework: TargetFramework.Mscorlib40, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            // PEVerify: [ : S::set_Property] Cannot change initonly field outside its .ctor.
            CompileAndVerify(comp1, expectedOutput: "42",
                verify: ExecutionConditionUtil.IsCoreClr ? Verification.Passes : Verification.Fails);
            var comp1Ref = new[] { comp1.ToMetadataReference() };

            var comp7 = CreateCompilation(source2, references: comp1Ref,
                targetFramework: TargetFramework.Mscorlib46, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp7, expectedOutput: "43");

            var property = comp7.GetMember<PropertySymbol>("S.Property");
            var setter = (RetargetingMethodSymbol)property.SetMethod;
            Assert.True(setter.IsInitOnly);
        }
    }
}
