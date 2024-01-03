' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Navigation

Namespace Microsoft.CodeAnalysis.VisualBasic.Navigation
    <ExportLanguageService(GetType(INavigableItemsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicFindDefinitionService
        Inherits AbstractNavigableItemsService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub
    End Class
End Namespace
