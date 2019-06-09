' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic
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

            Private Shared ReadOnly s_minimallyQualifiedFormatWithConstantsAndModifiers As SymbolDisplayFormat = s_minimallyQualifiedFormatWithConstants _
                .AddMemberOptions(SymbolDisplayMemberOptions.IncludeModifiers)

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
                    PlainText(VBFeaturesResources.Awaitable_Extension),
                    Punctuation(">"),
                    Space())
            End Sub

            Protected Overrides Function GetInitializerSourcePartsAsync(symbol As ISymbol) As Task(Of ImmutableArray(Of SymbolDisplayPart))
                If TypeOf symbol Is IParameterSymbol Then
                    Return GetInitializerSourcePartsAsync(DirectCast(symbol, IParameterSymbol))
                ElseIf TypeOf symbol Is ILocalSymbol Then
                    Return GetInitializerSourcePartsAsync(DirectCast(symbol, ILocalSymbol))
                ElseIf TypeOf symbol Is IFieldSymbol Then
                    Return GetInitializerSourcePartsAsync(DirectCast(symbol, IFieldSymbol))
                End If

                Return SpecializedTasks.EmptyImmutableArray(Of SymbolDisplayPart)
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
                Dim list = New List(Of T)()
                For Each syntaxRef In symbol.DeclaringSyntaxReferences
                    Dim syntax = Await syntaxRef.GetSyntaxAsync(Me.CancellationToken).ConfigureAwait(False)
                    Dim casted = TryCast(syntax, T)
                    If casted IsNot Nothing Then
                        list.Add(casted)
                    End If
                Next

                Return list
            End Function

            Private Overloads Async Function GetInitializerSourcePartsAsync(symbol As IParameterSymbol) As Task(Of ImmutableArray(Of SymbolDisplayPart))
                Dim syntax = Await GetFirstDeclarationAsync(Of ParameterSyntax)(symbol).ConfigureAwait(False)
                If syntax IsNot Nothing Then
                    Return Await GetInitializerSourcePartsAsync(syntax.Default).ConfigureAwait(False)
                End If

                Return ImmutableArray(Of SymbolDisplayPart).Empty
            End Function

            Private Overloads Async Function GetInitializerSourcePartsAsync(symbol As ILocalSymbol) As Task(Of ImmutableArray(Of SymbolDisplayPart))
                Dim ids = Await GetDeclarationsAsync(Of ModifiedIdentifierSyntax)(symbol).ConfigureAwait(False)
                Dim syntax = ids.Select(Function(i) i.Parent).OfType(Of VariableDeclaratorSyntax).FirstOrDefault()
                If syntax IsNot Nothing Then
                    Return Await GetInitializerSourcePartsAsync(syntax.Initializer).ConfigureAwait(False)
                End If

                Return Nothing
            End Function

            Private Overloads Async Function GetInitializerSourcePartsAsync(symbol As IFieldSymbol) As Task(Of ImmutableArray(Of SymbolDisplayPart))
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

            Private Overloads Async Function GetInitializerSourcePartsAsync(equalsValue As EqualsValueSyntax) As Task(Of ImmutableArray(Of SymbolDisplayPart))
                If equalsValue IsNot Nothing AndAlso equalsValue.Value IsNot Nothing Then
                    Dim semanticModel = GetSemanticModel(equalsValue.SyntaxTree)
                    If semanticModel IsNot Nothing Then
                        Return Await Classifier.GetClassifiedSymbolDisplayPartsAsync(
                            semanticModel, equalsValue.Value.Span,
                            Me.Workspace, cancellationToken:=Me.CancellationToken).ConfigureAwait(False)
                    End If
                End If

                Return Nothing
            End Function

            Protected Overrides Sub AddAwaitableUsageText(method As IMethodSymbol, semanticModel As SemanticModel, position As Integer)
                AddToGroup(SymbolDescriptionGroups.AwaitableUsageText,
                    method.ToAwaitableParts(SyntaxFacts.GetText(SyntaxKind.AwaitKeyword), "r", semanticModel, position))
            End Sub

            Protected Overrides Sub AddCaptures(symbol As ISymbol)
                Dim method = TryCast(symbol, IMethodSymbol)
                If method IsNot Nothing AndAlso method.ContainingSymbol.IsKind(SymbolKind.Method) Then
                    Dim syntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()
                    AddCaptures(syntax)
                End If
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

            Protected Overrides ReadOnly Property MinimallyQualifiedFormatWithConstantsAndModifiers As SymbolDisplayFormat
                Get
                    Return s_minimallyQualifiedFormatWithConstantsAndModifiers
                End Get
            End Property
        End Class
    End Class
End Namespace
