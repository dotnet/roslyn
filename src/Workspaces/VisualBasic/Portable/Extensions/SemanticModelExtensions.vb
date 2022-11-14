' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
Imports Microsoft.CodeAnalysis.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module SemanticModelExtensions
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
                                               canUse As Func(Of String, Boolean),
                                               cancellationToken As CancellationToken) As ImmutableArray(Of ParameterName)
            If arguments.Count = 0 Then
                Return ImmutableArray(Of ParameterName).Empty
            End If

            ' We can't change the names of named parameters.  Any other names we're flexible on.
            Dim isFixed = Aggregate arg In arguments
                          Select arg = TryCast(arg, SimpleArgumentSyntax)
                          Select arg IsNot Nothing AndAlso arg.NameColonEquals IsNot Nothing
                          Into ToImmutableArray()

            Dim parameterNames = arguments.Select(Function(a) semanticModel.GenerateNameForArgument(a, cancellationToken)).ToImmutableArray()
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
                          Into ToImmutableArray()

            Dim parameterNames = arguments.Select(Function(a) semanticModel.GenerateNameForArgument(a, cancellationToken)).ToImmutableArray()
            Return NameGenerator.EnsureUniqueness(parameterNames, isFixed, canUse).
                                 Select(Function(name, index) New ParameterName(name, isFixed(index), parameterNamingRule)).
                                 ToImmutableArray()
        End Function

        <Extension()>
        Public Function GenerateNameForArgument(semanticModel As SemanticModel,
                                                argument As ArgumentSyntax,
                                                cancellationToken As CancellationToken) As String
            Dim result = GenerateNameForArgumentWorker(semanticModel, argument, cancellationToken)
            Return If(String.IsNullOrWhiteSpace(result), [Shared].Extensions.ITypeSymbolExtensions.DefaultParameterName, result)
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
                Return [Shared].Extensions.ITypeSymbolExtensions.DefaultParameterName
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
