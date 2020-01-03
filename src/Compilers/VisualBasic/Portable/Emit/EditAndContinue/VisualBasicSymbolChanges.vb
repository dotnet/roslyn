' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend NotInheritable Class VisualBasicSymbolChanges
        Inherits SymbolChanges

        Public Sub New(definitionMap As DefinitionMap, edits As IEnumerable(Of SemanticEdit), isAddedSymbol As Func(Of ISymbol, Boolean))
            MyBase.New(definitionMap, edits, isAddedSymbol)
        End Sub

        Protected Overrides Function GetISymbolInternalOrNull(symbol As ISymbol) As ISymbolInternal
            Return TryCast(symbol, Symbol)
        End Function

    End Class
End Namespace
