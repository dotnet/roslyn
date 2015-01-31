' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Friend Class TestOrderableLanguageMetadata
        Inherits OrderableLanguageMetadata

        Public Sub New(language As String)
            MyBase.New(New Dictionary(Of String, Object) From {
                       {"Language", language},
                       {"Name", String.Empty}
                       })
        End Sub
    End Class
End Namespace
