// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;
using System.Threading;

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

        private IList<SymbolDisplayPart> _displayParts;
        internal IList<SymbolDisplayPart> DisplayParts => InitializedThis._displayParts;

        public ITrackingSpan ApplicableToSpan { get; }

        private string _content;
        public string Content => InitializedThis._content;

        private int _parameterIndex = -1;
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

        private IList<SymbolDisplayPart> _prettyPrintedDisplayParts;
        internal IList<SymbolDisplayPart> PrettyPrintedDisplayParts
        {
            get
            {
                return InitializedThis._prettyPrintedDisplayParts;
            }

            set
            {
                _prettyPrintedDisplayParts = value;
            }
        }

        private void Initialize()
        {
            var content = new StringBuilder();
            var prettyPrintedContent = new StringBuilder();

            var parts = new List<SymbolDisplayPart>();
            var prettyPrintedParts = new List<SymbolDisplayPart>();

            var parameters = new List<IParameter>();

            var signaturePrefixParts = _signatureHelpItem.PrefixDisplayParts;
            var signaturePrefixContent = _signatureHelpItem.PrefixDisplayParts.GetFullText();

            AddRange(signaturePrefixParts, parts, prettyPrintedParts);
            Append(signaturePrefixContent, content, prettyPrintedContent);

            var separatorParts = _signatureHelpItem.SeparatorDisplayParts;
            var separatorContent = separatorParts.GetFullText();

            var newLinePart = new SymbolDisplayPart(SymbolDisplayPartKind.LineBreak, null, "\r\n");
            var newLineContent = newLinePart.ToString();
            var spacerPart = new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, new string(' ', signaturePrefixContent.Length));
            var spacerContent = spacerPart.ToString();

            var paramColumnCount = 0;

            for (int i = 0; i < _signatureHelpItem.Parameters.Length; i++)
            {
                var sigHelpParameter = _signatureHelpItem.Parameters[i];

                var parameterPrefixParts = sigHelpParameter.PrefixDisplayParts;
                var parameterPrefixContext = sigHelpParameter.PrefixDisplayParts.GetFullText();

                var parameterParts = AddOptionalBrackets(sigHelpParameter.IsOptional, sigHelpParameter.DisplayParts);
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

        private void AddRange(IList<SymbolDisplayPart> values, List<SymbolDisplayPart> parts, List<SymbolDisplayPart> prettyPrintedParts)
        {
            parts.AddRange(values);
            prettyPrintedParts.AddRange(values);
        }

        private void Append(string text, StringBuilder content, StringBuilder prettyPrintedContent)
        {
            content.Append(text);
            prettyPrintedContent.Append(text);
        }

        private IList<SymbolDisplayPart> AddOptionalBrackets(bool isOptional, IList<SymbolDisplayPart> list)
        {
            if (isOptional)
            {
                var result = new List<SymbolDisplayPart>();
                result.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, "["));
                result.AddRange(list);
                result.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, "]"));
                return result;
            }

            return list;
        }
    }
}
