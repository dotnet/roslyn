' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    Friend Class LineCommitOptionsStorage
        Public Shared ReadOnly PrettyListing As New PerLanguageOption2(Of Boolean)("visual_basic_pretty_listing", defaultValue:=True)
    End Class
End Namespace
