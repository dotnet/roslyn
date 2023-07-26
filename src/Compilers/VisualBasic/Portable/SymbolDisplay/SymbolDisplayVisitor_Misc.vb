' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class SymbolDisplayVisitor
        Friend Overrides Sub VisitPreprocessing(symbol As IPreprocessingSymbol)
            Dim part = New SymbolDisplayPart(SymbolDisplayPartKind.PreprocessingName, symbol, symbol.Name)
            builder.Add(part)
        End Sub
    End Class
End Namespace
