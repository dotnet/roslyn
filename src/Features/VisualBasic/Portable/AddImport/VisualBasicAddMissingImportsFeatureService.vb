' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AddMissingImports
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.AddImport

Namespace Microsoft.CodeAnalysis.VisualBasic.AddMissingImports
    <ExportLanguageService(GetType(IAddMissingImportsFeatureService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicAddMissingImportsFeatureService
        Inherits AbstractAddMissingImportsFeatureService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = AddImportDiagnosticIds.FixableDiagnosticIds
    End Class
End Namespace
