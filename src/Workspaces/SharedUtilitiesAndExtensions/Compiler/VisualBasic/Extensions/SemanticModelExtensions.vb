' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module SemanticModelExtensions
        <Extension()>
        Public Function LookupTypeRegardlessOfArity(semanticModel As SemanticModel,
                                                    name As SyntaxToken,
                                                    cancellationToken As CancellationToken) As IList(Of ITypeSymbol)
            Dim expression = TryCast(name.Parent, ExpressionSyntax)
            If expression IsNot Nothing Then
                Dim results = semanticModel.LookupName(expression, namespacesAndTypesOnly:=True, cancellationToken:=cancellationToken)
                If results.Count > 0 Then
                    Return results.OfType(Of ITypeSymbol)().ToList()
                End If
            End If

            Return SpecializedCollections.EmptyList(Of ITypeSymbol)()
        End Function

        <Extension()>
        Public Function LookupName(semanticModel As SemanticModel, name As SyntaxToken,
                                   namespacesAndTypesOnly As Boolean,
                                   cancellationToken As CancellationToken) As IList(Of ISymbol)
            Dim expression = TryCast(name.Parent, ExpressionSyntax)
            If expression IsNot Nothing Then
                Return semanticModel.LookupName(expression, namespacesAndTypesOnly, cancellationToken)
            End If

            Return SpecializedCollections.EmptyList(Of ISymbol)()
        End Function

        <Extension()>
        Public Function LookupName(semanticModel As SemanticModel,
                                   expression As ExpressionSyntax,
                                   namespacesAndTypesOnly As Boolean,
                                   cancellationToken As CancellationToken) As IList(Of ISymbol)
            Dim expr = SyntaxFactory.GetStandaloneExpression(expression)

            Dim qualifier As ExpressionSyntax = Nothing
            Dim name As String = Nothing
            Dim arity As Integer = Nothing
            expr.DecomposeName(qualifier, name, arity)

            Dim symbol As INamespaceOrTypeSymbol = Nothing
            If qualifier IsNot Nothing AndAlso TypeOf qualifier Is TypeSyntax Then
                Dim typeInfo = semanticModel.GetTypeInfo(qualifier, cancellationToken)
                Dim symbolInfo = semanticModel.GetSymbolInfo(qualifier, cancellationToken)

                If typeInfo.Type IsNot Nothing Then
                    symbol = typeInfo.Type
                ElseIf symbolInfo.Symbol IsNot Nothing Then
                    symbol = TryCast(symbolInfo.Symbol, INamespaceOrTypeSymbol)
                End If
            End If

            Return If(
                namespacesAndTypesOnly,
                semanticModel.LookupNamespacesAndTypes(expr.SpanStart, container:=symbol, name:=name),
                semanticModel.LookupSymbols(expr.SpanStart, container:=symbol, name:=name))
        End Function

        <Extension()>
        Public Function GetSymbolInfo(semanticModel As SemanticModel, token As SyntaxToken) As SymbolInfo
            Dim expression = TryCast(token.Parent, ExpressionSyntax)
            If expression Is Nothing Then
                Return Nothing
            End If

            Return semanticModel.GetSymbolInfo(expression)
        End Function

        <Extension()>
        Public Function GetImportNamespacesInScope(semanticModel As SemanticModel, location As SyntaxNode) As ISet(Of INamespaceSymbol)
            Dim q =
                From u In location.GetAncestorOrThis(Of CompilationUnitSyntax).Imports
                From importClause In u.ImportsClauses.OfType(Of SimpleImportsClauseSyntax)()
                Where importClause.Alias Is Nothing
                Let info = semanticModel.GetSymbolInfo(importClause.Name)
                Let ns = TryCast(info.Symbol, INamespaceSymbol)
                Where ns IsNot Nothing
                Select ns

            Return q.ToSet()
        End Function

        <Extension()>
        Public Function GetAliasInfo(semanticModel As SemanticModel, expression As ExpressionSyntax, cancellationToken As CancellationToken) As IAliasSymbol
            Dim nameSyntax = TryCast(expression, IdentifierNameSyntax)
            If nameSyntax Is Nothing Then
                Return Nothing
            Else
                Return semanticModel.GetAliasInfo(nameSyntax, cancellationToken)
            End If
        End Function

        <Extension()>
        Public Function DetermineAccessibilityConstraint(semanticModel As SemanticModel,
                                                         type As TypeSyntax,
                                                         cancellationToken As CancellationToken) As Accessibility
            If type Is Nothing Then
                Return Accessibility.Private
            End If

            type = type.GetAncestorsOrThis(Of TypeSyntax)().Last()

            If type.IsParentKind(SyntaxKind.InheritsStatement) Then
                Dim containingType = semanticModel.GetEnclosingNamedType(type.SpanStart, cancellationToken)
                Return containingType.DeclaredAccessibility
            End If

            ' Determine accessibility of field or event
            '  Public B as B
            If type.IsParentKind(SyntaxKind.SimpleAsClause) AndAlso
                type.Parent.IsParentKind(SyntaxKind.VariableDeclarator) Then
                If type.Parent.Parent.IsParentKind(SyntaxKind.FieldDeclaration) OrElse
                   type.Parent.Parent.IsParentKind(SyntaxKind.EventStatement) Then
                    Dim variableDeclarator = DirectCast(type.Parent.Parent, VariableDeclaratorSyntax)
                    If variableDeclarator.Names.Count > 0 Then
                        Dim variableDeclaration = semanticModel.GetDeclaredSymbol(variableDeclarator.Names(0), cancellationToken)
                        Return variableDeclaration.DeclaredAccessibility
                    End If
                End If
            End If

            ' Determine accessibility of field or event
            '  Public B as New B()
            If type.IsParentKind(SyntaxKind.ObjectCreationExpression) AndAlso
                type.Parent.IsParentKind(SyntaxKind.AsNewClause) AndAlso
                type.Parent.Parent.IsParentKind(SyntaxKind.VariableDeclarator) Then
                If type.Parent.Parent.Parent.IsParentKind(SyntaxKind.FieldDeclaration) OrElse
                   type.Parent.Parent.Parent.IsParentKind(SyntaxKind.EventStatement) Then
                    Dim variableDeclarator = DirectCast(type.Parent.Parent.Parent, VariableDeclaratorSyntax)
                    If variableDeclarator.Names.Count > 0 Then
                        Dim variableDeclaration = semanticModel.GetDeclaredSymbol(variableDeclarator.Names(0), cancellationToken)
                        Return variableDeclaration.DeclaredAccessibility
                    End If
                End If
            End If

            If type.IsParentKind(SyntaxKind.SimpleAsClause) Then
                If type.Parent.IsParentKind(SyntaxKind.DelegateFunctionStatement) OrElse
                   type.Parent.IsParentKind(SyntaxKind.FunctionStatement) OrElse
                   type.Parent.IsParentKind(SyntaxKind.PropertyStatement) OrElse
                   type.Parent.IsParentKind(SyntaxKind.EventStatement) OrElse
                   type.Parent.IsParentKind(SyntaxKind.OperatorStatement) Then
                    Return semanticModel.GetDeclaredSymbol(
                        type.Parent.Parent, cancellationToken).DeclaredAccessibility
                End If
            End If

            If type.IsParentKind(SyntaxKind.SimpleAsClause) AndAlso
               type.Parent.IsParentKind(SyntaxKind.Parameter) AndAlso
               type.Parent.Parent.IsParentKind(SyntaxKind.ParameterList) Then
                If type.Parent.Parent.Parent.IsParentKind(SyntaxKind.DelegateFunctionStatement) OrElse
                   type.Parent.Parent.Parent.IsParentKind(SyntaxKind.FunctionStatement) OrElse
                   type.Parent.Parent.Parent.IsParentKind(SyntaxKind.PropertyStatement) OrElse
                   type.Parent.Parent.Parent.IsParentKind(SyntaxKind.OperatorStatement) OrElse
                   type.Parent.Parent.Parent.IsParentKind(SyntaxKind.SubNewStatement) OrElse
                   type.Parent.Parent.Parent.IsParentKind(SyntaxKind.SubStatement) Then
                    Return semanticModel.GetDeclaredSymbol(
                        type.Parent.Parent.Parent.Parent, cancellationToken).DeclaredAccessibility
                End If
            End If

            Return Accessibility.Private
        End Function

        <Extension>
        Public Iterator Function GetAliasSymbols(semanticModel As SemanticModel) As IEnumerable(Of IAliasSymbol)
            semanticModel = semanticModel.GetOriginalSemanticModel()

            Dim root = semanticModel.SyntaxTree.GetCompilationUnitRoot()
            For Each importsClause In root.GetAliasImportsClauses()
                Dim [alias] = semanticModel.GetDeclaredSymbol(importsClause)
                If [alias] IsNot Nothing Then
                    Yield [alias]
                End If
            Next
        End Function

        ''' <summary>
        ''' Given an expression node, tries to generate an appropriate name that can be used for
        ''' that expression.
        ''' </summary> 
        <Extension>
        Public Function GenerateNameForExpression(semanticModel As SemanticModel,
                                                  expression As ExpressionSyntax,
                                                  capitalize As Boolean,
                                                  cancellationToken As CancellationToken) As String
            ' Try to find a usable name node that we can use to name the
            ' parameter.  If we have an expression that has a name as part of it
            ' then we try to use that part.
            Dim current = expression
            Dim returnType As ITypeSymbol = Nothing

            ' If we have an implicitly callable expression (like `WriteLine(SomeMethod)`) we don't want to generate
            ' `someMethod` as the name of the parameter.  Just fallback to our default naming strategy.
            If Not IsImplicitlyCallable(expression, semanticModel, cancellationToken, returnType) Then
                While True
                    current = current.WalkDownParentheses()
                    If current.Kind = SyntaxKind.IdentifierName Then
                        Return (DirectCast(current, IdentifierNameSyntax)).Identifier.ValueText.ToCamelCase()
                    ElseIf TypeOf current Is MemberAccessExpressionSyntax Then
                        Return (DirectCast(current, MemberAccessExpressionSyntax)).Name.Identifier.ValueText.ToCamelCase()
                    ElseIf TypeOf current Is CastExpressionSyntax Then
                        current = (DirectCast(current, CastExpressionSyntax)).Expression
                    Else
                        Exit While
                    End If
                End While
            End If

            ' there was nothing in the expression to signify a name.  If we're in an argument
            ' location, then try to choose a name based on the argument name.
            Dim argumentName = TryGenerateNameForArgumentExpression(
                semanticModel, expression, cancellationToken)
            If argumentName IsNot Nothing Then
                Return If(capitalize, argumentName.ToPascalCase(), argumentName.ToCamelCase())
            End If

            ' Otherwise, figure out the type of the expression and generate a name from that
            ' instead.
            Dim info = semanticModel.GetTypeInfo(expression, cancellationToken)
            If info.Type Is Nothing Then
                Return [Shared].Extensions.ITypeSymbolExtensions.DefaultParameterName
            End If

            Return semanticModel.GenerateNameFromType(info.Type, VisualBasicSyntaxFacts.Instance, capitalize)
        End Function

        Private Function TryGenerateNameForArgumentExpression(semanticModel As SemanticModel, expression As ExpressionSyntax, cancellationToken As CancellationToken) As String
            Dim topExpression = expression.WalkUpParentheses()
            If TypeOf topExpression.Parent Is ArgumentSyntax Then
                Dim argument = DirectCast(topExpression.Parent, ArgumentSyntax)
                Dim simpleArgument = TryCast(argument, SimpleArgumentSyntax)

                If simpleArgument?.NameColonEquals IsNot Nothing Then
                    Return simpleArgument.NameColonEquals.Name.Identifier.ValueText
                End If

                Dim argumentList = TryCast(argument.Parent, ArgumentListSyntax)
                If argumentList IsNot Nothing Then
                    Dim index = argumentList.Arguments.IndexOf(argument)
                    Dim member = TryCast(semanticModel.GetSymbolInfo(argumentList.Parent, cancellationToken).Symbol, IMethodSymbol)
                    If member IsNot Nothing AndAlso index < member.Parameters.Length Then
                        Dim parameter = member.Parameters(index)
                        If parameter.Type.TypeKind <> TypeKind.TypeParameter Then
                            Return parameter.Name
                        End If
                    End If
                End If
            End If

            Return Nothing
        End Function
    End Module
End Namespace
