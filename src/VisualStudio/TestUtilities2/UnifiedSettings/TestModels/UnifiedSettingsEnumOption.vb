' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Options
Imports Newtonsoft.Json

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
    Friend Class UnifiedSettingsEnumOption
        Inherits UnifiedSettingsOption(Of String)

        <JsonProperty(NameOf([Enum]))>
        Public ReadOnly Property [Enum] As String()

        <JsonProperty(NameOf(EnumLabels))>
        <JsonConverter(GetType(ResourceConverter))>
        Public ReadOnly Property EnumLabels As String()

        Public Sub New(title As String, type As String, order As Integer, enableWhen As String, migration As Migration, [default] As String, alternateDefault As AlternateDefault(Of String), [enum] As String(), enumLabels As String())
            MyBase.New(title, type, order, enableWhen, migration, [default], alternateDefault)
            Me.Enum = [enum]
            Me.EnumLabels = enumLabels
        End Sub

        Public Shared Function CreateEnumOption(
                roslynOption As IOption2,
                title As String,
                defaultValue As Boolean,
                featureFlagOption As IOption2,
                enumLabels As String(),
                [enum] As String(),
                languageName As String) As UnifiedSettingsEnumOption

        End Function
    End Class
End Namespace
