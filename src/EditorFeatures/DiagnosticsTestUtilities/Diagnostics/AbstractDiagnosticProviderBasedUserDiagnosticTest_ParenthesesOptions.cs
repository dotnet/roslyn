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
#if !CODE_STYLE
    using IOptionsCollection = IDictionary<OptionKey2, object>;
#endif

    public abstract partial class AbstractDiagnosticProviderBasedUserDiagnosticTest : AbstractUserDiagnosticTest
    {
        #region Parentheses options

        private static readonly CodeStyleOption2<ParenthesesPreference> IgnorePreference =
            new CodeStyleOption2<ParenthesesPreference>(ParenthesesPreference.AlwaysForClarity, NotificationOption2.None);

        private static readonly CodeStyleOption2<ParenthesesPreference> RequireForPrecedenceClarityPreference =
            new CodeStyleOption<ParenthesesPreference>(ParenthesesPreference.AlwaysForClarity, NotificationOption2.Suggestion);

        private static readonly CodeStyleOption2<ParenthesesPreference> RemoveIfUnnecessaryPreference =
            new CodeStyleOption2<ParenthesesPreference>(ParenthesesPreference.NeverIfUnnecessary, NotificationOption2.Suggestion);

        private static IEnumerable<PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>>> GetAllExceptOtherParenthesesOptions()
        {
            yield return CodeStyleOptions2.ArithmeticBinaryParentheses;
            yield return CodeStyleOptions2.RelationalBinaryParentheses;
            yield return CodeStyleOptions2.OtherBinaryParentheses;
        }

        private protected IOptionsCollection RequireArithmeticBinaryParenthesesForClarity
            => GetSingleRequireOption(CodeStyleOptions2.ArithmeticBinaryParentheses);

        private protected IOptionsCollection RequireRelationalBinaryParenthesesForClarity
            => GetSingleRequireOption(CodeStyleOptions2.RelationalBinaryParentheses);

        private protected IOptionsCollection RequireOtherBinaryParenthesesForClarity
            => GetSingleRequireOption(CodeStyleOptions2.OtherBinaryParentheses);

        private IEnumerable<PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>>> GetAllParenthesesOptions()
            => GetAllExceptOtherParenthesesOptions().Concat(CodeStyleOptions2.OtherParentheses);

        private protected IOptionsCollection IgnoreAllParentheses
            => OptionsSet(GetAllParenthesesOptions().Select(
                o => SingleOption(o, IgnorePreference)).ToArray());

        private protected IOptionsCollection RemoveAllUnnecessaryParentheses
            => OptionsSet(GetAllParenthesesOptions().Select(
                o => SingleOption(o, RemoveIfUnnecessaryPreference)).ToArray());

        private protected IOptionsCollection RequireAllParenthesesForClarity
            => OptionsSet(GetAllExceptOtherParenthesesOptions()
                    .Select(o => SingleOption(o, RequireForPrecedenceClarityPreference))
                    .Concat(SingleOption(CodeStyleOptions2.OtherParentheses, RemoveIfUnnecessaryPreference)).ToArray());

        private IOptionsCollection GetSingleRequireOption(PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> option)
            => OptionsSet(GetAllParenthesesOptions()
                    .Where(o => o != option)
                    .Select(o => SingleOption(o, RemoveIfUnnecessaryPreference))
                    .Concat(SingleOption(option, RequireForPrecedenceClarityPreference)).ToArray());

        #endregion
    }
}
