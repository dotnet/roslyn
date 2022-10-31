' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis.AddImport
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.Suppression
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Suppression
    <ExportConfigurationFixProvider(PredefinedConfigurationFixProviderNames.Suppression, LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSuppressionCodeFixProvider
        Inherits AbstractSuppressionCodeFixProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function CreatePragmaRestoreDirectiveTrivia(diagnostic As Diagnostic, formatNode As Func(Of SyntaxNode, CancellationToken, SyntaxNode), needsLeadingEndOfLine As Boolean, needsTrailingEndOfLine As Boolean, cancellationToken As CancellationToken) As SyntaxTriviaList
            Dim includeTitle As Boolean
            Dim errorCodes = GetErrorCodes(diagnostic, includeTitle)
            Dim pragmaDirective = SyntaxFactory.EnableWarningDirectiveTrivia(errorCodes)
            Return CreatePragmaDirectiveTrivia(pragmaDirective, includeTitle, diagnostic, formatNode, needsLeadingEndOfLine, needsTrailingEndOfLine, cancellationToken)
        End Function

        Protected Overrides Function CreatePragmaDisableDirectiveTrivia(diagnostic As Diagnostic, formatNode As Func(Of SyntaxNode, CancellationToken, SyntaxNode), needsLeadingEndOfLine As Boolean, needsTrailingEndOfLine As Boolean, cancellationToken As CancellationToken) As SyntaxTriviaList
            Dim includeTitle As Boolean
            Dim errorCodes = GetErrorCodes(diagnostic, includeTitle)
            Dim pragmaDirective = SyntaxFactory.DisableWarningDirectiveTrivia(errorCodes)
            Return CreatePragmaDirectiveTrivia(pragmaDirective, includeTitle, diagnostic, formatNode, needsLeadingEndOfLine, needsTrailingEndOfLine, cancellationToken)
        End Function

        Private Shared Function GetErrorCodes(diagnostic As Diagnostic, ByRef includeTitle As Boolean) As SeparatedSyntaxList(Of IdentifierNameSyntax)
            Dim text = GetOrMapDiagnosticId(diagnostic, includeTitle)
            If SyntaxFacts.GetKeywordKind(text) <> SyntaxKind.None Then
                text = "[" & text & "]"
            End If

            Return New SeparatedSyntaxList(Of IdentifierNameSyntax)().Add(SyntaxFactory.IdentifierName(text))
        End Function

        Private Shared Function CreatePragmaDirectiveTrivia(
                enableOrDisablePragmaDirective As StructuredTriviaSyntax,
                includeTitle As Boolean,
                diagnostic As Diagnostic,
                formatNode As Func(Of SyntaxNode, CancellationToken, SyntaxNode),
                needsLeadingEndOfLine As Boolean,
                needsTrailingEndOfLine As Boolean,
                cancellationToken As CancellationToken) As SyntaxTriviaList

            enableOrDisablePragmaDirective = CType(formatNode(enableOrDisablePragmaDirective, cancellationToken), StructuredTriviaSyntax)
            Dim pragmaDirectiveTrivia = SyntaxFactory.Trivia(enableOrDisablePragmaDirective)
            Dim endOfLineTrivia = SyntaxFactory.ElasticCarriageReturnLineFeed
            Dim triviaList = SyntaxFactory.TriviaList(pragmaDirectiveTrivia)

            Dim title = If(includeTitle, diagnostic.Descriptor.Title.ToString(CultureInfo.CurrentUICulture), Nothing)
            If Not String.IsNullOrWhiteSpace(title) Then
                Dim titleComment = SyntaxFactory.CommentTrivia(String.Format(" ' {0}", title)).WithAdditionalAnnotations(Formatter.Annotation)
                triviaList = triviaList.Add(titleComment)
            End If

            If needsLeadingEndOfLine Then
                triviaList = triviaList.Insert(0, endOfLineTrivia)
            End If

            If needsTrailingEndOfLine Then
                triviaList = triviaList.Add(endOfLineTrivia)
            End If

            Return triviaList
        End Function

        Protected Overrides Function GetContainingStatement(token As SyntaxToken) As SyntaxNode
            Return token.GetAncestor(Of StatementSyntax)()
        End Function

        Protected Overrides Function TokenHasTrailingLineContinuationChar(token As SyntaxToken) As Boolean
            Return token.TrailingTrivia.Any(Function(t) t.Kind = SyntaxKind.LineContinuationTrivia)
        End Function

        Protected Overrides ReadOnly Property DefaultFileExtension() As String
            Get
                Return ".vb"
            End Get
        End Property

        Protected Overrides ReadOnly Property SingleLineCommentStart() As String
            Get
                Return "'"
            End Get
        End Property

        Protected Overrides Function IsAttributeListWithAssemblyAttributes(node As SyntaxNode) As Boolean
            Dim attributesStatement = TryCast(node, AttributesStatementSyntax)
            Return attributesStatement IsNot Nothing AndAlso
                attributesStatement.AttributeLists.All(Function(attributeList) attributeList.Attributes.All(Function(a) a.Target.AttributeModifier.Kind() = SyntaxKind.AssemblyKeyword))
        End Function

        Protected Overrides Function IsEndOfLine(trivia As SyntaxTrivia) As Boolean
            Return trivia.Kind = SyntaxKind.EndOfLineTrivia
        End Function

        Protected Overrides Function IsEndOfFileToken(token As SyntaxToken) As Boolean
            Return token.Kind = SyntaxKind.EndOfFileToken
        End Function

        Protected Overrides Function AddGlobalSuppressMessageAttribute(
                newRoot As SyntaxNode,
                targetSymbol As ISymbol,
                suppressMessageAttribute As INamedTypeSymbol,
                diagnostic As Diagnostic,
                services As SolutionServices,
                options As SyntaxFormattingOptions,
                addImportsService As IAddImportsService,
                cancellationToken As CancellationToken) As SyntaxNode
            Dim compilationRoot = DirectCast(newRoot, CompilationUnitSyntax)
            Dim isFirst = Not compilationRoot.Attributes.Any()

            Dim attributeName = DirectCast(suppressMessageAttribute.GenerateTypeSyntax(), NameSyntax).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation)
            Dim attributeList = CreateAttributeList(targetSymbol, attributeName, diagnostic, isAssemblyAttribute:=True)

            Dim attributeStatement = SyntaxFactory.AttributesStatement(New SyntaxList(Of AttributeListSyntax)().Add(attributeList))
            If Not isFirst Then
                Dim trailingTrivia = compilationRoot.Attributes.Last().GetTrailingTrivia()
                Dim lastTrivia = If(trailingTrivia.IsEmpty, Nothing, trailingTrivia(trailingTrivia.Count - 1))
                If Not IsEndOfLine(lastTrivia) Then
                    ' Add leading end of line trivia to attribute statement
                    attributeStatement = attributeStatement.WithLeadingTrivia(attributeStatement.GetLeadingTrivia.Add(SyntaxFactory.ElasticCarriageReturnLineFeed))
                End If
            End If

            attributeStatement = CType(Formatter.Format(attributeStatement, services, options, cancellationToken), AttributesStatementSyntax)

            Dim leadingTrivia = If(isFirst AndAlso Not compilationRoot.HasLeadingTrivia,
                SyntaxFactory.TriviaList(SyntaxFactory.CommentTrivia(GlobalSuppressionsFileHeaderComment)),
                Nothing)
            leadingTrivia = leadingTrivia.AddRange(compilationRoot.GetLeadingTrivia())
            compilationRoot = compilationRoot.WithoutLeadingTrivia()
            Return compilationRoot.AddAttributes(attributeStatement).WithLeadingTrivia(leadingTrivia)
        End Function

        Protected Overrides Function AddLocalSuppressMessageAttribute(
                targetNode As SyntaxNode,
                targetSymbol As ISymbol,
                suppressMessageAttribute As INamedTypeSymbol,
                diagnostic As Diagnostic) As SyntaxNode
            Dim memberNode = DirectCast(targetNode, StatementSyntax)

            Dim attributeName = DirectCast(suppressMessageAttribute.GenerateTypeSyntax(), NameSyntax)
            Dim attributeList = CreateAttributeList(targetSymbol, attributeName, diagnostic, isAssemblyAttribute:=False)
            Dim leadingTrivia = memberNode.GetLeadingTrivia()
            memberNode = memberNode.WithoutLeadingTrivia()
            Return memberNode.AddAttributeLists(attributeList).WithLeadingTrivia(leadingTrivia)
        End Function

        Private Shared Function CreateAttributeList(
                targetSymbol As ISymbol,
                attributeName As NameSyntax,
                diagnostic As Diagnostic,
                isAssemblyAttribute As Boolean) As AttributeListSyntax
            Dim attributeTarget = If(isAssemblyAttribute, SyntaxFactory.AttributeTarget(SyntaxFactory.Token(SyntaxKind.AssemblyKeyword)), Nothing)
            Dim attributeArguments = CreateAttributeArguments(targetSymbol, diagnostic, isAssemblyAttribute)

            Dim attribute As AttributeSyntax = SyntaxFactory.Attribute(attributeTarget, attributeName, attributeArguments)
            Return SyntaxFactory.AttributeList().AddAttributes(attribute)
        End Function

        Private Shared Function CreateAttributeArguments(targetSymbol As ISymbol, diagnostic As Diagnostic, isAssemblyAttribute As Boolean) As ArgumentListSyntax
            ' SuppressMessage("Rule Category", "Rule Id", Justification := "Justification", Scope := "Scope", Target := "Target")
            Dim category = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(diagnostic.Descriptor.Category))
            Dim categoryArgument = SyntaxFactory.SimpleArgument(category)

            Dim title = diagnostic.Descriptor.Title.ToString(CultureInfo.CurrentUICulture)
            Dim ruleIdText = If(String.IsNullOrWhiteSpace(title), diagnostic.Id, String.Format("{0}:{1}", diagnostic.Id, title))
            Dim ruleId = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(ruleIdText))
            Dim ruleIdArgument = SyntaxFactory.SimpleArgument(ruleId)

            Dim justificationExpr = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(FeaturesResources.Pending))
            Dim justificationArgument = SyntaxFactory.SimpleArgument(SyntaxFactory.NameColonEquals(SyntaxFactory.IdentifierName("Justification")), expression:=justificationExpr)

            Dim attributeArgumentList = SyntaxFactory.ArgumentList().AddArguments(categoryArgument, ruleIdArgument, justificationArgument)

            Dim scopeString = GetScopeString(targetSymbol.Kind)
            If isAssemblyAttribute Then
                If scopeString IsNot Nothing Then
                    Dim scopeExpr = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(scopeString))
                    Dim scopeArgument = SyntaxFactory.SimpleArgument(SyntaxFactory.NameColonEquals(SyntaxFactory.IdentifierName("Scope")), expression:=scopeExpr)

                    Dim targetString = GetTargetString(targetSymbol)
                    Dim targetExpr = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(targetString))
                    Dim targetArgument = SyntaxFactory.SimpleArgument(SyntaxFactory.NameColonEquals(SyntaxFactory.IdentifierName("Target")), expression:=targetExpr)

                    attributeArgumentList = attributeArgumentList.AddArguments(scopeArgument, targetArgument)
                End If
            End If

            Return attributeArgumentList
        End Function

        Protected Overrides Function IsSingleAttributeInAttributeList(attribute As SyntaxNode) As Boolean
            Dim attributeSyntax = TryCast(attribute, AttributeSyntax)
            If attributeSyntax IsNot Nothing Then
                Dim attributeList = TryCast(attributeSyntax.Parent, AttributeListSyntax)
                Return attributeList IsNot Nothing AndAlso attributeList.Attributes.Count = 1
            End If

            Return False
        End Function

        Protected Overrides Function IsAnyPragmaDirectiveForId(trivia As SyntaxTrivia, id As String, ByRef enableDirective As Boolean, ByRef hasMultipleIds As Boolean) As Boolean
            Dim errorCodes As SeparatedSyntaxList(Of IdentifierNameSyntax)

            Select Case trivia.Kind()
                Case SyntaxKind.DisableWarningDirectiveTrivia
                    Dim pragmaWarning = DirectCast(trivia.GetStructure(), DisableWarningDirectiveTriviaSyntax)
                    errorCodes = pragmaWarning.ErrorCodes
                    enableDirective = False

                Case SyntaxKind.EnableWarningDirectiveTrivia
                    Dim pragmaWarning = DirectCast(trivia.GetStructure(), EnableWarningDirectiveTriviaSyntax)
                    errorCodes = pragmaWarning.ErrorCodes
                    enableDirective = True

                Case Else
                    enableDirective = False
                    hasMultipleIds = False
                    Return False
            End Select

            hasMultipleIds = errorCodes.Count > 1
            Return errorCodes.Any(Function(node) node.ToString = id)
        End Function

        Protected Overrides Function TogglePragmaDirective(trivia As SyntaxTrivia) As SyntaxTrivia
            Select Case trivia.Kind()
                Case SyntaxKind.DisableWarningDirectiveTrivia
                    Dim pragmaWarning = DirectCast(trivia.GetStructure(), DisableWarningDirectiveTriviaSyntax)
                    Dim disabledKeyword = pragmaWarning.DisableKeyword
                    Dim enabledKeyword = SyntaxFactory.Token(disabledKeyword.LeadingTrivia, SyntaxKind.EnableKeyword, disabledKeyword.TrailingTrivia)
                    Dim newPragmaWarning = SyntaxFactory.EnableWarningDirectiveTrivia(pragmaWarning.HashToken, enabledKeyword, pragmaWarning.WarningKeyword, pragmaWarning.ErrorCodes) _
                        .WithLeadingTrivia(pragmaWarning.GetLeadingTrivia) _
                        .WithTrailingTrivia(pragmaWarning.GetTrailingTrivia)
                    Return SyntaxFactory.Trivia(newPragmaWarning)

                Case SyntaxKind.EnableWarningDirectiveTrivia
                    Dim pragmaWarning = DirectCast(trivia.GetStructure(), EnableWarningDirectiveTriviaSyntax)
                    Dim enabledKeyword = pragmaWarning.EnableKeyword
                    Dim disabledKeyword = SyntaxFactory.Token(enabledKeyword.LeadingTrivia, SyntaxKind.DisableKeyword, enabledKeyword.TrailingTrivia)
                    Dim newPragmaWarning = SyntaxFactory.DisableWarningDirectiveTrivia(pragmaWarning.HashToken, disabledKeyword, pragmaWarning.WarningKeyword, pragmaWarning.ErrorCodes) _
                        .WithLeadingTrivia(pragmaWarning.GetLeadingTrivia) _
                        .WithTrailingTrivia(pragmaWarning.GetTrailingTrivia)
                    Return SyntaxFactory.Trivia(newPragmaWarning)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(trivia.Kind())
            End Select
        End Function
    End Class
End Namespace
