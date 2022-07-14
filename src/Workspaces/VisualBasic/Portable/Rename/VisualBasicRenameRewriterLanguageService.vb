' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Rename

    Friend Class VisualBasicRenameRewriterLanguageService
        Inherits AbstractRenameRewriterLanguageService

        Public Shared ReadOnly Instance As New VisualBasicRenameRewriterLanguageService()

        Private Sub New()
        End Sub

        Public Overrides Function AnnotateAndRename(parameters As RenameRewriterParameters) As SyntaxNode
            Dim renameRewriter = New SymbolsRenameRewriter(parameters)
            Return renameRewriter.Visit(parameters.SyntaxRoot)
        End Function

#Region "Declaration Conflicts"

        Public Overrides Function LocalVariableConflict(
            token As SyntaxToken,
            newReferencedSymbols As IEnumerable(Of ISymbol)) As Boolean

            ' This scenario is not present in VB and only in C#
            Return False
        End Function

        Public Overrides Function ComputeDeclarationConflictsAsync(
            replacementText As String,
            renamedSymbol As ISymbol,
            renameSymbol As ISymbol,
            referencedSymbols As IEnumerable(Of ISymbol),
            baseSolution As Solution,
            newSolution As Solution,
            reverseMappedLocations As IDictionary(Of Location, Location),
            cancellationToken As CancellationToken
        ) As Task(Of ImmutableArray(Of Location))

            Dim conflicts = ArrayBuilder(Of Location).GetInstance()

            If renamedSymbol.Kind = SymbolKind.Parameter OrElse
               renamedSymbol.Kind = SymbolKind.Local OrElse
               renamedSymbol.Kind = SymbolKind.RangeVariable Then

                Dim token = renamedSymbol.Locations.Single().FindToken(cancellationToken)

                ' Find the method block or field declaration that we're in. Note the LastOrDefault
                ' so we find the uppermost one, since VariableDeclarators live in methods too.
                Dim methodBase = token.Parent.AncestorsAndSelf.Where(Function(s) TypeOf s Is MethodBlockBaseSyntax OrElse TypeOf s Is VariableDeclaratorSyntax) _
                                                              .LastOrDefault()

                Dim visitor As New LocalConflictVisitor(token, newSolution, cancellationToken)
                visitor.Visit(methodBase)

                conflicts.AddRange(visitor.ConflictingTokens.Select(Function(t) t.GetLocation()) _
                               .Select(Function(loc) reverseMappedLocations(loc)))

                ' If this is a parameter symbol for a partial method definition, be sure we visited 
                ' the implementation part's body.
                If renamedSymbol.Kind = SymbolKind.Parameter AndAlso
                    renamedSymbol.ContainingSymbol.Kind = SymbolKind.Method Then
                    Dim methodSymbol = DirectCast(renamedSymbol.ContainingSymbol, IMethodSymbol)
                    If methodSymbol.PartialImplementationPart IsNot Nothing Then
                        Dim matchingParameterSymbol = methodSymbol.PartialImplementationPart.Parameters((DirectCast(renamedSymbol, IParameterSymbol)).Ordinal)

                        token = matchingParameterSymbol.Locations.Single().FindToken(cancellationToken)
                        methodBase = token.GetAncestor(Of MethodBlockSyntax)
                        visitor = New LocalConflictVisitor(token, newSolution, cancellationToken)
                        visitor.Visit(methodBase)

                        conflicts.AddRange(visitor.ConflictingTokens.Select(Function(t) t.GetLocation()) _
                                       .Select(Function(loc) reverseMappedLocations(loc)))
                    End If
                End If

                ' in VB parameters of properties are not allowed to be the same as the containing property
                If renamedSymbol.Kind = SymbolKind.Parameter AndAlso
                    renamedSymbol.ContainingSymbol.Kind = SymbolKind.Property AndAlso
                    CaseInsensitiveComparison.Equals(renamedSymbol.ContainingSymbol.Name, renamedSymbol.Name) Then

                    Dim propertySymbol = renamedSymbol.ContainingSymbol

                    While propertySymbol IsNot Nothing
                        conflicts.AddRange(renamedSymbol.ContainingSymbol.Locations _
                                       .Select(Function(loc) reverseMappedLocations(loc)))

                        propertySymbol = propertySymbol.GetOverriddenMember()
                    End While
                End If

            ElseIf renamedSymbol.Kind = SymbolKind.Label Then
                Dim token = renamedSymbol.Locations.Single().FindToken(cancellationToken)
                Dim containingMethod = token.Parent.FirstAncestorOrSelf(Of SyntaxNode)(
                    Function(s) TypeOf s Is MethodBlockBaseSyntax OrElse
                                TypeOf s Is LambdaExpressionSyntax)

                Dim visitor As New LabelConflictVisitor(token)
                visitor.Visit(containingMethod)
                conflicts.AddRange(visitor.ConflictingTokens.Select(Function(t) t.GetLocation()) _
                    .Select(Function(loc) reverseMappedLocations(loc)))

            ElseIf renamedSymbol.Kind = SymbolKind.Method Then
                conflicts.AddRange(
                    DeclarationConflictHelpers.GetMembersWithConflictingSignatures(DirectCast(renamedSymbol, IMethodSymbol), trimOptionalParameters:=True) _
                        .Select(Function(loc) reverseMappedLocations(loc)))

            ElseIf renamedSymbol.Kind = SymbolKind.Property Then
                conflicts.AddRange(
                    DeclarationConflictHelpers.GetMembersWithConflictingSignatures(DirectCast(renamedSymbol, IPropertySymbol), trimOptionalParameters:=True) _
                        .Select(Function(loc) reverseMappedLocations(loc)))
                AddConflictingParametersOfProperties(
                    referencedSymbols.Concat(renameSymbol).Where(Function(sym) sym.Kind = SymbolKind.Property),
                    renamedSymbol.Name,
                    conflicts)

            ElseIf renamedSymbol.Kind = SymbolKind.TypeParameter Then
                For Each location In renamedSymbol.Locations
                    Dim token = location.FindToken(cancellationToken)
                    Dim currentTypeParameter = token.Parent

                    For Each typeParameter In DirectCast(currentTypeParameter.Parent, TypeParameterListSyntax).Parameters
                        If typeParameter IsNot currentTypeParameter AndAlso CaseInsensitiveComparison.Equals(token.ValueText, typeParameter.Identifier.ValueText) Then
                            conflicts.Add(reverseMappedLocations(typeParameter.Identifier.GetLocation()))
                        End If
                    Next
                Next
            End If

            ' if the renamed symbol is a type member, it's name should not conflict with a type parameter
            If renamedSymbol.ContainingType IsNot Nothing AndAlso renamedSymbol.ContainingType.GetMembers(renamedSymbol.Name).Contains(renamedSymbol) Then
                Dim conflictingLocations = renamedSymbol.ContainingType.TypeParameters _
                    .Where(Function(t) CaseInsensitiveComparison.Equals(t.Name, renamedSymbol.Name)) _
                    .SelectMany(Function(t) t.Locations)

                For Each location In conflictingLocations
                    Dim typeParameterToken = location.FindToken(cancellationToken)
                    conflicts.Add(reverseMappedLocations(typeParameterToken.GetLocation()))
                Next
            End If

            Return Task.FromResult(conflicts.ToImmutableAndFree())
        End Function

        Public Overrides Async Function ComputeImplicitReferenceConflictsAsync(
                renameSymbol As ISymbol, renamedSymbol As ISymbol,
                implicitReferenceLocations As IEnumerable(Of ReferenceLocation),
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of Location))

            ' Handle renaming of symbols used for foreach
            Dim implicitReferencesMightConflict = renameSymbol.Kind = SymbolKind.Property AndAlso
                                                CaseInsensitiveComparison.Equals(renameSymbol.Name, "Current")
            implicitReferencesMightConflict = implicitReferencesMightConflict OrElse
                                                (renameSymbol.Kind = SymbolKind.Method AndAlso
                                                    (CaseInsensitiveComparison.Equals(renameSymbol.Name, "MoveNext") OrElse
                                                    CaseInsensitiveComparison.Equals(renameSymbol.Name, "GetEnumerator")))

            ' TODO: handle Dispose for using statement and Add methods for collection initializers.

            If implicitReferencesMightConflict Then
                If Not CaseInsensitiveComparison.Equals(renamedSymbol.Name, renameSymbol.Name) Then
                    For Each implicitReferenceLocation In implicitReferenceLocations
                        Dim token = Await implicitReferenceLocation.Location.SourceTree.GetTouchingTokenAsync(
                            implicitReferenceLocation.Location.SourceSpan.Start, cancellationToken, findInsideTrivia:=False).ConfigureAwait(False)

                        If token.Kind = SyntaxKind.ForKeyword AndAlso token.Parent.IsKind(SyntaxKind.ForEachStatement) Then
                            Return ImmutableArray.Create(DirectCast(token.Parent, ForEachStatementSyntax).Expression.GetLocation())
                        End If
                    Next
                End If
            End If

            Return ImmutableArray(Of Location).Empty
        End Function

#End Region

        ''' <summary>
        ''' Gets the top most enclosing statement as target to call MakeExplicit on.
        ''' It's either the enclosing statement, or if this statement is inside of a lambda expression, the enclosing
        ''' statement of this lambda.
        ''' </summary>
        ''' <param name="token">The token to get the complexification target for.</param>
        Public Overrides Function GetExpansionTargetForLocation(token As SyntaxToken) As SyntaxNode
            Return GetExpansionTarget(token)
        End Function

        Private Shared Function GetExpansionTarget(token As SyntaxToken) As SyntaxNode
            ' get the directly enclosing statement
            Dim enclosingStatement = token.FirstAncestorOrSelf(Function(n) TypeOf (n) Is ExecutableStatementSyntax)

            ' for nodes in a using, for or for each statement, we do not need the enclosing _executable_ statement, which is the whole block.
            ' it's enough to expand the using, for or foreach statement.
            Dim possibleSpecialStatement = token.FirstAncestorOrSelf(Function(n) n.Kind = SyntaxKind.ForStatement OrElse
                                                                                 n.Kind = SyntaxKind.ForEachStatement OrElse
                                                                                 n.Kind = SyntaxKind.UsingStatement OrElse
                                                                                 n.Kind = SyntaxKind.CatchBlock)
            If possibleSpecialStatement IsNot Nothing Then
                If enclosingStatement Is possibleSpecialStatement.Parent Then
                    enclosingStatement = If(possibleSpecialStatement.Kind = SyntaxKind.CatchBlock,
                                                DirectCast(possibleSpecialStatement, CatchBlockSyntax).CatchStatement,
                                                possibleSpecialStatement)
                End If
            End If

            ' see if there's an enclosing lambda expression
            Dim possibleLambdaExpression As SyntaxNode = Nothing
            If enclosingStatement Is Nothing Then
                possibleLambdaExpression = token.FirstAncestorOrSelf(Function(n) TypeOf (n) Is LambdaExpressionSyntax)
            End If

            Dim enclosingCref = token.FirstAncestorOrSelf(Function(n) TypeOf (n) Is CrefReferenceSyntax)
            If enclosingCref IsNot Nothing Then
                Return enclosingCref
            End If

            ' there seems to be no statement above this one. Let's see if we can at least get an SimpleNameSyntax
            Return If(enclosingStatement, If(possibleLambdaExpression, token.FirstAncestorOrSelf(Function(n) TypeOf (n) Is SimpleNameSyntax)))
        End Function

        Public Overrides Function IsRenamableTokenInComment(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.XmlTextLiteralToken, SyntaxKind.XmlNameToken)
        End Function

#Region "Helper Methods"
        Public Overrides Function IsIdentifierValid(replacementText As String, syntaxFactsService As ISyntaxFactsService) As Boolean
            replacementText = SyntaxFacts.MakeHalfWidthIdentifier(replacementText)
            Dim possibleIdentifier As String
            If syntaxFactsService.IsTypeCharacter(replacementText.Last()) Then
                ' We don't allow to use identifiers with type characters
                Return False
            Else
                If replacementText.StartsWith("[", StringComparison.Ordinal) AndAlso replacementText.EndsWith("]", StringComparison.Ordinal) Then
                    possibleIdentifier = replacementText
                Else
                    possibleIdentifier = "[" & replacementText & "]"
                End If
            End If

            ' Make sure we got an identifier. 
            If Not syntaxFactsService.IsValidIdentifier(possibleIdentifier) Then
                ' We still don't have an identifier, so let's fail
                Return False
            End If

            ' This is a valid Identifier
            Return True
        End Function

        Public Overrides Function ComputePossibleImplicitUsageConflicts(
            renamedSymbol As ISymbol,
            semanticModel As SemanticModel,
            originalDeclarationLocation As Location,
            newDeclarationLocationStartingPosition As Integer,
            cancellationToken As CancellationToken) As ImmutableArray(Of Location)

            ' TODO: support other implicitly used methods like dispose
            If CaseInsensitiveComparison.Equals(renamedSymbol.Name, "MoveNext") OrElse
                    CaseInsensitiveComparison.Equals(renamedSymbol.Name, "GetEnumerator") OrElse
                    CaseInsensitiveComparison.Equals(renamedSymbol.Name, "Current") Then

                If TypeOf renamedSymbol Is IMethodSymbol Then
                    If DirectCast(renamedSymbol, IMethodSymbol).IsOverloads AndAlso
                            (renamedSymbol.GetAllTypeArguments().Length <> 0 OrElse
                            DirectCast(renamedSymbol, IMethodSymbol).Parameters.Length <> 0) Then
                        Return ImmutableArray(Of Location).Empty
                    End If
                End If

                If TypeOf renamedSymbol Is IPropertySymbol Then
                    If DirectCast(renamedSymbol, IPropertySymbol).IsOverloads Then
                        Return ImmutableArray(Of Location).Empty
                    End If
                End If

                ' TODO: Partial methods currently only show the location where the rename happens As a conflict.
                '       Consider showing both locations as a conflict.

                Dim baseType = renamedSymbol.ContainingType?.GetBaseTypes().FirstOrDefault()
                If baseType IsNot Nothing Then
                    Dim implicitSymbols = semanticModel.LookupSymbols(
                            newDeclarationLocationStartingPosition,
                            baseType,
                            renamedSymbol.Name) _
                                .Where(Function(sym) Not sym.Equals(renamedSymbol))

                    For Each symbol In implicitSymbols
                        If symbol.GetAllTypeArguments().Length <> 0 Then
                            Continue For
                        End If

                        If symbol.Kind = SymbolKind.Method Then
                            Dim method = DirectCast(symbol, IMethodSymbol)

                            If CaseInsensitiveComparison.Equals(symbol.Name, "MoveNext") Then
                                If Not method.ReturnsVoid AndAlso Not method.Parameters.Any() AndAlso method.ReturnType.SpecialType = SpecialType.System_Boolean Then
                                    Return ImmutableArray.Create(originalDeclarationLocation)
                                End If
                            ElseIf CaseInsensitiveComparison.Equals(symbol.Name, "GetEnumerator") Then
                                ' we are a bit pessimistic here. 
                                ' To be sure we would need to check if the returned type Is having a MoveNext And Current as required by foreach
                                If Not method.ReturnsVoid AndAlso
                                        Not method.Parameters.Any() Then
                                    Return ImmutableArray.Create(originalDeclarationLocation)
                                End If
                            End If

                        ElseIf CaseInsensitiveComparison.Equals(symbol.Name, "Current") Then
                            Dim [property] = DirectCast(symbol, IPropertySymbol)

                            If Not [property].Parameters.Any() AndAlso Not [property].IsWriteOnly Then
                                Return ImmutableArray.Create(originalDeclarationLocation)
                            End If
                        End If
                    Next
                End If
            End If

            Return ImmutableArray(Of Location).Empty
        End Function

        Public Overrides Sub TryAddPossibleNameConflicts(symbol As ISymbol, replacementText As String, possibleNameConflicts As ICollection(Of String))
            Dim halfWidthReplacementText = SyntaxFacts.MakeHalfWidthIdentifier(replacementText)

            Const AttributeSuffix As String = "Attribute"
            Const AttributeSuffixLength As Integer = 9
            Debug.Assert(AttributeSuffixLength = AttributeSuffix.Length, "Assert (AttributeSuffixLength = AttributeSuffix.Length) failed.")

            If replacementText.Length > AttributeSuffixLength AndAlso CaseInsensitiveComparison.Equals(halfWidthReplacementText.Substring(halfWidthReplacementText.Length - AttributeSuffixLength), AttributeSuffix) Then
                Dim conflict = replacementText.Substring(0, replacementText.Length - AttributeSuffixLength)
                If Not possibleNameConflicts.Contains(conflict) Then
                    possibleNameConflicts.Add(conflict)
                End If
            End If

            If symbol.Kind = SymbolKind.Property Then
                For Each conflict In {"_" + replacementText, "get_" + replacementText, "set_" + replacementText}
                    If Not possibleNameConflicts.Contains(conflict) Then
                        possibleNameConflicts.Add(conflict)
                    End If
                Next
            End If

            ' consider both versions of the identifier (escaped and unescaped)
            Dim valueText = replacementText
            Dim kind = SyntaxFacts.GetKeywordKind(replacementText)
            If kind <> SyntaxKind.None Then
                valueText = SyntaxFacts.GetText(kind)
            Else
                Dim name = SyntaxFactory.ParseName(replacementText)
                If name.Kind = SyntaxKind.IdentifierName Then
                    valueText = DirectCast(name, IdentifierNameSyntax).Identifier.ValueText
                End If
            End If

            If Not CaseInsensitiveComparison.Equals(valueText, replacementText) Then
                possibleNameConflicts.Add(valueText)
            End If
        End Sub

        ''' <summary>
        ''' Gets the semantic model for the given node. 
        ''' If the node belongs to the syntax tree of the original semantic model, then returns originalSemanticModel.
        ''' Otherwise, returns a speculative model.
        ''' The assumption for the later case is that span start position of the given node in it's syntax tree is same as
        ''' the span start of the original node in the original syntax tree.
        ''' </summary>
        ''' <param name="node"></param>
        ''' <param name="originalSemanticModel"></param>
        Public Shared Function GetSemanticModelForNode(node As SyntaxNode, originalSemanticModel As SemanticModel) As SemanticModel
            If node.SyntaxTree Is originalSemanticModel.SyntaxTree Then
                ' This is possible if the previous rename phase didn't rewrite any nodes in this tree.
                Return originalSemanticModel
            End If

            Dim syntax = node
            Dim nodeToSpeculate = syntax.GetAncestorsOrThis(Of SyntaxNode).Where(Function(n) SpeculationAnalyzer.CanSpeculateOnNode(n)).LastOrDefault
            If nodeToSpeculate Is Nothing Then
                If syntax.IsKind(SyntaxKind.CrefReference) Then
                    nodeToSpeculate = DirectCast(syntax, CrefReferenceSyntax).Name
                ElseIf syntax.IsKind(SyntaxKind.TypeConstraint) Then
                    nodeToSpeculate = DirectCast(syntax, TypeConstraintSyntax).Type
                Else
                    Return Nothing
                End If
            End If

            Dim isInNamespaceOrTypeContext = SyntaxFacts.IsInNamespaceOrTypeContext(TryCast(syntax, ExpressionSyntax))
            Dim position = nodeToSpeculate.SpanStart
            Return SpeculationAnalyzer.CreateSpeculativeSemanticModelForNode(nodeToSpeculate, DirectCast(originalSemanticModel, SemanticModel), position, isInNamespaceOrTypeContext)
        End Function
#End Region

    End Class

End Namespace
