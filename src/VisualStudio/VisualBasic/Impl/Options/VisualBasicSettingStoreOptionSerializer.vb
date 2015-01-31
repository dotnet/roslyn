' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Options.Providers
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Shared.Options
Imports System.Composition

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    <ExportLanguageSpecificOptionSerializer(
        LanguageNames.VisualBasic,
        SimplificationOptions.PerLanguageFeatureName,
        ExtractMethodOptions.FeatureName,
        FeatureOnOffOptions.OptionName,
        ServiceFeatureOnOffOptions.OptionName,
        FormattingOptions.InternalTabFeatureName), [Shared]>
    Friend NotInheritable Class VisualBasicSettingStoreOptionSerializer
        Inherits AbstractSettingStoreOptionSerializer

        Private Const VisualBasicRoot As String = "VB Editor"

        <ImportingConstructor>
        Public Sub New(serviceProvider As SVsServiceProvider)
            MyBase.New(serviceProvider)
        End Sub

        Protected Overrides Function GetCollectionPathAndPropertyNameForOption(key As IOption, languageName As String) As Tuple(Of String, String)
            If key.Feature = FeatureOnOffOptions.OptionName AndAlso languageName = LanguageNames.VisualBasic Then
                Select Case key.Name
                    Case FeatureOnOffOptions.PrettyListing.Name
                        Return Tuple.Create(VisualBasicRoot, "PrettyListing")
                    Case FeatureOnOffOptions.LineSeparator.Name
                        Return Tuple.Create(VisualBasicRoot, "DisplayLineSeparators")
                    Case FeatureOnOffOptions.Outlining.Name
                        Return Tuple.Create(VisualBasicRoot, "Outlining")
                    Case FeatureOnOffOptions.ReferenceHighlighting.Name
                        Return Tuple.Create(VisualBasicRoot, "EnableHighlightReferences")
                    Case FeatureOnOffOptions.KeywordHighlighting.Name
                        Return Tuple.Create(VisualBasicRoot, "EnableHighlightRelatedKeywords")
                    Case FeatureOnOffOptions.EndConstruct.Name
                        Return Tuple.Create(VisualBasicRoot, "AutoEndInsert")
                    Case FeatureOnOffOptions.AutoXmlDocCommentGeneration.Name
                        Return Tuple.Create(VisualBasicRoot, "AutoComment")
                    Case FeatureOnOffOptions.AutomaticInsertionOfAbstractOrInterfaceMembers.Name
                        Return Tuple.Create(VisualBasicRoot, "AutoRequiredMemberInsert")
                    Case FeatureOnOffOptions.FormatOnPaste.Name
                        Return Nothing
                End Select
            ElseIf key.Feature = FormattingOptions.InternalTabFeatureName AndAlso languageName = LanguageNames.VisualBasic Then
                Return Tuple.Create("Roslyn\Internal\Formatting", key.Name)
            ElseIf key.Feature = ExtractMethodOptions.FeatureName AndAlso languageName = LanguageNames.VisualBasic Then
                Return Tuple.Create(String.Format("{0}\ExtractMethod", VisualBasicRoot), key.Name)
            ElseIf key.Feature = SimplificationOptions.PerLanguageFeatureName AndAlso languageName = LanguageNames.VisualBasic Then
                Return Tuple.Create(String.Format("{0}\Simplification", VisualBasicRoot), key.Name)
            ElseIf key.Feature = ServiceFeatureOnOffOptions.OptionName AndAlso languageName = LanguageNames.VisualBasic Then
                Return Tuple.Create(String.Format("{0}\Diagnostics", VisualBasicRoot), key.Name)
            End If

            Return Nothing
        End Function
    End Class
End Namespace
