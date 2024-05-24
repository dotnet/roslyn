' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.LanguageServices.Implementation.F1Help

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Help
    <ExportLanguageService(GetType(IHelpContextService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicHelpContextService
        Inherits AbstractHelpContextService

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property Language As String
            Get
                Return "VB"
            End Get
        End Property

        Public Overrides ReadOnly Property Product As String
            Get
                Return "VB"
            End Get
        End Property

        Public Overrides Async Function GetHelpTermAsync(document As Document, span As TextSpan, cancellationToken As CancellationToken) As Task(Of String)
            Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim token = tree.GetRoot(cancellationToken).FindToken(span.Start, findInsideTrivia:=True)

            If TokenIsHelpKeyword(token) Then
                Return "vb." + token.Text
            End If

            If token.Span.IntersectsWith(span) OrElse token.GetAncestor(Of XmlElementSyntax)() IsNot Nothing Then
                Dim visitor = New Visitor(token.Span, Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False), document.Project.Solution.WorkspaceKind <> WorkspaceKind.MetadataAsSource, Me, cancellationToken)
                visitor.Visit(token.Parent)
                Return visitor.result
            End If

            Dim trivia = tree.GetRoot(cancellationToken).FindTrivia(span.Start, findInsideTrivia:=True)

            Dim text = If(trivia.ToFullString(), String.Empty).Replace(" ", "").TrimStart("'"c)
            If text.StartsWith("TODO:", StringComparison.CurrentCultureIgnoreCase) Then
                Return HelpKeywords.TaskListUserComments
            End If

            If trivia.IsKind(SyntaxKind.CommentTrivia) Then
                Return "vb.Rem"
            End If

            Return String.Empty
        End Function

        Private Shared Function TokenIsHelpKeyword(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.SharedKeyword, SyntaxKind.WideningKeyword, SyntaxKind.CTypeKeyword, SyntaxKind.NarrowingKeyword,
                                SyntaxKind.OperatorKeyword, SyntaxKind.AddHandlerKeyword, SyntaxKind.RemoveHandlerKeyword, SyntaxKind.AnsiKeyword,
                                SyntaxKind.AutoKeyword, SyntaxKind.UnicodeKeyword, SyntaxKind.HandlesKeyword, SyntaxKind.NotKeyword, SyntaxKind.DirectCastKeyword, SyntaxKind.TryCastKeyword)
        End Function

        Private Shared Function FormatNamespaceOrTypeSymbol(symbol As INamespaceOrTypeSymbol) As String
            If symbol.IsAnonymousType() Then
                Return HelpKeywords.AnonymousType
            End If

            Dim displayString = symbol.ToDisplayString(TypeFormat)
            If symbol.GetTypeArguments().Any() Then
                Return $"{displayString}`{symbol.GetTypeArguments().Length}"
            End If

            Return displayString
        End Function

        Public Overloads Overrides Function FormatSymbol(symbol As ISymbol) As String
            Return FormatSymbol(symbol, isContainingType:=False)
        End Function

        Private Overloads Shared Function FormatSymbol(symbol As ISymbol, isContainingType As Boolean) As String
            Dim symbolType = symbol.GetSymbolType()

            If TypeOf symbolType Is IArrayTypeSymbol Then
                symbolType = DirectCast(symbolType, IArrayTypeSymbol).ElementType
            End If

            If (symbolType IsNot Nothing AndAlso symbolType.IsAnonymousType) OrElse symbol.IsAnonymousType() OrElse symbol.IsAnonymousTypeProperty() Then
                Return HelpKeywords.AnonymousType
            End If

            If symbol.MatchesKind(SymbolKind.Alias, SymbolKind.Local, SymbolKind.Parameter, SymbolKind.RangeVariable) Then
                Return FormatNamespaceOrTypeSymbol(symbol.GetSymbolType())
            End If

            If Not isContainingType AndAlso TypeOf symbol Is INamedTypeSymbol Then
                Dim type = DirectCast(symbol, INamedTypeSymbol)
                If type.SpecialType <> SpecialType.None Then
                    Return "vb." + type.ToDisplayString(SpecialTypeFormat)
                End If
            End If

            If TypeOf symbol Is ITypeSymbol OrElse TypeOf symbol Is INamespaceSymbol Then
                Return FormatNamespaceOrTypeSymbol(DirectCast(symbol, INamespaceOrTypeSymbol))
            End If

            Dim containingType = FormatSymbol(symbol.ContainingType, isContainingType:=True)
            Dim name = symbol.ToDisplayString(NameFormat)

            If symbol.IsConstructor() Then
                Return $"{containingType}.New"
            End If

            Dim arity = symbol.GetArity()
            If arity > 0 Then
                Return $"{containingType}.{name}``{arity}"
            End If

            Return $"{containingType}.{name}"
        End Function
    End Class
End Namespace
