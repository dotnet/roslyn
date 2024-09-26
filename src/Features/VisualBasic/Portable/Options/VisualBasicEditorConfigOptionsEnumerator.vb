' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle

Namespace Microsoft.CodeAnalysis.VisualBasic.Options
    <EditorConfigOptionsEnumerator(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEditorConfigOptionsEnumerator
        Implements IEditorConfigOptionsEnumerator

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Iterator Function GetOptions(includeUnsupported As Boolean) As IEnumerable(Of (String, ImmutableArray(Of IOption2))) Implements IEditorConfigOptionsEnumerator.GetOptions
            For Each entry In EditorConfigOptionsEnumerator.GetLanguageAgnosticEditorConfigOptions(includeUnsupported)
                Yield entry
            Next

            Yield (VBWorkspaceResources.VB_Coding_Conventions, VisualBasicCodeStyleOptions.EditorConfigOptions.As(Of IOption2))
        End Function
    End Class
End Namespace
