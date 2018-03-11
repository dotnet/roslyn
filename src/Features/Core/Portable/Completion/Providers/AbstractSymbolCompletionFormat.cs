// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractSymbolCompletionFormat
    {
        private readonly SymbolDisplayFormat _displayFormatWithFrameworkTypes;
        private readonly SymbolDisplayFormat _displayFormatWithPredefinedTypes;
        private readonly SymbolDisplayFormat _insertionFormatWithFrameworkTypes;
        private readonly SymbolDisplayFormat _insertionFormatWithPredefinedTypes;

        private readonly char _genericCommitChar;

        protected AbstractSymbolCompletionFormat(SymbolDisplayFormat displayFormat, SymbolDisplayFormat insertionFormat, char genericCommitChar)
        {
            if (displayFormat == null)
            {
                throw new ArgumentNullException(nameof(displayFormat));
            }

            if (insertionFormat == null)
            {
                throw new ArgumentNullException(nameof(insertionFormat));
            }

            _displayFormatWithPredefinedTypes = displayFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
            _displayFormatWithFrameworkTypes = displayFormat.RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
            _insertionFormatWithPredefinedTypes = insertionFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
            _insertionFormatWithFrameworkTypes = insertionFormat.RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            _genericCommitChar = genericCommitChar;
        }

        protected abstract string Escape(string identifier, SyntaxContext context);

        public (string displayText, string insertionText) GetMinimalDisplayAndInsertionText(ISymbol symbol, SyntaxContext context, OptionSet options)
        {
            var displayText = GetMinimalDisplayText(symbol, context, options);
            var insertionText = GetMinimalInsertionText(symbol, context, options);

            return (displayText, insertionText);
        }

        private string GetMinimalDisplayText(ISymbol symbol, SyntaxContext context, OptionSet options)
        {
            var format = SimplificationHelpers.PreferPredefinedTypeKeywordInDeclarations(options, context.SemanticModel.Language)
                ? _displayFormatWithPredefinedTypes
                : _displayFormatWithFrameworkTypes;

            return GetMinimalText(symbol, context, options, format);
        }

        private string GetMinimalInsertionText(ISymbol symbol, SyntaxContext context, OptionSet options)
        {
            var format = SimplificationHelpers.PreferPredefinedTypeKeywordInDeclarations(options, context.SemanticModel.Language)
                ? _insertionFormatWithPredefinedTypes
                : _insertionFormatWithFrameworkTypes;

            return GetMinimalText(symbol, context, options, format);
        }

        private string GetMinimalText(ISymbol symbol, SyntaxContext context, OptionSet options, SymbolDisplayFormat format)
        {
            if (symbol is IAliasSymbol)
            {
                // Using SymbolDisplayFormat would result in something like "alias = type",
                // so this needs to be special-cased here
                return (format.MiscellaneousOptions & SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers) != SymbolDisplayMiscellaneousOptions.None
                    ? Escape(symbol.Name, context)
                    : symbol.Name;
            }

            var service = context.GetLanguageService<ISymbolDisplayService>();
            return service.ToMinimalDisplayString(context.SemanticModel, context.Position, symbol, format);
        }

        public string GetInsertionTextAtInsertionTime(CompletionItem item, char? ch)
        {
            string insertionText = SymbolCompletionItem.GetInsertionText(item);

            if (ch == _genericCommitChar)
            {
                var index = insertionText.IndexOf(_genericCommitChar);

                if (index >= 0)
                {
                    return insertionText.Substring(0, index);
                }
            }

            return insertionText;
        }
    }
}
