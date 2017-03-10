' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure.MetadataAsSource
    Friend Class MetadataOperatorDeclarationStructureProvider
        Inherits AbstractMetadataAsSourceStructureProvider(Of OperatorStatementSyntax)

        Protected Overrides Function GetEndToken(node As OperatorStatementSyntax) As SyntaxToken
            Return If(node.Modifiers.Count > 0,
                      node.Modifiers.First(),
                      node.DeclarationKeyword)
        End Function
    End Class
End Namespace