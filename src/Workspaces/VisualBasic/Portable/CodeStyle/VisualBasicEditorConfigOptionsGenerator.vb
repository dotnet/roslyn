' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeStyle
    <EditorConfigGenerator(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEditorConfigOptionsGenerator
        Implements IEditorConfigOptionsCollection

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function GetOptions() As IEnumerable(Of (String, ImmutableArray(Of IOption2))) Implements IEditorConfigOptionsCollection.GetOptions
            Dim builder = ArrayBuilder(Of (String, ImmutableArray(Of IOption2))).GetInstance()
            builder.Add((VBWorkspaceResources.VB_Coding_Conventions, VisualBasicCodeStyleOptions.AllOptions.As(Of IOption2)))
            Return builder.ToArrayAndFree()
        End Function
    End Class
End Namespace
