' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Internal.Log
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    <ExportLanguageService(GetType(ISimplificationService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicSimplificationService
        Inherits AbstractSimplificationService(Of ExpressionSyntax, ExecutableStatementSyntax, CrefReferenceSyntax)

        Private Shared ReadOnly s_reducers As ImmutableArray(Of AbstractReducer) =
            ImmutableArray.Create(Of AbstractReducer)(
                New VisualBasicExtensionMethodReducer(),
                New VisualBasicCastReducer(),
                New VisualBasicNameReducer(),
                New VisualBasicParenthesesReducer(),
                New VisualBasicCallReducer(),
                New VisualBasicEscapingReducer(), ' order before VisualBasicMiscellaneousReducer, see RenameNewOverload test
                New VisualBasicMiscellaneousReducer(),
                New VisualBasicCastReducer(),
                New VisualBasicVariableDeclaratorReducer(),
                New VisualBasicInferredMemberNameReducer())

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(s_reducers)
        End Sub

        Public Overrides ReadOnly Property DefaultOptions As SimplifierOptions
            Get
                Return VisualBasicSimplifierOptions.Default
            End Get
        End Property

        Public Overrides Function GetSimplifierOptions(options As AnalyzerConfigOptions, fallbackOptions As SimplifierOptions) As SimplifierOptions
            Return VisualBasicSimplifierOptions.Create(options, DirectCast(fallbackOptions, VisualBasicSimplifierOptions))
        End Function

        Public Overrides Function Expand(node As SyntaxNode, semanticModel As SemanticModel, aliasReplacementAnnotation As SyntaxAnnotation, expandInsideNode As Func(Of SyntaxNode, Boolean), expandParameter As Boolean, cancellationToken As CancellationToken) As SyntaxNode
            Using Logger.LogBlock(FunctionId.Simplifier_ExpandNode, cancellationToken)
                If TypeOf node Is ExpressionSyntax OrElse
                    TypeOf node Is StatementSyntax OrElse
                    TypeOf node Is AttributeSyntax OrElse
                    TypeOf node Is SimpleArgumentSyntax OrElse
                    TypeOf node Is CrefReferenceSyntax OrElse
                    TypeOf node Is TypeConstraintSyntax Then

                    Dim rewriter = New Expander(semanticModel, expandInsideNode, cancellationToken, expandParameter, aliasReplacementAnnotation)
                    Return rewriter.Visit(node)
                Else
                    Throw New ArgumentException(
                        VBWorkspaceResources.Only_attributes_expressions_or_statements_can_be_made_explicit,
                        paramName:=NameOf(node))
                End If
            End Using
        End Function

        Public Overrides Function Expand(token As SyntaxToken, semanticModel As SemanticModel, expandInsideNode As Func(Of SyntaxNode, Boolean), cancellationToken As CancellationToken) As SyntaxToken
            Using Logger.LogBlock(FunctionId.Simplifier_ExpandToken, cancellationToken)
                Dim rewriter = New Expander(semanticModel, expandInsideNode, cancellationToken)
                Return TryEscapeIdentifierToken(rewriter.VisitToken(token))
            End Using
        End Function

        Protected Overrides Function GetSpeculativeSemanticModel(ByRef nodeToSpeculate As SyntaxNode, originalSemanticModel As SemanticModel, originalNode As SyntaxNode) As SemanticModel
            Contract.ThrowIfNull(nodeToSpeculate)
            Contract.ThrowIfNull(originalNode)

            Dim speculativeModel As SemanticModel
            Dim methodBlockBase = TryCast(nodeToSpeculate, MethodBlockBaseSyntax)

            ' Speculation over Field Declarations is not supported
            If originalNode.Kind() = SyntaxKind.VariableDeclarator AndAlso
               originalNode.Parent.Kind() = SyntaxKind.FieldDeclaration Then
                Return originalSemanticModel
            End If

            If methodBlockBase IsNot Nothing Then
                ' Certain reducers for VB (escaping, parentheses) require to operate on the entire method body, rather than individual statements.
                ' Hence, we need to reduce the entire method body as a single unit.
                ' However, there is no SyntaxNode for the method body or statement list, hence NodesAndTokensToReduceComputer added the MethodBlockBaseSyntax to the list of nodes to be reduced.
                ' Here we make sure that we create a speculative semantic model for the method body for the given MethodBlockBaseSyntax.
                Dim originalMethod = DirectCast(originalNode, MethodBlockBaseSyntax)
                Contract.ThrowIfFalse(originalMethod.Statements.Any(), "How did empty method body get reduced?")

                Dim position As Integer
                If originalSemanticModel.IsSpeculativeSemanticModel Then
                    ' Chaining speculative model Not supported, speculate off the original model.
                    Debug.Assert(originalSemanticModel.ParentModel IsNot Nothing)
                    Debug.Assert(Not originalSemanticModel.ParentModel.IsSpeculativeSemanticModel)
                    position = originalSemanticModel.OriginalPositionForSpeculation
                    originalSemanticModel = originalSemanticModel.ParentModel
                Else
                    position = originalMethod.Statements.First.SpanStart
                End If

                speculativeModel = Nothing
                originalSemanticModel.TryGetSpeculativeSemanticModelForMethodBody(position, methodBlockBase, speculativeModel)
                Return speculativeModel
            End If

            Contract.ThrowIfFalse(SpeculationAnalyzer.CanSpeculateOnNode(nodeToSpeculate))

            Dim isAsNewClause = nodeToSpeculate.Kind = SyntaxKind.AsNewClause
            If isAsNewClause Then
                ' Currently, there is no support for speculating on an AsNewClauseSyntax node.
                ' So we synthesize an EqualsValueSyntax with the inner NewExpression and speculate on this EqualsValueSyntax node.
                Dim asNewClauseNode = DirectCast(nodeToSpeculate, AsNewClauseSyntax)
                nodeToSpeculate = SyntaxFactory.EqualsValue(asNewClauseNode.NewExpression)
                nodeToSpeculate = asNewClauseNode.CopyAnnotationsTo(nodeToSpeculate)
            End If

            speculativeModel = SpeculationAnalyzer.CreateSpeculativeSemanticModelForNode(originalNode, nodeToSpeculate, originalSemanticModel)

            If isAsNewClause Then
                nodeToSpeculate = speculativeModel.SyntaxTree.GetRoot()
            End If

            Return speculativeModel
        End Function

        Protected Overrides Function TransformReducedNode(reducedNode As SyntaxNode, originalNode As SyntaxNode) As SyntaxNode
            ' Please see comments within the above GetSpeculativeSemanticModel method for details.

            If originalNode.Kind = SyntaxKind.AsNewClause AndAlso reducedNode.Kind = SyntaxKind.EqualsValue Then
                Return originalNode.ReplaceNode(DirectCast(originalNode, AsNewClauseSyntax).NewExpression, DirectCast(reducedNode, EqualsValueSyntax).Value)
            End If

            Dim originalMethod = TryCast(originalNode, MethodBlockBaseSyntax)
            If originalMethod IsNot Nothing Then
                Dim reducedMethod = DirectCast(reducedNode, MethodBlockBaseSyntax)
                reducedMethod = reducedMethod.ReplaceNode(reducedMethod.BlockStatement, originalMethod.BlockStatement)
                Return reducedMethod.ReplaceNode(reducedMethod.EndBlockStatement, originalMethod.EndBlockStatement)
            End If

            Return reducedNode
        End Function

        Protected Overrides Function GetNodesAndTokensToReduce(root As SyntaxNode, isNodeOrTokenOutsideSimplifySpans As Func(Of SyntaxNodeOrToken, Boolean)) As ImmutableArray(Of NodeOrTokenToReduce)
            Return NodesAndTokensToReduceComputer.Compute(root, isNodeOrTokenOutsideSimplifySpans)
        End Function

        Protected Overrides Function NodeRequiresNonSpeculativeSemanticModel(node As SyntaxNode) As Boolean
            Return node IsNot Nothing AndAlso node.Parent IsNot Nothing AndAlso
                TypeOf node Is VariableDeclaratorSyntax AndAlso
                TypeOf node.Parent Is FieldDeclarationSyntax
        End Function

        Private Const s_BC50000_UnusedImportsClause As String = "BC50000"
        Private Const s_BC50001_UnusedImportsStatement As String = "BC50001"

        Protected Overrides Sub GetUnusedNamespaceImports(model As SemanticModel, namespaceImports As HashSet(Of SyntaxNode), cancellationToken As CancellationToken)
            Dim root = model.SyntaxTree.GetRoot(cancellationToken)
            Dim diagnostics = model.GetDiagnostics(cancellationToken:=cancellationToken)

            For Each diagnostic In diagnostics
                If diagnostic.Id = s_BC50000_UnusedImportsClause OrElse diagnostic.Id = s_BC50001_UnusedImportsStatement Then
                    Dim node = root.FindNode(diagnostic.Location.SourceSpan)
                    Dim statement = TryCast(node, ImportsStatementSyntax)
                    Dim clause = TryCast(node, ImportsStatementSyntax)
                    If statement IsNot Nothing Or clause IsNot Nothing Then
                        namespaceImports.Add(node)
                    End If
                End If
            Next
        End Sub

    End Class
End Namespace
