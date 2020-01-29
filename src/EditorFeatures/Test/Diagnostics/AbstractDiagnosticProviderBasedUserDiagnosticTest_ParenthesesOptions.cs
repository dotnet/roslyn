// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public abstract partial class AbstractDiagnosticProviderBasedUserDiagnosticTest : AbstractUserDiagnosticTest
    {
        #region Parentheses options

        private static readonly CodeStyleOption<ParenthesesPreference> IgnorePreference =
            new CodeStyleOption<ParenthesesPreference>(ParenthesesPreference.AlwaysForClarity, NotificationOption.None);

        private static readonly CodeStyleOption<ParenthesesPreference> RequireForPrecedenceClarityPreference =
            new CodeStyleOption<ParenthesesPreference>(ParenthesesPreference.AlwaysForClarity, NotificationOption.Suggestion);

        private static readonly CodeStyleOption<ParenthesesPreference> RemoveIfUnnecessaryPreference =
            new CodeStyleOption<ParenthesesPreference>(ParenthesesPreference.NeverIfUnnecessary, NotificationOption.Suggestion);

        private static IEnumerable<PerLanguageOption<CodeStyleOption<ParenthesesPreference>>> GetAllExceptOtherParenthesesOptions()
        {
            yield return CodeStyleOptions.ArithmeticBinaryParentheses;
            yield return CodeStyleOptions.RelationalBinaryParentheses;
            yield return CodeStyleOptions.OtherBinaryParentheses;
        }

        protected IDictionary<OptionKey, object> RequireArithmeticBinaryParenthesesForClarity
            => GetSingleRequireOption(CodeStyleOptions.ArithmeticBinaryParentheses);

        protected IDictionary<OptionKey, object> RequireRelationalBinaryParenthesesForClarity
            => GetSingleRequireOption(CodeStyleOptions.RelationalBinaryParentheses);

        protected IDictionary<OptionKey, object> RequireOtherBinaryParenthesesForClarity
            => GetSingleRequireOption(CodeStyleOptions.OtherBinaryParentheses);

        private IEnumerable<PerLanguageOption<CodeStyleOption<ParenthesesPreference>>> GetAllParenthesesOptions()
            => GetAllExceptOtherParenthesesOptions().Concat(CodeStyleOptions.OtherParentheses);

        protected IDictionary<OptionKey, object> IgnoreAllParentheses
            => OptionsSet(GetAllParenthesesOptions().Select(
                o => SingleOption(o, IgnorePreference)).ToArray());

        protected IDictionary<OptionKey, object> RemoveAllUnnecessaryParentheses
            => OptionsSet(GetAllParenthesesOptions().Select(
                o => SingleOption(o, RemoveIfUnnecessaryPreference)).ToArray());

        protected IDictionary<OptionKey, object> RequireAllParenthesesForClarity
            => OptionsSet(GetAllExceptOtherParenthesesOptions()
                    .Select(o => SingleOption(o, RequireForPrecedenceClarityPreference))
                    .Concat(SingleOption(CodeStyleOptions.OtherParentheses, RemoveIfUnnecessaryPreference)).ToArray());

        private IDictionary<OptionKey, object> GetSingleRequireOption(PerLanguageOption<CodeStyleOption<ParenthesesPreference>> option)
            => OptionsSet(GetAllParenthesesOptions()
                    .Where(o => o != option)
                    .Select(o => SingleOption(o, RemoveIfUnnecessaryPreference))
                    .Concat(SingleOption(option, RequireForPrecedenceClarityPreference)).ToArray());

        #endregion
    }
}
