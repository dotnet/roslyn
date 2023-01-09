' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServer

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageServer
    ' Keep in sync with IsBuildOnlyDiagnostic
    ' src\Compilers\VisualBasic\Portable\Errors\ErrorFacts.vb
    <LspBuildOnlyDiagnostics(
        "VB31093", ' ERRID.ERR_TypeRefResolutionError3,
        "VB35000", ' ERRID.ERR_MissingRuntimeHelper,
        "VB36957" ' ERRID.ERR_CannotGotoNonScopeBlocksWithClosure
    )>
    Friend Class VisualBasicLspBuildOnlyDiagnostics
        Implements ILspBuildOnlyDiagnostics

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub
    End Class
End Namespace
