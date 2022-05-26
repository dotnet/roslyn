// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public sealed class ParenthesesOptionsProvider
    {
        private readonly string _language;
        public ParenthesesOptionsProvider(string language)
        {
            _language = language;
        }

        private static readonly CodeStyleOption2<ParenthesesPreference> IgnorePreference =
            new CodeStyleOption2<ParenthesesPreference>(ParenthesesPreference.AlwaysForClarity, NotificationOption2.None);

        private static readonly CodeStyleOption2<ParenthesesPreference> RequireForPrecedenceClarityPreference =
            new CodeStyleOption2<ParenthesesPreference>(ParenthesesPreference.AlwaysForClarity, NotificationOption2.Suggestion);

        private static readonly CodeStyleOption2<ParenthesesPreference> RemoveIfUnnecessaryPreference =
            new CodeStyleOption2<ParenthesesPreference>(ParenthesesPreference.NeverIfUnnecessary, NotificationOption2.Suggestion);

        private static IEnumerable<PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>>> GetAllExceptOtherParenthesesOptions()
        {
            yield return CodeStyleOptions2.ArithmeticBinaryParentheses;
            yield return CodeStyleOptions2.RelationalBinaryParentheses;
            yield return CodeStyleOptions2.OtherBinaryParentheses;
        }

        internal OptionsCollection RequireArithmeticBinaryParenthesesForClarity
            => GetSingleRequireOption(CodeStyleOptions2.ArithmeticBinaryParentheses);

        internal OptionsCollection RequireRelationalBinaryParenthesesForClarity
            => GetSingleRequireOption(CodeStyleOptions2.RelationalBinaryParentheses);

        internal OptionsCollection RequireOtherBinaryParenthesesForClarity
            => GetSingleRequireOption(CodeStyleOptions2.OtherBinaryParentheses);

        private static IEnumerable<PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>>> GetAllParenthesesOptions()
            => GetAllExceptOtherParenthesesOptions().Concat(CodeStyleOptions2.OtherParentheses);

        internal OptionsCollection IgnoreAllParentheses
        {
            get
            {
                var optionsCollection = new OptionsCollection(_language);
                foreach (var option in GetAllParenthesesOptions())
                {
                    optionsCollection.Add(option, IgnorePreference);
                }

                return optionsCollection;
            }
        }

        internal OptionsCollection RemoveAllUnnecessaryParentheses
        {
            get
            {
                var optionsCollection = new OptionsCollection(_language);
                foreach (var option in GetAllParenthesesOptions())
                {
                    optionsCollection.Add(option, RemoveIfUnnecessaryPreference);
                }

                return optionsCollection;
            }
        }

        internal OptionsCollection RequireAllParenthesesForClarity
        {
            get
            {
                var optionsCollection = new OptionsCollection(_language);
                foreach (var option in GetAllExceptOtherParenthesesOptions())
                {
                    optionsCollection.Add(option, RequireForPrecedenceClarityPreference);
                }

                optionsCollection.Add(CodeStyleOptions2.OtherParentheses, RemoveIfUnnecessaryPreference);
                return optionsCollection;
            }
        }

        private OptionsCollection GetSingleRequireOption(PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> option)
        {
            var optionsCollection = new OptionsCollection(_language);
            foreach (var o in GetAllParenthesesOptions())
            {
                if (o != option)
                {
                    optionsCollection.Add(o, RemoveIfUnnecessaryPreference);
                }
            }

            optionsCollection.Add(option, RequireForPrecedenceClarityPreference);
            return optionsCollection;
        }
    }
}
