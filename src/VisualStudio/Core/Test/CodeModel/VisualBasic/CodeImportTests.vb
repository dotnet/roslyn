' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeImportTests
        Inherits AbstractCodeImportTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint1()
            Dim code =
<Code>
Imports $$System
</Code>

            TestGetStartPoint(code,
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint2()
            Dim code =
<Code>
Imports System, $$System.Linq
</Code>

            TestGetStartPoint(code,
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint3()
            Dim code =
<Code>
Imports System, $$S = System.Linq
</Code>

            TestGetStartPoint(code,
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
        End Sub

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint1()
            Dim code =
<Code>
Imports $$System
</Code>

            TestGetEndPoint(code,
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint2()
            Dim code =
<Code>
Imports System, $$System.Linq
</Code>

            TestGetEndPoint(code,
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint3()
            Dim code =
<Code>
Imports System, $$S = System.Linq
</Code>

            TestGetEndPoint(code,
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
        End Sub

#End Region

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FullName1()
            Dim code =
<Code>
Imports $$System
</Code>

            Assert.Throws(Of NotImplementedException)(
                Sub()
                    TestFullName(code, "System")
                End Sub)
        End Sub

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name1()
            Dim code =
<Code>
Imports $$Foo
</Code>

            TestName(code, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name2()
            Dim code =
<Code>
Imports System$$, System.Linq
</Code>

            TestName(code, "System")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name3()
            Dim code =
<Code>
Imports System, System.Linq$$
</Code>

            TestName(code, "System.Linq")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name4()
            Dim code =
<Code>
Imports System _
    .
        Linq$$
</Code>

            TestName(code, "System.Linq")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name5()
            Dim code =
<Code>
Imports System, S = System.Linq$$
</Code>

            TestName(code, "System.Linq")
        End Sub

#End Region

#Region "Namespace tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Namespace1()
            Dim code =
<Code>
Imports $$Foo
</Code>

            TestNamespace(code, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Namespace2()
            Dim code =
<Code>
Imports System _
    .
        Linq$$
</Code>

            TestNamespace(code, "System.Linq")
        End Sub

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
