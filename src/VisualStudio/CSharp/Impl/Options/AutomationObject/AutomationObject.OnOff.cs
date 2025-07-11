// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.AddImportOnPaste;
using Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentEditing;
using Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement;
using Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.KeywordHighlighting;
using Microsoft.CodeAnalysis.LineSeparators;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.ReferenceHighlighting;
using Microsoft.CodeAnalysis.StringCopyPaste;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options;

public partial class AutomationObject
{
    public int AutoInsertAsteriskForNewLinesOfBlockComments
    {
        get { return GetBooleanOption(BlockCommentEditingOptionsStorage.AutoInsertBlockCommentStartString); }
        set { SetBooleanOption(BlockCommentEditingOptionsStorage.AutoInsertBlockCommentStartString, value); }
    }

    public int AutomaticallyFixStringContentsOnPaste
    {
        get { return GetBooleanOption(StringCopyPasteOptionsStorage.AutomaticallyFixStringContentsOnPaste); }
        set { SetBooleanOption(StringCopyPasteOptionsStorage.AutomaticallyFixStringContentsOnPaste, value); }
    }

    public int DisplayLineSeparators
    {
        get { return GetBooleanOption(LineSeparatorsOptionsStorage.LineSeparator); }
        set { SetBooleanOption(LineSeparatorsOptionsStorage.LineSeparator, value); }
    }

    public int EnableHighlightRelatedKeywords
    {
        get { return GetBooleanOption(KeywordHighlightingOptionsStorage.KeywordHighlighting); }
        set { SetBooleanOption(KeywordHighlightingOptionsStorage.KeywordHighlighting, value); }
    }

    public int EnterOutliningModeOnOpen
    {
        get { return GetBooleanOption(OutliningOptionsStorage.Outlining); }
        set { SetBooleanOption(OutliningOptionsStorage.Outlining, value); }
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
        get { return GetBooleanOption(ReferenceHighlightingOptionsStorage.ReferenceHighlighting); }
        set { SetBooleanOption(ReferenceHighlightingOptionsStorage.ReferenceHighlighting, value); }
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
        get { return GetBooleanOption(RenameTrackingOptionsStorage.RenameTrackingPreview); }
        set { SetBooleanOption(RenameTrackingOptionsStorage.RenameTrackingPreview, value); }
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
        get { return GetBooleanOption(AddImportOnPasteOptionsStorage.AddImportsOnPaste); }
        set { SetBooleanOption(AddImportOnPasteOptionsStorage.AddImportsOnPaste, value); }
    }

    public int OfferRemoveUnusedReferences
    {
        get { return GetBooleanOption(FeatureOnOffOptions.OfferRemoveUnusedReferences); }
        set { SetBooleanOption(FeatureOnOffOptions.OfferRemoveUnusedReferences, value); }
    }

    public int AutomaticallyCompleteStatementOnSemicolon
    {
        get { return GetBooleanOption(CompleteStatementOptionsStorage.AutomaticallyCompleteStatementOnSemicolon); }
        set { SetBooleanOption(CompleteStatementOptionsStorage.AutomaticallyCompleteStatementOnSemicolon, value); }
    }

    public int SkipAnalyzersForImplicitlyTriggeredBuilds
    {
        get { return GetBooleanOption(FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds); }
        set { SetBooleanOption(FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds, value); }
    }
}
