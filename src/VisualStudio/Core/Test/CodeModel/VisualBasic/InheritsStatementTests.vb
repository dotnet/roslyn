' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class InheritsStatementTests
        Inherits AbstractCodeElementTests

#Region "GetStartPoint() tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint1()
            Dim code =
<Code>
Class B
End Class

Class C
    Inherits $$B
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=5, lineOffset:=5, absoluteOffset:=32, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=5, lineOffset:=5, absoluteOffset:=32, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=5, lineOffset:=5, absoluteOffset:=32, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=5, lineOffset:=5, absoluteOffset:=32, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=5, lineOffset:=5, absoluteOffset:=32, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=5, lineOffset:=5, absoluteOffset:=32, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=5, lineOffset:=5, absoluteOffset:=32, lineLength:=14)))
        End Sub

#End Region

#Region "GetEndPoint() tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint1()
            Dim code =
<Code>
Class B
End Class

Class C
    Inherits $$B
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=5, lineOffset:=15, absoluteOffset:=42, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=5, lineOffset:=15, absoluteOffset:=42, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=5, lineOffset:=15, absoluteOffset:=42, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=5, lineOffset:=15, absoluteOffset:=42, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=5, lineOffset:=15, absoluteOffset:=42, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=5, lineOffset:=15, absoluteOffset:=42, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=5, lineOffset:=15, absoluteOffset:=42, lineLength:=14)))
        End Sub

#End Region

#Region "Kind tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestKind1()
            Dim code =
<Code>
Class B
End Class

Class C
    Inherits B$$
End Class
</Code>

            TestKind(code, EnvDTE.vsCMElement.vsCMElementInheritsStmt)
        End Sub

#End Region

#Region "Name tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName1()
            Dim code =
<Code>
Class B
End Class

Class C
    Inherits B$$
End Class
</Code>

            TestName(code, "Inherits")
        End Sub

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
