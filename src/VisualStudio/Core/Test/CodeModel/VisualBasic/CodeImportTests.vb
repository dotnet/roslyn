' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeImportTests
        Inherits AbstractCodeImportTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint1() As Task
            Dim code =
<Code>
Imports $$System
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=14)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint2() As Task
            Dim code =
<Code>
Imports System, $$System.Linq
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=27)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint3() As Task
            Dim code =
<Code>
Imports System, $$S = System.Linq
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=31)))
        End Function

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint1() As Task
            Dim code =
<Code>
Imports $$System
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=15, absoluteOffset:=15, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=15, absoluteOffset:=15, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=15, absoluteOffset:=15, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=15, absoluteOffset:=15, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=15, absoluteOffset:=15, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=15, absoluteOffset:=15, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=15, absoluteOffset:=15, lineLength:=14)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint2() As Task
            Dim code =
<Code>
Imports System, $$System.Linq
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=28, absoluteOffset:=28, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=28, absoluteOffset:=28, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=28, absoluteOffset:=28, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=28, absoluteOffset:=28, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=28, absoluteOffset:=28, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=28, absoluteOffset:=28, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=28, absoluteOffset:=28, lineLength:=27)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint3() As Task
            Dim code =
<Code>
Imports System, $$S = System.Linq
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=32, absoluteOffset:=32, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=32, absoluteOffset:=32, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=32, absoluteOffset:=32, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=32, absoluteOffset:=32, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=32, absoluteOffset:=32, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=32, absoluteOffset:=32, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=32, absoluteOffset:=32, lineLength:=31)))
        End Function

#End Region

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName1() As Task
            Dim code =
<Code>
Imports $$System
</Code>

            Await Assert.ThrowsAsync(Of NotImplementedException)(
                Async Function()
                    Await TestFullName(code, "System")
                End Function)
        End Function

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName1() As Task
            Dim code =
<Code>
Imports $$Foo
</Code>

            Await TestName(code, "Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName2() As Task
            Dim code =
<Code>
Imports System$$, System.Linq
</Code>

            Await TestName(code, "System")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName3() As Task
            Dim code =
<Code>
Imports System, System.Linq$$
</Code>

            Await TestName(code, "System.Linq")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName4() As Task
            Dim code =
<Code>
Imports System _
    .
        Linq$$
</Code>

            Await TestName(code, "System.Linq")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName5() As Task
            Dim code =
<Code>
Imports System, S = System.Linq$$
</Code>

            Await TestName(code, "System.Linq")
        End Function

#End Region

#Region "Namespace tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestNamespace1() As Task
            Dim code =
<Code>
Imports $$Foo
</Code>

            Await TestNamespace(code, "Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestNamespace2() As Task
            Dim code =
<Code>
Imports System _
    .
        Linq$$
</Code>

            Await TestNamespace(code, "System.Linq")
        End Function

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
