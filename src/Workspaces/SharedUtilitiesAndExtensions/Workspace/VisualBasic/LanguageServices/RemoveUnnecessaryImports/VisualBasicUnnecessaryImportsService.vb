' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryImports

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryImports
    <ExportLanguageService(GetType(IUnnecessaryImportsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicUnnecessaryImportsService
        Inherits AbstractVisualBasicRemoveUnnecessaryImportsService

        <ImportingConstructor>
        Public Sub New()
        End Sub
    End Class
End Namespace
