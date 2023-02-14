// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.AddImportOnPaste;
using Microsoft.CodeAnalysis.BlockCommentEditing;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.KeywordHighlighting;
using Microsoft.CodeAnalysis.LineSeparators;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.ReferenceHighlighting;
using Microsoft.CodeAnalysis.RenameTracking;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int AutoInsertAsteriskForNewLinesOfBlockComments
        {
            get { return GetBooleanOption(BlockCommentEditingOptions.AutoInsertBlockCommentStartString); }
            set { SetBooleanOption(BlockCommentEditingOptions.AutoInsertBlockCommentStartString, value); }
        }

        public int AutomaticallyFixStringContentsOnPaste
        {
            get { return GetBooleanOption(FeatureOnOffOptions.AutomaticallyFixStringContentsOnPaste); }
            set { SetBooleanOption(FeatureOnOffOptions.AutomaticallyFixStringContentsOnPaste, value); }
        }

        public int DisplayLineSeparators
        {
            get { return GetBooleanOption(LineSeparatorsOptions.LineSeparator); }
            set { SetBooleanOption(LineSeparatorsOptions.LineSeparator, value); }
        }

        public int EnableHighlightRelatedKeywords
        {
            get { return GetBooleanOption(KeywordHighlightingOptions.KeywordHighlighting); }
            set { SetBooleanOption(KeywordHighlightingOptions.KeywordHighlighting, value); }
        }

        public int EnterOutliningModeOnOpen
        {
            get { return GetBooleanOption(OutliningOptions.Outlining); }
            set { SetBooleanOption(OutliningOptions.Outlining, value); }
        }

        public int CollapseImportsWhenFirstOpened
        {
            get { return GetBooleanOption(BlockStructureOptionsStorage.CollapseImportsWhenFirstOpened); }
            set { SetBooleanOption(BlockStructureOptionsStorage.CollapseImportsWhenFirstOpened, value); }
        }

        public int CollapseRegionsWhenFirstOpened
        {
            get { return GetBooleanOption(BlockStructureOptionsStorage.CollapseRegionsWhenFirstOpened); }
            set { SetBooleanOption(BlockStructureOptionsStorage.CollapseRegionsWhenFirstOpened, value); }
        }

        public int CollapseMetadataSignatureFilesWhenFirstOpened
        {
            get { return GetBooleanOption(BlockStructureOptionsStorage.CollapseMetadataSignatureFilesWhenFirstOpened); }
            set { SetBooleanOption(BlockStructureOptionsStorage.CollapseMetadataSignatureFilesWhenFirstOpened, value); }
        }

        public int CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened
        {
            get { return GetBooleanOption(BlockStructureOptionsStorage.CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened); }
            set { SetBooleanOption(BlockStructureOptionsStorage.CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened, value); }
        }

        public int HighlightReferences
        {
            get { return GetBooleanOption(ReferenceHighlightingOptions.ReferenceHighlighting); }
            set { SetBooleanOption(ReferenceHighlightingOptions.ReferenceHighlighting, value); }
        }

        public int Refactoring_Verification_Enabled
        {
            get { return GetBooleanOption(FeatureOnOffOptions.RefactoringVerification); }
            set { SetBooleanOption(FeatureOnOffOptions.RefactoringVerification, value); }
        }

        public int RenameSmartTagEnabled
        {
            get { return GetBooleanOption(FeatureOnOffOptions.RenameTracking); }
            set { SetBooleanOption(FeatureOnOffOptions.RenameTracking, value); }
        }

        public int RenameTrackingPreview
        {
            get { return GetBooleanOption(RenameTrackingOptions.RenameTrackingPreview); }
            set { SetBooleanOption(RenameTrackingOptions.RenameTrackingPreview, value); }
        }

        public int NavigateAsynchronously
        {
            get { return GetBooleanOption(FeatureOnOffOptions.NavigateAsynchronously); }
            set { SetBooleanOption(FeatureOnOffOptions.NavigateAsynchronously, value); }
        }

        public int NavigateToDecompiledSources
        {
            get { return GetBooleanOption(MetadataAsSourceOptionsStorage.NavigateToDecompiledSources); }
            set { SetBooleanOption(MetadataAsSourceOptionsStorage.NavigateToDecompiledSources, value); }
        }

        public int NavigateToSourceLinkAndEmbeddedSources
        {
            get { return GetBooleanOption(MetadataAsSourceOptionsStorage.NavigateToSourceLinkAndEmbeddedSources); }
            set { SetBooleanOption(MetadataAsSourceOptionsStorage.NavigateToSourceLinkAndEmbeddedSources, value); }
        }

        public int AlwaysUseDefaultSymbolServers
        {
            get { return GetBooleanOption(MetadataAsSourceOptionsStorage.AlwaysUseDefaultSymbolServers); }
            set { SetBooleanOption(MetadataAsSourceOptionsStorage.AlwaysUseDefaultSymbolServers, value); }
        }

        public int AddImportsOnPaste
        {
            get { return GetBooleanOption(AddImportOnPasteOptions.AddImportsOnPaste); }
            set { SetBooleanOption(AddImportOnPasteOptions.AddImportsOnPaste, value); }
        }

        public int OfferRemoveUnusedReferences
        {
            get { return GetBooleanOption(FeatureOnOffOptions.OfferRemoveUnusedReferences); }
            set { SetBooleanOption(FeatureOnOffOptions.OfferRemoveUnusedReferences, value); }
        }

        public int AutomaticallyCompleteStatementOnSemicolon
        {
            get { return GetBooleanOption(FeatureOnOffOptions.AutomaticallyCompleteStatementOnSemicolon); }
            set { SetBooleanOption(FeatureOnOffOptions.AutomaticallyCompleteStatementOnSemicolon, value); }
        }

        public int SkipAnalyzersForImplicitlyTriggeredBuilds
        {
            get { return GetBooleanOption(FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds); }
            set { SetBooleanOption(FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds, value); }
        }
    }
}
