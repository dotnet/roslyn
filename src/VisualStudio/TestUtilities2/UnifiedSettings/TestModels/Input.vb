' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices.Options
Imports Microsoft.VisualStudio.LanguageServices.Options.VisualStudioOptionStorage
Imports Newtonsoft.Json
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
    Friend Class Input
        <JsonProperty("store")>
        Public ReadOnly Property Store As String
        <JsonProperty("path")>
        Public ReadOnly Property Path As String

        Public Sub New(store As String, path As String)
            Me.Store = store
            Me.Path = path
        End Sub

        Public Shared Function CreateInput([option] As IOption2) As Input
            Dim configName = [option].Definition.ConfigName
            Dim visualStudioStorage = Storages(configName)

            Return New Input(GetStore(visualStudioStorage), GetPath(visualStudioStorage))
        End Function

        Public Shared Function CreateInput([option] As IPerLanguageValuedOption, languageName As String) As Input
            Dim configName = [option].Definition.ConfigName
            Dim visualStudioStorage = Storages(configName)
            Return New Input(GetStore(visualStudioStorage), GetPath(visualStudioStorage, languageName))
        End Function

        Private Shared Function GetStore(storage As VisualStudioOptionStorage) As String
            If TypeOf storage Is RoamingProfileStorage Then
                Return "SettingsManager"
            ElseIf TypeOf storage Is LocalUserProfileStorage Then
                Return "VsUserSettingsRegistry"
            Else
                Throw ExceptionUtilities.Unreachable
            End If
        End Function

        Private Shared Function GetPath(storage As VisualStudioOptionStorage, Optional languageName As String = Nothing) As String
            If TypeOf storage Is RoamingProfileStorage Then
                Dim roamingProfileStorage = DirectCast(storage, RoamingProfileStorage)
                Return roamingProfileStorage.Key.Replace("%LANGUAGE%", GetSubstituteLanguage(If(languageName = Nothing, String.Empty, languageName)))
            ElseIf TypeOf storage Is LocalUserProfileStorage Then
                Dim localUserProfileStorage = DirectCast(storage, LocalUserProfileStorage)
                Return $"{localUserProfileStorage.Path}\\{localUserProfileStorage.Key}"
            Else
                Throw ExceptionUtilities.Unreachable
            End If
        End Function

        Private Shared Function GetSubstituteLanguage(languageName As String) As String
            Select Case languageName
                Case LanguageNames.CSharp
                    Return "CSharp"
                Case LanguageNames.VisualBasic
                    Return "VisualBasic"
                Case Else
                    Return languageName
            End Select
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim input = TryCast(obj, Input)
            Return input IsNot Nothing AndAlso
                   Store = input.Store AndAlso
                   Path = input.Path
        End Function
    End Class
End Namespace
