' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeFunctionTests
        Inherits AbstractCodeFunctionTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint_MustOverride1()
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
        Public Sub GetStartPoint_MustOverride2()
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
        Public Sub GetStartPoint_DeclareFunction_WithoutAttribute()
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
        Public Sub GetStartPoint_DeclareFunction_WithAttribute()
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
        Public Sub GetStartPoint_DeclareSub_WithoutAttribute()
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
        Public Sub GetStartPoint_DeclareSub_WithAttribute()
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
        Public Sub GetEndPoint_MustOverride1()
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
        Public Sub GetEndPoint_MustOverride2()
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
        Public Sub GetEndPoint_DeclareFunction_WithoutAttribute()
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
        Public Sub GetEndPoint_DeclareFunction_WithAttribute()
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
        Public Sub GetEndPoint_DeclareSub_WithoutAttribute()
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
        Public Sub GetEndPoint_DeclareSub_WithAttribute()
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
        Public Sub Access1()
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
        Public Sub Access2()
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
        Public Sub Access3()
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
        Public Sub Access4()
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
        Public Sub Access5()
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
        Public Sub Access6()
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
        Public Sub Access7()
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
        Public Sub PropertyGetAttribute_WithNoSet()
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
        Public Sub PropertySetAttribute_WithNoGet()
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
        Public Sub PropertySetAttribute_WithGet()
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
        Public Sub PropertyGetAttribute_WithSet()
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
        Public Sub Attribute_1()
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
        Public Sub CanOverride1()
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$Foo()
End Class
</Code>

            TestCanOverride(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub CanOverride2()
            Dim code =
<Code>
Interface I
    Sub $$Foo()
End Interface
</Code>

            TestCanOverride(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub CanOverride3()
            Dim code =
<Code>
Class C
    Protected Overridable Sub $$Foo()

    End Sub
End Class
</Code>

            TestCanOverride(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub CanOverride4()
            Dim code =
<Code>
Class C
    Protected Sub $$Foo()

    End Sub
End Class
</Code>

            TestCanOverride(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub CanOverride5()
            Dim code =
<Code>
Class B
    Protected Overridable Sub Foo()

    End Sub
End Class

Class C
    Inherits B

    Protected Overrides Sub $$Foo()

    End Sub
End Class
</Code>

            TestCanOverride(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub CanOverride6()
            Dim code =
<Code>
Class B
    Protected Overridable Sub Foo()

    End Sub
End Class

Class C
    Inherits B

    Protected NotOverridable Overrides Sub $$Foo()

    End Sub
End Class
</Code>

            TestCanOverride(code, False)
        End Sub

#End Region

#Region "FunctionKind tests"

        <WorkItem(1843, "https://github.com/dotnet/roslyn/issues/1843")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FunctionKind_Constructor()
            Dim code =
<Code>
Public Class C1

   Public Sub $$New()
   End Sub

End Clas
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionConstructor)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FunctionKind_Destructor()
            Dim code =
<Code>
Public Class C1

   Protected Overrides Sub $$Finalize()
      MyBase.Finalize()
   End Sub

End Clas
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionDestructor)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FunctionKind_Sub()
            Dim code =
<Code>
Public Class C1

   Private Sub $$M()
   End Sub

End Clas
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionSub)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FunctionKind_Function()
            Dim code =
<Code>
Public Class C1

   Private Function $$M() As Integer
   End Sub

End Clas
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionFunction)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FunctionKind_DeclareSub()
            Dim code =
<Code>
Public Class C1

   Private Declare Sub $$MethodB Lib "MyDll.dll" ()

End Clas
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionSub)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FunctionKind_DeclareFunction()
            Dim code =
<Code>
Public Class C1

   Private Declare Function $$MethodC Lib "MyDll.dll" () As Integer

End Clas
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionFunction)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FunctionKind__Operator()
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
        Public Sub FunctionKind_Conversion()
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
        Public Sub MustImplement1()
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$Foo()
End Class
</Code>

            TestMustImplement(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub MustImplement2()
            Dim code =
<Code>
Interface I
    Sub $$Foo()
End Interface
</Code>

            TestMustImplement(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub MustImplement3()
            Dim code =
<Code>
Class C
    Protected Overridable Sub $$Foo()

    End Sub
End Class
</Code>

            TestMustImplement(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub MustImplement4()
            Dim code =
<Code>
Class C
    Protected Sub $$Foo()

    End Sub
End Class
</Code>

            TestMustImplement(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub MustImplement5()
            Dim code =
<Code>
Class B
    Protected Overridable Sub Foo()

    End Sub
End Class

Class C
    Inherits B

    Protected Overrides Sub $$Foo()

    End Sub
End Class
</Code>

            TestMustImplement(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub MustImplement6()
            Dim code =
<Code>
Class B
    Protected Overridable Sub Foo()

    End Sub
End Class

Class C
    Inherits B

    Protected NotOverridable Overrides Sub $$Foo()

    End Sub
End Class
</Code>

            TestMustImplement(code, False)
        End Sub

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name1()
            Dim code =
<Code>
Class C
    MustOverride Sub $$Foo()
End Class
</Code>

            TestName(code, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name_NoParens()
            Dim code =
<Code>
Class C
    Sub $$Foo
End Class
</Code>

            TestName(code, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name_Constructor1()
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
        Public Sub Name_Constructor2()
            Dim code =
<Code>
Class C
    Sub $$New()
End Class
</Code>

            TestName(code, "New")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name_Operator1()
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
        Public Sub Name_Operator2()
            Dim code =
<Code>
Class C
    Shared Narrowing Operator $$CType(i As Integer) As C
End Class
</Code>

            TestName(code, "CType")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name_Operator3()
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
        Public Sub DeclareSubKind()
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
        Public Sub DeclareFunctionKind()
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
        Public Sub SubKind()
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
        Public Sub OverrideKind_Abstract()
            Dim code =
<Code>
MustInherit Class C
    Protected MustOverride Sub $$Foo()
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub OverrideKind_Virtual()
            Dim code =
<Code>
Class C
    Protected Overridable Sub $$Foo()
    End Sub
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub OverrideKind_Sealed()
            Dim code =
<Code>
Class C
    Protected NotOverridable Sub $$Foo()
    End Sub
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub OverrideKind_Override()
            Dim code =
<Code>
MustInherit Class B
    Protected MustOverride Sub Foo()
End Class

Class C
    Inherits B

    Protected Overrides Sub $$Foo()
    End Sub
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub OverrideKind_New()
            Dim code =
<Code>
MustInherit Class B
    Protected MustOverride Sub Foo()
End Class

Class C
    Inherits B

    Protected Shadows Sub $$Foo()
    End Sub
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNew)
        End Sub

#End Region

#Region "Prototype tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_UniqueSignature()
            Dim code =
<Code>
Namespace N
    Class C
        Sub $$Foo()
        End Sub
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature, "M:N.C.Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_FullName()
            Dim code =
<Code>
Namespace N
    Class C
        Sub $$Foo()
        End Sub
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "N.C.Foo()")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName()
            Dim code =
<Code>
Namespace N
    Class C
        Sub $$Foo()
        End Sub
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.Foo()")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_Type1()
            Dim code =
<Code>
Namespace N
    Class C
        Sub $$Foo()
        End Sub
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "Foo()")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_Type2()
            Dim code =
<Code>
Namespace N
    Class C
        Function $$Foo() As Integer
        End Function
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "Foo() As Integer")
        End Sub

#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type1()
            Dim code =
<Code>
Class C
    Sub $$Foo()
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
        Public Sub Type2()
            Dim code =
<Code>
Class C
    Function $$Foo$()
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
        Public Sub Type3()
            Dim code =
<Code>
Class C
    Function $$Foo() As String
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
        Public Sub Type4()
            Dim code =
<Code>
MustInherit Class C
    MustOverride Function $$Foo() As String
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
        Public Sub AddAttribute_Sub()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_Function()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_Sub_MustOverride()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_Function_MustOverride()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_DeclareSub()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_DeclareFunction()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_Constructor()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_Operator()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_Conversion()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_Sub_BelowDocComment()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_Function_BelowDocComment()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_Sub_MustOverride_BelowDocComment()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_Function_MustOverride_BelowDocComment()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_DeclareSub_BelowDocComment()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_DeclareFunction_BelowDocComment()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_Constructor_BelowDocComment()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_Operator_BelowDocComment()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_Conversion_BelowDocComment()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

#End Region

#Region "AddParameter tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddParameter1()
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

            TestAddParameter(code, expected, New ParameterData With {.Name = "a", .Type = "Integer"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddParameter2()
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

            TestAddParameter(code, expected, New ParameterData With {.Name = "b", .Type = "String"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddParameter3()
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

            TestAddParameter(code, expected, New ParameterData With {.Name = "c", .Type = "System.Boolean", .Position = 1})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddParameter4()
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

            TestAddParameter(code, expected, New ParameterData With {.Name = "b", .Type = "String", .Position = -1})
        End Sub

        <WorkItem(1873, "https://github.com/dotnet/roslyn/issues/1873")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddParameter_DeclareFunction()
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
            TestAddParameter(code, expected, New ParameterData With {.Name = "b", .Type = "String", .Position = -1})
        End Sub

        <WorkItem(1873, "https://github.com/dotnet/roslyn/issues/1873")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddParameter_DeclareSub()
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
            TestAddParameter(code, expected, New ParameterData With {.Name = "b", .Type = "String", .Position = -1})
        End Sub

#End Region

#Region "RemoveParameter tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveParameter1()
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

            TestRemoveChild(code, expected, "a")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveParameter2()
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

            TestRemoveChild(code, expected, "b")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveParameter3()
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

            TestRemoveChild(code, expected, "a")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveParameter4()
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

            TestRemoveChild(code, expected, "b")
        End Sub

#End Region

#Region "Set Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess1()
            Dim code =
<Code>
Class C
    Function $$Foo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Function Foo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess2()
            Dim code =
<Code>
Class C
    Public Function $$Foo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Friend Function Foo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess3()
            Dim code =
<Code>
Class C
    Protected Friend Function $$Foo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Function Foo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess4()
            Dim code =
<Code>
Class C
    Public Function $$Foo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Protected Friend Function Foo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess5()
            Dim code =
<Code>
Class C
    Public Function $$Foo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Function Foo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess6()
            Dim code =
<Code>
Interface C
    Function $$Foo() As Integer
End Class
</Code>

            Dim expected =
<Code>
Interface C
    Function Foo() As Integer
End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess7()
            Dim code =
<Code>
Interface C
    Function $$Foo() As Integer
End Class
</Code>

            Dim expected =
<Code>
Interface C
    Function Foo() As Integer
End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Set CanOverride tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetCanOverride1()
            Dim code =
<Code>
MustInherit Class C
    Overridable Sub $$Foo()

    End Sub
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    Overridable Sub Foo()

    End Sub
End Class
</Code>

            TestSetCanOverride(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetCanOverride2()
            Dim code =
<Code>
MustInherit Class C
    Overridable Sub $$Foo()

    End Sub
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    Sub Foo()

    End Sub
End Class
</Code>

            TestSetCanOverride(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetCanOverride3()
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$Foo()
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    Overridable Sub Foo()

    End Sub
End Class
</Code>

            TestSetCanOverride(code, expected, True)
        End Sub

#End Region

#Region "Set MustImplement tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetMustImplement1()
            Dim code =
<Code>
MustInherit Class C
    Overridable Sub $$Foo()

    End Sub
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    MustOverride Sub Foo()
End Class
</Code>

            TestSetMustImplement(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetMustImplement2()
            Dim code =
<Code>
MustInherit Class C
    Overridable Sub $$Foo()

    End Sub
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    Sub Foo()

    End Sub
End Class
</Code>

            TestSetMustImplement(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetMustImplement3()
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$Foo()
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    MustOverride Sub Foo()
End Class
</Code>

            TestSetMustImplement(code, expected, True)
        End Sub

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared1()
            Dim code =
<Code>
Class C
    Function $$Foo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Shared Function Foo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            TestSetIsShared(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared2()
            Dim code =
<Code>
Class C
    Shared Function $$Foo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Function Foo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            TestSetIsShared(code, expected, False)
        End Sub

#End Region

#Region "Set Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName1()
            Dim code =
<Code>
Class C
    Sub $$Foo()
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

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub

#End Region

#Region "Set OverrideKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetOverrideKind1()
            Dim code =
<Code>
MustInherit Class C
    Sub $$Foo()

    End Sub
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    Sub Foo()

    End Sub
End Class
</Code>

            TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetOverrideKind2()
            Dim code =
<Code>
MustInherit Class C
    Sub $$Foo()

    End Sub
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    MustOverride Sub Foo()
End Class
</Code>

            TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetOverrideKind3()
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$Foo()
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    Sub Foo()

    End Sub
End Class
</Code>

            TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Sub

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType1()
            Dim code =
<Code>
Class C
    Sub $$Foo()
        Dim i As Integer
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub Foo()
        Dim i As Integer
    End Sub
End Class
</Code>

            TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType2()
            Dim code =
<Code>
Class C
    Sub $$Foo()
        Dim i As Integer
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Function Foo() As Integer
        Dim i As Integer
    End Function
End Class
</Code>

            TestSetTypeProp(code, expected, "System.Int32")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType3()
            Dim code =
<Code>
Class C
    Function $$Foo() As System.Int32
        Dim i As Integer
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Function Foo() As String
        Dim i As Integer
    End Function
End Class
</Code>

            TestSetTypeProp(code, expected, "System.String")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType4()
            Dim code =
<Code>
Class C
    Function $$Foo() As System.Int32
        Dim i As Integer
    End Function
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub Foo()
        Dim i As Integer
    End Sub
End Class
</Code>

            TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType5()
            Dim code =
<Code>
MustInherit Class C
    MustOverride Function $$Foo() As System.Int32
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    MustOverride Sub Foo()
End Class
</Code>

            TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType6()
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$Foo()
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
    MustOverride Function Foo() As Integer
End Class
</Code>

            TestSetTypeProp(code, expected, "System.Int32")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType7()
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

            TestSetTypeProp(code, expected, "System.Int32")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType8()
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

            TestSetTypeProp(code, expected, "System.Int32")
        End Sub

        <WorkItem(1873, "https://github.com/dotnet/roslyn/issues/1873")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType_DeclareFunction()
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
            TestSetTypeProp(code, expected, "System.Int32")
        End Sub

        <WorkItem(1873, "https://github.com/dotnet/roslyn/issues/1873")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType_DeclareFunctionToSub()
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

            TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Sub

        <WorkItem(1873, "https://github.com/dotnet/roslyn/issues/1873")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType_DeclareSubToFunction()
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


            TestSetTypeProp(code, expected, "System.Int32")
        End Sub

#End Region

#Region "PartialMethodExtender"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub PartialMethodExtender_IsPartial1()
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
        Public Sub PartialMethodExtender_IsPartial2()
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
        Public Sub PartialMethodExtender_IsPartial3()
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
        Public Sub PartialMethodExtender_IsDeclaration1()
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
        Public Sub PartialMethodExtender_IsDeclaration2()
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
        Public Sub PartialMethodExtender_IsDeclaration3()
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
        Public Sub IsOverloaded1()
            Dim code =
<Code>
Class C
    Sub $$Foo(x As C)
    End Sub
End Class
</Code>
            TestIsOverloaded(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsOverloaded2()
            Dim code =
<Code>
Class C
    Sub Foo()
    End Sub

    Sub $$Foo(x As C)
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
    Sub $$Foo()
    End Sub

    Sub Foo(x As C)
    End Sub
End Class

</Code>
            TestOverloadsUniqueSignatures(code, "M:C.Foo", "M:C.Foo(C)")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverloads2()
            Dim code =
<Code>
Class C
    Sub $$Foo()
    End Sub
End Class

</Code>
            TestOverloadsUniqueSignatures(code, "M:C.Foo")
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

        <WorkItem(1147885)>
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

        <WorkItem(1147885)>
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
