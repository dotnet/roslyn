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

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim [option] = TryCast(obj, UnifiedSettingsOption(Of T))
            Return [option] IsNot Nothing AndAlso
                   Title = [option].Title AndAlso
                   Type = [option].Type AndAlso
                   Order = [option].Order AndAlso
                   EnableWhen = [option].EnableWhen AndAlso
                   EqualityComparer(Of Migration).Default.Equals(Migration, [option].Migration) AndAlso
                   EqualityComparer(Of T).Default.Equals([Default], [option].Default) AndAlso
                   EqualityComparer(Of AlternateDefault(Of T)).Default.Equals(AlternateDefault, [option].AlternateDefault)
        End Function
    End Class
End Namespace
