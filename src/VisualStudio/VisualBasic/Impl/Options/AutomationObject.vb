' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.SymbolSearch

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    <ComVisible(True)>
    Public Class AutomationObject
        Private ReadOnly _workspace As CodeAnalysis.Workspace

        Friend Sub New(workspace As CodeAnalysis.Workspace)
            _workspace = workspace
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

        <Obsolete("This SettingStore option has now been deprecated")>
        Public Property ClosedFileDiagnostics As Boolean
            Get
                Return False
            End Get
            Set(value As Boolean)
            End Set
        End Property

        <Obsolete("This SettingStore option has now been deprecated")>
        Public Property BasicClosedFileDiagnostics As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
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

        Public Property Style_PreferIntrinsicPredefinedTypeKeywordInDeclaration_CodeStyle As String
            Get
                Return GetXmlOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, value)
            End Set
        End Property

        Public Property Style_PreferIntrinsicPredefinedTypeKeywordInMemberAccess_CodeStyle As String
            Get
                Return GetXmlOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, value)
            End Set
        End Property

        Public Property Style_QualifyFieldAccess As String
            Get
                Return GetXmlOption(CodeStyleOptions.QualifyFieldAccess)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions.QualifyFieldAccess, value)
            End Set
        End Property

        Public Property Style_QualifyPropertyAccess As String
            Get
                Return GetXmlOption(CodeStyleOptions.QualifyPropertyAccess)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions.QualifyPropertyAccess, value)
            End Set
        End Property

        Public Property Style_QualifyMethodAccess As String
            Get
                Return GetXmlOption(CodeStyleOptions.QualifyMethodAccess)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions.QualifyMethodAccess, value)
            End Set
        End Property

        Public Property Style_QualifyEventAccess As String
            Get
                Return GetXmlOption(CodeStyleOptions.QualifyEventAccess)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions.QualifyEventAccess, value)
            End Set
        End Property

        Public Property Style_PreferObjectInitializer As String
            Get
                Return GetXmlOption(CodeStyleOptions.PreferObjectInitializer)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions.PreferObjectInitializer, value)
            End Set
        End Property

        Public Property Style_PreferCollectionInitializer As String
            Get
                Return GetXmlOption(CodeStyleOptions.PreferCollectionInitializer)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions.PreferCollectionInitializer, value)
            End Set
        End Property

        Public Property Style_PreferCoalesceExpression As String
            Get
                Return GetXmlOption(CodeStyleOptions.PreferCoalesceExpression)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions.PreferCoalesceExpression, value)
            End Set
        End Property

        Public Property Style_PreferNullPropagation As String
            Get
                Return GetXmlOption(CodeStyleOptions.PreferNullPropagation)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions.PreferNullPropagation, value)
            End Set
        End Property

        Public Property Style_PreferInferredTupleNames As String
            Get
                Return GetXmlOption(CodeStyleOptions.PreferInferredTupleNames)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions.PreferInferredTupleNames, value)
            End Set
        End Property

        Public Property Style_PreferInferredAnonymousTypeMemberNames As String
            Get
                Return GetXmlOption(CodeStyleOptions.PreferInferredAnonymousTypeMemberNames)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions.PreferInferredAnonymousTypeMemberNames, value)
            End Set
        End Property

        Public Property Style_PreferExplicitTupleNames As String
            Get
                Return GetXmlOption(CodeStyleOptions.PreferExplicitTupleNames)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions.PreferExplicitTupleNames, value)
            End Set
        End Property

        Public Property Style_PreferReadonly As String
            Get
                Return GetXmlOption(CodeStyleOptions.PreferReadonly)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions.PreferReadonly, value)
            End Set
        End Property

        Public Property Option_PlaceSystemNamespaceFirst As Boolean
            Get
                Return GetBooleanOption(GenerationOptions.PlaceSystemNamespaceFirst)
            End Get
            Set(value As Boolean)
                SetBooleanOption(GenerationOptions.PlaceSystemNamespaceFirst, value)
            End Set
        End Property

        Public Property Option_SuggestImportsForTypesInReferenceAssemblies As Boolean
            Get
                Return GetBooleanOption(SymbolSearchOptions.SuggestForTypesInReferenceAssemblies)
            End Get
            Set(value As Boolean)
                SetBooleanOption(SymbolSearchOptions.SuggestForTypesInReferenceAssemblies, value)
            End Set
        End Property

        Public Property Option_SuggestImportsForTypesInNuGetPackages As Boolean
            Get
                Return GetBooleanOption(SymbolSearchOptions.SuggestForTypesInNuGetPackages)
            End Get
            Set(value As Boolean)
                SetBooleanOption(SymbolSearchOptions.SuggestForTypesInNuGetPackages, value)
            End Set
        End Property

        Public Property Option_ShowItemsFromUnimportedNamespaces As Integer
            Get
                Return GetBooleanOption(CompletionOptions.ShowItemsFromUnimportedNamespaces)
            End Get
            Set(value As Integer)
                SetBooleanOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, value)
            End Set
        End Property

        Private Function GetBooleanOption(key As [PerLanguageOption](Of Boolean)) As Boolean
            Return _workspace.Options.GetOption(key, LanguageNames.VisualBasic)
        End Function

        Private Function GetXmlOption(key As PerLanguageOption(Of CodeStyleOption(Of Boolean))) As String
            Return _workspace.Options.GetOption(key, LanguageNames.VisualBasic).ToXElement().ToString()
        End Function

        Private Sub SetBooleanOption(key As [PerLanguageOption](Of Boolean), value As Boolean)
            _workspace.Options = _workspace.Options.WithChangedOption(key, LanguageNames.VisualBasic, value)
        End Sub

        Private Function GetBooleanOption(key As PerLanguageOption(Of Boolean?)) As Integer
            Dim [option] = _workspace.Options.GetOption(key, LanguageNames.VisualBasic)
            If Not [option].HasValue Then
                Return -1
            End If

            Return If([option].Value, 1, 0)
        End Function

        Private Sub SetBooleanOption(key As PerLanguageOption(Of Boolean?), value As Integer)
            Dim boolValue As Boolean? = If(value < 0, Nothing, value > 0)
            _workspace.Options = _workspace.Options.WithChangedOption(key, LanguageNames.VisualBasic, boolValue)
        End Sub

        Private Sub SetXmlOption(key As PerLanguageOption(Of CodeStyleOption(Of Boolean)), value As String)
            Dim convertedValue = CodeStyleOption(Of Boolean).FromXElement(XElement.Parse(value))
            _workspace.Options = _workspace.Options.WithChangedOption(key, LanguageNames.VisualBasic, convertedValue)
        End Sub

    End Class
End Namespace
