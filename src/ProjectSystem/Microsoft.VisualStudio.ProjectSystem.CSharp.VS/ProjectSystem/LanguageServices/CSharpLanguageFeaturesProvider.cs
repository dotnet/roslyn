// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    /// Unconfigured project level component to provide C# language specific features.
    /// </summary>
    [Export(typeof(ILanguageFeaturesProvider))]
    [AppliesTo(ProjectCapabilities.CSharp)]
    internal class CSharpLanguageFeaturesProvider : ILanguageFeaturesProvider
    {
        private static readonly ImmutableHashSet<UnicodeCategory> IdentifierCharCategories = ImmutableHashSet<UnicodeCategory>.Empty
            .Add(UnicodeCategory.UppercaseLetter)
            .Add(UnicodeCategory.LowercaseLetter)
            .Add(UnicodeCategory.TitlecaseLetter)
            .Add(UnicodeCategory.ModifierLetter)
            .Add(UnicodeCategory.OtherLetter)
            .Add(UnicodeCategory.DecimalDigitNumber)
            .Add(UnicodeCategory.ConnectorPunctuation)
            .Add(UnicodeCategory.EnclosingMark)
            .Add(UnicodeCategory.NonSpacingMark);

        private static readonly ImmutableHashSet<UnicodeCategory> FirstIdentifierCharCategories = ImmutableHashSet<UnicodeCategory>.Empty
            .Add(UnicodeCategory.UppercaseLetter)
            .Add(UnicodeCategory.LowercaseLetter)
            .Add(UnicodeCategory.TitlecaseLetter)
            .Add(UnicodeCategory.ModifierLetter)
            .Add(UnicodeCategory.OtherLetter)
            .Add(UnicodeCategory.ConnectorPunctuation);

        [ImportingConstructor]
        public CSharpLanguageFeaturesProvider()
        {
        }

        [Import]
        private UnconfiguredProject UnconfiguredProject
        {
            get;
            set;
        }

        /// <summary>
        /// Makes a proper identifier from the given string.
        /// </summary>
        /// <param name="name">The input string.</param>
        /// <returns>A proper identifier which meets the C# language spec.</returns>
        public string MakeProperIdentifier(string name)
        {
            Requires.NotNullOrEmpty(name, nameof(name));

            var identifier = string.Concat(name.Select(c => IsValidIdentifierChar(c) ? c : '_'));
            if (!IsValidFirstIdentifierChar(identifier.First())
                || identifier == "_")
            {
                identifier = '_' + identifier;
            }

            return identifier;
        }

        /// <summary>
        /// Makes a proper namespace from the given string.
        /// </summary>
        /// <param name="name">The input string.</param>
        /// <returns>A proper namespace which meets the C# language spec.</returns>
        public string MakeProperNamespace(string name)
        {
            Requires.NotNullOrEmpty(name, nameof(name));

            var identifiers = from token in name.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                              let id = MakeProperIdentifier(token)
                              where !string.IsNullOrEmpty(id)
                              select id;
            return string.Join(".", identifiers);
        }

        /// <summary>
        /// Concatenate multiple namespace names.
        /// </summary>
        /// <param name="namespaceNames">The array of namespace names to be concatenated.</param>
        /// <returns>A concatenated namespace name.</returns>
        public string ConcatNamespaces(params string[] namespaceNames)
        {
            Requires.NotNull(namespaceNames, nameof(namespaceNames));

            return string.Join(".", namespaceNames.Where(name => !string.IsNullOrEmpty(name)));
        }

        private static bool IsValidIdentifierChar(char ch)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            return IdentifierCharCategories.Contains(category);
        }

        private static bool IsValidFirstIdentifierChar(char ch)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            return FirstIdentifierCharCategories.Contains(category);
        }
    }
}
