' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LanguageServices
    Partial Friend Class VisualBasicSymbolDisplayService
        Protected Class SymbolDescriptionBuilder
            Inherits AbstractSymbolDescriptionBuilder

            Private Shared ReadOnly s_minimallyQualifiedFormat As SymbolDisplayFormat = SymbolDisplayFormat.MinimallyQualifiedFormat _
                .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName) _
                .RemoveParameterOptions(SymbolDisplayParameterOptions.IncludeDefaultValue)

            Private Shared ReadOnly s_minimallyQualifiedFormatWithConstants As SymbolDisplayFormat = s_minimallyQualifiedFormat _
                .AddLocalOptions(SymbolDisplayLocalOptions.IncludeConstantValue) _
                .AddMemberOptions(SymbolDisplayMemberOptions.IncludeConstantValue) _
                .AddParameterOptions(SymbolDisplayParameterOptions.IncludeDefaultValue)

            Public Sub New(displayService As ISymbolDisplayService,
                           semanticModel As SemanticModel,
                           position As Integer,
                           workspace As Workspace,
                           anonymousTypeDisplayService As IAnonymousTypeDisplayService,
                           cancellationToken As CancellationToken)
                MyBase.New(displayService, semanticModel, position, workspace, anonymousTypeDisplayService, cancellationToken)
            End Sub

            Protected Overrides Sub AddDeprecatedPrefix()
                AddToGroup(SymbolDescriptionGroups.MainDescription,
                           Punctuation("("),
                           PlainText(VBFeaturesResources.Deprecated),
                           Punctuation(")"),
                           Space())
            End Sub

            Protected Overrides Sub AddExtensionPrefix()
                AddToGroup(SymbolDescriptionGroups.MainDescription,
                    Punctuation("<"),
                    PlainText(VBFeaturesResources.Extension),
                    Punctuation(">"),
                    Space())
            End Sub

            Protected Overrides Sub AddAwaitablePrefix()
                AddToGroup(SymbolDescriptionGroups.MainDescription,
                    Punctuation("<"),
                    PlainText(VBFeaturesResources.Awaitable),
                    Punctuation(">"),
                    Space())
            End Sub

            Protected Overrides Sub AddAwaitableExtensionPrefix()
                AddToGroup(SymbolDescriptionGroups.MainDescription,
                    Punctuation("<"),
                    PlainText(VBFeaturesResources.AwaitableExtension),
                    Punctuation(">"),
                    Space())
            End Sub

            Protected Overrides Function GetInitializerSourcePartsAsync(symbol As ISymbol) As Task(Of IEnumerable(Of SymbolDisplayPart))
                If TypeOf symbol Is IParameterSymbol Then
                    Return GetInitializerSourcePartsAsync(DirectCast(symbol, IParameterSymbol))
                ElseIf TypeOf symbol Is ILocalSymbol Then
                    Return GetInitializerSourcePartsAsync(DirectCast(symbol, ILocalSymbol))
                ElseIf TypeOf symbol Is IFieldSymbol Then
                    Return GetInitializerSourcePartsAsync(DirectCast(symbol, IFieldSymbol))
                End If

                Return SpecializedTasks.Default(Of IEnumerable(Of SymbolDisplayPart))()
            End Function

            Private Async Function GetFirstDeclarationAsync(Of T As SyntaxNode)(symbol As ISymbol) As Task(Of T)
                For Each syntaxRef In symbol.DeclaringSyntaxReferences
                    Dim syntax = Await syntaxRef.GetSyntaxAsync(Me.CancellationToken).ConfigureAwait(False)
                    Dim casted = TryCast(syntax, T)
                    If casted IsNot Nothing Then
                        Return casted
                    End If
                Next

                Return Nothing
            End Function

            Private Async Function GetDeclarationsAsync(Of T As SyntaxNode)(symbol As ISymbol) As Task(Of List(Of T))
                Dim list = New list(Of T)()
                For Each syntaxRef In symbol.DeclaringSyntaxReferences
                    Dim syntax = Await syntaxRef.GetSyntaxAsync(Me.CancellationToken).ConfigureAwait(False)
                    Dim casted = TryCast(syntax, T)
                    If casted IsNot Nothing Then
                        list.Add(casted)
                    End If
                Next

                Return list
            End Function

            Private Overloads Async Function GetInitializerSourcePartsAsync(symbol As IParameterSymbol) As Task(Of IEnumerable(Of SymbolDisplayPart))
                Dim syntax = Await GetFirstDeclarationAsync(Of ParameterSyntax)(symbol).ConfigureAwait(False)
                If syntax IsNot Nothing Then
                    Return Await GetInitializerSourcePartsAsync(syntax.Default).ConfigureAwait(False)
                End If

                Return Nothing
            End Function

            Private Overloads Async Function GetInitializerSourcePartsAsync(symbol As ILocalSymbol) As Task(Of IEnumerable(Of SymbolDisplayPart))
                Dim ids = Await GetDeclarationsAsync(Of ModifiedIdentifierSyntax)(symbol).ConfigureAwait(False)
                Dim syntax = ids.Select(Function(i) i.Parent).OfType(Of VariableDeclaratorSyntax).FirstOrDefault()
                If syntax IsNot Nothing Then
                    Return Await GetInitializerSourcePartsAsync(syntax.Initializer).ConfigureAwait(False)
                End If

                Return Nothing
            End Function

            Private Overloads Async Function GetInitializerSourcePartsAsync(symbol As IFieldSymbol) As Task(Of IEnumerable(Of SymbolDisplayPart))
                Dim ids = Await GetDeclarationsAsync(Of ModifiedIdentifierSyntax)(symbol).ConfigureAwait(False)
                Dim variableDeclarator = ids.Select(Function(i) i.Parent).OfType(Of VariableDeclaratorSyntax).FirstOrDefault()
                If variableDeclarator IsNot Nothing Then
                    Return Await GetInitializerSourcePartsAsync(variableDeclarator.Initializer).ConfigureAwait(False)
                End If

                Dim enumMemberDeclaration = Await GetFirstDeclarationAsync(Of EnumMemberDeclarationSyntax)(symbol).ConfigureAwait(False)
                If enumMemberDeclaration IsNot Nothing Then
                    Return Await GetInitializerSourcePartsAsync(enumMemberDeclaration.Initializer).ConfigureAwait(False)
                End If

                Return Nothing
            End Function

            Private Overloads Async Function GetInitializerSourcePartsAsync(equalsValue As EqualsValueSyntax) As Task(Of IEnumerable(Of SymbolDisplayPart))
                If equalsValue IsNot Nothing AndAlso equalsValue.Value IsNot Nothing Then
                    Dim semanticModel = GetSemanticModel(equalsValue.SyntaxTree)
                    If semanticModel Is Nothing Then
                        Return Nothing
                    End If

                    Dim classifications = Classifier.GetClassifiedSpans(semanticModel, equalsValue.Value.Span, Me.Workspace, Me.CancellationToken)
                    Dim text = Await semanticModel.SyntaxTree.GetTextAsync(Me.CancellationToken).ConfigureAwait(False)
                    Return ConvertClassifications(text, classifications)
                End If

                Return Nothing
            End Function

            Protected Overrides Sub AddAwaitableUsageText(method As IMethodSymbol, semanticModel As SemanticModel, position As Integer)
                AddToGroup(SymbolDescriptionGroups.AwaitableUsageText,
                    method.ToAwaitableParts(VBFeaturesResources.Await, "r", semanticModel, position))
            End Sub

            Protected Overrides ReadOnly Property MinimallyQualifiedFormat As SymbolDisplayFormat
                Get
                    Return s_minimallyQualifiedFormat
                End Get
            End Property

            Protected Overrides ReadOnly Property MinimallyQualifiedFormatWithConstants As SymbolDisplayFormat
                Get
                    Return s_minimallyQualifiedFormatWithConstants
                End Get
            End Property
        End Class
    End Class
End Namespace
