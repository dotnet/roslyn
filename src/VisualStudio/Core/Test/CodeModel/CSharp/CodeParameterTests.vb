' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeParameterTests
        Inherits AbstractCodeParameterTests

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute1()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Out"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute2()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Foo"})
        End Sub
#End Region

#Region "DefaultValue tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DefaultValue()
            Dim code =
<Code>
class C
{
    void M(string $$s = "Foo") { }
}
</Code>

            TestDefaultValue(code, """Foo""")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DefaultValue_ExternalCodeParameter_NoDefaultValue()
            Dim code =
<Code>
class C : System.Console
{
    void M(string $$s = "Foo") { }
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

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName1()
            Dim code =
<Code>
class C
{
    void Foo(string $$s)
    {
    }
}
</Code>

            TestName(code, "s")
        End Sub

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName2()
            Dim code =
<Code>
class C
{
    void Foo(ref string $$s)
    {
    }
}
</Code>

            TestName(code, "s")
        End Sub

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName3()
            Dim code =
<Code>
class C
{
    void Foo(out string $$s)
    {
    }
}
</Code>

            TestName(code, "s")
        End Sub

#End Region

#Region "FullName tests"

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FullName()
            Dim code =
<Code>
class C
{
    void Foo(string $$s)
    {
    }
}
</Code>

            TestFullName(code, "s")
        End Sub

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName2()
            Dim code =
<Code>
class C
{
    void Foo(ref string $$s)
    {
    }
}
</Code>

            TestFullName(code, "s")
        End Sub

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName3()
            Dim code =
<Code>
class C
{
    void Foo(out string $$s)
    {
    }
}
</Code>

            TestFullName(code, "s")
        End Sub

#End Region

#Region "Kind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Kind()
            Dim code =
<Code>
class C
{
    void Foo(string $$s)
    {
    }
}
</Code>

            TestKind(code, EnvDTE.vsCMElement.vsCMElementParameter)
        End Sub

#End Region

#Region "ParameterKind tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ParameterKind_None()
            Dim code =
<Code>
class C
{
    void Foo(string $$s)
    {
    }
}
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindNone)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ParameterKind_Ref()
            Dim code =
<Code>
class C
{
    void Foo(ref string $$s)
    {
    }
}
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Sub


        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ParameterKind_Out()
            Dim code =
<Code>
class C
{
    void Foo(out string $$s)
    {
    }
}
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindOut)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ParameterKind_ParamArray()
            Dim code =
<Code>
class C
{
    void Foo(params string[] $$s)
    {
    }
}
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ParameterKind_Optional()
            Dim code =
<Code>
class C
{
    void Foo(string $$s = "Foo")
    {
    }
}
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ParameterKind_OptionalAndRef()
            Dim code =
<Code>
class C
{
    void Foo(ref string $$s = "Foo")
    {
    }
}
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional Or EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Sub
#End Region

#Region "Parent tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parent1()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parent2()
            Dim code =
<Code>
delegate void Foo(int $$i);
</Code>

            TestParent(code, IsElement("Foo", kind:=EnvDTE.vsCMElement.vsCMElementDelegate))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parent3()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type1()
            Dim code =
<Code>
class C
{
    public void Foo(int i$$ = 0) { }
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetParameterKind_In()
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

            TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindIn)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetParameterKind_None()
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

            TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindNone)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetParameterKind_Out()
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

            TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindOut)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetParameterKind_Ref()
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
            TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetParameterKind_Params()
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
            TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetParameterKind_ParamsInvalid()
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
            TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray, ThrowsArgumentException(Of EnvDTE80.vsCMParameterKind)())
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetParameterKind_Optional()
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

            TestSetParameterKind(code,
                                 expected,
                                 EnvDTE80.vsCMParameterKind.vsCMParameterKindRef Or EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetParameterKind_Same()
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
            TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindOut)
        End Sub

#End Region

#Region "Set DefaultValue tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDefaultValue()
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
            TestSetDefaultValue(code, expected, """Foo""")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDefaultValue_ReplaceExisting()
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
            TestSetDefaultValue(code, expected, """Foo""")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDefaultValue_None()
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
            TestSetDefaultValue(code, expected, "")
        End Sub

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType1()
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

            TestSetTypeProp(code, expected, "byte?[,]")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType2()
            Dim code =
<Code>
delegate void Foo(int $$i) { }
</Code>

            Dim expected =
<Code>
delegate void Foo(byte?[,] i) { }
</Code>

            TestSetTypeProp(code, expected, "byte?[,]")
        End Sub

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TypeDescriptor_GetProperties()
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

            TestPropertyDescriptors(code, expectedPropertyNames)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace

