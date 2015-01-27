// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation
{
    internal class Signature : ISignature
    {
        private const int MaxParamColumnCount = 100;

        private readonly SignatureHelpItem _signatureHelpItem;
        private IParameter _currentParameter;
        internal IList<SymbolDisplayPart> DisplayParts { get; private set; }
        private IList<SymbolDisplayPart> _prettyPrintedDisplayParts;

        public ITrackingSpan ApplicableToSpan { get; internal set; }
        public string Content { get; private set; }
        public string PrettyPrintedContent { get; private set; }
        public ReadOnlyCollection<IParameter> Parameters { get; private set; }
        public string Documentation { get; private set; }

        public event EventHandler<CurrentParameterChangedEventArgs> CurrentParameterChanged;

        public Signature(ITrackingSpan applicableToSpan, SignatureHelpItem signatureHelpItem)
        {
            this.ApplicableToSpan = applicableToSpan;
            _signatureHelpItem = signatureHelpItem;
            this.Initialize(setParameters: true);
        }

        private void Initialize(bool setParameters)
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

            for (int i = 0; i < _signatureHelpItem.Parameters.Count; i++)
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

            if (_currentParameter != null)
            {
                var sigHelpParameter = _signatureHelpItem.Parameters[this.Parameters.IndexOf(_currentParameter)];

                AddRange(sigHelpParameter.SelectedDisplayParts, parts, prettyPrintedParts);
                Append(sigHelpParameter.SelectedDisplayParts.GetFullText(), content, prettyPrintedContent);
            }

            AddRange(_signatureHelpItem.DescriptionParts, parts, prettyPrintedParts);
            Append(_signatureHelpItem.DescriptionParts.GetFullText(), content, prettyPrintedContent);

            if (_signatureHelpItem.Documentation.Count > 0)
            {
                AddRange(new[] { newLinePart }, parts, prettyPrintedParts);
                Append(newLineContent, content, prettyPrintedContent);

                AddRange(_signatureHelpItem.Documentation, parts, prettyPrintedParts);
                Append(_signatureHelpItem.Documentation.GetFullText(), content, prettyPrintedContent);
            }

            this.Content = content.ToString();
            this.PrettyPrintedContent = prettyPrintedContent.ToString();
            this.DisplayParts = parts.ToImmutableArrayOrEmpty();
            this.PrettyPrintedDisplayParts = prettyPrintedParts.ToImmutableArrayOrEmpty();

            if (setParameters)
            {
                this.Parameters = parameters.ToReadOnlyCollection();
            }
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

        public IParameter CurrentParameter
        {
            get
            {
                return _currentParameter;
            }

            set
            {
                if (value == _currentParameter)
                {
                    return;
                }

                var oldValue = _currentParameter;
                _currentParameter = value;

                Initialize(setParameters: false);

                var currentParameterChanged = this.CurrentParameterChanged;
                if (currentParameterChanged != null)
                {
                    currentParameterChanged(this, new CurrentParameterChangedEventArgs(oldValue, value));
                }
            }
        }

        internal IList<SymbolDisplayPart> PrettyPrintedDisplayParts
        {
            get
            {
                return _prettyPrintedDisplayParts;
            }

            set
            {
                _prettyPrintedDisplayParts = value;
            }
        }
    }
}
