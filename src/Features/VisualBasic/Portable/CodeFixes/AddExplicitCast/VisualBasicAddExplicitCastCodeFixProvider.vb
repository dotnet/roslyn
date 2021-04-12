﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.AddExplicitCast
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.AddExplicitCast

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddExplicitCast), [Shared]>
    Partial Friend NotInheritable Class VisualBasicAddExplicitCastCodeFixProvider
        Inherits AbstractAddExplicitCastCodeFixProvider(Of ExpressionSyntax)

        Friend Const BC30512 As String = "BC30512" ' Option Strict On disallows implicit conversions from '{0}' to '{1}'.
        Friend Const BC42016 As String = "BC42016" ' Implicit conversions from '{0}' to '{1}'.
        Friend Const BC30518 As String = "BC30518" ' Overload resolution failed because no accessible 'sub1' can be called with these arguments.
        Friend Const BC30519 As String = "BC30519" ' Overload resolution failed because no accessible 'sub1' can be called without a narrowing conversion.

        Private ReadOnly _fixer As ArgumentFixer

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
            MyBase.New(VisualBasicSyntaxFacts.Instance)
            _fixer = New ArgumentFixer(Me)
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(BC30512, BC42016, BC30518, BC30519)

        Protected Overrides Function ApplyFix(currentRoot As SyntaxNode, targetNode As ExpressionSyntax,
                conversionType As ITypeSymbol) As SyntaxNode
            ' TODO:
            ' the Simplifier doesn't remove the redundant cast from the expression
            ' Issue link: https : //github.com/dotnet/roslyn/issues/41500
            Dim castExpression = targetNode.Cast(
                conversionType, isResultPredefinedCast:=Nothing).WithAdditionalAnnotations(Simplifier.Annotation)
            Dim newRoot = currentRoot.ReplaceNode(targetNode, castExpression)
            Return newRoot
        End Function

        Protected Overrides Function TryGetTargetTypeInfo(document As Document, semanticModel As SemanticModel,
                root As SyntaxNode, diagnosticId As String, spanNode As ExpressionSyntax,
                cancellationToken As CancellationToken,
                ByRef potentialConversionTypes As ImmutableArray(Of (ExpressionSyntax, ITypeSymbol))) As Boolean
            potentialConversionTypes = ImmutableArray(Of (ExpressionSyntax, ITypeSymbol)).Empty
            Dim mutablePotentialConversionTypes = ArrayBuilder(Of (ExpressionSyntax, ITypeSymbol)).GetInstance()

            Select Case diagnosticId
                Case BC30512, BC42016
                    Dim argument = spanNode.GetAncestors(Of ArgumentSyntax).FirstOrDefault()
                    If argument IsNot Nothing AndAlso argument.GetExpression.Equals(spanNode) Then
                        ' spanNode is an argument expression
                        Dim argumentList = DirectCast(argument.Parent, ArgumentListSyntax)
                        Dim invocationNode = argumentList.Parent

                        mutablePotentialConversionTypes.AddRange(_fixer.GetPotentialConversionTypes(
                            semanticModel, root, argument, argumentList, invocationNode, cancellationToken))
                    Else
                        ' spanNode is a right expression in assignment operation
                        Dim inferenceService = document.GetRequiredLanguageService(Of ITypeInferenceService)()
                        Dim conversionType = inferenceService.InferType(semanticModel, spanNode, objectAsDefault:=False,
                            cancellationToken)
                        mutablePotentialConversionTypes.Add((spanNode, conversionType))
                    End If
                Case BC30518, BC30519
                    Dim invocationNode = spanNode.GetAncestors(Of ExpressionSyntax).FirstOrDefault(
                        Function(node) Not node.ChildNodes.OfType(Of ArgumentListSyntax).IsEmpty())

                    ' Collect available cast pairs without target argument
                    mutablePotentialConversionTypes.AddRange(
                        GetPotentialConversionTypesWithInvocationNode(semanticModel, root, invocationNode, cancellationToken))
            End Select

            ' clear up duplicate types
            potentialConversionTypes = FilterValidPotentialConversionTypes(semanticModel, mutablePotentialConversionTypes)
            Return Not potentialConversionTypes.IsEmpty
        End Function

        Protected Overrides Function ClassifyConversion(semanticModel As SemanticModel,
                expression As ExpressionSyntax, type As ITypeSymbol) As CommonConversion
            Return semanticModel.ClassifyConversion(expression, type).ToCommonConversion()
        End Function

        ''' <summary>
        ''' Find the first argument that need to be cast
        ''' </summary>
        ''' <param name="parameters"> The parameters of method</param>
        ''' <param name="arguments"> The arguments of invocation node</param>
        ''' <returns>
        ''' Return the first argument that need to be cast, could be null if such argument doesn't exist
        ''' </returns>
        Private Function GetTargetArgument(semanticModel As SemanticModel,
                parameters As ImmutableArray(Of IParameterSymbol), arguments As SeparatedSyntaxList(Of ArgumentSyntax)) As ArgumentSyntax
            If parameters.Length = 0 Then
                Return Nothing
            End If

            For i = 0 To arguments.Count - 1
                ' Parameter index cannot out of its range, #arguments Is larger than #parameter only if 
                ' the last parameter with keyword params
                Dim parameterIndex = Math.Min(i, parameters.Length - 1)

                ' If the argument has a name, get the corresponding parameter index
                Dim argumentName = SyntaxFacts.GetNameForArgument(arguments(i))
                If argumentName IsNot String.Empty AndAlso Not FindCorrespondingParameterByName(argumentName, parameters,
                        parameterIndex) Then
                    Return Nothing
                End If

                Dim argumentExpression = arguments(i).GetExpression
                If argumentExpression Is Nothing Then
                    Continue For
                End If

                Dim parameterType = parameters(parameterIndex).Type

                If parameters(parameterIndex).IsParams Then
                    Dim paramsType = TryCast(parameterType, IArrayTypeSymbol)
                    Dim conversion = semanticModel.ClassifyConversion(argumentExpression, paramsType.ElementType)
                    If conversion.Exists AndAlso Not conversion.IsIdentity Then
                        Return arguments(i)
                    End If
                End If

                Dim converison = semanticModel.ClassifyConversion(argumentExpression, parameterType)
                If converison.Exists AndAlso Not converison.IsIdentity Then
                    Return arguments(i)
                End If
            Next

            Return Nothing
        End Function

        ''' <summary>
        ''' Collect available cast pairs without target argument.
        ''' For each method, the first argument need to be cast is the target argument.
        ''' Return format is (argument expression, potential conversion type).
        ''' </summary>
        ''' <param name="invocationNode">The invocation node that contains some arguments need to be cast</param>
        ''' <returns>
        ''' Return all the available cast pairs, format is (argument expression, potential conversion type)
        ''' </returns>
        Private Function GetPotentialConversionTypesWithInvocationNode(
                semanticModel As SemanticModel, root As SyntaxNode, invocationNode As SyntaxNode,
                cancellationToken As CancellationToken) As ImmutableArray(Of (ExpressionSyntax, ITypeSymbol))
            Dim argumentList = invocationNode.ChildNodes.OfType(Of ArgumentListSyntax).FirstOrDefault()

            Dim symbolInfo = semanticModel.GetSymbolInfo(invocationNode, cancellationToken)
            Dim candidateSymbols = symbolInfo.CandidateSymbols

            Dim mutablePotentialConversionTypes = ArrayBuilder(Of (ExpressionSyntax, ITypeSymbol)).GetInstance()

            For Each candidateSymbol In candidateSymbols.OfType(Of IMethodSymbol)()
                Dim targetArgument = GetTargetArgument(semanticModel, candidateSymbol.Parameters, argumentList.Arguments)
                If targetArgument Is Nothing Then
                    Continue For
                End If

                Dim conversionType As ITypeSymbol = Nothing
                If _fixer.CanArgumentTypesBeConvertedToParameterTypes(
                        semanticModel, root, argumentList, candidateSymbol.Parameters, targetArgument, cancellationToken, conversionType) Then
                    mutablePotentialConversionTypes.Add((targetArgument.GetExpression(), conversionType))
                End If
            Next

            ' Sort the potential conversion types by inheritance distance, so that
            ' operations are in order And user can choose least specific types(more accurate)
            mutablePotentialConversionTypes.Sort(New InheritanceDistanceComparer(Of ExpressionSyntax)(semanticModel))

            Return mutablePotentialConversionTypes.ToImmutable()
        End Function
    End Class
End Namespace
