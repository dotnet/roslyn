' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices.Options.VisualStudioOptionStorage
Imports Newtonsoft.Json

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
    Friend Class AlternateDefault(Of T)
        <JsonProperty(NameOf(FlagName))>
        Public Property FlagName As String

        <JsonProperty("default")>
        Public Property [Default] As T

        Public Shared Function CreateFromOption([option] As IOption2, alternativeDefault As T) As AlternateDefault(Of T)
            Dim configName = [option].Definition.ConfigName
            Dim visualStudioStorage = Storages(configName)
            ' Option has to be FeatureFlagStorage to be used as alternative default
            Dim featureFlagStorage = DirectCast(visualStudioStorage, FeatureFlagStorage)
            Return New AlternateDefault(Of T) With {.FlagName = featureFlagStorage.FlagName, .[Default] = alternativeDefault}
        End Function
    End Class
End Namespace
