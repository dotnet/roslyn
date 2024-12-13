' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings
    Friend Class ExpectedUnifiedSetting
        Public ReadOnly Property UnifiedSettingPath As String

        Public ReadOnly Property [Option] As IOption2

        Public ReadOnly Property UnifiedSettingsOption As UnifiedSettingsOptionBase

        Public Sub New(unifiedSettingPath As String, [option] As IOption2, unifiedSettingsOption As UnifiedSettingsOptionBase)
            Me.UnifiedSettingPath = unifiedSettingPath
            Me.Option = [option]
            Me.UnifiedSettingsOption = unifiedSettingsOption
        End Sub
    End Class
End Namespace
