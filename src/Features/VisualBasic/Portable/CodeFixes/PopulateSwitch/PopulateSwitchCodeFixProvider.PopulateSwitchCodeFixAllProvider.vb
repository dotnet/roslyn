Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.PopulateSwitch
    Partial Friend Class PopulateSwitchCodeFixProvider
        Inherits CodeFixProvider
		
		Private Class PopulateSwitchCodeFixAllProvider
            Inherits BatchSimplificationFixAllProvider
			
		    Friend Shared Shadows ReadOnly Instance As PopulateSwitchCodeFixAllProvider = New PopulateSwitchCodeFixAllProvider()

            Protected Overrides Function GetNodeToSimplify(root As SyntaxNode, model As SemanticModel, diagnostic As Diagnostic, workspace As Workspace, ByRef codeActionId As String, cancellationToken As CancellationToken) As SyntaxNode
                codeActionId = Nothing
                Return GetSelectBlockNode(root, diagnostic.Location.SourceSpan)
            End Function

            Protected Overrides ReadOnly Property NeedsParentFixup As Boolean
                Get
                    Return True
                End Get
            End Property

            Protected Overrides Async Function AddSimplifyAnnotationsAsync(document As Document, nodeToSimplify As SyntaxNode, cancellationToken As CancellationToken) As Task(Of Document)
				
				Dim selectBlock = TryCast(nodeToSimplify, SelectBlockSyntax)
				If selectBlock Is Nothing
					Return Nothing
				End If

				Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
				Dim model = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

				Return Await AddMissingSwitchLabelsAsync(model, document, root, selectBlock).ConfigureAwait(False)
            End Function
        End Class
    End Class
End Namespace