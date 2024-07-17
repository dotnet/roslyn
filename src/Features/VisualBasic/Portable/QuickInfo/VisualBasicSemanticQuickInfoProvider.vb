' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.QuickInfo
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.VisualBasic.QuickInfo
    <ExportQuickInfoProvider(QuickInfoProviderNames.Semantic, LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSemanticQuickInfoProvider
        Inherits CommonSemanticQuickInfoProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Async Function BuildQuickInfoAsync(
                context As QuickInfoContext,
                token As SyntaxToken) As Task(Of QuickInfoItem)
            Dim semanticModel = Await context.Document.GetRequiredSemanticModelAsync(context.CancellationToken).ConfigureAwait(False)
            Dim services = context.Document.Project.Solution.Services
            Dim info = Await BuildQuickInfoAsync(services, semanticModel, token, context.Options, context.CancellationToken).ConfigureAwait(False)
            If info IsNot Nothing Then
                Return info
            End If

            Return Await MyBase.BuildQuickInfoAsync(context, token).ConfigureAwait(False)
        End Function

        Protected Overrides Async Function BuildQuickInfoAsync(
                context As CommonQuickInfoContext,
                token As SyntaxToken) As Task(Of QuickInfoItem)
            Dim info = Await BuildQuickInfoAsync(context.Services, context.SemanticModel, token, context.Options, context.CancellationToken).ConfigureAwait(False)
            If info IsNot Nothing Then
                Return info
            End If

            Return Await MyBase.BuildQuickInfoAsync(context, token).ConfigureAwait(False)
        End Function

        Private Overloads Shared Async Function BuildQuickInfoAsync(
                services As SolutionServices,
                semanticModel As SemanticModel,
                token As SyntaxToken,
                options As SymbolDescriptionOptions,
                cancellationToken As CancellationToken) As Task(Of QuickInfoItem)

            Dim parent = token.Parent

            Dim predefinedCastExpression = TryCast(parent, PredefinedCastExpressionSyntax)
            If predefinedCastExpression IsNot Nothing AndAlso token = predefinedCastExpression.Keyword Then
                Dim documentation = New PredefinedCastExpressionDocumentation(predefinedCastExpression.Keyword.Kind, semanticModel.Compilation)
                Return BuildContentForIntrinsicOperator(semanticModel, token, parent, documentation, Glyph.MethodPublic, cancellationToken)
            End If

            Select Case token.Kind
                Case SyntaxKind.AddHandlerKeyword
                    If TypeOf parent Is AddRemoveHandlerStatementSyntax Then
                        Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New AddHandlerStatementDocumentation(), Glyph.Keyword, cancellationToken)
                    End If

                Case SyntaxKind.DimKeyword
                    If TypeOf parent Is FieldDeclarationSyntax Then
                        Return Await BuildContentAsync(services, semanticModel, token, DirectCast(parent, FieldDeclarationSyntax).Declarators, options, cancellationToken).ConfigureAwait(False)
                    ElseIf TypeOf parent Is LocalDeclarationStatementSyntax Then
                        Return Await BuildContentAsync(services, semanticModel, token, DirectCast(parent, LocalDeclarationStatementSyntax).Declarators, options, cancellationToken).ConfigureAwait(False)
                    End If

                Case SyntaxKind.CTypeKeyword
                    If TypeOf parent Is CTypeExpressionSyntax Then
                        Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New CTypeCastExpressionDocumentation(), Glyph.MethodPublic, cancellationToken)
                    End If

                Case SyntaxKind.DirectCastKeyword
                    If TypeOf parent Is DirectCastExpressionSyntax Then
                        Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New DirectCastExpressionDocumentation(), Glyph.MethodPublic, cancellationToken)
                    End If

                Case SyntaxKind.GetTypeKeyword
                    If TypeOf parent Is GetTypeExpressionSyntax Then
                        Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New GetTypeExpressionDocumentation(), Glyph.MethodPublic, cancellationToken)
                    End If

                Case SyntaxKind.GetXmlNamespaceKeyword
                    If TypeOf parent Is GetXmlNamespaceExpressionSyntax Then
                        Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New GetXmlNamespaceExpressionDocumentation(), Glyph.MethodPublic, cancellationToken)
                    End If

                Case SyntaxKind.IfKeyword
                    If parent.Kind = SyntaxKind.BinaryConditionalExpression Then
                        Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New BinaryConditionalExpressionDocumentation(), Glyph.MethodPublic, cancellationToken)
                    ElseIf parent.Kind = SyntaxKind.TernaryConditionalExpression Then
                        Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New TernaryConditionalExpressionDocumentation(), Glyph.MethodPublic, cancellationToken)
                    End If

                Case SyntaxKind.RemoveHandlerKeyword
                    If TypeOf parent Is AddRemoveHandlerStatementSyntax Then
                        Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New RemoveHandlerStatementDocumentation(), Glyph.Keyword, cancellationToken)
                    End If

                Case SyntaxKind.TryCastKeyword
                    If TypeOf parent Is TryCastExpressionSyntax Then
                        Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New TryCastExpressionDocumentation(), Glyph.MethodPublic, cancellationToken)
                    End If

                Case SyntaxKind.IdentifierToken
                    If SyntaxFacts.GetContextualKeywordKind(token.ToString()) = SyntaxKind.MidKeyword Then
                        If parent.Kind = SyntaxKind.MidExpression Then
                            Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New MidAssignmentDocumentation(), Glyph.MethodPublic, cancellationToken)
                        End If
                    End If
            End Select

            Return Nothing
        End Function

        ''' <summary>
        ''' If the token is a 'Sub' or 'Function' in a lambda, returns the syntax for the whole lambda
        ''' </summary>
        Protected Overrides Function GetBindableNodeForTokenIndicatingLambda(token As SyntaxToken, <Out> ByRef found As SyntaxNode) As Boolean
            If token.IsKind(SyntaxKind.SubKeyword, SyntaxKind.FunctionKeyword) AndAlso token.Parent.IsKind(SyntaxKind.SubLambdaHeader, SyntaxKind.FunctionLambdaHeader) Then
                found = token.Parent.Parent
                Return True
            End If

            found = Nothing
            Return False
        End Function

        Protected Overrides Function GetBindableNodeForTokenIndicatingPossibleIndexerAccess(token As SyntaxToken, ByRef found As SyntaxNode) As Boolean
            If token.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken) AndAlso
                token.Parent?.Parent.IsKind(SyntaxKind.InvocationExpression) = True Then
                found = token.Parent.Parent
                Return True
            End If

            found = Nothing
            Return False
        End Function

        Protected Overrides Function GetBindableNodeForTokenIndicatingMemberAccess(token As SyntaxToken, ByRef found As SyntaxToken) As Boolean
            If token.IsKind(SyntaxKind.DotToken) AndAlso
                token.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then

                found = DirectCast(token.Parent, MemberAccessExpressionSyntax).Name.Identifier
                Return True
            End If

            found = Nothing
            Return False
        End Function

        Private Overloads Shared Async Function BuildContentAsync(
                services As SolutionServices,
                semanticModel As SemanticModel,
                token As SyntaxToken,
                declarators As SeparatedSyntaxList(Of VariableDeclaratorSyntax),
                options As SymbolDescriptionOptions,
                cancellationToken As CancellationToken) As Task(Of QuickInfoItem)

            If declarators.Count = 0 Then
                Return Nothing
            End If

            Dim types = declarators.SelectMany(Function(d) d.Names).Select(
                Function(n) As ISymbol
                    Dim symbol = semanticModel.GetDeclaredSymbol(n, cancellationToken)
                    If symbol Is Nothing Then
                        Return Nothing
                    End If

                    If TypeOf symbol Is ILocalSymbol Then
                        Return DirectCast(symbol, ILocalSymbol).Type
                    ElseIf TypeOf symbol Is IFieldSymbol Then
                        Return DirectCast(symbol, IFieldSymbol).Type
                    Else
                        Return Nothing
                    End If
                End Function).WhereNotNull().Distinct().ToImmutableArray()

            If types.Length = 0 Then
                Return Nothing
            End If

            If types.Length > 1 Then
                Return QuickInfoItem.Create(token.Span, sections:=ImmutableArray.Create(QuickInfoSection.Create(QuickInfoSectionKinds.Description, ImmutableArray.Create(New TaggedText(TextTags.Text, VBFeaturesResources.Multiple_Types)))))
            End If

            Return Await CreateContentAsync(services, semanticModel, token, New TokenInformation(types), supportedPlatforms:=Nothing, options, onTheFlyDocsElement:=Nothing, cancellationToken).ConfigureAwait(False)
        End Function

        Private Shared Function BuildContentForIntrinsicOperator(
                semanticModel As SemanticModel,
                token As SyntaxToken,
                expression As SyntaxNode,
                documentation As AbstractIntrinsicOperatorDocumentation,
                glyph As Glyph,
                cancellationToken As CancellationToken) As QuickInfoItem
            Dim builder = New List(Of SymbolDisplayPart)

            builder.AddRange(documentation.PrefixParts)

            Dim position = expression.SpanStart

            For i = 0 To documentation.ParameterCount - 1
                If i <> 0 Then
                    builder.AddPunctuation(",")
                    builder.AddSpace()
                End If

                Dim typeNameToBind = documentation.TryGetTypeNameParameter(expression, i)

                If typeNameToBind IsNot Nothing Then
                    ' We'll try to bind the type name
                    Dim typeInfo = semanticModel.GetTypeInfo(typeNameToBind, cancellationToken)

                    If typeInfo.Type IsNot Nothing Then
                        builder.AddRange(typeInfo.Type.ToMinimalDisplayParts(semanticModel, position))
                        Continue For
                    End If
                End If

                builder.AddRange(documentation.GetParameterDisplayParts(i))
            Next

            builder.AddRange(documentation.GetSuffix(semanticModel, position, expression, cancellationToken))

            Return QuickInfoItem.Create(
                token.Span,
                tags:=GlyphTags.GetTags(glyph),
                sections:=ImmutableArray.Create(
                    QuickInfoSection.Create(QuickInfoSectionKinds.Description, builder.ToTaggedText()),
                    QuickInfoSection.Create(QuickInfoSectionKinds.DocumentationComments, ImmutableArray.Create(New TaggedText(TextTags.Text, documentation.DocumentationText)))))
        End Function
    End Class
End Namespace

