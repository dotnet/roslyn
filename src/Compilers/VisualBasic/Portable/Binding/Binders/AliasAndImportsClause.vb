' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Structure AliasAndImportsClausePosition
        Public ReadOnly [Alias] As AliasSymbol
        Public ReadOnly ImportsClausePosition As Integer

        Public Sub New([alias] As AliasSymbol, importsClausePosition As Integer)
            Me.Alias = [alias]
            Me.ImportsClausePosition = importsClausePosition
        End Sub
    End Structure
End Namespace
