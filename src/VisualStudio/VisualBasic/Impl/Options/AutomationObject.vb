' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Options
Imports Microsoft.CodeAnalysis.Simplification

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    <ComVisible(True)>
    Public Class AutomationObject
        Private ReadOnly _optionService As IOptionService

        Friend Sub New(optionService As IOptionService)
            _optionService = optionService
        End Sub

        Public Property AutoComment As Boolean
            Get
                Return GetBooleanOption(FeatureOnOffOptions.AutoXmlDocCommentGeneration)
            End Get
            Set(value As Boolean)
                SetBooleanOption(FeatureOnOffOptions.AutoXmlDocCommentGeneration, value)
            End Set
        End Property

        Public Property AutoEndInsert As Boolean
            Get
                Return GetBooleanOption(FeatureOnOffOptions.EndConstruct)
            End Get
            Set(value As Boolean)
                SetBooleanOption(FeatureOnOffOptions.EndConstruct, value)
            End Set
        End Property

        Public Property AutoRequiredMemberInsert As Boolean
            Get
                Return GetBooleanOption(FeatureOnOffOptions.AutomaticInsertionOfAbstractOrInterfaceMembers)
            End Get
            Set(value As Boolean)
                SetBooleanOption(FeatureOnOffOptions.AutomaticInsertionOfAbstractOrInterfaceMembers, value)
            End Set
        End Property

        Public Property ClosedFileDiagnostics As Boolean
            Get
                Return GetBooleanOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic)
            End Get
            Set(value As Boolean)
                SetBooleanOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, value)
            End Set
        End Property

        Public Property RenameTrackingPreview As Boolean
            Get
                Return GetBooleanOption(FeatureOnOffOptions.RenameTrackingPreview)
            End Get
            Set(value As Boolean)
                SetBooleanOption(FeatureOnOffOptions.RenameTrackingPreview, value)
            End Set
        End Property

        Public Property DisplayLineSeparators As Boolean
            Get
                Return GetBooleanOption(FeatureOnOffOptions.LineSeparator)
            End Get
            Set(value As Boolean)
                SetBooleanOption(FeatureOnOffOptions.LineSeparator, value)
            End Set
        End Property

        Public Property EnableHighlightReferences As Boolean
            Get
                Return GetBooleanOption(FeatureOnOffOptions.ReferenceHighlighting)
            End Get
            Set(value As Boolean)
                SetBooleanOption(FeatureOnOffOptions.ReferenceHighlighting, value)
            End Set
        End Property

        Public Property EnableHighlightRelatedKeywords As Boolean
            Get
                Return GetBooleanOption(FeatureOnOffOptions.KeywordHighlighting)
            End Get
            Set(value As Boolean)
                SetBooleanOption(FeatureOnOffOptions.KeywordHighlighting, value)
            End Set
        End Property

        Public Property ExtractMethod_DoNotPutOutOrRefOnStruct As Boolean
            Get
                Return GetBooleanOption(ExtractMethodOptions.DontPutOutOrRefOnStruct)
            End Get
            Set(value As Boolean)
                SetBooleanOption(ExtractMethodOptions.DontPutOutOrRefOnStruct, value)
            End Set
        End Property

        Public Property ExtractMethod_AllowMovingDeclaration As Boolean
            Get
                Return GetBooleanOption(ExtractMethodOptions.AllowMovingDeclaration)
            End Get
            Set(value As Boolean)
                SetBooleanOption(ExtractMethodOptions.AllowMovingDeclaration, value)
            End Set
        End Property

        Public Property Outlining As Boolean
            Get
                Return GetBooleanOption(FeatureOnOffOptions.Outlining)
            End Get
            Set(value As Boolean)
                SetBooleanOption(FeatureOnOffOptions.Outlining, value)
            End Set
        End Property

        Public Property PrettyListing As Boolean
            Get
                Return GetBooleanOption(FeatureOnOffOptions.PrettyListing)
            End Get
            Set(value As Boolean)
                SetBooleanOption(FeatureOnOffOptions.PrettyListing, value)
            End Set
        End Property

        Public Property Style_PreferIntrinsicPredefinedTypeKeywordInDeclaration As Boolean
            Get
                Return GetBooleanOption(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration)
            End Get
            Set(value As Boolean)
                SetBooleanOption(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, value)
            End Set
        End Property

        Public Property Style_PreferIntrinsicPredefinedTypeKeywordInMemberAccess As Boolean
            Get
                Return GetBooleanOption(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess)
            End Get
            Set(value As Boolean)
                SetBooleanOption(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, value)
            End Set
        End Property

        Public Property Style_QualifyFieldAccess As Boolean
            Get
                Return GetBooleanOption(SimplificationOptions.QualifyFieldAccess)
            End Get
            Set(value As Boolean)
                SetBooleanOption(SimplificationOptions.QualifyFieldAccess, value)
            End Set
        End Property

        Public Property Style_QualifyPropertyAccess As Boolean
            Get
                Return GetBooleanOption(SimplificationOptions.QualifyPropertyAccess)
            End Get
            Set(value As Boolean)
                SetBooleanOption(SimplificationOptions.QualifyPropertyAccess, value)
            End Set
        End Property

        Public Property Style_QualifyMethodAccess As Boolean
            Get
                Return GetBooleanOption(SimplificationOptions.QualifyMethodAccess)
            End Get
            Set(value As Boolean)
                SetBooleanOption(SimplificationOptions.QualifyMethodAccess, value)
            End Set
        End Property

        Public Property Style_QualifyEventAccess As Boolean
            Get
                Return GetBooleanOption(SimplificationOptions.QualifyEventAccess)
            End Get
            Set(value As Boolean)
                SetBooleanOption(SimplificationOptions.QualifyEventAccess, value)
            End Set
        End Property

        Private Function GetBooleanOption(key As [Option](Of Boolean)) As Boolean
            Return _optionService.GetOption(key)
        End Function

        Private Sub SetBooleanOption(key As [Option](Of Boolean), value As Boolean)
            Dim optionSet = _optionService.GetOptions()
            optionSet = optionSet.WithChangedOption(key, value)
            _optionService.SetOptions(optionSet)
        End Sub

        Private Function GetBooleanOption(key As [PerLanguageOption](Of Boolean)) As Boolean
            Return _optionService.GetOption(key, LanguageNames.VisualBasic)
        End Function

        Private Sub SetBooleanOption(key As [PerLanguageOption](Of Boolean), value As Boolean)
            Dim optionSet = _optionService.GetOptions()
            optionSet = optionSet.WithChangedOption(key, LanguageNames.VisualBasic, value)
            _optionService.SetOptions(optionSet)
        End Sub
    End Class
End Namespace
