' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.RemoveUnusedMembers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedMembers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicRemoveUnusedMembersDiagnosticAnalyzer
        Inherits AbstractRemoveUnusedMembersDiagnosticAnalyzer(Of DocumentationCommentTriviaSyntax, IdentifierNameSyntax)

        Protected Overrides Sub HandleNamedTypeSymbolStart(context As SymbolStartAnalysisContext, onSymbolUsageFound As Action(Of ISymbol, ValueUsageInfo))
            ' Mark all methods with handles clause as having a read reference
            ' to ensure that we consider the method as "used".
            ' Such methods are essentially event handlers and are normally
            ' not referenced directly.
            For Each method In DirectCast(context.Symbol, INamedTypeSymbol).GetMembers().OfType(Of IMethodSymbol)
                If Not method.HandledEvents.IsEmpty Then
                    onSymbolUsageFound(method, ValueUsageInfo.Read)
                End If
            Next

            ' Register syntax node action for HandlesClause
            ' This is a workaround for following bugs:
            '  1. https://github.com/dotnet/roslyn/issues/30978
            '  2. https://github.com/dotnet/roslyn/issues/30979

            context.RegisterSyntaxNodeAction(
                Sub(syntaxNodeContext As SyntaxNodeAnalysisContext)
                    AnalyzeHandlesClause(syntaxNodeContext, onSymbolUsageFound)
                End Sub,
                SyntaxKind.HandlesClause)
        End Sub

        Private Shared Sub AnalyzeHandlesClause(context As SyntaxNodeAnalysisContext, onSymbolUsageFound As Action(Of ISymbol, ValueUsageInfo))
            ' Identify all symbol references within the HandlesClause.
            For Each node In context.Node.DescendantNodes()
                Dim symbolInfo = context.SemanticModel.GetSymbolInfo(node, context.CancellationToken)
                For Each symbol In symbolInfo.GetAllSymbols()
                    onSymbolUsageFound(symbol, ValueUsageInfo.Read)
                Next
            Next
        End Sub

        Protected Overrides Sub AddAllDocumentationComments(
                namedType As INamedTypeSymbol,
                documentationComments As ArrayBuilder(Of DocumentationCommentTriviaSyntax),
                cancellationToken As CancellationToken)

            Dim stack = ArrayBuilder(Of TypeBlockSyntax).GetInstance()

            For Each reference In namedType.DeclaringSyntaxReferences
                Dim node = reference.GetSyntax(cancellationToken)

                Dim typeBlock = TryCast(node, TypeBlockSyntax)
                If typeBlock Is Nothing Then
                    Continue For
                End If

                stack.Clear()
                stack.Push(typeBlock)

                While stack.Count > 0
                    Dim currentType = stack.Pop()

                    ' Add the doc comments on the type itself.
                    AddDocumentationComments(currentType, documentationComments)

                    ' Walk each member
                    For Each member In currentType.GetMembers()
                        Dim childType = TryCast(member, TypeBlockSyntax)
                        If childType IsNot Nothing Then
                            ' If the member Is a nested type, recurse into it.
                            stack.Push(childType)
                        Else
                            ' Otherwise, add the doc comments on the member itself.
                            AddDocumentationComments(member, documentationComments)
                        End If
                    Next
                End While
            Next

            stack.Free()
        End Sub
    End Class
End Namespace
