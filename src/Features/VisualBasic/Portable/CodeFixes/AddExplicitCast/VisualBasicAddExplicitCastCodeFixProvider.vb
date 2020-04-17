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
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.AddExplicitCast

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddExplicitCast), [Shared]>
    Friend NotInheritable Class VisualBasicAddExplicitCastCodeFixProvider
        Inherits AbstractAddExplicitCastCodeFixProvider(Of ExpressionSyntax)

        Friend Const BC30512 As String = "BC30512" ' Option Strict On disallows implicit conversions from '{0}' to '{1}'.
        Friend Const BC42016 As String = "BC42016" ' Implicit conversions from '{0}' to '{1}'.
        Friend Const BC30518 As String = "BC30518" ' Overload resolution failed because no accessible 'sub1' can be called with these arguments.
        Friend Const BC30519 As String = "BC30519" ' Overload resolution failed because no accessible 'sub1' can be called without a narrowing conversion.

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(BC30512, BC42016, BC30518, BC30519)
        Protected Overrides Function GetDescription(context As CodeFixContext, semanticModel As SemanticModel,
                Optional targetNode As SyntaxNode = Nothing, Optional conversionType As ITypeSymbol = Nothing) As String
            If targetNode IsNot Nothing Then
                Return String.Format(
                    VBFeaturesResources.Cast_0_to_1, targetNode.GetText(),
                    conversionType.ToMinimalDisplayString(semanticModel, context.Span.Start))
            End If
            Return FeaturesResources.Add_explicit_cast
        End Function

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

            ' The error happens either on an assignement operation Or on an invocation expression.
            ' If the error happens on assignment operation, "ConvertedType" Is different from the current "Type"
            Dim mutablePotentialConversionTypes = ArrayBuilder(Of (ExpressionSyntax, ITypeSymbol)).GetInstance()
            Select Case diagnosticId
                Case BC30512, BC42016
                    Dim argument = spanNode.GetAncestors(Of ArgumentSyntax).FirstOrDefault()
                    If argument IsNot Nothing AndAlso argument.GetExpression.Equals(spanNode) Then
                        ' spanNode is an argument expression
                        Dim argumentList = argument.GetAncestorOrThis(Of ArgumentListSyntax)
                        Dim invocationNode = argumentList.Parent

                        mutablePotentialConversionTypes.AddRange(GetPotentialConversionTypes(semanticModel, root,
                            argument, argumentList, invocationNode, cancellationToken))
                    Else
                        ' spanNode is a right expression in assignment operation
                        Dim inferenceService = document.GetRequiredLanguageService(Of ITypeInferenceService)()
                        Dim conversionType = inferenceService.InferType(semanticModel, spanNode, False, cancellationToken)
                        mutablePotentialConversionTypes.Add((spanNode, conversionType))
                    End If
                Case BC30518, BC30519
                    Dim invocationNode = spanNode.GetAncestors(Of ExpressionSyntax).FirstOrDefault(
                        Function(node) Not node.ChildNodes.OfType(Of ArgumentListSyntax).IsEmpty())
                    Dim argumentList = invocationNode.ChildNodes.OfType(Of ArgumentListSyntax).FirstOrDefault()

                    ' spanArgument is null because the span is on the invocation identifier name according to BC30519
                    mutablePotentialConversionTypes.AddRange(GetPotentialConversionTypes(semanticModel, root,
                        spanArgument:=Nothing, argumentList, invocationNode, cancellationToken))
            End Select

            ' clear up duplicate types
            potentialConversionTypes = FilterValidPotentialConversionTypes(semanticModel,
                document.GetRequiredLanguageService(Of ISyntaxFactsService), mutablePotentialConversionTypes)
            Return Not potentialConversionTypes.IsEmpty
        End Function

        Protected Overrides Function ClassifyConversion(semanticModel As SemanticModel,
                expression As ExpressionSyntax, type As ITypeSymbol) As CommonConversion
            Return semanticModel.ClassifyConversion(expression, type).ToCommonConversion()
        End Function

        Protected Overrides Function GetArguments(argumentList As SyntaxNode) As SeparatedSyntaxList(Of SyntaxNode)
            Return If(TryCast(argumentList, ArgumentListSyntax)?.Arguments,
                SyntaxFactory.SeparatedList(Of ArgumentSyntax)())
        End Function

        Protected Overrides Function GenerateNewArgument(
                oldArgument As SyntaxNode, conversionType As ITypeSymbol) As SyntaxNode
            Select Case oldArgument.Kind
                Case SyntaxKind.SimpleArgument
                    Dim simpleArgument = DirectCast(oldArgument, SimpleArgumentSyntax)
                    Return simpleArgument.WithExpression(
                        simpleArgument.GetExpression().Cast(conversionType, Nothing))
                Case Else
                    Return oldArgument
            End Select
        End Function

        Protected Overrides Function GetArgumentExpression(argument As SyntaxNode) As ExpressionSyntax
            Return TryCast(argument, ArgumentSyntax)?.GetExpression()
        End Function

        Protected Overrides Function IsDeclarationExpression(expression As ExpressionSyntax) As Boolean
            ' VB does not have keyword "out", so VB doesn't support declaration declaration in an argument
            Return False
        End Function

        Protected Overrides Function TryGetName(argument As SyntaxNode) As String
            Return If(TryCast(argument, ArgumentSyntax)?.IsNamed,
                DirectCast(argument, SimpleArgumentSyntax).NameColonEquals.Name.Identifier.ValueText,
                Nothing)
        End Function

        Protected Overrides Function GenerateNewArgumentList(
                oldArgumentList As SyntaxNode, newArguments As List(Of SyntaxNode)) As SyntaxNode
            Return If(TryCast(oldArgumentList, ArgumentListSyntax)?.WithArguments(SyntaxFactory.SeparatedList(newArguments)),
                oldArgumentList)
        End Function

        Protected Overrides Function IsInvocationExpressionWithNewArgumentsApplicable(
                semanticModel As SemanticModel, root As SyntaxNode, oldArgumentList As SyntaxNode,
                newArguments As List(Of SyntaxNode), targetNode As SyntaxNode) As Boolean
            Dim newRoot = root.ReplaceNode(oldArgumentList, GenerateNewArgumentList(oldArgumentList, newArguments))
            Dim newArgumentListNode = newRoot.FindNode(targetNode.Span).GetAncestorOrThis(Of ArgumentListSyntax)
            Dim symbolInfo As SymbolInfo
            Select Case newArgumentListNode.Parent.Kind
                Case SyntaxKind.Attribute
                    Dim attribute = DirectCast(newArgumentListNode.Parent, AttributeSyntax)
                    symbolInfo = semanticModel.GetSpeculativeSymbolInfo(attribute.SpanStart, attribute)
                Case Else
                    Dim expression = newArgumentListNode.Parent
                    symbolInfo = semanticModel.GetSpeculativeSymbolInfo(
                        expression.SpanStart, expression, SpeculativeBindingOption.BindAsExpression)
            End Select
            Return symbolInfo.Symbol IsNot Nothing
        End Function
    End Class
End Namespace
