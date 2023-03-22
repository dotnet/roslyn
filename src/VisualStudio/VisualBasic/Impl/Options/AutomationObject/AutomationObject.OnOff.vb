' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.AddImportOnPaste
Imports Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
Imports Microsoft.CodeAnalysis.KeywordHighlighting
Imports Microsoft.CodeAnalysis.LineSeparators
Imports Microsoft.CodeAnalysis.MetadataAsSource
Imports Microsoft.CodeAnalysis.ReferenceHighlighting
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.AutomaticInsertionOfAbstractOrInterfaceMembers

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Public Class AutomationObject
        Public Property AutoEndInsert As Boolean
            Get
                Return GetBooleanOption(EndConstructGenerationOptionsStorage.EndConstruct)
            End Get
            Set(value As Boolean)
                SetBooleanOption(EndConstructGenerationOptionsStorage.EndConstruct, value)
            End Set
        End Property

        Public Property AutoRequiredMemberInsert As Boolean
            Get
                Return GetBooleanOption(AutomaticInsertionOfAbstractOrInterfaceMembersOptionsStorage.AutomaticInsertionOfAbstractOrInterfaceMembers)
            End Get
            Set(value As Boolean)
                SetBooleanOption(AutomaticInsertionOfAbstractOrInterfaceMembersOptionsStorage.AutomaticInsertionOfAbstractOrInterfaceMembers, value)
            End Set
        End Property

        Public Property RenameTrackingPreview As Boolean
            Get
                Return GetBooleanOption(RenameTrackingOptionsStorage.RenameTrackingPreview)
            End Get
            Set(value As Boolean)
                SetBooleanOption(RenameTrackingOptionsStorage.RenameTrackingPreview, value)
            End Set
        End Property

        Public Property DisplayLineSeparators As Boolean
            Get
                Return GetBooleanOption(LineSeparatorsOptionsStorage.LineSeparator)
            End Get
            Set(value As Boolean)
                SetBooleanOption(LineSeparatorsOptionsStorage.LineSeparator, value)
            End Set
        End Property

        Public Property EnableHighlightReferences As Boolean
            Get
                Return GetBooleanOption(ReferenceHighlightingOptionsStorage.ReferenceHighlighting)
            End Get
            Set(value As Boolean)
                SetBooleanOption(ReferenceHighlightingOptionsStorage.ReferenceHighlighting, value)
            End Set
        End Property

        Public Property EnableHighlightRelatedKeywords As Boolean
            Get
                Return GetBooleanOption(KeywordHighlightingOptionsStorage.KeywordHighlighting)
            End Get
            Set(value As Boolean)
                SetBooleanOption(KeywordHighlightingOptionsStorage.KeywordHighlighting, value)
            End Set
        End Property

        Public Property Outlining As Boolean
            Get
                Return GetBooleanOption(OutliningOptionsStorage.Outlining)
            End Get
            Set(value As Boolean)
                SetBooleanOption(OutliningOptionsStorage.Outlining, value)
            End Set
        End Property

        Public Property CollapseImportsWhenFirstOpened As Boolean
            Get
                Return GetBooleanOption(BlockStructureOptionsStorage.CollapseImportsWhenFirstOpened)
            End Get
            Set(value As Boolean)
                SetBooleanOption(BlockStructureOptionsStorage.CollapseImportsWhenFirstOpened, value)
            End Set
        End Property

        Public Property CollapseRegionsWhenFirstOpened As Boolean
            Get
                Return GetBooleanOption(BlockStructureOptionsStorage.CollapseRegionsWhenFirstOpened)
            End Get
            Set(value As Boolean)
                SetBooleanOption(BlockStructureOptionsStorage.CollapseRegionsWhenFirstOpened, value)
            End Set
        End Property

        Public Property CollapseMetadataSignatureFilesWhenFirstOpened As Boolean
            Get
                Return GetBooleanOption(BlockStructureOptionsStorage.CollapseMetadataSignatureFilesWhenFirstOpened)
            End Get
            Set(value As Boolean)
                SetBooleanOption(BlockStructureOptionsStorage.CollapseMetadataSignatureFilesWhenFirstOpened, value)
            End Set
        End Property

        Public Property CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened As Boolean
            Get
                Return GetBooleanOption(BlockStructureOptionsStorage.CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened)
            End Get
            Set(value As Boolean)
                SetBooleanOption(BlockStructureOptionsStorage.CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened, value)
            End Set
        End Property

        Public Property PrettyListing As Boolean
            Get
                Return GetBooleanOption(LineCommitOptionsStorage.PrettyListing)
            End Get
            Set(value As Boolean)
                SetBooleanOption(LineCommitOptionsStorage.PrettyListing, value)
            End Set
        End Property

        Public Property NavigateToDecompiledSources As Boolean
            Get
                Return GetBooleanOption(MetadataAsSourceOptionsStorage.NavigateToDecompiledSources)
            End Get
            Set(value As Boolean)
                SetBooleanOption(MetadataAsSourceOptionsStorage.NavigateToDecompiledSources, value)
            End Set
        End Property

        Public Property NavigateToSourceLinkAndEmbeddedSources As Boolean
            Get
                Return GetBooleanOption(MetadataAsSourceOptionsStorage.NavigateToSourceLinkAndEmbeddedSources)
            End Get
            Set(value As Boolean)
                SetBooleanOption(MetadataAsSourceOptionsStorage.NavigateToSourceLinkAndEmbeddedSources, value)
            End Set
        End Property

        Public Property AlwaysUseDefaultSymbolServers As Boolean
            Get
                Return GetBooleanOption(MetadataAsSourceOptionsStorage.AlwaysUseDefaultSymbolServers)
            End Get
            Set(value As Boolean)
                SetBooleanOption(MetadataAsSourceOptionsStorage.AlwaysUseDefaultSymbolServers, value)
            End Set
        End Property

        Public Property AddImportsOnPaste As Boolean
            Get
                Return GetBooleanOption(AddImportOnPasteOptionsStorage.AddImportsOnPaste)
            End Get
            Set(value As Boolean)
                SetBooleanOption(AddImportOnPasteOptionsStorage.AddImportsOnPaste, value)
            End Set
        End Property

        Public Property OfferRemoveUnusedReferences As Integer
            Get
                Return GetBooleanOption(FeatureOnOffOptions.OfferRemoveUnusedReferences)
            End Get
            Set(value As Integer)
                SetBooleanOption(FeatureOnOffOptions.OfferRemoveUnusedReferences, value)
            End Set
        End Property

        Public Property SkipAnalyzersForImplicitlyTriggeredBuilds As Integer
            Get
                Return If(GetBooleanOption(FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds), 1, 0)
            End Get
            Set(value As Integer)
                SetBooleanOption(FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds, value <> 0)
            End Set
        End Property
    End Class
End Namespace
