' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeEnumTests
        Inherits AbstractCodeEnumTests

#Region "GetStartPoint() tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint1()
            Dim code =
<Code>
Enum E$$

End Enum
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=8, lineLength:=0)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=8, lineLength:=0)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=6)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=6)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=6, absoluteOffset:=6, lineLength:=6)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=Nothing, lineLength:=0)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=6)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=6)))
        End Sub

#End Region

#Region "GetEndPoint() tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint1()
            Dim code =
<Code>
Enum E$$

End Enum
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=9, lineLength:=8)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=9, lineLength:=8)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=7, absoluteOffset:=7, lineLength:=6)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=7, absoluteOffset:=7, lineLength:=6)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=7, absoluteOffset:=7, lineLength:=6)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=9, lineLength:=8)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=9, absoluteOffset:=17, lineLength:=8)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=9, absoluteOffset:=17, lineLength:=8)))
        End Sub

#End Region

#Region "Access tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess1()
            Dim code =
<Code>
Enum $$E : End Enum
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess2()
            Dim code =
<Code>
Class C
    Enum $$E : End Enum
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess3()
            Dim code =
<Code>
Private Enum $$E : End Enum
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess4()
            Dim code =
<Code>
Protected Enum $$E : End Enum
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess5()
            Dim code =
<Code>
Protected Friend Enum $$E : End Enum
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess6()
            Dim code =
<Code>
Friend Enum $$E : End Enum
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess7()
            Dim code =
<Code>
Public Enum $$E : End Enum
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "AddAttribute tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
            Dim code =
<Code>
Imports System

Enum $$E
    Goo = 1,
    Bar
End Enum
</Code>

            Dim expected =
<Code>
Imports System

&lt;Flags()&gt;
Enum E
    Goo = 1,
    Bar
End Enum
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Flags"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
            Dim code =
<Code>
Imports System

&lt;Flags&gt;
Enum $$E
    Goo = 1,
    Bar
End Enum
</Code>

            Dim expected =
<Code>
Imports System

&lt;Flags&gt;
&lt;CLSCompliant(True)&gt;
Enum E
    Goo = 1,
    Bar
End Enum
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
Enum $$E
    Goo = 1,
    Bar
End Enum
</Code>

            Dim expected =
<Code>
Imports System

''' &lt;summary&gt;&lt;/summary&gt;
&lt;Flags()&gt;
Enum E
    Goo = 1,
    Bar
End Enum
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Flags"})
        End Function

#End Region

#Region "Set Name tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code>
Enum $$Goo
End Enum
</Code>

            Dim expected =
<Code>
Enum Bar
End Enum
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function
#End Region

#Region "GenericExtender"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetBaseTypesCount()
            Dim code =
<Code>
Enum E$$
End Enum
</Code>

            TestGenericNameExtender_GetBaseTypesCount(code, 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetBaseGenericName()
            Dim code =
<Code>
Enum E$$
End Enum
</Code>

            TestGenericNameExtender_GetBaseGenericName(code, 1, "System.Enum")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetImplementedTypesCount()
            Dim code =
<Code>
Enum E$$
End Enum
</Code>

            TestGenericNameExtender_GetImplementedTypesCountThrows(Of ArgumentException)(code)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetImplTypeGenericName()
            Dim code =
<Code>
Enum E$$
End Enum
</Code>

            TestGenericNameExtender_GetImplTypeGenericName(code, 1, Nothing)
        End Sub

#End Region

        Private Shared Function GetGenericExtender(codeElement As EnvDTE.CodeEnum) As IVBGenericExtender
            Return CType(codeElement.Extender(ExtenderNames.VBGenericExtender), IVBGenericExtender)
        End Function

        Protected Overrides Function GenericNameExtender_GetBaseTypesCount(codeElement As EnvDTE.CodeEnum) As Integer
            Return GetGenericExtender(codeElement).GetBaseTypesCount()
        End Function

        Protected Overrides Function GenericNameExtender_GetImplementedTypesCount(codeElement As EnvDTE.CodeEnum) As Integer
            Return GetGenericExtender(codeElement).GetImplementedTypesCount()
        End Function

        Protected Overrides Function GenericNameExtender_GetBaseGenericName(codeElement As EnvDTE.CodeEnum, index As Integer) As String
            Return GetGenericExtender(codeElement).GetBaseGenericName(index)
        End Function

        Protected Overrides Function GenericNameExtender_GetImplTypeGenericName(codeElement As EnvDTE.CodeEnum, index As Integer) As String
            Return GetGenericExtender(codeElement).GetImplTypeGenericName(index)
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace

