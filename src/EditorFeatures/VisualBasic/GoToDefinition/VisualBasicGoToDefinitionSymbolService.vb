' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.GoToDefinition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.GoToDefinition
    <ExportLanguageService(GetType(IGoToDefinitionSymbolService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGoToDefinitionSymbolService
        Inherits AbstractGoToDefinitionSymbolService

        Protected Overrides Function FindRelatedExplicitlyDeclaredSymbol(symbol As ISymbol, compilation As Compilation) As ISymbol
            Return symbol.FindRelatedExplicitlyDeclaredSymbol(compilation)
        End Function

        Protected Overrides Function TokenIsPartOfDeclaringSyntax(token As SyntaxToken) As Boolean
            Dim parentNode = token.Parent
            Select Case parentNode.Kind()
                Case SyntaxKind.ClassStatement,
                     SyntaxKind.PropertyStatement,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement,
                     SyntaxKind.SubNewStatement,
                     SyntaxKind.EventStatement,
                     SyntaxKind.GetAccessorStatement,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerStatement,
                     SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement,
                     SyntaxKind.TypeParameter,
                     SyntaxKind.EnumStatement,
                     SyntaxKind.EnumMemberDeclaration,
                     SyntaxKind.AnonymousObjectCreationExpression
                    Return True
                Case SyntaxKind.ModifiedIdentifier
                    Return If(parentNode?.Parent.IsKind(SyntaxKind.VariableDeclarator, SyntaxKind.Parameter), False)
                Case SyntaxKind.IdentifierName
                    Return If(parentNode?.Parent.IsKind(SyntaxKind.NameColonEquals, SyntaxKind.NamedFieldInitializer), False)
            End Select

            Return False
        End Function
    End Class
End Namespace