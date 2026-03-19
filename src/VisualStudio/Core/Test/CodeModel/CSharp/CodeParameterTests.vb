' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeParameterTests
        Inherits AbstractCodeParameterTests

#Region "AddAttribute tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
            Dim code =
<Code>
class C
{
    void Goo(string $$s)
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Goo([Out()] string s)
    {
    }
}
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Out"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
            Dim code =
<Code>
class C
{
    void Goo([Out()]string $$s)
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Goo([Goo()][Out()]string s)
    {
    }
}
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Goo"})
        End Function
#End Region

#Region "DefaultValue tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDefaultValue1()
            Dim code =
<Code>
class C
{
    void M(string $$s = "Goo") { }
}
</Code>

            TestDefaultValue(code, """Goo""")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDefaultValue_ExternalCodeParameter_NoDefaultValue()
            Dim code =
<Code>
class C : System.Console
{
    void M(string $$s = "Goo") { }
}
</Code>
            TestElement(code,
                        Sub(codeParameter)
                            Dim method = TryCast(codeParameter.Parent, EnvDTE80.CodeFunction2)
                            Assert.NotNull(method)

                            Dim containingType = TryCast(method.Parent, EnvDTE80.CodeClass2)
                            Assert.NotNull(containingType)

                            Dim baseType = TryCast(containingType.Bases.Item(1), EnvDTE80.CodeClass2)
                            Assert.NotNull(baseType)

                            Dim [overloads] = CType(baseType.Members.Item("WriteLine"), EnvDTE80.CodeFunction2).Overloads
                            Dim method2 = CType([overloads].Item(2), EnvDTE80.CodeFunction2)
                            Dim defaultValue = CType(method2.Parameters.Item(1), EnvDTE80.CodeParameter2).DefaultValue
                        End Sub)

        End Sub

#End Region

#Region "Name tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName1()
            Dim code =
<Code>
class C
{
    void Goo(string $$s)
    {
    }
}
</Code>

            TestName(code, "s")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName2()
            Dim code =
<Code>
class C
{
    void Goo(ref string $$s)
    {
    }
}
</Code>

            TestName(code, "s")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName3()
            Dim code =
<Code>
class C
{
    void Goo(out string $$s)
    {
    }
}
</Code>

            TestName(code, "s")
        End Sub

#End Region

#Region "FullName tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName1()
            Dim code =
<Code>
class C
{
    void Goo(string $$s)
    {
    }
}
</Code>

            TestFullName(code, "s")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName2()
            Dim code =
<Code>
class C
{
    void Goo(ref string $$s)
    {
    }
}
</Code>

            TestFullName(code, "s")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName3()
            Dim code =
<Code>
class C
{
    void Goo(out string $$s)
    {
    }
}
</Code>

            TestFullName(code, "s")
        End Sub

#End Region

#Region "Kind tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestKind1()
            Dim code =
<Code>
class C
{
    void Goo(string $$s)
    {
    }
}
</Code>

            TestKind(code, EnvDTE.vsCMElement.vsCMElementParameter)
        End Sub

#End Region

#Region "ParameterKind tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterKind_None()
            Dim code =
<Code>
class C
{
    void Goo(string $$s)
    {
    }
}
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindNone)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterKind_Ref()
            Dim code =
<Code>
class C
{
    void Goo(ref string $$s)
    {
    }
}
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterKind_Out()
            Dim code =
<Code>
class C
{
    void Goo(out string $$s)
    {
    }
}
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindOut)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterKind_ParamArray()
            Dim code =
<Code>
class C
{
    void Goo(params string[] $$s)
    {
    }
}
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterKind_Optional()
            Dim code =
<Code>
class C
{
    void Goo(string $$s = "Goo")
    {
    }
}
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterKind_OptionalAndRef()
            Dim code =
<Code>
class C
{
    void Goo(ref string $$s = "Goo")
    {
    }
}
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional Or EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Sub
#End Region

#Region "Parent tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParent1()
            Dim code =
<Code>
class C
{
    void M(string $$s)
    {
    }
}
</Code>

            TestParent(code, IsElement("M", kind:=EnvDTE.vsCMElement.vsCMElementFunction))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParent2()
            Dim code =
<Code>
delegate void Goo(int $$i);
</Code>

            TestParent(code, IsElement("Goo", kind:=EnvDTE.vsCMElement.vsCMElementDelegate))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParent3()
            Dim code =
<Code>
class C
{
    int this[int $$i]
    {
        get { return 0; }
    }
}
</Code>

            TestParent(code, IsElement("this", kind:=EnvDTE.vsCMElement.vsCMElementProperty))
        End Sub

#End Region

#Region "Type tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType1()
            Dim code =
<Code>
class C
{
    public void Goo(int i$$ = 0) { }
}
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "int",
                             .AsFullName = "System.Int32",
                             .CodeTypeFullName = "System.Int32",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                         })
        End Sub

#End Region

#Region "Set ParameterKind tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetParameterKind_In() As Task
            Dim code =
<Code>
class C
{
    void M(out string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(string s) { }
}
</Code>

            Await TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindIn)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetParameterKind_None() As Task
            Dim code =
<Code>
class C
{
    void M(out string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(string s) { }
}
</Code>

            Await TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindNone)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetParameterKind_Out() As Task
            Dim code =
<Code>
class C
{
    void M(ref string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(out string s) { }
}
</Code>

            Await TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindOut)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetParameterKind_Ref() As Task
            Dim code =
<Code>
class C
{
    void M(string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(ref string s) { }
}
</Code>
            Await TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetParameterKind_Params() As Task
            Dim code =
<Code>
class C
{
    void M(string[] $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(params string[] s) { }
}
</Code>
            Await TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetParameterKind_ParamsInvalid() As Task
            Dim code =
<Code>
class C
{
    void M(string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(string s) { }
}
</Code>
            Await TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray, ThrowsArgumentException(Of EnvDTE80.vsCMParameterKind)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetParameterKind_Optional() As Task
            Dim code =
<Code>
class C
{
    void M(string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(ref string s) { }
}
</Code>

            Await TestSetParameterKind(code,
                                 expected,
                                 EnvDTE80.vsCMParameterKind.vsCMParameterKindRef Or EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetParameterKind_Same() As Task
            Dim code =
<Code>
class C
{
    void M(out string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(out string s) { }
}
</Code>
            Await TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindOut)
        End Function

#End Region

#Region "Set DefaultValue tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDefaultValue1() As Task
            Dim code =
<Code>
class C
{
    void M(string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(string s = "Goo") { }
}
</Code>
            Await TestSetDefaultValue(code, expected, """Goo""")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDefaultValue_ReplaceExisting() As Task
            Dim code =
<Code>
class C
{
    void M(string $$s = "Bar") { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(string s = "Goo") { }
}
</Code>
            Await TestSetDefaultValue(code, expected, """Goo""")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDefaultValue_None() As Task
            Dim code =
<Code>
class C
{
    void M(string $$s = "Bar") { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(string s) { }
}
</Code>
            Await TestSetDefaultValue(code, expected, "")
        End Function

#End Region

#Region "Set Type tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
            Dim code =
<Code>
class C
{
    public void Goo(int $$i) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    public void Goo(byte?[,] i) { }
}
</Code>

            Await TestSetTypeProp(code, expected, "byte?[,]")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType2() As Task
            Dim code =
<Code>
delegate void Goo(int $$i) { }
</Code>

            Dim expected =
<Code>
delegate void Goo(byte?[,] i) { }
</Code>

            Await TestSetTypeProp(code, expected, "byte?[,]")
        End Function

#End Region

#Region "IParameterKind.GetParameterPassingMode tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterPassingMode_NoModifier()
            Dim code =
<Code>
class C
{
    void Goo(string $$s)
    {
    }
}
</Code>

            TestGetParameterPassingMode(code, PARAMETER_PASSING_MODE.cmParameterTypeIn)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterPassingMode_RefModifier()
            Dim code =
<Code>
class C
{
    void Goo(ref string $$s)
    {
    }
}
</Code>

            TestGetParameterPassingMode(code, PARAMETER_PASSING_MODE.cmParameterTypeInOut)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterPassingMode_OutModifier()
            Dim code =
<Code>
class C
{
    void Goo(out string $$s)
    {
    }
}
</Code>

            TestGetParameterPassingMode(code, PARAMETER_PASSING_MODE.cmParameterTypeOut)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterPassingMode_ParamsModifier()
            Dim code =
<Code>
class C
{
    void Goo(params string[] $$s)
    {
    }
}
</Code>

            TestGetParameterPassingMode(code, PARAMETER_PASSING_MODE.cmParameterTypeIn)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterPassingMode_DefaultValue()
            Dim code =
<Code>
class C
{
    void Goo(string $$s = "Goo")
    {
    }
}
</Code>

            TestGetParameterPassingMode(code, PARAMETER_PASSING_MODE.cmParameterTypeIn)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterPassingMode_DefaultValueAndRefModifiers()
            Dim code =
<Code>
class C
{
    void Goo(ref string $$s = "Goo")
    {
    }
}
</Code>

            TestGetParameterPassingMode(code, PARAMETER_PASSING_MODE.cmParameterTypeInOut)
        End Sub

#End Region

#Region "IParmeterKind.SetParameterPassingMode tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_NoModifier_In() As Task
            Dim code =
<Code>
class C
{
    void M(string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(string s) { }
}
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeIn)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_NoModifier_InOut() As Task
            Dim code =
<Code>
class C
{
    void M(string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(ref string s) { }
}
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeInOut)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_NoModifier_Out() As Task
            Dim code =
<Code>
class C
{
    void M(string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(out string s) { }
}
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeOut)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_RefModifier_In() As Task
            Dim code =
<Code>
class C
{
    void M(ref string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(string s) { }
}
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeIn)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_RefModifier_InOut() As Task
            Dim code =
<Code>
class C
{
    void M(ref string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(ref string s) { }
}
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeInOut)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_RefModifier_Out() As Task
            Dim code =
<Code>
class C
{
    void M(ref string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(out string s) { }
}
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeOut)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_OutModifier_In() As Task
            Dim code =
<Code>
class C
{
    void M(out string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(string s) { }
}
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeIn)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_OutModifier_InOut() As Task
            Dim code =
<Code>
class C
{
    void M(out string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(ref string s) { }
}
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeInOut)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_OutModifier_Out() As Task
            Dim code =
<Code>
class C
{
    void M(out string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(out string s) { }
}
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeOut)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_ParamsModifier_In() As Task
            Dim code =
<Code>
class C
{
    void M(params string[] $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(params string[] s) { }
}
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeIn)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_DefaultValue_Ref() As Task
            Dim code =
<Code>
class C
{
    void M(string $$s = "hello!") { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(ref string s = "hello!") { }
}
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeInOut)
        End Function

#End Region

#Region "IParameterKind.GetParameterArrayCount tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayCount_0()
            Dim code =
<Code>
class C
{
    void M(string $$s) { }
}
</Code>

            TestGetParameterArrayCount(code, 0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayCount_1()
            Dim code =
<Code>
class C
{
    void M(string[] $$s) { }
}
</Code>

            TestGetParameterArrayCount(code, 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayCount_2()
            Dim code =
<Code>
class C
{
    void M(string[][] $$s) { }
}
</Code>

            TestGetParameterArrayCount(code, 2)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayCount_1_Multi()
            Dim code =
<Code>
class C
{
    void M(string[,,] $$s) { }
}
</Code>

            TestGetParameterArrayCount(code, 1)
        End Sub

#End Region

#Region "IParameterKind.GetParameterArrayDimensions tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayDimensions_0_1()
            Dim code =
<Code>
class C
{
    void M(string[] $$s) { }
}
</Code>

            TestGetParameterArrayDimensions(code, index:=0, expected:=1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayDimensions_0_2()
            Dim code =
<Code>
class C
{
    void M(string[,] $$s) { }
}
</Code>

            TestGetParameterArrayDimensions(code, index:=0, expected:=2)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayDimensions_0_3()
            Dim code =
<Code>
class C
{
    void M(string[,,] $$s) { }
}
</Code>

            TestGetParameterArrayDimensions(code, index:=0, expected:=3)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayDimensions_1_1()
            Dim code =
<Code>
class C
{
    void M(string[,,][] $$s) { }
}
</Code>

            TestGetParameterArrayDimensions(code, index:=1, expected:=1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayDimensions_1_2()
            Dim code =
<Code>
class C
{
    void M(string[,,][,] $$s) { }
}
</Code>

            TestGetParameterArrayDimensions(code, index:=1, expected:=2)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayDimensions_2_1()
            Dim code =
<Code>
class C
{
    void M(string[,,][,][] $$s) { }
}
</Code>

            TestGetParameterArrayDimensions(code, index:=2, expected:=1)
        End Sub

#End Region

#Region "IParmeterKind.SetParameterArrayDimensions tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterArrayDimensions_None_0() As Task
            ' The C# implementation had a weird behavior where it wold allow setting array dimensions
            ' to 0 to create an array with a single rank.

            Dim code =
<Code>
class C
{
    void M(string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(string[] s) { }
}
</Code>

            Await TestSetParameterArrayDimensions(code, expected, dimensions:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterArrayDimensions_None_1() As Task
            Dim code =
<Code>
class C
{
    void M(string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(string[] s) { }
}
</Code>

            Await TestSetParameterArrayDimensions(code, expected, dimensions:=1)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterArrayDimensions_None_2() As Task
            Dim code =
<Code>
class C
{
    void M(string $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(string[,] s) { }
}
</Code>

            Await TestSetParameterArrayDimensions(code, expected, dimensions:=2)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterArrayDimensions_1_2() As Task
            Dim code =
<Code>
class C
{
    void M(string[] $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(string[,] s) { }
}
</Code>

            Await TestSetParameterArrayDimensions(code, expected, dimensions:=2)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterArrayDimensions_1_2_WithInnerArray() As Task
            Dim code =
<Code>
class C
{
    void M(string[][] $$s) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(string[,][] s) { }
}
</Code>

            Await TestSetParameterArrayDimensions(code, expected, dimensions:=2)
        End Function

#End Region

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestTypeDescriptor_GetProperties()
            Dim code =
<Code>
class C
{
    void M(int $$p) { }
}
</Code>

            TestPropertyDescriptors(Of EnvDTE80.CodeParameter2)(code)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace

