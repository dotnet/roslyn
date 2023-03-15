' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AddMissingImports
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.AddImport

Namespace Microsoft.CodeAnalysis.VisualBasic.AddMissingImports
    <ExportLanguageService(GetType(IAddMissingImportsFeatureService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicAddMissingImportsFeatureService
        Inherits AbstractAddMissingImportsFeatureService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = AddImportDiagnosticIds.FixableDiagnosticIds

        Protected Overrides Function GetFormatRules(text As SourceText) As ImmutableArray(Of AbstractFormattingRule)
            Return ImmutableArray.Create(Of AbstractFormattingRule)(New CleanUpNewLinesFormatter(text))
        End Function
    End Class
End Namespace
