' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeParameterTests
        Inherits AbstractCodeParameterTests

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
            Dim code =
<Code>
class C
{
    void Foo(string $$s)
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Foo([Out()] string s)
    {
    }
}
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Out"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
            Dim code =
<Code>
class C
{
    void Foo([Out()]string $$s)
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Foo([Foo()][Out()]string s)
    {
    }
}
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Foo"})
        End Function
#End Region

#Region "DefaultValue tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDefaultValue1() As Task
            Dim code =
<Code>
class C
{
    void M(string $$s = "Foo") { }
}
</Code>

            Await TestDefaultValue(code, """Foo""")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDefaultValue_ExternalCodeParameter_NoDefaultValue() As Task
            Dim code =
<Code>
class C : System.Console
{
    void M(string $$s = "Foo") { }
}
</Code>
            Await TestElement(code,
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

        End Function

#End Region

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName1() As Task
            Dim code =
<Code>
class C
{
    void Foo(string $$s)
    {
    }
}
</Code>

            Await TestFullName(code, "s")
        End Function

#End Region

#Region "Kind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestKind1() As Task
            Dim code =
<Code>
class C
{
    void Foo(string $$s)
    {
    }
}
</Code>

            Await TestKind(code, EnvDTE.vsCMElement.vsCMElementParameter)
        End Function

#End Region

#Region "ParameterKind tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterKind_None() As Task
            Dim code =
<Code>
class C
{
    void Foo(string $$s)
    {
    }
}
</Code>

            Await TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterKind_Ref() As Task
            Dim code =
<Code>
class C
{
    void Foo(ref string $$s)
    {
    }
}
</Code>

            Await TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Function


        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterKind_Out() As Task
            Dim code =
<Code>
class C
{
    void Foo(out string $$s)
    {
    }
}
</Code>

            Await TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindOut)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterKind_ParamArray() As Task
            Dim code =
<Code>
class C
{
    void Foo(params string[] $$s)
    {
    }
}
</Code>

            Await TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterKind_Optional() As Task
            Dim code =
<Code>
class C
{
    void Foo(string $$s = "Foo")
    {
    }
}
</Code>

            Await TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterKind_OptionalAndRef() As Task
            Dim code =
<Code>
class C
{
    void Foo(ref string $$s = "Foo")
    {
    }
}
</Code>

            Await TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional Or EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Function
#End Region

#Region "Parent tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParent1() As Task
            Dim code =
<Code>
class C
{
    void M(string $$s)
    {
    }
}
</Code>

            Await TestParent(code, IsElement("M", kind:=EnvDTE.vsCMElement.vsCMElementFunction))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParent2() As Task
            Dim code =
<Code>
delegate void Foo(int $$i);
</Code>

            Await TestParent(code, IsElement("Foo", kind:=EnvDTE.vsCMElement.vsCMElementDelegate))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParent3() As Task
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

            Await TestParent(code, IsElement("this", kind:=EnvDTE.vsCMElement.vsCMElementProperty))
        End Function

#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType1() As Task
            Dim code =
<Code>
class C
{
    public void Foo(int i$$ = 0) { }
}
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "int",
                             .AsFullName = "System.Int32",
                             .CodeTypeFullName = "System.Int32",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                         })
        End Function

#End Region

#Region "Set ParameterKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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
    void M(string s = "Foo") { }
}
</Code>
            Await TestSetDefaultValue(code, expected, """Foo""")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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
    void M(string s = "Foo") { }
}
</Code>
            Await TestSetDefaultValue(code, expected, """Foo""")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
            Dim code =
<Code>
class C
{
    public void Foo(int $$i) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    public void Foo(byte?[,] i) { }
}
</Code>

            Await TestSetTypeProp(code, expected, "byte?[,]")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType2() As Task
            Dim code =
<Code>
delegate void Foo(int $$i) { }
</Code>

            Dim expected =
<Code>
delegate void Foo(byte?[,] i) { }
</Code>

            Await TestSetTypeProp(code, expected, "byte?[,]")
        End Function

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestTypeDescriptor_GetProperties() As Task
            Dim code =
<Code>
class C
{
    void M(int $$p) { }
}
</Code>

            Dim expectedPropertyNames =
                {"DTE", "Collection", "Name", "FullName", "ProjectItem", "Kind", "IsCodeType",
                 "InfoLocation", "Children", "Language", "StartPoint", "EndPoint", "ExtenderNames",
                 "ExtenderCATID", "Parent", "Type", "Attributes", "DocComment", "ParameterKind", "DefaultValue"}

            Await TestPropertyDescriptors(code, expectedPropertyNames)
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace

