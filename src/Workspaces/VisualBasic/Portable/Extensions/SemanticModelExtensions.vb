' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
Imports Microsoft.CodeAnalysis.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module SemanticModelExtensions

        Private Const s_defaultParameterName = "p"
        Private Const s_defaultBuiltInParameterName = "v"

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
        Public Function GenerateNameForArgument(semanticModel As SemanticModel,
                                                argument As ArgumentSyntax,
                                                cancellationToken As CancellationToken) As String
            Dim result = GenerateNameForArgumentWorker(semanticModel, argument, cancellationToken)
            Return If(String.IsNullOrWhiteSpace(result), s_defaultParameterName, result)
        End Function

        Private Function GenerateNameForArgumentWorker(semanticModel As SemanticModel,
                                                       argument As ArgumentSyntax,
                                                       cancellationToken As CancellationToken) As String
            If argument.IsNamed Then
                Return DirectCast(argument, SimpleArgumentSyntax).NameColonEquals.Name.Identifier.ValueText
            ElseIf Not argument.IsOmitted Then
                Return semanticModel.GenerateNameForExpression(
                    argument.GetExpression(), capitalize:=False, cancellationToken:=cancellationToken)
            Else
                Return s_defaultParameterName
            End If
        End Function

        ''' <summary>
        ''' Given an expression node, tries to generate an appropriate name that can be used for
        ''' that expression.
        ''' </summary> 
        <Extension()>
        Public Function GenerateNameForExpression(semanticModel As SemanticModel,
                                                  expression As ExpressionSyntax,
                                                  capitalize As Boolean,
                                                  cancellationToken As CancellationToken) As String
            ' Try to find a usable name node that we can use to name the
            ' parameter.  If we have an expression that has a name as part of it
            ' then we try to use that part.
            Dim current = expression

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

            ' If we can't determine the type, then fallback to some placeholders.
            Dim [type] = info.Type
            Return [type].CreateParameterName(capitalize)
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

        <Extension()>
        Public Function GenerateParameterNames(semanticModel As SemanticModel,
                                               arguments As ArgumentListSyntax,
                                               reservedNames As IEnumerable(Of String),
                                               cancellationToken As CancellationToken) As ImmutableArray(Of ParameterName)
            If arguments Is Nothing Then
                Return ImmutableArray(Of ParameterName).Empty
            End If

            Return GenerateParameterNames(
                semanticModel, arguments.Arguments.ToList(),
                reservedNames, cancellationToken)
        End Function

        <Extension()>
        Public Function GenerateParameterNames(semanticModel As SemanticModel,
                                               arguments As IList(Of ArgumentSyntax),
                                               reservedNames As IEnumerable(Of String),
                                               cancellationToken As CancellationToken) As ImmutableArray(Of ParameterName)
            reservedNames = If(reservedNames, SpecializedCollections.EmptyEnumerable(Of String))
            Return semanticModel.GenerateParameterNames(
                arguments,
                Function(s) Not reservedNames.Any(Function(n) CaseInsensitiveComparison.Equals(s, n)),
                cancellationToken)
        End Function

        <Extension()>
        Public Function GenerateParameterNames(semanticModel As SemanticModel,
                                               arguments As IList(Of ArgumentSyntax),
                                               reservedNames As IEnumerable(Of String),
                                               parameterNamingRule As NamingRule,
                                               cancellationToken As CancellationToken) As ImmutableArray(Of ParameterName)
            reservedNames = If(reservedNames, SpecializedCollections.EmptyEnumerable(Of String))
            Return semanticModel.GenerateParameterNames(
                arguments,
                Function(s) Not reservedNames.Any(Function(n) CaseInsensitiveComparison.Equals(s, n)),
                parameterNamingRule,
                cancellationToken)
        End Function

        <Extension()>
        Public Function GenerateParameterNames(semanticModel As SemanticModel,
                                               arguments As IList(Of ArgumentSyntax),
                                               canUse As Func(Of String, Boolean),
                                               cancellationToken As CancellationToken) As ImmutableArray(Of ParameterName)
            If arguments.Count = 0 Then
                Return ImmutableArray(Of ParameterName).Empty
            End If

            ' We can't change the names of named parameters.  Any other names we're flexible on.
            Dim isFixed = Aggregate arg In arguments
                          Select arg = TryCast(arg, SimpleArgumentSyntax)
                          Select arg IsNot Nothing AndAlso arg.NameColonEquals IsNot Nothing
                          Into ToList()

            Dim parameterNames = arguments.Select(Function(a) semanticModel.GenerateNameForArgument(a, cancellationToken)).ToList()
            Return NameGenerator.EnsureUniqueness(parameterNames, isFixed, canUse).
                                 Select(Function(name, index) New ParameterName(name, isFixed(index))).
                                 ToImmutableArray()
        End Function

        <Extension()>
        Public Function GenerateParameterNames(semanticModel As SemanticModel,
                                               arguments As IList(Of ArgumentSyntax),
                                               canUse As Func(Of String, Boolean),
                                               parameterNamingRule As NamingRule,
                                               cancellationToken As CancellationToken) As ImmutableArray(Of ParameterName)
            If arguments.Count = 0 Then
                Return ImmutableArray(Of ParameterName).Empty
            End If

            ' We can't change the names of named parameters.  Any other names we're flexible on.
            Dim isFixed = Aggregate arg In arguments
                          Select arg = TryCast(arg, SimpleArgumentSyntax)
                          Select arg IsNot Nothing AndAlso arg.NameColonEquals IsNot Nothing
                          Into ToList()

            Dim parameterNames = arguments.Select(Function(a) semanticModel.GenerateNameForArgument(a, cancellationToken)).ToList()
            Return NameGenerator.EnsureUniqueness(parameterNames, isFixed, canUse).
                                 Select(Function(name, index) New ParameterName(name, isFixed(index), parameterNamingRule)).
                                 ToImmutableArray()
        End Function

        Private Function SetEquals(array1 As ImmutableArray(Of ISymbol), array2 As ImmutableArray(Of ISymbol)) As Boolean
            ' Do some quick up front checks so we won't have to allocate memory below.
            If array1.Length = 0 AndAlso array2.Length = 0 Then
                Return True
            End If

            If array1.Length = 0 OrElse array2.Length = 0 Then
                Return False
            End If

            If array1.Length = 1 AndAlso array2.Length = 1 Then
                Return array1(0).Equals(array2(0))
            End If

            Dim [set] = New HashSet(Of ISymbol)(array1)
            Return [set].SetEquals(array2)
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

        <Extension()>
        Public Iterator Function GetAliasSymbols(semanticModel As SemanticModel) As IEnumerable(Of IAliasSymbol)
            semanticModel = DirectCast(semanticModel.GetOriginalSemanticModel(), SemanticModel)

            Dim root = semanticModel.SyntaxTree.GetCompilationUnitRoot()
            For Each importsClause In root.GetAliasImportsClauses()
                Dim [alias] = DirectCast(semanticModel.GetDeclaredSymbol(importsClause), IAliasSymbol)
                If [alias] IsNot Nothing Then
                    Yield [alias]
                End If
            Next
        End Function
    End Module
End Namespace
