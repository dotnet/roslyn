' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Newtonsoft.Json

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels

    Friend Class UnifiedSettingsOption(Of T)
        Inherits UnifiedSettingsOptionBase

        <JsonProperty(NameOf([Default]))>
        Public ReadOnly Property [Default] As T

        <JsonProperty(NameOf(AlternateDefault))>
        Public ReadOnly Property AlternateDefault As AlternateDefault(Of T)

        Public Sub New(title As String, type As String, order As Integer, enableWhen As String, migration As Migration, [default] As T, alternateDefault As AlternateDefault(Of T))
            MyBase.New(title, type, order, enableWhen, migration)
            Me.Default = [default]
            Me.AlternateDefault = alternateDefault
        End Sub
    End Class
End Namespace
