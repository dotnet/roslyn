// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeGen;
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

    internal override string VisualizeRealIL(IModuleSymbol peModule, CompilationTestData.MethodData methodData, IReadOnlyDictionary<int, string> markers, bool areLocalsZeroed)
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
            // (2,1): error CS9512: 'OnType' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnType onType;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnType").WithArguments("OnType", "test").WithLocation(2, 1),
            // (3,1): error CS9512: 'OnType' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnType.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnType").WithArguments("OnType", "test").WithLocation(3, 1),
            // (4,1): error CS9512: 'OnMethod.M()' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethod.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnMethod.M").WithArguments("OnMethod.M()", "test").WithLocation(4, 1),
            // (6,1): error CS9512: 'int' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnParameter.M(1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnParameter.M").WithArguments("int", "test").WithLocation(6, 1),
            // (7,13): error CS9512: 'OnField.Field' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnField.Field;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Field").WithArguments("OnField.Field", "test").WithLocation(7, 13),
            // (8,12): error CS9512: 'OnProperty.Property' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnProperty.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnProperty.Property", "test").WithLocation(8, 12),
            // (9,16): error CS9512: 'OnProperty.Property' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnProperty.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnProperty.Property", "test").WithLocation(9, 16),
            // (10,18): error CS9512: 'OnPropertySetter.Property.set' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnPropertySetter.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnPropertySetter.Property.set", "test").WithLocation(10, 18),
            // (13,22): error CS9512: 'OnPropertyGetter.Property.get' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnPropertyGetter.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnPropertyGetter.Property.get", "test").WithLocation(13, 22),
            // (14,9): error CS9512: 'OnEvent.Event' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEvent.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("OnEvent.Event", "test").WithLocation(14, 9),
            // (15,9): error CS9512: 'OnEvent.Event' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEvent.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("OnEvent.Event", "test").WithLocation(15, 9),
            // (20,1): error CS9512: 'OnEnum' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEnum onEnum;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEnum").WithArguments("OnEnum", "test").WithLocation(20, 1),
            // (21,18): error CS9512: 'OnEnumMember.A' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnEnumMember.A;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "A").WithArguments("OnEnumMember.A", "test").WithLocation(21, 18),
            // (22,1): error CS9512: 'T' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnClassTypeParameter<int> onClassTypeParameter;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnClassTypeParameter<int>").WithArguments("T", "test").WithLocation(22, 1),
            // (23,1): error CS9512: 'T' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethodTypeParameter.M<int>();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnMethodTypeParameter.M<int>").WithArguments("T", "test").WithLocation(23, 1),
            // (24,1): error CS9512: 'OnDelegateType' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnDelegateType onDelegateType;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnDelegateType").WithArguments("OnDelegateType", "test").WithLocation(24, 1),
            // (25,1): error CS9512: 'int' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnIndexedPropertyParameter.set_Property(1, 1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnIndexedPropertyParameter.set_Property").WithArguments("int", "test").WithLocation(25, 1),
            // (26,5): error CS9512: 'int' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnIndexedPropertyParameter.get_Property(1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnIndexedPropertyParameter.get_Property").WithArguments("int", "test").WithLocation(26, 5),
            // (27,29): error CS9512: 'int' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // new OnThisIndexerParameter()[1] = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "[1]").WithArguments("int", "test").WithLocation(27, 29),
            // (28,33): error CS9512: 'int' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = new OnThisIndexerParameter()[1];
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "[1]").WithArguments("int", "test").WithLocation(28, 33)
        );
    }

    protected override void AssertModuleAndAssemblyErrors(CSharpCompilation comp)
    {
        comp.VerifyDiagnostics(
            // (2,1): error CS9512: 'OnType' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnType onType;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnType").WithArguments("OnType", "test").WithLocation(2, 1),
            // (3,1): error CS9512: 'OnType' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnType.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnType").WithArguments("OnType", "test").WithLocation(3, 1),
            // (3,1): error CS9512: 'OnType.M()' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnType.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnType.M").WithArguments("OnType.M()", "test").WithLocation(3, 1),
            // (4,1): error CS9512: 'OnMethod' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethod.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnMethod").WithArguments("OnMethod", "test").WithLocation(4, 1),
            // (4,1): error CS9512: 'OnMethod.M()' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethod.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnMethod.M").WithArguments("OnMethod.M()", "test").WithLocation(4, 1),
            // (5,1): error CS9512: 'OnMethodReturn' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethodReturn.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnMethodReturn").WithArguments("OnMethodReturn", "test").WithLocation(5, 1),
            // (5,1): error CS9512: 'OnMethodReturn.M()' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethodReturn.M();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnMethodReturn.M").WithArguments("OnMethodReturn.M()", "test").WithLocation(5, 1),
            // (6,1): error CS9512: 'OnParameter' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnParameter.M(1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnParameter").WithArguments("OnParameter", "test").WithLocation(6, 1),
            // (6,1): error CS9512: 'int' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnParameter.M(1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnParameter.M").WithArguments("int", "test").WithLocation(6, 1),
            // (7,5): error CS9512: 'OnField' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnField.Field;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnField").WithArguments("OnField", "test").WithLocation(7, 5),
            // (7,13): error CS9512: 'OnField.Field' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnField.Field;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Field").WithArguments("OnField.Field", "test").WithLocation(7, 13),
            // (8,1): error CS9512: 'OnProperty' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnProperty.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnProperty").WithArguments("OnProperty", "test").WithLocation(8, 1),
            // (8,12): error CS9512: 'OnProperty.Property' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnProperty.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnProperty.Property", "test").WithLocation(8, 12),
            // (9,5): error CS9512: 'OnProperty' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnProperty.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnProperty").WithArguments("OnProperty", "test").WithLocation(9, 5),
            // (9,16): error CS9512: 'OnProperty.Property' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnProperty.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnProperty.Property", "test").WithLocation(9, 16),
            // (10,1): error CS9512: 'OnPropertySetter' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnPropertySetter.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnPropertySetter").WithArguments("OnPropertySetter", "test").WithLocation(10, 1),
            // (10,18): error CS9512: 'OnPropertySetter.Property' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnPropertySetter.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnPropertySetter.Property", "test").WithLocation(10, 18),
            // (11,5): error CS9512: 'OnPropertySetter' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnPropertySetter.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnPropertySetter").WithArguments("OnPropertySetter", "test").WithLocation(11, 5),
            // (11,22): error CS9512: 'OnPropertySetter.Property' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnPropertySetter.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnPropertySetter.Property", "test").WithLocation(11, 22),
            // (12,1): error CS9512: 'OnPropertyGetter' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnPropertyGetter.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnPropertyGetter").WithArguments("OnPropertyGetter", "test").WithLocation(12, 1),
            // (12,18): error CS9512: 'OnPropertyGetter.Property' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnPropertyGetter.Property = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnPropertyGetter.Property", "test").WithLocation(12, 18),
            // (13,5): error CS9512: 'OnPropertyGetter' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnPropertyGetter.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnPropertyGetter").WithArguments("OnPropertyGetter", "test").WithLocation(13, 5),
            // (13,22): error CS9512: 'OnPropertyGetter.Property' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnPropertyGetter.Property;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Property").WithArguments("OnPropertyGetter.Property", "test").WithLocation(13, 22),
            // (14,1): error CS9512: 'OnEvent' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEvent.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEvent").WithArguments("OnEvent", "test").WithLocation(14, 1),
            // (14,9): error CS9512: 'OnEvent.Event' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEvent.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("OnEvent.Event", "test").WithLocation(14, 9),
            // (15,1): error CS9512: 'OnEvent' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEvent.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEvent").WithArguments("OnEvent", "test").WithLocation(15, 1),
            // (15,9): error CS9512: 'OnEvent.Event' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEvent.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("OnEvent.Event", "test").WithLocation(15, 9),
            // (16,1): error CS9512: 'OnEventAdder' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventAdder.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEventAdder").WithArguments("OnEventAdder", "test").WithLocation(16, 1),
            // (16,14): error CS9512: 'OnEventAdder.Event' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventAdder.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("OnEventAdder.Event", "test").WithLocation(16, 14),
            // (17,1): error CS9512: 'OnEventAdder' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventAdder.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEventAdder").WithArguments("OnEventAdder", "test").WithLocation(17, 1),
            // (17,14): error CS9512: 'OnEventAdder.Event' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventAdder.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("OnEventAdder.Event", "test").WithLocation(17, 14),
            // (18,1): error CS9512: 'OnEventRemover' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventRemover.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEventRemover").WithArguments("OnEventRemover", "test").WithLocation(18, 1),
            // (18,16): error CS9512: 'OnEventRemover.Event' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventRemover.Event += () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("OnEventRemover.Event", "test").WithLocation(18, 16),
            // (19,1): error CS9512: 'OnEventRemover' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventRemover.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEventRemover").WithArguments("OnEventRemover", "test").WithLocation(19, 1),
            // (19,16): error CS9512: 'OnEventRemover.Event' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEventRemover.Event -= () => {};
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Event").WithArguments("OnEventRemover.Event", "test").WithLocation(19, 16),
            // (20,1): error CS9512: 'OnEnum' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnEnum onEnum;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEnum").WithArguments("OnEnum", "test").WithLocation(20, 1),
            // (21,5): error CS9512: 'OnEnumMember' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnEnumMember.A;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnEnumMember").WithArguments("OnEnumMember", "test").WithLocation(21, 5),
            // (21,18): error CS9512: 'OnEnumMember.A' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnEnumMember.A;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "A").WithArguments("OnEnumMember.A", "test").WithLocation(21, 18),
            // (22,1): error CS9512: 'T' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnClassTypeParameter<int> onClassTypeParameter;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnClassTypeParameter<int>").WithArguments("T", "test").WithLocation(22, 1),
            // (23,1): error CS9512: 'OnMethodTypeParameter' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethodTypeParameter.M<int>();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnMethodTypeParameter").WithArguments("OnMethodTypeParameter", "test").WithLocation(23, 1),
            // (23,1): error CS9512: 'T' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnMethodTypeParameter.M<int>();
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnMethodTypeParameter.M<int>").WithArguments("T", "test").WithLocation(23, 1),
            // (24,1): error CS9512: 'OnDelegateType' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnDelegateType onDelegateType;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnDelegateType").WithArguments("OnDelegateType", "test").WithLocation(24, 1),
            // (25,1): error CS9512: 'OnIndexedPropertyParameter' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnIndexedPropertyParameter.set_Property(1, 1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnIndexedPropertyParameter").WithArguments("OnIndexedPropertyParameter", "test").WithLocation(25, 1),
            // (25,1): error CS9512: 'int' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // OnIndexedPropertyParameter.set_Property(1, 1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnIndexedPropertyParameter.set_Property").WithArguments("int", "test").WithLocation(25, 1),
            // (26,5): error CS9512: 'OnIndexedPropertyParameter' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnIndexedPropertyParameter.get_Property(1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnIndexedPropertyParameter").WithArguments("OnIndexedPropertyParameter", "test").WithLocation(26, 5),
            // (26,5): error CS9512: 'int' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = OnIndexedPropertyParameter.get_Property(1);
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnIndexedPropertyParameter.get_Property").WithArguments("int", "test").WithLocation(26, 5),
            // (27,5): error CS9512: 'OnThisIndexerParameter' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // new OnThisIndexerParameter()[1] = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnThisIndexerParameter").WithArguments("OnThisIndexerParameter", "test").WithLocation(27, 5),
            // (27,5): error CS9512: 'OnThisIndexerParameter.OnThisIndexerParameter()' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // new OnThisIndexerParameter()[1] = 1;
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnThisIndexerParameter").WithArguments("OnThisIndexerParameter.OnThisIndexerParameter()", "test").WithLocation(27, 5),
            // (28,9): error CS9512: 'OnThisIndexerParameter' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = new OnThisIndexerParameter()[1];
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnThisIndexerParameter").WithArguments("OnThisIndexerParameter", "test").WithLocation(28, 9),
            // (28,9): error CS9512: 'OnThisIndexerParameter.OnThisIndexerParameter()' requires compiler feature 'test', which is not supported by this version of the C# compiler.
            // _ = new OnThisIndexerParameter()[1];
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "OnThisIndexerParameter").WithArguments("OnThisIndexerParameter.OnThisIndexerParameter()", "test").WithLocation(28, 9)
        );
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
