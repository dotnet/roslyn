' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.EncapsulateField
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EncapsulateField
    <ExportLanguageService(GetType(AbstractEncapsulateFieldService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEncapsulateFieldService
        Inherits AbstractEncapsulateFieldService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Async Function RewriteFieldNameAndAccessibilityAsync(
                originalFieldName As String,
                makePrivate As Boolean,
                document As Document,
                declarationAnnotation As SyntaxAnnotation,
                fallbackOptions As CodeAndImportGenerationOptionsProvider,
                cancellationToken As CancellationToken) As Task(Of SyntaxNode)

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim fieldIdentifier = root.GetAnnotatedNodes(Of ModifiedIdentifierSyntax)(declarationAnnotation).FirstOrDefault()

            ' There may be no field to rewrite if this document is part of a set of linked files 
            ' and the declaration is not conditionally compiled in this document's project.
            If fieldIdentifier Is Nothing Then
                Return root
            End If

            Dim identifier = fieldIdentifier.Identifier
            Dim annotation = New SyntaxAnnotation()
            Dim escapedName = originalFieldName.EscapeIdentifier()
            Dim newIdentifier = SyntaxFactory.Identifier(
                text:=escapedName,
                isBracketed:=escapedName <> originalFieldName,
                identifierText:=originalFieldName,
                typeCharacter:=TypeCharacter.None)
            root = root.ReplaceNode(fieldIdentifier, fieldIdentifier.WithIdentifier(newIdentifier).WithAdditionalAnnotations(annotation, Formatter.Annotation))
            fieldIdentifier = root.GetAnnotatedNodes(Of ModifiedIdentifierSyntax)(annotation).First()
            If (DirectCast(fieldIdentifier.Parent, VariableDeclaratorSyntax).Names.Count = 1) Then
                Dim fieldDeclaration = DirectCast(fieldIdentifier.Parent.Parent, FieldDeclarationSyntax)

                Dim modifierKinds = {SyntaxKind.FriendKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.PrintStatement, SyntaxKind.PublicKeyword}
                If makePrivate Then
                    Dim useableModifiers = fieldDeclaration.Modifiers.Where(Function(m) Not modifierKinds.Contains(m.Kind))
                    Dim newModifiers = SpecializedCollections.SingletonEnumerable(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)).Concat(useableModifiers)
                    Dim updatedDeclaration = fieldDeclaration.WithModifiers(SyntaxFactory.TokenList(newModifiers)) _
                                                            .WithLeadingTrivia(fieldDeclaration.GetLeadingTrivia()) _
                                                            .WithTrailingTrivia(fieldDeclaration.GetTrailingTrivia()) _
                                                            .WithAdditionalAnnotations(Formatter.Annotation)

                    Return root.ReplaceNode(fieldDeclaration, updatedDeclaration)
                End If
            End If

            Return root
        End Function

        Protected Overrides Async Function GetFieldsAsync(document As Document, span As TextSpan, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of IFieldSymbol))
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            Dim fields = root.DescendantNodes(Function(n) n.Span.IntersectsWith(span)) _
                                                        .OfType(Of FieldDeclarationSyntax)() _
                                                        .Where(Function(n) n.Span.IntersectsWith(span) AndAlso CanEncapsulate(n))

            Dim names As IEnumerable(Of ModifiedIdentifierSyntax)
            If span.IsEmpty Then
                ' no selection, get all variables
                names = fields.SelectMany(Function(f) f.Declarators.SelectMany(Function(d) d.Names))
            Else
                ' has selection, get only the ones that are included in the selection
                names = fields.SelectMany(Function(f) f.Declarators.SelectMany(Function(d) d.Names.Where(Function(n) n.Span.IntersectsWith(span))))
            End If

            Return names.Select(Function(n) semanticModel.GetDeclaredSymbol(n)).
                         OfType(Of IFieldSymbol)().
                         WhereNotNull().
                         Where(Function(f) f.Name.Length > 0).
                         ToImmutableArray()
        End Function

        Private Shared Function CanEncapsulate(field As FieldDeclarationSyntax) As Boolean
            Return TypeOf field.Parent Is TypeBlockSyntax
        End Function

        Protected Shared Function MakeUnique(baseName As String, originalFieldName As String, containingType As INamedTypeSymbol, Optional willChangeFieldName As Boolean = True) As String
            If willChangeFieldName Then
                Return NameGenerator.GenerateUniqueName(baseName, containingType.MemberNames.Where(Function(x) x <> originalFieldName).ToSet(), StringComparer.OrdinalIgnoreCase)
            Else
                Return NameGenerator.GenerateUniqueName(baseName, containingType.MemberNames.ToSet(), StringComparer.OrdinalIgnoreCase)
            End If
        End Function

        Protected Overrides Function GenerateFieldAndPropertyNames(field As IFieldSymbol) As (fieldName As String, propertyName As String)
            ' If the field is marked shadows, it will keep its name.
            If field.DeclaredAccessibility = Accessibility.Private OrElse IsShadows(field) Then
                Dim propertyName = GeneratePropertyName(field.Name)
                propertyName = MakeUnique(propertyName, field)
                Return (field.Name, propertyName)
            Else
                Dim propertyName = GeneratePropertyName(field.Name)
                Dim containingTypeMemberNames = field.ContainingType.GetAccessibleMembersInThisAndBaseTypes(Of ISymbol)(field.ContainingType).Select(Function(s) s.Name)
                propertyName = NameGenerator.GenerateUniqueName(propertyName, containingTypeMemberNames.Where(Function(m) m <> field.Name).ToSet(), StringComparer.OrdinalIgnoreCase)

                Dim newFieldName = MakeUnique("_" + Char.ToLower(propertyName(0)) + propertyName.Substring(1), field)
                Return (newFieldName, propertyName)
            End If
        End Function

        Private Shared Function IsShadows(field As IFieldSymbol) As Boolean
            Return field.DeclaringSyntaxReferences.Any(Function(d) d.GetSyntax().GetAncestor(Of FieldDeclarationSyntax)().Modifiers.Any(SyntaxKind.ShadowsKeyword))
        End Function

        Private Shared Function MakeUnique(propertyName As String, field As IFieldSymbol) As String
            Dim containingTypeMemberNames = field.ContainingType.GetAccessibleMembersInThisAndBaseTypes(Of ISymbol)(field.ContainingType).Select(Function(s) s.Name)
            Return NameGenerator.GenerateUniqueName(propertyName, containingTypeMemberNames.ToSet(), StringComparer.OrdinalIgnoreCase)
        End Function

        Protected Overrides Function GetConstructorNodes(containingType As INamedTypeSymbol) As IEnumerable(Of SyntaxNode)
            Return containingType.Constructors.SelectMany(Function(c As IMethodSymbol)
                                                              Return c.DeclaringSyntaxReferences.Select(Function(d) d.GetSyntax().Parent)
                                                          End Function)
        End Function
    End Class
End Namespace
