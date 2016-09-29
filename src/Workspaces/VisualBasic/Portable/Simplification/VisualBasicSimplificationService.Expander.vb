' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicSimplificationService
        Private Class Expander
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _semanticModel As SemanticModel
            Private ReadOnly _expandInsideNode As Func(Of SyntaxNode, Boolean)
            Private ReadOnly _expandParameter As Boolean
            Private ReadOnly _cancellationToken As CancellationToken
            Private ReadOnly _annotationForReplacedAliasIdentifier As SyntaxAnnotation

            Public Sub New(
                semanticModel As SemanticModel,
                expandInsideNode As Func(Of SyntaxNode, Boolean),
                cancellationToken As CancellationToken,
                Optional expandParameter As Boolean = False,
                Optional aliasReplacementAnnotation As SyntaxAnnotation = Nothing)

                MyBase.New(visitIntoStructuredTrivia:=True)

                _semanticModel = semanticModel
                _expandInsideNode = expandInsideNode
                _cancellationToken = cancellationToken
                _expandParameter = expandParameter
                _annotationForReplacedAliasIdentifier = aliasReplacementAnnotation
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                If _expandInsideNode Is Nothing OrElse _expandInsideNode(node) Then
                    Return MyBase.Visit(node)
                End If

                Return node
            End Function

            Private Function AddCast(expression As ExpressionSyntax, targetType As ITypeSymbol, oldExpression As ExpressionSyntax) As ExpressionSyntax
                Dim semanticModel = _semanticModel
                If expression.SyntaxTree IsNot oldExpression.SyntaxTree Then
                    Dim specAnalyzer = New SpeculationAnalyzer(oldExpression, expression, _semanticModel, _cancellationToken)
                    semanticModel = specAnalyzer.SpeculativeSemanticModel

                    If semanticModel Is Nothing Then
                        Return expression
                    End If

                    expression = specAnalyzer.ReplacedExpression
                End If

                Return AddCast(expression, targetType, semanticModel)
            End Function

            Private Shared Function AddCast(expression As ExpressionSyntax, targetType As ITypeSymbol, semanticModel As SemanticModel) As ExpressionSyntax
                Dim wasCastAdded As Boolean = False
                Dim result = expression.CastIfPossible(targetType, expression.SpanStart, semanticModel, wasCastAdded)

                If wasCastAdded Then
                    result = result.Parenthesize()
                End If

                Return result
            End Function

            Private Function AddCasts(expression As ExpressionSyntax, typeInfo As TypeInfo, conversion As Conversion, oldExpression As ExpressionSyntax) As ExpressionSyntax
                Dim result = expression

                If typeInfo.Type IsNot Nothing AndAlso
                   typeInfo.Type.IsAnonymousDelegateType() AndAlso
                   conversion.IsUserDefined AndAlso
                   conversion.IsWidening AndAlso
                   conversion.MethodSymbol IsNot Nothing AndAlso
                   conversion.MethodSymbol.Parameters.Length > 0 Then

                    Dim conversionType = conversion.MethodSymbol.Parameters(0).Type
                    If conversionType IsNot Nothing Then
                        result = AddCast(result, conversionType, oldExpression)
                    End If
                End If

                If typeInfo.ConvertedType IsNot Nothing Then
                    result = AddCast(result, typeInfo.ConvertedType, oldExpression)
                End If

                ' If we didn't add a cast, at least parenthesize the expression.
                If result Is expression Then
                    result = result.Parenthesize()
                End If

                Return result
            End Function

            Public Overrides Function VisitParameter(node As ParameterSyntax) As SyntaxNode
                Dim newNode = DirectCast(MyBase.VisitParameter(node), ParameterSyntax)

                If newNode IsNot Nothing AndAlso newNode.AsClause Is Nothing AndAlso _expandParameter Then
                    Dim newNodeSymbol = _semanticModel.GetDeclaredSymbol(node)
                    If newNodeSymbol IsNot Nothing AndAlso newNodeSymbol.Kind = SymbolKind.Parameter Then
                        Dim symbolType = newNodeSymbol.Type
                        If symbolType IsNot Nothing Then
                            Dim typeSyntax = symbolType.GenerateTypeSyntax(True)
                            Dim asClause = SyntaxFactory.SimpleAsClause(typeSyntax).NormalizeWhitespace()
                            If newNode.Default Is Nothing Then
                                Dim newAsClause = asClause.WithTrailingTrivia(newNode.Identifier.GetTrailingTrivia())
                                Dim newIdentifier = newNode.Identifier.WithTrailingTrivia({SyntaxFactory.WhitespaceTrivia(" ")}.ToSyntaxTriviaList())

                                Return SyntaxFactory.Parameter(newNode.AttributeLists, newNode.Modifiers, newIdentifier, newAsClause, newNode.Default) _
                                    .WithAdditionalAnnotations(Simplifier.Annotation)
                            End If

                            Return SyntaxFactory.Parameter(newNode.AttributeLists, newNode.Modifiers, newNode.Identifier, asClause, newNode.Default).WithAdditionalAnnotations(Simplifier.Annotation)
                        End If
                    End If
                End If

                Return newNode
            End Function

            Public Overrides Function VisitAssignmentStatement(node As AssignmentStatementSyntax) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                Dim newAssignment = DirectCast(MyBase.VisitAssignmentStatement(node), AssignmentStatementSyntax)

                Dim typeInfo = _semanticModel.GetTypeInfo(node.Right)
                Dim conversion = _semanticModel.GetConversion(node.Right)
                Dim newExpression = AddCasts(newAssignment.Right, typeInfo, conversion, node.Right)

                newAssignment = newAssignment _
                    .WithRight(newExpression) _
                    .WithAdditionalAnnotations(Simplifier.Annotation)

                Return newAssignment
            End Function

            Public Overrides Function VisitExpressionStatement(node As ExpressionStatementSyntax) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                Dim newExpressionStatement = DirectCast(MyBase.VisitExpressionStatement(node), ExpressionStatementSyntax)

                If newExpressionStatement.Expression.IsKind(SyntaxKind.InvocationExpression) Then

                    ' move all leading trivia before the call keyword
                    Dim leadingTrivia = newExpressionStatement.GetLeadingTrivia()
                    newExpressionStatement = newExpressionStatement.WithLeadingTrivia({SyntaxFactory.WhitespaceTrivia(" ")}.ToSyntaxTriviaList())

                    Dim callStatement = SyntaxFactory.CallStatement(newExpressionStatement.Expression) _
                        .WithLeadingTrivia(leadingTrivia) _
                        .WithAdditionalAnnotations(Simplifier.Annotation)

                    ' copy over annotations, if any.
                    callStatement = newExpressionStatement.CopyAnnotationsTo(callStatement)
                    Return callStatement
                End If

                Return newExpressionStatement
            End Function

            Public Overrides Function VisitEqualsValue(node As EqualsValueSyntax) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                Dim newEqualsValue = DirectCast(MyBase.VisitEqualsValue(node), EqualsValueSyntax)

                If node.Value IsNot Nothing AndAlso Not node.Value.IsMissing AndAlso
                   newEqualsValue.Value IsNot Nothing AndAlso Not newEqualsValue.IsMissing Then

                    Dim typeInfo = _semanticModel.GetTypeInfo(node.Value)
                    Dim conversion = _semanticModel.GetConversion(node.Value)
                    Dim newValue = AddCasts(newEqualsValue.Value, typeInfo, conversion, node.Value)

                    newEqualsValue = newEqualsValue _
                        .WithValue(newValue) _
                        .WithAdditionalAnnotations(Simplifier.Annotation)

                End If

                Return newEqualsValue
            End Function

            Public Overrides Function VisitInvocationExpression(node As InvocationExpressionSyntax) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                Dim newInvocationExpression = DirectCast(MyBase.VisitInvocationExpression(node), InvocationExpressionSyntax)

                ' the argument for redim needs to be an LValue and therefore cannot be a cast or parenthesized expression.
                If node.IsParentKind(SyntaxKind.ReDimKeyword) Then
                    Return newInvocationExpression
                End If

                If newInvocationExpression.ArgumentList Is Nothing Then
                    Dim trailingTrivia = newInvocationExpression.GetTrailingTrivia()

                    newInvocationExpression = newInvocationExpression _
                        .WithTrailingTrivia(SyntaxTriviaList.Empty) _
                        .WithArgumentList(SyntaxFactory.ArgumentList().WithTrailingTrivia(trailingTrivia)) _
                        .WithAdditionalAnnotations(Simplifier.Annotation)
                End If

                If node.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                    Dim memberAccess = DirectCast(node.Expression, MemberAccessExpressionSyntax)
                    Dim targetSymbol = SimplificationHelpers.GetOriginalSymbolInfo(_semanticModel, memberAccess.Name)

                    If Not targetSymbol Is Nothing And targetSymbol.IsReducedExtension() AndAlso memberAccess.Expression IsNot Nothing Then
                        newInvocationExpression = RewriteExtensionMethodInvocation(node, newInvocationExpression, memberAccess.Expression, DirectCast(newInvocationExpression.Expression, MemberAccessExpressionSyntax).Expression, DirectCast(targetSymbol, IMethodSymbol))
                    End If
                End If

                Return newInvocationExpression
            End Function

            Private Function RewriteExtensionMethodInvocation(
                originalNode As InvocationExpressionSyntax,
                rewrittenNode As InvocationExpressionSyntax,
                oldThisExpression As ExpressionSyntax,
                thisExpression As ExpressionSyntax,
                reducedExtensionMethod As IMethodSymbol) As InvocationExpressionSyntax

                Dim originalMemberAccess = DirectCast(originalNode.Expression, MemberAccessExpressionSyntax)
                If originalMemberAccess.GetCorrespondingConditionalAccessExpression IsNot Nothing Then
                    ' Bail out on extension method invocations in conditional access expression.
                    ' Note that this is a temporary workaround for https://github.com/dotnet/roslyn/issues/2593.
                    ' Issue https//github.com/dotnet/roslyn/issues/3260 tracks fixing this workaround.
                    Return rewrittenNode
                End If

                Dim expression = RewriteExtensionMethodInvocation(rewrittenNode, oldThisExpression, thisExpression, reducedExtensionMethod, typeNameFormatWithoutGenerics)

                Dim binding = _semanticModel.GetSpeculativeSymbolInfo(originalNode.SpanStart, expression, SpeculativeBindingOption.BindAsExpression)

                If (Not binding.Symbol Is Nothing) Then
                    Return expression
                End If

                ' The previous binding did not work. So we are going to include the type arguments as well
                Return RewriteExtensionMethodInvocation(rewrittenNode, oldThisExpression, thisExpression, reducedExtensionMethod, typeNameFormatWithGenerics)
            End Function

            Private Function RewriteExtensionMethodInvocation(
            originalNode As InvocationExpressionSyntax,
            oldThisExpression As ExpressionSyntax,
            thisExpression As ExpressionSyntax,
            reducedExtensionMethod As IMethodSymbol,
            symbolDisplayFormat As SymbolDisplayFormat) As InvocationExpressionSyntax

                Dim containingType = reducedExtensionMethod.ContainingType.ToDisplayString(symbolDisplayFormat)
                Dim oldMemberAccess = DirectCast(originalNode.Expression, MemberAccessExpressionSyntax)
                Dim newMemberAccess = SyntaxFactory.SimpleMemberAccessExpression(SyntaxFactory.ParseExpression(containingType), oldMemberAccess.OperatorToken, oldMemberAccess.Name).WithLeadingTrivia(thisExpression.GetFirstToken().LeadingTrivia)

                ' Copies the annotation for the member access expression
                newMemberAccess = originalNode.Expression.CopyAnnotationsTo(newMemberAccess).WithAdditionalAnnotations(Simplifier.Annotation)

                Dim typeInfo = _semanticModel.GetTypeInfo(oldThisExpression)
                Dim conversion = _semanticModel.GetConversion(oldThisExpression)

                Dim castedThisExpression = AddCasts(thisExpression, typeInfo, conversion, oldThisExpression)

                Dim thisArgument = SyntaxFactory.SimpleArgument(castedThisExpression) _
                    .WithLeadingTrivia(SyntaxTriviaList.Empty) _
                    .WithAdditionalAnnotations(Simplifier.Annotation)

                thisArgument = DirectCast(originalNode.Expression, MemberAccessExpressionSyntax).Expression.CopyAnnotationsTo(thisArgument)

                Dim arguments = originalNode.ArgumentList.Arguments.Insert(0, thisArgument)
                Dim replacementNode = SyntaxFactory.InvocationExpression(
                    newMemberAccess,
                    originalNode.ArgumentList.WithArguments(arguments))
                Return originalNode.CopyAnnotationsTo(replacementNode).WithAdditionalAnnotations(Simplifier.Annotation)
            End Function

            Public Overrides Function VisitObjectCreationExpression(node As ObjectCreationExpressionSyntax) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                Dim newObjectCreationExpression = DirectCast(MyBase.VisitObjectCreationExpression(node), ObjectCreationExpressionSyntax)

                If newObjectCreationExpression.ArgumentList Is Nothing Then
                    Dim trailingTrivia = newObjectCreationExpression.Type.GetTrailingTrivia()

                    newObjectCreationExpression = newObjectCreationExpression _
                        .WithType(newObjectCreationExpression.Type.WithTrailingTrivia(SyntaxTriviaList.Empty)) _
                        .WithArgumentList(SyntaxFactory.ArgumentList().WithTrailingTrivia(trailingTrivia)) _
                        .WithAdditionalAnnotations(Simplifier.Annotation)
                End If

                Return newObjectCreationExpression
            End Function

            Public Overrides Function VisitSimpleArgument(node As SimpleArgumentSyntax) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                Dim newSimpleArgument = DirectCast(MyBase.VisitSimpleArgument(node), SimpleArgumentSyntax)

                ' We need to be careful here. if this is a local, field or property passed to a ByRef argument, we shouldn't
                ' parenthesize to avoid breaking copy-back semantics.
                Dim symbol = _semanticModel.GetSymbolInfo(node.Expression, _cancellationToken).Symbol
                If symbol IsNot Nothing Then
                    If symbol.MatchesKind(SymbolKind.Local, SymbolKind.Field, SymbolKind.Property) Then
                        Dim parameter = node.DetermineParameter(_semanticModel, cancellationToken:=_cancellationToken)

                        If parameter IsNot Nothing AndAlso
                           parameter.RefKind <> RefKind.None Then

                            Return newSimpleArgument
                        End If
                    End If
                End If

                If newSimpleArgument.Expression.Kind = SyntaxKind.AddressOfExpression Then
                    Return newSimpleArgument
                End If

                Dim typeInfo = _semanticModel.GetTypeInfo(node.Expression)
                Dim conversion = _semanticModel.GetConversion(node.Expression)

                Dim newExpression = AddCasts(newSimpleArgument.Expression, typeInfo, conversion, node.Expression)

                newSimpleArgument = newSimpleArgument _
                    .WithExpression(newExpression) _
                    .WithAdditionalAnnotations(Simplifier.Annotation)

                Return newSimpleArgument
            End Function

            Public Overrides Function VisitGenericName(node As GenericNameSyntax) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                Dim newNode = DirectCast(MyBase.VisitGenericName(node), SimpleNameSyntax)
                Return VisitSimpleName(newNode, node)
            End Function

            Public Overrides Function VisitSingleLineLambdaExpression(node As SingleLineLambdaExpressionSyntax) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                Dim baseSingleLineLambda = DirectCast(MyBase.VisitSingleLineLambdaExpression(node), SingleLineLambdaExpressionSyntax)

                Dim newSingleLineLambda = baseSingleLineLambda _
                                          .Parenthesize()

                Return newSingleLineLambda
            End Function

            Public Overrides Function VisitQualifiedName(node As QualifiedNameSyntax) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                Dim rewrittenQualifiedName = MyBase.VisitQualifiedName(node)

                Dim symbolForQualifiedName = _semanticModel.GetSymbolInfo(node).Symbol

                If symbolForQualifiedName.IsConstructor Then
                    symbolForQualifiedName = symbolForQualifiedName.ContainingSymbol
                End If

                If symbolForQualifiedName.IsModuleMember Then
                    Dim symbolForLeftPart = _semanticModel.GetSymbolInfo(node.Left).Symbol

                    If Not symbolForQualifiedName.ContainingType.Equals(symbolForLeftPart) Then

                        ' <rewritten_left>.<module_name>.<rewritten_right>
                        Dim moduleIdentifierToken = SyntaxFactory.Identifier(symbolForQualifiedName.ContainingType.Name)
                        moduleIdentifierToken = TryEscapeIdentifierToken(moduleIdentifierToken, _semanticModel)

                        Dim qualifiedNameWithModuleName = rewrittenQualifiedName.CopyAnnotationsTo(SyntaxFactory.QualifiedName(
                            SyntaxFactory.QualifiedName(DirectCast(rewrittenQualifiedName, QualifiedNameSyntax).Left, SyntaxFactory.IdentifierName(moduleIdentifierToken)) _
                                .WithAdditionalAnnotations(Simplifier.Annotation, SimplificationHelpers.SimplifyModuleNameAnnotation),
                            DirectCast(rewrittenQualifiedName, QualifiedNameSyntax).Right))

                        If symbolForQualifiedName.Equals(_semanticModel.GetSpeculativeSymbolInfo(node.SpanStart, qualifiedNameWithModuleName, SpeculativeBindingOption.BindAsExpression).Symbol) Then
                            rewrittenQualifiedName = qualifiedNameWithModuleName
                        End If
                    End If
                End If

                Return rewrittenQualifiedName
            End Function

            Public Overrides Function VisitMemberAccessExpression(node As MemberAccessExpressionSyntax) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                Dim rewrittenMemberAccess = MyBase.VisitMemberAccessExpression(node)

                Dim symbolForMemberAccess = _semanticModel.GetSymbolInfo(node).Symbol

                If node.Expression IsNot Nothing AndAlso symbolForMemberAccess.IsModuleMember Then
                    Dim symbolForLeftPart = _semanticModel.GetSymbolInfo(node.Expression).Symbol

                    If Not symbolForMemberAccess.ContainingType.Equals(symbolForLeftPart) Then

                        ' <rewritten_left>.<module_name>.<rewritten_right>
                        Dim moduleIdentifierToken = SyntaxFactory.Identifier(symbolForMemberAccess.ContainingType.Name)
                        moduleIdentifierToken = TryEscapeIdentifierToken(moduleIdentifierToken, _semanticModel)

                        Dim memberAccessWithModuleName = rewrittenMemberAccess.CopyAnnotationsTo(
                            SyntaxFactory.SimpleMemberAccessExpression(
                                SyntaxFactory.SimpleMemberAccessExpression(
                                    DirectCast(rewrittenMemberAccess, MemberAccessExpressionSyntax).Expression,
                                    node.OperatorToken,
                                    SyntaxFactory.IdentifierName(moduleIdentifierToken)) _
                                        .WithAdditionalAnnotations(Simplifier.Annotation, SimplificationHelpers.SimplifyModuleNameAnnotation),
                                node.OperatorToken,
                                DirectCast(rewrittenMemberAccess, MemberAccessExpressionSyntax).Name))

                        If symbolForMemberAccess.Equals(_semanticModel.GetSpeculativeSymbolInfo(node.SpanStart, memberAccessWithModuleName, SpeculativeBindingOption.BindAsExpression).Symbol) Then
                            rewrittenMemberAccess = memberAccessWithModuleName
                        End If
                    End If
                End If

                Return rewrittenMemberAccess
            End Function

            Public Overrides Function VisitIdentifierName(node As IdentifierNameSyntax) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                Dim newNode = DirectCast(MyBase.VisitIdentifierName(node), SimpleNameSyntax)

                Return VisitSimpleName(newNode, node)
            End Function

            Private Function VisitSimpleName(rewrittenSimpleName As SimpleNameSyntax, originalSimpleName As SimpleNameSyntax) As ExpressionSyntax
                _cancellationToken.ThrowIfCancellationRequested()

                Dim identifier = rewrittenSimpleName.Identifier
                Dim newNode As ExpressionSyntax = rewrittenSimpleName

                '
                ' 1. if this identifier is an alias, we'll expand it here and replace the node completely.
                '
                If originalSimpleName.Kind = SyntaxKind.IdentifierName Then
                    Dim aliasInfo = _semanticModel.GetAliasInfo(DirectCast(originalSimpleName, IdentifierNameSyntax))
                    If aliasInfo IsNot Nothing Then
                        Dim aliasTarget = aliasInfo.Target

                        If aliasTarget.IsNamespace() AndAlso DirectCast(aliasTarget, INamespaceSymbol).IsGlobalNamespace Then
                            Return rewrittenSimpleName
                        End If

                        ' if the enclosing expression is a typeof expression that already contains open type we cannot
                        ' we need to insert an open type as well.
                        Dim typeOfExpression = originalSimpleName.GetAncestor(Of TypeOfExpressionSyntax)()
                        If typeOfExpression IsNot Nothing AndAlso IsTypeOfUnboundGenericType(_semanticModel, typeOfExpression) Then
                            aliasTarget = DirectCast(aliasTarget, INamedTypeSymbol).ConstructUnboundGenericType()
                        End If

                        ' the expanded form replaces the current identifier name.
                        Dim replacement = FullyQualifyIdentifierName(
                            aliasTarget,
                            newNode,
                            originalSimpleName,
                            replaceNode:=True) _
                             .WithAdditionalAnnotations(Simplifier.Annotation)

                        If replacement.Kind = SyntaxKind.QualifiedName Then
                            Dim qualifiedReplacement = DirectCast(replacement, QualifiedNameSyntax)

                            Dim newIdentifier = identifier.CopyAnnotationsTo(qualifiedReplacement.Right.Identifier)

                            If Me._annotationForReplacedAliasIdentifier IsNot Nothing Then
                                newIdentifier = newIdentifier.WithAdditionalAnnotations(Me._annotationForReplacedAliasIdentifier)
                            End If

                            Dim aliasAnnotationInfo = AliasAnnotation.Create(aliasInfo.Name)
                            newIdentifier = newIdentifier.WithAdditionalAnnotations(aliasAnnotationInfo)

                            replacement = replacement.ReplaceNode(
                                 qualifiedReplacement.Right,
                                 qualifiedReplacement.Right.WithIdentifier(newIdentifier))

                            replacement = newNode.CopyAnnotationsTo(replacement)

                            Return replacement
                        End If

                        If replacement.IsKind(SyntaxKind.IdentifierName) Then
                            Dim identifierReplacement = DirectCast(replacement, IdentifierNameSyntax)

                            Dim newIdentifier = identifier.CopyAnnotationsTo(identifierReplacement.Identifier)

                            If Me._annotationForReplacedAliasIdentifier IsNot Nothing Then
                                newIdentifier = newIdentifier.WithAdditionalAnnotations(Me._annotationForReplacedAliasIdentifier)
                            End If

                            Dim aliasAnnotationInfo = AliasAnnotation.Create(aliasInfo.Name)
                            newIdentifier = newIdentifier.WithAdditionalAnnotations(aliasAnnotationInfo)

                            replacement = replacement.ReplaceToken(identifier, newIdentifier)

                            replacement = newNode.CopyAnnotationsTo(replacement)

                            Return replacement
                        End If

                        Throw New NotImplementedException()
                    End If
                End If

                Dim symbol = _semanticModel.GetSymbolInfo(originalSimpleName.Identifier).Symbol
                If symbol Is Nothing Then
                    Return newNode
                End If

                '
                ' 2. If it's an attribute, make sure the identifier matches the attribute's class name without the attribute suffix.
                '
                If originalSimpleName.GetAncestor(Of AttributeSyntax)() IsNot Nothing Then
                    If symbol.IsConstructor() AndAlso symbol.ContainingType?.IsAttribute() Then
                        symbol = symbol.ContainingType
                        Dim name = symbol.Name
                        Debug.Assert(name.StartsWith(originalSimpleName.Identifier.ValueText, StringComparison.Ordinal))

                        ' Note, VB can't escape attribute names like C#, so we actually need to expand to the symbol name
                        ' without a suffix, see http://msdn.microsoft.com/en-us/library/aa711866(v=vs.71).aspx
                        Dim newName = String.Empty
                        If name.TryGetWithoutAttributeSuffix(isCaseSensitive:=False, result:=newName) Then
                            If identifier.ValueText <> newName Then
                                identifier = If(identifier.IsBracketed(),
                                    identifier.CopyAnnotationsTo(SyntaxFactory.BracketedIdentifier(identifier.LeadingTrivia, newName, identifier.TrailingTrivia)),
                                    identifier.CopyAnnotationsTo(SyntaxFactory.Identifier(identifier.LeadingTrivia, newName, identifier.TrailingTrivia)))
                            End If

                            ' if the user already used the Attribute suffix in the attribute, we'll maintain it.
                            If identifier.ValueText = name Then
                                identifier = identifier.WithAdditionalAnnotations(SimplificationHelpers.DontSimplifyAnnotation)
                            End If
                        End If
                    End If
                End If

                '
                ' 3. Always try to escape keyword identifiers
                '
                identifier = TryEscapeIdentifierToken(identifier, Me._semanticModel)
                If identifier <> rewrittenSimpleName.Identifier Then
                    Select Case newNode.Kind
                        Case SyntaxKind.IdentifierName,
                             SyntaxKind.GenericName
                            newNode = DirectCast(newNode, SimpleNameSyntax).WithIdentifier(identifier).WithAdditionalAnnotations(Simplifier.Annotation)

                        Case Else
                            Throw New NotImplementedException()
                    End Select
                End If

                Dim parent = originalSimpleName.Parent

                ' do not complexify further for location where only simple names are allowed
                If (TypeOf (parent) Is FieldInitializerSyntax) OrElse
                    ((TypeOf (parent) Is DeclarationStatementSyntax) AndAlso Not TypeOf (parent) Is InheritsOrImplementsStatementSyntax) OrElse
                    (TypeOf (parent) Is MemberAccessExpressionSyntax AndAlso parent.Kind <> SyntaxKind.SimpleMemberAccessExpression) OrElse
                    (parent.Kind = SyntaxKind.SimpleMemberAccessExpression AndAlso originalSimpleName.IsRightSideOfDot()) OrElse
                    (parent.Kind = SyntaxKind.QualifiedName AndAlso originalSimpleName.IsRightSideOfQualifiedName()) Then

                    Return TryAddTypeArgumentToIdentifierName(newNode, symbol)
                End If

                '
                ' 4. If this is a standalone identifier or the left side of a qualified name or member access try to fully qualify it
                '

                ' we need to treat the constructor as type name, so just get the containing type.
                If symbol.IsConstructor() AndAlso parent.Kind = SyntaxKind.ObjectCreationExpression Then
                    symbol = symbol.ContainingType
                End If

                ' if it's a namespace or type name, fully qualify it.
                If symbol.Kind = SymbolKind.NamedType OrElse symbol.Kind = SymbolKind.Namespace Then
                    Return FullyQualifyIdentifierName(
                        DirectCast(symbol, INamespaceOrTypeSymbol),
                        newNode,
                        originalSimpleName,
                        replaceNode:=False) _
                            .WithAdditionalAnnotations(Simplifier.Annotation)
                End If

                ' if it's a member access, we're fully qualifying the left side and make it a member access.
                If symbol.Kind = SymbolKind.Method OrElse
                   symbol.Kind = SymbolKind.Field OrElse
                   symbol.Kind = SymbolKind.Property Then

                    If symbol.IsStatic OrElse
                       (TypeOf (parent) Is CrefReferenceSyntax) OrElse
                       _semanticModel.SyntaxTree.IsNameOfContext(originalSimpleName.SpanStart, _cancellationToken) Then

                        newNode = FullyQualifyIdentifierName(
                            symbol,
                            newNode,
                            originalSimpleName,
                            replaceNode:=False)
                    Else
                        Dim left As ExpressionSyntax
                        If _semanticModel.GetEnclosingNamedType(originalSimpleName.SpanStart, _cancellationToken) IsNot symbol.ContainingType Then
                            left = SyntaxFactory.MyBaseExpression()
                        Else
                            left = SyntaxFactory.MeExpression()
                        End If

                        Dim identifiersLeadingTrivia = newNode.GetLeadingTrivia()

                        newNode = TryAddTypeArgumentToIdentifierName(newNode, symbol)
                        newNode = SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        left,
                        SyntaxFactory.Token(SyntaxKind.DotToken),
                        DirectCast(newNode, SimpleNameSyntax).WithoutLeadingTrivia()) _
                            .WithLeadingTrivia(identifiersLeadingTrivia)
                    End If

                    newNode = newNode.WithAdditionalAnnotations(Simplifier.Annotation)
                End If

                Return newNode
            End Function

            Private Function TryAddTypeArgumentToIdentifierName(
                newNode As ExpressionSyntax,
                symbol As ISymbol) As ExpressionSyntax
                If newNode.Kind = SyntaxKind.IdentifierName AndAlso symbol.Kind = SymbolKind.Method Then
                    If DirectCast(symbol, IMethodSymbol).TypeArguments.Length <> 0 Then
                        Dim typeArguments = DirectCast(symbol, IMethodSymbol).TypeArguments

                        Dim genericName = SyntaxFactory.GenericName(
                                        DirectCast(newNode, IdentifierNameSyntax).Identifier,
                                        SyntaxFactory.TypeArgumentList(
                                            SyntaxFactory.SeparatedList(typeArguments.Select(Function(p) SyntaxFactory.ParseTypeName(p.ToDisplayParts(typeNameFormatWithGenerics).ToDisplayString()))))) _
                            .WithLeadingTrivia(newNode.GetLeadingTrivia()) _
                            .WithTrailingTrivia(newNode.GetTrailingTrivia()) _
                            .WithAdditionalAnnotations(Simplifier.Annotation)
                        genericName = newNode.CopyAnnotationsTo(genericName)
                        Return genericName
                    End If
                End If

                Return newNode
            End Function

            Private Function FullyQualifyIdentifierName(
                symbol As ISymbol,
                rewrittenNode As ExpressionSyntax,
                originalNode As ExpressionSyntax,
                replaceNode As Boolean
            ) As ExpressionSyntax
                Debug.Assert(Not replaceNode OrElse rewrittenNode.Kind = SyntaxKind.IdentifierName)

                ' TODO: use and expand Generate*Syntax(isymbol) to not depend on symbol display any more.
                ' See GenerateExpressionSyntax();

                Dim result = rewrittenNode

                ' only if this symbol has a containing type or namespace there is work for us to do.
                If replaceNode OrElse symbol.ContainingType IsNot Nothing OrElse symbol.ContainingNamespace IsNot Nothing Then
                    Dim symbolForQualification = If(replaceNode, symbol, symbol.ContainingSymbol)

                    rewrittenNode = TryAddTypeArgumentToIdentifierName(rewrittenNode, symbol)

                    Dim displayParts = symbolForQualification.ToDisplayParts(typeNameFormatWithGenerics)
                    Dim left As ExpressionSyntax = SyntaxFactory.ParseTypeName(displayParts.ToDisplayString())

                    ' symbol display always includes module names in the qualification, but this can sometimes break code
                    ' (see bug 529837).
                    ' if we don't get back the same symbol for the full qualification, then we'll omit the module name.
                    If symbol.IsModuleMember Then
                        Dim newSymbol = _semanticModel.GetSpeculativeSymbolInfo(originalNode.SpanStart, left, SpeculativeBindingOption.BindAsExpression).Symbol

                        If Not symbolForQualification.Equals(newSymbol) Then
                            displayParts = symbolForQualification.ContainingSymbol.ToDisplayParts(typeNameFormatWithGenerics)
                            left = SyntaxFactory.ParseTypeName(displayParts.ToDisplayString())
                        End If
                    End If

                    If replaceNode Then
                        result = left _
                            .WithLeadingTrivia(rewrittenNode.GetLeadingTrivia()) _
                            .WithTrailingTrivia(rewrittenNode.GetTrailingTrivia())

                        Debug.Assert(
                            symbol.Equals(_semanticModel.GetSpeculativeSymbolInfo(originalNode.SpanStart, result, SpeculativeBindingOption.BindAsExpression).Symbol))

                        Return result
                    End If

                    ' now create syntax for the combination of left and right syntax, or a simple replacement in case of an identifier
                    Dim parent = originalNode.Parent
                    Dim leadingTrivia = rewrittenNode.GetLeadingTrivia()
                    rewrittenNode = rewrittenNode.WithoutLeadingTrivia()

                    Select Case parent.Kind
                        Case SyntaxKind.QualifiedName
                            Dim qualifiedParent = DirectCast(parent, QualifiedNameSyntax)

                            result = rewrittenNode.CopyAnnotationsTo(
                                SyntaxFactory.QualifiedName(
                                    DirectCast(left, NameSyntax),
                                    DirectCast(rewrittenNode, SimpleNameSyntax)))

                        Case SyntaxKind.SimpleMemberAccessExpression
                            Dim memberAccessParent = DirectCast(parent, MemberAccessExpressionSyntax)

                            result = rewrittenNode.CopyAnnotationsTo(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    left,
                                    memberAccessParent.OperatorToken,
                                    DirectCast(rewrittenNode, SimpleNameSyntax)))

                        Case Else
                            Debug.Assert(TypeOf (rewrittenNode) Is SimpleNameSyntax)

                            If SyntaxFacts.IsInNamespaceOrTypeContext(originalNode) OrElse TypeOf (parent) Is CrefReferenceSyntax Then
                                Dim right = DirectCast(rewrittenNode, SimpleNameSyntax)
                                result = rewrittenNode.CopyAnnotationsTo(SyntaxFactory.QualifiedName(DirectCast(left, NameSyntax), right.WithAdditionalAnnotations(Simplifier.SpecialTypeAnnotation)))
                            Else
                                result = rewrittenNode.CopyAnnotationsTo(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        DirectCast(left, ExpressionSyntax),
                                        SyntaxFactory.Token(SyntaxKind.DotToken),
                                        DirectCast(rewrittenNode, SimpleNameSyntax)))
                            End If
                    End Select

                    result = result.WithLeadingTrivia(leadingTrivia)
                End If

                Return result
            End Function

            Private Function IsTypeOfUnboundGenericType(semanticModel As SemanticModel, typeOfExpression As TypeOfExpressionSyntax) As Boolean
                If typeOfExpression IsNot Nothing Then
                    Dim type = TryCast(semanticModel.GetTypeInfo(typeOfExpression.Type, _cancellationToken).Type, INamedTypeSymbol)

                    ' It's possible the immediate type might not be an unbound type, such as typeof(A<>.B). So walk through
                    ' parent types too
                    Do While type IsNot Nothing
                        If type.IsUnboundGenericType Then
                            Return True
                        End If

                        type = type.ContainingType
                    Loop
                End If

                Return False
            End Function

            Public Overrides Function VisitLabelStatement(node As LabelStatementSyntax) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                Dim newLabelStatement = DirectCast(MyBase.VisitLabelStatement(node), LabelStatementSyntax)

                Dim escapedLabelToken = TryEscapeIdentifierToken(newLabelStatement.LabelToken, Me._semanticModel)
                If newLabelStatement.LabelToken <> escapedLabelToken Then
                    newLabelStatement = newLabelStatement.WithLabelToken(escapedLabelToken)
                End If

                Return newLabelStatement
            End Function

        End Class
    End Class
End Namespace
