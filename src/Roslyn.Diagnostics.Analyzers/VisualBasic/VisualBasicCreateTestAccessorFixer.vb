' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Diagnostics.Analyzers

Namespace Roslyn.Diagnostics.VisualBasic.Analyzers
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Public NotInheritable Class VisualBasicCreateTestAccessorFixer
        Inherits CreateTestAccessorFixer

        Protected Overrides Function GetTypeDeclarationForNode(reportedNode As SyntaxNode) As SyntaxNode
            Return reportedNode.FirstAncestorOrSelf(Of TypeStatementSyntax)()?.Parent
        End Function
    End Class
End Namespace
