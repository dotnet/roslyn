' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Newtonsoft.Json

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
    Friend Class UnifiedSettingsCategory
        <JsonProperty(NameOf(Title))>
        <JsonConverter(GetType(ResourceConverter))>
        Public ReadOnly Property Title As String

        <JsonProperty(NameOf(LegacyOptionPageId))>
        Public ReadOnly Property LegacyOptionPageId As String

        Public Sub New(title As String, legacyOptionPageId As String)
            Me.Title = title
            Me.LegacyOptionPageId = legacyOptionPageId
        End Sub
    End Class
End Namespace
