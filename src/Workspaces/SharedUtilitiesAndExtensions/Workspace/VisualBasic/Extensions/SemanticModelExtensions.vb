' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

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
    End Module
End Namespace
