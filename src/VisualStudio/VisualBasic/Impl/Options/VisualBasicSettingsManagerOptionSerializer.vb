' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Reflection
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    <ExportLanguageSpecificOptionSerializer(
        LanguageNames.VisualBasic,
        AddImportOptions.FeatureName,
        CodeStyleOptions.PerLanguageCodeStyleOption,
        CompletionOptions.FeatureName,
        ExtractMethodOptions.FeatureName,
        FeatureOnOffOptions.OptionName,
        FormattingOptions.InternalTabFeatureName,
        ServiceFeatureOnOffOptions.OptionName,
        SimplificationOptions.PerLanguageFeatureName,
        VisualStudioNavigationOptions.FeatureName), [Shared]>
    Friend NotInheritable Class VisualBasicSettingsManagerOptionSerializer
        Inherits AbstractSettingsManagerOptionSerializer

        <ImportingConstructor>
        Public Sub New(workspace As VisualStudioWorkspaceImpl)
            MyBase.New(workspace)
        End Sub

        Private Const Style_QualifyFieldAccess As String = NameOf(AutomationObject.Style_QualifyFieldAccess)
        Private Const Style_QualifyPropertyAccess As String = NameOf(AutomationObject.Style_QualifyPropertyAccess)
        Private Const Style_QualifyMethodAccess As String = NameOf(AutomationObject.Style_QualifyMethodAccess)
        Private Const Style_QualifyEventAccess As String = NameOf(AutomationObject.Style_QualifyEventAccess)

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
                GetType(AddImportOptions),
                GetType(CompletionOptions),
                GetType(FormattingOptions),
                GetType(ExtractMethodOptions),
                GetType(SimplificationOptions),
                GetType(ServiceFeatureOnOffOptions),
                GetType(VisualStudioNavigationOptions),
                GetType(CodeStyleOptions)}

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
            If [option].Name = CompletionOptions.EnterKeyBehavior.Name OrElse
                [option].Name = CompletionOptions.TriggerOnTypingLetters.Name OrElse
                [option].Name = CompletionOptions.TriggerOnDeletion.Name Then
                Return True
            ElseIf [option].Name = CompletionOptions.SnippetsBehavior.Name Then
                Return True
            ElseIf languageName = LanguageNames.VisualBasic Then
                If [option].Feature = FeatureOnOffOptions.OptionName Then
                    Return [option].Name = FeatureOnOffOptions.PrettyListing.Name OrElse
                           [option].Name = FeatureOnOffOptions.LineSeparator.Name OrElse
                           [option].Name = FeatureOnOffOptions.Outlining.Name OrElse
                           [option].Name = FeatureOnOffOptions.ReferenceHighlighting.Name OrElse
                           [option].Name = FeatureOnOffOptions.KeywordHighlighting.Name OrElse
                           [option].Name = FeatureOnOffOptions.RenameTrackingPreview.Name OrElse
                           [option].Name = FeatureOnOffOptions.EndConstruct.Name OrElse
                           [option].Name = FeatureOnOffOptions.AutoXmlDocCommentGeneration.Name OrElse
                           [option].Name = FeatureOnOffOptions.AutomaticInsertionOfAbstractOrInterfaceMembers.Name
                End If

                Return [option].Feature = FormattingOptions.InternalTabFeatureName OrElse
                       [option].Feature = AddImportOptions.FeatureName OrElse
                       [option].Feature = CodeStyleOptions.PerLanguageCodeStyleOption OrElse
                       [option].Feature = CompletionOptions.FeatureName OrElse
                       [option].Feature = ExtractMethodOptions.FeatureName OrElse
                       [option].Feature = SimplificationOptions.PerLanguageFeatureName OrElse
                       [option].Feature = ServiceFeatureOnOffOptions.OptionName OrElse
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

        Public Overrides Function TryFetch(optionKey As OptionKey, ByRef value As Object) As Boolean
            If Me.Manager Is Nothing Then
                Debug.Fail("Manager is unexpectedly Nothing")
                Return False
            End If

            ' code style use Me.
            If optionKey.Option Is CodeStyleOptions.QualifyFieldAccess Then
                Return FetchStyleBool(Style_QualifyFieldAccess, value)
            ElseIf optionKey.Option Is CodeStyleOptions.QualifyPropertyAccess Then
                Return FetchStyleBool(Style_QualifyPropertyAccess, value)
            ElseIf optionKey.Option Is CodeStyleOptions.QualifyMethodAccess Then
                Return FetchStyleBool(Style_QualifyMethodAccess, value)
            ElseIf optionKey.Option Is CodeStyleOptions.QualifyEventAccess Then
                Return FetchStyleBool(Style_QualifyEventAccess, value)
            End If

            If optionKey.Option Is CompletionOptions.EnterKeyBehavior Then
                Return FetchEnterKeyBehavior(optionKey, value)
            End If

            If optionKey.Option Is CompletionOptions.SnippetsBehavior Then
                Return FetchSnippetsBehavior(optionKey, value)
            End If

            If optionKey.Option Is CompletionOptions.TriggerOnDeletion Then
                Return FetchTriggerOnDeletion(optionKey, value)
            End If

            Return MyBase.TryFetch(optionKey, value)
        End Function

        Private Function FetchTriggerOnDeletion(optionKey As OptionKey, ByRef value As Object) As Boolean
            If MyBase.TryFetch(optionKey, value) Then
                If value Is Nothing Then
                    ' The default behavior for VB is to trigger completion on deletion.
                    value = CType(True, Boolean?)
                End If

                Return True
            End If

            Return False
        End Function

        Private Function FetchEnterKeyBehavior(optionKey As OptionKey, ByRef value As Object) As Boolean
            If MyBase.TryFetch(optionKey, value) Then
                If value.Equals(EnterKeyRule.Default) Then
                    value = EnterKeyRule.Always
                End If

                Return True
            End If

            Return False
        End Function

        Private Function FetchSnippetsBehavior(optionKey As OptionKey, ByRef value As Object) As Boolean
            If MyBase.TryFetch(optionKey, value) Then
                If value.Equals(SnippetsRule.Default) Then
                    value = SnippetsRule.IncludeAfterTypingIdentifierQuestionTab
                End If

                Return True
            End If

            Return False
        End Function

        Public Overrides Function TryPersist(optionKey As OptionKey, value As Object) As Boolean
            If Me.Manager Is Nothing Then
                Debug.Fail("Manager is unexpectedly Nothing")
                Return False
            End If

            ' code style use Me.
            If optionKey.Option Is CodeStyleOptions.QualifyFieldAccess Then
                Return PersistStyleOption(Of Boolean)(Style_QualifyFieldAccess, value)
            ElseIf optionKey.Option Is CodeStyleOptions.QualifyPropertyAccess Then
                Return PersistStyleOption(Of Boolean)(Style_QualifyPropertyAccess, value)
            ElseIf optionKey.Option Is CodeStyleOptions.QualifyMethodAccess Then
                Return PersistStyleOption(Of Boolean)(Style_QualifyMethodAccess, value)
            ElseIf optionKey.Option Is CodeStyleOptions.QualifyEventAccess Then
                Return PersistStyleOption(Of Boolean)(Style_QualifyEventAccess, value)
            End If

            Return MyBase.TryPersist(optionKey, value)
        End Function

        Private Function FetchStyleBool(settingName As String, ByRef value As Object) As Boolean
            Dim typeStyleValue = Manager.GetValueOrDefault(Of String)(settingName)
            Return FetchStyleOption(Of Boolean)(typeStyleValue, value)
        End Function

        Private Shared Function FetchStyleOption(Of T)(typeStyleOptionValue As String, ByRef value As Object) As Boolean
            If String.IsNullOrEmpty(typeStyleOptionValue) Then
                value = CodeStyleOption(Of T).Default
            Else
                value = CodeStyleOption(Of T).FromXElement(XElement.Parse(typeStyleOptionValue))
            End If

            Return True
        End Function

        Private Function PersistStyleOption(Of T)([option] As String, value As Object) As Boolean
            Dim serializedValue = CType(value, CodeStyleOption(Of T)).ToXElement().ToString()
            Me.Manager.SetValueAsync([option], value:=serializedValue, isMachineLocal:=False)
            Return True
        End Function

    End Class
End Namespace
