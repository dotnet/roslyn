' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeStructTests
        Inherits AbstractCodeStructTests

#Region "Parts tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parts1()
            Dim code =
<Code>
Structure $$S
End Structure
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parts2()
            Dim code =
<Code>
Partial Structure $$S
End Structure
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parts3()
            Dim code =
<Code>
Partial Structure $$S
End Structure

Partial Structure S
End Structure
</Code>

            TestParts(code, 2)
        End Sub
#End Region

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute1()
            Dim code =
<Code>
Imports System

Structure $$S
End Structure
</Code>

            Dim expected =
<Code>
Imports System

&lt;Serializable()&gt;
Structure S
End Structure
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute2()
            Dim code =
<Code>
Imports System

&lt;Serializable&gt;
Structure $$S
End Structure
</Code>

            Dim expected =
<Code>
Imports System

&lt;Serializable&gt;
&lt;CLSCompliant(True)&gt;
Structure S
End Structure
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True", .Position = 1})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_BelowDocComment()
            Dim code =
<Code>
Imports System

''' &lt;summary&gt;&lt;/summary&gt;
Structure $$S
End Structure
</Code>

            Dim expected =
<Code>
Imports System

''' &lt;summary&gt;&lt;/summary&gt;
&lt;CLSCompliant(True)&gt;
Structure S
End Structure
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True"})
        End Sub

#End Region

#Region "AddImplementedInterface tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface1()
            Dim code =
<Code>
Structure $$S
End Structure
</Code>

            Dim expected =
<Code>
Structure S
    Implements I
End Structure
</Code>

            TestAddImplementedInterface(code, "I", Nothing, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface2()
            Dim code =
<Code>
Structure $$S
    Implements I
End Structure
</Code>

            Dim expected =
<Code>
Structure S
    Implements J
    Implements I
End Structure
</Code>

            TestAddImplementedInterface(code, "J", Nothing, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface3()
            Dim code =
<Code>
Structure $$S
    Implements I
End Structure
</Code>

            Dim expected =
<Code>
Structure S
    Implements I
    Implements J
End Structure
</Code>

            TestAddImplementedInterface(code, "J", -1, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface4()
            Dim code =
<Code>
Structure $$S
End Structure
</Code>

            TestAddImplementedInterfaceThrows(Of ArgumentException)(code, "I", 1)
        End Sub

#End Region

#Region "RemoveImplementedInterface tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveImplementedInterface1()
            Dim code =
<Code>
Structure $$S
    Implements I
End Structure
</Code>

            Dim expected =
<Code>
Structure S
End Structure
</Code>
            TestRemoveImplementedInterface(code, "I", expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveImplementedInterface2()
            Dim code =
<Code>
Structure $$S
End Structure
</Code>

            TestRemoveImplementedInterfaceThrows(Of COMException)(code, "I")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveImplementedInterface3()
            Dim code =
<Code>
Structure $$S
    Implements I, J
End Structure
</Code>

            Dim expected =
<Code>
Structure S
    Implements I
End Structure
</Code>
            TestRemoveImplementedInterface(code, "J", expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveImplementedInterface4()
            Dim code =
<Code>
Structure $$S
    Implements I, J
End Structure
</Code>

            Dim expected =
<Code>
Structure S
    Implements J
End Structure
</Code>
            TestRemoveImplementedInterface(code, "I", expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveImplementedInterface5()
            Dim code =
<Code>
Structure $$S
    Implements I, J, K
End Structure
</Code>

            Dim expected =
<Code>
Structure S
    Implements I, K
End Structure
</Code>
            TestRemoveImplementedInterface(code, "J", expected)
        End Sub

#End Region

#Region "Set Name tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName1()
            Dim code =
<Code>
Structure $$Foo
End Structure
</Code>

            Dim expected =
<Code>
Structure Bar
End Structure
</Code>

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub
#End Region

#Region "GenericExtender"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetBaseTypesCount()
            Dim code =
<Code>
Structure S$$
End Structure
</Code>

            TestGenericNameExtender_GetBaseTypesCount(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetBaseGenericName()
            Dim code =
<Code>
Structure S$$
End Structure
</Code>

            TestGenericNameExtender_GetBaseGenericName(code, 1, "System.ValueType")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetImplementedTypesCount1()
            Dim code =
<Code>
Structure S$$
End Structure
</Code>

            TestGenericNameExtender_GetImplementedTypesCount(code, 0)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetImplementedTypesCount2()
            Dim code =
<Code>
Namespace N
    Structure S$$
        Implements IFoo(Of Integer)

    End Structure

    Interface IFoo(Of T)
    End Interface
End Namespace
</Code>

            TestGenericNameExtender_GetImplementedTypesCount(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetImplTypeGenericName1()
            Dim code =
<Code>
Structure S$$
End Structure
</Code>

            TestGenericNameExtender_GetImplTypeGenericName(code, 1, Nothing)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetImplTypeGenericName2()
            Dim code =
<Code>
Namespace N
    Structure S$$
        Implements IFoo(Of Integer)

    End Structure

    Interface IFoo(Of T)
    End Interface
End Namespace
</Code>

            TestGenericNameExtender_GetImplTypeGenericName(code, 1, "N.IFoo(Of Integer)")
        End Sub

#End Region

        Private Function GetGenericExtender(codeElement As EnvDTE80.CodeStruct2) As IVBGenericExtender
            Return CType(codeElement.Extender(ExtenderNames.VBGenericExtender), IVBGenericExtender)
        End Function

        Protected Overrides Function GenericNameExtender_GetBaseTypesCount(codeElement As EnvDTE80.CodeStruct2) As Integer
            Return GetGenericExtender(codeElement).GetBaseTypesCount()
        End Function

        Protected Overrides Function GenericNameExtender_GetImplementedTypesCount(codeElement As EnvDTE80.CodeStruct2) As Integer
            Return GetGenericExtender(codeElement).GetImplementedTypesCount()
        End Function

        Protected Overrides Function GenericNameExtender_GetBaseGenericName(codeElement As EnvDTE80.CodeStruct2, index As Integer) As String
            Return GetGenericExtender(codeElement).GetBaseGenericName(index)
        End Function

        Protected Overrides Function GenericNameExtender_GetImplTypeGenericName(codeElement As EnvDTE80.CodeStruct2, index As Integer) As String
            Return GetGenericExtender(codeElement).GetImplTypeGenericName(index)
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
