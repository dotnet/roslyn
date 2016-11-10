// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    internal partial class AdvancedOptionPageControl : AbstractOptionPageControl
    {
        public AdvancedOptionPageControl(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            InitializeComponent();

            BindToOption(PlaceSystemNamespaceFirst, GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp);
            BindToOption(SuggestForTypesInReferenceAssemblies, SymbolSearchOptions.SuggestForTypesInReferenceAssemblies, LanguageNames.CSharp);
            BindToOption(SuggestForTypesInNuGetPackages, SymbolSearchOptions.SuggestForTypesInNuGetPackages, LanguageNames.CSharp);
            BindToOption(Split_string_literals_on_enter, SplitStringLiteralOptions.Enabled, LanguageNames.CSharp);

            BindToOption(EnterOutliningMode, FeatureOnOffOptions.Outlining, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_declaration_level_constructs, BlockStructureOptions.ShowOutliningForDeclarationLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_code_level_constructs, BlockStructureOptions.ShowOutliningForCodeLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_comments_and_preprocessor_regions, BlockStructureOptions.ShowOutliningForCommentsAndPreprocessorRegions, LanguageNames.CSharp);
            BindToOption(Collapse_regions_when_collapsing_to_definitions, BlockStructureOptions.CollapseRegionsWhenCollapsingToDefinitions, LanguageNames.CSharp);

            BindToOption(Show_guides_for_declaration_level_constructs, BlockStructureOptions.ShowBlockStructureGuidesForDeclarationLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_guides_for_code_level_constructs, BlockStructureOptions.ShowBlockStructureGuidesForCodeLevelConstructs, LanguageNames.CSharp);

            BindToOption(GenerateXmlDocCommentsForTripleSlash, FeatureOnOffOptions.AutoXmlDocCommentGeneration, LanguageNames.CSharp);
            BindToOption(InsertAsteriskAtTheStartOfNewLinesWhenWritingBlockComments, FeatureOnOffOptions.AutoInsertBlockCommentStartString, LanguageNames.CSharp);
            BindToOption(DisplayLineSeparators, FeatureOnOffOptions.LineSeparator, LanguageNames.CSharp);
            BindToOption(EnableHighlightReferences, FeatureOnOffOptions.ReferenceHighlighting, LanguageNames.CSharp);
            BindToOption(EnableHighlightKeywords, FeatureOnOffOptions.KeywordHighlighting, LanguageNames.CSharp);
            BindToOption(RenameTrackingPreview, FeatureOnOffOptions.RenameTrackingPreview, LanguageNames.CSharp);
            BindToOption(DontPutOutOrRefOnStruct, ExtractMethodOptions.DontPutOutOrRefOnStruct, LanguageNames.CSharp);
            BindToOption(AllowMovingDeclaration, ExtractMethodOptions.AllowMovingDeclaration, LanguageNames.CSharp);
            BindToFullSolutionAnalysisOption(ClosedFileDiagnostics, LanguageNames.CSharp);
        }
    }
}
