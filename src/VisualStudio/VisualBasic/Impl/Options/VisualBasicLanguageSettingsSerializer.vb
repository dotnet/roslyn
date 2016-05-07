' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.Options
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Microsoft.CodeAnalysis.Options
Imports System.Composition
Imports Microsoft.CodeAnalysis.Shared.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    <ExportLanguageSpecificOptionSerializer(
        LanguageNames.VisualBasic,
        FormattingOptions.TabFeatureName,
        BraceCompletionOptions.FeatureName,
        CompletionOptions.FeatureName,
        SignatureHelpOptions.FeatureName,
        NavigationBarOptions.FeatureName), [Shared]>
    Friend NotInheritable Class VisualBasicLanguageSettingsSerializer
        Inherits AbstractLanguageSettingsSerializer

        <ImportingConstructor>
        Public Sub New(serviceProvider As SVsServiceProvider)
            MyBase.New(Guids.VisualBasicLanguageServiceId, LanguageNames.VisualBasic, serviceProvider)
        End Sub

        Public Overrides Function TryFetch(optionKey As OptionKey, ByRef value As Object) As Boolean
            If optionKey.Option Is CompletionOptions.HideAdvancedMembers Then
                Return False
            End If

            Return MyBase.TryFetch(optionKey, value)
        End Function
    End Class
End Namespace
