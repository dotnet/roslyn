' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class ImplementsStatementTests
        Inherits AbstractCodeElementTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint1() As Task
            Dim code =
<Code>
Interface I
End Interface

Class C
    Implements $$I
End Class
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=5, lineOffset:=5, absoluteOffset:=40, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=5, lineOffset:=5, absoluteOffset:=40, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=5, lineOffset:=5, absoluteOffset:=40, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=5, lineOffset:=5, absoluteOffset:=40, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=5, lineOffset:=5, absoluteOffset:=40, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=5, lineOffset:=5, absoluteOffset:=40, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=5, lineOffset:=5, absoluteOffset:=40, lineLength:=16)))
        End Function

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint1() As Task
            Dim code =
<Code>
Interface I
End Interface

Class C
    Implements $$I
End Class
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=5, lineOffset:=17, absoluteOffset:=52, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=5, lineOffset:=17, absoluteOffset:=52, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=5, lineOffset:=17, absoluteOffset:=52, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=5, lineOffset:=17, absoluteOffset:=52, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=5, lineOffset:=17, absoluteOffset:=52, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=5, lineOffset:=17, absoluteOffset:=52, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=5, lineOffset:=17, absoluteOffset:=52, lineLength:=16)))
        End Function

#End Region

#Region "Kind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestKind1() As Task
            Dim code =
<Code>
Interface I
End Interface

Class C
    Implements I$$
End Class
</Code>

            Await TestKind(code, EnvDTE.vsCMElement.vsCMElementImplementsStmt)
        End Function

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName1() As Task
            Dim code =
<Code>
Interface I
End Interface

Class C
    Implements I$$
End Class
</Code>

            Await TestName(code, "Implements")
        End Function

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
