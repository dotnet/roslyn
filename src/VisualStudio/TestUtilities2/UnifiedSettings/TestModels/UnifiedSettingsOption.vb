' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Options
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

        Public Shared Function CreateBooleanOption(
                roslynOption As IOption2,
                title As String,
                defaultValue As Boolean,
                alternativeDefault As Boolean,
                featureFlagOption As IOption2,
                enableWhenOptionAndValue As ([option] As String, value As Object),
                languageName As String) As UnifiedSettingsOption(Of Boolean)
            Dim type = roslynOption.Type
            Assert.True(type = GetType(Boolean) OrElse Nullable.GetUnderlyingType(type) = GetType(Boolean))
            Assert.NotEqual(defaultValue, alternativeDefault)

            Return New UnifiedSettingsOption(Of Boolean)() With {
                .Title = title,
                .Type = "boolean",
                .[Default] = defaultValue,
                .EnableWhen = $"config:{enableWhenOptionAndValue.option}=='{enableWhenOptionAndValue.value}'",
                .AlternateDefault = TestModels.AlternateDefault(Of Boolean).CreateFromOption(roslynOption, alternativeDefault)
                }

        End Function
    End Class
End Namespace
