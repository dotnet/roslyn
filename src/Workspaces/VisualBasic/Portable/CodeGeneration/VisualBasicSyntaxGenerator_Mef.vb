' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    <ExportLanguageService(GetType(SyntaxGenerator), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicSyntaxGenerator
        Inherits SyntaxGenerator

        <ImportingConstructor>
        Public Sub New()
        End Sub
    End Class
End Namespace
