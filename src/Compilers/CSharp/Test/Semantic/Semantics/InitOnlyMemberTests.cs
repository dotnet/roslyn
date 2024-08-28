// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.InitOnlySetters)]
    public class InitOnlyMemberTests : CompilingTestBase
    {
        // Spec: https://github.com/dotnet/csharplang/blob/main/proposals/init.md

        // https://github.com/dotnet/roslyn/issues/44685
        // test dynamic scenario
        // test whether reflection use property despite modreq?
        // test behavior of old compiler with modreq. For example VB

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
            comp.VerifyEmitDiagnostics(
                // (4,35): error CS8400: Feature 'init-only setters' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public string Property { get; init; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "init").WithArguments("init-only setters", "9.0").WithLocation(4, 35)
                );

            var property = (PropertySymbol)comp.GlobalNamespace.GetMember("C.Property");
            Assert.False(property.GetMethod.IsInitOnly);
            Assert.True(property.SetMethod.IsInitOnly);
            IPropertySymbol publicProperty = property.GetPublicSymbol();
            Assert.False(publicProperty.GetMethod.IsInitOnly);
            Assert.True(publicProperty.SetMethod.IsInitOnly);
        }

        [Theory, CombinatorialData, WorkItem(50245, "https://github.com/dotnet/roslyn/issues/50245")]
        public void TestCSharp8_ConsumptionInObjectInitializer(bool useMetadataImage)
        {
            string lib_cs = @"
public class C
{
    public string Property { get; init; }
    public string Property2 { get; }
}
";
            var lib = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);

            string source = @"
public class D
{
    void M()
    {
        _ = new C() { Property = string.Empty, Property2 = string.Empty };
    }
}
";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8,
                references: new[] { useMetadataImage ? lib.EmitToImageReference() : lib.ToMetadataReference() });
            comp.VerifyEmitDiagnostics(
                // (6,23): error CS8400: Feature 'init-only setters' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         _ = new C() { Property = string.Empty, Property2 = string.Empty };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "Property").WithArguments("init-only setters", "9.0").WithLocation(6, 23),
                // (6,48): error CS0200: Property or indexer 'C.Property2' cannot be assigned to -- it is read only
                //         _ = new C() { Property = string.Empty, Property2 = string.Empty };
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "Property2").WithArguments("C.Property2").WithLocation(6, 48)
                );
        }

        [Theory, CombinatorialData, WorkItem(50245, "https://github.com/dotnet/roslyn/issues/50245")]
        public void TestCSharp8_ConsumptionInNestedObjectInitializer(bool useMetadataImage)
        {
            string lib_cs = @"
public class C
{
    public string Property { get; init; }
    public string Property2 { get; }
}
public class Container
{
    public C contained;
}
";
            var lib = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);

            string source = @"
public class D
{
    void M(C c)
    {
        _ = new Container() { contained = { Property = string.Empty, Property2 = string.Empty } };
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8,
                references: new[] { useMetadataImage ? lib.EmitToImageReference() : lib.ToMetadataReference() });
            comp.VerifyEmitDiagnostics(
                // (6,45): error CS8852: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         _ = new Container() { contained = { Property = string.Empty, Property2 = string.Empty } };
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("C.Property").WithLocation(6, 45),
                // (6,70): error CS0200: Property or indexer 'C.Property2' cannot be assigned to -- it is read only
                //         _ = new Container() { contained = { Property = string.Empty, Property2 = string.Empty } };
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "Property2").WithArguments("C.Property2").WithLocation(6, 70)
                );
        }

        [Theory, CombinatorialData, WorkItem(50245, "https://github.com/dotnet/roslyn/issues/50245")]
        public void TestCSharp8_ConsumptionInWithExpression(bool useMetadataImage)
        {
            string lib_cs = @"
public record C
{
    public string Property { get; init; }
    public string Property2 { get; }
}
";
            var lib = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);

            string source = @"
public class D
{
    void M(C c)
    {
        _ = c with { Property = string.Empty, Property2 = string.Empty };
    }
}
";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8,
                references: new[] { useMetadataImage ? lib.EmitToImageReference() : lib.ToMetadataReference() });
            comp.VerifyEmitDiagnostics(
                // (6,15): error CS8400: Feature 'records' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         _ = c with { Property = string.Empty, Property2 = string.Empty };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "with").WithArguments("records", "9.0").WithLocation(6, 15),
                // (6,22): error CS8400: Feature 'init-only setters' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         _ = c with { Property = string.Empty, Property2 = string.Empty };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "Property").WithArguments("init-only setters", "9.0").WithLocation(6, 22),
                // (6,47): error CS0200: Property or indexer 'C.Property2' cannot be assigned to -- it is read only
                //         _ = c with { Property = string.Empty, Property2 = string.Empty };
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "Property2").WithArguments("C.Property2").WithLocation(6, 47)
                );
        }

        [Theory, CombinatorialData, WorkItem(50245, "https://github.com/dotnet/roslyn/issues/50245")]
        public void TestCSharp8_ConsumptionInAssignment(bool useMetadataImage)
        {
            string lib_cs = @"
public record C
{
    public string Property { get; init; }
    public string Property2 { get; }
}
";
            var lib = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);

            string source = @"
public class D
{
    void M(C c)
    {
        c.Property = string.Empty;
        c.Property2 = string.Empty;
    }
}
";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8,
                references: new[] { useMetadataImage ? lib.EmitToImageReference() : lib.ToMetadataReference() });
            comp.VerifyEmitDiagnostics(
                // (6,9): error CS8852: Init-only property or indexer 'C.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         c.Property = string.Empty;
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.Property").WithArguments("C.Property").WithLocation(6, 9),
                // (7,9): error CS0200: Property or indexer 'C.Property2' cannot be assigned to -- it is read only
                //         c.Property2 = string.Empty;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "c.Property2").WithArguments("C.Property2").WithLocation(7, 9)
                );
        }

        [Theory, CombinatorialData, WorkItem(50245, "https://github.com/dotnet/roslyn/issues/50245")]
        public void TestCSharp8_ConsumptionInAttribute(bool useMetadataImage)
        {
            string lib_cs = @"
public class TestAttribute : System.Attribute
{
    public int Property { get; init; }
    public int Property2 { get; }
}
";
            var lib = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);

            string source = @"
[Test(Property = 42, Property2 = 43)]
class C
{
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8,
                references: new[] { useMetadataImage ? lib.EmitToImageReference() : lib.ToMetadataReference() });
            comp.VerifyEmitDiagnostics(
                // (2,7): error CS8400: Feature 'init-only setters' is not available in C# 8.0. Please use language version 9.0 or greater.
                // [Test(Property = 42, Property2 = 43)]
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "Property = 42").WithArguments("init-only setters", "9.0").WithLocation(2, 7),
                // (2,22): error CS0617: 'Property2' is not a valid named attribute argument. Named attribute arguments must be fields which are not readonly, static, or const, or read-write properties which are public and not static.
                // [Test(Property = 42, Property2 = 43)]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgument, "Property2").WithArguments("Property2").WithLocation(2, 22)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular9,
                references: new[] { useMetadataImage ? lib.EmitToImageReference() : lib.ToMetadataReference() });
            comp.VerifyEmitDiagnostics(
                // (2,22): error CS0617: 'Property2' is not a valid named attribute argument. Named attribute arguments must be fields which are not readonly, static, or const, or read-write properties which are public and not static.
                // [Test(Property = 42, Property2 = 43)]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgument, "Property2").WithArguments("Property2").WithLocation(2, 22)
                );
        }

        [Fact, WorkItem(50245, "https://github.com/dotnet/roslyn/issues/50245")]
        public void TestCSharp8_ConsumptionWithinSameCompilation()
        {
            string source = @"
class C
{
    string Property { get; init; }
    string Property2 { get; }

    void M(C c)
    {
        _ = new C() { Property = string.Empty, Property2 = string.Empty };
    }
}
";

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // (4,28): error CS8400: Feature 'init-only setters' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     string Property { get; init; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "init").WithArguments("init-only setters", "9.0").WithLocation(4, 28),
                // (9,48): error CS0200: Property or indexer 'C.Property2' cannot be assigned to -- it is read only
                //         _ = new C() { Property = string.Empty, Property2 = string.Empty };
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "Property2").WithArguments("C.Property2").WithLocation(9, 48)
                );
        }

        [Fact, WorkItem(50245, "https://github.com/dotnet/roslyn/issues/50245")]
        public void TestCSharp8_ConsumptionWithinSameCompilation_InAttribute()
        {
            string source = @"
public class TestAttribute : System.Attribute
{
    public int Property { get; init; }
    public int Property2 { get; }
}

[Test(Property = 42, Property2 = 43)]
class C
{
}
";

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // (4,32): error CS8400: Feature 'init-only setters' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public int Property { get; init; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "init").WithArguments("init-only setters", "9.0").WithLocation(4, 32),
                // (8,22): error CS0617: 'Property2' is not a valid named attribute argument. Named attribute arguments must be fields which are not readonly, static, or const, or read-write properties which are public and not static.
                // [Test(Property = 42, Property2 = 43)]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgument, "Property2").WithArguments("Property2").WithLocation(8, 22)
                );
        }

        [Fact, WorkItem(50245, "https://github.com/dotnet/roslyn/issues/50245")]
        public void TestCSharp8_ConsumptionFromCompilationReference()
        {
            string lib_cs = @"
public class C
{
    public string Property { get; init; }
    public string Property2 { get; }
}
";
            var lib = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular8, assemblyName: "lib");

            string source = @"
public class D
{
    void M()
    {
        _ = new C() { Property = string.Empty, Property2 = string.Empty };
    }
}
";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8, references: new[] { lib.ToMetadataReference() }, assemblyName: "comp");
            comp.VerifyEmitDiagnostics(
                // (6,23): error CS8400: Feature 'init-only setters' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         _ = new C() { Property = string.Empty, Property2 = string.Empty };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "Property").WithArguments("init-only setters", "9.0").WithLocation(6, 23),
                // (6,48): error CS0200: Property or indexer 'C.Property2' cannot be assigned to -- it is read only
                //         _ = new C() { Property = string.Empty, Property2 = string.Empty };
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "Property2").WithArguments("C.Property2").WithLocation(6, 48)
                );
        }

        [Theory, CombinatorialData, WorkItem(50245, "https://github.com/dotnet/roslyn/issues/50245")]
        public void TestCSharp8_ConsumptionWithDynamicArgument(bool useMetadataImage)
        {
            string lib_cs = @"
public class C
{
    public string Property { get; init; }
    public string Property2 { get; }
    public C(int i) { }
}
";
            var lib = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);

            string source = @"
public class D
{
    void M(dynamic d)
    {
        _ = new C(d) { Property = string.Empty, Property2 = string.Empty };
    }
}
";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8,
                references: new[] { useMetadataImage ? lib.EmitToImageReference() : lib.ToMetadataReference() });
            comp.VerifyEmitDiagnostics(
                // (6,24): error CS8400: Feature 'init-only setters' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         _ = new C(d) { Property = string.Empty, Property2 = string.Empty };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "Property").WithArguments("init-only setters", "9.0").WithLocation(6, 24),
                // (6,49): error CS0200: Property or indexer 'C.Property2' cannot be assigned to -- it is read only
                //         _ = new C(d) { Property = string.Empty, Property2 = string.Empty };
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "Property2").WithArguments("C.Property2").WithLocation(6, 49)
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics();
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
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
            var libComp = CreateCompilation(new[] { parent, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            var comp = CreateCompilation(new[] { source, main }, references: new[] { emitImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() },
                parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Main set:42 get:42");

            libComp = CreateCompilation(new[] { parent, source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp = CreateCompilation(new[] { main }, references: new[] { emitImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() },
                parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
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
            var libComp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            var comp = CreateCompilation(new[] { main }, references: new[] { emitImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() },
                parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
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
            var libComp = CreateCompilation(new[] { parent, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            var comp = CreateCompilation(new[] { source, main }, references: new[] { emitImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() },
                parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Main set:42 get:42");

            libComp = CreateCompilation(new[] { parent, source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp = CreateCompilation(new[] { main }, references: new[] { emitImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() },
                parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (8,16): error CS0206: A non ref-returning property or indexer may not be used as an out or ref value
                //         M2(out Property); // 1
                Diagnostic(ErrorCode.ERR_RefProperty, "Property").WithLocation(8, 16)
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (4,13): error CS8145: Auto-implemented properties cannot return by reference
                //     ref int Property1 { get; init; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "Property1").WithLocation(4, 13),
                // (4,30): error CS8147: Properties which return by reference cannot have set accessors
                //     ref int Property1 { get; init; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "init").WithLocation(4, 30),
                // (5,13): error CS8146: Properties which return by reference must have a get accessor
                //     ref int Property2 { init; }
                Diagnostic(ErrorCode.ERR_RefPropertyMustHaveGetAccessor, "Property2").WithLocation(5, 13),
                // (6,44): error CS8147: Properties which return by reference cannot have set accessors
                //     ref int Property3 { get => throw null; init => throw null; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "init").WithLocation(6, 44),
                // (7,13): error CS8146: Properties which return by reference must have a get accessor
                //     ref int Property4 { init => throw null; }
                Diagnostic(ErrorCode.ERR_RefPropertyMustHaveGetAccessor, "Property4").WithLocation(7, 13)
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
                parseOptions: TestOptions.Regular9,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            comp.VerifyDiagnostics();

            // PEVerify:  [ : C::set_Property] Cannot change initonly field outside its .ctor.
            CompileAndVerify(comp, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator,
                verify: Verification.FailsPEVerify);

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
            var libComp = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
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
                parseOptions: TestOptions.Regular9);

            comp.VerifyEmitDiagnostics(
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

        [Fact, WorkItem(50053, "https://github.com/dotnet/roslyn/issues/50053")]
        public void PrivatelyImplementingInitOnlyProperty_ReferenceConversion()
        {
            string source = @"
var x = new DerivedType() { SomethingElse = 42 };
System.Console.Write(x.SomethingElse);

public interface ISomething { int Property { get; init; } }
public record BaseType : ISomething { int ISomething.Property { get; init; } }

public record DerivedType : BaseType
{
    public int SomethingElse
    {
        get => ((ISomething)this).Property;
        init => ((ISomething)this).Property = value;
    }
}
";

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (13,17): error CS8852: Init-only property or indexer 'ISomething.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         init => ((ISomething)this).Property = value;
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "((ISomething)this).Property").WithArguments("ISomething.Property").WithLocation(13, 17)
                );
        }

        [Fact, WorkItem(50053, "https://github.com/dotnet/roslyn/issues/50053")]
        public void PrivatelyImplementingInitOnlyProperty_BoxingConversion()
        {
            string source = @"
var x = new Type() { SomethingElse = 42 };

public interface ISomething { int Property { get; init; } }

public struct Type : ISomething
{
    int ISomething.Property { get; init; }

    public int SomethingElse
    {
        get => throw null;
        init => ((ISomething)this).Property = value;
    }
}
";

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (13,17): error CS8852: Init-only property or indexer 'ISomething.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         init => ((ISomething)this).Property = value;
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "((ISomething)this).Property").WithArguments("ISomething.Property").WithLocation(13, 17)
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

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var libComp = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9,
                references: new[] { emitAsImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() });
            comp.VerifyEmitDiagnostics(
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

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var libComp = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            libComp.VerifyEmitDiagnostics();

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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9,
                references: new[] { emitAsImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() });
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var libComp = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9,
                references: new[] { emitAsImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() });
            comp.VerifyEmitDiagnostics(
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
                targetFramework: TargetFramework.NetCoreApp,
                parseOptions: TestOptions.Regular9);
            Assert.True(comp.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            Assert.True(comp.SupportsRuntimeCapability(RuntimeCapability.DefaultImplementationsOfInterfaces));

            comp.VerifyEmitDiagnostics(
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
                targetFramework: TargetFramework.NetCoreApp,
                parseOptions: TestOptions.Regular9);
            Assert.True(comp.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            Assert.True(comp.SupportsRuntimeCapability(RuntimeCapability.DefaultImplementationsOfInterfaces));

            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
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
                "System.String C.ToString()",
                "System.Boolean C." + WellKnownMemberNames.PrintMembersMethodName + "(System.Text.StringBuilder builder)",
                "System.Boolean C.op_Inequality(C? left, C? right)",
                "System.Boolean C.op_Equality(C? left, C? right)",
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            @field = null;
        }
    }
    public int RegularProperty
    {
        get
        {
            @field = null; // 3
            throw null;
        }
        set
        {
            @field = null; // 4
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
            @field = null; // 7
        }
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // 0.cs(11,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         field = null; // 1
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(11, 9),
                // 0.cs(12,23): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         _ = new C() { field = null }; // 2
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(12, 23),
                // 0.cs(25,13): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //             @field = null; // 3
                Diagnostic(ErrorCode.ERR_AssgReadonly, "@field").WithLocation(25, 13),
                // 0.cs(30,13): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //             @field = null; // 4
                Diagnostic(ErrorCode.ERR_AssgReadonly, "@field").WithLocation(30, 13),
                // 0.cs(38,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         field = null; // 5
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(38, 9),
                // 0.cs(42,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         field = null; // 6
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(42, 9),
                // 0.cs(48,13): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //             @field = null; // 7
                Diagnostic(ErrorCode.ERR_AssgReadonly, "@field").WithLocation(48, 13)
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
            @field = value;
        }
    }
}
";

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition },
                options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            // PEVerify: [ : C::set_Property] Cannot change initonly field outside its .ctor.
            CompileAndVerify(comp, expectedOutput: "42 43",
                verify: Verification.FailsPEVerify);
        }

        [Fact]
        public void ReadonlyFields_TypesDifferingNullability()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        System.Console.Write(C1<int>.F1.content);
        System.Console.Write("" "");
        System.Console.Write(C2<int>.F1.content);
    }
}

public struct Container
{
    public int content;
}

class C1<T>
{
    public static readonly Container F1;

    static C1()
    {
        C1<T>.F1.content = 2;
    }
}

#nullable enable

class C2<T>
{
    public static readonly Container F1;

    static C2()
    {
        C2<T>.F1.content = 3;
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "2 3", verify: Verification.Skipped);

            // PEVerify bug
            // [ : C::Main][mdToken=0x6000004][offset 0x00000001] Cannot change initonly field outside its .ctor.
            v.VerifyIL("C.Main", @"
{
  // Code size       45 (0x2d)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldsflda    ""Container C1<int>.F1""
  IL_0006:  ldfld      ""int Container.content""
  IL_000b:  call       ""void System.Console.Write(int)""
  IL_0010:  nop
  IL_0011:  ldstr      "" ""
  IL_0016:  call       ""void System.Console.Write(string)""
  IL_001b:  nop
  IL_001c:  ldsflda    ""Container C2<int>.F1""
  IL_0021:  ldfld      ""int Container.content""
  IL_0026:  call       ""void System.Console.Write(int)""
  IL_002b:  nop
  IL_002c:  ret
}
");
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
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
            c.@field = null; // 2
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
            c.@field = null; // 4
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

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // 0.cs(9,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         c.field = null; // 1
                Diagnostic(ErrorCode.ERR_AssgReadonly, "c.field").WithLocation(9, 9),
                // 0.cs(16,13): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //             c.@field = null; // 2
                Diagnostic(ErrorCode.ERR_AssgReadonly, "c.@field").WithLocation(16, 13),
                // 0.cs(24,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         c.field = null; // 3
                Diagnostic(ErrorCode.ERR_AssgReadonly, "c.field").WithLocation(24, 9),
                // 0.cs(31,13): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //             c.@field = null; // 4
                Diagnostic(ErrorCode.ERR_AssgReadonly, "c.@field").WithLocation(31, 13),
                // 0.cs(40,13): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //             field = // 5
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(40, 13),
                // 0.cs(41,18): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //                 (c.field = null)  // 6
                Diagnostic(ErrorCode.ERR_AssgReadonly, "c.field").WithLocation(41, 18)
                );
        }

        [Fact, WorkItem(45657, "https://github.com/dotnet/roslyn/issues/45657")]
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
            @field.content = null;
        }
    }
    public int RegularProperty
    {
        get
        {
            @field.content = null; // 2
            throw null;
        }
        set
        {
            @field.content = null; // 3
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
            @field.content = null; // 6
        }
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // 0.cs(15,9): error CS1648: Members of readonly field 'C.field' cannot be modified (except in a constructor or a variable initializer)
                //         field.content = null; // 1
                Diagnostic(ErrorCode.ERR_AssgReadonly2, "field.content").WithArguments("C.field").WithLocation(15, 9),
                // 0.cs(28,13): error CS1648: Members of readonly field 'C.field' cannot be modified (except in a constructor or a variable initializer)
                //             @field.content = null; // 2
                Diagnostic(ErrorCode.ERR_AssgReadonly2, "@field.content").WithArguments("C.field").WithLocation(28, 13),
                // 0.cs(33,13): error CS1648: Members of readonly field 'C.field' cannot be modified (except in a constructor or a variable initializer)
                //             @field.content = null; // 3
                Diagnostic(ErrorCode.ERR_AssgReadonly2, "@field.content").WithArguments("C.field").WithLocation(33, 13),
                // 0.cs(41,9): error CS1648: Members of readonly field 'C.field' cannot be modified (except in a constructor or a variable initializer)
                //         field.content = null; // 4
                Diagnostic(ErrorCode.ERR_AssgReadonly2, "field.content").WithArguments("C.field").WithLocation(41, 9),
                // 0.cs(45,9): error CS1648: Members of readonly field 'C.field' cannot be modified (except in a constructor or a variable initializer)
                //         field.content = null; // 5
                Diagnostic(ErrorCode.ERR_AssgReadonly2, "field.content").WithArguments("C.field").WithLocation(45, 9),
                // 0.cs(51,13): error CS1648: Members of readonly field 'C.field' cannot be modified (except in a constructor or a variable initializer)
                //             @field.content = null; // 6
                Diagnostic(ErrorCode.ERR_AssgReadonly2, "@field.content").WithArguments("C.field").WithLocation(51, 13)
                );
        }

        [Fact, WorkItem(45657, "https://github.com/dotnet/roslyn/issues/45657")]
        public void ReadonlyFieldsMembers_Evaluation()
        {
            string source = @"
public struct Container
{
    public int content;
}

public class C
{
    public readonly Container field;

    public int InitOnlyProperty1
    {
        init
        {
            @field.content = value;
            System.Console.Write(""RAN "");
        }
    }

    public static void Main()
    {
        var c = new C() { InitOnlyProperty1 = 42 };
        System.Console.Write(c.field.content);
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "RAN 42", verify: Verification.Skipped /* init-only */);
        }

        [Fact, WorkItem(45657, "https://github.com/dotnet/roslyn/issues/45657")]
        public void ReadonlyFieldsMembers_Static()
        {
            string source = @"
public struct Container
{
    public int content;
}

public static class C
{
    public static readonly Container field;

    public static int InitOnlyProperty1
    {
        init
        {
            @field.content = value;
        }
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // 0.cs(13,9): error CS8856: The 'init' accessor is not valid on static members
                //         init
                Diagnostic(ErrorCode.ERR_BadInitAccessor, "init").WithLocation(13, 9),
                // 0.cs(15,13): error CS1650: Fields of static readonly field 'C.field' cannot be assigned to (except in a static constructor or a variable initializer)
                //             @field.content = value;
                Diagnostic(ErrorCode.ERR_AssgReadonlyStatic2, "@field.content").WithArguments("C.field").WithLocation(15, 13)
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
            var libComp = CreateCompilation(new[] { lib_cs, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);

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
            @field = null; // 5
        }
    }
}
";
            var comp = CreateCompilation(source,
                references: new[] { emitAsImage ? libComp.EmitToImageReference() : libComp.ToMetadataReference() },
                parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (6,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         field = null; // 1
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(6, 9),
                // (7,23): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         _ = new C() { field = null }; // 2
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(7, 23),
                // (12,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         field = null; // 3
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(12, 9),
                // (13,23): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         _ = new C() { field = null }; // 4
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(13, 23),
                // (20,13): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //             @field = null; // 5
                Diagnostic(ErrorCode.ERR_AssgReadonly, "@field").WithLocation(20, 13)
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
", IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);

            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
        public void ModReqOnStaticSet()
        {
            string il = @"
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig newslot specialname
            static void modreq(System.Runtime.CompilerServices.IsExternalInit) set_P(int32 x) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    } 

    .property instance int32 P()
    {
      .set void modreq(System.Runtime.CompilerServices.IsExternalInit) C::set_P(int32)
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
        C.P = 2;
    }
}
";

            var reference = CreateMetadataReferenceFromIlSource(il);
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (6,11): error CS0570: 'C.P.set' is not supported by the language
                //         C.P = 2;
                Diagnostic(ErrorCode.ERR_BindToBogus, "P").WithArguments("C.P.set").WithLocation(6, 11)
                );

            var method = (PEMethodSymbol)comp.GlobalNamespace.GetMember("C.set_P");
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
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(source, references: new[] { reference }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
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
", IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);

            comp.VerifyEmitDiagnostics();

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
                targetFramework: TargetFramework.Mscorlib40, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            // PEVerify: [ : S::set_Property] Cannot change initonly field outside its .ctor.
            CompileAndVerify(comp1, expectedOutput: "42",
                verify: Verification.FailsPEVerify);
            var comp1Ref = new[] { comp1.ToMetadataReference() };

            var comp7 = CreateCompilation(source2, references: comp1Ref,
                targetFramework: TargetFramework.Mscorlib46, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            CompileAndVerify(comp7, expectedOutput: "43");

            var property = comp7.GetMember<PropertySymbol>("S.Property");
            var setter = (RetargetingMethodSymbol)property.SetMethod;
            Assert.True(setter.IsInitOnly);
        }

        [Fact]
        [WorkItem(47612, "https://github.com/dotnet/roslyn/issues/47612")]
        public void InitOnlyOnReadonlyStruct_AutoProp()
        {
            var verifier = CompileAndVerify(new[] { IsExternalInitTypeDefinition, @"
var s = new S { I = 1 };
System.Console.Write(s.I);

public readonly struct S
{
    public int I { get; init; }
}
" }, verify: Verification.FailsPEVerify, expectedOutput: "1");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (S V_0, //s
                S V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.1
  IL_000b:  call       ""void S.I.init""
  IL_0010:  ldloc.1
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""int S.I.get""
  IL_0019:  call       ""void System.Console.Write(int)""
  IL_001e:  ret
}
");
        }

        [Fact]
        [WorkItem(47612, "https://github.com/dotnet/roslyn/issues/47612")]
        public void InitOnlyOnReadonlyStruct_ManualProp()
        {
            var verifier = CompileAndVerify(new[] { IsExternalInitTypeDefinition, @"
var s = new S { I = 1 };
System.Console.Write(s.I);

public readonly struct S
{
    private readonly int i;
    public int I { get => i; init => i = value; }
}
" }, verify: Verification.FailsPEVerify, expectedOutput: "1");

            var s = verifier.Compilation.GetTypeByMetadataName("S");
            var i = s.GetMember<IPropertySymbol>("I");
            Assert.False(i.SetMethod.IsReadOnly);

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (S V_0, //s
                S V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.1
  IL_000b:  call       ""void S.I.init""
  IL_0010:  ldloc.1
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""int S.I.get""
  IL_0019:  call       ""void System.Console.Write(int)""
  IL_001e:  ret
}
");
        }

        [Fact]
        [WorkItem(47612, "https://github.com/dotnet/roslyn/issues/47612")]
        public void InitOnlyOnReadonlyProperty_AutoProp()
        {
            var verifier = CompileAndVerify(new[] { IsExternalInitTypeDefinition, @"
var s = new S { I = 1 };
System.Console.Write(s.I);

public struct S
{
    public readonly int I { get; init; }
}
" }, verify: Verification.FailsPEVerify, expectedOutput: "1");

            var s = verifier.Compilation.GetTypeByMetadataName("S");
            var i = s.GetMember<IPropertySymbol>("I");
            Assert.False(i.SetMethod.IsReadOnly);

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (S V_0, //s
                S V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.1
  IL_000b:  call       ""void S.I.init""
  IL_0010:  ldloc.1
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""readonly int S.I.get""
  IL_0019:  call       ""void System.Console.Write(int)""
  IL_001e:  ret
}
");
        }

        [Fact]
        [WorkItem(47612, "https://github.com/dotnet/roslyn/issues/47612")]
        public void InitOnlyOnReadonlyProperty_ManualProp()
        {
            var verifier = CompileAndVerify(new[] { IsExternalInitTypeDefinition, @"
var s = new S { I = 1 };
System.Console.Write(s.I);

public struct S
{
    private readonly int i;
    public readonly int I { get => i; init => i = value; }
}
" }, verify: Verification.FailsPEVerify, expectedOutput: "1");

            var s = verifier.Compilation.GetTypeByMetadataName("S");
            var i = s.GetMember<IPropertySymbol>("I");
            Assert.False(i.SetMethod.IsReadOnly);

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (S V_0, //s
                S V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.1
  IL_000b:  call       ""void S.I.init""
  IL_0010:  ldloc.1
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""readonly int S.I.get""
  IL_0019:  call       ""void System.Console.Write(int)""
  IL_001e:  ret
}
");
        }

        [Fact]
        [WorkItem(47612, "https://github.com/dotnet/roslyn/issues/47612")]
        public void InitOnlyOnReadonlyInit_AutoProp()
        {
            var comp = CreateCompilation(new[] { IsExternalInitTypeDefinition, @"
public struct S
{
    public int I { get; readonly init; }
}
" });

            comp.VerifyDiagnostics(
                // (4,34): error CS8903: 'init' accessors cannot be marked 'readonly'. Mark 'S.I' readonly instead.
                //     public int I { get; readonly init; }
                Diagnostic(ErrorCode.ERR_InitCannotBeReadonly, "init", isSuppressed: false).WithArguments("S.I").WithLocation(4, 34)
            );

            var s = ((Compilation)comp).GetTypeByMetadataName("S");
            var i = s.GetMember<IPropertySymbol>("I");
            Assert.False(i.SetMethod.IsReadOnly);
            Assert.True(((Symbols.PublicModel.PropertySymbol)i).GetSymbol<PropertySymbol>().SetMethod.IsDeclaredReadOnly);
        }

        [Fact]
        [WorkItem(47612, "https://github.com/dotnet/roslyn/issues/47612")]
        public void InitOnlyOnReadonlyInit_ManualProp()
        {
            var comp = CreateCompilation(new[] { IsExternalInitTypeDefinition, @"
public struct S
{
    public int I { get => 1; readonly init { } }
}
" });

            comp.VerifyDiagnostics(
                // (4,39): error CS8903: 'init' accessors cannot be marked 'readonly'. Mark 'S.I' readonly instead.
                //     public int I { get => 1; readonly init { } }
                Diagnostic(ErrorCode.ERR_InitCannotBeReadonly, "init", isSuppressed: false).WithArguments("S.I").WithLocation(4, 39)
            );

            var s = ((Compilation)comp).GetTypeByMetadataName("S");
            var i = s.GetMember<IPropertySymbol>("I");
            Assert.False(i.SetMethod.IsReadOnly);
            Assert.True(((Symbols.PublicModel.PropertySymbol)i).GetSymbol<PropertySymbol>().SetMethod.IsDeclaredReadOnly);
        }

        [Fact]
        [WorkItem(47612, "https://github.com/dotnet/roslyn/issues/47612")]
        public void InitOnlyOnReadonlyInit_ReassignsSelf()
        {
            var verifier = CompileAndVerify(new[] { IsExternalInitTypeDefinition, @"
var s = new S { I1 = 1, I2 = 2 };
System.Console.WriteLine($""I1 is {s.I1}"");

public readonly struct S
{
    private readonly int i;
    public readonly int I1 { get => i; init => i = value; }
    public int I2
    { 
        get => throw null;
        init
        {
            System.Console.WriteLine($""I1 was {I1}"");
            this = default;
        }
    }
}
" }, verify: Verification.FailsPEVerify, expectedOutput: @"I1 was 1
I1 is 0");

            var s = verifier.Compilation.GetTypeByMetadataName("S");
            var i1 = s.GetMember<IPropertySymbol>("I1");
            Assert.False(i1.SetMethod.IsReadOnly);
            var i2 = s.GetMember<IPropertySymbol>("I2");
            Assert.False(i2.SetMethod.IsReadOnly);

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (S V_0, //s
                S V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.1
  IL_000b:  call       ""void S.I1.init""
  IL_0010:  ldloca.s   V_1
  IL_0012:  ldc.i4.2
  IL_0013:  call       ""void S.I2.init""
  IL_0018:  ldloc.1
  IL_0019:  stloc.0
  IL_001a:  ldstr      ""I1 is {0}""
  IL_001f:  ldloca.s   V_0
  IL_0021:  call       ""int S.I1.get""
  IL_0026:  box        ""int""
  IL_002b:  call       ""string string.Format(string, object)""
  IL_0030:  call       ""void System.Console.WriteLine(string)""
  IL_0035:  ret
}
");
        }

        [Fact]
        [WorkItem(50126, "https://github.com/dotnet/roslyn/issues/50126")]
        public void NestedInitializer()
        {
            var source = @"
using System;

Person person = new Person(""j"", ""p"");
Container c = new Container(person)
{
    Person = { FirstName = ""c"" }
};

public record Person(String FirstName, String LastName);
public record Container(Person Person);
";
            var comp = CreateCompilation(new[] { IsExternalInitTypeDefinition, source }, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (7,16): error CS8852: Init-only property or indexer 'Person.FirstName' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //     Person = { FirstName = "c" }
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "FirstName").WithArguments("Person.FirstName").WithLocation(7, 16)
                );
        }

        [Fact]
        [WorkItem(50126, "https://github.com/dotnet/roslyn/issues/50126")]
        public void NestedInitializer_NewT()
        {
            var source = @"
using System;

class C
{
    void M<T>(Person person) where T : Container, new()
    {
        Container c = new T()
        {
            Person = { FirstName = ""c"" }
        };
    }
}

public record Person(String FirstName, String LastName);
public record Container(Person Person);
";
            var comp = CreateCompilation(new[] { IsExternalInitTypeDefinition, source });
            comp.VerifyEmitDiagnostics(
                // (10,24): error CS8852: Init-only property or indexer 'Person.FirstName' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //             Person = { FirstName = "c" }
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "FirstName").WithArguments("Person.FirstName").WithLocation(10, 24)
                );
        }

        [Fact]
        [WorkItem(50126, "https://github.com/dotnet/roslyn/issues/50126")]
        public void NestedInitializer_UsingGenericType()
        {
            var source = @"
using System;

Person person = new Person(""j"", ""p"");
var c = new Container<Person>(person)
{
    PropertyT = { FirstName = ""c"" }
};

public record Person(String FirstName, String LastName);
public record Container<T>(T PropertyT) where T : Person;
";
            var comp = CreateCompilation(new[] { IsExternalInitTypeDefinition, source }, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (7,19): error CS8852: Init-only property or indexer 'Person.FirstName' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //     PropertyT = { FirstName = "c" }
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "FirstName").WithArguments("Person.FirstName").WithLocation(7, 19)
                );
        }

        [Fact]
        [WorkItem(50126, "https://github.com/dotnet/roslyn/issues/50126")]
        public void NestedInitializer_UsingNew()
        {
            var source = @"
using System;

Person person = new Person(""j"", ""p"");
Container c = new Container(person)
{
    Person = new Person(""j"", ""p"") { FirstName = ""c"" }
};

Console.Write(c.Person.FirstName);

public record Person(String FirstName, String LastName);
public record Container(Person Person);
";
            var comp = CreateCompilation(new[] { IsExternalInitTypeDefinition, source }, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            // PEVerify: Cannot change initonly field outside its .ctor.
            CompileAndVerify(comp, expectedOutput: "c", verify: Verification.FailsPEVerify);
        }

        [Fact]
        [WorkItem(50126, "https://github.com/dotnet/roslyn/issues/50126")]
        public void NestedInitializer_UsingNewNoPia()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58277"")]
[CoClass(typeof(ClassITest28))]
public interface ITest28
{
    int Property { get; init; }
}

[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
public abstract class ClassITest28 //: ITest28
{
    public ClassITest28(int x) { }
}
";

            var piaCompilation = CreateCompilationWithMscorlib461(new[] { IsExternalInitTypeDefinition, pia }, options: TestOptions.DebugDll);

            CompileAndVerify(piaCompilation);

            string source = @"
class UsePia
{
    public ITest28 Property2 { get; init; }

    public static void Main()
    {
        var x1 = new ITest28() { Property = 42 };
        var x2 = new UsePia() { Property2 = { Property = 43 } };
    }
}";

            var compilation = CreateCompilationWithMscorlib461(new[] { source },
                new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) },
                options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
                // (9,47): error CS8852: Init-only property or indexer 'ITest28.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         var x2 = new UsePia() { Property2 = { Property = 43 } };
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "Property").WithArguments("ITest28.Property").WithLocation(9, 47)
                );
        }

        [Fact, WorkItem(50696, "https://github.com/dotnet/roslyn/issues/50696")]
        public void PickAmbiguousTypeFromCorlib()
        {
            var corlib_cs = @"
namespace System
{
    public class Object { }
    public struct Int32 { }
    public struct Boolean { }
    public class String { }
    public class ValueType { }
    public struct Void { }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public struct Enum { }
    public enum AttributeTargets { }
}
";

            string source = @"
public class C
{
    public int Property { get; init; }
}
";
            var corlibWithoutIsExternalInitRef = CreateEmptyCompilation(corlib_cs, assemblyName: "corlibWithoutIsExternalInit")
                .EmitToImageReference();

            var corlibWithIsExternalInitRef = CreateEmptyCompilation(corlib_cs + IsExternalInitTypeDefinition, assemblyName: "corlibWithIsExternalInit")
                .EmitToImageReference();

            var libWithIsExternalInitRef = CreateEmptyCompilation(IsExternalInitTypeDefinition, references: new[] { corlibWithoutIsExternalInitRef }, assemblyName: "libWithIsExternalInit")
                .EmitToImageReference();

            var libWithIsExternalInitRef2 = CreateEmptyCompilation(IsExternalInitTypeDefinition, references: new[] { corlibWithoutIsExternalInitRef }, assemblyName: "libWithIsExternalInit2")
                .EmitToImageReference();

            {
                // type in source
                var comp = CreateEmptyCompilation(new[] { source, IsExternalInitTypeDefinition }, references: new[] { corlibWithoutIsExternalInitRef }, assemblyName: "source");
                comp.VerifyEmitDiagnostics();
                verify(comp, "source");
            }

            {
                // type in library
                var comp = CreateEmptyCompilation(new[] { source }, references: new[] { corlibWithoutIsExternalInitRef, libWithIsExternalInitRef }, assemblyName: "source");
                comp.VerifyEmitDiagnostics();
                verify(comp, "libWithIsExternalInit");
            }

            {
                // type in corlib and in source
                var comp = CreateEmptyCompilation(new[] { source, IsExternalInitTypeDefinition }, references: new[] { corlibWithIsExternalInitRef }, assemblyName: "source");
                comp.VerifyEmitDiagnostics();
                verify(comp, "source");
            }

            {
                // type in corlib, in library and in source
                var comp = CreateEmptyCompilation(new[] { source, IsExternalInitTypeDefinition }, references: new[] { corlibWithIsExternalInitRef, libWithIsExternalInitRef }, assemblyName: "source");
                comp.VerifyEmitDiagnostics();
                verify(comp, "source");
            }

            {
                // type in corlib and in two libraries
                var comp = CreateEmptyCompilation(source, references: new[] { corlibWithIsExternalInitRef, libWithIsExternalInitRef, libWithIsExternalInitRef2 });
                comp.VerifyEmitDiagnostics();
                verify(comp, "corlibWithIsExternalInit");
            }

            {
                // type in corlib and in two libraries (corlib in middle)
                var comp = CreateEmptyCompilation(source, references: new[] { libWithIsExternalInitRef, corlibWithIsExternalInitRef, libWithIsExternalInitRef2 });
                comp.VerifyEmitDiagnostics();
                verify(comp, "corlibWithIsExternalInit");
            }

            {
                // type in corlib and in two libraries (corlib last)
                var comp = CreateEmptyCompilation(source, references: new[] { libWithIsExternalInitRef, libWithIsExternalInitRef2, corlibWithIsExternalInitRef });
                comp.VerifyEmitDiagnostics();
                verify(comp, "corlibWithIsExternalInit");
            }

            {
                // type in corlib and in two libraries, but flag is set
                var comp = CreateEmptyCompilation(source, references: new[] { corlibWithIsExternalInitRef, libWithIsExternalInitRef, libWithIsExternalInitRef2 },
                    options: TestOptions.DebugDll.WithTopLevelBinderFlags(BinderFlags.IgnoreCorLibraryDuplicatedTypes));
                comp.VerifyEmitDiagnostics(
                    // (4,32): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
                    //     public int Property { get; init; }
                    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "init").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(4, 32)
                    );
            }

            {
                // type in two libraries
                var comp = CreateEmptyCompilation(source, references: new[] { corlibWithoutIsExternalInitRef, libWithIsExternalInitRef, libWithIsExternalInitRef2 });
                comp.VerifyEmitDiagnostics(
                    // (4,32): error CS018: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
                    //     public int Property { get; init; }
                    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "init").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(4, 32)
                    );
            }

            {
                // type in two libraries, but flag is set
                var comp = CreateEmptyCompilation(source, references: new[] { corlibWithoutIsExternalInitRef, libWithIsExternalInitRef, libWithIsExternalInitRef2 },
                    options: TestOptions.DebugDll.WithTopLevelBinderFlags(BinderFlags.IgnoreCorLibraryDuplicatedTypes));
                comp.VerifyEmitDiagnostics(
                    // (4,32): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
                    //     public int Property { get; init; }
                    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "init").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(4, 32)
                    );
            }

            {
                // type in corlib and in a library
                var comp = CreateEmptyCompilation(source, references: new[] { corlibWithIsExternalInitRef, libWithIsExternalInitRef });
                comp.VerifyEmitDiagnostics();
                verify(comp, "corlibWithIsExternalInit");
            }

            {
                // type in corlib and in a library (reverse order)
                var comp = CreateEmptyCompilation(source, references: new[] { libWithIsExternalInitRef, corlibWithIsExternalInitRef });
                comp.VerifyEmitDiagnostics();
                verify(comp, "corlibWithIsExternalInit");
            }

            {
                // type in corlib and in a library, but flag is set
                var comp = CreateEmptyCompilation(source, references: new[] { corlibWithIsExternalInitRef, libWithIsExternalInitRef },
                    options: TestOptions.DebugDll.WithTopLevelBinderFlags(BinderFlags.IgnoreCorLibraryDuplicatedTypes));
                comp.VerifyEmitDiagnostics();
                Assert.Equal("libWithIsExternalInit", comp.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_IsExternalInit).ContainingAssembly.Name);
                Assert.Equal("corlibWithIsExternalInit", comp.GetTypeByMetadataName("System.Runtime.CompilerServices.IsExternalInit").ContainingAssembly.Name);
            }

            static void verify(CSharpCompilation comp, string expectedAssemblyName)
            {
                var modifier = ((SourcePropertySymbol)comp.GlobalNamespace.GetMember("C.Property")).SetMethod.ReturnTypeWithAnnotations.CustomModifiers.Single();
                Assert.Equal(expectedAssemblyName, modifier.Modifier.ContainingAssembly.Name);

                Assert.Equal(expectedAssemblyName, comp.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_IsExternalInit).ContainingAssembly.Name);
                Assert.Equal(expectedAssemblyName, comp.GetTypeByMetadataName("System.Runtime.CompilerServices.IsExternalInit").ContainingAssembly.Name);
            }
        }

        [Theory, WorkItem(67079, "https://github.com/dotnet/roslyn/issues/67079")]
        [CombinatorialData]
        public void DoNotPickTypeFromSourceWithFileModifier(bool useCompilationReference)
        {
            var corlib_cs = """
                namespace System
                {
                    public class Object { }
                    public struct Int32 { }
                    public struct Boolean { }
                    public class String { }
                    public class ValueType { }
                    public struct Void { }
                    public class Attribute { }
                    public class AttributeUsageAttribute : Attribute
                    {
                        public AttributeUsageAttribute(AttributeTargets t) { }
                        public bool AllowMultiple { get; set; }
                        public bool Inherited { get; set; }
                    }
                    public struct Enum { }
                    public enum AttributeTargets { }
                }
                """;

            var source = """
                namespace System.Runtime.CompilerServices
                {
                    file class IsExternalInit {}
                }

                public class C
                {
                    public string Property { get; init; }
                }
                """;

            var corlibWithoutIsExternalInitRef = AsReference(CreateEmptyCompilation(corlib_cs), useCompilationReference);
            var corlibWithIsExternalInitRef = AsReference(CreateEmptyCompilation(corlib_cs + IsExternalInitTypeDefinition), useCompilationReference);
            var emitOptions = EmitOptions.Default.WithRuntimeMetadataVersion("0.0.0.0");

            {
                // proper type in corlib and file type in source
                var comp = CreateEmptyCompilation(source, references: new[] { corlibWithIsExternalInitRef });
                comp.VerifyEmitDiagnostics(emitOptions);
                var modifier = ((SourcePropertySymbol)comp.GlobalNamespace.GetMember("C.Property")).SetMethod.ReturnTypeWithAnnotations.CustomModifiers.Single();
                Assert.False(modifier.Modifier.IsFileLocal);
            }

            {
                // no type in corlib and file type in source
                var comp = CreateEmptyCompilation(source, references: new[] { corlibWithoutIsExternalInitRef });
                comp.VerifyEmitDiagnostics(emitOptions,
                    // (8,35): error CS018: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
                    //     public int Property { get; init; }
                    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "init").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(8, 35)
                    );
                var modifier = ((SourcePropertySymbol)comp.GlobalNamespace.GetMember("C.Property")).SetMethod.ReturnTypeWithAnnotations.CustomModifiers.Single();
                Assert.False(modifier.Modifier.IsFileLocal);
            }
        }
    }
}
