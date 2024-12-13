' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Newtonsoft.Json

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
    Friend Class Map
        <JsonProperty("result")>
        Public ReadOnly Property Result As String
        <JsonProperty("match")>
        Public ReadOnly Property Match As Integer

        Public Sub New(result As String, match As Integer)
            Me.Result = result
            Me.Match = match
        End Sub
    End Class
End Namespace
