' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.GoToDefinition
    <ExportLanguageService(GetType(IGoToDefinitionSymbolService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGoToDefinitionSymbolService
        Inherits AbstractGoToDefinitionSymbolService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function FindRelatedExplicitlyDeclaredSymbol(symbol As ISymbol, compilation As Compilation) As ISymbol
            Return symbol.FindRelatedExplicitlyDeclaredSymbol(compilation)
        End Function
    End Class
End Namespace