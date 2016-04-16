' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource
    Friend Class ConstructorDeclarationOutliner
        Inherits AbstractMetadataAsSourceOutliner(Of SubNewStatementSyntax)

        Protected Overrides Function GetEndToken(node As SubNewStatementSyntax) As SyntaxToken
            Return If(node.Modifiers.Count > 0,
                      node.Modifiers.First(),
                      node.DeclarationKeyword)
        End Function
    End Class
End Namespace
