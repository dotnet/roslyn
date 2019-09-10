' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.GoToBase
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.GoToBase
    <ExportLanguageService(GetType(IGoToBaseService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGoToBaseService
        Inherits AbstractGoToBaseService

        <ImportingConstructor>
        Public Sub New()
        End Sub
    End Class
End Namespace
