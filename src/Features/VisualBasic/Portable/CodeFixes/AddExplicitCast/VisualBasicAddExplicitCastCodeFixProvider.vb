' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.AddExplicitCast
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.AddExplicitCast

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddExplicitCast), [Shared]>
    Friend NotInheritable Class VisualBasicAddExplicitCastCodeFixProvider
        Inherits AbstractAddExplicitCastCodeFixProvider(Of
            ExpressionSyntax, ArgumentListSyntax, ArgumentSyntax)

        Friend Const BC30512 As String = "BC30512" ' Option Strict On disallows implicit conversions from '{0}' to '{1}'.
        Friend Const BC42016 As String = "BC42016" ' Implicit conversions from '{0}' to '{1}'.
        Friend Const BC30518 As String = "BC30518" ' Overload resolution failed because no accessible 'sub1' can be called with these arguments.
        Friend Const BC30519 As String = "BC30519" ' Overload resolution failed because no accessible 'sub1' can be called without a narrowing conversion.

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30512, BC42016, BC30518, BC30519)
            End Get
        End Property


        Protected Overrides Function GetDescription(context As CodeFixContext, semanticModel As SemanticModel, Optional targetNode As SyntaxNode = Nothing,
                Optional conversionType As ITypeSymbol = Nothing) As String
            If targetNode IsNot Nothing Then
                Return String.Format(
                    VBFeaturesResources.Cast_0_to_1, targetNode.GetText(),
                    conversionType.ToMinimalDisplayString(semanticModel, context.Span.Start))
            End If
            Return VBFeaturesResources.Insert_Missing_Cast
        End Function

        Protected Overrides Function ApplyFix(currentRoot As SyntaxNode, targetNode As ExpressionSyntax,
                conversionType As ITypeSymbol) As SyntaxNode
            Return currentRoot.ReplaceNode(targetNode, targetNode.Cast(conversionType, Nothing))
        End Function

        Protected Overrides Function TryGetTargetTypeInfo(document As Document, semanticModel As SemanticModel, root As SyntaxNode,
                diagnosticId As String, spanNode As ExpressionSyntax, cancellationToken As CancellationToken,
                ByRef potentialConversionTypes As ImmutableArray(Of Tuple(Of ExpressionSyntax, ITypeSymbol))) As Boolean
            potentialConversionTypes = ImmutableArray(Of Tuple(Of ExpressionSyntax, ITypeSymbol)).Empty

            ' The error happens either on an assignement operation Or on an invocation expression.
            ' If the error happens on assignment operation, "ConvertedType" Is different from the current "Type"
            Dim mutablePotentialConversionTypes = ArrayBuilder(Of Tuple(Of ExpressionSyntax, ITypeSymbol)).GetInstance()
            Select Case diagnosticId
                Case BC30512, BC42016
                    Dim inferenceService = document.GetLanguageService(Of ITypeInferenceService)()
                    Dim conversionType = inferenceService.InferType(semanticModel, spanNode, False, cancellationToken)
                    mutablePotentialConversionTypes.Add(Tuple.Create(spanNode, conversionType))
                Case BC30518, BC30519
                    Dim invocationNode = spanNode.GetAncestors(Of ExpressionSyntax).FirstOrDefault() ' invocation node could be Invocation Expression, Object Creation, Base Constructor...
                    Dim argumentList = TryCast(invocationNode.ChildNodes().FirstOrDefault(Function(node As SyntaxNode) TryCast(node, ArgumentListSyntax) IsNot Nothing), ArgumentListSyntax)
                    mutablePotentialConversionTypes.AddRange(GetPotentialConversionTypes(semanticModel, root,
                        Nothing, argumentList, invocationNode, cancellationToken))
            End Select

            ' clear up duplicate types
            potentialConversionTypes = FilterValidPotentialConversionTypes(semanticModel, mutablePotentialConversionTypes)
            Return Not potentialConversionTypes.IsEmpty
        End Function

        Protected Overrides Function IsObjectCreationExpression(targetNode As ExpressionSyntax) As Boolean
            Return targetNode.Kind() = SyntaxKind.ObjectCreationExpression
        End Function

        Protected Overrides Function ClassifyConversionExists(semanticModel As SemanticModel,
                expression As ExpressionSyntax, type As ITypeSymbol) As Boolean
            Return semanticModel.ClassifyConversion(expression, type).Exists
        End Function

        Protected Overrides Function IsConversionIdentity(semanticModel As SemanticModel,
                expression As ExpressionSyntax, type As ITypeSymbol) As Boolean
            Return semanticModel.ClassifyConversion(expression, type).IsIdentity
        End Function

        Protected Overrides Function GetArguments(argumentList As ArgumentListSyntax) As SeparatedSyntaxList(Of ArgumentSyntax)
            Return argumentList.Arguments
        End Function

        Protected Overrides Function GenerateNewArgument(
                oldArgument As ArgumentSyntax, conversionType As ITypeSymbol) As ArgumentSyntax
            Select Case oldArgument.Kind
                Case SyntaxKind.SimpleArgument
                    Dim simpleArgument = DirectCast(oldArgument, SimpleArgumentSyntax)
                    Return simpleArgument.WithExpression(
                        simpleArgument.GetExpression().Cast(conversionType, Nothing))
                Case Else
                    Return oldArgument
            End Select
        End Function

        Protected Overrides Function GetArgumentExpression(argument As ArgumentSyntax) As ExpressionSyntax
            Return argument.GetExpression()
        End Function

        Protected Overrides Function IsDeclarationExpression(expression As ExpressionSyntax) As Boolean
            ' VB does not have keyword "out", so VB doesn't support declaration declaration in an argument
            Return False
        End Function

        Protected Overrides Function TryGetName(argument As ArgumentSyntax) As String
            Return If(argument.IsNamed,
                DirectCast(argument, SimpleArgumentSyntax).NameColonEquals.Name.Identifier.ValueText,
                Nothing)
        End Function

        Protected Overrides Sub SortConversionTypes(semanticModel As SemanticModel, conversionTypes As ArrayBuilder(Of Tuple(Of ExpressionSyntax, ITypeSymbol)), argumentList As ArgumentListSyntax)
            conversionTypes.Sort(New InheritanceDistanceComparer(Of ExpressionSyntax, ArgumentSyntax)(semanticModel, argumentList.Arguments))
        End Sub

        Protected Overrides Function GenerateNewArgumentList(
                oldArgumentList As ArgumentListSyntax, newArguments As List(Of ArgumentSyntax)) As ArgumentListSyntax
            Return oldArgumentList.WithArguments(SyntaxFactory.SeparatedList(newArguments))
        End Function

        Protected Overrides Function IsConversionUserDefined(semanticModel As SemanticModel, expression As ExpressionSyntax, type As ITypeSymbol) As Boolean
            Return semanticModel.ClassifyConversion(expression, type).IsUserDefined
        End Function
    End Class
End Namespace
