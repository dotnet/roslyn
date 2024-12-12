' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Newtonsoft.Json

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels

    Friend Class UnifiedSettingsOption(Of T)
        <JsonProperty(NameOf(Title))>
        <JsonConverter(GetType(ResourceConverter))>
        Public Property Title As String

        <JsonProperty(NameOf(Type))>
        Public Property Type As String

        <JsonProperty(NameOf([Default]))>
        Public Property [Default] As T

        <JsonProperty(NameOf(AlternateDefault))>
        Public Property AlternateDefault As AlternateDefault(Of T)

        <JsonProperty(NameOf(Order))>
        Public Property Order As Integer

        <JsonProperty(NameOf(EnableWhen))>
        Public Property EnableWhen As String

        <JsonProperty(NameOf(Migration))>
        Public Property Migration As Migration

    End Class
End Namespace
