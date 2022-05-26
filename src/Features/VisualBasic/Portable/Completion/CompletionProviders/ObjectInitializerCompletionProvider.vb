' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(ObjectInitializerCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(PreprocessorCompletionProvider))>
    <[Shared]>
    Friend Class ObjectInitializerCompletionProvider
        Inherits AbstractObjectInitializerCompletionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected Overrides Function GetInitializedMembers(tree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As HashSet(Of String)
            Dim token = tree.FindTokenOnLeftOfPosition(position, cancellationToken)
            token = token.GetPreviousTokenIfTouchingWord(position)

            ' The dot wasn't part of the identifier, so move over one more to get the , or {
            If token.Kind = SyntaxKind.DotToken Then
                token = token.GetPreviousToken()
            End If

            If token.Kind <> SyntaxKind.CommaToken AndAlso token.Kind <> SyntaxKind.OpenBraceToken Then
                Return New HashSet(Of String)
            End If

            Dim initializer = TryCast(token.Parent, ObjectMemberInitializerSyntax)
            If initializer Is Nothing Then
                Return New HashSet(Of String)()
            End If

            Return New HashSet(Of String)(initializer.Initializers.OfType(Of NamedFieldInitializerSyntax)().Select(Function(i) i.Name.Identifier.ValueText))
        End Function

        Protected Overrides Function GetInitializedType(document As Document,
                                                        semanticModel As SemanticModel,
                                                        position As Integer,
                                                        cancellationToken As CancellationToken) As Tuple(Of ITypeSymbol, Location)
            Dim tree = semanticModel.SyntaxTree
            If tree.IsInNonUserCode(position, cancellationToken) Then
                Return Nothing
            End If

            Dim token = tree.FindTokenOnLeftOfPosition(position, cancellationToken)
            token = token.GetPreviousTokenIfTouchingWord(position)

            ' We should have gotten a ".", since that all we want to come up on 
            If token.Kind <> SyntaxKind.DotToken Then
                Return Nothing
            End If

            ' The dot must be following a comma or open brace
            Dim commaOrBrace = token.GetPreviousToken()
            If commaOrBrace.Kind <> SyntaxKind.CommaToken AndAlso commaOrBrace.Kind <> SyntaxKind.OpenBraceToken Then
                Return Nothing
            End If

            ' We have the right tokens. Get the containing object initializer. Will we be able to walk
            ' up and determine the type?
            Dim containingInitializer = commaOrBrace.Parent
            If containingInitializer Is Nothing OrElse
                containingInitializer.Parent Is Nothing OrElse
                containingInitializer.Kind <> SyntaxKind.ObjectMemberInitializer Then
                Return Nothing
            End If

            ' Get the grandparent object creation expression
            Dim objectCreationExpression = TryCast(containingInitializer.Parent, ObjectCreationExpressionSyntax)
            If objectCreationExpression Is Nothing Then
                Return Nothing
            End If

            Dim initializerLocation As Location = token.GetLocation()
            Dim symbolInfo = semanticModel.GetSymbolInfo(objectCreationExpression.Type, cancellationToken)
            Dim symbol = TryCast(symbolInfo.Symbol, ITypeSymbol)
            Return Tuple.Create(symbol, initializerLocation)
        End Function

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Return text(characterPosition) = "."c
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = ImmutableHashSet.Create("."c)

        Protected Overrides Function IsExclusiveAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of Boolean)
            ' Object initializers are explicitly indicated by "With", so we're always exclusive.
            Return SpecializedTasks.True
        End Function

        Protected Overrides Function IsInitializable(member As ISymbol, containingType As INamedTypeSymbol) As Boolean
            ' Unlike CSharp, we don't want to suggest readonly members, even if they are Collections
            Return member.IsWriteableFieldOrProperty() AndAlso
                IsValidProperty(member) AndAlso
                Not member.IsStatic AndAlso
                member.IsAccessibleWithin(containingType)
        End Function

        Protected Overrides Function EscapeIdentifier(symbol As ISymbol) As String
            Return symbol.Name.EscapeIdentifier()
        End Function

        Private Shared Function IsValidProperty(member As ISymbol) As Boolean
            Dim [property] = TryCast(member, IPropertySymbol)
            If [property] IsNot Nothing Then
                Return [property].Parameters.IsDefaultOrEmpty OrElse [property].Parameters.All(Function(p) p.IsOptional OrElse p.IsParams)
            End If

            Return True
        End Function

    End Class
End Namespace
