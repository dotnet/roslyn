' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices.Options.VisualStudioOptionStorage
Imports Newtonsoft.Json

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
    Friend Class AlternateDefault(Of T)
        <JsonProperty(NameOf(FlagName))>
        Public ReadOnly Property FlagName As String

        <JsonProperty("default")>
        Public ReadOnly Property [Default] As T

        Public Sub New(flagName As String, [default] As T)
            Me.FlagName = flagName
            Me.Default = [default]
        End Sub

        Public Shared Function CreateFromOption([option] As IOption2, alternativeDefault As T) As AlternateDefault(Of T)
            Dim configName = [option].Definition.ConfigName
            Dim visualStudioStorage = Storages(configName)
            ' Option has to be FeatureFlagStorage to be used as alternative default
            Dim featureFlagStorage = DirectCast(visualStudioStorage, FeatureFlagStorage)
            Return New AlternateDefault(Of T)(featureFlagStorage.FlagName, alternativeDefault)
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim [default] = TryCast(obj, AlternateDefault(Of T))
            Return [default] IsNot Nothing AndAlso
                   FlagName = [default].FlagName AndAlso
                   EqualityComparer(Of T).Default.Equals(Me.Default, [default].Default)
        End Function
    End Class
End Namespace
