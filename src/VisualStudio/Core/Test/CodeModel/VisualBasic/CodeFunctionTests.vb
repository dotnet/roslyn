' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeFunctionTests
        Inherits AbstractCodeFunctionTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_MustOverride1() As Task
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$M()
End Class
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_MustOverride2() As Task
            Dim code =
<Code>
MustInherit Class C
    &lt;System.CLSCompliant(True)&gt;
    MustOverride Sub $$M()
End Class
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <WorkItem(1839, "https://github.com/dotnet/roslyn/issues/1839")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_DeclareFunction_WithoutAttribute() As Task
            Dim code =
<Code>
Public Class C1
    Declare Function $$getUserName Lib "My1.dll" () As String
End Class
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <WorkItem(1839, "https://github.com/dotnet/roslyn/issues/1839")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_DeclareFunction_WithAttribute() As Task
            Dim code =
<Code>
Public Class C1
    &lt;System.CLSCompliant(True)&gt;
    Declare Function $$getUserName Lib "My1.dll" () As String
End Class
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <WorkItem(1839, "https://github.com/dotnet/roslyn/issues/1839")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_DeclareSub_WithoutAttribute() As Task
            Dim code =
<Code>
Public Class C1
    Public Declare Sub $$MethodName Lib "My1.dll"
End Class
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <WorkItem(1839, "https://github.com/dotnet/roslyn/issues/1839")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_DeclareSub_WithAttribute() As Task
            Dim code =
<Code>
Public Class C1
    &lt;System.CLSCompliant(True)&gt;
    Public Declare Sub $$MethodName Lib "My1.dll"
End Class
</Code>

            Await TestGetStartPoint(code,
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
        End Function

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_MustOverride1() As Task
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$M()
End Class
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_MustOverride2() As Task
            Dim code =
<Code>
MustInherit Class C
    &lt;System.CLSCompliant(True)&gt;
    MustOverride Sub $$M()
End Class
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <WorkItem(1839, "https://github.com/dotnet/roslyn/issues/1839")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_DeclareFunction_WithoutAttribute() As Task
            Dim code =
<Code>
Public Class C1
    Declare Function $$getUserName Lib "My1.dll" () As String
End Class
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <WorkItem(1839, "https://github.com/dotnet/roslyn/issues/1839")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_DeclareFunction_WithAttribute() As Task
            Dim code =
<Code>
Public Class C1
    &lt;System.CLSCompliant(True)&gt;
    Declare Function $$getUserName Lib "My1.dll" () As String
End Class
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <WorkItem(1839, "https://github.com/dotnet/roslyn/issues/1839")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_DeclareSub_WithoutAttribute() As Task
            Dim code =
<Code>
Public Class C1
    Declare Sub $$getUserName Lib "My1.dll" ()
End Class
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <WorkItem(1839, "https://github.com/dotnet/roslyn/issues/1839")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_DeclareSub_WithAttribute() As Task
            Dim code =
<Code>
Public Class C1
    &lt;System.CLSCompliant(True)&gt;
    Declare Sub $$getUserName Lib "My1.dll" ()
End Class
</Code>

            Await TestGetEndPoint(code,
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
        End Function

#End Region

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess1() As Task
            Dim code =
    <Code>
Class C
    Function $$F() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess2() As Task
            Dim code =
    <Code>
Class C
    Private Function $$F() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess3() As Task
            Dim code =
    <Code>
Class C
    Protected Function $$F() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess4() As Task
            Dim code =
    <Code>
Class C
    Protected Friend Function $$F() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess5() As Task
            Dim code =
    <Code>
Class C
    Friend Function $$F() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess6() As Task
            Dim code =
    <Code>
Class C
    Public Function $$F() As Integer
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess7() As Task
            Dim code =
<Code>
Interface I
    Function $$F() As Integer
End Interface
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

#End Region

#Region "Attribute Tests"
        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPropertyGetAttribute_WithNoSet() As Task
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

            Await TestAttributes(code, IsElement("Obsolete"))
        End Function

        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPropertySetAttribute_WithNoGet() As Task
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

            Await TestAttributes(code, IsElement("Obsolete"))
        End Function

        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPropertySetAttribute_WithGet() As Task
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

            Await TestAttributes(code, IsElement("Obsolete"))
        End Function

        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPropertyGetAttribute_WithSet() As Task
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

            Await TestAttributes(code, IsElement("Obsolete"))
        End Function

        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttribute_1() As Task
            Dim code =
<Code>
Class Program
    &lt;Obsolete&gt;
    Sub F$$()

    End Sub
End Class
</Code>

            Await TestAttributes(code, IsElement("Obsolete"))
        End Function
#End Region

#Region "CanOverride tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCanOverride1() As Task
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$Foo()
End Class
</Code>

            Await TestCanOverride(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCanOverride2() As Task
            Dim code =
<Code>
Interface I
    Sub $$Foo()
End Interface
</Code>

            Await TestCanOverride(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCanOverride3() As Task
            Dim code =
<Code>
Class C
    Protected Overridable Sub $$Foo()

    End Sub
End Class
</Code>

            Await TestCanOverride(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCanOverride4() As Task
            Dim code =
<Code>
Class C
    Protected Sub $$Foo()

    End Sub
End Class
</Code>

            Await TestCanOverride(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCanOverride5() As Task
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

            Await TestCanOverride(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCanOverride6() As Task
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

            Await TestCanOverride(code, False)
        End Function

#End Region

#Region "FunctionKind tests"

        <WorkItem(1843, "https://github.com/dotnet/roslyn/issues/1843")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_Constructor() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub $$New()
   End Sub

End Clas
</Code>

            Await TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionConstructor)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_Destructor() As Task
            Dim code =
<Code>
Public Class C1

   Protected Overrides Sub $$Finalize()
      MyBase.Finalize()
   End Sub

End Clas
</Code>

            Await TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionDestructor)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_Sub() As Task
            Dim code =
<Code>
Public Class C1

   Private Sub $$M()
   End Sub

End Clas
</Code>

            Await TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionSub)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_Function() As Task
            Dim code =
<Code>
Public Class C1

   Private Function $$M() As Integer
   End Sub

End Clas
</Code>

            Await TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionFunction)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_DeclareSub() As Task
            Dim code =
<Code>
Public Class C1

   Private Declare Sub $$MethodB Lib "MyDll.dll" ()

End Clas
</Code>

            Await TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionSub)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_DeclareFunction() As Task
            Dim code =
<Code>
Public Class C1

   Private Declare Function $$MethodC Lib "MyDll.dll" () As Integer

End Clas
</Code>

            Await TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionFunction)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind__Operator() As Task
            Dim code =
<Code>
Imports System

Class C
    Public Shared Operator $$+(x As C, y As C) As C
    End Operator
End Class
</Code>

            Await TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionOperator)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_Conversion() As Task
            Dim code =
<Code>
Imports System

Class C
    Public Shared Operator Widening $$CType(x As Integer) As C
    End Operator
End Class
</Code>

            Await TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionOperator)
        End Function

#End Region

#Region "MustImplement tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestMustImplement1() As Task
            Dim code =
<Code>
MustInherit Class C
    MustOverride Sub $$Foo()
End Class
</Code>

            Await TestMustImplement(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestMustImplement2() As Task
            Dim code =
<Code>
Interface I
    Sub $$Foo()
End Interface
</Code>

            Await TestMustImplement(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestMustImplement3() As Task
            Dim code =
<Code>
Class C
    Protected Overridable Sub $$Foo()

    End Sub
End Class
</Code>

            Await TestMustImplement(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestMustImplement4() As Task
            Dim code =
<Code>
Class C
    Protected Sub $$Foo()

    End Sub
End Class
</Code>

            Await TestMustImplement(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestMustImplement5() As Task
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

            Await TestMustImplement(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestMustImplement6() As Task
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

            Await TestMustImplement(code, False)
        End Function

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName1() As Task
            Dim code =
<Code>
Class C
    MustOverride Sub $$Foo()
End Class
</Code>

            Await TestName(code, "Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_NoParens() As Task
            Dim code =
<Code>
Class C
    Sub $$Foo
End Class
</Code>

            Await TestName(code, "Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_Constructor1() As Task
            Dim code =
<Code>
Class C
    Sub $$New()
    End Sub
End Class
</Code>

            Await TestName(code, "New")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_Constructor2() As Task
            Dim code =
<Code>
Class C
    Sub $$New()
End Class
</Code>

            Await TestName(code, "New")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_Operator1() As Task
            Dim code =
<Code>
Class C
    Shared Narrowing Operator $$CType(i As Integer) As C
    End Operator
End Class
</Code>

            Await TestName(code, "CType")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_Operator2() As Task
            Dim code =
<Code>
Class C
    Shared Narrowing Operator $$CType(i As Integer) As C
End Class
</Code>

            Await TestName(code, "CType")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_Operator3() As Task
            Dim code =
<Code>
Class C
    Shared Operator $$*(i As Integer, c As C) As C
    End Operator
End Class
</Code>

            Await TestName(code, "*")
        End Function

#End Region

#Region "Kind tests"

        <WorkItem(2355, "https://github.com/dotnet/roslyn/issues/2355")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDeclareSubKind() As Task
            Dim code =
<Code>
Public Class Class1 
Public Declare Sub $$f1 Lib "MyLib.dll" () 
End Class 
</Code>

            Await TestKind(code, EnvDTE.vsCMElement.vsCMElementDeclareDecl)
        End Function

        <WorkItem(2355, "https://github.com/dotnet/roslyn/issues/2355")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDeclareFunctionKind() As Task
            Dim code =
<Code>
Public Class Class1 
Public Declare Function f2$$ Lib "MyLib.dll" () As Integer 
End Class 
</Code>

            Await TestKind(code, EnvDTE.vsCMElement.vsCMElementDeclareDecl)
        End Function

        <WorkItem(2355, "https://github.com/dotnet/roslyn/issues/2355")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSubKind() As Task
            Dim code =
<Code>
Public Class Class1 
    Public Sub F1$$()
    End Sub
End Class 
</Code>

            Await TestKind(code, EnvDTE.vsCMElement.vsCMElementFunction)
        End Function
#End Region

#Region "OverrideKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Abstract() As Task
            Dim code =
<Code>
MustInherit Class C
    Protected MustOverride Sub $$Foo()
End Class
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Virtual() As Task
            Dim code =
<Code>
Class C
    Protected Overridable Sub $$Foo()
    End Sub
End Class
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Sealed() As Task
            Dim code =
<Code>
Class C
    Protected NotOverridable Sub $$Foo()
    End Sub
End Class
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Override() As Task
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

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_New() As Task
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

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNew)
        End Function

#End Region

#Region "Prototype tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_UniqueSignature() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Sub $$Foo()
        End Sub
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature, "M:N.C.Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_FullName() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Sub $$Foo()
        End Sub
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "N.C.Foo()")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Sub $$Foo()
        End Sub
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.Foo()")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_Type1() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Sub $$Foo()
        End Sub
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "Foo()")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_Type2() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Function $$Foo() As Integer
        End Function
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "Foo() As Integer")
        End Function

#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType1() As Task
            Dim code =
<Code>
Class C
    Sub $$Foo()
    End Sub
End Class
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "Void",
                             .AsFullName = "System.Void",
                             .CodeTypeFullName = "System.Void",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefVoid
                         })
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType2() As Task
            Dim code =
<Code>
Class C
    Function $$Foo$()
    End Function
End Class
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "String",
                             .AsFullName = "System.String",
                             .CodeTypeFullName = "System.String",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefString
                         })
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType3() As Task
            Dim code =
<Code>
Class C
    Function $$Foo() As String
    End Function
End Class
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "String",
                             .AsFullName = "System.String",
                             .CodeTypeFullName = "System.String",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefString
                         })
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType4() As Task
            Dim code =
<Code>
MustInherit Class C
    MustOverride Function $$Foo() As String
End Class
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "String",
                             .AsFullName = "System.String",
                             .CodeTypeFullName = "System.String",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefString
                         })
        End Function

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

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess2() As Task
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

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess3() As Task
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

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess4() As Task
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

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess5() As Task
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

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess6() As Task
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

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess7() As Task
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

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

#End Region

#Region "Set CanOverride tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride1() As Task
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

            Await TestSetCanOverride(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride2() As Task
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

            Await TestSetCanOverride(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride3() As Task
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

            Await TestSetCanOverride(code, expected, True)
        End Function

#End Region

#Region "Set MustImplement tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement1() As Task
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

            Await TestSetMustImplement(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement2() As Task
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

            Await TestSetMustImplement(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement3() As Task
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

            Await TestSetMustImplement(code, expected, True)
        End Function

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared1() As Task
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

            Await TestSetIsShared(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared2() As Task
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

            Await TestSetIsShared(code, expected, False)
        End Function

#End Region

#Region "Set Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
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

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

#End Region

#Region "Set OverrideKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind1() As Task
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

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind2() As Task
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

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind3() As Task
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

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Function

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
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

            Await TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType2() As Task
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

            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType3() As Task
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

            Await TestSetTypeProp(code, expected, "System.String")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType4() As Task
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

            Await TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType5() As Task
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

            Await TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType6() As Task
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
        Public Async Function TestPartialMethodExtender_IsPartial1() As Task
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

            Await TestPartialMethodExtender_IsPartial(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPartialMethodExtender_IsPartial2() As Task
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

            Await TestPartialMethodExtender_IsPartial(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPartialMethodExtender_IsPartial3() As Task
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

            Await TestPartialMethodExtender_IsPartial(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPartialMethodExtender_IsDeclaration1() As Task
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

            Await TestPartialMethodExtender_IsDeclaration(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPartialMethodExtender_IsDeclaration2() As Task
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

            Await TestPartialMethodExtender_IsDeclaration(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPartialMethodExtender_IsDeclaration3() As Task
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

            Await TestPartialMethodExtender_IsDeclaration(code, False)
        End Function

#End Region

#Region "Overloads Tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsOverloaded1() As Task
            Dim code =
<Code>
Class C
    Sub $$Foo(x As C)
    End Sub
End Class
</Code>
            Await TestIsOverloaded(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsOverloaded2() As Task
            Dim code =
<Code>
Class C
    Sub Foo()
    End Sub

    Sub $$Foo(x As C)
    End Sub
End Class
</Code>
            Await TestIsOverloaded(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverloads1() As Task
            Dim code =
<Code>
Class C
    Sub $$Foo()
    End Sub

    Sub Foo(x As C)
    End Sub
End Class

</Code>
            Await TestOverloadsUniqueSignatures(code, "M:C.Foo", "M:C.Foo(C)")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverloads2() As Task
            Dim code =
<Code>
Class C
    Sub $$Foo()
    End Sub
End Class

</Code>
            Await TestOverloadsUniqueSignatures(code, "M:C.Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverloads3() As Task
            Dim code =
<Code>
Class C
    Shared Operator $$*(i As Integer, c As C) As C
    End Operator
End Class
</Code>
            Await TestOverloadsUniqueSignatures(code, "M:C.op_Multiply(System.Int32,C)")
        End Function

#End Region

#Region "Parameter name tests"

        <WorkItem(1147885)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterNameWithEscapeCharacters() As Task
            Dim code =
<Code>
Class C
    Sub $$M1([integer] As Integer)
    End Sub
End Class
</Code>
            Await TestAllParameterNames(code, "[integer]")
        End Function

        <WorkItem(1147885)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterNameWithEscapeCharacters_2() As Task
            Dim code =
<Code>
Class C
    Sub $$M1([integer] As Integer, [string] as String)
    End Sub
End Class
</Code>
            Await TestAllParameterNames(code, "[integer]", "[string]")
        End Function

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
