' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.GoToDefinition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.GoToDefinition
    <ExportLanguageService(GetType(IGoToDefinitionSymbolService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGoToDefinitionSymbolService
        Inherits AbstractGoToDefinitionSymbolService

        Protected Overrides Function FindRelatedExplicitlyDeclaredSymbol(symbol As ISymbol, compilation As Compilation) As ISymbol
            Return symbol.FindRelatedExplicitlyDeclaredSymbol(compilation)
        End Function

        Protected Overrides Async Function GetDeclarationPositionIfOverride(syntaxTree As SyntaxTree,
                                                                            position As Integer,
                                                                            cancellationToken As CancellationToken) As Task(Of Integer?)

            Dim token = Await CodeAnalysis.Shared.Extensions.SyntaxTreeExtensions.GetTouchingTokenAsync(syntaxTree, position, cancellationToken).
                ConfigureAwait(False)

            If token.Kind() = SyntaxKind.OverridesKeyword Then
                Dim parent = token.Parent

                Select Case parent.Kind()
                    Case SyntaxKind.SubStatement, SyntaxKind.FunctionStatement
                        Dim method = DirectCast(parent, MethodStatementSyntax)
                        Return method.Identifier.SpanStart

                    Case SyntaxKind.PropertyStatement
                        Dim [property] = DirectCast(parent, PropertyStatementSyntax)
                        Return [property].Identifier.SpanStart

                End Select
            End If

            Return Nothing
        End Function

    End Class
End Namespace
