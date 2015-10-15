' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeInterfaceTests
        Inherits AbstractCodeInterfaceTests

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access1()
            Dim code =
<Code>
Interface $$I : End Interface
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access2()
            Dim code =
<Code>
Friend Interface $$I : End Interface
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access3()
            Dim code =
<Code>
Public Interface $$I : End Interface
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Parts tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parts1()
            Dim code =
<Code>
Interface $$I
End Interface
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parts2()
            Dim code =
<Code>
Partial Interface $$I
End Interface
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parts3()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute1()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute2()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True", .Position = 1})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_BelowDocComment()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True"})
        End Sub

#End Region

#Region "AddBase tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddBase1()
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
            TestAddBase(code, "J", Nothing, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddBase2()
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
            TestAddBase(code, "K", Nothing, expected)
        End Sub

#End Region

#Region "RemoveBase tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveBase1()
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
            TestRemoveBase(code, "J", expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveBase2()
            Dim code =
<Code>
Interface $$I
End Interface
</Code>

            TestRemoveBaseThrows(Of COMException)(code, "J")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveBase3()
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
            TestRemoveBase(code, "K", expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveBase4()
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
            TestRemoveBase(code, "J", expected)
        End Sub

#End Region

#Region "Set Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName1()
            Dim code =
<Code>
Interface $$Foo
End Interface
</Code>

            Dim expected =
<Code>
Interface Bar
End Interface
</Code>

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub
#End Region

#Region "GenericExtender"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetBaseTypesCount1()
            Dim code =
<Code>
Interface I$$
End Interface
</Code>

            TestGenericNameExtender_GetBaseTypesCount(code, 0)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetBaseTypesCount2()
            Dim code =
<Code>
Namespace N
    Interface I$$
        Inherits IFoo(Of Integer)
    End Interface

    Interface IBar
    End Interface

    Interface IFoo(Of T)
        Inherits IBar
    End Interface
End Namespace
</Code>

            TestGenericNameExtender_GetBaseTypesCount(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetBaseGenericName1()
            Dim code =
<Code>
Interface I$$
End Interface
</Code>

            TestGenericNameExtender_GetBaseGenericName(code, 1, Nothing)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetBaseGenericName2()
            Dim code =
<Code>
Namespace N
    Interface I$$
        Inherits IFoo(Of System.Int32)
    End Interface

    Interface IBar
    End Interface

    Interface IFoo(Of T)
        Inherits IBar
    End Interface
End Namespace
</Code>

            TestGenericNameExtender_GetBaseGenericName(code, 1, "N.IFoo(Of Integer)")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetImplementedTypesCount1()
            Dim code =
<Code>
Interface I$$
End Interface
</Code>

            TestGenericNameExtender_GetImplementedTypesCountThrows(Of ArgumentException)(code)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetImplementedTypesCount2()
            Dim code =
<Code>
Namespace N
    Interface I$$
        Inherits IFoo(Of Integer)
    End Interface

    Interface IBar
    End Interface

    Interface IFoo(Of T)
        Inherits IBar
    End Interface
End Namespace
</Code>

            TestGenericNameExtender_GetImplementedTypesCountThrows(Of ArgumentException)(code)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetImplTypeGenericName1()
            Dim code =
<Code>
Interface I$$
End Interface
</Code>

            TestGenericNameExtender_GetImplTypeGenericNameThrows(Of ArgumentException)(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetImplTypeGenericName2()
            Dim code =
<Code>
Namespace N
    Interface I$$
        Inherits IFoo(Of Integer)
    End Interface

    Interface IBar
    End Interface

    Interface IFoo(Of T)
        Inherits IBar
    End Interface
End Namespace
</Code>

            TestGenericNameExtender_GetImplTypeGenericNameThrows(Of ArgumentException)(code, 1)
        End Sub

#End Region

        Private Function GetGenericExtender(codeElement As EnvDTE80.CodeInterface2) As IVBGenericExtender
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
