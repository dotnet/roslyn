// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class RefStructInterfacesTests : CSharpTestBase
    {
        // PROTOTYPE(RefStructInterfaces): Switch to supporting target framework once we have its ref assemblies.
        private static readonly TargetFramework s_targetFrameworkSupportingByRefLikeGenerics = TargetFramework.Net80;

        [Theory]
        [CombinatorialData]
        public void UnscopedRefInInterface_Method_01(bool isVirtual)
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    [UnscopedRef]
    " + (isVirtual ? "virtual " : "") + @" ref int M()" + (isVirtual ? " => throw null" : "") + @";
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify, verify: ExecutionConditionUtil.IsMonoOrCoreClr || !isVirtual ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                Assert.True(m.GlobalNamespace.GetMember<MethodSymbol>("I.M").HasUnscopedRefAttribute);
            }

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (6,6): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     [UnscopedRef]
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(6, 6)
                );
        }

        [Fact]
        public void UnscopedRefInInterface_Method_02()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    [UnscopedRef]
    ref int M()
    {
        throw null;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                Assert.True(m.GlobalNamespace.GetMember<MethodSymbol>("I.M").HasUnscopedRefAttribute);
            }

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (6,6): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     [UnscopedRef]
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(6, 6)
                );
        }

        [Fact]
        public void UnscopedRefInInterface_Method_03()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    [UnscopedRef]
    sealed ref int M()
    {
        throw null;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            comp.VerifyDiagnostics(
                // (6,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                //     [UnscopedRef]
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(6, 6)
                );

            Assert.False(comp.GetMember<MethodSymbol>("I.M").HasUnscopedRefAttribute);
        }

        [Fact]
        public void UnscopedRefInInterface_Method_04()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    [UnscopedRef]
    abstract static ref int M();
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            comp.VerifyDiagnostics(
                // (6,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                //     [UnscopedRef]
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(6, 6)
                );

            Assert.False(comp.GetMember<MethodSymbol>("I.M").HasUnscopedRefAttribute);
        }

        [Theory]
        [CombinatorialData]
        public void UnscopedRefInInterface_Property_01(bool isVirtual)
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    [UnscopedRef]
    " + (isVirtual ? "virtual " : "") + @" ref int P { get" + (isVirtual ? " => throw null" : "") + @"; }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify, verify: ExecutionConditionUtil.IsMonoOrCoreClr || !isVirtual ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("I.P");
                Assert.True(propertySymbol.HasUnscopedRefAttribute);
                Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
            }

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (6,6): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     [UnscopedRef]
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(6, 6)
                );
        }

        [Fact]
        public void UnscopedRefInInterface_Property_02()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    [UnscopedRef]
    ref int P => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("I.P");
                Assert.True(propertySymbol.HasUnscopedRefAttribute);
                Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
            }

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (6,6): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     [UnscopedRef]
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(6, 6)
                );
        }

        [Fact]
        public void UnscopedRefInInterface_Property_03()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    [UnscopedRef]
    sealed ref int P => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            comp.VerifyDiagnostics(
                // (6,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                //     [UnscopedRef]
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(6, 6)
                );

            PropertySymbol propertySymbol = comp.GetMember<PropertySymbol>("I.P");
            Assert.False(propertySymbol.HasUnscopedRefAttribute);
            Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
        }

        [Fact]
        public void UnscopedRefInInterface_Property_04()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    [UnscopedRef]
    abstract static ref int P { get; }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            comp.VerifyDiagnostics(
                // (6,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                //     [UnscopedRef]
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(6, 6)
                );

            PropertySymbol propertySymbol = comp.GetMember<PropertySymbol>("I.P");
            Assert.False(propertySymbol.HasUnscopedRefAttribute);
            Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
        }

        [Fact]
        public void UnscopedRefInInterface_Property_05()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    ref int P
    {
        [UnscopedRef]
        get;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("I.P");
                Assert.False(propertySymbol.HasUnscopedRefAttribute);
                Assert.True(propertySymbol.GetMethod.HasUnscopedRefAttribute);
            }

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (8,10): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         [UnscopedRef]
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(8, 10)
                );
        }

        [Fact]
        public void UnscopedRefInInterface_Property_06()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    ref int P
    {
        [UnscopedRef]
        get
        {
            throw null;
        }
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("I.P");
                Assert.False(propertySymbol.HasUnscopedRefAttribute);
                Assert.True(propertySymbol.GetMethod.HasUnscopedRefAttribute);
            }

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (8,10): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         [UnscopedRef]
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(8, 10)
                );
        }

        [Fact]
        public void UnscopedRefInInterface_Property_07()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    sealed ref int P
    {
        [UnscopedRef]
        get
        {
            throw null;
        }
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            comp.VerifyDiagnostics(
                // (8,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                //         [UnscopedRef]
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(8, 10)
                );

            PropertySymbol propertySymbol = comp.GetMember<PropertySymbol>("I.P");
            Assert.False(propertySymbol.HasUnscopedRefAttribute);
            Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
        }

        [Fact]
        public void UnscopedRefInInterface_Property_08()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    abstract static ref int P
    {
        [UnscopedRef]
        get;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            comp.VerifyDiagnostics(
                // (8,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                //         [UnscopedRef]
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(8, 10)
                );

            PropertySymbol propertySymbol = comp.GetMember<PropertySymbol>("I.P");
            Assert.False(propertySymbol.HasUnscopedRefAttribute);
            Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
        }

        [Theory]
        [CombinatorialData]
        public void UnscopedRefInInterface_Indexer_01(bool isVirtual)
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    [UnscopedRef]
    " + (isVirtual ? "virtual " : "") + @" ref int this[int i]  { get" + (isVirtual ? " => throw null" : "") + @"; }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify, verify: ExecutionConditionUtil.IsMonoOrCoreClr || !isVirtual ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("I." + WellKnownMemberNames.Indexer);
                Assert.True(propertySymbol.HasUnscopedRefAttribute);
                Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
            }

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (6,6): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     [UnscopedRef]
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(6, 6)
                );
        }

        [Fact]
        public void UnscopedRefInInterface_Indexer_02()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    [UnscopedRef]
    ref int this[int i] => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("I." + WellKnownMemberNames.Indexer);
                Assert.True(propertySymbol.HasUnscopedRefAttribute);
                Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
            }

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (6,6): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     [UnscopedRef]
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(6, 6)
                );
        }

        [Fact]
        public void UnscopedRefInInterface_Indexer_03()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    [UnscopedRef]
    sealed ref int this[int i] => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            comp.VerifyDiagnostics(
                // (6,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                //     [UnscopedRef]
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(6, 6)
                );

            PropertySymbol propertySymbol = comp.GetMember<PropertySymbol>("I." + WellKnownMemberNames.Indexer);
            Assert.False(propertySymbol.HasUnscopedRefAttribute);
            Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
        }

        [Fact]
        public void UnscopedRefInInterface_Indexer_04()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    [UnscopedRef]
    abstract static ref int this[int i] { get; }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            comp.VerifyDiagnostics(
                // (7,29): error CS0106: The modifier 'static' is not valid for this item
                //     abstract static ref int this[int i] { get; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(7, 29)
                );

            PropertySymbol propertySymbol = comp.GetMember<PropertySymbol>("I." + WellKnownMemberNames.Indexer);
            Assert.False(propertySymbol.IsStatic);
            Assert.True(propertySymbol.HasUnscopedRefAttribute);
            Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
        }

        [Fact]
        public void UnscopedRefInInterface_Indexer_05()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    ref int this[int i]
    {
        [UnscopedRef]
        get;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("I." + WellKnownMemberNames.Indexer);
                Assert.False(propertySymbol.HasUnscopedRefAttribute);
                Assert.True(propertySymbol.GetMethod.HasUnscopedRefAttribute);
            }

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (8,10): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         [UnscopedRef]
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(8, 10)
                );
        }

        [Fact]
        public void UnscopedRefInInterface_Indexer_06()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    ref int this[int i]
    {
        [UnscopedRef]
        get
        {
            throw null;
        }
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("I." + WellKnownMemberNames.Indexer);
                Assert.False(propertySymbol.HasUnscopedRefAttribute);
                Assert.True(propertySymbol.GetMethod.HasUnscopedRefAttribute);
            }

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (8,10): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         [UnscopedRef]
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(8, 10)
                );
        }

        [Fact]
        public void UnscopedRefInInterface_Indexer_07()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    sealed ref int this[int i]
    {
        [UnscopedRef]
        get
        {
            throw null;
        }
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            comp.VerifyDiagnostics(
                // (8,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                //         [UnscopedRef]
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(8, 10)
                );

            PropertySymbol propertySymbol = comp.GetMember<PropertySymbol>("I." + WellKnownMemberNames.Indexer);
            Assert.False(propertySymbol.HasUnscopedRefAttribute);
            Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
        }

        [Fact]
        public void UnscopedRefInInterface_Indexer_08()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    abstract static ref int this[int i]
    {
        [UnscopedRef]
        get;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            comp.VerifyDiagnostics(
                // (6,29): error CS0106: The modifier 'static' is not valid for this item
                //     abstract static ref int this[int i]
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(6, 29)
                );

            PropertySymbol propertySymbol = comp.GetMember<PropertySymbol>("I." + WellKnownMemberNames.Indexer);
            Assert.False(propertySymbol.IsStatic);
            Assert.False(propertySymbol.HasUnscopedRefAttribute);
            Assert.True(propertySymbol.GetMethod.HasUnscopedRefAttribute);
        }

        [Fact]
        public void UnscopedRefInImplementation_Method_01()
        {
            var src1 = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    [UnscopedRef]
    ref int M();
}
";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net80);
            MetadataReference[] comp1Refs = [comp1.EmitToImageReference(), comp1.ToMetadataReference()];

            var src2 = @"
using System.Diagnostics.CodeAnalysis;

class C : I
{
    [UnscopedRef]
    public ref int M()
    {
        throw null;
    }
}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp2 = CreateCompilation(src2, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                comp2.VerifyDiagnostics(
                    // (6,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                    //     [UnscopedRef]
                    Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(6, 6)
                    );
                Assert.False(comp2.GetMember<MethodSymbol>("C.M").HasUnscopedRefAttribute);
            }

            var src3 = @"
using System.Diagnostics.CodeAnalysis;

class C : I
{
    [UnscopedRef]
    ref int I.M()
    {
        throw null;
    }
}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp3 = CreateCompilation(src3, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                comp3.VerifyDiagnostics(
                    // (6,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                    //     [UnscopedRef]
                    Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(6, 6)
                    );
                Assert.False(comp3.GetMember<MethodSymbol>("C.I.M").HasUnscopedRefAttribute);
            }

            var src4 = @"
class C1 : I
{
    int f = 0;
    public ref int M()
    {
        return ref f;
    }
}

class C2 : I
{
    int f = 0;
    ref int I.M()
    {
        return ref f;
    }
}

class C3
{
    int f = 0;
    public ref int M()
    {
        return ref f;
    }
}

class C4 : C3, I {}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp4 = CreateCompilation(src4, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                CompileAndVerify(comp4, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                void verify(ModuleSymbol m)
                {
                    Assert.False(m.GlobalNamespace.GetMember<MethodSymbol>("C1.M").HasUnscopedRefAttribute);
                    Assert.False(m.GlobalNamespace.GetMember<MethodSymbol>("C2.I.M").HasUnscopedRefAttribute);
                    Assert.False(m.GlobalNamespace.GetMember<MethodSymbol>("C3.M").HasUnscopedRefAttribute);
                }
            }

            var src5 = @"
using System.Diagnostics.CodeAnalysis;

interface C : I
{
    [UnscopedRef]
    ref int I.M()
    {
        throw null;
    }
}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp5 = CreateCompilation(src5, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                comp5.VerifyDiagnostics(
                    // (6,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                    //     [UnscopedRef]
                    Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(6, 6)
                    );
                Assert.False(comp5.GetMember<MethodSymbol>("C.I.M").HasUnscopedRefAttribute);
            }

            var src6 = @"
interface C : I
{
    ref int I.M()
    {
        throw null;
    }
}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp6 = CreateCompilation(src6, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                CompileAndVerify(comp6, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                void verify(ModuleSymbol m)
                {
                    Assert.False(m.GlobalNamespace.GetMember<MethodSymbol>("C.I.M").HasUnscopedRefAttribute);
                }
            }

            var src7 = @"
using System.Diagnostics.CodeAnalysis;

public struct C : I
{
    public int f;

    [UnscopedRef]
    public ref int M()
    {
        return ref f;
    }
}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp7 = CreateCompilation(src7, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                CompileAndVerify(comp7, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                void verify(ModuleSymbol m)
                {
                    Assert.True(m.GlobalNamespace.GetMember<MethodSymbol>("C.M").HasUnscopedRefAttribute);
                }

                CreateCompilation(src7, references: [comp1Ref], targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

                CreateCompilation(src7, references: [comp1Ref], targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                    // (8,6): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     [UnscopedRef]
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(8, 6)
                    );
            }

            var src8 = @"
using System.Diagnostics.CodeAnalysis;

public struct C : I
{
    public int f;

    [UnscopedRef]
    ref int I.M()
    {
        return ref f;
    }
}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp8 = CreateCompilation(src8, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                CompileAndVerify(comp8, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                void verify(ModuleSymbol m)
                {
                    Assert.True(m.GlobalNamespace.GetMember<MethodSymbol>("C.I.M").HasUnscopedRefAttribute);
                }

                CreateCompilation(src8, references: [comp1Ref], targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

                CreateCompilation(src8, references: [comp1Ref], targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                    // (8,6): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     [UnscopedRef]
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(8, 6)
                    );
            }

            var src9 = @"
public struct C : I
{
    public ref int M()
    {
        throw null;
    }
}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp9 = CreateCompilation(src9, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                CompileAndVerify(comp9, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                void verify(ModuleSymbol m)
                {
                    Assert.False(m.GlobalNamespace.GetMember<MethodSymbol>("C.M").HasUnscopedRefAttribute);
                }
            }

            var src10 = @"
public struct C : I
{
    ref int I.M()
    {
        throw null;
    }
}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp10 = CreateCompilation(src10, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                CompileAndVerify(comp10, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                void verify(ModuleSymbol m)
                {
                    Assert.False(m.GlobalNamespace.GetMember<MethodSymbol>("C.I.M").HasUnscopedRefAttribute);
                }
            }

            var src11 = @"
public struct C : I
{
    public int f;

    public ref int M()
    {
        return ref f;
    }
}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp11 = CreateCompilation(src11, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                comp11.VerifyDiagnostics(
                    // (8,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                    //         return ref f;
                    Diagnostic(ErrorCode.ERR_RefReturnStructThis, "f").WithLocation(8, 20)
                    );
            }

            var src12 = @"
public struct C : I
{
    public int f;

    ref int I.M()
    {
        return ref f;
    }
}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp12 = CreateCompilation(src12, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                comp12.VerifyDiagnostics(
                    // (8,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                    //         return ref f;
                    Diagnostic(ErrorCode.ERR_RefReturnStructThis, "f").WithLocation(8, 20)
                    );
            }
        }

        [Fact]
        public void UnscopedRefInImplementation_Method_02()
        {
            var src1 = @"
public interface I
{
    ref int M();
}
";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net80);
            MetadataReference[] comp1Refs = [comp1.EmitToImageReference(), comp1.ToMetadataReference()];

            var src7 = @"
using System.Diagnostics.CodeAnalysis;

public struct C : I
{
    public int f;

    [UnscopedRef]
    public ref int M()
    {
        return ref f;
    }
}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp7 = CreateCompilation(src7, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                comp7.VerifyDiagnostics(
                    // (9,20): error CS9102: UnscopedRefAttribute cannot be applied to an interface implementation because implemented member 'I.M()' doesn't have this attribute.
                    //     public ref int M()
                    Diagnostic(ErrorCode.ERR_UnscopedRefAttributeInterfaceImplementation, "M").WithArguments("I.M()").WithLocation(9, 20)
                    );

                Assert.True(comp7.GetMember<MethodSymbol>("C.M").HasUnscopedRefAttribute);
            }

            var src8 = @"
using System.Diagnostics.CodeAnalysis;

public struct C : I
{
    public int f;

    [UnscopedRef]
    ref int I.M()
    {
        return ref f;
    }
}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp8 = CreateCompilation(src8, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                comp8.VerifyDiagnostics(
                    // (9,15): error CS9102: UnscopedRefAttribute cannot be applied to an interface implementation because implemented member 'I.M()' doesn't have this attribute.
                    //     ref int I.M()
                    Diagnostic(ErrorCode.ERR_UnscopedRefAttributeInterfaceImplementation, "M").WithArguments("I.M()").WithLocation(9, 15)
                    );

                Assert.True(comp8.GetMember<MethodSymbol>("C.I.M").HasUnscopedRefAttribute);
            }
        }

        [Fact]
        public void UnscopedRefInImplementation_Method_03()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
#line 100
    [UnscopedRef]
    ref int M();
}

public struct C : I
{
    public int f;

#line 200
    [UnscopedRef]
    public ref int M()
    {
        return ref f;
    }
}
";

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (100,6): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     [UnscopedRef]
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(100, 6)
                );
        }

        [Theory]
        [CombinatorialData]
        public void UnscopedRefInImplementation_Property_01(bool onInterfaceProperty, bool onInterfaceGet, bool onImplementationProperty, bool onImplementationGet)
        {
            if (!onInterfaceProperty && !onInterfaceGet)
            {
                return;
            }

            var src1 = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    " + (onInterfaceProperty ? "[UnscopedRef]" : "") + @"
    ref int P { " + (onInterfaceGet ? "[UnscopedRef] " : "") + @"get; }
}
";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net80);

            var p = comp1.GetMember<PropertySymbol>("I.P");
            Assert.Equal(onInterfaceProperty, p.HasUnscopedRefAttribute);
            Assert.Equal(onInterfaceGet, p.GetMethod.HasUnscopedRefAttribute);

            MetadataReference[] comp1Refs = [comp1.EmitToImageReference(), comp1.ToMetadataReference()];

            if (onImplementationProperty || onImplementationGet)
            {
                var src2 = @"
using System.Diagnostics.CodeAnalysis;

class C : I
{
#line 100
    " + (onImplementationProperty ? "[UnscopedRef]" : "") + @"
    public ref int P
    {
#line 200
        " + (onImplementationGet ? "[UnscopedRef] " : "") + @"
        get
            => throw null;
    }
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp2 = CreateCompilation(src2, references: [comp1Ref], targetFramework: TargetFramework.Net80);

                    if (onImplementationProperty)
                    {
                        if (onImplementationGet)
                        {
                            comp2.VerifyDiagnostics(
                                // (100,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //     [UnscopedRef]
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(100, 6),
                                // (200,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //         [UnscopedRef] 
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(200, 10)
                                );
                        }
                        else
                        {
                            comp2.VerifyDiagnostics(
                                // (100,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //     [UnscopedRef]
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(100, 6)
                                );
                        }
                    }
                    else
                    {
                        comp2.VerifyDiagnostics(
                            // (200,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                            //         [UnscopedRef] 
                            Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(200, 10)
                            );
                    }

                    PropertySymbol propertySymbol = comp2.GetMember<PropertySymbol>("C.P");
                    Assert.False(propertySymbol.HasUnscopedRefAttribute);
                    Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
                }

                var src3 = @"
using System.Diagnostics.CodeAnalysis;

class C : I
{
#line 100
    " + (onImplementationProperty ? "[UnscopedRef]" : "") + @"
    ref int I. P
    {
#line 200
        " + (onImplementationGet ? "[UnscopedRef] " : "") + @"
        get
            => throw null;
    }
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp3 = CreateCompilation(src3, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    if (onImplementationProperty)
                    {
                        if (onImplementationGet)
                        {
                            comp3.VerifyDiagnostics(
                                // (100,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //     [UnscopedRef]
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(100, 6),
                                // (200,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //         [UnscopedRef] 
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(200, 10)
                                );
                        }
                        else
                        {
                            comp3.VerifyDiagnostics(
                                // (100,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //     [UnscopedRef]
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(100, 6)
                                );
                        }
                    }
                    else
                    {
                        comp3.VerifyDiagnostics(
                            // (200,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                            //         [UnscopedRef] 
                            Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(200, 10)
                            );
                    }

                    PropertySymbol propertySymbol = comp3.GetMember<PropertySymbol>("C.I.P");
                    Assert.False(propertySymbol.HasUnscopedRefAttribute);
                    Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
                }
            }

            if (!onImplementationProperty && !onImplementationGet)
            {
                var src4 = @"
class C1 : I
{
    int f = 0;
    public ref int P 
    { get{
        return ref f;
    }}
}

class C2 : I
{
    int f = 0;
    ref int I.P 
    { get{
        return ref f;
    }}
}

class C3
{
    int f = 0;
    public ref int P 
    { get{
        return ref f;
    }}
}

class C4 : C3, I {}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp4 = CreateCompilation(src4, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    CompileAndVerify(comp4, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                    void verify(ModuleSymbol m)
                    {
                        PropertySymbol c1P = m.GlobalNamespace.GetMember<PropertySymbol>("C1.P");
                        Assert.False(c1P.HasUnscopedRefAttribute);
                        Assert.False(c1P.GetMethod.HasUnscopedRefAttribute);
                        PropertySymbol c2P = m.GlobalNamespace.GetMember<PropertySymbol>("C2.I.P");
                        Assert.False(c2P.HasUnscopedRefAttribute);
                        Assert.False(c2P.GetMethod.HasUnscopedRefAttribute);
                        PropertySymbol c3P = m.GlobalNamespace.GetMember<PropertySymbol>("C3.P");
                        Assert.False(c3P.HasUnscopedRefAttribute);
                        Assert.False(c3P.GetMethod.HasUnscopedRefAttribute);
                    }
                }
            }

            if (onImplementationProperty || onImplementationGet)
            {
                var src5 = @"
using System.Diagnostics.CodeAnalysis;

interface C : I
{
#line 100
    " + (onImplementationProperty ? "[UnscopedRef]" : "") + @"
    ref int I.P
    {
#line 200
        " + (onImplementationGet ? "[UnscopedRef] " : "") + @"
        get
            => throw null;
    }
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp5 = CreateCompilation(src5, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    if (onImplementationProperty)
                    {
                        if (onImplementationGet)
                        {
                            comp5.VerifyDiagnostics(
                                // (100,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //     [UnscopedRef]
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(100, 6),
                                // (200,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //         [UnscopedRef] 
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(200, 10)
                                );
                        }
                        else
                        {
                            comp5.VerifyDiagnostics(
                                // (100,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //     [UnscopedRef]
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(100, 6)
                                );
                        }
                    }
                    else
                    {
                        comp5.VerifyDiagnostics(
                            // (200,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                            //         [UnscopedRef] 
                            Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(200, 10)
                            );
                    }

                    PropertySymbol propertySymbol = comp5.GetMember<PropertySymbol>("C.I.P");
                    Assert.False(propertySymbol.HasUnscopedRefAttribute);
                    Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
                }
            }

            if (!onImplementationProperty && !onImplementationGet)
            {
                var src6 = @"
interface C : I
{
    ref int I.P => throw null;
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp6 = CreateCompilation(src6, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    CompileAndVerify(comp6, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                    void verify(ModuleSymbol m)
                    {
                        PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("C.I.P");
                        Assert.False(propertySymbol.HasUnscopedRefAttribute);
                        Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
                    }
                }
            }

            if (onImplementationProperty || onImplementationGet)
            {
                var src7 = @"
using System.Diagnostics.CodeAnalysis;

public struct C : I
{
    public int f;

#line 100
    " + (onImplementationProperty ? "[UnscopedRef]" : "") + @"
    public ref int P 
    {
#line 200
        " + (onImplementationGet ? "[UnscopedRef] " : "") + @"
        get
        {
            return ref f;
        }
    }
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp7 = CreateCompilation(src7, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    CompileAndVerify(comp7, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                    void verify(ModuleSymbol m)
                    {
                        PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("C.P");
                        Assert.Equal(onImplementationProperty, propertySymbol.HasUnscopedRefAttribute);
                        Assert.Equal(onImplementationGet, propertySymbol.GetMethod.HasUnscopedRefAttribute);
                    }

                    CreateCompilation(src7, references: [comp1Ref], targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

                    comp7 = CreateCompilation(src7, references: [comp1Ref], targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12);
                    if (onImplementationGet)
                    {
                        comp7.VerifyDiagnostics(
                            // (200,10): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                            //         [UnscopedRef] 
                            Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(200, 10)
                            );
                    }
                    else
                    {
                        comp7.VerifyDiagnostics(
                            // (100,6): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                            //     [UnscopedRef]
                            Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(100, 6)
                            );
                    }
                }

                var src8 = @"
using System.Diagnostics.CodeAnalysis;

public struct C : I
{
    public int f;
#line 100
    " + (onImplementationProperty ? "[UnscopedRef]" : "") + @"
    ref int I.P 
    {
#line 200
        " + (onImplementationGet ? "[UnscopedRef] " : "") + @"
        get
        {
            return ref f;
        }
    }
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp8 = CreateCompilation(src8, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    CompileAndVerify(comp8, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                    void verify(ModuleSymbol m)
                    {
                        PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("C.I.P");
                        Assert.Equal(onImplementationProperty, propertySymbol.HasUnscopedRefAttribute);
                        Assert.Equal(onImplementationGet, propertySymbol.GetMethod.HasUnscopedRefAttribute);
                    }

                    CreateCompilation(src8, references: [comp1Ref], targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

                    comp8 = CreateCompilation(src8, references: [comp1Ref], targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12);
                    if (onImplementationProperty)
                    {
                        if (onImplementationGet)
                        {
                            comp8.VerifyDiagnostics(
                                // (100,6): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                //     [UnscopedRef]
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(100, 6),
                                // (200,10): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                //         [UnscopedRef] 
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(200, 10)
                                );
                        }
                        else
                        {
                            comp8.VerifyDiagnostics(
                                // (100,6): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                //     [UnscopedRef]
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(100, 6)
                                );
                        }
                    }
                    else
                    {
                        comp8.VerifyDiagnostics(
                            // (200,10): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                            //         [UnscopedRef] 
                            Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(200, 10)
                            );
                    }
                }
            }

            if (!onImplementationProperty && !onImplementationGet)
            {
                var src9 = @"
public struct C : I
{
    public ref int P => throw null;
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp9 = CreateCompilation(src9, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    CompileAndVerify(comp9, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                    void verify(ModuleSymbol m)
                    {
                        PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("C.P");
                        Assert.False(propertySymbol.HasUnscopedRefAttribute);
                        Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
                    }
                }

                var src10 = @"
public struct C : I
{
    ref int I.P => throw null;
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp10 = CreateCompilation(src10, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    CompileAndVerify(comp10, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                    void verify(ModuleSymbol m)
                    {
                        PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("C.I.P");
                        Assert.False(propertySymbol.HasUnscopedRefAttribute);
                        Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
                    }
                }

                var src11 = @"
public struct C : I
{
    public int f;

    public ref int P 
    { get{
        return ref f;
    }}
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp11 = CreateCompilation(src11, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    comp11.VerifyDiagnostics(
                        // (8,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                        //         return ref f;
                        Diagnostic(ErrorCode.ERR_RefReturnStructThis, "f").WithLocation(8, 20)
                        );
                }

                var src12 = @"
public struct C : I
{
    public int f;

    ref int I.P 
    { get{
        return ref f;
    }}
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp12 = CreateCompilation(src12, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    comp12.VerifyDiagnostics(
                        // (8,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                        //         return ref f;
                        Diagnostic(ErrorCode.ERR_RefReturnStructThis, "f").WithLocation(8, 20)
                        );
                }
            }
        }

        [Theory]
        [CombinatorialData]
        public void UnscopedRefInImplementation_Property_02(bool onProperty, bool onGet)
        {
            if (!onProperty && !onGet)
            {
                return;
            }

            var src1 = @"
public interface I
{
    ref int P { get; }
}
";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net80);
            MetadataReference[] comp1Refs = [comp1.EmitToImageReference(), comp1.ToMetadataReference()];

            var src7 = @"
using System.Diagnostics.CodeAnalysis;

public struct C : I
{
    public int f;

    " + (onProperty ? "[UnscopedRef]" : "") + @"
    public ref int P 
    {
#line 200
        " + (onGet ? "[UnscopedRef] " : "") + @"
        get
        {
            return ref f;
        }
    }
}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp7 = CreateCompilation(src7, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                comp7.VerifyDiagnostics(
                    // (201,9): error CS9102: UnscopedRefAttribute cannot be applied to an interface implementation because implemented member 'I.P.get' doesn't have this attribute.
                    //         get
                    Diagnostic(ErrorCode.ERR_UnscopedRefAttributeInterfaceImplementation, "get").WithArguments("I.P.get").WithLocation(201, 9)
                    );

                PropertySymbol propertySymbol = comp7.GetMember<PropertySymbol>("C.P");
                Assert.Equal(onProperty, propertySymbol.HasUnscopedRefAttribute);
                Assert.Equal(onGet, propertySymbol.GetMethod.HasUnscopedRefAttribute);
            }

            var src8 = @"
using System.Diagnostics.CodeAnalysis;

public struct C : I
{
    public int f;

    " + (onProperty ? "[UnscopedRef]" : "") + @"
    ref int I.P 
    {
#line 200
        " + (onGet ? "[UnscopedRef] " : "") + @"
        get
        {
            return ref f;
        }
    }
}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp8 = CreateCompilation(src8, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                comp8.VerifyDiagnostics(
                    // (201,9): error CS9102: UnscopedRefAttribute cannot be applied to an interface implementation because implemented member 'I.P.get' doesn't have this attribute.
                    //         get
                    Diagnostic(ErrorCode.ERR_UnscopedRefAttributeInterfaceImplementation, "get").WithArguments("I.P.get").WithLocation(201, 9)
                    );

                PropertySymbol propertySymbol = comp8.GetMember<PropertySymbol>("C.I.P");
                Assert.Equal(onProperty, propertySymbol.HasUnscopedRefAttribute);
                Assert.Equal(onGet, propertySymbol.GetMethod.HasUnscopedRefAttribute);
            }
        }

        [Theory]
        [CombinatorialData]
        public void UnscopedRefInImplementation_Indexer_01(bool onInterfaceProperty, bool onInterfaceGet, bool onImplementationProperty, bool onImplementationGet)
        {
            if (!onInterfaceProperty && !onInterfaceGet)
            {
                return;
            }

            var src1 = @"
using System.Diagnostics.CodeAnalysis;

public interface I
{
    " + (onInterfaceProperty ? "[UnscopedRef]" : "") + @"
    ref int this[int i] { " + (onInterfaceGet ? "[UnscopedRef] " : "") + @"get; }
}
";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net80);

            var p = comp1.GetMember<PropertySymbol>("I." + WellKnownMemberNames.Indexer);
            Assert.Equal(onInterfaceProperty, p.HasUnscopedRefAttribute);
            Assert.Equal(onInterfaceGet, p.GetMethod.HasUnscopedRefAttribute);

            MetadataReference[] comp1Refs = [comp1.EmitToImageReference(), comp1.ToMetadataReference()];

            if (onImplementationProperty || onImplementationGet)
            {
                var src2 = @"
using System.Diagnostics.CodeAnalysis;

class C : I
{
#line 100
    " + (onImplementationProperty ? "[UnscopedRef]" : "") + @"
    public ref int this[int i]
    {
#line 200
        " + (onImplementationGet ? "[UnscopedRef] " : "") + @"
        get
            => throw null;
    }
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp2 = CreateCompilation(src2, references: [comp1Ref], targetFramework: TargetFramework.Net80);

                    if (onImplementationProperty)
                    {
                        if (onImplementationGet)
                        {
                            comp2.VerifyDiagnostics(
                                // (100,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //     [UnscopedRef]
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(100, 6),
                                // (200,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //         [UnscopedRef] 
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(200, 10)
                                );
                        }
                        else
                        {
                            comp2.VerifyDiagnostics(
                                // (100,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //     [UnscopedRef]
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(100, 6)
                                );
                        }
                    }
                    else
                    {
                        comp2.VerifyDiagnostics(
                            // (200,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                            //         [UnscopedRef] 
                            Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(200, 10)
                            );
                    }

                    PropertySymbol propertySymbol = comp2.GetMember<PropertySymbol>("C." + WellKnownMemberNames.Indexer);
                    Assert.False(propertySymbol.HasUnscopedRefAttribute);
                    Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
                }

                var src3 = @"
using System.Diagnostics.CodeAnalysis;

class C : I
{
#line 100
    " + (onImplementationProperty ? "[UnscopedRef]" : "") + @"
    ref int I. this[int i]
    {
#line 200
        " + (onImplementationGet ? "[UnscopedRef] " : "") + @"
        get
            => throw null;
    }
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp3 = CreateCompilation(src3, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    if (onImplementationProperty)
                    {
                        if (onImplementationGet)
                        {
                            comp3.VerifyDiagnostics(
                                // (100,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //     [UnscopedRef]
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(100, 6),
                                // (200,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //         [UnscopedRef] 
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(200, 10)
                                );
                        }
                        else
                        {
                            comp3.VerifyDiagnostics(
                                // (100,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //     [UnscopedRef]
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(100, 6)
                                );
                        }
                    }
                    else
                    {
                        comp3.VerifyDiagnostics(
                            // (200,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                            //         [UnscopedRef] 
                            Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(200, 10)
                            );
                    }

                    PropertySymbol propertySymbol = comp3.GetMember<PropertySymbol>("C.I." + WellKnownMemberNames.Indexer);
                    Assert.False(propertySymbol.HasUnscopedRefAttribute);
                    Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
                }
            }

            if (!onImplementationProperty && !onImplementationGet)
            {
                var src4 = @"
class C1 : I
{
    int f = 0;
    public ref int this[int i] 
    { get{
        return ref f;
    }}
}

class C2 : I
{
    int f = 0;
    ref int I.this[int i] 
    { get{
        return ref f;
    }}
}

class C3
{
    int f = 0;
    public ref int this[int i] 
    { get{
        return ref f;
    }}
}

class C4 : C3, I {}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp4 = CreateCompilation(src4, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    CompileAndVerify(comp4, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                    void verify(ModuleSymbol m)
                    {
                        PropertySymbol c1P = m.GlobalNamespace.GetMember<PropertySymbol>("C1." + WellKnownMemberNames.Indexer);
                        Assert.False(c1P.HasUnscopedRefAttribute);
                        Assert.False(c1P.GetMethod.HasUnscopedRefAttribute);
                        PropertySymbol c2P = m.GlobalNamespace.GetMember<PropertySymbol>("C2.I." + (m is PEModuleSymbol ? "Item" : WellKnownMemberNames.Indexer));
                        Assert.False(c2P.HasUnscopedRefAttribute);
                        Assert.False(c2P.GetMethod.HasUnscopedRefAttribute);
                        PropertySymbol c3P = m.GlobalNamespace.GetMember<PropertySymbol>("C3." + WellKnownMemberNames.Indexer);
                        Assert.False(c3P.HasUnscopedRefAttribute);
                        Assert.False(c3P.GetMethod.HasUnscopedRefAttribute);
                    }
                }
            }

            if (onImplementationProperty || onImplementationGet)
            {
                var src5 = @"
using System.Diagnostics.CodeAnalysis;

interface C : I
{
#line 100
    " + (onImplementationProperty ? "[UnscopedRef]" : "") + @"
    ref int I.this[int i]
    {
#line 200
        " + (onImplementationGet ? "[UnscopedRef] " : "") + @"
        get
            => throw null;
    }
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp5 = CreateCompilation(src5, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    if (onImplementationProperty)
                    {
                        if (onImplementationGet)
                        {
                            comp5.VerifyDiagnostics(
                                // (100,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //     [UnscopedRef]
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(100, 6),
                                // (200,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //         [UnscopedRef] 
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(200, 10)
                                );
                        }
                        else
                        {
                            comp5.VerifyDiagnostics(
                                // (100,6): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                                //     [UnscopedRef]
                                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(100, 6)
                                );
                        }
                    }
                    else
                    {
                        comp5.VerifyDiagnostics(
                            // (200,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
                            //         [UnscopedRef] 
                            Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "UnscopedRef").WithLocation(200, 10)
                            );
                    }

                    PropertySymbol propertySymbol = comp5.GetMember<PropertySymbol>("C.I." + WellKnownMemberNames.Indexer);
                    Assert.False(propertySymbol.HasUnscopedRefAttribute);
                    Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
                }
            }

            if (!onImplementationProperty && !onImplementationGet)
            {
                var src6 = @"
interface C : I
{
    ref int I.this[int i] => throw null;
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp6 = CreateCompilation(src6, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    CompileAndVerify(comp6, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                    void verify(ModuleSymbol m)
                    {
                        PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("C.I." + (m is PEModuleSymbol ? "Item" : WellKnownMemberNames.Indexer));
                        Assert.False(propertySymbol.HasUnscopedRefAttribute);
                        Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
                    }
                }
            }

            if (onImplementationProperty || onImplementationGet)
            {
                var src7 = @"
using System.Diagnostics.CodeAnalysis;

public struct C : I
{
    public int f;

#line 100
    " + (onImplementationProperty ? "[UnscopedRef]" : "") + @"
    public ref int this[int i] 
    {
#line 200
        " + (onImplementationGet ? "[UnscopedRef] " : "") + @"
        get
        {
            return ref f;
        }
    }
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp7 = CreateCompilation(src7, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    CompileAndVerify(comp7, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                    void verify(ModuleSymbol m)
                    {
                        PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("C." + WellKnownMemberNames.Indexer);
                        Assert.Equal(onImplementationProperty, propertySymbol.HasUnscopedRefAttribute);
                        Assert.Equal(onImplementationGet, propertySymbol.GetMethod.HasUnscopedRefAttribute);
                    }

                    CreateCompilation(src7, references: [comp1Ref], targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

                    comp7 = CreateCompilation(src7, references: [comp1Ref], targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12);
                    if (onImplementationGet)
                    {
                        comp7.VerifyDiagnostics(
                            // (200,10): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                            //         [UnscopedRef] 
                            Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(200, 10)
                            );
                    }
                    else
                    {
                        comp7.VerifyDiagnostics(
                            // (100,6): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                            //     [UnscopedRef]
                            Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(100, 6)
                            );
                    }
                }

                var src8 = @"
using System.Diagnostics.CodeAnalysis;

public struct C : I
{
    public int f;
#line 100
    " + (onImplementationProperty ? "[UnscopedRef]" : "") + @"
    ref int I.this[int i] 
    {
#line 200
        " + (onImplementationGet ? "[UnscopedRef] " : "") + @"
        get
        {
            return ref f;
        }
    }
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp8 = CreateCompilation(src8, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    CompileAndVerify(comp8, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                    void verify(ModuleSymbol m)
                    {
                        PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("C.I." + (m is PEModuleSymbol ? "Item" : WellKnownMemberNames.Indexer));
                        Assert.Equal(onImplementationProperty, propertySymbol.HasUnscopedRefAttribute);
                        Assert.Equal(onImplementationGet, propertySymbol.GetMethod.HasUnscopedRefAttribute);
                    }

                    CreateCompilation(src8, references: [comp1Ref], targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

                    comp8 = CreateCompilation(src8, references: [comp1Ref], targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12);
                    if (onImplementationProperty)
                    {
                        if (onImplementationGet)
                        {
                            comp8.VerifyDiagnostics(
                                // (100,6): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                //     [UnscopedRef]
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(100, 6),
                                // (200,10): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                //         [UnscopedRef] 
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(200, 10)
                                );
                        }
                        else
                        {
                            comp8.VerifyDiagnostics(
                                // (100,6): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                //     [UnscopedRef]
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(100, 6)
                                );
                        }
                    }
                    else
                    {
                        comp8.VerifyDiagnostics(
                            // (200,10): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                            //         [UnscopedRef] 
                            Diagnostic(ErrorCode.ERR_FeatureInPreview, "UnscopedRef").WithArguments("ref struct interfaces").WithLocation(200, 10)
                            );
                    }
                }
            }

            if (!onImplementationProperty && !onImplementationGet)
            {
                var src9 = @"
public struct C : I
{
    public ref int this[int i] => throw null;
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp9 = CreateCompilation(src9, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    CompileAndVerify(comp9, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                    void verify(ModuleSymbol m)
                    {
                        PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("C." + WellKnownMemberNames.Indexer);
                        Assert.False(propertySymbol.HasUnscopedRefAttribute);
                        Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
                    }
                }

                var src10 = @"
public struct C : I
{
    ref int I.this[int i] => throw null;
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp10 = CreateCompilation(src10, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    CompileAndVerify(comp10, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.Skipped).VerifyDiagnostics();

                    void verify(ModuleSymbol m)
                    {
                        PropertySymbol propertySymbol = m.GlobalNamespace.GetMember<PropertySymbol>("C.I." + (m is PEModuleSymbol ? "Item" : WellKnownMemberNames.Indexer));
                        Assert.False(propertySymbol.HasUnscopedRefAttribute);
                        Assert.False(propertySymbol.GetMethod.HasUnscopedRefAttribute);
                    }
                }

                var src11 = @"
public struct C : I
{
    public int f;

    public ref int this[int i] 
    { get{
        return ref f;
    }}
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp11 = CreateCompilation(src11, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    comp11.VerifyDiagnostics(
                        // (8,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                        //         return ref f;
                        Diagnostic(ErrorCode.ERR_RefReturnStructThis, "f").WithLocation(8, 20)
                        );
                }

                var src12 = @"
public struct C : I
{
    public int f;

    ref int I.this[int i] 
    { get{
        return ref f;
    }}
}
";

                foreach (var comp1Ref in comp1Refs)
                {
                    var comp12 = CreateCompilation(src12, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                    comp12.VerifyDiagnostics(
                        // (8,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                        //         return ref f;
                        Diagnostic(ErrorCode.ERR_RefReturnStructThis, "f").WithLocation(8, 20)
                        );
                }
            }
        }

        [Theory]
        [CombinatorialData]
        public void UnscopedRefInImplementation_Indexer_02(bool onProperty, bool onGet)
        {
            if (!onProperty && !onGet)
            {
                return;
            }

            var src1 = @"
public interface I
{
    ref int this[int i] { get; }
}
";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net80);
            MetadataReference[] comp1Refs = [comp1.EmitToImageReference(), comp1.ToMetadataReference()];

            var src7 = @"
using System.Diagnostics.CodeAnalysis;

public struct C : I
{
    public int f;

    " + (onProperty ? "[UnscopedRef]" : "") + @"
    public ref int this[int i] 
    {
#line 200
        " + (onGet ? "[UnscopedRef] " : "") + @"
        get
        {
            return ref f;
        }
    }
}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp7 = CreateCompilation(src7, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                comp7.VerifyDiagnostics(
                    // (201,9): error CS9102: UnscopedRefAttribute cannot be applied to an interface implementation because implemented member 'I.P.get' doesn't have this attribute.
                    //         get
                    Diagnostic(ErrorCode.ERR_UnscopedRefAttributeInterfaceImplementation, "get").WithArguments("I.this[int].get").WithLocation(201, 9)
                    );

                PropertySymbol propertySymbol = comp7.GetMember<PropertySymbol>("C." + WellKnownMemberNames.Indexer);
                Assert.Equal(onProperty, propertySymbol.HasUnscopedRefAttribute);
                Assert.Equal(onGet, propertySymbol.GetMethod.HasUnscopedRefAttribute);
            }

            var src8 = @"
using System.Diagnostics.CodeAnalysis;

public struct C : I
{
    public int f;

    " + (onProperty ? "[UnscopedRef]" : "") + @"
    ref int I.this[int i] 
    {
#line 200
        " + (onGet ? "[UnscopedRef] " : "") + @"
        get
        {
            return ref f;
        }
    }
}
";

            foreach (var comp1Ref in comp1Refs)
            {
                var comp8 = CreateCompilation(src8, references: [comp1Ref], targetFramework: TargetFramework.Net80);
                comp8.VerifyDiagnostics(
                    // (201,9): error CS9102: UnscopedRefAttribute cannot be applied to an interface implementation because implemented member 'I.P.get' doesn't have this attribute.
                    //         get
                    Diagnostic(ErrorCode.ERR_UnscopedRefAttributeInterfaceImplementation, "get").WithArguments("I.this[int].get").WithLocation(201, 9)
                    );

                PropertySymbol propertySymbol = comp8.GetMember<PropertySymbol>("C.I." + WellKnownMemberNames.Indexer);
                Assert.Equal(onProperty, propertySymbol.HasUnscopedRefAttribute);
                Assert.Equal(onGet, propertySymbol.GetMethod.HasUnscopedRefAttribute);
            }
        }

        // This is a clone of MethodArgumentsMustMatch_16 from RefFieldTests.cs
        [Fact]
        public void MethodArgumentsMustMatch_16_DirectInterface()
        {
            var source = """
                using System.Diagnostics.CodeAnalysis;
                interface R
                {
                    public ref int FA();
                    [UnscopedRef] public ref int FB();
                }
                class Program
                {
                    static void F1(ref R r1, ref int i1) { }
                    static void F2(ref R r2, [UnscopedRef] ref int i2) { }
                    static void F(ref R x)
                    {
                        R y = default;
                        F1(ref x, ref y.FA());
                        F1(ref x, ref y.FB());
                        F2(ref x, ref y.FA());
                        F2(ref x, ref y.FB()); // 1
                    }
                }
                """;
            var comp = CreateCompilation(new[] { source, UnscopedRefAttributeDefinition });
            comp.VerifyDiagnostics();
        }

        // This is a clone of MethodArgumentsMustMatch_16 from RefFieldTests.cs
        [Fact(Skip = "'allow' is not supported yet")] // PROTOTYPE(RefStructInterfaces): Enable once new constraints are supported
        public void MethodArgumentsMustMatch_16_ConstrainedTypeParameter()
        {
            var source = """
                using System.Diagnostics.CodeAnalysis;
                interface R
                {
                    public ref int FA();
                    [UnscopedRef] public ref int FB();
                }
                class Program<T> where T : allows ref struct, R
                {
                    static void F1(ref T r1, ref int i1) { }
                    static void F2(ref T r2, [UnscopedRef] ref int i2) { }
                    static void F(ref T x)
                    {
                        T y = default;
                        F1(ref x, ref y.FA());
                        F1(ref x, ref y.FB());
                        F2(ref x, ref y.FA());
                        F2(ref x, ref y.FB()); // 1
                    }
                }
                """;
            var comp = CreateCompilation(new[] { source, UnscopedRefAttributeDefinition });
            comp.VerifyDiagnostics(
                // (17,9): error CS8350: This combination of arguments to 'Program.F2(ref R, ref int)' is disallowed because it may expose variables referenced by parameter 'i2' outside of their declaration scope
                //         F2(ref x, ref y.FB()); // 1
                Diagnostic(ErrorCode.ERR_CallArgMixing, "F2(ref x, ref y.FB())").WithArguments("Program.F2(ref R, ref int)", "i2").WithLocation(17, 9),
                // (17,23): error CS8168: Cannot return local 'y' by reference because it is not a ref local
                //         F2(ref x, ref y.FB()); // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "y").WithArguments("y").WithLocation(17, 23));
        }

        // This is a clone of MethodArgumentsMustMatch_16 from RefFieldTests.cs
        [Fact]
        public void MethodArgumentsMustMatch_16_ClassConstrainedTypeParameter()
        {
            var source = """
                using System.Diagnostics.CodeAnalysis;
                interface R
                {
                    public ref int FA();
                    [UnscopedRef] public ref int FB();
                }
                class Program<T> where T : class, R
                {
                    static void F1(ref T r1, ref int i1) { }
                    static void F2(ref T r2, [UnscopedRef] ref int i2) { }
                    static void F(ref T x)
                    {
                        T y = default;
                        F1(ref x, ref y.FA());
                        F1(ref x, ref y.FB());
                        F2(ref x, ref y.FA());
                        F2(ref x, ref y.FB()); // 1
                    }
                }
                """;
            var comp = CreateCompilation(new[] { source, UnscopedRefAttributeDefinition });
            comp.VerifyDiagnostics();
        }

        // PROTOTYPE(RefStructInterfaces): Clone from RefFieldTests.cs once new constraints are supported:
        //  - MethodArgumentsMustMatch_17
        //  - MethodArgumentsMustMatch_18
        //  - ReturnOnlyScope_01
        //  - ReturnRefToRefStruct_ValEscape_01
        //  - ReturnRefToRefStruct_ValEscape_02
        //  - ReturnRefToRefStruct_ValEscape_03
        //  - ReturnRefToRefStruct_ValEscape_04
        //
        //  Also LocalScope_DeclarationExpression_06 from RefEscapingTests.cs

        // This is a clone of UnscopedRefAttribute_Method_03 from RefFieldTests.cs
        [CombinatorialData]
        [Theory]
        public void UnscopedRefAttribute_Method_03_DirectInterface(bool useCompilationReference)
        {
            var sourceA =
@"using System.Diagnostics.CodeAnalysis;
public interface S<T>
{
    public ref T F1();
    [UnscopedRef] public ref T F2();
}";
            var comp = CreateCompilation(new[] { sourceA, UnscopedRefAttributeDefinition });
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"class Program
{
    static ref int F1()
    {
        var s = GetS();
        return ref s.F1();
    }
    static ref int F2()
    {
        var s = GetS();
        return ref s.F2(); // 1
    }

    static S<int> GetS() => throw null;
}";
            comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics();
        }

        // This is a clone of UnscopedRefAttribute_Method_03 from RefFieldTests.cs
        [CombinatorialData]
        [Theory]
        public void UnscopedRefAttribute_Method_03_ConstrainedTypeParameter(bool useCompilationReference, bool addStructConstraint)
        {
            var sourceA =
@"using System.Diagnostics.CodeAnalysis;
public interface S<T>
{
    public ref T F1();
    [UnscopedRef] public ref T F2();
}";
            var comp = CreateCompilation(new[] { sourceA, UnscopedRefAttributeDefinition });
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"class Program<T> where T : " + (addStructConstraint ? "struct, " : "") + @"S<int>
{
    static ref int F1()
    {
        var s = GetS();
        return ref s.F1();
    }
    static ref int F2()
    {
        var s = GetS();
        return ref s.F2(); // 1
    }

    static T GetS() => throw null;
}";
            comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics(
                // (11,20): error CS8168: Cannot return local 's' by reference because it is not a ref local
                //         return ref s.F2(); // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "s").WithArguments("s").WithLocation(11, 20));
        }

        // This is a clone of UnscopedRefAttribute_Method_03 from RefFieldTests.cs
        [CombinatorialData]
        [Theory]
        public void UnscopedRefAttribute_Method_03_ClassConstrainedTypeParameter(bool useCompilationReference)
        {
            var sourceA =
@"using System.Diagnostics.CodeAnalysis;
public interface S<T>
{
    public ref T F1();
    [UnscopedRef] public ref T F2();
}";
            var comp = CreateCompilation(new[] { sourceA, UnscopedRefAttributeDefinition });
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"class Program<T> where T : class, S<int>
{
    static ref int F1()
    {
        var s = GetS();
        return ref s.F1();
    }
    static ref int F2()
    {
        var s = GetS();
        return ref s.F2(); // 1
    }

    static T GetS() => throw null;
}";
            comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics();
        }

        // This is a clone of UnscopedRefAttribute_Property_02 from RefFieldTests.cs
        [CombinatorialData]
        [Theory]
        public void UnscopedRefAttribute_Property_02_DirectInterface(bool useCompilationReference)
        {
            var sourceA =
@"using System.Diagnostics.CodeAnalysis;
public interface S<T>
{
    public ref T P1 {get;}
    [UnscopedRef] public ref T P2 {get;}
}";
            var comp = CreateCompilation(new[] { sourceA, UnscopedRefAttributeDefinition });
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"class Program
{
    static ref int F1()
    {
        var s = default(S<int>);
        return ref s.P1;
    }
    static ref int F2()
    {
        var s = default(S<int>);
        return ref s.P2; // 1
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics();
        }

        // This is a clone of UnscopedRefAttribute_Property_02 from RefFieldTests.cs
        [CombinatorialData]
        [Theory]
        public void UnscopedRefAttribute_Property_02_ConstrainedTypeParameter(bool useCompilationReference, bool addStructConstraint)
        {
            var sourceA =
@"using System.Diagnostics.CodeAnalysis;
public interface S<T>
{
    public ref T P1 {get;}
    [UnscopedRef] public ref T P2 {get;}
}";
            var comp = CreateCompilation(new[] { sourceA, UnscopedRefAttributeDefinition });
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"class Program<T> where T : " + (addStructConstraint ? "struct, " : "") + @"S<int>" + (!addStructConstraint ? ", new()" : "") + @"
{
    static ref int F1()
    {
        var s = new T();
        return ref s.P1;
    }
    static ref int F2()
    {
        var s = new T();
        return ref s.P2; // 1
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics(
                // (11,20): error CS8168: Cannot return local 's' by reference because it is not a ref local
                //         return ref s.P2; // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "s").WithArguments("s").WithLocation(11, 20));
        }

        // This is a clone of UnscopedRefAttribute_Property_02 from RefFieldTests.cs
        [CombinatorialData]
        [Theory]
        public void UnscopedRefAttribute_Property_02_ClassConstrainedTypeParameter(bool useCompilationReference)
        {
            var sourceA =
@"using System.Diagnostics.CodeAnalysis;
public interface S<T>
{
    public ref T P1 {get;}
    [UnscopedRef] public ref T P2 {get;}
}";
            var comp = CreateCompilation(new[] { sourceA, UnscopedRefAttributeDefinition });
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"class Program<T> where T : class, S<int>, new()
{
    static ref int F1()
    {
        var s = new T();
        return ref s.P1;
    }
    static ref int F2()
    {
        var s = new T();
        return ref s.P2; // 1
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics();
        }

        public enum ThreeState : byte
        {
            Unknown = 0,
            False = 1,
            True = 2,
        }

        // This is a clone of UnscopedRefAttribute_NestedAccess_MethodOrProperty from RefFieldTests.cs
        [Theory, CombinatorialData]
        public void UnscopedRefAttribute_NestedAccess_MethodOrProperty(bool firstIsMethod, bool secondIsMethod, ThreeState tS1IsClass, ThreeState tS2IsClass)
        {
            var source = $$"""
using System.Diagnostics.CodeAnalysis;

{{(tS1IsClass == ThreeState.True || tS2IsClass == ThreeState.True ? "" : """
var c = new C<S1<S2>, S2>();
c.Value() = 12;
System.Console.WriteLine(c.Value());
""")}}

class C<TS1, TS2>
    where TS1 : {{(tS1IsClass switch { ThreeState.False => "struct, ", ThreeState.True => "class, ", _ => "" })}}IS1<TS2>  
    where TS2 : {{(tS2IsClass switch { ThreeState.False => "struct, ", ThreeState.True => "class, ", _ => "" })}}IS2
{
    public ref int Value() => ref s1.S2{{csharp(firstIsMethod)}}.Value{{csharp(secondIsMethod)}};
#line 100
    private TS1 s1;
}

struct S1<TS2> : IS1<TS2> where TS2 : IS2
{
    private TS2 s2;
    [UnscopedRef] public ref TS2 S2{{csharp(firstIsMethod)}} => ref s2;
}

struct S2 : IS2
{
    private int value;
    [UnscopedRef] public ref int Value{{csharp(secondIsMethod)}} => ref value;
}

interface IS1<TS2> where TS2 : IS2
{
    [UnscopedRef] public ref TS2 S2{{(firstIsMethod ? "();" : "{get;}")}}
}

interface IS2
{
    [UnscopedRef] public ref int Value{{(secondIsMethod ? "();" : "{get;}")}}
}
""";
            var verifier = CompileAndVerify(new[] { source, UnscopedRefAttributeDefinition }, expectedOutput: (tS1IsClass == ThreeState.True || tS2IsClass == ThreeState.True ? null : "12"), verify: Verification.Fails);
            verifier.VerifyDiagnostics(
                // 0.cs(100,17): warning CS0649: Field 'C<TS1, TS2>.s1' is never assigned to, and will always have its default value 
                //     private TS1 s1;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "s1").WithArguments("C<TS1, TS2>.s1", tS1IsClass == ThreeState.True ? "null" : "").WithLocation(100, 17)
                );
            verifier.VerifyMethodBody("C<TS1, TS2>.Value",
                tS1IsClass == ThreeState.True ? $$"""
{
  // Code size       28 (0x1c)
  .maxstack  1
  // sequence point: s1.S2{{csharp(firstIsMethod)}}.Value{{csharp(secondIsMethod)}}
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "TS1 C<TS1, TS2>.s1"
  IL_0006:  box        "TS1"
  IL_000b:  callvirt   "ref TS2 IS1<TS2>.S2{{il(firstIsMethod)}}"
  IL_0010:  constrained. "TS2"
  IL_0016:  callvirt   "ref int IS2.Value{{il(secondIsMethod)}}"
  IL_001b:  ret
}
""" : $$"""
{
  // Code size       29 (0x1d)
  .maxstack  1
  // sequence point: s1.S2{{csharp(firstIsMethod)}}.Value{{csharp(secondIsMethod)}}
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "TS1 C<TS1, TS2>.s1"
  IL_0006:  constrained. "TS1"
  IL_000c:  callvirt   "ref TS2 IS1<TS2>.S2{{il(firstIsMethod)}}"
  IL_0011:  constrained. "TS2"
  IL_0017:  callvirt   "ref int IS2.Value{{il(secondIsMethod)}}"
  IL_001c:  ret
}
""");

            static string csharp(bool method) => method ? "()" : "";
            static string il(bool method) => method ? "()" : ".get";
        }

        // This is a clone of UnscopedRefAttribute_NestedAccess_Properties_Invalid from RefFieldTests.cs
        [Fact]
        public void UnscopedRefAttribute_NestedAccess_Properties_Invalid_DirectInterface()
        {
            var source = """
                using System.Diagnostics.CodeAnalysis;

                class C
                {
                    private S1 s1;
                    public ref int Value() => ref s1.S2.Value;
                }

                struct S1
                {
                    private S2 s2;
                    public S2 S2 => s2;
                }

                interface S2
                {
                    [UnscopedRef] public ref int Value {get;}
                }
                """;
            CreateCompilation(new[] { source, UnscopedRefAttributeDefinition }).VerifyDiagnostics(
                // 0.cs(11,16): warning CS0649: Field 'S1.s2' is never assigned to, and will always have its default value null
                //     private S2 s2;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "s2").WithArguments("S1.s2", "null").WithLocation(11, 16));
        }

        // This is a clone of UnscopedRefAttribute_NestedAccess_Properties_Invalid from RefFieldTests.cs
        [CombinatorialData]
        [Theory]
        public void UnscopedRefAttribute_NestedAccess_Properties_Invalid_ConstrainedTypeParameter(bool addStructConstraint)
        {
            var source =
@"using System.Diagnostics.CodeAnalysis;

class C<T> where T : " + (addStructConstraint ? "struct, " : "") + @"S2
{
    private S1<T> s1;
    public ref int Value() => ref s1.S2.Value;
}

struct S1<T> where T : S2
{
    private T s2;
    public T S2 => s2;
}

interface S2
{
    [UnscopedRef] public ref int Value {get;}
}
";
            CreateCompilation(new[] { source, UnscopedRefAttributeDefinition }).VerifyDiagnostics(
                // 0.cs(6,35): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     public ref int Value() => ref s1.S2.Value;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "s1.S2").WithLocation(6, 35),
                // 0.cs(11,15): warning CS0649: Field 'S1<T>.s2' is never assigned to, and will always have its default value 
                //     private T s2;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "s2").WithArguments("S1<T>.s2", "").WithLocation(11, 15));
        }

        // This is a clone of UnscopedRefAttribute_NestedAccess_Properties_Invalid from RefFieldTests.cs
        [Fact]
        public void UnscopedRefAttribute_NestedAccess_Properties_Invalid_ClassConstrainedTypeParameter()
        {
            var source =
@"using System.Diagnostics.CodeAnalysis;

class C<T> where T : class, S2
{
    private S1<T> s1;
    public ref int Value() => ref s1.S2.Value;
}

struct S1<T> where T : S2
{
    private T s2;
    public T S2 => s2;
}

interface S2
{
    [UnscopedRef] public ref int Value {get;}
}
";
            CreateCompilation(new[] { source, UnscopedRefAttributeDefinition }).VerifyDiagnostics(
                // 0.cs(11,15): warning CS0649: Field 'S1<T>.s2' is never assigned to, and will always have its default value 
                //     private T s2;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "s2").WithArguments("S1<T>.s2", "").WithLocation(11, 15));
        }

        // This is a clone of UnscopedRef_ArgumentsMustMatch_01 from RefFieldTests.cs
        [Fact]
        public void UnscopedRef_ArgumentsMustMatch_01_DirectInterface()
        {
            var source = """
                using System.Diagnostics.CodeAnalysis;

                ref struct RefByteContainer
                {
                    public ref byte RB;

                    public RefByteContainer(ref byte rb)
                    {
                        RB = ref rb;
                    }
                }

                interface ByteContainer
                {
                    [UnscopedRef]
                    public RefByteContainer ByteRef {get;}

                    [UnscopedRef]
                    public RefByteContainer GetByteRef();
                }

                public class Program
                {
                    static void M11(ref ByteContainer bc)
                    {
                        // ok. because ref-safe-to-escape of 'this' in 'ByteContainer.ByteRef.get' is 'ReturnOnly',
                        // we know that 'ref bc' will not end up written to a ref field within 'bc'.
                        _ = bc.ByteRef;
                    }
                    static void M12(ref ByteContainer bc)
                    {
                        // ok. because ref-safe-to-escape of 'this' in 'ByteContainer.GetByteRef()' is 'ReturnOnly',
                        // we know that 'ref bc' will not end up written to a ref field within 'bc'.
                        _ = bc.GetByteRef();
                    }

                    static void M21(ref ByteContainer bc, ref RefByteContainer rbc)
                    {
                        // error. ref-safe-to-escape of 'bc' is 'ReturnOnly', therefore 'bc.ByteRef' can't be assigned to a ref parameter.
                        rbc = bc.ByteRef; // 1
                    }
                    static void M22(ref ByteContainer bc, ref RefByteContainer rbc)
                    {
                        // error. ref-safe-to-escape of 'bc' is 'ReturnOnly', therefore 'bc.ByteRef' can't be assigned to a ref parameter.
                        rbc = bc.GetByteRef(); // 2
                    }

                    static RefByteContainer M31(ref ByteContainer bc)
                        // ok. ref-safe-to-escape of 'bc' is 'ReturnOnly'.
                        => bc.ByteRef;

                    static RefByteContainer M32(ref ByteContainer bc)
                        // ok. ref-safe-to-escape of 'bc' is 'ReturnOnly'.
                        => bc.GetByteRef();

                    static RefByteContainer M41(scoped ref ByteContainer bc)
                        // error: `bc.ByteRef` may contain a reference to `bc`, whose ref-safe-to-escape is CurrentMethod.
                        => bc.ByteRef; // 3

                    static RefByteContainer M42(scoped ref ByteContainer bc)
                        // error: `bc.GetByteRef()` may contain a reference to `bc`, whose ref-safe-to-escape is CurrentMethod.
                        => bc.GetByteRef(); // 4
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
        }

        // This is a clone of UnscopedRef_ArgumentsMustMatch_01 from RefFieldTests.cs
        [Theory]
        [CombinatorialData]
        public void UnscopedRef_ArgumentsMustMatch_01_ConstrainedTypeParameter(bool addStructConstraint)
        {
            var source = $$"""
                using System.Diagnostics.CodeAnalysis;

                ref struct RefByteContainer
                {
                    public ref byte RB;

                    public RefByteContainer(ref byte rb)
                    {
                        RB = ref rb;
                    }
                }

                interface ByteContainer
                {
                    [UnscopedRef]
                    public RefByteContainer ByteRef {get;}

                    [UnscopedRef]
                    public RefByteContainer GetByteRef();
                }

                class Program<TByteContainer> where TByteContainer : {{(addStructConstraint ? "struct, " : "")}} ByteContainer
                {
                    static void M11(ref TByteContainer bc)
                    {
                        // ok. because ref-safe-to-escape of 'this' in 'ByteContainer.ByteRef.get' is 'ReturnOnly',
                        // we know that 'ref bc' will not end up written to a ref field within 'bc'.
                        _ = bc.ByteRef;
                    }
                    static void M12(ref TByteContainer bc)
                    {
                        // ok. because ref-safe-to-escape of 'this' in 'ByteContainer.GetByteRef()' is 'ReturnOnly',
                        // we know that 'ref bc' will not end up written to a ref field within 'bc'.
                        _ = bc.GetByteRef();
                    }

                    static void M21(ref TByteContainer bc, ref RefByteContainer rbc)
                    {
                        // error. ref-safe-to-escape of 'bc' is 'ReturnOnly', therefore 'bc.ByteRef' can't be assigned to a ref parameter.
                        rbc = bc.ByteRef; // 1
                    }
                    static void M22(ref TByteContainer bc, ref RefByteContainer rbc)
                    {
                        // error. ref-safe-to-escape of 'bc' is 'ReturnOnly', therefore 'bc.ByteRef' can't be assigned to a ref parameter.
                        rbc = bc.GetByteRef(); // 2
                    }

                    static RefByteContainer M31(ref TByteContainer bc)
                        // ok. ref-safe-to-escape of 'bc' is 'ReturnOnly'.
                        => bc.ByteRef;

                    static RefByteContainer M32(ref TByteContainer bc)
                        // ok. ref-safe-to-escape of 'bc' is 'ReturnOnly'.
                        => bc.GetByteRef();

                    static RefByteContainer M41(scoped ref TByteContainer bc)
                        // error: `bc.ByteRef` may contain a reference to `bc`, whose ref-safe-to-escape is CurrentMethod.
                        => bc.ByteRef; // 3

                    static RefByteContainer M42(scoped ref TByteContainer bc)
                        // error: `bc.GetByteRef()` may contain a reference to `bc`, whose ref-safe-to-escape is CurrentMethod.
                        => bc.GetByteRef(); // 4
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (40,15): error CS9077: Cannot return a parameter by reference 'bc' through a ref parameter; it can only be returned in a return statement
                //         rbc = bc.ByteRef; // 1
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "bc").WithArguments("bc").WithLocation(40, 15),
                // (45,15): error CS9077: Cannot return a parameter by reference 'bc' through a ref parameter; it can only be returned in a return statement
                //         rbc = bc.GetByteRef(); // 2
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "bc").WithArguments("bc").WithLocation(45, 15),
                // (58,12): error CS9075: Cannot return a parameter by reference 'bc' because it is scoped to the current method
                //         => bc.ByteRef; // 3
                Diagnostic(ErrorCode.ERR_RefReturnScopedParameter, "bc").WithArguments("bc").WithLocation(58, 12),
                // (62,12): error CS9075: Cannot return a parameter by reference 'bc' because it is scoped to the current method
                //         => bc.GetByteRef(); // 4
                Diagnostic(ErrorCode.ERR_RefReturnScopedParameter, "bc").WithArguments("bc").WithLocation(62, 12)
                );
        }

        // This is a clone of UnscopedRef_ArgumentsMustMatch_01 from RefFieldTests.cs
        [Fact]
        public void UnscopedRef_ArgumentsMustMatch_01_ClassConstrainedTypeParameter()
        {
            var source = """
                using System.Diagnostics.CodeAnalysis;

                ref struct RefByteContainer
                {
                    public ref byte RB;

                    public RefByteContainer(ref byte rb)
                    {
                        RB = ref rb;
                    }
                }

                interface ByteContainer
                {
                    [UnscopedRef]
                    public RefByteContainer ByteRef {get;}

                    [UnscopedRef]
                    public RefByteContainer GetByteRef();
                }

                class Program<TByteContainer> where TByteContainer : class, ByteContainer
                {
                    static void M11(ref TByteContainer bc)
                    {
                        // ok. because ref-safe-to-escape of 'this' in 'ByteContainer.ByteRef.get' is 'ReturnOnly',
                        // we know that 'ref bc' will not end up written to a ref field within 'bc'.
                        _ = bc.ByteRef;
                    }
                    static void M12(ref TByteContainer bc)
                    {
                        // ok. because ref-safe-to-escape of 'this' in 'ByteContainer.GetByteRef()' is 'ReturnOnly',
                        // we know that 'ref bc' will not end up written to a ref field within 'bc'.
                        _ = bc.GetByteRef();
                    }

                    static void M21(ref TByteContainer bc, ref RefByteContainer rbc)
                    {
                        // error. ref-safe-to-escape of 'bc' is 'ReturnOnly', therefore 'bc.ByteRef' can't be assigned to a ref parameter.
                        rbc = bc.ByteRef; // 1
                    }
                    static void M22(ref TByteContainer bc, ref RefByteContainer rbc)
                    {
                        // error. ref-safe-to-escape of 'bc' is 'ReturnOnly', therefore 'bc.ByteRef' can't be assigned to a ref parameter.
                        rbc = bc.GetByteRef(); // 2
                    }

                    static RefByteContainer M31(ref TByteContainer bc)
                        // ok. ref-safe-to-escape of 'bc' is 'ReturnOnly'.
                        => bc.ByteRef;

                    static RefByteContainer M32(ref TByteContainer bc)
                        // ok. ref-safe-to-escape of 'bc' is 'ReturnOnly'.
                        => bc.GetByteRef();

                    static RefByteContainer M41(scoped ref TByteContainer bc)
                        // error: `bc.ByteRef` may contain a reference to `bc`, whose ref-safe-to-escape is CurrentMethod.
                        => bc.ByteRef; // 3

                    static RefByteContainer M42(scoped ref TByteContainer bc)
                        // error: `bc.GetByteRef()` may contain a reference to `bc`, whose ref-safe-to-escape is CurrentMethod.
                        => bc.GetByteRef(); // 4
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
        }

        // PROTOTYPE(RefStructInterfaces): Add flavor of UnscopedRef_ArgumentsMustMatch_01 tests with RefByteContainer as an interface 

        // This is a clone of PatternIndex_01 from RefFieldTests.cs
        [Fact]
        public void PatternIndex_01_DirectInterface()
        {
            string source = """
                using System;
                using System.Diagnostics.CodeAnalysis;
                interface R
                {
                    public int Length {get;}
                    [UnscopedRef] public ref int this[int i] {get;}
                }
                class Program
                {
                    static ref int F1(ref R r1)
                    {
                        ref int i1 = ref r1[^1];
                        return ref i1;
                    }
                    static ref int F2(ref R r2, Index i)
                    {
                        ref int i2 = ref r2[i];
                        return ref i2;
                    }
                    static ref int F3()
                    {
                        R r3 = GetR();
                        ref int i3 = ref r3[^3];
                        return ref i3; // 1
                    }
                    static ref int F4(Index i)
                    {
                        R r4 = GetR();
                        ref int i4 = ref r4[i];
                        return ref i4; // 2
                    }

                    static R GetR() => null;
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
        }

        // This is a clone of PatternIndex_01 from RefFieldTests.cs
        [Theory]
        [CombinatorialData]
        public void PatternIndex_01_ConstrainedTypeParameter(bool addStructConstraint)
        {
            string source = $$"""
                using System;
                using System.Diagnostics.CodeAnalysis;
                interface R
                {
                    public int Length {get;}
                    [UnscopedRef] public ref int this[int i] {get;}
                }
                class Program<TR> where TR : {{(addStructConstraint ? "struct, " : "")}} R {{(!addStructConstraint ? ", new()" : "")}}
                {
                    static ref int F1(ref TR r1)
                    {
                        ref int i1 = ref r1[^1];
                        return ref i1;
                    }
                    static ref int F2(ref TR r2, Index i)
                    {
                        ref int i2 = ref r2[i];
                        return ref i2;
                    }
                    static ref int F3()
                    {
                        TR r3 = new TR();
                        ref int i3 = ref r3[^3];
                        return ref i3; // 1
                    }
                    static ref int F4(Index i)
                    {
                        TR r4 = new TR();
                        ref int i4 = ref r4[i];
                        return ref i4; // 2
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (24,20): error CS8157: Cannot return 'i3' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref i3; // 1
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "i3").WithArguments("i3").WithLocation(24, 20),
                // (30,20): error CS8157: Cannot return 'i4' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref i4; // 2
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "i4").WithArguments("i4").WithLocation(30, 20));
        }

        // This is a clone of PatternIndex_01 from RefFieldTests.cs
        [Fact]
        public void PatternIndex_01_ClassConstrainedTypeParameter()
        {
            string source = """
                using System;
                using System.Diagnostics.CodeAnalysis;
                interface R
                {
                    public int Length {get;}
                    [UnscopedRef] public ref int this[int i] {get;}
                }
                class Program<TR> where TR : class, R, new()
                {
                    static ref int F1(ref TR r1)
                    {
                        ref int i1 = ref r1[^1];
                        return ref i1;
                    }
                    static ref int F2(ref TR r2, Index i)
                    {
                        ref int i2 = ref r2[i];
                        return ref i2;
                    }
                    static ref int F3()
                    {
                        TR r3 = new TR();
                        ref int i3 = ref r3[^3];
                        return ref i3; // 1
                    }
                    static ref int F4(Index i)
                    {
                        TR r4 = new TR();
                        ref int i4 = ref r4[i];
                        return ref i4; // 2
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
        }

        // This is a clone of MemberOfReadonlyRefLikeEscape from RefEscapingTests.cs
        [Fact]
        public void MemberOfReadonlyRefLikeEscape_DirectInterface()
        {
            var text = @"
    using System;
    using System.Diagnostics.CodeAnalysis;
    public static class Program
    {
        public static void Main()
        {
            Span<int> value1 = stackalloc int[1];

            // Ok, the new value can be copied into SW but not the 
            // ref to the value
            Get_SW().TryGet(out value1);

            // Error as the ref of this can escape into value2
            Span<int> value2 = default;
            Get_SW().TryGet2(out value2);
        }

        static SW Get_SW() => throw null;
    }

    interface SW
    {
        public void TryGet(out Span<int> result); 

        [UnscopedRef]
        public void TryGet2(out Span<int> result);
    }
";
            CreateCompilationWithMscorlibAndSpan(new[] { text, UnscopedRefAttributeDefinition }).VerifyDiagnostics();
        }

        // This is a clone of MemberOfReadonlyRefLikeEscape from RefEscapingTests.cs
        [Theory]
        [CombinatorialData]
        public void MemberOfReadonlyRefLikeEscape_ConstrainedTypeParameter(bool addStructConstraint)
        {
            var text = @"
    using System;
    using System.Diagnostics.CodeAnalysis;
    static class Program<TSW> where TSW : " + (addStructConstraint ? "struct, " : "") + @"SW" + (!addStructConstraint ? ", new()" : "") + @"
    {
        public static void Main()
        {
            Span<int> value1 = stackalloc int[1];

            // Ok, the new value can be copied into SW but not the 
            // ref to the value
            new TSW().TryGet(out value1);

            // Error as the ref of this can escape into value2
            Span<int> value2 = default;
            new TSW().TryGet2(out value2);
        }
    }

    interface SW
    {
        public void TryGet(out Span<int> result);

        [UnscopedRef]
        public void TryGet2(out Span<int> result);
    }
";
            CreateCompilationWithMscorlibAndSpan(new[] { text, UnscopedRefAttributeDefinition }).VerifyDiagnostics(
                // 0.cs(16,13): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //             new TSW().TryGet2(out value2);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "new TSW()").WithLocation(16, 13),
                // 0.cs(16,13): error CS8350: This combination of arguments to 'SW.TryGet2(out Span<int>)' is disallowed because it may expose variables referenced by parameter 'this' outside of their declaration scope
                //             new TSW().TryGet2(out value2);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "new TSW().TryGet2(out value2)").WithArguments("SW.TryGet2(out System.Span<int>)", "this").WithLocation(16, 13)
                );
        }

        // This is a clone of MemberOfReadonlyRefLikeEscape from RefEscapingTests.cs
        [Fact]
        public void MemberOfReadonlyRefLikeEscape_ClassConstrainedTypeParameter()
        {
            var text = @"
    using System;
    using System.Diagnostics.CodeAnalysis;
    static class Program<TSW> where TSW : class, SW, new()
    {
        public static void Main()
        {
            Span<int> value1 = stackalloc int[1];

            // Ok, the new value can be copied into SW but not the 
            // ref to the value
            new TSW().TryGet(out value1);

            // Error as the ref of this can escape into value2
            Span<int> value2 = default;
            new TSW().TryGet2(out value2);
        }
    }

    interface SW
    {
        public void TryGet(out Span<int> result);

        [UnscopedRef]
        public void TryGet2(out Span<int> result);
    }
";
            CreateCompilationWithMscorlibAndSpan(new[] { text, UnscopedRefAttributeDefinition }).VerifyDiagnostics();
        }

        // This is a clone of DefensiveCopy_01 from RefEscapingTests.cs
        [Fact]
        public void DefensiveCopy_01_DirectInterface()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static readonly Vec4 ReadOnlyVec = GetVec4();

    static void Main()
    {
        // This refers to stack memory that has already been left out.
        ref Vec4 local = ref Test1();
        Console.WriteLine(local);
    }

    private static ref Vec4 Test1()
    {
        // Defensive copy occurs and it is placed in stack memory implicitly.
        // The method returns a reference to the copy, which happens invalid memory access.
        ref Vec4 xyzw1 = ref ReadOnlyVec.Self;
        return ref xyzw1;
    }

    private static ref Vec4 Test2()
    {
        var copy = ReadOnlyVec;
        ref Vec4 xyzw2 = ref copy.Self;
        return ref xyzw2;
    }

    private static ref Vec4 Test3()
    {
        ref Vec4 xyzw3 = ref ReadOnlyVec.Self2();
        return ref xyzw3;
    }

    private static ref Vec4 Test4()
    {
        var copy = ReadOnlyVec;
        ref Vec4 xyzw4 = ref copy.Self2();
        return ref xyzw4;
    }

    static Vec4 GetVec4() => throw null;
}

public interface Vec4
{
    [UnscopedRef]
    public ref Vec4 Self {get;}

    [UnscopedRef]
    public ref Vec4 Self2();
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics();
        }

        // This is a clone of DefensiveCopy_01 from RefEscapingTests.cs
        [Theory]
        [CombinatorialData]
        public void DefensiveCopy_01_ConstrainedTypeParameter(bool addStructConstraint)
        {
            var source =
@"
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

internal class Program<TVec4> where TVec4 : " + (addStructConstraint ? "struct, " : "") + @" Vec4<TVec4>
{
    private static readonly TVec4 ReadOnlyVec = GetVec4();

    static void Main()
    {
        // This refers to stack memory that has already been left out.
        ref TVec4 local = ref Test1();
        Console.WriteLine(local);
    }

    private static ref TVec4 Test1()
    {
        // Defensive copy occurs and it is placed in stack memory implicitly.
        // The method returns a reference to the copy, which happens invalid memory access.
        ref TVec4 xyzw1 = ref ReadOnlyVec.Self;
        return ref xyzw1;
    }

    private static ref TVec4 Test2()
    {
        var copy = ReadOnlyVec;
        ref TVec4 xyzw2 = ref copy.Self;
        return ref xyzw2;
    }

    private static ref TVec4 Test3()
    {
        ref TVec4 xyzw3 = ref ReadOnlyVec.Self2();
        return ref xyzw3;
    }

    private static ref TVec4 Test4()
    {
        var copy = ReadOnlyVec;
        ref TVec4 xyzw4 = ref copy.Self2();
        return ref xyzw4;
    }

    static TVec4 GetVec4() => throw null;
}

public interface Vec4<TVec4> where TVec4 : Vec4<TVec4>
{
    [UnscopedRef]
    public ref TVec4 Self {get;}

    [UnscopedRef]
    public ref TVec4 Self2();
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (22,20): error CS8157: Cannot return 'xyzw1' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref xyzw1;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "xyzw1").WithArguments("xyzw1").WithLocation(22, 20),
                // (29,20): error CS8157: Cannot return 'xyzw2' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref xyzw2;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "xyzw2").WithArguments("xyzw2").WithLocation(29, 20),
                // (35,20): error CS8157: Cannot return 'xyzw3' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref xyzw3;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "xyzw3").WithArguments("xyzw3").WithLocation(35, 20),
                // (42,20): error CS8157: Cannot return 'xyzw4' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref xyzw4;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "xyzw4").WithArguments("xyzw4").WithLocation(42, 20)
                );
        }

        // This is a clone of DefensiveCopy_01 from RefEscapingTests.cs
        [Fact]
        public void DefensiveCopy_01_ClassConstrainedTypeParameter()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program<TVec4> where TVec4 : class, Vec4<TVec4>
{
    private static readonly TVec4 ReadOnlyVec = GetVec4();

    static void Main()
    {
        // This refers to stack memory that has already been left out.
        ref TVec4 local = ref Test1();
        Console.WriteLine(local);
    }

    private static ref TVec4 Test1()
    {
        // Defensive copy occurs and it is placed in stack memory implicitly.
        // The method returns a reference to the copy, which happens invalid memory access.
        ref TVec4 xyzw1 = ref ReadOnlyVec.Self;
        return ref xyzw1;
    }

    private static ref TVec4 Test2()
    {
        var copy = ReadOnlyVec;
        ref TVec4 xyzw2 = ref copy.Self;
        return ref xyzw2;
    }

    private static ref TVec4 Test3()
    {
        ref TVec4 xyzw3 = ref ReadOnlyVec.Self2();
        return ref xyzw3;
    }

    private static ref TVec4 Test4()
    {
        var copy = ReadOnlyVec;
        ref TVec4 xyzw4 = ref copy.Self2();
        return ref xyzw4;
    }

    static TVec4 GetVec4() => throw null;
}

public interface Vec4<TVec4> where TVec4 : Vec4<TVec4>
{
    [UnscopedRef]
    public ref TVec4 Self {get;}

    [UnscopedRef]
    public ref TVec4 Self2();
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics();
        }

        // This is a clone of DefensiveCopy_02 from RefEscapingTests.cs
        [Fact]
        public void DefensiveCopy_02_DirectInterface()
        {
            var source =
@"using System.Diagnostics.CodeAnalysis;

class Program
{
    static ref Wrap m1(in Wrap i)
    {
        ref Wrap r1 = ref i.Self; // defensive copy
        return ref r1; // ref to the local copy
    }

    static ref Wrap m2(in Wrap i)
    {
        var copy = i;
        ref Wrap r2 = ref copy.Self;
        return ref r2; // ref to the local copy
    }

    static ref Wrap m3(in Wrap i)
    {
        ref Wrap r3 = ref i.Self2();
        return ref r3;
    }

    static ref Wrap m4(in Wrap i)
    {
        var copy = i;
        ref Wrap r4 = ref copy.Self2();
        return ref r4; // ref to the local copy
    }
}

interface Wrap
{
    [UnscopedRef]
    public ref Wrap Self {get;}

    [UnscopedRef]
    public ref Wrap Self2();
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics();
        }

        // This is a clone of DefensiveCopy_02 from RefEscapingTests.cs
        [Theory]
        [CombinatorialData]
        public void DefensiveCopy_02_ConstrainedTypeParameter(bool addStructConstraint)
        {
            var source =
@"using System.Diagnostics.CodeAnalysis;

class Program<TWrap> where TWrap : " + (addStructConstraint ? "struct, " : "") + @"Wrap<TWrap>
{
    static ref TWrap m1(in TWrap i)
    {
        ref TWrap r1 = ref i.Self; // defensive copy
        return ref r1; // ref to the local copy
    }

    static ref TWrap m2(in TWrap i)
    {
        var copy = i;
        ref TWrap r2 = ref copy.Self;
        return ref r2; // ref to the local copy
    }

    static ref TWrap m3(in TWrap i)
    {
        ref TWrap r3 = ref i.Self2();
        return ref r3;
    }

    static ref TWrap m4(in TWrap i)
    {
        var copy = i;
        ref TWrap r4 = ref copy.Self2();
        return ref r4; // ref to the local copy
    }
}

interface Wrap<T> where T : Wrap<T>
{
    [UnscopedRef]
    public ref T Self {get;}

    [UnscopedRef]
    public ref T Self2();
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (8,20): error CS8157: Cannot return 'r1' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref r1; // ref to the local copy
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r1").WithArguments("r1").WithLocation(8, 20),
                // (15,20): error CS8157: Cannot return 'r2' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref r2; // ref to the local copy
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r2").WithArguments("r2").WithLocation(15, 20),
                // (21,20): error CS8157: Cannot return 'r3' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref r3;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r3").WithArguments("r3").WithLocation(21, 20),
                // (28,20): error CS8157: Cannot return 'r4' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref r4; // ref to the local copy
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r4").WithArguments("r4").WithLocation(28, 20)
                );
        }

        // This is a clone of DefensiveCopy_02 from RefEscapingTests.cs
        [Fact]
        public void DefensiveCopy_02_ClassConstrainedTypeParameter()
        {
            var source =
@"using System.Diagnostics.CodeAnalysis;

class Program<TWrap> where TWrap : class, Wrap<TWrap>
{
    static ref TWrap m1(in TWrap i)
    {
        ref TWrap r1 = ref i.Self; // defensive copy
        return ref r1; // ref to the local copy
    }

    static ref TWrap m2(in TWrap i)
    {
        var copy = i;
        ref TWrap r2 = ref copy.Self;
        return ref r2; // ref to the local copy
    }

    static ref TWrap m3(in TWrap i)
    {
        ref TWrap r3 = ref i.Self2();
        return ref r3;
    }

    static ref TWrap m4(in TWrap i)
    {
        var copy = i;
        ref TWrap r4 = ref copy.Self2();
        return ref r4; // ref to the local copy
    }
}

interface Wrap<T> where T : Wrap<T>
{
    [UnscopedRef]
    public ref T Self {get;}

    [UnscopedRef]
    public ref T Self2();
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics();
        }

        // This is a clone of DefensiveCopy_05 from RefEscapingTests.cs
        [Fact]
        public void DefensiveCopy_05_DirectInterface()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static readonly Vec4 ReadOnlyVec = default;

    static void Main()
    {
    }

    private static Span<float> Test1()
    {
        var xyzw1 = ReadOnlyVec.Self;
        return xyzw1;
    }

    private static Span<float> Test2()
    {
        var r2 = ReadOnlyVec;
        var xyzw2 = r2.Self;
        return xyzw2;
    }
}

public interface Vec4
{
    [UnscopedRef]
    public Span<float> Self
    {  get; set; }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics();
        }

        // This is a clone of DefensiveCopy_05 from RefEscapingTests.cs
        [Theory]
        [CombinatorialData]
        public void DefensiveCopy_05_ConstrainedTypeParameter(bool addStructConstraint)
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program<TVec4> where TVec4 : " + (addStructConstraint ? "struct, " : "") + @" Vec4
{
    private static readonly TVec4 ReadOnlyVec = default;

    static void Main()
    {
    }

    private static Span<float> Test1()
    {
        var xyzw1 = ReadOnlyVec.Self;
        return xyzw1;
    }

    private static Span<float> Test2()
    {
        var r2 = ReadOnlyVec;
        var xyzw2 = r2.Self;
        return xyzw2;
    }
}

public interface Vec4
{
    [UnscopedRef]
    public Span<float> Self
    {  get; set; }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (16,16): error CS8352: Cannot use variable 'xyzw1' in this context because it may expose referenced variables outside of their declaration scope
                //         return xyzw1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw1").WithArguments("xyzw1").WithLocation(16, 16),
                // (23,16): error CS8352: Cannot use variable 'xyzw2' in this context because it may expose referenced variables outside of their declaration scope
                //         return xyzw2;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw2").WithArguments("xyzw2").WithLocation(23, 16)
                );
        }

        // This is a clone of DefensiveCopy_05 from RefEscapingTests.cs
        [Fact]
        public void DefensiveCopy_05_ClassConstrainedTypeParameter()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program<TVec4> where TVec4 : class, Vec4
{
    private static readonly TVec4 ReadOnlyVec = default;

    static void Main()
    {
    }

    private static Span<float> Test1()
    {
        var xyzw1 = ReadOnlyVec.Self;
        return xyzw1;
    }

    private static Span<float> Test2()
    {
        var r2 = ReadOnlyVec;
        var xyzw2 = r2.Self;
        return xyzw2;
    }
}

public interface Vec4
{
    [UnscopedRef]
    public Span<float> Self
    {  get; set; }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics();
        }

        // This is a clone of DefensiveCopy_21 from RefEscapingTests.cs
        [Fact]
        public void DefensiveCopy_21_DirectInterface()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static readonly Vec4 ReadOnlyVec = default;

    static void Main()
    {
    }

    private static Span<float> Test1()
    {
        var (xyzw1, _) = ReadOnlyVec;
        return xyzw1;
    }

    private static Span<float> Test2()
    {
        var r2 = ReadOnlyVec;
        var (xyzw2, _) = r2;
        return xyzw2;
    }

    private static Span<float> Test3()
    {
        ReadOnlyVec.Deconstruct(out var xyzw3, out _);
        return xyzw3;
    }

    private static Span<float> Test4()
    {
        var r4 = ReadOnlyVec;
        r4.Deconstruct(out var xyzw4, out _);
        return xyzw4;
    }
}

public interface Vec4
{
    [UnscopedRef]
    public void Deconstruct(out Span<float> x, out int i);
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics();
        }

        // This is a clone of DefensiveCopy_21 from RefEscapingTests.cs
        [Theory]
        [CombinatorialData]
        public void DefensiveCopy_21_ConstrainedTypeParameter(bool addStructConstraint)
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program<TVec4> where TVec4 : " + (addStructConstraint ? "struct, " : "") + @" Vec4
{
    private static readonly TVec4 ReadOnlyVec = default;

    static void Main()
    {
    }

    private static Span<float> Test1()
    {
        var (xyzw1, _) = ReadOnlyVec;
        return xyzw1;
    }

    private static Span<float> Test2()
    {
        var r2 = ReadOnlyVec;
        var (xyzw2, _) = r2;
        return xyzw2;
    }

    private static Span<float> Test3()
    {
        ReadOnlyVec.Deconstruct(out var xyzw3, out _);
        return xyzw3;
    }

    private static Span<float> Test4()
    {
        var r4 = ReadOnlyVec;
        r4.Deconstruct(out var xyzw4, out _);
        return xyzw4;
    }
}

public interface Vec4
{
    [UnscopedRef]
    public void Deconstruct(out Span<float> x, out int i);
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (16,16): error CS8352: Cannot use variable 'xyzw1' in this context because it may expose referenced variables outside of their declaration scope
                //         return xyzw1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw1").WithArguments("xyzw1").WithLocation(16, 16),
                // (23,16): error CS8352: Cannot use variable 'xyzw2' in this context because it may expose referenced variables outside of their declaration scope
                //         return xyzw2;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw2").WithArguments("xyzw2").WithLocation(23, 16),
                // (29,16): error CS8352: Cannot use variable 'xyzw3' in this context because it may expose referenced variables outside of their declaration scope
                //         return xyzw3;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw3").WithArguments("xyzw3").WithLocation(29, 16),
                // (36,16): error CS8352: Cannot use variable 'xyzw4' in this context because it may expose referenced variables outside of their declaration scope
                //         return xyzw4;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw4").WithArguments("xyzw4").WithLocation(36, 16)
                );
        }

        // This is a clone of DefensiveCopy_21 from RefEscapingTests.cs
        [Fact]
        public void DefensiveCopy_21_ClassConstrainedTypeParameter()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program<TVec4> where TVec4 : class, Vec4
{
    private static readonly TVec4 ReadOnlyVec = default;

    static void Main()
    {
    }

    private static Span<float> Test1()
    {
        var (xyzw1, _) = ReadOnlyVec;
        return xyzw1;
    }

    private static Span<float> Test2()
    {
        var r2 = ReadOnlyVec;
        var (xyzw2, _) = r2;
        return xyzw2;
    }

    private static Span<float> Test3()
    {
        ReadOnlyVec.Deconstruct(out var xyzw3, out _);
        return xyzw3;
    }

    private static Span<float> Test4()
    {
        var r4 = ReadOnlyVec;
        r4.Deconstruct(out var xyzw4, out _);
        return xyzw4;
    }
}

public interface Vec4
{
    [UnscopedRef]
    public void Deconstruct(out Span<float> x, out int i);
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void AllowsConstraint_01_SimpleTypeTypeParameter()
        {
            var src = @"
public class C<T>
    where T : allows ref struct
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify, verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var t = c.TypeParameters.Single();
                Assert.False(t.HasReferenceTypeConstraint);
                Assert.False(t.HasValueTypeConstraint);
                Assert.False(t.HasUnmanagedTypeConstraint);
                Assert.False(t.HasNotNullConstraint);
                Assert.True(t.AllowByRefLike);
            }

            CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

            CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (3,22): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     where T : allows ref struct
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "ref struct").WithArguments("ref struct interfaces").WithLocation(3, 22)
                );

            CreateCompilation(src, targetFramework: TargetFramework.DesktopLatestExtended, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(
                // (3,22): error CS9500: Target runtime doesn't support by-ref-like generics.
                //     where T : allows ref struct
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportByRefLikeGenerics, "ref struct").WithLocation(3, 22)
                );
        }

        [Fact]
        public void AllowsConstraint_02_SimpleMethodTypeParameter()
        {
            var src = @"
public class C
{
    public void M<T>()
        where T : allows ref struct
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify, verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var method = m.GlobalNamespace.GetMember<MethodSymbol>("C.M");
                var t = method.TypeParameters.Single();
                Assert.False(t.HasReferenceTypeConstraint);
                Assert.False(t.HasValueTypeConstraint);
                Assert.False(t.HasUnmanagedTypeConstraint);
                Assert.False(t.HasNotNullConstraint);
                Assert.True(t.AllowByRefLike);
            }

            CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

            CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (5,26): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         where T : allows ref struct
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "ref struct").WithArguments("ref struct interfaces").WithLocation(5, 26)
                );

            CreateCompilation(src, targetFramework: TargetFramework.DesktopLatestExtended, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(
                // (5,26): error CS9500: Target runtime doesn't support by-ref-like generics.
                //         where T : allows ref struct
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportByRefLikeGenerics, "ref struct").WithLocation(5, 26)
                );
        }

        [Fact]
        public void AllowsConstraint_03_TwoRefStructInARow()
        {
            var src = @"
public class C<T>
    where T : allows ref struct, ref struct
{
}

public class D<T>
    where T : allows ref struct, ref
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (3,34): error CS9501: 'ref struct' is already specified.
                //     where T : allows ref struct, ref struct
                Diagnostic(ErrorCode.ERR_RefStructConstraintAlreadySpecified, "ref struct").WithLocation(3, 34),
                // (8,34): error CS9501: 'ref struct' is already specified.
                //     where T : allows ref struct, ref
                Diagnostic(ErrorCode.ERR_RefStructConstraintAlreadySpecified, @"ref
").WithLocation(8, 34),
                // (8,37): error CS1003: Syntax error, 'struct' expected
                //     where T : allows ref struct, ref
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("struct").WithLocation(8, 37)
                );

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);
            Assert.True(t.AllowByRefLike);

            var d = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("D");
            var dt = d.TypeParameters.Single();
            Assert.False(dt.HasReferenceTypeConstraint);
            Assert.False(dt.HasValueTypeConstraint);
            Assert.False(dt.HasUnmanagedTypeConstraint);
            Assert.False(dt.HasNotNullConstraint);
            Assert.True(dt.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_04_TwoAllows()
        {
            var src = @"
public class C<T>
    where T : allows ref struct, allows ref struct
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (3,15): error CS9502: The 'allows' constraint clause must be the last constraint specified
                //     where T : allows ref struct, allows ref struct
                Diagnostic(ErrorCode.ERR_AllowsClauseMustBeLast, "allows").WithLocation(3, 15)
                );

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var ct = c.TypeParameters.Single();
            Assert.False(ct.HasReferenceTypeConstraint);
            Assert.False(ct.HasValueTypeConstraint);
            Assert.False(ct.HasUnmanagedTypeConstraint);
            Assert.False(ct.HasNotNullConstraint);
            Assert.True(ct.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_05_FollowedByStruct()
        {
            var src = @"
public class C<T>
    where T : allows ref struct, struct
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (3,15): error CS9502: The 'allows' constraint clause must be the last constraint specified
                //     where T : allows ref struct, struct
                Diagnostic(ErrorCode.ERR_AllowsClauseMustBeLast, "allows").WithLocation(3, 15),
                // (3,34): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
                //     where T : allows ref struct, struct
                Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "struct").WithLocation(3, 34)
                );

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.True(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_06_AfterStruct()
        {
            var src = @"
public class C<T>
    where T : struct, allows ref struct
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics();

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.True(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_07_FollowedByClass()
        {
            var src = @"
public class C<T>
    where T : allows ref struct, class
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (2,16): error CS9503: Cannot allow ref structs for a type parameter known from other constraints to be a class
                // public class C<T>
                Diagnostic(ErrorCode.ERR_ClassIsCombinedWithRefStruct, "T").WithLocation(2, 16),
                // (3,15): error CS9502: The 'allows' constraint clause must be the last constraint specified
                //     where T : allows ref struct, class
                Diagnostic(ErrorCode.ERR_AllowsClauseMustBeLast, "allows").WithLocation(3, 15),
                // (3,34): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
                //     where T : allows ref struct, class
                Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "class").WithLocation(3, 34)
                );

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.True(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_08_AfterClass()
        {
            var src = @"
public class C<T>
    where T : class, allows ref struct
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (2,16): error CS9503: Cannot allow ref structs for a type parameter known from other constraints to be a class
                // public class C<T>
                Diagnostic(ErrorCode.ERR_ClassIsCombinedWithRefStruct, "T").WithLocation(2, 16)
                );

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.True(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_09_FollowedByDefault()
        {
            var src = @"
public class C<T>
    where T : allows ref struct, default
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (3,15): error CS9502: The 'allows' constraint clause must be the last constraint specified
                //     where T : allows ref struct, default
                Diagnostic(ErrorCode.ERR_AllowsClauseMustBeLast, "allows").WithLocation(3, 15),
                // (3,34): error CS8823: The 'default' constraint is valid on override and explicit interface implementation methods only.
                //     where T : allows ref struct, default
                Diagnostic(ErrorCode.ERR_DefaultConstraintOverrideOnly, "default").WithLocation(3, 34),
                // (3,34): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
                //     where T : allows ref struct, default
                Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "default").WithLocation(3, 34)
                );

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_10_FollowedByDefault()
        {
            var src = @"
public class C
{
    public void M<T>()
        where T : allows ref struct, default
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (5,19): error CS9502: The 'allows' constraint clause must be the last constraint specified
                //         where T : allows ref struct, default
                Diagnostic(ErrorCode.ERR_AllowsClauseMustBeLast, "allows").WithLocation(5, 19),
                // (5,38): error CS8823: The 'default' constraint is valid on override and explicit interface implementation methods only.
                //         where T : allows ref struct, default
                Diagnostic(ErrorCode.ERR_DefaultConstraintOverrideOnly, "default").WithLocation(5, 38),
                // (5,38): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
                //         where T : allows ref struct, default
                Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "default").WithLocation(5, 38)
                );

            var method = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C.M");
            var t = method.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_11_FollowedByDefault()
        {
            var src = @"
public class C : B
{
    public override void M<T>()
        where T : allows ref struct, default
    {
    }
}

public class B
{
    public virtual void M<T>()
        where T : allows ref struct
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (5,19): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //         where T : allows ref struct, default
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "allows ref struct").WithLocation(5, 19)
                );

            var method = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C.M");
            var t = method.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_12_AfterDefault()
        {
            var src = @"
public class C<T>
    where T : default, allows ref struct
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (3,15): error CS8823: The 'default' constraint is valid on override and explicit interface implementation methods only.
                //     where T : default, allows ref struct
                Diagnostic(ErrorCode.ERR_DefaultConstraintOverrideOnly, "default").WithLocation(3, 15)
                );

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_13_AfterDefault()
        {
            var src = @"
public class C
{
    public void M<T>()
        where T : default, allows ref struct
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (5,19): error CS8823: The 'default' constraint is valid on override and explicit interface implementation methods only.
                //         where T : default, allows ref struct
                Diagnostic(ErrorCode.ERR_DefaultConstraintOverrideOnly, "default").WithLocation(5, 19)
                );

            var method = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C.M");
            var t = method.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_14_AfterDefault()
        {
            var src = @"
public class C : B
{
    public override void M<T>()
        where T : default, allows ref struct
    {
    }
}

public class B
{
    public virtual void M<T>()
        where T : allows ref struct
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (5,28): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //         where T : default, allows ref struct
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "allows ref struct").WithLocation(5, 28)
                );

            var method = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C.M");
            var t = method.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_15_FollowedByUnmanaged()
        {
            var src = @"
public class C<T>
    where T : allows ref struct, unmanaged
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (3,15): error CS9502: The 'allows' constraint clause must be the last constraint specified
                //     where T : allows ref struct, unmanaged
                Diagnostic(ErrorCode.ERR_AllowsClauseMustBeLast, "allows").WithLocation(3, 15),
                // (3,34): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
                //     where T : allows ref struct, unmanaged
                Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "unmanaged").WithLocation(3, 34)
                );

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_16_AfterUnmanaged()
        {
            var src = @"
public class C<T>
    where T : unmanaged, allows ref struct
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics();

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.True(t.HasValueTypeConstraint);
            Assert.True(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_17_FollowedByNotNull()
        {
            var src = @"
public class C<T>
    where T : allows ref struct, notnull
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (3,15): error CS9502: The 'allows' constraint clause must be the last constraint specified
                //     where T : allows ref struct, notnull
                Diagnostic(ErrorCode.ERR_AllowsClauseMustBeLast, "allows").WithLocation(3, 15),
                // (3,34): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
                //     where T : allows ref struct, notnull
                Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "notnull").WithLocation(3, 34)
                );

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.True(t.HasNotNullConstraint);
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_18_AfterNotNull()
        {
            var src = @"
public class C<T>
    where T : notnull, allows ref struct
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics();

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.True(t.HasNotNullConstraint);
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_19_FollowedByType()
        {
            var src = @"
public class C<T>
    where T : allows ref struct, I1
{
}

public interface I1 {}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (3,15): error CS9502: The 'allows' constraint clause must be the last constraint specified
                //     where T : allows ref struct, notnull
                Diagnostic(ErrorCode.ERR_AllowsClauseMustBeLast, "allows").WithLocation(3, 15)
                );

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);

            Assert.Equal("I1", t.ConstraintTypesNoUseSiteDiagnostics.Single().ToTestDisplayString());

            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_20_AfterType()
        {
            var src = @"
public class C<T>
    where T : I1, allows ref struct
{
}

public interface I1 {}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics();

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);

            Assert.Equal("I1", t.ConstraintTypesNoUseSiteDiagnostics.Single().ToTestDisplayString());

            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_21_AfterClassType()
        {
            var src = @"
public class C<T>
    where T : C1, allows ref struct
{
}

public class C1 {}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (2,16): error CS9503: Cannot allow ref structs for a type parameter known from other constraints to be a class
                // public class C<T>
                Diagnostic(ErrorCode.ERR_ClassIsCombinedWithRefStruct, "T").WithLocation(2, 16)
                );

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);

            Assert.Equal("C1", t.ConstraintTypesNoUseSiteDiagnostics.Single().ToTestDisplayString());

            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_22_AfterSystemValueType()
        {
            var src = @"
public class C<T>
    where T : System.ValueType, allows ref struct
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (3,15): error CS0702: Constraint cannot be special class 'ValueType'
                //     where T : System.ValueType, allows ref struct
                Diagnostic(ErrorCode.ERR_SpecialTypeAsBound, "System.ValueType").WithArguments("System.ValueType").WithLocation(3, 15)
                );

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);

            Assert.Empty(t.ConstraintTypesNoUseSiteDiagnostics);

            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_23_AfterSystemEnum()
        {
            var src = @"
public class C<T>
    where T : System.Enum, allows ref struct
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics();

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);

            Assert.Equal("System.Enum", t.ConstraintTypesNoUseSiteDiagnostics.Single().ToTestDisplayString());

            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_24_FollowedByNew()
        {
            var src = @"
public class C<T>
    where T : allows ref struct, new()
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (3,15): error CS9502: The 'allows' constraint clause must be the last constraint specified
                //     where T : allows ref struct, new()
                Diagnostic(ErrorCode.ERR_AllowsClauseMustBeLast, "allows").WithLocation(3, 15)
                );

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);
            Assert.True(t.HasConstructorConstraint);
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_25_AfterNew()
        {
            var src = @"
public class C<T>
    where T : new(), allows ref struct
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics();

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.HasReferenceTypeConstraint);
            Assert.False(t.HasValueTypeConstraint);
            Assert.False(t.HasUnmanagedTypeConstraint);
            Assert.False(t.HasNotNullConstraint);
            Assert.True(t.HasConstructorConstraint);
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_26_PartialTypes()
        {
            var src = @"
partial class C<T> where T : allows ref struct
{
}

partial class C<T> where T : allows ref struct
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics();

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_27_PartialTypes()
        {
            var src = @"
partial class C<T>
{
}

partial class C<T> where T : allows ref struct
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics();

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_28_PartialTypes()
        {
            var src = @"
partial class C<T> where T : allows ref struct
{
}

partial class C<T>
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics();

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_29_PartialTypes()
        {
            var src = @"
partial class C<T> where T : struct
{
}

partial class C<T> where T : struct, allows ref struct
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (2,15): error CS0265: Partial declarations of 'C<T>' have inconsistent constraints for type parameter 'T'
                // partial class C<T> where T : struct
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "C").WithArguments("C<T>", "T").WithLocation(2, 15)
                );

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.False(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_30_PartialTypes()
        {
            var src = @"
partial class C<T> where T : struct, allows ref struct
{
}

partial class C<T> where T : struct
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (2,15): error CS0265: Partial declarations of 'C<T>' have inconsistent constraints for type parameter 'T'
                // partial class C<T> where T : struct, allows ref struct
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "C").WithArguments("C<T>", "T").WithLocation(2, 15)
                );

            var c = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var t = c.TypeParameters.Single();
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_31_PartialMethod()
        {
            var src = @"
partial class C
{
    partial void M<T>() where T : allows ref struct;
}

partial class C
{
    partial void M<T>() where T : allows ref struct
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics();

            var method = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C.M");
            var t = method.TypeParameters.Single();
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_32_PartialMethod()
        {
            var src = @"
partial class C
{
    partial void M<T>();
}

partial class C
{
    partial void M<T>() where T : allows ref struct
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (9,18): error CS0761: Partial method declarations of 'C.M<T>()' have inconsistent constraints for type parameter 'T'
                //     partial void M<T>() where T : allows ref struct
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "M").WithArguments("C.M<T>()", "T").WithLocation(9, 18)
                );

            var method = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C.M");
            var t = method.TypeParameters.Single();
            Assert.False(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_33_PartialMethod()
        {
            var src = @"
partial class C
{
    partial void M<T>() where T : allows ref struct;
}

partial class C
{
    partial void M<T>()
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (9,18): error CS0761: Partial method declarations of 'C.M<T>()' have inconsistent constraints for type parameter 'T'
                //     partial void M<T>()
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "M").WithArguments("C.M<T>()", "T").WithLocation(9, 18)
                );

            var method = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C.M");
            var t = method.TypeParameters.Single();
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_34_PartialMethod()
        {
            var src = @"
partial class C
{
    partial void M<T>() where T : struct;
}

partial class C
{
    partial void M<T>() where T : struct, allows ref struct
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (9,18): error CS0761: Partial method declarations of 'C.M<T>()' have inconsistent constraints for type parameter 'T'
                //     partial void M<T>() where T : struct, allows ref struct
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "M").WithArguments("C.M<T>()", "T").WithLocation(9, 18)
                );

            var method = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C.M");
            var t = method.TypeParameters.Single();
            Assert.False(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_35_PartialMethod()
        {
            var src = @"
partial class C
{
    partial void M<T>() where T : struct, allows ref struct;
}

partial class C
{
    partial void M<T>() where T : struct
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (9,18): error CS0761: Partial method declarations of 'C.M<T>()' have inconsistent constraints for type parameter 'T'
                //     partial void M<T>() where T : struct
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "M").WithArguments("C.M<T>()", "T").WithLocation(9, 18)
                );

            var method = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C.M");
            var t = method.TypeParameters.Single();
            Assert.True(t.AllowByRefLike);
        }

        [Fact]
        public void AllowsConstraint_36_InheritedByOverride()
        {
            var src = @"
class C1
{
    public virtual void M1<T>() where T : allows ref struct
    {
    }
    public virtual void M2<T>() where T : unmanaged
    {
    }
}

class C2 : C1
{
    public override void M1<T>() where T : allows ref struct
    {
    }
}

class C3 : C1
{
    public override void M2<T>() where T : unmanaged
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (14,44): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void M1<T>() where T : allows ref struct
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "allows ref struct").WithLocation(14, 44),
                // (21,44): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void M2<T>() where T : unmanaged
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "unmanaged").WithLocation(21, 44)
                );

            var method1 = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C2.M1");
            var t1 = method1.TypeParameters.Single();
            Assert.True(t1.AllowByRefLike);

            var method2 = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C3.M2");
            var t2 = method2.TypeParameters.Single();
            Assert.True(t2.HasUnmanagedTypeConstraint);
        }

        [Fact]
        public void AllowsConstraint_37_InheritedByOverride()
        {
            var src1 = @"
public class C1
{
    public virtual void M1<T>() where T : allows ref struct
    {
    }
    public virtual void M2<T>() where T : unmanaged
    {
    }
}
";

            var src2 = @"
class C2 : C1
{
    public override void M1<T>()
    {
    }
    public override void M2<T>()
    {
    }
}
";
            var comp1 = CreateCompilation([src1, src2], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp1.VerifyDiagnostics();

            var method1 = comp1.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C2.M1");
            var t1 = method1.TypeParameters.Single();
            Assert.True(t1.AllowByRefLike);

            var method2 = comp1.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C2.M2");
            var t2 = method2.TypeParameters.Single();
            Assert.True(t2.HasUnmanagedTypeConstraint);

            CreateCompilation(src2, references: [comp1.ToMetadataReference()], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

            CreateCompilation(src2, references: [comp1.ToMetadataReference()], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (4,29): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override void M1<T>()
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "T").WithArguments("ref struct interfaces").WithLocation(4, 29)
                );

            var comp2 = CreateCompilation(src1, targetFramework: TargetFramework.Net70);

            CreateCompilation(src2, references: [comp2.ToMetadataReference()], targetFramework: TargetFramework.Net70).VerifyDiagnostics(
                // (4,29): error CS9500: Target runtime doesn't support by-ref-like generics.
                //     public override void M1<T>()
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportByRefLikeGenerics, "T").WithLocation(4, 29)
                );
        }

        [Fact]
        public void AllowsConstraint_38_InheritedByOverride()
        {
            var src = @"
class C1<S>
{
    public virtual void M1<T>() where T : S, allows ref struct
    {
    }
    public virtual void M2<T>() where T : class, S
    {
    }
}

class C2 : C1<C>
{
    public override void M1<T>()
    {
    }
    public override void M2<T>()
    {
    }
}

class C {}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics();

            var method1 = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C2.M1");
            var t1 = method1.TypeParameters.Single();
            Assert.True(t1.AllowByRefLike);
            Assert.Equal("C", t1.ConstraintTypesNoUseSiteDiagnostics.Single().ToTestDisplayString());

            var method2 = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C2.M2");
            var t2 = method2.TypeParameters.Single();
            Assert.True(t2.HasReferenceTypeConstraint);
            Assert.Equal("C", t2.ConstraintTypesNoUseSiteDiagnostics.Single().ToTestDisplayString());
        }

        [Fact]
        public void AllowsConstraint_39_InheritedByExplicitImplementation()
        {
            var src = @"
interface C1
{
    void M1<T>() where T : allows ref struct;
    void M2<T>() where T : unmanaged;
}

class C2 : C1
{
    void C1.M1<T>() where T : allows ref struct
    {
    }

    void C1.M2<T>() where T : unmanaged
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (10,31): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     void C1.M1<T>() where T : allows ref struct
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "allows ref struct").WithLocation(10, 31),
                // (14,31): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     void C1.M2<T>() where T : unmanaged
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "unmanaged").WithLocation(14, 31)
                );

            var method1 = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C2.C1.M1");
            var t1 = method1.TypeParameters.Single();
            Assert.True(t1.AllowByRefLike);

            var method2 = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C2.C1.M2");
            var t2 = method2.TypeParameters.Single();
            Assert.True(t2.HasUnmanagedTypeConstraint);
        }

        [Fact]
        public void AllowsConstraint_40_InheritedByExplicitImplementation()
        {
            var src1 = @"
public interface C1
{
    void M1<T>() where T : allows ref struct;
    void M2<T>() where T : unmanaged;
}
";

            var src2 = @"
class C2 : C1
{
    void C1.M1<T>()
    {
    }
    void C1.M2<T>()
    {
    }
}
";
            var comp1 = CreateCompilation([src1, src2], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp1.VerifyDiagnostics();

            var method1 = comp1.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C2.C1.M1");
            var t1 = method1.TypeParameters.Single();
            Assert.True(t1.AllowByRefLike);

            var method2 = comp1.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C2.C1.M2");
            var t2 = method2.TypeParameters.Single();
            Assert.True(t2.HasUnmanagedTypeConstraint);

            CreateCompilation(src2, references: [comp1.ToMetadataReference()], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

            CreateCompilation(src2, references: [comp1.ToMetadataReference()], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (4,16): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     void C1.M1<T>()
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "T").WithArguments("ref struct interfaces").WithLocation(4, 16)
                );

            var comp2 = CreateCompilation(src1, targetFramework: TargetFramework.Net70);

            CreateCompilation(src2, references: [comp2.ToMetadataReference()], targetFramework: TargetFramework.Net70).VerifyDiagnostics(
                // (4,16): error CS9500: Target runtime doesn't support by-ref-like generics.
                //     void C1.M1<T>()
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportByRefLikeGenerics, "T").WithLocation(4, 16)
                );
        }

        [Fact]
        public void AllowsConstraint_41_InheritedByExplicitImplementation()
        {
            var src = @"
interface C1<S>
{
    void M1<T>() where T : S, allows ref struct;
    void M2<T>() where T : class, S;
}

class C2 : C1<C>
{
    void C1<C>.M1<T>()
    {
    }
    void C1<C>.M2<T>()
    {
    }
}

class C {}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics();

            var method1 = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C2.C1<C>.M1");
            var t1 = method1.TypeParameters.Single();
            Assert.True(t1.AllowByRefLike);
            Assert.Equal("C", t1.ConstraintTypesNoUseSiteDiagnostics.Single().ToTestDisplayString());

            var method2 = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C2.C1<C>.M2");
            var t2 = method2.TypeParameters.Single();
            Assert.True(t2.HasReferenceTypeConstraint);
            Assert.Equal("C", t2.ConstraintTypesNoUseSiteDiagnostics.Single().ToTestDisplayString());
        }

        [Fact]
        public void AllowsConstraint_42_ImplicitImplementationMustMatch()
        {
            var src = @"
interface C1
{
    void M1<T>() where T : allows ref struct;
    void M2<T>() where T : unmanaged;
}

class C2 : C1
{
    public void M1<T>() where T : allows ref struct
    {
    }

    public void M2<T>() where T : unmanaged
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AllowsConstraint_43_ImplicitImplementationMustMatch()
        {
            var src = @"
interface C1
{
    void M1<T>() where T : allows ref struct;
    void M2<T>() where T : unmanaged;
}

class C2 : C1
{
    public void M1<T>()
    {
    }
    public void M2<T>()
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (10,17): error CS0425: The constraints for type parameter 'T' of method 'C2.M1<T>()' must match the constraints for type parameter 'T' of interface method 'C1.M1<T>()'. Consider using an explicit interface implementation instead.
                //     public void M1<T>()
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M1").WithArguments("T", "C2.M1<T>()", "T", "C1.M1<T>()").WithLocation(10, 17),
                // (13,17): error CS0425: The constraints for type parameter 'T' of method 'C2.M2<T>()' must match the constraints for type parameter 'T' of interface method 'C1.M2<T>()'. Consider using an explicit interface implementation instead.
                //     public void M2<T>()
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M2").WithArguments("T", "C2.M2<T>()", "T", "C1.M2<T>()").WithLocation(13, 17)
                );
        }

        [Fact]
        public void AllowsConstraint_44_ImplicitImplementationMustMatch()
        {
            var src = @"
interface C1<S>
{
    void M1<T>() where T : S, allows ref struct;
    void M2<T>() where T : class, S;
}

class C2 : C1<C>
{
    public void M1<T>() where T : C, allows ref struct
    {
    }
    public void M2<T>() where T : class, C
    {
    }
}

class C {}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (10,20): error CS9503: Cannot allow ref structs for a type parameter known from other constraints to be a class
                //     public void M1<T>() where T : C, allows ref struct
                Diagnostic(ErrorCode.ERR_ClassIsCombinedWithRefStruct, "T").WithLocation(10, 20),
                // (13,17): error CS0425: The constraints for type parameter 'T' of method 'C2.M2<T>()' must match the constraints for type parameter 'T' of interface method 'C1<C>.M2<T>()'. Consider using an explicit interface implementation instead.
                //     public void M2<T>() where T : class, C
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M2").WithArguments("T", "C2.M2<T>()", "T", "C1<C>.M2<T>()").WithLocation(13, 17),
                // (13,42): error CS0450: 'C': cannot specify both a constraint class and the 'class' or 'struct' constraint
                //     public void M2<T>() where T : class, C
                Diagnostic(ErrorCode.ERR_RefValBoundWithClass, "C").WithArguments("C").WithLocation(13, 42)
                );
        }

        [Fact]
        public void AllowsConstraint_45_NotPresent()
        {
            var src = @"
public class C<T>
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify, verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var t = c.TypeParameters.Single();
                Assert.False(t.HasReferenceTypeConstraint);
                Assert.False(t.HasValueTypeConstraint);
                Assert.False(t.HasUnmanagedTypeConstraint);
                Assert.False(t.HasNotNullConstraint);
                Assert.False(t.AllowByRefLike);
            }
        }

        [Fact]
        public void AllowsConstraint_46()
        {
            var src = @"
class C<T, U>
    where T : allows ref struct
    where U : T, allows ref struct
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify, verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var t = c.TypeParameters[0];
                Assert.False(t.HasReferenceTypeConstraint);
                Assert.False(t.HasValueTypeConstraint);
                Assert.False(t.HasUnmanagedTypeConstraint);
                Assert.False(t.HasNotNullConstraint);
                Assert.True(t.AllowByRefLike);

                var u = c.TypeParameters[1];
                Assert.False(u.HasReferenceTypeConstraint);
                Assert.False(u.HasValueTypeConstraint);
                Assert.False(u.HasUnmanagedTypeConstraint);
                Assert.False(u.HasNotNullConstraint);
                Assert.True(u.AllowByRefLike);
            }
        }

        [Fact]
        public void AllowsConstraint_47()
        {
            var src = @"
class C<T, U>
    where T : allows ref struct
    where U : T
{
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify, verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var t = c.TypeParameters[0];
                Assert.False(t.HasReferenceTypeConstraint);
                Assert.False(t.HasValueTypeConstraint);
                Assert.False(t.HasUnmanagedTypeConstraint);
                Assert.False(t.HasNotNullConstraint);
                Assert.True(t.AllowByRefLike);

                var u = c.TypeParameters[1];
                Assert.False(u.HasReferenceTypeConstraint);
                Assert.False(u.HasValueTypeConstraint);
                Assert.False(u.HasUnmanagedTypeConstraint);
                Assert.False(u.HasNotNullConstraint);
                Assert.False(u.AllowByRefLike);
            }
        }

        [Fact]
        public void ImplementAnInterface_01()
        {
            var src = @"
interface I1
{}

ref struct S1 : I1
{}
";
            var comp = CreateCompilation(src);

            CompileAndVerify(comp, sourceSymbolValidator: verify, symbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var s1 = m.GlobalNamespace.GetMember<NamedTypeSymbol>("S1");
                Assert.Equal("I1", s1.InterfacesNoUseSiteDiagnostics().Single().ToTestDisplayString());
            }

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (5,17): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // ref struct S1 : I1
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "I1").WithArguments("ref struct interfaces").WithLocation(5, 17)
                );
        }

        [Fact]
        public void ImplementAnInterface_02_IllegalBoxing()
        {
            var src = @"
interface I1
{}

ref struct S1 : I1
{}

class C
{
    static I1 Test1(S1 x) => x;
    static I1 Test2(S1 x) => (I1)x;
}
";
            var comp = CreateCompilation(src);

            comp.VerifyDiagnostics(
                // (10,30): error CS0029: Cannot implicitly convert type 'S1' to 'I1'
                //     static I1 Test1(S1 x) => x;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("S1", "I1").WithLocation(10, 30),
                // (11,30): error CS0030: Cannot convert type 'S1' to 'I1'
                //     static I1 Test2(S1 x) => (I1)x;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(I1)x").WithArguments("S1", "I1").WithLocation(11, 30)
                );
        }

        [Fact]
        public void ImplementAnInterface_03()
        {
            var src = @"
interface I1
{
    void M1();
}

ref struct S1 : I1
{
    public void M1()
    {
        System.Console.Write(""S1.M1"");
    }
}

class C
{
    static void Main()
    {
        Test(new S1());
    }
    
    static void Test<T>(T x) where T : I1
    {
        x.M1();
    }
}
";
            var comp = CreateCompilation(src);

            comp.VerifyDiagnostics(
                // (19,9): error CS9504: The type 'S1' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'C.Test<T>(T)'
                //         Test(new S1());
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "Test").WithArguments("C.Test<T>(T)", "T", "S1").WithLocation(19, 9)
                );
        }

        [Fact]
        public void ImplementAnInterface_04()
        {
            var src = @"
interface I1
{
    void M1();
}

ref struct S1 : I1
{
    public void M1()
    {
        System.Console.Write(""S1.M1"");
    }
}

class C
{
    static void Main()
    {
        Test1(new S1());
        System.Console.Write("" "");
        Test2(new S1());
    }
    
    static void Test1<T>(T x) where T : I1, allows ref struct
    {
        x.M1();
    }
    
    static void Test2<T>(T x) where T : I1, allows ref struct
    {
        Test1(x);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? @"S1.M1 S1.M1" : null, verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();
            verifier.VerifyIL("C.Test1<T>(T)",
@"
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  constrained. ""T""
  IL_0008:  callvirt   ""void I1.M1()""
  IL_000d:  ret
}
");
        }

        [Fact]
        public void ImplementAnInterface_05_Variance()
        {
            var src = @"
interface I1<in T>
{
    void M1(T x);
}

ref struct S1 : I1<object>
{
    public void M1(object x)
    {
        System.Console.Write(""S1.M1"");
        System.Console.Write("" "");
        System.Console.Write(x);
    }
}

class C
{
    static void Main()
    {
        Test(new S1(), ""y"");
    }
    
    static void Test<T>(T x, string y) where T : I1<string>, allows ref struct
    {
        x.M1(y);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? @"S1.M1 y" : null, verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();
            verifier.VerifyIL("C.Test<T>(T, string)",
@"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarg.1
  IL_0003:  constrained. ""T""
  IL_0009:  callvirt   ""void I1<string>.M1(string)""
  IL_000e:  ret
}
");
        }

        [Fact]
        public void ImplementAnInterface_06_DefaultImplementation()
        {
            var src1 = @"
public interface I1
{
    virtual void M1() {}
    static virtual void M2() {}
    sealed void M3() {}

    public class C1 {}
}

ref struct S1 : I1
{
}

ref struct S2 : I1
{
    public void M1()
    {
    }
}
";

            var src2 = @"
class C
{
    static void Test1(I1 x)
    {
        x.M1();
        x.M3();
    }

    static void Test2<T>(T x) where T : I1, allows ref struct
    {
        x.M1();
        T.M2();
#line 100
        x.M3();
        _ = new T.C1();
    }
}
";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.NetCoreApp);

            comp1.VerifyDiagnostics(
                // (11,17): error CS9505: 'I1.M1()' cannot implement interface member 'I1.M1()' for ref struct 'S1'.
                // ref struct S1 : I1
                Diagnostic(ErrorCode.ERR_RefStructDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.M1()", "I1.M1()", "S1").WithLocation(11, 17)
                );

            comp1 = CreateCompilation(src1, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);
            var comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            // PROTOTYPE(RefStructInterfaces): Specification suggests to report a warning for access to virtual (non-abstract) members.
            comp2.VerifyDiagnostics(
                // (100,9): error CS9506: A non-virtual instance interface member cannot be accessed on a type parameter that allows ref struct.
                //         x.M3();
                Diagnostic(ErrorCode.ERR_BadNonVirtualInterfaceMemberAccessOnAllowsRefLike, "x.M3").WithLocation(100, 9),
                // (101,17): error CS0704: Cannot do non-virtual member lookup in 'T' because it is a type parameter
                //         _ = new T.C1();
                Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T.C1").WithArguments("T").WithLocation(101, 17)
                );
        }

        [Fact]
        public void ImplementAnInterface_07_DefaultImplementation()
        {
            var src1 = @"
public interface I1
{
    virtual int P1 => 1;
    static virtual int P2 => 2;
    sealed int P3 => 3;
}

ref struct S1 : I1
{
}

ref struct S2 : I1
{
    public int P1 => 21;
}
";

            var src2 = @"
class C
{
    static void Test1(I1 x)
    {
        _ = x.P1;
        _ = x.P3;
    }

    static void Test2<T>(T x) where T : I1, allows ref struct
    {
        _ = x.P1;
        _ = T.P2;
#line 100
        _ = x.P3;
    }
}
";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.NetCoreApp);

            comp1.VerifyDiagnostics(
                // (9,17): error CS9505: 'I1.P1.get' cannot implement interface member 'I1.P1.get' for ref struct 'S1'.
                // ref struct S1 : I1
                Diagnostic(ErrorCode.ERR_RefStructDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P1.get", "I1.P1.get", "S1").WithLocation(9, 17)
                );

            comp1 = CreateCompilation(src1, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);
            var comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            // PROTOTYPE(RefStructInterfaces): Specification suggests to report a warning for access to virtual (non-abstract) members.
            comp2.VerifyDiagnostics(
                // (100,13): error CS9506: A non-virtual instance interface member cannot be accessed on a type parameter that allows ref struct.
                //         _ = x.P3;
                Diagnostic(ErrorCode.ERR_BadNonVirtualInterfaceMemberAccessOnAllowsRefLike, "x.P3").WithLocation(100, 13)
                );
        }

        [Fact]
        public void ImplementAnInterface_08_DefaultImplementation()
        {
            var src1 = @"
public interface I1
{
    virtual int P1 {set{}}
    static virtual int P2 {set{}}
    sealed int P3 {set{}}
}

ref struct S1 : I1
{
}

ref struct S2 : I1
{
    public int P1 {set{}}
}
";

            var src2 = @"
class C
{
    static void Test1(I1 x)
    {
        x.P1 = 11;
        x.P3 = 11;
    }

    static void Test2<T>(T x) where T : I1, allows ref struct
    {
        x.P1 = 123;
        T.P2 = 123;
        x.P3 = 123;
    }
}
";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.NetCoreApp);

            comp1.VerifyDiagnostics(
                // (9,17): error CS9505: 'I1.P1.set' cannot implement interface member 'I1.P1.set' for ref struct 'S1'.
                // ref struct S1 : I1
                Diagnostic(ErrorCode.ERR_RefStructDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P1.set", "I1.P1.set", "S1").WithLocation(9, 17)
                );

            comp1 = CreateCompilation(src1, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);
            var comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            // PROTOTYPE(RefStructInterfaces): Specification suggests to report a warning for access to virtual (non-abstract) members.
            comp2.VerifyDiagnostics(
                // (14,9): error CS9506: A non-virtual instance interface member cannot be accessed on a type parameter that allows ref struct.
                //         x.P3 = 123;
                Diagnostic(ErrorCode.ERR_BadNonVirtualInterfaceMemberAccessOnAllowsRefLike, "x.P3").WithLocation(14, 9)
                );
        }

        [Fact]
        public void ImplementAnInterface_09_DefaultImplementation()
        {
            var src1 = @"
public interface I1
{
    virtual int this[int x] => 1;
}

public interface I2
{
    sealed int this[long x] => 1;
}

ref struct S1 : I1, I2
{
}

ref struct S2 : I1, I2
{
    public int this[int x] => 21;
}
";

            var src2 = @"
class C
{
    static void Test1(I1 x)
    {
        _ = x[1];
    }

    static void Test1(I2 x)
    {
        _ = x[2];
    }

    static void Test2<T>(T x) where T : I1, allows ref struct
    {
        _ = x[3];
    }

    static void Test3<T>(T x) where T : I2, allows ref struct
    {
        _ = x[4];
    }
}
";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.NetCoreApp);

            comp1.VerifyDiagnostics(
                // (12,17): error CS9505: 'I1.this[int].get' cannot implement interface member 'I1.this[int].get' for ref struct 'S1'.
                // ref struct S1 : I1, I2
                Diagnostic(ErrorCode.ERR_RefStructDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[int].get", "I1.this[int].get", "S1").WithLocation(12, 17)
                );

            comp1 = CreateCompilation(src1, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);
            var comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            // PROTOTYPE(RefStructInterfaces): Specification suggests to report a warning for access to virtual (non-abstract) members.
            comp2.VerifyDiagnostics(
                // (21,13): error CS9506: A non-virtual instance interface member cannot be accessed on a type parameter that allows ref struct.
                //         _ = x[4];
                Diagnostic(ErrorCode.ERR_BadNonVirtualInterfaceMemberAccessOnAllowsRefLike, "x[4]").WithLocation(21, 13)
                );
        }

        [Fact]
        public void ImplementAnInterface_10_DefaultImplementation()
        {
            var src1 = @"
public interface I1
{
    virtual int this[int x] {set{}}
}

public interface I2
{
    sealed int this[long x] {set{}}
}

ref struct S1 : I1, I2
{
}

ref struct S2 : I1, I2
{
    public int this[int x] {set{}}
}
";

            var src2 = @"
class C
{
    static void Test1(I1 x)
    {
        x[1] = 1;
    }

    static void Test1(I2 x)
    {
        x[2] = 2;
    }

    static void Test2<T>(T x) where T : I1, allows ref struct
    {
        x[3] = 3;
    }

    static void Test3<T>(T x) where T : I2, allows ref struct
    {
        x[4] = 4;
    }
}
";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.NetCoreApp);

            comp1.VerifyDiagnostics(
                // (12,17): error CS9505: 'I1.this[int].set' cannot implement interface member 'I1.this[int].set' for ref struct 'S1'.
                // ref struct S1 : I1, I2
                Diagnostic(ErrorCode.ERR_RefStructDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[int].set", "I1.this[int].set", "S1").WithLocation(12, 17)
                );

            comp1 = CreateCompilation(src1, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);
            var comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            // PROTOTYPE(RefStructInterfaces): Specification suggests to report a warning for access to virtual (non-abstract) members.
            comp2.VerifyDiagnostics(
                // (21,9): error CS9506: A non-virtual instance interface member cannot be accessed on a type parameter that allows ref struct.
                //         x[4] = 4;
                Diagnostic(ErrorCode.ERR_BadNonVirtualInterfaceMemberAccessOnAllowsRefLike, "x[4]").WithLocation(21, 9)
                );
        }

        [Fact]
        public void ImplementAnInterface_11_DefaultImplementation()
        {
            var src1 = @"
public interface I1
{
    virtual event System.Action E1 {add{} remove{}}
    static virtual event System.Action E2 {add{} remove{}}
    sealed event System.Action E3 {add{} remove{}}
}

ref struct S1 : I1
{
}

ref struct S2 : I1
{
#pragma warning disable CS0067 // The event 'S2.E1' is never used
    public event System.Action E1;
}
";

            var src2 = @"
class C
{
    static void Test1(I1 x)
    {
        x.E1 += null;
        x.E3 += null;
        x.E1 -= null;
        x.E3 -= null;
    }

    static void Test2<T>(T x) where T : I1, allows ref struct
    {
        x.E1 += null;
        T.E2 += null;
#line 100
        x.E3 += null;
        x.E1 -= null;
        T.E2 -= null;
#line 200
        x.E3 -= null;
    }
}
";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.NetCoreApp);

            comp1.VerifyDiagnostics(
                // (9,17): error CS9505: 'I1.E1.add' cannot implement interface member 'I1.E1.add' for ref struct 'S1'.
                // ref struct S1 : I1
                Diagnostic(ErrorCode.ERR_RefStructDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.E1.add", "I1.E1.add", "S1").WithLocation(9, 17),
                // (9,17): error CS9505: 'I1.E1.remove' cannot implement interface member 'I1.E1.remove' for ref struct 'S1'.
                // ref struct S1 : I1
                Diagnostic(ErrorCode.ERR_RefStructDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.E1.remove", "I1.E1.remove", "S1").WithLocation(9, 17)
                );

            comp1 = CreateCompilation(src1, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);
            var comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            // PROTOTYPE(RefStructInterfaces): Specification suggests to report a warning for access to virtual (non-abstract) members.
            comp2.VerifyDiagnostics(
                // (100,9): error CS9506: A non-virtual instance interface member cannot be accessed on a type parameter that allows ref struct.
                //         x.E3 += null;
                Diagnostic(ErrorCode.ERR_BadNonVirtualInterfaceMemberAccessOnAllowsRefLike, "x.E3 += null").WithLocation(100, 9),
                // (200,9): error CS9506: A non-virtual instance interface member cannot be accessed on a type parameter that allows ref struct.
                //         x.E3 -= null;
                Diagnostic(ErrorCode.ERR_BadNonVirtualInterfaceMemberAccessOnAllowsRefLike, "x.E3 -= null").WithLocation(200, 9)
                );
        }

        [Fact]
        public void ImplementAnInterface_12_Variance_ErrorScenarios()
        {
            var src = @"
interface I<T>
{
    void M(T t);
}
interface IOut<out T>
{
    T MOut();
}
ref struct S : I<object>, IOut<object>
{
    public void M(object o) { }
    public object MOut() => null;
}
class Program
{
    static void Main()
    {
        Test1(new S());
        Test2(new S());
    }
    static void Test1<T>(T x) where T : I<string>, allows ref struct
    {
    }
    static void Test2<T>(T x) where T : IOut<string>, allows ref struct
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (19,9): error CS0315: The type 'S' cannot be used as type parameter 'T' in the generic type or method 'Program.Test1<T>(T)'. There is no boxing conversion from 'S' to 'I<string>'.
                //         Test1(new S());
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "Test1").WithArguments("Program.Test1<T>(T)", "I<string>", "T", "S").WithLocation(19, 9),
                // (20,9): error CS0315: The type 'S' cannot be used as type parameter 'T' in the generic type or method 'Program.Test2<T>(T)'. There is no boxing conversion from 'S' to 'IOut<string>'.
                //         Test2(new S());
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "Test2").WithArguments("Program.Test2<T>(T)", "IOut<string>", "T", "S").WithLocation(20, 9)
                );
        }

        [Fact]
        public void ConstraintsCheck_01()
        {
            var src = @"
ref struct S1
{
}

class C
{
    static void Main()
    {
        Test(new S1());
    }
    
    static void Test<T>(T x)
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (10,9): error CS9504: The type 'S1' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'C.Test<T>(T)'
                //         Test(new S1());
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "Test").WithArguments("C.Test<T>(T)", "T", "S1").WithLocation(10, 9)
                );
        }

        [Fact]
        public void ConstraintsCheck_02()
        {
            var src1 = @"
public class Helper
{
#line 100
    public static void Test<T>(T x) where T : allows ref struct
    {
        System.Console.Write(""Called"");
    }
}
";
            var src2 = @"
ref struct S1
{
}

class C
{
    static void Main()
    {
#line 200
        Helper.Test(new S1());
    }
}
";
            var comp = CreateCompilation([src1, src2], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? @"Called" : null, verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            CreateCompilation([src1, src2], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
            CreateCompilation([src1, src2], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (100,54): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static void Test<T>(T x) where T : allows ref struct
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "ref struct").WithArguments("ref struct interfaces").WithLocation(100, 54)
                );

            var comp1Ref = CreateCompilation(src1, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics).ToMetadataReference();

            comp = CreateCompilation(src2, references: [comp1Ref], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? @"Called" : null, verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            CreateCompilation(src2, references: [comp1Ref], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
            CreateCompilation(src2, references: [comp1Ref], targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (200,9): error CS8652: The feature 'ref struct interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Helper.Test(new S1());
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "Helper.Test").WithArguments("ref struct interfaces").WithLocation(200, 9)
                );

            CreateCompilation([src1, src2], targetFramework: TargetFramework.DesktopLatestExtended, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (100,54): error CS9500: Target runtime doesn't support by-ref-like generics.
                //     public static void Test<T>(T x) where T : allows ref struct
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportByRefLikeGenerics, "ref struct").WithLocation(100, 54)
                );

            comp1Ref = CreateCompilation(src1, targetFramework: TargetFramework.DesktopLatestExtended).ToMetadataReference();

            CreateCompilation(src2, references: [comp1Ref], targetFramework: TargetFramework.DesktopLatestExtended, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (200,9): error CS9500: Target runtime doesn't support by-ref-like generics.
                //         Helper.Test(new S1());
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportByRefLikeGenerics, "Helper.Test").WithLocation(200, 9)
                );
        }

        [Fact]
        public void ConstraintsCheck_03()
        {
            var src = @"
ref struct S1
{
}

class C
{
    static void Test1<T>(T x) where T : allows ref struct
    {
        Test2(x);
        Test2<T>(x);
    }

    static void Test2<T>(T x)
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (10,9): error CS9504: The type 'T' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'C.Test2<T>(T)'
                //         Test2(x);
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "Test2").WithArguments("C.Test2<T>(T)", "T", "T").WithLocation(10, 9),
                // (11,9): error CS9504: The type 'T' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'C.Test2<T>(T)'
                //         Test2<T>(x);
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "Test2<T>").WithArguments("C.Test2<T>(T)", "T", "T").WithLocation(11, 9)
                );
        }

        [Fact]
        public void ConstraintsCheck_04()
        {
            var src = @"
ref struct S1
{
}

class C
{
    static void Main()
    {
        Test2((byte)2);
        Test3((int)3);
        Test3(new S1());
        Test4((long)4);
    }

    static void Test1<T>(T x) where T : allows ref struct
    {
        System.Console.WriteLine(""Called {0}"", typeof(T));
    }

    static void Test2<T>(T x)
    {
        Test1(x);
        Test1<T>(x);
    }

    static void Test3<T>(T x) where T : allows ref struct
    {
        Test1(x);
        Test1<T>(x);
    }

    static void Test4<T>(T x)
    {
        Test2(x);
        Test2<T>(x);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: !ExecutionConditionUtil.IsMonoOrCoreClr ? null : @"
Called System.Byte
Called System.Byte
Called System.Int32
Called System.Int32
Called S1
Called S1
Called System.Int64
Called System.Int64
Called System.Int64
Called System.Int64", verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void ConstraintsCheck_05()
        {
            var src = @"
ref struct S1
{
}

class C<T, S>
    where T : allows ref struct
    where S : T
{
    static void Main()
    {
        _ = typeof(C<S1, S1>);
        _ = typeof(C<int, int>);
        _ = typeof(C<object, object>);
        _ = typeof(C<object, string>);
        _ = typeof(C<T, T>);
        _ = typeof(C<S, S>);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            comp.VerifyDiagnostics(
                // (12,26): error CS9504: The type 'S1' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'S' in the generic type or method 'C<T, S>'
                //         _ = typeof(C<S1, S1>);
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "S1").WithArguments("C<T, S>", "S", "S1").WithLocation(12, 26),
                // (16,25): error CS9504: The type 'T' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'S' in the generic type or method 'C<T, S>'
                //         _ = typeof(C<T, T>);
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "T").WithArguments("C<T, S>", "S", "T").WithLocation(16, 25)
                );
        }

        [Fact]
        public void IllegalBoxing_01()
        {
            var src = @"
public class Helper
{
    public static object Test1<T>(T x) where T : allows ref struct
    {
        return x;
    }

    public static object Test2<T>(T x) where T : allows ref struct
    {
        return (object)x;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            // PROTOTYPE(RefStructInterfaces): The boxing should be disallowed.
            comp.VerifyDiagnostics(
                );
        }

        [Fact]
        public void IllegalBoxing_02()
        {
            var src = @"
public interface I1
{
}

public class Helper
{
    public static I1 Test1<T>(T x) where T : I1, allows ref struct
    {
        return x;
    }

    public static I1 Test2<T>(T x) where T : I1, allows ref struct
    {
        return (I1)x;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            // PROTOTYPE(RefStructInterfaces): The boxing should be disallowed.
            comp.VerifyDiagnostics(
                );
        }

        [Fact]
        public void Unboxing_01()
        {
            var src = @"
public interface I1
{
}

public class Helper
{
    static U Test1<T, U>(T x)
        where T : allows ref struct
        where U : T
    {
        return x;
    }
    static U Test2<T, U>(T x)
        where T : allows ref struct
        where U : T
    {
        return (U)x;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);
            comp.VerifyDiagnostics(
                // (12,16): error CS0266: Cannot implicitly convert type 'T' to 'U'. An explicit conversion exists (are you missing a cast?)
                //         return x;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("T", "U").WithLocation(12, 16)
                );
        }
    }
}
