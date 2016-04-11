' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings.MoveType
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.MoveType
    <ExportLanguageService(GetType(IMoveTypeService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicMoveTypeService
        Inherits AbstractMoveTypeService(Of VisualBasicMoveTypeService, TypeBlockSyntax, NamespaceBlockSyntax, DeclarationStatementSyntax, CompilationUnitSyntax)

        Protected Overrides Function IsPartial(typeDeclaration As TypeBlockSyntax) As Boolean
            Return typeDeclaration.BlockStatement.Modifiers.Any(SyntaxKind.PartialKeyword)
        End Function
    End Class
End Namespace
