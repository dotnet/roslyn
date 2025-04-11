' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.SolutionCrawler
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SolutionCrawler
    <ExportLanguageService(GetType(IDocumentDifferenceService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicDocumentDifferenceService
        Inherits AbstractDocumentDifferenceService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function IsContainedInMemberBody(node As SyntaxNode, span As Text.TextSpan) As Boolean
            Dim method = TryCast(node, MethodBlockBaseSyntax)
            If method IsNot Nothing Then
                Return method.Statements.Count > 0 AndAlso ContainsExclusively(GetSyntaxListSpan(method.Statements), span)
            End If

            Dim [event] = TryCast(node, EventBlockSyntax)
            If [event] IsNot Nothing Then
                Return [event].Accessors.Count > 0 AndAlso ContainsExclusively(GetSyntaxListSpan([event].Accessors), span)
            End If

            Dim [property] = TryCast(node, PropertyBlockSyntax)
            If [property] IsNot Nothing Then
                Return [property].Accessors.Count > 0 AndAlso ContainsExclusively(GetSyntaxListSpan([property].Accessors), span)
            End If

            Dim field = TryCast(node, FieldDeclarationSyntax)
            If field IsNot Nothing Then
                Return field.Declarators.Count > 0 AndAlso ContainsExclusively(GetSeparatedSyntaxListSpan(field.Declarators), span)
            End If

            Dim [enum] = TryCast(node, EnumMemberDeclarationSyntax)
            If [enum] IsNot Nothing Then
                Return [enum].Initializer IsNot Nothing AndAlso ContainsExclusively([enum].Initializer.Span, span)
            End If

            Dim propStatement = TryCast(node, PropertyStatementSyntax)
            If propStatement IsNot Nothing Then
                Return propStatement.Initializer IsNot Nothing AndAlso ContainsExclusively(propStatement.Initializer.Span, span)
            End If

            Return False
        End Function

        Private Shared Function ContainsExclusively(outerSpan As TextSpan, innerSpan As TextSpan) As Boolean
            If innerSpan.IsEmpty Then
                Return outerSpan.Contains(innerSpan.Start)
            End If

            Return outerSpan.Contains(innerSpan)
        End Function

        Private Shared Function GetSyntaxListSpan(Of T As SyntaxNode)(list As SyntaxList(Of T)) As TextSpan
            Debug.Assert(list.Count > 0)
            Return TextSpan.FromBounds(list.First.SpanStart, list.Last.Span.End)
        End Function

        Private Shared Function GetSeparatedSyntaxListSpan(Of T As SyntaxNode)(list As SeparatedSyntaxList(Of T)) As TextSpan
            Debug.Assert(list.Count > 0)
            Return TextSpan.FromBounds(list.First.SpanStart, list.Last.Span.End)
        End Function
    End Class
End Namespace
