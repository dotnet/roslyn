' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.GenerateFromMembers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateToStringFromMembers
    ''' <summary>
    ''' Offers to generate an explicit <see cref="Object.ToString"/> from the selected
    ''' members of a class or structure, so its logic can be customized instead of
    ''' written from scratch. Visual Basic has no records, so there is no
    ''' <c>PrintMembers</c> to offer here.
    ''' </summary>
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.GenerateToStringFromMembers), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers)>
    Friend NotInheritable Class GenerateToStringFromMembersCodeRefactoringProvider
        Inherits CodeRefactoringProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides Async Function ComputeRefactoringsAsync(context As CodeRefactoringContext) As Task
            Dim document = context.Document
            Dim textSpan = context.Span
            Dim cancellationToken = context.CancellationToken

            If document.Project.Solution.WorkspaceKind = WorkspaceKind.MiscellaneousFiles Then
                Return
            End If

            Dim actions = Await GenerateToStringFromMembersAsync(document, textSpan, cancellationToken).ConfigureAwait(False)
            context.RegisterRefactorings(actions)

            If actions.IsDefaultOrEmpty AndAlso textSpan.IsEmpty Then
                Await HandleNonSelectionAsync(context).ConfigureAwait(False)
            End If
        End Function

        Private Shared Async Function HandleNonSelectionAsync(context As CodeRefactoringContext) As Task
            Dim document = context.Document
            Dim textSpan = context.Span
            Dim cancellationToken = context.CancellationToken

            Dim helpers = document.GetRequiredLanguageService(Of IRefactoringHelpersService)()
            Dim root = Await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            ' We offer the refactoring when the user is on the header of a class or structure.
            ' We deliberately do not offer it between members: the caret landing there
            ' is too weak a signal, and the offering would show up in every gap
            ' between members of every type.
            Dim typeDeclaration As SyntaxNode = Nothing
            If Not helpers.IsOnTypeHeader(root, textSpan.Start, typeDeclaration) Then
                Return
            End If

            Dim semanticModel = Await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            Dim containingType = TryCast(semanticModel.GetDeclaredSymbol(typeDeclaration), INamedTypeSymbol)
            If containingType Is Nothing OrElse
               (containingType.TypeKind <> TypeKind.Class AndAlso containingType.TypeKind <> TypeKind.Structure) OrElse
               containingType.IsStatic Then
                Return
            End If

            If Not Await IsStringBuilderAvailableAsync(document, cancellationToken).ConfigureAwait(False) Then
                Return
            End If

            ' Generate from the readable instance fields and properties declared in
            ' the type itself.
            Dim builder = ImmutableArray.CreateBuilder(Of ISymbol)()
            For Each member In containingType.GetMembers()
                If GenerateFromMembersHelpers.IsReadableInstanceFieldOrProperty(member) AndAlso
                   SymbolEqualityComparer.Default.Equals(member.ContainingType, containingType) Then
                    builder.Add(member)
                End If
            Next

            If builder.Count = 0 Then
                Return
            End If

            Dim actions = CreateActions(document, typeDeclaration, containingType, builder.ToImmutable())
            context.RegisterRefactorings(actions, textSpan)
        End Function

        Private Shared Async Function IsStringBuilderAvailableAsync(document As Document, cancellationToken As CancellationToken) As Task(Of Boolean)
            Dim compilation = Await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(False)
            Return compilation.GetTypeByMetadataName("System.Text.StringBuilder") IsNot Nothing
        End Function

        Private Shared Async Function GenerateToStringFromMembersAsync(
            document As Document,
            textSpan As TextSpan,
            cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of CodeAction))

            Dim info = Await GenerateFromMembersHelpers.GetSelectedMemberInfoAsync(document, textSpan, False, cancellationToken).ConfigureAwait(False)
            If info IsNot Nothing AndAlso
               info.SelectedMembers.All(Function(m) GenerateFromMembersHelpers.IsReadableInstanceFieldOrProperty(m)) Then

                Dim containingType = info.ContainingType
                If containingType IsNot Nothing AndAlso
                   (containingType.TypeKind = TypeKind.Class OrElse containingType.TypeKind = TypeKind.Structure) AndAlso
                   Not containingType.IsStatic Then

                    If Not Await IsStringBuilderAvailableAsync(document, cancellationToken).ConfigureAwait(False) Then
                        Return ImmutableArray(Of CodeAction).Empty
                    End If

                    Dim syntaxFacts = document.GetRequiredLanguageService(Of ISyntaxFactsService)()
                    Dim root = Await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
                    Dim typeDeclaration = syntaxFacts.GetContainingTypeDeclaration(root, textSpan.Start)
                    RoslynDebug.AssertNotNull(typeDeclaration)

                    Return CreateActions(document, typeDeclaration, containingType, info.SelectedMembers)
                End If
            End If

            Return ImmutableArray(Of CodeAction).Empty
        End Function

        Private Shared Function CreateActions(
            document As Document,
            typeDeclaration As SyntaxNode,
            containingType As INamedTypeSymbol,
            members As ImmutableArray(Of ISymbol)) As ImmutableArray(Of CodeAction)

            If HasToString(containingType) Then
                Return ImmutableArray(Of CodeAction).Empty
            End If

            Return ImmutableArray.Create(Of CodeAction)(
                New GenerateToStringAction(document, typeDeclaration, containingType, members))
        End Function

        Private Shared Function HasToString(containingType As INamedTypeSymbol) As Boolean
            For Each member In containingType.GetMembers(NameOf(Object.ToString))
                Dim method = TryCast(member, IMethodSymbol)
                If method IsNot Nothing AndAlso
                   method.Parameters.Length = 0 AndAlso
                   Not method.IsStatic AndAlso
                   Not method.IsImplicitlyDeclared Then
                    Return True
                End If
            Next

            Return False
        End Function
    End Class
End Namespace
