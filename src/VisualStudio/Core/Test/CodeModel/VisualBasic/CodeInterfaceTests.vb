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
    Public Class CodeInterfaceTests
        Inherits AbstractCodeInterfaceTests

#Region "Access tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess1()
            Dim code =
<Code>
Interface $$I : End Interface
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess2()
            Dim code =
<Code>
Friend Interface $$I : End Interface
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess3()
            Dim code =
<Code>
Public Interface $$I : End Interface
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Parts tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParts1()
            Dim code =
<Code>
Interface $$I
End Interface
</Code>

            TestParts(code, 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParts2()
            Dim code =
<Code>
Partial Interface $$I
End Interface
</Code>

            TestParts(code, 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParts3()
            Dim code =
<Code>
Partial Interface $$I
End Interface

Partial Interface I
End Interface
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

Interface $$I
End Interface
</Code>

            Dim expected =
<Code>
Imports System

&lt;Serializable()&gt;
Interface I
End Interface
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
            Dim code =
<Code>
Imports System

&lt;Serializable&gt;
Interface $$I
End Interface
</Code>

            Dim expected =
<Code>
Imports System

&lt;Serializable&gt;
&lt;CLSCompliant(True)&gt;
Interface I
End Interface
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True", .Position = 1})
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/2825")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_BelowDocComment() As Task
            Dim code =
<Code>
Imports System

''' &lt;summary&gt;&lt;/summary&gt;
Interface $$I
End Interface
</Code>

            Dim expected =
<Code>
Imports System

''' &lt;summary&gt;&lt;/summary&gt;
&lt;CLSCompliant(True)&gt;
Interface I
End Interface
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True"})
        End Function

#End Region

#Region "AddBase tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddBase1() As Task
            Dim code =
<Code>
Interface $$I
End Interface

Interface J
End Interface
</Code>

            Dim expected =
<Code>
Interface I
    Inherits J
End Interface

Interface J
End Interface
</Code>
            Await TestAddBase(code, "J", Nothing, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddBase2() As Task
            Dim code =
<Code>
Interface $$I
    Inherits J
End Interface

Interface K
End Interface
</Code>

            Dim expected =
<Code>
Interface I
    Inherits K
    Inherits J
End Interface

Interface K
End Interface
</Code>
            Await TestAddBase(code, "K", Nothing, expected)
        End Function

#End Region

#Region "RemoveBase tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveBase1() As Task
            Dim code =
<Code>
Interface $$I
    Inherits J
End Interface
</Code>

            Dim expected =
<Code>
Interface I
End Interface
</Code>
            Await TestRemoveBase(code, "J", expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestRemoveBase2()
            Dim code =
<Code>
Interface $$I
End Interface
</Code>

            TestRemoveBaseThrows(Of COMException)(code, "J")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveBase3() As Task
            Dim code =
<Code>
Interface $$I
    Inherits J, K
End Interface
</Code>

            Dim expected =
<Code>
Interface I
    Inherits J
End Interface
</Code>
            Await TestRemoveBase(code, "K", expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveBase4() As Task
            Dim code =
<Code>
Interface $$I
    Inherits J, K
End Interface
</Code>

            Dim expected =
<Code>
Interface I
    Inherits K
End Interface
</Code>
            Await TestRemoveBase(code, "J", expected)
        End Function

#End Region

#Region "Set Name tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code>
Interface $$Goo
End Interface
</Code>

            Dim expected =
<Code>
Interface Bar
End Interface
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function
#End Region

#Region "GenericExtender"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetBaseTypesCount1()
            Dim code =
<Code>
Interface I$$
End Interface
</Code>

            TestGenericNameExtender_GetBaseTypesCount(code, 0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetBaseTypesCount2()
            Dim code =
<Code>
Namespace N
    Interface I$$
        Inherits IGoo(Of Integer)
    End Interface

    Interface IBar
    End Interface

    Interface IGoo(Of T)
        Inherits IBar
    End Interface
End Namespace
</Code>

            TestGenericNameExtender_GetBaseTypesCount(code, 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetBaseGenericName1()
            Dim code =
<Code>
Interface I$$
End Interface
</Code>

            TestGenericNameExtender_GetBaseGenericName(code, 1, Nothing)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetBaseGenericName2()
            Dim code =
<Code>
Namespace N
    Interface I$$
        Inherits IGoo(Of System.Int32)
    End Interface

    Interface IBar
    End Interface

    Interface IGoo(Of T)
        Inherits IBar
    End Interface
End Namespace
</Code>

            TestGenericNameExtender_GetBaseGenericName(code, 1, "N.IGoo(Of Integer)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetImplementedTypesCount1()
            Dim code =
<Code>
Interface I$$
End Interface
</Code>

            TestGenericNameExtender_GetImplementedTypesCountThrows(Of ArgumentException)(code)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetImplementedTypesCount2()
            Dim code =
<Code>
Namespace N
    Interface I$$
        Inherits IGoo(Of Integer)
    End Interface

    Interface IBar
    End Interface

    Interface IGoo(Of T)
        Inherits IBar
    End Interface
End Namespace
</Code>

            TestGenericNameExtender_GetImplementedTypesCountThrows(Of ArgumentException)(code)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetImplTypeGenericName1()
            Dim code =
<Code>
Interface I$$
End Interface
</Code>

            TestGenericNameExtender_GetImplTypeGenericNameThrows(Of ArgumentException)(code, 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetImplTypeGenericName2()
            Dim code =
<Code>
Namespace N
    Interface I$$
        Inherits IGoo(Of Integer)
    End Interface

    Interface IBar
    End Interface

    Interface IGoo(Of T)
        Inherits IBar
    End Interface
End Namespace
</Code>

            TestGenericNameExtender_GetImplTypeGenericNameThrows(Of ArgumentException)(code, 1)
        End Sub

#End Region

        Private Shared Function GetGenericExtender(codeElement As EnvDTE80.CodeInterface2) As IVBGenericExtender
            Return CType(codeElement.Extender(ExtenderNames.VBGenericExtender), IVBGenericExtender)
        End Function

        Protected Overrides Function GenericNameExtender_GetBaseTypesCount(codeElement As EnvDTE80.CodeInterface2) As Integer
            Return GetGenericExtender(codeElement).GetBaseTypesCount()
        End Function

        Protected Overrides Function GenericNameExtender_GetImplementedTypesCount(codeElement As EnvDTE80.CodeInterface2) As Integer
            Return GetGenericExtender(codeElement).GetImplementedTypesCount()
        End Function

        Protected Overrides Function GenericNameExtender_GetBaseGenericName(codeElement As EnvDTE80.CodeInterface2, index As Integer) As String
            Return GetGenericExtender(codeElement).GetBaseGenericName(index)
        End Function

        Protected Overrides Function GenericNameExtender_GetImplTypeGenericName(codeElement As EnvDTE80.CodeInterface2, index As Integer) As String
            Return GetGenericExtender(codeElement).GetImplTypeGenericName(index)
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
