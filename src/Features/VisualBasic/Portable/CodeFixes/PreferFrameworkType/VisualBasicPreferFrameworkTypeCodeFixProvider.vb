'Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition

Namespace Microsoft.CodeAnalysis.CodeFixes.PreferFrameworkType

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.PreferFrameworkType), [Shared]>
    Friend Class VisualBasicPreferFrameworkTypeCodeFixProvider
        Inherits AbstractPreferFrameworkTypeCodeFixProvider

        Protected Overrides Function GenerateTypeSyntax(symbol As ITypeSymbol) As SyntaxNode
            Return symbol.GenerateTypeSyntax()
        End Function
    End Class
End Namespace
