Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend NotInheritable Class VisualBasicSyntaxDiffer
        Inherits SyntaxDiffer

        Public Sub New(oldNode As SyntaxNode, newNode As SyntaxNode, computeNewText As Boolean)
            MyBase.New(oldNode, newNode, computeNewText)
        End Sub

        Protected Overrides Function AreSimilarCore(node1 As SyntaxNode, node2 As SyntaxNode) As Boolean
            Dim kindGroup =
                    Function(node As SyntaxNode) As Integer
                        Select Case node.Kind()
                            Case SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.InterfaceBlock, SyntaxKind.ModuleBlock
                                Return 1
                            Case SyntaxKind.FunctionBlock, SyntaxKind.SubBlock
                                Return 2
                            Case Else
                                Return -1
                        End Select
                    End Function

            Dim node1Group = kindGroup(node1)

            Return node1Group <> -1 AndAlso node1Group = kindGroup(node2)
        End Function
    End Class
End NameSpace