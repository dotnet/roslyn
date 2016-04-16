// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// This class works around the fact that shell theme changes are not fully propagated into an
    /// editor classification format map unless a classification type is registered as a font and
    /// color item in that format map's font and color category. So, for example, the "Keyword"
    /// classification type in the "tooltip" classification format map is never is never updated
    /// from its default blue. As a work around, we listen to <see cref="IClassificationFormatMap.ClassificationFormatMappingChanged"/>
    /// and update the classification format maps that we care about.
    /// </summary>
    [Export]
    internal sealed class HACK_ThemeColorFixer
    {
        private readonly IClassificationTypeRegistryService _classificationTypeRegistryService;
        private readonly IClassificationFormatMapService _classificationFormatMapService;

        [ImportingConstructor]
        private HACK_ThemeColorFixer(
            IClassificationTypeRegistryService classificationTypeRegistryService,
            IClassificationFormatMapService classificationFormatMapService)
        {
            _classificationTypeRegistryService = classificationTypeRegistryService;
            _classificationFormatMapService = classificationFormatMapService;

            // Note: We never unsubscribe from this event. This service lives for the lifetime of VS.
            _classificationFormatMapService.GetClassificationFormatMap("text").ClassificationFormatMappingChanged += TextFormatMap_ClassificationFormatMappingChanged;

            // make this to run on UI thread when there is no work for the application
            VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.UIThreadIdlePriority, RefreshThemeColors);
        }

        private void TextFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e)
        {
            VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.UIThreadIdlePriority, RefreshThemeColors);
        }

        public void RefreshThemeColors()
        {
            var textFormatMap = _classificationFormatMapService.GetClassificationFormatMap("text");
            var tooltipFormatMap = _classificationFormatMapService.GetClassificationFormatMap("tooltip");
            var immediateFormatMap = _classificationFormatMapService.GetClassificationFormatMap("immediate");

            UpdateForegroundColors(textFormatMap, tooltipFormatMap);
            UpdateForegroundColors(textFormatMap, immediateFormatMap);
        }

        private void UpdateForegroundColors(
            IClassificationFormatMap sourceFormatMap,
            IClassificationFormatMap targetFormatMap)
        {
            UpdateForegroundColor(ClassificationTypeNames.Comment, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.ExcludedCode, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.Identifier, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.Keyword, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.NumericLiteral, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.StringLiteral, sourceFormatMap, targetFormatMap);

            UpdateForegroundColor(ClassificationTypeNames.XmlDocCommentAttributeName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlDocCommentAttributeQuotes, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlDocCommentAttributeValue, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlDocCommentText, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlDocCommentDelimiter, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlDocCommentComment, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlDocCommentCDataSection, sourceFormatMap, targetFormatMap);

            UpdateForegroundColor(ClassificationTypeNames.PreprocessorKeyword, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.PreprocessorText, sourceFormatMap, targetFormatMap);

            UpdateForegroundColor(ClassificationTypeNames.Operator, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.Punctuation, sourceFormatMap, targetFormatMap);

            UpdateForegroundColor(ClassificationTypeNames.ClassName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.StructName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.InterfaceName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.DelegateName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.EnumName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.TypeParameterName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.ModuleName, sourceFormatMap, targetFormatMap);

            UpdateForegroundColor(ClassificationTypeNames.VerbatimStringLiteral, sourceFormatMap, targetFormatMap);

            UpdateForegroundColor(ClassificationTypeNames.XmlLiteralText, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlLiteralProcessingInstruction, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlLiteralName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlLiteralEmbeddedExpression, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlLiteralDelimiter, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlLiteralComment, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlLiteralCDataSection, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlLiteralAttributeValue, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlLiteralAttributeQuotes, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlLiteralAttributeName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlLiteralEntityReference, sourceFormatMap, targetFormatMap);
        }

        private void UpdateForegroundColor(
            string classificationTypeName,
            IClassificationFormatMap sourceFormatMap,
            IClassificationFormatMap targetFormatMap)
        {
            var classificationType = _classificationTypeRegistryService.GetClassificationType(classificationTypeName);
            if (classificationType == null)
            {
                return;
            }

            var sourceProps = sourceFormatMap.GetTextProperties(classificationType);
            var targetProps = targetFormatMap.GetTextProperties(classificationType);

            targetProps = targetProps.SetForegroundBrush(sourceProps.ForegroundBrush);

            targetFormatMap.SetTextProperties(classificationType, targetProps);
        }
    }
}
