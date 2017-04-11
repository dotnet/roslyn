' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities

Public NotInheritable Class BasicSyntaxNodeGetOperationWalker
    Inherits SyntaxNodeGetOperationWalker

    Protected Overrides Function IsSyntaxNodeKindExcluded(node As SyntaxNode) As Boolean
        Return TypeOf node Is ImportsStatementSyntax OrElse
               TypeOf node Is NamespaceBlockSyntax OrElse
               TypeOf node Is CompilationUnitSyntax OrElse
               TypeOf node Is ModuleBlockSyntax
    End Function
End Class
