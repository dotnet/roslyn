' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeParameterTests
        Inherits AbstractCodeParameterTests

#Region "AddAttribute tests"
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute1()
            Dim code =
<Code><![CDATA[
Class C
    Sub Foo($$s As String)
    End Sub
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C
    Sub Foo(<Out()> s As String)
    End Sub
End Class
]]></Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Out"})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute2()
            Dim code =
<Code><![CDATA[
Class C
    Sub Foo(<Out()> $$s As String)
    End Sub
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C
    Sub Foo(<Foo()> <Out()> s As String)
    End Sub
End Class
]]></Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Foo"})
        End Sub
#End Region

#Region "FullName tests"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FullName()
            Dim code =
<Code>
Class C
    Sub Foo($$s As String)
    End Sub
End Class
</Code>

            TestFullName(code, "s")
        End Sub

#End Region

#Region "Kind tests"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Kind()
            Dim code =
<Code>
Class C
    Sub Foo($$s As String)
    End Sub
End Class
</Code>

            TestKind(code, EnvDTE.vsCMElement.vsCMElementParameter)
        End Sub

#End Region

#Region "ParameterKind tests"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ParameterKind_In()
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindIn)
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ParameterKind_Ref()
            Dim code =
<Code>
Class C
    Sub M(ByRef $$s As String)
    End Sub
End Class
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Sub


        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ParameterKind_ParamArray()
            Dim code =
<Code>
Class C
    Sub M(ParamArray $$s() As String)
    End Sub
End Class
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray Or EnvDTE80.vsCMParameterKind.vsCMParameterKindIn)
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ParameterKind_Optional()
            Dim code =
<Code>
Class C
    Sub M(Optional $$s As String = "Foo")
    End Sub
End Class
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional Or EnvDTE80.vsCMParameterKind.vsCMParameterKindIn)
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ParameterKind_OptionalAndRef()
            Dim code =
<Code>
Class C
    Sub M(Optional ByRef $$s As String = "Foo")
    End Sub
End Class
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional Or EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Sub

#End Region

#Region "Parent tests"
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parent()
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            TestParent(code, IsElement("M", kind:=EnvDTE.vsCMElement.vsCMElementFunction))
        End Sub
#End Region

#Region "Type tests"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type1()
            Dim code =
<Code>
Class C
    Public Sub Foo(Optional i$$ As Integer = 0) { }
End Class
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "Integer",
                             .AsFullName = "System.Int32",
                             .CodeTypeFullName = "System.Int32",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                         })
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type2()
            Dim code =
<Code>
Class C
    Public Sub Foo(Optional $$s$ = 0) { }
End Class
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "String",
                             .AsFullName = "System.String",
                             .CodeTypeFullName = "System.String",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefString
                         })
        End Sub

#End Region

#Region "DefaultValue tests"
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DefaultValue()
            Dim code =
<Code>
Class C
    Sub M(Optional $$s As String = "Foo")
    End Sub
End Class
</Code>

            TestDefaultValue(code, """Foo""")
        End Sub
#End Region

#Region "Set ParameterKind tests"
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetParameterKind_In()
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String)
    End Sub
End Class
</Code>
            TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindIn)
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetParameterKind_None()
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String)
    End Sub
End Class
</Code>
            TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindNone)
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetParameterKind_Out()
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String)
    End Sub
End Class
</Code>
            TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindOut, ThrowsArgumentException(Of EnvDTE80.vsCMParameterKind)())
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetParameterKind_Ref()
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(ByRef s As String)
    End Sub
End Class
</Code>
            TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetParameterKind_Params()
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(ParamArray s As String)
    End Sub
End Class
</Code>
            TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray)
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetParameterKind_Optional()
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(Optional s As String)
    End Sub
End Class
</Code>
            TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional)
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetParameterKind_Same()
            Dim code =
<Code>
Class C
    Sub M(ByRef $$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(ByRef s As String)
    End Sub
End Class
</Code>
            TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetParameterKind_OptionalByref()
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(Optional ByRef s As String)
    End Sub
End Class
</Code>
            TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindRef Or EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional)
        End Sub

#End Region

#Region "Set DefaultValue tests"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDefaultValue()
            Dim code =
<Code>
Class C
    Sub M(Optional $$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(Optional s As String = "Foo")
    End Sub
End Class
</Code>
            TestSetDefaultValue(code, expected, """Foo""")
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDefaultValue_ReplaceExisting()
            Dim code =
<Code>
Class C
    Sub M(Optional $$s As String = "Bar")
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(Optional s As String = "Foo")
    End Sub
End Class
</Code>
            TestSetDefaultValue(code, expected, """Foo""")
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDefaultValue_None()
            Dim code =
<Code>
Class C
    Sub M(Optional $$s As String = "Bar")
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String)
    End Sub
End Class
</Code>
            TestSetDefaultValue(code, expected, "")
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDefaultValue_OptionalMissing()
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String)
    End Sub
End Class
</Code>
            TestSetDefaultValue(code, expected, """Foo""", ThrowsArgumentException(Of String)())
        End Sub

#End Region

#Region "Set Type tests"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType1()
            Dim code =
<Code>
Class C
    Public Sub Foo(Optional i$$ As Integer = 0)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Sub Foo(Optional i As System.Nullable(Of Byte)(,) = 0)
    End Sub
End Class
</Code>

            TestSetTypeProp(code, expected, "Byte?(,)")
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType2()
            Dim code =
<Code>
Class C
    Public Sub Foo(Optional $$s$ = "Foo")
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Sub Foo(Optional s$ As Integer = "Foo")
    End Sub
End Class
</Code>

            TestSetTypeProp(code, expected, "System.Int32")
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType3()
            Dim code =
<Code>
Class C
    Public Sub Foo(i$$ As Integer,
                   j As Integer)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Sub Foo(i As String,
                   j As Integer)
    End Sub
End Class
</Code>

            TestSetTypeProp(code, expected, "String")
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType4()
            Dim code =
<Code>
Class C
    Public Sub Foo(i$$ As Integer,
                   j As Integer)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Sub Foo(i As Integer,
                   j As Integer)
    End Sub
End Class
</Code>

            TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Sub

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

    End Class
End Namespace

