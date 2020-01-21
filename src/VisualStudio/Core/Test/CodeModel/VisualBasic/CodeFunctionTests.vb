' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeFunctionTests
        Inherits AbstractCodeFunctionTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_MustOverride1()
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$M()
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=25, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=25, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=25, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=25, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=22, absoluteOffset:=42, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=25, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=25, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=25, lineLength:=24)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_MustOverride2()
            Dim code =
<Code>
MustInherit Class C
    &lt;System.CLSCompliant(True)&gt;
    MustOverride Sub $$M()
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=25, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=25, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=57, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=57, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=57, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=25, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=74, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=57, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=57, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=25, lineLength:=31)))
        End Sub

        <WorkItem(1839, "https://github.com/dotnet/roslyn/issues/1839")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_DeclareFunction_WithoutAttribute()
            Dim code =
<Code>
Public Class C1
    Declare Function $$getUserName Lib "My1.dll" () As String
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=22, absoluteOffset:=38, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=59)))
        End Sub

        <WorkItem(1839, "https://github.com/dotnet/roslyn/issues/1839")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_DeclareFunction_WithAttribute()
            Dim code =
<Code>
Public Class C1
    &lt;System.CLSCompliant(True)&gt;
    Declare Function $$getUserName Lib "My1.dll" () As String
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=53, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=53, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=53, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=70, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=53, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=53, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=31)))
        End Sub

        <WorkItem(1839, "https://github.com/dotnet/roslyn/issues/1839")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_DeclareSub_WithoutAttribute()
            Dim code =
<Code>
Public Class C1
    Public Declare Sub $$MethodName Lib "My1.dll"
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=47)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=47)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=47)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=47)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=24, absoluteOffset:=40, lineLength:=47)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=47)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=47)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=47)))
        End Sub

        <WorkItem(1839, "https://github.com/dotnet/roslyn/issues/1839")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_DeclareSub_WithAttribute()
            Dim code =
<Code>
Public Class C1
    &lt;System.CLSCompliant(True)&gt;
    Public Declare Sub $$MethodName Lib "My1.dll"
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=53, lineLength:=47)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=53, lineLength:=47)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=53, lineLength:=47)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=24, absoluteOffset:=72, lineLength:=47)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=53, lineLength:=47)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=53, lineLength:=47)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=21, lineLength:=31)))
        End Sub

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_MustOverride1()
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$M()
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=25, absoluteOffset:=45, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=25, absoluteOffset:=45, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=25, absoluteOffset:=45, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=25, absoluteOffset:=45, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=23, absoluteOffset:=43, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=25, absoluteOffset:=45, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=25, absoluteOffset:=45, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=25, absoluteOffset:=45, lineLength:=24)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_MustOverride2()
            Dim code =
<Code>
MustInherit Class C
    &lt;System.CLSCompliant(True)&gt;
    MustOverride Sub $$M()
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=52, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=52, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=25, absoluteOffset:=77, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=25, absoluteOffset:=77, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=25, absoluteOffset:=77, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=25, absoluteOffset:=77, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=23, absoluteOffset:=75, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=25, absoluteOffset:=77, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=25, absoluteOffset:=77, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=25, absoluteOffset:=77, lineLength:=24)))
        End Sub

        <WorkItem(1839, "https://github.com/dotnet/roslyn/issues/1839")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_DeclareFunction_WithoutAttribute()
            Dim code =
<Code>
Public Class C1
    Declare Function $$getUserName Lib "My1.dll" () As String
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=60, absoluteOffset:=76, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=60, absoluteOffset:=76, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=60, absoluteOffset:=76, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=60, absoluteOffset:=76, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=33, absoluteOffset:=49, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=60, absoluteOffset:=76, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=60, absoluteOffset:=76, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=60, absoluteOffset:=76, lineLength:=59)))
        End Sub

        <WorkItem(1839, "https://github.com/dotnet/roslyn/issues/1839")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_DeclareFunction_WithAttribute()
            Dim code =
<Code>
Public Class C1
    &lt;System.CLSCompliant(True)&gt;
    Declare Function $$getUserName Lib "My1.dll" () As String
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=48, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=48, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=60, absoluteOffset:=108, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=60, absoluteOffset:=108, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=60, absoluteOffset:=108, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=60, absoluteOffset:=108, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=33, absoluteOffset:=81, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=60, absoluteOffset:=108, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=60, absoluteOffset:=108, lineLength:=59)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=60, absoluteOffset:=108, lineLength:=59)))
        End Sub

        <WorkItem(1839, "https://github.com/dotnet/roslyn/issues/1839")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_DeclareSub_WithoutAttribute()
            Dim code =
<Code>
Public Class C1
    Declare Sub $$getUserName Lib "My1.dll" ()
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=45, absoluteOffset:=61, lineLength:=44)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=45, absoluteOffset:=61, lineLength:=44)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=45, absoluteOffset:=61, lineLength:=44)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=45, absoluteOffset:=61, lineLength:=44)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=28, absoluteOffset:=44, lineLength:=44)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=45, absoluteOffset:=61, lineLength:=44)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=45, absoluteOffset:=61, lineLength:=44)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=45, absoluteOffset:=61, lineLength:=44)))
        End Sub

        <WorkItem(1839, "https://github.com/dotnet/roslyn/issues/1839")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_DeclareSub_WithAttribute()
            Dim code =
<Code>
Public Class C1
    &lt;System.CLSCompliant(True)&gt;
    Declare Sub $$getUserName Lib "My1.dll" ()
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=48, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=48, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=45, absoluteOffset:=93, lineLength:=44)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=45, absoluteOffset:=93, lineLength:=44)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=45, absoluteOffset:=93, lineLength:=44)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=45, absoluteOffset:=93, lineLength:=44)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=28, absoluteOffset:=76, lineLength:=44)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=45, absoluteOffset:=93, lineLength:=44)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=45, absoluteOffset:=93, lineLength:=44)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=45, absoluteOffset:=93, lineLength:=44)))
        End Sub

#End Region

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess1()
            Dim code =
    <Code>
Class C
    Function $$F() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess2()
            Dim code =
    <Code>
Class C
    Private Function $$F() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess3()
            Dim code =
    <Code>
Class C
    Protected Function $$F() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess4()
            Dim code =
    <Code>
Class C
    Protected Friend Function $$F() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess5()
            Dim code =
    <Code>
Class C
    Friend Function $$F() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess6()
            Dim code =
    <Code>
Class C
    Public Function $$F() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess7()
            Dim code =
<Code>
Interface I
    Function $$F() As Integer
End Interface
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Attribute Tests"
        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPropertyGetAttribute_WithNoSet()
            Dim code =
<Code>
Public Class Class1
    Public Property Property1 As Integer
        &lt;Obsolete&gt;
        $$Get
            Return 0
        End Get
    End Property
End Class
</Code>

            TestAttributes(code, IsElement("Obsolete"))
        End Sub

        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPropertySetAttribute_WithNoGet()
            Dim code =
<Code>
Public Class Class1
    Public Property Property1 As Integer
        &lt;Obsolete&gt;
        $$Set(value As Integer)

        End Set
    End Property
End Class
</Code>

            TestAttributes(code, IsElement("Obsolete"))
        End Sub

        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPropertySetAttribute_WithGet()
            Dim code =
<Code>
Public Class Class1
    Public Property Property1 As Integer
        &lt;Obsolete&gt;
        Get
            Return 0
        End Get
        &lt;Obsolete&gt;
        $$Set(value As Integer)

        End Set
    End Property
End Class
</Code>

            TestAttributes(code, IsElement("Obsolete"))
        End Sub

        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPropertyGetAttribute_WithSet()
            Dim code =
<Code>
Public Class Class1
    Public Property Property1 As Integer
        &lt;Obsolete&gt;
        $$Get
            Return 0
        End Get
        &lt;Obsolete&gt;
        Set(value As Integer)

        End Set
    End Property
End Class
</Code>

            TestAttributes(code, IsElement("Obsolete"))
        End Sub

        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttribute_1()
            Dim code =
<Code>
Class Program
    &lt;Obsolete&gt;
    Sub F$$()

    End Sub
End Class
</Code>

            TestAttributes(code, IsElement("Obsolete"))
        End Sub
#End Region

#Region "CanOverride tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCanOverride1()
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$Goo()
End Class
</Code>

            TestCanOverride(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCanOverride2()
            Dim code =
<Code>
Interface I
    Sub $$Goo()
End Interface
</Code>

            TestCanOverride(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCanOverride3()
            Dim code =
<Code>
Class C
    Protected Overridable Sub $$Goo()

    End Sub
End Class
</Code>

            TestCanOverride(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCanOverride4()
            Dim code =
<Code>
Class C
    Protected Sub $$Goo()

    End Sub
End Class
</Code>

            TestCanOverride(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCanOverride5()
            Dim code =
<Code>
Class B
    Protected Overridable Sub Goo()

    End Sub
End Class

Class C
    Inherits B

    Protected Overrides Sub $$Goo()

    End Sub
End Class
</Code>

            TestCanOverride(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCanOverride6()
            Dim code =
<Code>
Class B
    Protected Overridable Sub Goo()

    End Sub
End Class

Class C
    Inherits B

    Protected NotOverridable Overrides Sub $$Goo()

    End Sub
End Class
</Code>

            TestCanOverride(code, False)
        End Sub

#End Region

#Region "FunctionKind tests"

        <WorkItem(1843, "https://github.com/dotnet/roslyn/issues/1843")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFunctionKind_Constructor()
            Dim code =
<Code>
Public Class C1

   Public Sub $$New()
   End Sub

End Class
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionConstructor)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFunctionKind_Destructor()
            Dim code =
<Code>
Public Class C1

   Protected Overrides Sub $$Finalize()
      MyBase.Finalize()
   End Sub

End Class
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionDestructor)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFunctionKind_Sub()
            Dim code =
<Code>
Public Class C1

   Private Sub $$M()
   End Sub

End Class
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionSub)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFunctionKind_Function()
            Dim code =
<Code>
Public Class C1

   Private Function $$M() As Integer
   End Sub

End Class
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionFunction)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFunctionKind_DeclareSub()
            Dim code =
<Code>
Public Class C1

   Private Declare Sub $$MethodB Lib "MyDll.dll" ()

End Class
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionSub)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFunctionKind_DeclareFunction()
            Dim code =
<Code>
Public Class C1

   Private Declare Function $$MethodC Lib "MyDll.dll" () As Integer

End Class
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionFunction)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFunctionKind__Operator()
            Dim code =
<Code>
Imports System

Class C
    Public Shared Operator $$+(x As C, y As C) As C
    End Operator
End Class
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionOperator)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFunctionKind_Conversion()
            Dim code =
<Code>
Imports System

Class C
    Public Shared Operator Widening $$CType(x As Integer) As C
    End Operator
End Class
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionOperator)
        End Sub

#End Region

#Region "MustImplement tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestMustImplement1()
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$Goo()
End Class
</Code>

            TestMustImplement(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestMustImplement2()
            Dim code =
<Code>
Interface I
    Sub $$Goo()
End Interface
</Code>

            TestMustImplement(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestMustImplement3()
            Dim code =
<Code>
Class C
    Protected Overridable Sub $$Goo()

    End Sub
End Class
</Code>

            TestMustImplement(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestMustImplement4()
            Dim code =
<Code>
Class C
    Protected Sub $$Goo()

    End Sub
End Class
</Code>

            TestMustImplement(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestMustImplement5()
            Dim code =
<Code>
Class B
    Protected Overridable Sub Goo()

    End Sub
End Class

Class C
    Inherits B

    Protected Overrides Sub $$Goo()

    End Sub
End Class
</Code>

            TestMustImplement(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestMustImplement6()
            Dim code =
<Code>
Class B
    Protected Overridable Sub Goo()

    End Sub
End Class

Class C
    Inherits B

    Protected NotOverridable Overrides Sub $$Goo()

    End Sub
End Class
</Code>

            TestMustImplement(code, False)
        End Sub

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName1()
            Dim code =
<Code>
Class C
    MustOverride Sub $$Goo()
End Class
</Code>

            TestName(code, "Goo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_NoParens()
            Dim code =
<Code>
Class C
    Sub $$Goo
End Class
</Code>

            TestName(code, "Goo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_Constructor1()
            Dim code =
<Code>
Class C
    Sub $$New()
    End Sub
End Class
</Code>

            TestName(code, "New")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_Constructor2()
            Dim code =
<Code>
Class C
    Sub $$New()
End Class
</Code>

            TestName(code, "New")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_Operator1()
            Dim code =
<Code>
Class C
    Shared Narrowing Operator $$CType(i As Integer) As C
    End Operator
End Class
</Code>

            TestName(code, "CType")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_Operator2()
            Dim code =
<Code>
Class C
    Shared Narrowing Operator $$CType(i As Integer) As C
End Class
</Code>

            TestName(code, "CType")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_Operator3()
            Dim code =
<Code>
Class C
    Shared Operator $$*(i As Integer, c As C) As C
    End Operator
End Class
</Code>

            TestName(code, "*")
        End Sub

#End Region

#Region "Kind tests"

        <WorkItem(2355, "https://github.com/dotnet/roslyn/issues/2355")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDeclareSubKind()
            Dim code =
<Code>
Public Class Class1 
Public Declare Sub $$f1 Lib "MyLib.dll" () 
End Class 
</Code>

            TestKind(code, EnvDTE.vsCMElement.vsCMElementDeclareDecl)
        End Sub

        <WorkItem(2355, "https://github.com/dotnet/roslyn/issues/2355")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDeclareFunctionKind()
            Dim code =
<Code>
Public Class Class1 
Public Declare Function f2$$ Lib "MyLib.dll" () As Integer 
End Class 
</Code>

            TestKind(code, EnvDTE.vsCMElement.vsCMElementDeclareDecl)
        End Sub

        <WorkItem(2355, "https://github.com/dotnet/roslyn/issues/2355")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestSubKind()
            Dim code =
<Code>
Public Class Class1 
    Public Sub F1$$()
    End Sub
End Class 
</Code>

            TestKind(code, EnvDTE.vsCMElement.vsCMElementFunction)
        End Sub
#End Region

#Region "OverrideKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Abstract()
            Dim code =
<Code>
MustInherit Class C
    Protected MustOverride Sub $$Goo()
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Virtual()
            Dim code =
<Code>
Class C
    Protected Overridable Sub $$Goo()
    End Sub
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Sealed()
            Dim code =
<Code>
Class C
    Protected NotOverridable Sub $$Goo()
    End Sub
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Override()
            Dim code =
<Code>
MustInherit Class B
    Protected MustOverride Sub Goo()
End Class

Class C
    Inherits B

    Protected Overrides Sub $$Goo()
    End Sub
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_New()
            Dim code =
<Code>
MustInherit Class B
    Protected MustOverride Sub Goo()
End Class

Class C
    Inherits B

    Protected Shadows Sub $$Goo()
    End Sub
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNew)
        End Sub

#End Region

#Region "Prototype tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_UniqueSignature()
            Dim code =
<Code>
Namespace N
    Class C
        Sub $$Goo()
        End Sub
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature, "M:N.C.Goo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_FullName()
            Dim code =
<Code>
Namespace N
    Class C
        Sub $$Goo()
        End Sub
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "N.C.Goo()")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName()
            Dim code =
<Code>
Namespace N
    Class C
        Sub $$Goo()
        End Sub
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.Goo()")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_Type1()
            Dim code =
<Code>
Namespace N
    Class C
        Sub $$Goo()
        End Sub
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "Goo()")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_Type2()
            Dim code =
<Code>
Namespace N
    Class C
        Function $$Goo() As Integer
        End Function
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "Goo() As Integer")
        End Sub

#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType1()
            Dim code =
<Code>
Class C
    Sub $$Goo()
    End Sub
End Class
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "Void",
                             .AsFullName = "System.Void",
                             .CodeTypeFullName = "System.Void",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefVoid
                         })
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType2()
            Dim code =
<Code>
Class C
    Function $$Goo$()
    End Function
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType3()
            Dim code =
<Code>
Class C
    Function $$Goo() As String
    End Function
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType4()
            Dim code =
<Code>
MustInherit Class C
    MustOverride Function $$Goo() As String
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

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_Sub() As Task
            Dim code =
<Code>
Imports System

Class C
    Sub $$M()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    &lt;Serializable()&gt;
    Sub M()
    End Sub
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_Function() As Task
            Dim code =
<Code>
Imports System

Class C
    Function $$M() As integer
    End Function
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    &lt;Serializable()&gt;
    Function M() As integer
    End Function
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_Sub_MustOverride() As Task
            Dim code =
<Code>
Imports System

MustInherit Class C
    MustOverride Sub $$M()
End Class
</Code>

            Dim expected =
<Code>
Imports System

MustInherit Class C
    &lt;Serializable()&gt;
    MustOverride Sub M()
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_Function_MustOverride() As Task
            Dim code =
<Code>
Imports System

MustInherit Class C
    MustOverride Function $$M() As integer
End Class
</Code>

            Dim expected =
<Code>
Imports System

MustInherit Class C
    &lt;Serializable()&gt;
    MustOverride Function M() As integer
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_DeclareSub() As Task
            Dim code =
<Code>
Imports System

Class C
    Declare Sub $$M() Lib "MyDll.dll"
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    &lt;Serializable()&gt;
    Declare Sub M() Lib "MyDll.dll"
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_DeclareFunction() As Task
            Dim code =
<Code>
Imports System

Class C
    Declare Function $$M() Lib "MyDll.dll" As Integer
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    &lt;Serializable()&gt;
    Declare Function M() Lib "MyDll.dll" As Integer
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_Constructor() As Task
            Dim code =
<Code>
Imports System

Class C
    Sub $$New()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    &lt;Serializable()&gt;
    Sub New()
    End Sub
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_Operator() As Task
            Dim code =
<Code>
Imports System

Class C
    Public Shared Operator $$+(x As C, y As C) As C
    End Operator
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    &lt;Serializable()&gt;
    Public Shared Operator +(x As C, y As C) As C
    End Operator
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_Conversion() As Task
            Dim code =
<Code>
Imports System

Class C
    Public Shared Operator Widening $$CType(x As Integer) As C
    End Operator
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    &lt;Serializable()&gt;
    Public Shared Operator Widening CType(x As Integer) As C
    End Operator
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_Sub_BelowDocComment() As Task
            Dim code =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    Sub $$M()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    &lt;Serializable()&gt;
    Sub M()
    End Sub
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_Function_BelowDocComment() As Task
            Dim code =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    Function $$M() As integer
    End Function
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    &lt;Serializable()&gt;
    Function M() As integer
    End Function
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_Sub_MustOverride_BelowDocComment() As Task
            Dim code =
<Code>
Imports System

MustInherit Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    MustOverride Sub $$M()
End Class
</Code>

            Dim expected =
<Code>
Imports System

MustInherit Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    &lt;Serializable()&gt;
    MustOverride Sub M()
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_Function_MustOverride_BelowDocComment() As Task
            Dim code =
<Code>
Imports System

MustInherit Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    MustOverride Function $$M() As integer
End Class
</Code>

            Dim expected =
<Code>
Imports System

MustInherit Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    &lt;Serializable()&gt;
    MustOverride Function M() As integer
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_DeclareSub_BelowDocComment() As Task
            Dim code =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    Declare Sub $$M() Lib "MyDll.dll"
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    &lt;Serializable()&gt;
    Declare Sub M() Lib "MyDll.dll"
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_DeclareFunction_BelowDocComment() As Task
            Dim code =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    Declare Function $$M() Lib "MyDll.dll" As Integer
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    &lt;Serializable()&gt;
    Declare Function M() Lib "MyDll.dll" As Integer
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_Constructor_BelowDocComment() As Task
            Dim code =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    Sub $$New()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    &lt;Serializable()&gt;
    Sub New()
    End Sub
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_Operator_BelowDocComment() As Task
            Dim code =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    Public Shared Operator $$+(x As C, y As C) As C
    End Operator
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    &lt;Serializable()&gt;
    Public Shared Operator +(x As C, y As C) As C
    End Operator
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_Conversion_BelowDocComment() As Task
            Dim code =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    Public Shared Operator Widening $$CType(x As Integer) As C
    End Operator
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    &lt;Serializable()&gt;
    Public Shared Operator Widening CType(x As Integer) As C
    End Operator
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

#End Region

#Region "AddParameter tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter1() As Task
            Dim code =
<Code>
Class C
    Sub $$M()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(a As Integer)
    End Sub
End Class
</Code>

            Await TestAddParameter(code, expected, New ParameterData With {.Name = "a", .Type = "Integer"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter2() As Task
            Dim code =
<Code>
Class C
    Sub $$M(a As Integer)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(b As String, a As Integer)
    End Sub
End Class
</Code>

            Await TestAddParameter(code, expected, New ParameterData With {.Name = "b", .Type = "String"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter3() As Task
            Dim code =
<Code>
Class C
    Sub $$M(a As Integer, b As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(a As Integer, c As Boolean, b As String)
    End Sub
End Class
</Code>

            Await TestAddParameter(code, expected, New ParameterData With {.Name = "c", .Type = "System.Boolean", .Position = 1})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter4() As Task
            Dim code =
<Code>
Class C
    Sub $$M(a As Integer)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(a As Integer, b As String)
    End Sub
End Class
</Code>

            Await TestAddParameter(code, expected, New ParameterData With {.Name = "b", .Type = "String", .Position = -1})
        End Function

        <WorkItem(1873, "https://github.com/dotnet/roslyn/issues/1873")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter_DeclareFunction() As Task
            Dim code =
<Code>
Public Class C1
    Declare Function $$getUserName Lib "My1.dll" (a As Integer) As String
End Class
</Code>
            Dim expected =
<Code>
Public Class C1
    Declare Function getUserName Lib "My1.dll" (a As Integer, b As String) As String
End Class
</Code>
            Await TestAddParameter(code, expected, New ParameterData With {.Name = "b", .Type = "String", .Position = -1})
        End Function

        <WorkItem(1873, "https://github.com/dotnet/roslyn/issues/1873")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter_DeclareSub() As Task
            Dim code =
<Code>
Public Class C1
    Declare Sub $$getUserName Lib "My1.dll" (a As Integer)
End Class
</Code>
            Dim expected =
<Code>
Public Class C1
    Declare Sub getUserName Lib "My1.dll" (a As Integer, b As String)
End Class
</Code>
            Await TestAddParameter(code, expected, New ParameterData With {.Name = "b", .Type = "String", .Position = -1})
        End Function

#End Region

#Region "RemoveParameter tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveParameter1() As Task
            Dim code =
<Code>
Class C
    Sub $$M(a As Integer)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M()
    End Sub
End Class
</Code>

            Await TestRemoveChild(code, expected, "a")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveParameter2() As Task
            Dim code =
<Code>
Class C
    Sub $$M(a As Integer, b As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(a As Integer)
    End Sub
End Class
</Code>

            Await TestRemoveChild(code, expected, "b")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveParameter3() As Task
            Dim code =
<Code>
Class C
    Sub $$M(a As Integer, b As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(b As String)
    End Sub
End Class
</Code>

            Await TestRemoveChild(code, expected, "a")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveParameter4() As Task
            Dim code =
<Code>
Class C
    Sub $$M(a As Integer, b As String, c As Integer)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(a As Integer, c As Integer)
    End Sub
End Class
</Code>

            Await TestRemoveChild(code, expected, "b")
        End Function

#End Region

#Region "Set Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess1() As Task
            Dim code =
<Code>
Class C
    Function $$Goo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Function Goo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess2() As Task
            Dim code =
<Code>
Class C
    Public Function $$Goo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Friend Function Goo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess3() As Task
            Dim code =
<Code>
Class C
    Protected Friend Function $$Goo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Function Goo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess4() As Task
            Dim code =
<Code>
Class C
    Public Function $$Goo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Protected Friend Function Goo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess5() As Task
            Dim code =
<Code>
Class C
    Public Function $$Goo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Function Goo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess6() As Task
            Dim code =
<Code>
Interface C
    Function $$Goo() As Integer
End Class
</Code>

            Dim expected =
<Code>
Interface C
    Function Goo() As Integer
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess7() As Task
            Dim code =
<Code>
Interface C
    Function $$Goo() As Integer
End Class
</Code>

            Dim expected =
<Code>
Interface C
    Function Goo() As Integer
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

#End Region

#Region "Set CanOverride tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride1() As Task
            Dim code =
<Code>
MustInherit Class C
    Overridable Sub $$Goo()

    End Sub
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    Overridable Sub Goo()

    End Sub
End Class
</Code>

            Await TestSetCanOverride(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride2() As Task
            Dim code =
<Code>
MustInherit Class C
    Overridable Sub $$Goo()

    End Sub
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    Sub Goo()

    End Sub
End Class
</Code>

            Await TestSetCanOverride(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride3() As Task
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$Goo()
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    Overridable Sub Goo()

    End Sub
End Class
</Code>

            Await TestSetCanOverride(code, expected, True)
        End Function

#End Region

#Region "Set MustImplement tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement1() As Task
            Dim code =
<Code>
MustInherit Class C
    Overridable Sub $$Goo()

    End Sub
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    MustOverride Sub Goo()
End Class
</Code>

            Await TestSetMustImplement(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement2() As Task
            Dim code =
<Code>
MustInherit Class C
    Overridable Sub $$Goo()

    End Sub
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    Sub Goo()

    End Sub
End Class
</Code>

            Await TestSetMustImplement(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement3() As Task
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$Goo()
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    MustOverride Sub Goo()
End Class
</Code>

            Await TestSetMustImplement(code, expected, True)
        End Function

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared1() As Task
            Dim code =
<Code>
Class C
    Function $$Goo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Shared Function Goo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Await TestSetIsShared(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared2() As Task
            Dim code =
<Code>
Class C
    Shared Function $$Goo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Function Goo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Await TestSetIsShared(code, expected, False)
        End Function

#End Region

#Region "Set Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code>
Class C
    Sub $$Goo()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub Bar()
    End Sub
End Class
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

#End Region

#Region "Set OverrideKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind1() As Task
            Dim code =
<Code>
MustInherit Class C
    Sub $$Goo()

    End Sub
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    Sub Goo()

    End Sub
End Class
</Code>

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind2() As Task
            Dim code =
<Code>
MustInherit Class C
    Sub $$Goo()

    End Sub
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    MustOverride Sub Goo()
End Class
</Code>

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind3() As Task
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$Goo()
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    Sub Goo()

    End Sub
End Class
</Code>

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Function

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
            Dim code =
<Code>
Class C
    Sub $$Goo()
        Dim i As Integer
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub Goo()
        Dim i As Integer
    End Sub
End Class
</Code>

            Await TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType2() As Task
            Dim code =
<Code>
Class C
    Sub $$Goo()
        Dim i As Integer
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Function Goo() As Integer
        Dim i As Integer
    End Function
End Class
</Code>

            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType3() As Task
            Dim code =
<Code>
Class C
    Function $$Goo() As System.Int32
        Dim i As Integer
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Function Goo() As String
        Dim i As Integer
    End Function
End Class
</Code>

            Await TestSetTypeProp(code, expected, "System.String")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType4() As Task
            Dim code =
<Code>
Class C
    Function $$Goo() As System.Int32
        Dim i As Integer
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub Goo()
        Dim i As Integer
    End Sub
End Class
</Code>

            Await TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType5() As Task
            Dim code =
<Code>
MustInherit Class C
    MustOverride Function $$Goo() As System.Int32
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    MustOverride Sub Goo()
End Class
</Code>

            Await TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType6() As Task
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$Goo()
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    MustOverride Function Goo() As Integer
End Class
</Code>

            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType7() As Task
            Dim code =
<Code>
Class C
    Sub $$New()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub New()
    End Sub
End Class
</Code>

            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType8() As Task
            Dim code =
<Code>
Class C
    Shared Narrowing Operator $$CType(i As Integer) As C
    End Operator
End Class
</Code>

            Dim expected =
<Code>
Class C
    Shared Narrowing Operator CType(i As Integer) As C
    End Operator
End Class
</Code>

            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

        <WorkItem(1873, "https://github.com/dotnet/roslyn/issues/1873")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType_DeclareFunction() As Task
            Dim code =
<Code>
Public Class C1
    Declare Function $$getUserName Lib "My1.dll" (a As Integer) As String
End Class
</Code>
            Dim expected =
<Code>
Public Class C1
    Declare Function getUserName Lib "My1.dll" (a As Integer) As Integer
End Class
</Code>
            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

        <WorkItem(1873, "https://github.com/dotnet/roslyn/issues/1873")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType_DeclareFunctionToSub() As Task
            Dim code =
            <Code>
Public Class C1
    Declare Function $$getUserName Lib "My1.dll" (a As Integer) As String
End Class
</Code>
            Dim expected =
<Code>
Public Class C1
    Declare Sub getUserName Lib "My1.dll" (a As Integer)
End Class
</Code>

            Await TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Function

        <WorkItem(1873, "https://github.com/dotnet/roslyn/issues/1873")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType_DeclareSubToFunction() As Task
            Dim code =
<Code>
Public Class C1
    Declare Sub $$getUserName Lib "My1.dll" (a As Integer)
End Class
</Code>
            Dim expected =
            <Code>
Public Class C1
    Declare Function getUserName Lib "My1.dll" (a As Integer) As Integer
End Class
</Code>


            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

#End Region

#Region "PartialMethodExtender"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialMethodExtender_IsPartial1()
            Dim code =
<Code>
Partial Public Class Class2
    Public Sub $$M(i As Integer)
    End Sub

    Partial Private Sub M()
    End Sub

    Private Sub M()
    End Sub
End Class
</Code>

            TestPartialMethodExtender_IsPartial(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialMethodExtender_IsPartial2()
            Dim code =
<Code>
Partial Public Class Class2
    Public Sub M(i As Integer)
    End Sub

    Partial Private Sub $$M()
    End Sub

    Private Sub M()
    End Sub
End Class
</Code>

            TestPartialMethodExtender_IsPartial(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialMethodExtender_IsPartial3()
            Dim code =
<Code>
Partial Public Class Class2
    Public Sub M(i As Integer)
    End Sub

    Partial Private Sub M()
    End Sub

    Private Sub $$M()
    End Sub
End Class
</Code>

            TestPartialMethodExtender_IsPartial(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialMethodExtender_IsDeclaration1()
            Dim code =
<Code>
Partial Public Class Class2
    Public Sub $$M(i As Integer)
    End Sub

    Partial Private Sub M()
    End Sub

    Private Sub M()
    End Sub
End Class
</Code>

            TestPartialMethodExtender_IsDeclaration(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialMethodExtender_IsDeclaration2()
            Dim code =
<Code>
Partial Public Class Class2
    Public Sub M(i As Integer)
    End Sub

    Partial Private Sub $$M()
    End Sub

    Private Sub M()
    End Sub
End Class
</Code>

            TestPartialMethodExtender_IsDeclaration(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialMethodExtender_IsDeclaration3()
            Dim code =
<Code>
Partial Public Class Class2
    Public Sub M(i As Integer)
    End Sub

    Partial Private Sub M()
    End Sub

    Private Sub $$M()
    End Sub
End Class
</Code>

            TestPartialMethodExtender_IsDeclaration(code, False)
        End Sub

#End Region

#Region "Overloads Tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsOverloaded1()
            Dim code =
<Code>
Class C
    Sub $$Goo(x As C)
    End Sub
End Class
</Code>
            TestIsOverloaded(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsOverloaded2()
            Dim code =
<Code>
Class C
    Sub Goo()
    End Sub

    Sub $$Goo(x As C)
    End Sub
End Class
</Code>
            TestIsOverloaded(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverloads1()
            Dim code =
<Code>
Class C
    Sub $$Goo()
    End Sub

    Sub Goo(x As C)
    End Sub
End Class

</Code>
            TestOverloadsUniqueSignatures(code, "M:C.Goo", "M:C.Goo(C)")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverloads2()
            Dim code =
<Code>
Class C
    Sub $$Goo()
    End Sub
End Class

</Code>
            TestOverloadsUniqueSignatures(code, "M:C.Goo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverloads3()
            Dim code =
<Code>
Class C
    Shared Operator $$*(i As Integer, c As C) As C
    End Operator
End Class
</Code>
            TestOverloadsUniqueSignatures(code, "M:C.op_Multiply(System.Int32,C)")
        End Sub

#End Region

#Region "Parameter name tests"

        <WorkItem(1147885, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1147885")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterNameWithEscapeCharacters()
            Dim code =
<Code>
Class C
    Sub $$M1([integer] As Integer)
    End Sub
End Class
</Code>
            TestAllParameterNames(code, "[integer]")
        End Sub

        <WorkItem(1147885, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1147885")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterNameWithEscapeCharacters_2()
            Dim code =
<Code>
Class C
    Sub $$M1([integer] As Integer, [string] as String)
    End Sub
End Class
</Code>
            TestAllParameterNames(code, "[integer]", "[string]")
        End Sub

#End Region

        Private Function GetPartialMethodExtender(codeElement As EnvDTE80.CodeFunction2) As IVBPartialMethodExtender
            Return CType(codeElement.Extender(ExtenderNames.VBPartialMethodExtender), IVBPartialMethodExtender)
        End Function

        Protected Overrides Function PartialMethodExtender_GetIsPartial(codeElement As EnvDTE80.CodeFunction2) As Boolean
            Return GetPartialMethodExtender(codeElement).IsPartial
        End Function

        Protected Overrides Function PartialMethodExtender_GetIsDeclaration(codeElement As EnvDTE80.CodeFunction2) As Boolean
            Return GetPartialMethodExtender(codeElement).IsDeclaration
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
