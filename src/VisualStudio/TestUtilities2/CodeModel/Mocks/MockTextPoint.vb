' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.Mocks
    Friend NotInheritable Class MockTextPoint
        Implements EnvDTE.TextPoint

        Private ReadOnly _point As VirtualTreePoint
        Private ReadOnly _tabSize As Integer

        Public Sub New(point As VirtualTreePoint, tabSize As Integer)
            _point = point
            _tabSize = tabSize
        End Sub

        Public ReadOnly Property AbsoluteCharOffset As Integer Implements EnvDTE.TextPoint.AbsoluteCharOffset
            Get
                ' DTE TextPoints count each newline as a single character regardless
                ' of what the actual newline character is. So, we have to walk through all the lines
                ' and add up the length of each line + 1.
                '
                ' VS performs this same computation in GetAbsoluteOffset in env\msenv\textmgr\autoutil.cpp.

                Dim result = 0
                Dim containingLine = _point.GetContainingLine()

                For Each textLine In _point.Text.Lines
                    If textLine.LineNumber >= containingLine.LineNumber Then
                        Exit For
                    End If

                    result += textLine.Span.Length + 1
                Next

                result += _point.Position - containingLine.Start

                Return result + 1
            End Get
        End Property

        Public ReadOnly Property AtEndOfDocument As Boolean Implements EnvDTE.TextPoint.AtEndOfDocument
            Get
                Return _point.Position = _point.Text.Length
            End Get
        End Property

        Public ReadOnly Property AtEndOfLine As Boolean Implements EnvDTE.TextPoint.AtEndOfLine
            Get
                Return _point.Position = _point.GetContainingLine().End
            End Get
        End Property

        Public ReadOnly Property AtStartOfDocument As Boolean Implements EnvDTE.TextPoint.AtStartOfDocument
            Get
                Return _point.Position = 0
            End Get
        End Property

        Public ReadOnly Property AtStartOfLine As Boolean Implements EnvDTE.TextPoint.AtStartOfLine
            Get
                Return _point.Position = _point.GetContainingLine().Start
            End Get
        End Property

        Public ReadOnly Property CodeElement(Scope As EnvDTE.vsCMElement) As EnvDTE.CodeElement Implements EnvDTE.TextPoint.CodeElement
            Get
                Throw New NotImplementedException
            End Get
        End Property

        Public Function CreateEditPoint() As EnvDTE.EditPoint Implements EnvDTE.TextPoint.CreateEditPoint
            Throw New NotImplementedException
        End Function

        Public ReadOnly Property DisplayColumn As Integer Implements EnvDTE.TextPoint.DisplayColumn
            Get
                Throw New NotImplementedException
            End Get
        End Property

        Public ReadOnly Property DTE As EnvDTE.DTE Implements EnvDTE.TextPoint.DTE
            Get
                Throw New NotImplementedException
            End Get
        End Property

        Public Function EqualTo(Point As EnvDTE.TextPoint) As Boolean Implements EnvDTE.TextPoint.EqualTo
            Return Me.AbsoluteCharOffset = Point.AbsoluteCharOffset
        End Function

        Public Function GreaterThan(Point As EnvDTE.TextPoint) As Boolean Implements EnvDTE.TextPoint.GreaterThan
            Return Me.AbsoluteCharOffset > Point.AbsoluteCharOffset
        End Function

        Public Function LessThan(Point As EnvDTE.TextPoint) As Boolean Implements EnvDTE.TextPoint.LessThan
            Return Me.AbsoluteCharOffset < Point.AbsoluteCharOffset
        End Function

        Public ReadOnly Property Line As Integer Implements EnvDTE.TextPoint.Line
            Get
                ' These line numbers start at 1!
                Return _point.GetContainingLine().LineNumber + 1
            End Get
        End Property

        Public ReadOnly Property LineCharOffset As Integer Implements EnvDTE.TextPoint.LineCharOffset
            Get
                Dim result = _point.Position - _point.GetContainingLine().Start + 1
                If _point.IsInVirtualSpace Then
                    result += _point.VirtualSpaces
                End If
                Return result
            End Get
        End Property

        Public ReadOnly Property LineLength As Integer Implements EnvDTE.TextPoint.LineLength
            Get
                Dim line = _point.GetContainingLine()
                Return line.End - line.Start
            End Get
        End Property

        Public ReadOnly Property Parent As EnvDTE.TextDocument Implements EnvDTE.TextPoint.Parent
            Get
                Return CreateEditPoint().Parent
            End Get
        End Property

        Public Function TryToShow(Optional How As EnvDTE.vsPaneShowHow = EnvDTE.vsPaneShowHow.vsPaneShowCentered, Optional PointOrCount As Object = Nothing) As Boolean Implements EnvDTE.TextPoint.TryToShow
            Throw New NotImplementedException
        End Function
    End Class
End Namespace
