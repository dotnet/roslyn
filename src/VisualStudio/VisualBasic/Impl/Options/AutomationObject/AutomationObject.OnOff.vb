' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.MetadataAsSource

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Public Class AutomationObject
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

        Public Property NavigateToDecompiledSources As Boolean
            Get
                Return GetBooleanOption(MetadataAsSourceOptionsStorage.NavigateToDecompiledSources)
            End Get
            Set(value As Boolean)
                SetBooleanOption(MetadataAsSourceOptionsStorage.NavigateToDecompiledSources, value)
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

        Public Property AddImportsOnPaste As Integer
            Get
                Return GetBooleanOption(FeatureOnOffOptions.AddImportsOnPaste)
            End Get
            Set(value As Integer)
                SetBooleanOption(FeatureOnOffOptions.AddImportsOnPaste, value)
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
