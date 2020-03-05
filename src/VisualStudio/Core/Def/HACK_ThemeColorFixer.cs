﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

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
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Analyzable)]
    internal sealed class HACK_ThemeColorFixer : IWpfTextViewConnectionListener
    {
        private readonly IClassificationTypeRegistryService _classificationTypeRegistryService;
        private readonly IClassificationFormatMapService _classificationFormatMapService;

        private bool _done;

        [ImportingConstructor]
        public HACK_ThemeColorFixer(
            IClassificationTypeRegistryService classificationTypeRegistryService,
            IClassificationFormatMapService classificationFormatMapService)
        {
            _classificationTypeRegistryService = classificationTypeRegistryService;
            _classificationFormatMapService = classificationFormatMapService;

            // Note: We never unsubscribe from this event. This service lives for the lifetime of VS.
            _classificationFormatMapService.GetClassificationFormatMap("text").ClassificationFormatMappingChanged += TextFormatMap_ClassificationFormatMappingChanged;
        }

        private void TextFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e)
        {
            VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.UIThreadIdlePriority, RefreshThemeColors);
        }

        public void RefreshThemeColors()
        {
            var textFormatMap = _classificationFormatMapService.GetClassificationFormatMap("text");
            var tooltipFormatMap = _classificationFormatMapService.GetClassificationFormatMap("tooltip");

            UpdateForegroundColors(textFormatMap, tooltipFormatMap);
        }

        private void UpdateForegroundColors(
            IClassificationFormatMap sourceFormatMap,
            IClassificationFormatMap targetFormatMap)
        {
            UpdateForegroundColor(ClassificationTypeNames.Comment, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.ExcludedCode, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.Identifier, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.Keyword, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.ControlKeyword, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.NumericLiteral, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.StringLiteral, sourceFormatMap, targetFormatMap);

            UpdateForegroundColor(ClassificationTypeNames.XmlDocCommentAttributeName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlDocCommentAttributeQuotes, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlDocCommentAttributeValue, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlDocCommentText, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlDocCommentDelimiter, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlDocCommentComment, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.XmlDocCommentCDataSection, sourceFormatMap, targetFormatMap);

            UpdateForegroundColor(ClassificationTypeNames.RegexComment, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.RegexText, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.RegexCharacterClass, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.RegexQuantifier, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.RegexAnchor, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.RegexAlternation, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.RegexGrouping, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.RegexOtherEscape, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.RegexSelfEscapedCharacter, sourceFormatMap, targetFormatMap);

            UpdateForegroundColor(ClassificationTypeNames.PreprocessorKeyword, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.PreprocessorText, sourceFormatMap, targetFormatMap);

            UpdateForegroundColor(ClassificationTypeNames.Operator, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.OperatorOverloaded, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.Punctuation, sourceFormatMap, targetFormatMap);

            UpdateForegroundColor(ClassificationTypeNames.ClassName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.StructName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.InterfaceName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.DelegateName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.EnumName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.TypeParameterName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.ModuleName, sourceFormatMap, targetFormatMap);

            UpdateForegroundColor(ClassificationTypeNames.FieldName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.EnumMemberName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.ConstantName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.LocalName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.ParameterName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.MethodName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.ExtensionMethodName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.PropertyName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.EventName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.NamespaceName, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.LabelName, sourceFormatMap, targetFormatMap);

            UpdateForegroundColor(ClassificationTypeNames.VerbatimStringLiteral, sourceFormatMap, targetFormatMap);
            UpdateForegroundColor(ClassificationTypeNames.StringEscapeCharacter, sourceFormatMap, targetFormatMap);

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

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            // DevDiv https://devdiv.visualstudio.com/DevDiv/_workitems/edit/130129:
            //
            // This needs to be scheduled after editor has been composed. Otherwise
            // it may cause UI delays by composing the editor before it is needed
            // by the rest of VS.
            if (!_done)
            {
                _done = true;
                VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.UIThreadIdlePriority, RefreshThemeColors);
            }
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
        }
    }
}
