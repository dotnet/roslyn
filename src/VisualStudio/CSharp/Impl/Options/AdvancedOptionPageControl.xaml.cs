// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    internal partial class AdvancedOptionPageControl : AbstractOptionPageControl
    {
        public AdvancedOptionPageControl(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            InitializeComponent();

            BindToOption(PlaceSystemNamespaceFirst, OrganizerOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp);
            BindToOption(SuggestForTypesInReferenceAssemblies, AddImportOptions.SuggestForTypesInReferenceAssemblies, LanguageNames.CSharp);
            BindToOption(SuggestForTypesInNuGetPackages, AddImportOptions.SuggestForTypesInNuGetPackages, LanguageNames.CSharp);
            BindToOption(Split_string_literals_on_enter, SplitStringLiteralOptions.Enabled, LanguageNames.CSharp);

            BindToOption(EnterOutliningMode, FeatureOnOffOptions.Outlining, LanguageNames.CSharp);
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
