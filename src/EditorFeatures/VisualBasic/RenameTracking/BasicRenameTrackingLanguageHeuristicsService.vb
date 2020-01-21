' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.RenameTracking
    <ExportLanguageService(GetType(IRenameTrackingLanguageHeuristicsService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class BasicRenameTrackingLanguageHeuristicsService
        Implements IRenameTrackingLanguageHeuristicsService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function IsIdentifierValidForRenameTracking(name As String) As Boolean Implements IRenameTrackingLanguageHeuristicsService.IsIdentifierValidForRenameTracking
            Return True
        End Function
    End Class
End Namespace
