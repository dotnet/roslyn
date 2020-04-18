// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        private IEnumerable<PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>>> GetAllParenthesesOptions()
            => GetAllExceptOtherParenthesesOptions().Concat(CodeStyleOptions2.OtherParentheses);

        internal OptionsCollection IgnoreAllParentheses
            => OptionsSet(GetAllParenthesesOptions().Select(
                o => SingleOption(o, IgnorePreference)).ToArray());

        internal OptionsCollection RemoveAllUnnecessaryParentheses
            => OptionsSet(GetAllParenthesesOptions().Select(
                o => SingleOption(o, RemoveIfUnnecessaryPreference)).ToArray());

        internal OptionsCollection RequireAllParenthesesForClarity
            => OptionsSet(GetAllExceptOtherParenthesesOptions()
                    .Select(o => SingleOption(o, RequireForPrecedenceClarityPreference))
                    .Concat(SingleOption(CodeStyleOptions2.OtherParentheses, RemoveIfUnnecessaryPreference)).ToArray());

        private OptionsCollection GetSingleRequireOption(PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> option)
            => OptionsSet(GetAllParenthesesOptions()
                    .Where(o => o != option)
                    .Select(o => SingleOption(o, RemoveIfUnnecessaryPreference))
                    .Concat(SingleOption(option, RequireForPrecedenceClarityPreference)).ToArray());

        private (OptionKey2, object) SingleOption<T>(PerLanguageOption2<CodeStyleOption2<T>> option, CodeStyleOption2<T> codeStyle)
            => (new OptionKey2(option, _language), codeStyle);

        internal OptionsCollection OptionsSet(params (OptionKey2 key, object value)[] options)
            => new OptionsCollection(_language, options);
    }
}
