' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.ErrorReporting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(CrefCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(PartialTypeCompletionProvider))>
    <[Shared]>
    Partial Friend Class CrefCompletionProvider
        Inherits AbstractCrefCompletionProvider

        Private Shared ReadOnly s_crefFormat As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle:=SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Private Shared ReadOnly s_minimalParameterTypeFormat As SymbolDisplayFormat =
            SymbolDisplayFormat.MinimallyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.ExpandValueTuple)

        Private _testSpeculativeNodeCallbackOpt As Action(Of SyntaxNode)

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacter(text, characterPosition, options)
        End Function

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = CompletionUtilities.CommonTriggerChars

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides Async Function ProvideCompletionsAsync(context As CompletionContext) As Task
            Try
                Dim document = context.Document
                Dim position = context.Position
                Dim cancellationToken = context.CancellationToken

                Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
                Dim token = tree.GetTargetToken(position, cancellationToken)

                If IsCrefTypeParameterContext(token) Then
                    Return
                End If

                ' To get a Speculative SemanticModel (which is much faster), we need to 
                ' walk up to the node the DocumentationTrivia is attached to.
                Dim parentNode = token.Parent?.FirstAncestorOrSelf(Of DocumentationCommentTriviaSyntax)()?.ParentTrivia.Token.Parent
                _testSpeculativeNodeCallbackOpt?.Invoke(parentNode)
                If parentNode Is Nothing Then
                    Return
                End If

                Dim semanticModel = Await document.ReuseExistingSpeculativeModelAsync(parentNode, cancellationToken).ConfigureAwait(False)

                Dim symbols = GetSymbols(token, semanticModel, cancellationToken)
                If Not symbols.Any() Then
                    Return
                End If

                Dim text = Await document.GetValueTextAsync(cancellationToken).ConfigureAwait(False)

                Dim items = CreateCompletionItems(semanticModel, symbols, position)
                context.AddItems(items)

                If IsFirstCrefParameterContext(token) Then
                    ' Include Of in case they're typing a type parameter
                    context.AddItem(CreateOfCompletionItem())
                End If

                context.IsExclusive = True
            Catch e As Exception When FatalError.ReportAndCatchUnlessCanceled(e)
                ' nop
            End Try
        End Function

        Protected Overrides Async Function GetSymbolsAsync(document As Document, position As Integer, options As CompletionOptions, cancellationToken As CancellationToken) As Task(Of (SyntaxToken, SemanticModel, ImmutableArray(Of ISymbol)))
            Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim token = tree.GetTargetToken(position, cancellationToken)

            If IsCrefTypeParameterContext(token) Then
                Return Nothing
            End If

            ' To get a Speculative SemanticModel (which is much faster), we need to 
            ' walk up to the node the DocumentationTrivia is attached to.
            Dim parentNode = token.Parent?.FirstAncestorOrSelf(Of DocumentationCommentTriviaSyntax)()?.ParentTrivia.Token.Parent
            _testSpeculativeNodeCallbackOpt?.Invoke(parentNode)
            If parentNode Is Nothing Then
                Return Nothing
            End If

            Dim semanticModel = Await document.ReuseExistingSpeculativeModelAsync(parentNode, cancellationToken).ConfigureAwait(False)

            Dim symbols = GetSymbols(token, semanticModel, cancellationToken)
            Return (token, semanticModel, symbols.ToImmutableArray())
        End Function

        Private Shared Function IsCrefTypeParameterContext(token As SyntaxToken) As Boolean
            Return (token.IsChildToken(Function(t As TypeArgumentListSyntax) t.OfKeyword) OrElse
                token.IsChildSeparatorToken(Function(t As TypeArgumentListSyntax) t.Arguments)) AndAlso
                token.Parent?.FirstAncestorOrSelf(Of XmlCrefAttributeSyntax)() IsNot Nothing
        End Function

        Private Shared Function IsCrefStartContext(token As SyntaxToken) As Boolean
            ' cases:
            '   <see cref="x|
            '   <see cref='x|
            If token.IsChildToken(Function(x As XmlCrefAttributeSyntax) x.StartQuoteToken) Then
                Return True
            End If

            ' cases:
            '   <see cref="|
            '   <see cref='|
            If token.Parent.IsKind(SyntaxKind.XmlString) AndAlso token.Parent.IsParentKind(SyntaxKind.XmlAttribute) Then
                Dim xmlAttribute = DirectCast(token.Parent.Parent, XmlAttributeSyntax)
                Dim xmlName = TryCast(xmlAttribute.Name, XmlNameSyntax)
                Dim xmlValue = TryCast(xmlAttribute.Value, XmlStringSyntax)

                If xmlName?.LocalName.ValueText = "cref" AndAlso xmlValue?.StartQuoteToken = token Then
                    Return True
                End If
            End If

            Return False
        End Function

        Private Shared Function IsCrefParameterListContext(token As SyntaxToken) As Boolean
            ' cases:
            '   <see cref="M(|
            '   <see cref="M(x, |
            Return IsFirstCrefParameterContext(token) OrElse
                   token.IsChildSeparatorToken(Function(x As CrefSignatureSyntax) x.ArgumentTypes)
        End Function

        Private Shared Function IsFirstCrefParameterContext(ByRef token As SyntaxToken) As Boolean
            Return token.IsChildToken(Function(x As CrefSignatureSyntax) x.OpenParenToken)
        End Function

        Private Overloads Shared Function GetSymbols(token As SyntaxToken, semanticModel As SemanticModel, cancellationToken As CancellationToken) As IEnumerable(Of ISymbol)
            If IsCrefStartContext(token) Then
                Return semanticModel.LookupSymbols(token.SpanStart)
            ElseIf IsCrefParameterListContext(token) Then
                Return semanticModel.LookupNamespacesAndTypes(token.SpanStart)
            ElseIf token.IsChildToken(Function(x As QualifiedNameSyntax) x.DotToken) Then
                Return GetQualifiedSymbols(DirectCast(token.Parent, QualifiedNameSyntax), token, semanticModel, cancellationToken)
            End If

            Return SpecializedCollections.EmptyEnumerable(Of ISymbol)
        End Function

        Private Shared Iterator Function GetQualifiedSymbols(qualifiedName As QualifiedNameSyntax, token As SyntaxToken, semanticModel As SemanticModel, cancellationToken As CancellationToken) As IEnumerable(Of ISymbol)
            Dim leftSymbol = semanticModel.GetSymbolInfo(qualifiedName.Left, cancellationToken).Symbol
            Dim leftType = semanticModel.GetTypeInfo(qualifiedName.Left, cancellationToken).Type

            Dim container = TryCast(If(leftSymbol, leftType), INamespaceOrTypeSymbol)

            For Each symbol In semanticModel.LookupSymbols(token.SpanStart, container)
                Yield symbol
            Next

            Dim namedTypeContainer = TryCast(container, INamedTypeSymbol)
            If namedTypeContainer IsNot Nothing Then
                For Each constructor In namedTypeContainer.Constructors
                    If Not constructor.IsStatic Then
                        Yield constructor
                    End If
                Next
            End If
        End Function

        Private Shared Iterator Function CreateCompletionItems(
                semanticModel As SemanticModel,
                symbols As IEnumerable(Of ISymbol), position As Integer) As IEnumerable(Of CompletionItem)

            Dim builder = SharedPools.Default(Of StringBuilder).Allocate()
            Try
                For Each symbol In symbols
                    builder.Clear()
                    Yield CreateCompletionItem(semanticModel, symbol, position, builder)
                Next
            Finally
                SharedPools.Default(Of StringBuilder).ClearAndFree(builder)
            End Try
        End Function

        Private Shared Function CreateCompletionItem(
                semanticModel As SemanticModel,
                symbol As ISymbol, position As Integer, builder As StringBuilder) As CompletionItem

            If symbol.IsUserDefinedOperator() Then
                builder.Append("Operator ")
            End If

            builder.Append(symbol.ToDisplayString(s_crefFormat))

            Dim parameters = symbol.GetParameters()

            If Not parameters.IsDefaultOrEmpty Then
                builder.Append("("c)

                For i = 0 To parameters.Length - 1
                    If i > 0 Then
                        builder.Append(", ")
                    End If

                    Dim parameter = parameters(i)

                    If parameter.RefKind = RefKind.Ref Then
                        builder.Append("ByRef ")
                    End If

                    builder.Append(parameter.Type.ToMinimalDisplayString(semanticModel, position, s_minimalParameterTypeFormat))
                Next

                builder.Append(")"c)
            ElseIf symbol.Kind = SymbolKind.Method Then
                builder.Append("()")
            End If

            Dim displayString = builder.ToString()

            Return SymbolCompletionItem.CreateWithNameAndKind(
                displayText:=displayString,
                displayTextSuffix:="",
                insertionText:=Nothing,
                symbols:=ImmutableArray.Create(symbol),
                contextPosition:=position,
                rules:=GetRules(displayString))
        End Function

        Private Shared Function CreateOfCompletionItem() As CompletionItem
            Return CommonCompletionItem.Create(
                "Of", displayTextSuffix:="", CompletionItemRules.Default, Glyph.Keyword,
                description:=RecommendedKeyword.CreateDisplayParts("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure))
        End Function

        Private Shared ReadOnly s_WithoutOpenParen As CharacterSetModificationRule = CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, "("c)

        Private Shared ReadOnly s_defaultRules As CompletionItemRules = CompletionItemRules.Default

        Private Shared Function GetRules(displayText As String) As CompletionItemRules
            Dim commitRules = s_defaultRules.CommitCharacterRules

            If displayText.Contains("(") Then
                commitRules = commitRules.Add(s_WithoutOpenParen)
            End If

            Return s_defaultRules.WithCommitCharacterRules(commitRules)
        End Function

        Friend Function GetTestAccessor() As TestAccessor
            Return New TestAccessor(Me)
        End Function

        Friend Structure TestAccessor
            Private ReadOnly _crefCompletionProvider As CrefCompletionProvider

            Public Sub New(crefCompletionProvider As CrefCompletionProvider)
                _crefCompletionProvider = crefCompletionProvider
            End Sub

            Public Sub SetSpeculativeNodeCallback(value As Action(Of SyntaxNode))
                _crefCompletionProvider._testSpeculativeNodeCallbackOpt = value
            End Sub
        End Structure
    End Class
End Namespace
