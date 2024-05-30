// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation
{
    internal class Signature : ISignature
    {
        private const int MaxParamColumnCount = 100;

        private readonly SignatureHelpItem _signatureHelpItem;

        public Signature(ITrackingSpan applicableToSpan, SignatureHelpItem signatureHelpItem, int selectedParameterIndex)
        {
            if (selectedParameterIndex < -1 || selectedParameterIndex >= signatureHelpItem.Parameters.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedParameterIndex));
            }

            this.ApplicableToSpan = applicableToSpan;
            _signatureHelpItem = signatureHelpItem;
            _parameterIndex = selectedParameterIndex;
        }

        private bool _isInitialized;
        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                Initialize();
            }
        }

        private Signature InitializedThis
        {
            get
            {
                EnsureInitialized();
                return this;
            }
        }

        private IList<TaggedText> _displayParts;
        internal IList<TaggedText> DisplayParts => InitializedThis._displayParts;

        public ITrackingSpan ApplicableToSpan { get; }

        private string _content;
        public string Content => InitializedThis._content;

        private readonly int _parameterIndex = -1;
        public IParameter CurrentParameter
        {
            get
            {
                EnsureInitialized();
                return _parameterIndex >= 0 && _parameters != null ? _parameters[_parameterIndex] : null;
            }
        }

        /// <remarks>
        /// The documentation is included in <see cref="Content"/> so that it will be classified.
        /// </remarks>
        public string Documentation => null;

        private ReadOnlyCollection<IParameter> _parameters;
        public ReadOnlyCollection<IParameter> Parameters => InitializedThis._parameters;

        private string _prettyPrintedContent;
        public string PrettyPrintedContent => InitializedThis._prettyPrintedContent;

        // This event is required by the ISignature interface but it's not actually used
        // (once created the CurrentParameter property cannot change)
        public event EventHandler<CurrentParameterChangedEventArgs> CurrentParameterChanged
        {
            add
            {
            }
            remove
            {
            }
        }

        private IList<TaggedText> _prettyPrintedDisplayParts;
        internal IList<TaggedText> PrettyPrintedDisplayParts => InitializedThis._prettyPrintedDisplayParts;

        private void Initialize()
        {
            var content = new StringBuilder();
            var prettyPrintedContent = new StringBuilder();

            var parts = new List<TaggedText>();
            var prettyPrintedParts = new List<TaggedText>();

            var parameters = new List<IParameter>();

            var signaturePrefixParts = _signatureHelpItem.PrefixDisplayParts;
            var signaturePrefixContent = _signatureHelpItem.PrefixDisplayParts.GetFullText();

            AddRange(signaturePrefixParts, parts, prettyPrintedParts);
            Append(signaturePrefixContent, content, prettyPrintedContent);

            var separatorParts = _signatureHelpItem.SeparatorDisplayParts;
            var separatorContent = separatorParts.GetFullText();

            var newLinePart = new TaggedText(TextTags.LineBreak, "\r\n");
            var newLineContent = newLinePart.ToString();
            var spacerPart = new TaggedText(TextTags.Space, new string(' ', signaturePrefixContent.Length));
            var spacerContent = spacerPart.ToString();

            var paramColumnCount = 0;

            for (var i = 0; i < _signatureHelpItem.Parameters.Length; i++)
            {
                var sigHelpParameter = _signatureHelpItem.Parameters[i];

                var parameterPrefixParts = sigHelpParameter.PrefixDisplayParts;
                var parameterPrefixContext = sigHelpParameter.PrefixDisplayParts.GetFullText();

                var parameterParts = AddOptionalBrackets(
                    sigHelpParameter.IsOptional, sigHelpParameter.DisplayParts);
                var parameterContent = parameterParts.GetFullText();

                var parameterSuffixParts = sigHelpParameter.SuffixDisplayParts;
                var parameterSuffixContext = sigHelpParameter.SuffixDisplayParts.GetFullText();

                paramColumnCount += separatorContent.Length + parameterPrefixContext.Length + parameterContent.Length + parameterSuffixContext.Length;

                if (i > 0)
                {
                    AddRange(separatorParts, parts, prettyPrintedParts);
                    Append(separatorContent, content, prettyPrintedContent);

                    if (paramColumnCount > MaxParamColumnCount)
                    {
                        prettyPrintedParts.Add(newLinePart);
                        prettyPrintedParts.Add(spacerPart);
                        prettyPrintedContent.Append(newLineContent);
                        prettyPrintedContent.Append(spacerContent);

                        paramColumnCount = 0;
                    }
                }

                AddRange(parameterPrefixParts, parts, prettyPrintedParts);
                Append(parameterPrefixContext, content, prettyPrintedContent);

                parameters.Add(new Parameter(this, sigHelpParameter, parameterContent, content.Length, prettyPrintedContent.Length));

                AddRange(parameterParts, parts, prettyPrintedParts);
                Append(parameterContent, content, prettyPrintedContent);

                AddRange(parameterSuffixParts, parts, prettyPrintedParts);
                Append(parameterSuffixContext, content, prettyPrintedContent);
            }

            AddRange(_signatureHelpItem.SuffixDisplayParts, parts, prettyPrintedParts);
            Append(_signatureHelpItem.SuffixDisplayParts.GetFullText(), content, prettyPrintedContent);

            if (_parameterIndex >= 0)
            {
                var sigHelpParameter = _signatureHelpItem.Parameters[_parameterIndex];

                AddRange(sigHelpParameter.SelectedDisplayParts, parts, prettyPrintedParts);
                Append(sigHelpParameter.SelectedDisplayParts.GetFullText(), content, prettyPrintedContent);
            }

            AddRange(_signatureHelpItem.DescriptionParts, parts, prettyPrintedParts);
            Append(_signatureHelpItem.DescriptionParts.GetFullText(), content, prettyPrintedContent);

            var documentation = _signatureHelpItem.DocumentationFactory(CancellationToken.None).ToList();
            if (documentation.Count > 0)
            {
                AddRange(new[] { newLinePart }, parts, prettyPrintedParts);
                Append(newLineContent, content, prettyPrintedContent);

                AddRange(documentation, parts, prettyPrintedParts);
                Append(documentation.GetFullText(), content, prettyPrintedContent);
            }

            _content = content.ToString();
            _prettyPrintedContent = prettyPrintedContent.ToString();
            _displayParts = parts.ToImmutableArrayOrEmpty();
            _prettyPrintedDisplayParts = prettyPrintedParts.ToImmutableArrayOrEmpty();
            _parameters = parameters.ToReadOnlyCollection();
        }

        private static void AddRange(IList<TaggedText> values, List<TaggedText> parts, List<TaggedText> prettyPrintedParts)
        {
            parts.AddRange(values);
            prettyPrintedParts.AddRange(values);
        }

        private static void Append(string text, StringBuilder content, StringBuilder prettyPrintedContent)
        {
            content.Append(text);
            prettyPrintedContent.Append(text);
        }

        private static IList<TaggedText> AddOptionalBrackets(bool isOptional, IList<TaggedText> list)
        {
            if (isOptional)
            {
                var result = new List<TaggedText>
                {
                    new TaggedText(TextTags.Punctuation, "[")
                };
                result.AddRange(list);
                result.Add(new TaggedText(TextTags.Punctuation, "]"));
                return result;
            }

            return list;
        }
    }
}
