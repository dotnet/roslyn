// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal class NamingStyle
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public string WordSeparator { get; set; }
        public Capitalization CapitalizationScheme { get; set; }

        public NamingStyle()
        {
            ID = Guid.NewGuid();
        }

        public string CreateName(IEnumerable<string> words)
        {
            EnsureNonNullProperties();

            var wordsWithCasing = ApplyCapitalization(words);
            var combinedWordsWithCasing = string.Join(WordSeparator, wordsWithCasing);
            return Prefix + combinedWordsWithCasing + Suffix;
        }

        private IEnumerable<string> ApplyCapitalization(IEnumerable<string> words)
        {
            switch (CapitalizationScheme)
            {
                case Capitalization.PascalCase:
                    return words.Select(CapitalizeFirstLetter);
                case Capitalization.CamelCase:
                    return words.Take(1).Select(DecapitalizeFirstLetter).Concat(words.Skip(1).Select(CapitalizeFirstLetter));
                case Capitalization.FirstUpper:
                    return words.Take(1).Select(CapitalizeFirstLetter).Concat(words.Skip(1).Select(DecapitalizeFirstLetter));
                case Capitalization.AllUpper:
                    return words.Select(w => w.ToUpper());
                case Capitalization.AllLower:
                    return words.Select(w => w.ToLower());
                default:
                    throw new InvalidOperationException();
            }
        }

        private string CapitalizeFirstLetter(string word)
        {
            var chars = word.ToCharArray();
            return new string(chars.Take(1).Select(c => char.ToUpper(c)).Concat(chars.Skip(1)).ToArray());
        }

        private string DecapitalizeFirstLetter(string word)
        {
            var chars = word.ToCharArray();
            return new string(chars.Take(1).Select(c => char.ToLower(c)).Concat(chars.Skip(1)).ToArray());
        }

        public bool IsNameCompliant(string name, out string failureReason)
        {
            EnsureNonNullProperties();

            if (!name.StartsWith(Prefix))
            {
                failureReason = string.Format(FeaturesResources.Missing_prefix_colon_0, Prefix);
                return false;
            }

            if (!name.EndsWith(Suffix))
            {
                failureReason = string.Format(FeaturesResources.Missing_suffix_colon_0, Suffix);
                return false;
            }

            if (name.Length <= Prefix.Length + Suffix.Length)
            {
                failureReason = null;
                return true;
            }

            name = name.Substring(Prefix.Length);
            name = name.Substring(0, name.Length - Suffix.Length);

            var words = new[] { name };
            if (!string.IsNullOrEmpty(WordSeparator))
            {
                words = name.Split(new[] { WordSeparator }, StringSplitOptions.RemoveEmptyEntries);
            }

            failureReason = string.Empty;
            switch (CapitalizationScheme)
            {
                case Capitalization.PascalCase:
                    if (words.All(w => char.IsUpper(w[0])))
                    {
                        return true;
                    }
                    else
                    {
                        var violations = words.Where(w => !char.IsUpper(w[0]));
                        failureReason = string.Format(FeaturesResources.These_words_must_begin_with_upper_case_characters_colon_0, string.Join(", ", violations));
                        return false;
                    }
                case Capitalization.CamelCase:
                    if (char.IsLower(words.First()[0]) && words.Skip(1).All(w => char.IsUpper(w[0])))
                    {
                        return true;
                    }
                    else
                    {
                        if (!char.IsLower(words.First()[0]))
                        {
                            failureReason = string.Format(FeaturesResources.The_first_word_0_must_begin_with_a_lower_case_character, words.First());
                        }

                        var violations = words.Skip(1).Where(w => !char.IsUpper(w[0]));
                        if (violations.Any())
                        {
                            if (failureReason != string.Empty)
                            {
                                failureReason += Environment.NewLine;
                            }

                            failureReason += string.Format(FeaturesResources.These_non_leading_words_must_begin_with_an_upper_case_letter_colon_0, string.Join(", ", violations));
                        }

                        return false;
                    }
                case Capitalization.FirstUpper:
                    if (char.IsUpper(words.First()[0]) && words.Skip(1).All(w => char.IsLower(w[0])))
                    {
                        return true;
                    }
                    else
                    {
                        if (!char.IsUpper(words.First()[0]))
                        {
                            failureReason = string.Format(FeaturesResources.The_first_word_0_must_begin_with_an_upper_case_character, words.First());
                        }

                        var violations = words.Skip(1).Where(w => !char.IsLower(w[0]));
                        if (violations.Any())
                        {
                            if (failureReason != string.Empty)
                            {
                                failureReason += Environment.NewLine;
                            }

                            failureReason += string.Format(FeaturesResources.These_non_leading_words_must_begin_with_a_lowercase_letter_colon_0, string.Join(", ", violations));
                        }

                        return false;
                    }
                case Capitalization.AllUpper:
                    if (words.SelectMany(w => w.ToCharArray()).All(c => char.IsUpper(c)))
                    {
                        return true;
                    }
                    else
                    {
                        var violations = words.Where(w => !w.ToCharArray().All(c => char.IsUpper(c)));
                        failureReason = string.Format(FeaturesResources.These_words_cannot_contain_lower_case_characters_colon_0, string.Join(", ", violations));
                        return false;
                    }
                case Capitalization.AllLower:
                    if (words.SelectMany(w => w.ToCharArray()).All(c => char.IsLower(c)))
                    {
                        return true;
                    }
                    else
                    {
                        var violations = words.Where(w => !w.ToCharArray().All(c => char.IsLower(c)));
                        failureReason = string.Format(FeaturesResources.These_words_cannot_contain_upper_case_characters_colon_0, string.Join(", ", violations));
                        return false;
                    }
                default:
                    throw new InvalidOperationException();
            }
        }

        internal NamingStyle Clone()
        {
            return new NamingStyle
            {
                ID = ID,
                Name = Name,
                Prefix = Prefix,
                Suffix = Suffix,
                WordSeparator = WordSeparator,
                CapitalizationScheme = CapitalizationScheme
            };
        }

        private string CreateCompliantNameDirectly(string name)
        {
            EnsureNonNullProperties();

            bool addPrefix = !name.StartsWith(Prefix);
            bool addSuffix = !name.EndsWith(Suffix);

            name = addPrefix ? (Prefix + name) : name;
            name = addSuffix ? (name + Suffix) : name;

            return FinishFixingName(name);
        }

        public IEnumerable<string> MakeCompliant(string name)
        {
            var name1 = CreateCompliantNameReusingPartialPrefixesAndSuffixes(name);
            yield return name1;

            var name2 = CreateCompliantNameDirectly(name);
            if (name2 != name1)
            {
                yield return name2;
            }
        }

        private string CreateCompliantNameReusingPartialPrefixesAndSuffixes(string name)
        {
            EnsureNonNullProperties();

            name = EnsurePrefix(name);
            name = EnsureSuffix(name);

            return FinishFixingName(name);
        }

        private string FinishFixingName(string name)
        {
            // Edge case: prefix "as", suffix "sa", name "asa"
            if (Suffix.Length + Prefix.Length >= name.Length)
            {
                return name;
            }

            name = name.Substring(Prefix.Length, name.Length - Suffix.Length - Prefix.Length);
            IEnumerable<string> words = new[] { name };
            if (!string.IsNullOrEmpty(WordSeparator))
            {
                words = name.Split(new[] { WordSeparator }, StringSplitOptions.RemoveEmptyEntries);
            }

            words = ApplyCapitalization(words);

            return Prefix + string.Join(WordSeparator, words) + Suffix;
        }

        private string EnsureSuffix(string name)
        {
            // If the name already ends with any prefix of the Suffix, only append the suffix of
            // the Suffix not contained in the longest such Suffix prefix. For example, if the 
            // required suffix is "_catdog" and the name is "test_cat", then only append "dog".
            for (int i = Suffix.Length; i > 0 ; i--)
            {
                if (name.EndsWith(Suffix.Substring(0, i)))
                {
                    return name + Suffix.Substring(i);
                }
            }

            return name + Suffix;
        }

        private string EnsurePrefix(string name)
        {
            // If the name already starts with any suffix of the Prefix, only prepend the prefix of
            // the Prefix not contained in the longest such Prefix suffix. For example, if the 
            // required prefix is "catdog_" and the name is "dog_test", then only prepend "cat".
            for (int i = 0; i < Prefix.Length; i++)
            {
                if (name.StartsWith(Prefix.Substring(i)))
                {
                    return Prefix.Substring(0, i) + name;
                }
            }

            return Prefix + name;
        }

        private void EnsureNonNullProperties()
        {
            Prefix = Prefix ?? string.Empty;
            Suffix = Suffix ?? string.Empty;
            WordSeparator = WordSeparator ?? string.Empty;
        }

        internal XElement CreateXElement()
        {
            return new XElement(nameof(NamingStyle), 
                new XAttribute(nameof(ID), ID),
                new XAttribute(nameof(Name), Name),
                new XAttribute(nameof(Prefix), Prefix ?? string.Empty),
                new XAttribute(nameof(Suffix), Suffix ?? string.Empty),
                new XAttribute(nameof(WordSeparator), WordSeparator ?? string.Empty),
                new XAttribute(nameof(CapitalizationScheme), CapitalizationScheme));
        }

        internal static NamingStyle FromXElement(XElement namingStyleElement)
        {
            return new NamingStyle()
            {
                ID = Guid.Parse(namingStyleElement.Attribute(nameof(ID)).Value),
                Name = namingStyleElement.Attribute(nameof(Name)).Value,
                Prefix = namingStyleElement.Attribute(nameof(Prefix)).Value,
                Suffix = namingStyleElement.Attribute(nameof(Suffix)).Value,
                WordSeparator = namingStyleElement.Attribute(nameof(WordSeparator)).Value,
                CapitalizationScheme = (Capitalization)Enum.Parse(typeof(Capitalization), namingStyleElement.Attribute(nameof(CapitalizationScheme)).Value)
            };
        }
    }
}