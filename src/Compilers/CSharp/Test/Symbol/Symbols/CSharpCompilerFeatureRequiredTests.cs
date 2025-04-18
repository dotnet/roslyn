// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols;

public class CSharpCompilerFeatureRequiredTests : BaseCompilerFeatureRequiredTests<CSharpCompilation, CSharpTestSource>
{
    private class CompilerFeatureRequiredTests_CSharp : CSharpTestBase { }

    private readonly CompilerFeatureRequiredTests_CSharp _csharpTest = new CompilerFeatureRequiredTests_CSharp();

    protected override CSharpTestSource GetUsage() => """
        #pragma warning disable 168 // Unused local
        OnType onType;
        OnType.M();
        OnMethod.M();
        OnMethodReturn.M();
        OnParameter.M(1);
        _ = OnField.Field;
        OnProperty.Property = 1;
        _ = OnProperty.Property;
        OnPropertySetter.Property = 1;
        _ = OnPropertySetter.Property;
        OnPropertyGetter.Property = 1;
        _ = OnPropertyGetter.Property;
        OnEvent.Event += () => {};
        OnEvent.Event -= () => {};
        OnEventAdder.Event += () => {};
        OnEventAdder.Event -= () => {};
        OnEventRemover.Event += () => {};
        OnEventRemover.Event -= () => {};
        OnEnum onEnum;
        _ = OnEnumMember.A;
        OnClassTypeParameter<int> onClassTypeParameter;
        OnMethodTypeParameter.M<int>();
        OnDelegateType onDelegateType;
        OnIndexedPropertyParameter.set_Property(1, 1);
        _ = OnIndexedPropertyParameter.get_Property(1);
        new OnThisIndexerParameter()[1] = 1;
        _ = new OnThisIndexerParameter()[1];
        """;

    internal override string VisualizeRealIL(IModuleSymbol peModule, CompilationTestData.MethodData methodData, IReadOnlyDictionary<int, string>? markers, bool areLocalsZeroed)
    {
        return _csharpTest.VisualizeRealIL(peModule, methodData, markers, areLocalsZeroed);
    }

    protected override CSharpCompilation CreateCompilationWithIL(CSharpTestSource source, string ilSource)
    {
        return CSharpTestBase.CreateCompilationWithIL(source, ilSource);
    }

    protected override CSharpCompilation CreateCompilation(CSharpTestSource source, MetadataReference[] references)
    {
        return CSharpTestBase.CreateCompilation(source, references);
    }

    protected override CompilationVerifier CompileAndVerify(CSharpCompilation compilation)
    {
        return _csharpTest.CompileAndVerify(compilation);
    }

    protected override void AssertNormalErrors(CSharpCompilation comp)
    {
        comp.VerifyDiagnostics(
            // (2,1): error CS9041: 'OnType' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnType onType;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnType").WithArguments("OnType", "test").WithLocation(2, 1),
            // (3,1): error CS9041: 'OnType' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnType.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnType").WithArguments("OnType", "test").WithLocation(3, 1),
            // (3,8): error CS9041: 'OnType' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnType.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "M").WithArguments("OnType", "test").WithLocation(3, 8),
            // (4,10): error CS9041: 'OnMethod.M()' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethod.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "M").WithArguments("OnMethod.M()", "test").WithLocation(4, 10),
            // (5,16): error CS9041: 'void value' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethodReturn.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "M").WithArguments("void value", "test").WithLocation(5, 16),
            // (6,13): error CS9041: 'int param' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnParameter.M(1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "M").WithArguments("int param", "test").WithLocation(6, 13),
            // (7,13): error CS9041: 'OnField.Field' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnField.Field;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Field").WithArguments("OnField.Field", "test").WithLocation(7, 13),
            // (8,12): error CS9041: 'OnProperty.Property' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnProperty.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnProperty.Property", "test").WithLocation(8, 12),
            // (9,16): error CS9041: 'OnProperty.Property' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnProperty.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnProperty.Property", "test").WithLocation(9, 16),
            // (10,18): error CS9041: 'OnPropertySetter.Property.set' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnPropertySetter.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnPropertySetter.Property.set", "test").WithLocation(10, 18),
            // (13,22): error CS9041: 'OnPropertyGetter.Property.get' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnPropertyGetter.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnPropertyGetter.Property.get", "test").WithLocation(13, 22),
            // (14,9): error CS9041: 'OnEvent.Event' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEvent.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("OnEvent.Event", "test").WithLocation(14, 9),
            // (15,9): error CS9041: 'OnEvent.Event' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEvent.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("OnEvent.Event", "test").WithLocation(15, 9),
            // (20,1): error CS9041: 'OnEnum' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEnum onEnum;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEnum").WithArguments("OnEnum", "test").WithLocation(20, 1),
            // (21,18): error CS9041: 'OnEnumMember.A' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnEnumMember.A;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "A").WithArguments("OnEnumMember.A", "test").WithLocation(21, 18),
            // (22,1): error CS9041: 'T' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnClassTypeParameter<int> onClassTypeParameter;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnClassTypeParameter<int>").WithArguments("T", "test").WithLocation(22, 1),
            // (23,23): error CS9041: 'T' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethodTypeParameter.M<int>();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "M<int>").WithArguments("T", "test").WithLocation(23, 23),
            // (24,1): error CS9041: 'OnDelegateType' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnDelegateType onDelegateType;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnDelegateType").WithArguments("OnDelegateType", "test").WithLocation(24, 1),
            // (25,28): error CS9041: 'int param' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnIndexedPropertyParameter.set_Property(1, 1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "set_Property").WithArguments("int param", "test").WithLocation(25, 28),
            // (26,32): error CS9041: 'int param' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnIndexedPropertyParameter.get_Property(1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "get_Property").WithArguments("int param", "test").WithLocation(26, 32),
            // (27,1): error CS9041: 'int i' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // new OnThisIndexerParameter()[1] = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "new OnThisIndexerParameter()[1]").WithArguments("int i", "test").WithLocation(27, 1),
            // (28,5): error CS9041: 'int i' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = new OnThisIndexerParameter()[1];
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "new OnThisIndexerParameter()[1]").WithArguments("int i", "test").WithLocation(28, 5)
        );

        var onType = comp.GetTypeByMetadataName("OnType");
        Assert.True(onType!.HasUnsupportedMetadata);
        Assert.True(onType.GetMember<MethodSymbol>("M").HasUnsupportedMetadata);

        var onMethod = comp.GetTypeByMetadataName("OnMethod");
        Assert.False(onMethod!.HasUnsupportedMetadata);
        Assert.True(onMethod.GetMember<MethodSymbol>("M").HasUnsupportedMetadata);

        var onMethodReturn = comp.GetTypeByMetadataName("OnMethodReturn");
        Assert.False(onMethodReturn!.HasUnsupportedMetadata);
        Assert.True(onMethodReturn.GetMember<MethodSymbol>("M").HasUnsupportedMetadata);

        var onParameter = comp.GetTypeByMetadataName("OnParameter");
        Assert.False(onParameter!.HasUnsupportedMetadata);
        var onParameterMethod = onParameter.GetMember<MethodSymbol>("M");
        Assert.True(onParameterMethod.HasUnsupportedMetadata);
        Assert.True(onParameterMethod.Parameters[0].HasUnsupportedMetadata);

        var onField = comp.GetTypeByMetadataName("OnField");
        Assert.False(onField!.HasUnsupportedMetadata);
        Assert.True(onField.GetMember<FieldSymbol>("Field").HasUnsupportedMetadata);

        var onProperty = comp.GetTypeByMetadataName("OnProperty");
        Assert.False(onProperty!.HasUnsupportedMetadata);
        Assert.True(onProperty.GetMember<PropertySymbol>("Property").HasUnsupportedMetadata);

        var onPropertyGetter = comp.GetTypeByMetadataName("OnPropertyGetter");
        Assert.False(onPropertyGetter!.HasUnsupportedMetadata);
        var onPropertyGetterProperty = onPropertyGetter.GetMember<PropertySymbol>("Property");
        Assert.False(onPropertyGetterProperty.HasUnsupportedMetadata);
        Assert.False(onPropertyGetterProperty.SetMethod.HasUnsupportedMetadata);
        Assert.True(onPropertyGetterProperty.GetMethod.HasUnsupportedMetadata);

        var onPropertySetter = comp.GetTypeByMetadataName("OnPropertySetter");
        Assert.False(onPropertySetter!.HasUnsupportedMetadata);
        var onPropertySetterProperty = onPropertySetter.GetMember<PropertySymbol>("Property");
        Assert.False(onPropertySetterProperty.HasUnsupportedMetadata);
        Assert.True(onPropertySetterProperty.SetMethod.HasUnsupportedMetadata);
        Assert.False(onPropertySetterProperty.GetMethod.HasUnsupportedMetadata);

        var onEvent = comp.GetTypeByMetadataName("OnEvent");
        Assert.False(onEvent!.HasUnsupportedMetadata);
        Assert.True(onEvent.GetMember<EventSymbol>("Event").HasUnsupportedMetadata);

        var onEventAdder = comp.GetTypeByMetadataName("OnEventAdder");
        Assert.False(onEventAdder!.HasUnsupportedMetadata);
        var onEventAdderEvent = onEventAdder.GetMember<EventSymbol>("Event");
        Assert.False(onEventAdderEvent.HasUnsupportedMetadata);
        Assert.True(onEventAdderEvent.AddMethod!.HasUnsupportedMetadata);
        Assert.False(onEventAdderEvent.RemoveMethod!.HasUnsupportedMetadata);

        var onEventRemover = comp.GetTypeByMetadataName("OnEventRemover");
        Assert.False(onEventRemover!.HasUnsupportedMetadata);
        var onEventRemoverEvent = onEventRemover.GetMember<EventSymbol>("Event");
        Assert.False(onEventRemoverEvent.HasUnsupportedMetadata);
        Assert.False(onEventRemoverEvent.AddMethod!.HasUnsupportedMetadata);
        Assert.True(onEventRemoverEvent.RemoveMethod!.HasUnsupportedMetadata);

        var onEnum = comp.GetTypeByMetadataName("OnEnum");
        Assert.True(onEnum!.HasUnsupportedMetadata);

        var onEnumMember = comp.GetTypeByMetadataName("OnEnumMember");
        Assert.False(onEnumMember!.HasUnsupportedMetadata);
        Assert.True(onEnumMember.GetMember<FieldSymbol>("A").HasUnsupportedMetadata);

        var onClassTypeParameter = comp.GetTypeByMetadataName("OnClassTypeParameter`1");
        Assert.True(onClassTypeParameter!.HasUnsupportedMetadata);
        Assert.True(onClassTypeParameter.TypeParameters[0].HasUnsupportedMetadata);

        var onMethodTypeParameter = comp.GetTypeByMetadataName("OnMethodTypeParameter");
        Assert.False(onMethodTypeParameter!.HasUnsupportedMetadata);
        var onMethodTypeParameterMethod = onMethodTypeParameter.GetMember<MethodSymbol>("M");
        Assert.True(onMethodTypeParameterMethod.HasUnsupportedMetadata);
        Assert.True(onMethodTypeParameterMethod.TypeParameters[0].HasUnsupportedMetadata);

        var onDelegateType = comp.GetTypeByMetadataName("OnDelegateType");
        Assert.True(onDelegateType!.HasUnsupportedMetadata);

        var onIndexedPropertyParameter = comp.GetTypeByMetadataName("OnIndexedPropertyParameter");
        Assert.False(onIndexedPropertyParameter!.HasUnsupportedMetadata);
        Assert.True(onIndexedPropertyParameter.GetMember<MethodSymbol>("get_Property").Parameters[0].HasUnsupportedMetadata);
        Assert.True(onIndexedPropertyParameter.GetMember<MethodSymbol>("set_Property").Parameters[0].HasUnsupportedMetadata);

        var onThisParameterIndexer = comp.GetTypeByMetadataName("OnThisIndexerParameter");
        Assert.False(onThisParameterIndexer!.HasUnsupportedMetadata);
        var indexer = onThisParameterIndexer.GetMember<PropertySymbol>("this[]");
        Assert.True(indexer.HasUnsupportedMetadata);
        Assert.True(indexer.Parameters[0].HasUnsupportedMetadata);
    }

    protected override void AssertModuleErrors(CSharpCompilation comp, MetadataReference ilRef)
    {
        comp.VerifyDiagnostics(
            // (2,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnType onType;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnType").WithArguments("OnModule", "test").WithLocation(2, 1),
            // (3,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnType.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnType").WithArguments("OnModule", "test").WithLocation(3, 1),
            // (3,8): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnType.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "M").WithArguments("OnModule", "test").WithLocation(3, 8),
            // (4,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethod.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnMethod").WithArguments("OnModule", "test").WithLocation(4, 1),
            // (4,10): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethod.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "M").WithArguments("OnModule", "test").WithLocation(4, 10),
            // (5,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethodReturn.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnMethodReturn").WithArguments("OnModule", "test").WithLocation(5, 1),
            // (5,16): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethodReturn.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "M").WithArguments("OnModule", "test").WithLocation(5, 16),
            // (6,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnParameter.M(1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnParameter").WithArguments("OnModule", "test").WithLocation(6, 1),
            // (6,13): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnParameter.M(1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "M").WithArguments("OnModule", "test").WithLocation(6, 13),
            // (7,5): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnField.Field;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnField").WithArguments("OnModule", "test").WithLocation(7, 5),
            // (7,13): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnField.Field;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Field").WithArguments("OnModule", "test").WithLocation(7, 13),
            // (8,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnProperty.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnProperty").WithArguments("OnModule", "test").WithLocation(8, 1),
            // (8,12): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnProperty.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnModule", "test").WithLocation(8, 12),
            // (9,5): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnProperty.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnProperty").WithArguments("OnModule", "test").WithLocation(9, 5),
            // (9,16): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnProperty.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnModule", "test").WithLocation(9, 16),
            // (10,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnPropertySetter.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnPropertySetter").WithArguments("OnModule", "test").WithLocation(10, 1),
            // (10,18): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnPropertySetter.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnModule", "test").WithLocation(10, 18),
            // (11,5): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnPropertySetter.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnPropertySetter").WithArguments("OnModule", "test").WithLocation(11, 5),
            // (11,22): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnPropertySetter.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnModule", "test").WithLocation(11, 22),
            // (12,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnPropertyGetter.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnPropertyGetter").WithArguments("OnModule", "test").WithLocation(12, 1),
            // (12,18): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnPropertyGetter.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnModule", "test").WithLocation(12, 18),
            // (13,5): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnPropertyGetter.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnPropertyGetter").WithArguments("OnModule", "test").WithLocation(13, 5),
            // (13,22): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnPropertyGetter.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnModule", "test").WithLocation(13, 22),
            // (14,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEvent.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEvent").WithArguments("OnModule", "test").WithLocation(14, 1),
            // (14,9): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEvent.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("OnModule", "test").WithLocation(14, 9),
            // (15,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEvent.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEvent").WithArguments("OnModule", "test").WithLocation(15, 1),
            // (15,9): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEvent.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("OnModule", "test").WithLocation(15, 9),
            // (16,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventAdder.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEventAdder").WithArguments("OnModule", "test").WithLocation(16, 1),
            // (16,14): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventAdder.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("OnModule", "test").WithLocation(16, 14),
            // (17,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventAdder.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEventAdder").WithArguments("OnModule", "test").WithLocation(17, 1),
            // (17,14): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventAdder.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("OnModule", "test").WithLocation(17, 14),
            // (18,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventRemover.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEventRemover").WithArguments("OnModule", "test").WithLocation(18, 1),
            // (18,16): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventRemover.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("OnModule", "test").WithLocation(18, 16),
            // (19,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventRemover.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEventRemover").WithArguments("OnModule", "test").WithLocation(19, 1),
            // (19,16): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventRemover.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("OnModule", "test").WithLocation(19, 16),
            // (20,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEnum onEnum;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEnum").WithArguments("OnModule", "test").WithLocation(20, 1),
            // (21,5): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnEnumMember.A;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEnumMember").WithArguments("OnModule", "test").WithLocation(21, 5),
            // (21,18): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnEnumMember.A;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "A").WithArguments("OnModule", "test").WithLocation(21, 18),
            // (22,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnClassTypeParameter<int> onClassTypeParameter;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnClassTypeParameter<int>").WithArguments("OnModule", "test").WithLocation(22, 1),
            // (23,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethodTypeParameter.M<int>();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnMethodTypeParameter").WithArguments("OnModule", "test").WithLocation(23, 1),
            // (23,23): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethodTypeParameter.M<int>();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "M<int>").WithArguments("OnModule", "test").WithLocation(23, 23),
            // (24,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnDelegateType onDelegateType;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnDelegateType").WithArguments("OnModule", "test").WithLocation(24, 1),
            // (25,1): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnIndexedPropertyParameter.set_Property(1, 1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnIndexedPropertyParameter").WithArguments("OnModule", "test").WithLocation(25, 1),
            // (25,28): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnIndexedPropertyParameter.set_Property(1, 1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "set_Property").WithArguments("OnModule", "test").WithLocation(25, 28),
            // (26,5): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnIndexedPropertyParameter.get_Property(1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnIndexedPropertyParameter").WithArguments("OnModule", "test").WithLocation(26, 5),
            // (26,32): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnIndexedPropertyParameter.get_Property(1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "get_Property").WithArguments("OnModule", "test").WithLocation(26, 32),
            // (27,5): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // new OnThisIndexerParameter()[1] = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnThisIndexerParameter").WithArguments("OnModule", "test").WithLocation(27, 5),
            // (27,5): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // new OnThisIndexerParameter()[1] = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnThisIndexerParameter").WithArguments("OnModule", "test").WithLocation(27, 5),
            // (28,9): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = new OnThisIndexerParameter()[1];
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnThisIndexerParameter").WithArguments("OnModule", "test").WithLocation(28, 9),
            // (28,9): error CS9041: 'OnModule' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = new OnThisIndexerParameter()[1];
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnThisIndexerParameter").WithArguments("OnModule", "test").WithLocation(28, 9)
        );

        Assert.True(comp.GetReferencedAssemblySymbol(ilRef).Modules.Single().HasUnsupportedMetadata);
    }

    protected override void AssertAssemblyErrors(CSharpCompilation comp, MetadataReference ilRef)
    {
        comp.VerifyDiagnostics(
            // (2,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnType onType;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnType").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(2, 1),
            // (3,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnType.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnType").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(3, 1),
            // (3,8): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnType.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "M").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(3, 8),
            // (4,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethod.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnMethod").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(4, 1),
            // (4,10): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethod.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "M").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(4, 10),
            // (5,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethodReturn.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnMethodReturn").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(5, 1),
            // (5,16): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethodReturn.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "M").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(5, 16),
            // (6,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnParameter.M(1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnParameter").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(6, 1),
            // (6,13): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnParameter.M(1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "M").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(6, 13),
            // (7,5): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnField.Field;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnField").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(7, 5),
            // (7,13): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnField.Field;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Field").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(7, 13),
            // (8,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnProperty.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnProperty").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(8, 1),
            // (8,12): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnProperty.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(8, 12),
            // (9,5): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnProperty.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnProperty").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(9, 5),
            // (9,16): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnProperty.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(9, 16),
            // (10,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnPropertySetter.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnPropertySetter").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(10, 1),
            // (10,18): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnPropertySetter.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(10, 18),
            // (11,5): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnPropertySetter.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnPropertySetter").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(11, 5),
            // (11,22): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnPropertySetter.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(11, 22),
            // (12,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnPropertyGetter.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnPropertyGetter").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(12, 1),
            // (12,18): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnPropertyGetter.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(12, 18),
            // (13,5): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnPropertyGetter.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnPropertyGetter").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(13, 5),
            // (13,22): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnPropertyGetter.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(13, 22),
            // (14,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEvent.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEvent").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(14, 1),
            // (14,9): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEvent.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(14, 9),
            // (15,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEvent.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEvent").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(15, 1),
            // (15,9): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEvent.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(15, 9),
            // (16,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventAdder.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEventAdder").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(16, 1),
            // (16,14): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventAdder.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(16, 14),
            // (17,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventAdder.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEventAdder").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(17, 1),
            // (17,14): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventAdder.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(17, 14),
            // (18,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventRemover.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEventRemover").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(18, 1),
            // (18,16): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventRemover.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(18, 16),
            // (19,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventRemover.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEventRemover").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(19, 1),
            // (19,16): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventRemover.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(19, 16),
            // (20,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEnum onEnum;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEnum").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(20, 1),
            // (21,5): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnEnumMember.A;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEnumMember").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(21, 5),
            // (21,18): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnEnumMember.A;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "A").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(21, 18),
            // (22,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnClassTypeParameter<int> onClassTypeParameter;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnClassTypeParameter<int>").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(22, 1),
            // (23,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethodTypeParameter.M<int>();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnMethodTypeParameter").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(23, 1),
            // (23,23): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethodTypeParameter.M<int>();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "M<int>").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(23, 23),
            // (24,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnDelegateType onDelegateType;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnDelegateType").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(24, 1),
            // (25,1): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnIndexedPropertyParameter.set_Property(1, 1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnIndexedPropertyParameter").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(25, 1),
            // (25,28): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnIndexedPropertyParameter.set_Property(1, 1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "set_Property").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(25, 28),
            // (26,5): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnIndexedPropertyParameter.get_Property(1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnIndexedPropertyParameter").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(26, 5),
            // (26,32): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnIndexedPropertyParameter.get_Property(1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "get_Property").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(26, 32),
            // (27,5): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // new OnThisIndexerParameter()[1] = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnThisIndexerParameter").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(27, 5),
            // (27,5): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // new OnThisIndexerParameter()[1] = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnThisIndexerParameter").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(27, 5),
            // (28,9): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = new OnThisIndexerParameter()[1];
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnThisIndexerParameter").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(28, 9),
            // (28,9): error CS9041: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = new OnThisIndexerParameter()[1];
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnThisIndexerParameter").WithArguments("AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "test").WithLocation(28, 9)
        );

        Assert.True(comp.GetReferencedAssemblySymbol(ilRef).HasUnsupportedMetadata);
    }

    [Fact]
    public void Application()
    {
        var comp = CSharpTestBase.CreateCompilation(new[] { """
            using System;
            using System.Runtime.CompilerServices;

            [CompilerFeatureRequired("OnType")]
            public class OnType
            {
            }
            
            public class OnMethod
            {
                [CompilerFeatureRequired("OnMethod")]
                public static void M() {}
            }
            
            public class OnMethodReturn
            {
                [return: CompilerFeatureRequired("OnMethodReturn")]
                public static void M() {}
            }
            
            public class OnParameter
            {
                public static void M([CompilerFeatureRequired("OnParameter")] int param) {}
            }
            
            public class OnField
            {
                [CompilerFeatureRequired("OnField")]
                public static int Field;
            }
            
            public class OnProperty
            {
                [CompilerFeatureRequired("OnProperty")]
                public static int Property { get => 0; set {} }
            }
            
            public class OnPropertySetter
            {
                public static int Property { get => 0; [CompilerFeatureRequired("OnPropertySetter")] set {} }
            }
            
            public class OnPropertyGetter
            {
                public static int Property { [CompilerFeatureRequired("OnPropertyGetter")] get => 0; set {} }
            }
            
            public class OnEvent
            {
                [CompilerFeatureRequired("OnEvent")]
                public static event Action Event { add {} remove {} }
            }
            
            public class OnEventAdder
            {
                public static event Action Event { [CompilerFeatureRequired("OnEventAdder")] add {} remove {} }
            }
            
            public class OnEventRemover
            {
                public static event Action Event { [CompilerFeatureRequired("OnEventRemover")] add {} remove {} }
            }
            
            [CompilerFeatureRequired("OnEnum")]
            public enum OnEnum
            {
                A
            }
            
            public enum OnEnumMember
            {
                [CompilerFeatureRequired("OnEnumMember")] A
            }
            
            public class OnClassTypeParameter<[CompilerFeatureRequired("OnClassTypeParameter")] T>
            {
            }
            
            public class OnMethodTypeParameter
            {
                public static void M<[CompilerFeatureRequired("OnMethodTypeParameter")] T>() {}
            }
            
            [CompilerFeatureRequired("OnDelegateType")]
            public delegate void OnDelegateType();
            """, CSharpTestBase.CompilerFeatureRequiredAttribute });

        comp.VerifyDiagnostics(
            // (4,2): error CS8335: Do not use 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute'. This is reserved for compiler usage.
            // [CompilerFeatureRequired("OnType")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"CompilerFeatureRequired(""OnType"")").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").WithLocation(4, 2),
            // (11,6): error CS8335: Do not use 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute'. This is reserved for compiler usage.
            //     [CompilerFeatureRequired("OnMethod")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"CompilerFeatureRequired(""OnMethod"")").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").WithLocation(11, 6),
            // (17,14): error CS8335: Do not use 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute'. This is reserved for compiler usage.
            //     [return: CompilerFeatureRequired("OnMethodReturn")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"CompilerFeatureRequired(""OnMethodReturn"")").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").WithLocation(17, 14),
            // (23,27): error CS8335: Do not use 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute'. This is reserved for compiler usage.
            //     public static void M([CompilerFeatureRequired("OnParameter")] int param) {}
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"CompilerFeatureRequired(""OnParameter"")").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").WithLocation(23, 27),
            // (28,6): error CS8335: Do not use 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute'. This is reserved for compiler usage.
            //     [CompilerFeatureRequired("OnField")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"CompilerFeatureRequired(""OnField"")").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").WithLocation(28, 6),
            // (34,6): error CS8335: Do not use 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute'. This is reserved for compiler usage.
            //     [CompilerFeatureRequired("OnProperty")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"CompilerFeatureRequired(""OnProperty"")").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").WithLocation(34, 6),
            // (40,45): error CS8335: Do not use 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute'. This is reserved for compiler usage.
            //     public static int Property { get => 0; [CompilerFeatureRequired("OnPropertySetter")] set {} }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"CompilerFeatureRequired(""OnPropertySetter"")").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").WithLocation(40, 45),
            // (45,35): error CS8335: Do not use 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute'. This is reserved for compiler usage.
            //     public static int Property { [CompilerFeatureRequired("OnPropertyGetter")] get => 0; set {} }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"CompilerFeatureRequired(""OnPropertyGetter"")").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").WithLocation(45, 35),
            // (50,6): error CS8335: Do not use 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute'. This is reserved for compiler usage.
            //     [CompilerFeatureRequired("OnEvent")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"CompilerFeatureRequired(""OnEvent"")").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").WithLocation(50, 6),
            // (56,41): error CS8335: Do not use 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute'. This is reserved for compiler usage.
            //     public static event Action Event { [CompilerFeatureRequired("OnEventAdder")] add {} remove {} }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"CompilerFeatureRequired(""OnEventAdder"")").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").WithLocation(56, 41),
            // (61,41): error CS8335: Do not use 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute'. This is reserved for compiler usage.
            //     public static event Action Event { [CompilerFeatureRequired("OnEventRemover")] add {} remove {} }
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"CompilerFeatureRequired(""OnEventRemover"")").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").WithLocation(61, 41),
            // (64,2): error CS8335: Do not use 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute'. This is reserved for compiler usage.
            // [CompilerFeatureRequired("OnEnum")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"CompilerFeatureRequired(""OnEnum"")").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").WithLocation(64, 2),
            // (72,6): error CS8335: Do not use 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute'. This is reserved for compiler usage.
            //     [CompilerFeatureRequired("OnEnumMember")] A
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"CompilerFeatureRequired(""OnEnumMember"")").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").WithLocation(72, 6),
            // (75,36): error CS8335: Do not use 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute'. This is reserved for compiler usage.
            // public class OnClassTypeParameter<[CompilerFeatureRequired("OnClassTypeParameter")] T>
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"CompilerFeatureRequired(""OnClassTypeParameter"")").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").WithLocation(75, 36),
            // (81,27): error CS8335: Do not use 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute'. This is reserved for compiler usage.
            //     public static void M<[CompilerFeatureRequired("OnMethodTypeParameter")] T>() {}
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"CompilerFeatureRequired(""OnMethodTypeParameter"")").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").WithLocation(81, 27),
            // (84,2): error CS8335: Do not use 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute'. This is reserved for compiler usage.
            // [CompilerFeatureRequired("OnDelegateType")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"CompilerFeatureRequired(""OnDelegateType"")").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").WithLocation(84, 2)
        );
    }
}
