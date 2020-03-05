﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.ConflictMarkerResolution
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.ConflictMarkerResolution
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicResolveConflictMarkerCodeFixProvider
        Inherits AbstractResolveConflictMarkerCodeFixProvider

        Private Const BC37284 As String = NameOf(BC37284)

        <ImportingConstructor>
        Public Sub New()
            MyBase.New(VisualBasicSyntaxKinds.Instance, BC37284)
        End Sub
    End Class
End Namespace
