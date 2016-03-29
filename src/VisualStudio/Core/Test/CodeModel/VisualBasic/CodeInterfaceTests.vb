' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeInterfaceTests
        Inherits AbstractCodeInterfaceTests

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess1() As Task
            Dim code =
<Code>
Interface $$I : End Interface
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess2() As Task
            Dim code =
<Code>
Friend Interface $$I : End Interface
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess3() As Task
            Dim code =
<Code>
Public Interface $$I : End Interface
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

#End Region

#Region "Parts tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParts1() As Task
            Dim code =
<Code>
Interface $$I
End Interface
</Code>

            Await TestParts(code, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParts2() As Task
            Dim code =
<Code>
Partial Interface $$I
End Interface
</Code>

            Await TestParts(code, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParts3() As Task
            Dim code =
<Code>
Partial Interface $$I
End Interface

Partial Interface I
End Interface
</Code>

            Await TestParts(code, 2)
        End Function
#End Region

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveBase2() As Task
            Dim code =
<Code>
Interface $$I
End Interface
</Code>

            Await TestRemoveBaseThrows(Of COMException)(code, "J")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
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

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function
#End Region

#Region "GenericExtender"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetBaseTypesCount1() As Task
            Dim code =
<Code>
Interface I$$
End Interface
</Code>

            Await TestGenericNameExtender_GetBaseTypesCount(code, 0)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetBaseTypesCount2() As Task
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

            Await TestGenericNameExtender_GetBaseTypesCount(code, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetBaseGenericName1() As Task
            Dim code =
<Code>
Interface I$$
End Interface
</Code>

            Await TestGenericNameExtender_GetBaseGenericName(code, 1, Nothing)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetBaseGenericName2() As Task
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

            Await TestGenericNameExtender_GetBaseGenericName(code, 1, "N.IFoo(Of Integer)")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetImplementedTypesCount1() As Task
            Dim code =
<Code>
Interface I$$
End Interface
</Code>

            Await TestGenericNameExtender_GetImplementedTypesCountThrows(Of ArgumentException)(code)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetImplementedTypesCount2() As Task
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

            Await TestGenericNameExtender_GetImplementedTypesCountThrows(Of ArgumentException)(code)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetImplTypeGenericName1() As Task
            Dim code =
<Code>
Interface I$$
End Interface
</Code>

            Await TestGenericNameExtender_GetImplTypeGenericNameThrows(Of ArgumentException)(code, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetImplTypeGenericName2() As Task
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

            Await TestGenericNameExtender_GetImplTypeGenericNameThrows(Of ArgumentException)(code, 1)
        End Function

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
