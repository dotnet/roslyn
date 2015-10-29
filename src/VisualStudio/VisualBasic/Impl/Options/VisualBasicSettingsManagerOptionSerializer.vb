' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Reflection
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    <ExportLanguageSpecificOptionSerializer(
        LanguageNames.VisualBasic,
        SimplificationOptions.PerLanguageFeatureName,
        ExtractMethodOptions.FeatureName,
        FeatureOnOffOptions.OptionName,
        ServiceFeatureOnOffOptions.OptionName,
        FormattingOptions.InternalTabFeatureName,
        VisualStudioNavigationOptions.FeatureName), [Shared]>
    Friend NotInheritable Class VisualBasicSettingsManagerOptionSerializer
        Inherits AbstractSettingsManagerOptionSerializer

        <ImportingConstructor>
        Public Sub New(serviceProvider As SVsServiceProvider, importedOptionService As IOptionService)
            MyBase.New(serviceProvider, importedOptionService)
        End Sub

        Protected Overrides Function CreateStorageKeyToOptionMap() As ImmutableDictionary(Of String, IOption)
            Dim Result As ImmutableDictionary(Of String, IOption).Builder = ImmutableDictionary.Create(Of String, IOption)(StringComparer.OrdinalIgnoreCase).ToBuilder()

            Result.AddRange(New KeyValuePair(Of String, IOption)() {
                            New KeyValuePair(Of String, IOption)(SettingStorageRoot + "PrettyListing", FeatureOnOffOptions.PrettyListing),
                            New KeyValuePair(Of String, IOption)(SettingStorageRoot + "DisplayLineSeparators", FeatureOnOffOptions.LineSeparator),
                            New KeyValuePair(Of String, IOption)(SettingStorageRoot + "Outlining", FeatureOnOffOptions.Outlining),
                            New KeyValuePair(Of String, IOption)(SettingStorageRoot + "EnableHighlightReferences", FeatureOnOffOptions.ReferenceHighlighting),
                            New KeyValuePair(Of String, IOption)(SettingStorageRoot + "EnableHighlightRelatedKeywords", FeatureOnOffOptions.KeywordHighlighting),
                            New KeyValuePair(Of String, IOption)(SettingStorageRoot + "RenameTrackingPreview", FeatureOnOffOptions.RenameTrackingPreview),
                            New KeyValuePair(Of String, IOption)(SettingStorageRoot + "AutoEndInsert", FeatureOnOffOptions.EndConstruct),
                            New KeyValuePair(Of String, IOption)(SettingStorageRoot + "AutoComment", FeatureOnOffOptions.AutoXmlDocCommentGeneration),
                            New KeyValuePair(Of String, IOption)(SettingStorageRoot + "AutoRequiredMemberInsert", FeatureOnOffOptions.AutomaticInsertionOfAbstractOrInterfaceMembers)})

            Dim Types As Type() = {
                GetType(FormattingOptions),
                GetType(ExtractMethodOptions),
                GetType(SimplificationOptions),
                GetType(ServiceFeatureOnOffOptions),
                GetType(VisualStudioNavigationOptions)}

            Dim Flags As BindingFlags = BindingFlags.Public Or BindingFlags.Static
            Result.AddRange(AbstractSettingsManagerOptionSerializer.GetOptionInfoFromTypeFields(Types, Flags, AddressOf GetOptionInfo))

            Return Result.ToImmutable()
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected Overrides ReadOnly Property SettingStorageRoot As String
            Get
                Return "TextEditor.VisualBasic.Specific."
            End Get
        End Property

        Protected Overrides Function SupportsOption([option] As IOption, languageName As String) As Boolean
            If languageName = LanguageNames.VisualBasic Then
                If [option].Feature = FeatureOnOffOptions.OptionName Then
                    Return [option].Name = FeatureOnOffOptions.PrettyListing.Name Or
                           [option].Name = FeatureOnOffOptions.LineSeparator.Name Or
                           [option].Name = FeatureOnOffOptions.Outlining.Name Or
                           [option].Name = FeatureOnOffOptions.ReferenceHighlighting.Name Or
                           [option].Name = FeatureOnOffOptions.KeywordHighlighting.Name Or
                           [option].Name = FeatureOnOffOptions.RenameTrackingPreview.Name Or
                           [option].Name = FeatureOnOffOptions.EndConstruct.Name Or
                           [option].Name = FeatureOnOffOptions.AutoXmlDocCommentGeneration.Name Or
                           [option].Name = FeatureOnOffOptions.AutomaticInsertionOfAbstractOrInterfaceMembers.Name
                End If

                Return [option].Feature = FormattingOptions.InternalTabFeatureName Or
                       [option].Feature = ExtractMethodOptions.FeatureName Or
                       [option].Feature = SimplificationOptions.PerLanguageFeatureName Or
                       [option].Feature = ServiceFeatureOnOffOptions.OptionName Or
                       [option].Feature = VisualStudioNavigationOptions.FeatureName
            End If

            Return False
        End Function

        Protected Overrides Function GetStorageKeyForOption(key As IOption) As String
            If key.Feature = FeatureOnOffOptions.OptionName Then
                Select Case key.Name
                    Case FeatureOnOffOptions.PrettyListing.Name
                        Return SettingStorageRoot + "PrettyListing"
                    Case FeatureOnOffOptions.LineSeparator.Name
                        Return SettingStorageRoot + "DisplayLineSeparators"
                    Case FeatureOnOffOptions.Outlining.Name
                        Return SettingStorageRoot + "Outlining"
                    Case FeatureOnOffOptions.ReferenceHighlighting.Name
                        Return SettingStorageRoot + "EnableHighlightReferences"
                    Case FeatureOnOffOptions.KeywordHighlighting.Name
                        Return SettingStorageRoot + "EnableHighlightRelatedKeywords"
                    Case FeatureOnOffOptions.RenameTrackingPreview.Name
                        Return SettingStorageRoot + "RenameTrackingPreview"
                    Case FeatureOnOffOptions.EndConstruct.Name
                        Return SettingStorageRoot + "AutoEndInsert"
                    Case FeatureOnOffOptions.AutoXmlDocCommentGeneration.Name
                        Return SettingStorageRoot + "AutoComment"
                    Case FeatureOnOffOptions.AutomaticInsertionOfAbstractOrInterfaceMembers.Name
                        Return SettingStorageRoot + "AutoRequiredMemberInsert"
                    Case FeatureOnOffOptions.FormatOnPaste.Name
                        Return Nothing
                End Select
            End If

            Return MyBase.GetStorageKeyForOption(key)
        End Function
    End Class
End Namespace