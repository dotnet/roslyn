Imports System.Runtime.CompilerServices
Imports Roslyn.Compilers.VisualBasic

Namespace Roslyn.Services.VisualBasic.Extensions
    Friend Module TypeStatementSyntaxExtensions
        <Extension()>
        Public Function WithModifiers(node As TypeStatementSyntax, modifiers As SyntaxTokenList) As TypeStatementSyntax
            Select Case node.Kind
                Case SyntaxKind.ModuleStatement
                    Return DirectCast(node, ModuleStatementSyntax).WithModifiers(modifiers)
                Case SyntaxKind.InterfaceStatement
                    Return DirectCast(node, InterfaceStatementSyntax).WithModifiers(modifiers)
                Case SyntaxKind.StructureStatement
                    Return DirectCast(node, StructureStatementSyntax).WithModifiers(modifiers)
                Case SyntaxKind.ClassStatement
                    Return DirectCast(node, ClassStatementSyntax).WithModifiers(modifiers)
            End Select

            Throw Contract.Unreachable
        End Function
    End Module
End Namespace