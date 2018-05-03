' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
Imports Microsoft.CodeAnalysis.EncapsulateField
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EncapsulateField
    <ExportLanguageService(GetType(AbstractEncapsulateFieldService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEncapsulateFieldService
        Inherits AbstractEncapsulateFieldService

        Protected Overrides Async Function RewriteFieldNameAndAccessibility(originalFieldName As String,
                                                        makePrivate As Boolean,
                                                        document As Document,
                                                        declarationAnnotation As SyntaxAnnotation,
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

        Protected Overrides Async Function GetFieldsAsync(document As Document, span As TextSpan, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of IFieldSymbol))
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

            Return names.Select(Function(n) semanticModel.GetDeclaredSymbol(n)) _
                                                        .OfType(Of IFieldSymbol)() _
                                                        .WhereNotNull() _
                                                        .Where(Function(f) f.Name.Length > 0)
        End Function

        Private Function CanEncapsulate(field As FieldDeclarationSyntax) As Boolean
            Return TypeOf field.Parent Is TypeBlockSyntax
        End Function

        Private Shared Function GetNamingRules(document As Document, cancelationToken As CancellationToken) As ImmutableArray(Of NamingRule)
            Return document.GetNamingRulesAsync(cancelationToken).GetAwaiter().GetResult() _
                .AddRange(DefaultNamingRules.FieledAndPropertyRules)
        End Function

        Protected Overrides Function GeneratePropertyAndFieldNames(field As IFieldSymbol, document As Document, cancellationToken As CancellationToken) As Tuple(Of String, String)
            ' If the field is marked shadows, it will keep its name.
            Dim namingRules = GetNamingRules(document, cancellationToken)
            If field.DeclaredAccessibility = Accessibility.Private OrElse IsShadows(field) Then
                Dim propertyName = GeneratePropertyName(field.Name)
                propertyName = MakeUnique(propertyName, field, namingRules, SymbolKind.Property, Accessibility.Public)
                Return Tuple.Create(field.Name, propertyName)
            Else
                Dim basePropertyName = GeneratePropertyName(field.Name)
                Dim containingTypeMemberNames = field.ContainingType.GetAccessibleMembersInThisAndBaseTypes(Of ISymbol)(field.ContainingType).Select(Function(s) s.Name)
                Dim propertyName = NameGenerator.GenerateUniqueName(
                    basePropertyName,
                    containingTypeMemberNames.Where(Function(m) m <> field.Name).ToSet(),
                    StringComparer.OrdinalIgnoreCase,
                    Function(name)
                        Return NameGenerator.GenerateName(basePropertyName, namingRules, SymbolKind.Property, Accessibility.Public)
                    End Function)

                Dim newFieldName = MakeUnique(basePropertyName, field, namingRules, SymbolKind.Field, Accessibility.Private)
                Return Tuple.Create(newFieldName, propertyName)
            End If
        End Function

        Private Function IsShadows(field As IFieldSymbol) As Boolean
            Return field.DeclaringSyntaxReferences.Any(Function(d) d.GetSyntax().GetAncestor(Of FieldDeclarationSyntax)().Modifiers.Any(SyntaxKind.ShadowsKeyword))
        End Function

        Private Function MakeUnique(propertyName As String, field As IFieldSymbol, rules As ImmutableArray(Of NamingRule), symbolKind As SymbolKind, accessibility As Accessibility) As String
            Dim containingTypeMemberNames = field.ContainingType.GetAccessibleMembersInThisAndBaseTypes(Of ISymbol)(field.ContainingType).Select(Function(s) s.Name)
            Return NameGenerator.GenerateUniqueName(
                propertyName,
                containingTypeMemberNames.ToSet(),
                StringComparer.OrdinalIgnoreCase,
                Function(name)
                    Return NameGenerator.GenerateName(name, rules, symbolKind, accessibility)
                End Function)
        End Function

        Friend Overrides Function GetConstructorNodes(containingType As INamedTypeSymbol) As IEnumerable(Of SyntaxNode)
            Return containingType.Constructors.SelectMany(Function(c As IMethodSymbol)
                                                              Return c.DeclaringSyntaxReferences.Select(Function(d) d.GetSyntax().Parent)
                                                          End Function)
        End Function

    End Class
End Namespace
