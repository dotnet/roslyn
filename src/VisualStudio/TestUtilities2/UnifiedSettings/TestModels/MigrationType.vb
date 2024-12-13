' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Newtonsoft.Json

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
    Friend Class MigrationType
        <JsonProperty("input")>
        Public ReadOnly Property Input As Input

        Public Sub New(input As Input)
            Me.Input = input
        End Sub
    End Class
End Namespace
