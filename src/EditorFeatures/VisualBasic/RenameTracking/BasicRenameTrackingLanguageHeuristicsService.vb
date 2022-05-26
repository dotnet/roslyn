' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.RenameTracking
    <ExportLanguageService(GetType(IRenameTrackingLanguageHeuristicsService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class BasicRenameTrackingLanguageHeuristicsService
        Implements IRenameTrackingLanguageHeuristicsService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function IsIdentifierValidForRenameTracking(name As String) As Boolean Implements IRenameTrackingLanguageHeuristicsService.IsIdentifierValidForRenameTracking
            Return True
        End Function
    End Class
End Namespace
