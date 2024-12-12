' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Options
Imports Newtonsoft.Json

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
    Friend Class UnifiedSettingsEnumOption
        Inherits UnifiedSettingsOption(Of String)
        <JsonProperty(NameOf([Enum]))>
        Public Property [Enum] As String()

        <JsonProperty(NameOf(EnumLabels))>
        Public Property EnumLabels As String()

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
