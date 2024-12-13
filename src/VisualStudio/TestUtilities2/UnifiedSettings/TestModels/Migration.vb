' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Newtonsoft.Json

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
    Friend Class Migration
        <JsonProperty("pass")>
        Public ReadOnly Property Pass As Pass

        Public Sub New(pass As Pass)
            Me.Pass = pass
        End Sub
    End Class
End Namespace
