' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Options
Imports Newtonsoft.Json

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
    Friend Class UnifiedSettingsOptionBase
        <JsonProperty(NameOf(Title))>
        <JsonConverter(GetType(ResourceConverter))>
        Public ReadOnly Property Title As String

        <JsonProperty(NameOf(Type))>
        Public ReadOnly Property Type As String

        <JsonProperty(NameOf(Order))>
        Public ReadOnly Property Order As Integer

        <JsonProperty(NameOf(EnableWhen))>
        Public ReadOnly Property EnableWhen As String

        <JsonProperty(NameOf(Migration))>
        Public ReadOnly Property Migration As Migration

        Public Sub New(title As String, type As String, order As Integer, enableWhen As String, migration As Migration)
            Me.Title = title
            Me.Type = type
            Me.Order = order
            Me.EnableWhen = enableWhen
            Me.Migration = migration
        End Sub

        Public Shared Function CreateBooleanOption(
                roslynOption As IOption2,
                title As String,
                order As Integer,
                defaultValue As Boolean,
                alternativeDefault As Boolean,
                featureFlagOption As IOption2,
                enableWhenOptionAndValue As ([option] As String, value As Object),
                languageName As String) As UnifiedSettingsOption(Of Boolean)
            Dim type = roslynOption.Type
            Assert.True(type = GetType(Boolean) OrElse Nullable.GetUnderlyingType(type) = GetType(Boolean))
            Assert.NotEqual(defaultValue, alternativeDefault)

            Return New UnifiedSettingsOption(Of Boolean)(
                title,
                "boolean",
                order,
                $"config:{enableWhenOptionAndValue.option}=='{enableWhenOptionAndValue.value}'",
                New Migration(New Pass(Input.CreateInput(roslynOption))),
                defaultValue,
                AlternateDefault(Of Boolean).CreateFromOption(roslynOption, alternativeDefault))
        End Function
    End Class
End Namespace
