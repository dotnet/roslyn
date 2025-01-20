' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.AddExplicitCast
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.PooledObjects
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
            _fixer = New ArgumentFixer()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(BC30512, BC42016, BC30518, BC30519)

        Protected Overrides Sub GetPartsOfCastOrConversionExpression(
                expression As ExpressionSyntax,
                ByRef type As SyntaxNode,
                ByRef castedExpression As ExpressionSyntax)
            Dim directCastExpression = TryCast(expression, DirectCastExpressionSyntax)
            If directCastExpression IsNot Nothing Then
                type = directCastExpression.Type
                castedExpression = directCastExpression.Expression
                Return
            End If

            Dim conversionExpression = DirectCast(expression, CTypeExpressionSyntax)
            type = conversionExpression.Type
            castedExpression = conversionExpression.Expression
        End Sub

        Protected Overrides Function Cast(expression As ExpressionSyntax, type As ITypeSymbol) As ExpressionSyntax
            Return expression.Cast(type, isResultPredefinedCast:=Nothing)
        End Function

        Protected Overrides Sub AddPotentialTargetTypes(
                document As Document,
                semanticModel As SemanticModel,
                root As SyntaxNode,
                diagnosticId As String,
                spanNode As ExpressionSyntax,
                candidates As ArrayBuilder(Of (node As ExpressionSyntax, type As ITypeSymbol)),
                cancellationToken As CancellationToken)

            Select Case diagnosticId
                Case BC30512, BC42016
                    Dim argument = spanNode.GetAncestors(Of ArgumentSyntax).FirstOrDefault()
                    If argument IsNot Nothing AndAlso argument.GetExpression.Equals(spanNode) Then
                        ' spanNode is an argument expression
                        Dim argumentList = DirectCast(argument.Parent, ArgumentListSyntax)
                        Dim invocationNode = argumentList.Parent

                        candidates.AddRange(_fixer.GetPotentialConversionTypes(
                            document, semanticModel, root, argument, argumentList, invocationNode, cancellationToken))
                    Else
                        ' spanNode is a right expression in assignment operation
                        Dim inferenceService = document.GetRequiredLanguageService(Of ITypeInferenceService)()
                        Dim conversionType = inferenceService.InferType(semanticModel, spanNode, objectAsDefault:=False,
                            cancellationToken)
                        candidates.Add((spanNode, conversionType))
                    End If
                Case BC30518, BC30519
                    Dim invocationExpressionNode = spanNode.GetAncestors(Of InvocationExpressionSyntax).FirstOrDefault(
                        Function(node) node.ArgumentList IsNot Nothing)

                    Dim attributeNode = spanNode.GetAncestors(Of AttributeSyntax).FirstOrDefault(
                        Function(node) node.ArgumentList IsNot Nothing)

                    ' Collect available cast pairs without target argument
                    If invocationExpressionNode IsNot Nothing Then
                        candidates.AddRange(
                            GetPotentialConversionTypesWithInvocationNode(document, semanticModel, root, invocationExpressionNode, cancellationToken))
                    ElseIf attributeNode IsNot Nothing Then
                        candidates.AddRange(
                            GetPotentialConversionTypesWithInvocationNode(document, semanticModel, root, attributeNode, cancellationToken))
                    End If
            End Select
        End Sub

        ''' <summary>
        ''' Find the first argument that need to be cast
        ''' </summary>
        ''' <param name="parameters"> The parameters of method</param>
        ''' <param name="arguments"> The arguments of invocation node</param>
        ''' <returns>
        ''' Return the first argument that need to be cast, could be null if such argument doesn't exist
        ''' </returns>
        Private Shared Function GetTargetArgument(
                document As Document,
                semanticModel As SemanticModel,
                parameters As ImmutableArray(Of IParameterSymbol),
                arguments As SeparatedSyntaxList(Of ArgumentSyntax)) As ArgumentSyntax
            If parameters.Length = 0 Then
                Return Nothing
            End If

            Dim syntaxFacts = document.GetRequiredLanguageService(Of ISyntaxFactsService)()

            For i = 0 To arguments.Count - 1
                ' Parameter index cannot out of its range, #arguments Is larger than #parameter only if 
                ' the last parameter with keyword params
                Dim parameterIndex = Math.Min(i, parameters.Length - 1)

                ' If the argument has a name, get the corresponding parameter index
                Dim argumentName = syntaxFacts.GetNameForArgument(arguments(i))
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
                    If paramsType IsNot Nothing Then
                        Dim conversion = semanticModel.ClassifyConversion(argumentExpression, paramsType.ElementType)
                        If conversion.Exists AndAlso Not conversion.IsIdentity Then
                            Return arguments(i)
                        End If
                    End If
                End If

                Dim argumentConversion = semanticModel.ClassifyConversion(argumentExpression, parameterType)
                If argumentConversion.Exists AndAlso Not argumentConversion.IsIdentity Then
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
                document As Document,
                semanticModel As SemanticModel,
                root As SyntaxNode,
                invocationNode As SyntaxNode,
                cancellationToken As CancellationToken) As ImmutableArray(Of (ExpressionSyntax, ITypeSymbol))
            Dim argumentList = invocationNode.ChildNodes.OfType(Of ArgumentListSyntax).FirstOrDefault()

            Dim symbolInfo = semanticModel.GetSymbolInfo(invocationNode, cancellationToken)
            Dim candidateSymbols = symbolInfo.CandidateSymbols

            Dim mutablePotentialConversionTypes = ArrayBuilder(Of (ExpressionSyntax, ITypeSymbol)).GetInstance()

            For Each candidateSymbol In candidateSymbols.OfType(Of IMethodSymbol)()
                Dim targetArgument = GetTargetArgument(document, semanticModel, candidateSymbol.Parameters, argumentList.Arguments)
                If targetArgument Is Nothing Then
                    Continue For
                End If

                Dim conversionType As ITypeSymbol = Nothing
                If _fixer.CanArgumentTypesBeConvertedToParameterTypes(
                        document, semanticModel, root, argumentList, candidateSymbol.Parameters, targetArgument, cancellationToken, conversionType) Then
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
