' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeStructTests
        Inherits AbstractCodeStructTests

#Region "Parts tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParts1()
            Dim code =
<Code>
Structure $$S
End Structure
</Code>

            TestParts(code, 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParts2()
            Dim code =
<Code>
Partial Structure $$S
End Structure
</Code>

            TestParts(code, 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParts3()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True", .Position = 1})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_BelowDocComment() As Task
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True"})
        End Function

#End Region

#Region "AddImplementedInterface tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImplementedInterface1() As Task
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

            Await TestAddImplementedInterface(code, "I", Nothing, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImplementedInterface2() As Task
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

            Await TestAddImplementedInterface(code, "J", Nothing, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImplementedInterface3() As Task
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

            Await TestAddImplementedInterface(code, "J", -1, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAddImplementedInterface4()
            Dim code =
<Code>
Structure $$S
End Structure
</Code>

            TestAddImplementedInterfaceThrows(Of ArgumentException)(code, "I", 1)
        End Sub

#End Region

#Region "RemoveImplementedInterface tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveImplementedInterface1() As Task
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
            Await TestRemoveImplementedInterface(code, "I", expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestRemoveImplementedInterface2()
            Dim code =
<Code>
Structure $$S
End Structure
</Code>

            TestRemoveImplementedInterfaceThrows(Of COMException)(code, "I")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveImplementedInterface3() As Task
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
            Await TestRemoveImplementedInterface(code, "J", expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveImplementedInterface4() As Task
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
            Await TestRemoveImplementedInterface(code, "I", expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveImplementedInterface5() As Task
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
            Await TestRemoveImplementedInterface(code, "J", expected)
        End Function

#End Region

#Region "Set Name tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code>
Structure $$Goo
End Structure
</Code>

            Dim expected =
<Code>
Structure Bar
End Structure
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function
#End Region

#Region "GenericExtender"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetBaseTypesCount()
            Dim code =
<Code>
Structure S$$
End Structure
</Code>

            TestGenericNameExtender_GetBaseTypesCount(code, 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetBaseGenericName()
            Dim code =
<Code>
Structure S$$
End Structure
</Code>

            TestGenericNameExtender_GetBaseGenericName(code, 1, "System.ValueType")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetImplementedTypesCount1()
            Dim code =
<Code>
Structure S$$
End Structure
</Code>

            TestGenericNameExtender_GetImplementedTypesCount(code, 0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetImplementedTypesCount2()
            Dim code =
<Code>
Namespace N
    Structure S$$
        Implements IGoo(Of Integer)

    End Structure

    Interface IGoo(Of T)
    End Interface
End Namespace
</Code>

            TestGenericNameExtender_GetImplementedTypesCount(code, 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetImplTypeGenericName1()
            Dim code =
<Code>
Structure S$$
End Structure
</Code>

            TestGenericNameExtender_GetImplTypeGenericName(code, 1, Nothing)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetImplTypeGenericName2()
            Dim code =
<Code>
Namespace N
    Structure S$$
        Implements IGoo(Of Integer)

    End Structure

    Interface IGoo(Of T)
    End Interface
End Namespace
</Code>

            TestGenericNameExtender_GetImplTypeGenericName(code, 1, "N.IGoo(Of Integer)")
        End Sub

#End Region

        Private Shared Function GetGenericExtender(codeElement As EnvDTE80.CodeStruct2) As IVBGenericExtender
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
