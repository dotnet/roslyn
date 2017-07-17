' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#If False Then
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense
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
#end if
