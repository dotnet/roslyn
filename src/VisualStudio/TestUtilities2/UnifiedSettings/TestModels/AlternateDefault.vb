' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Newtonsoft.Json

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
    Friend Class AlternateDefault(Of T)
        <JsonProperty(NameOf(FlagName))>
        Public Property FlagName As String

        <JsonProperty("default")>
        Public Property [Default] As T
    End Class
End Namespace
