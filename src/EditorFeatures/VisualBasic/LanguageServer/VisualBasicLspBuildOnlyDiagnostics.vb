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
        "BC31091", ' ERRID.ERR_TypeRefResolutionError3,
        "BC35000", ' ERRID.ERR_MissingRuntimeHelper,
        "BC36597", ' ERRID.ERR_CannotGotoNonScopeBlocksWithClosure
        "BC37327"  ' ERRID.ERR_SymbolDefinedInAssembly
    )>
    Friend NotInheritable Class VisualBasicLspBuildOnlyDiagnostics
        Implements ILspBuildOnlyDiagnostics

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub
    End Class
End Namespace
