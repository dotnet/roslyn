// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_NullablePublicOnly : CSharpTestBase
    {
        [Fact]
        public void ExplicitAttribute_FromSource()
        {
            var source =
@"public class A<T>
{
}
public class B : A<object?>
{
}";
            var options = WithNonNullTypesTrue().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;

            var comp = CreateCompilation(new[] { NullablePublicOnlyAttributeDefinition, source }, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(new[] { NullablePublicOnlyAttributeDefinition, source }, options: options, parseOptions: parseOptions.WithFeature("nullablePublicOnly"));
            CompileAndVerify(comp, symbolValidator: AssertNullablePublicOnlyAttribute);
        }

        [Fact]
        public void ExplicitAttribute_FromMetadata()
        {
            var source =
@"public class A<T>
{
}
public class B : A<object?>
{
}";
            var comp = CreateCompilation(NullablePublicOnlyAttributeDefinition, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics();
            var ref1 = comp.EmitToImageReference();

            var options = WithNonNullTypesTrue().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;

            comp = CreateCompilation(source, references: new[] { ref1 }, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(source, references: new[] { ref1 }, options: options, parseOptions: parseOptions.WithFeature("nullablePublicOnly"));
            CompileAndVerify(comp, symbolValidator: AssertNullablePublicOnlyAttribute);
        }

        [Fact]
        public void ExplicitAttribute_MissingParameterlessConstructor()
        {
            var source1 =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NullablePublicOnlyAttribute : Attribute
    {
        public NullablePublicOnlyAttribute(bool b) { }
    }
}";
            var source2 =
@"public class A<T>
{
}
public class B : A<object?>
{
}";
            var options = WithNonNullTypesTrue().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;

            var comp = CreateCompilation(new[] { source1, source2 }, options: options, parseOptions: parseOptions);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(new[] { source1, source2 }, options: options, parseOptions: parseOptions.WithFeature("nullablePublicOnly"));
            comp.VerifyEmitDiagnostics(
                // error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullablePublicOnlyAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Runtime.CompilerServices.NullablePublicOnlyAttribute", ".ctor").WithLocation(1, 1));
        }

        [Fact]
        public void EmptyProject()
        {
            var source = @"";
            var options = WithNonNullTypesTrue().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;

            var comp = CreateCompilation(source, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(source, options: options, parseOptions: parseOptions.WithFeature("nullablePublicOnly"));
            CompileAndVerify(comp, symbolValidator: AssertNullablePublicOnlyAttribute);
        }

        [Fact]
        public void CSharp7_NoAttribute()
        {
            var source =
@"public class A<T>
{
}
public class B : A<object>
{
}";
            var options = TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular7;

            var comp = CreateCompilation(source, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(source, options: options, parseOptions: parseOptions.WithFeature("nullablePublicOnly"));
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);
        }

        [Fact]
        public void NullableEnabled()
        {
            var source =
@"public class A<T>
{
}
public class B : A<object?>
{
}";
            var options = WithNonNullTypesTrue().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;

            var comp = CreateCompilation(source, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(source, options: options, parseOptions: parseOptions.WithFeature("nullablePublicOnly"));
            CompileAndVerify(comp, symbolValidator: AssertNullablePublicOnlyAttribute);
        }

        [Fact]
        public void NullableEnabled_NoPublicMembers()
        {
            var source =
@"class A<T>
{
}
class B : A<object?>
{
}";
            var options = WithNonNullTypesTrue().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;

            var comp = CreateCompilation(source, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(source, options: options, parseOptions: parseOptions.WithFeature("nullablePublicOnly"));
            CompileAndVerify(comp, symbolValidator: AssertNullablePublicOnlyAttribute);
        }

        [Fact]
        public void NullableDisabled()
        {
            var source =
@"public class A<T>
{
}
public class B : A<object?>
{
}";
            var options = WithNonNullTypesFalse().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;

            var comp = CreateCompilation(source, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(source, options: options, parseOptions: parseOptions.WithFeature("nullablePublicOnly"));
            CompileAndVerify(comp, symbolValidator: AssertNullablePublicOnlyAttribute);
        }

        private static void AssertNoNullablePublicOnlyAttribute(ModuleSymbol module)
        {
            AssertAttributes(module.GetAttributes());
        }

        private static void AssertNullablePublicOnlyAttribute(ModuleSymbol module)
        {
            AssertAttributes(module.GetAttributes(), "System.Runtime.CompilerServices.NullablePublicOnlyAttribute");
        }

        private static void AssertAttributes(ImmutableArray<CSharpAttributeData> attributes, params string[] expectedNames)
        {
            var actualNames = attributes.Select(a => a.AttributeClass.ToTestDisplayString()).ToArray();
            AssertEx.SetEqual(actualNames, expectedNames);
        }
    }
}
