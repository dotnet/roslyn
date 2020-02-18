// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions
{
    internal static class UseVarTestExtensions
    {
        private static readonly CodeStyleOption<bool> onWithNone = new CodeStyleOption<bool>(true, NotificationOption.None);
        private static readonly CodeStyleOption<bool> offWithNone = new CodeStyleOption<bool>(false, NotificationOption.None);
        private static readonly CodeStyleOption<bool> onWithSilent = new CodeStyleOption<bool>(true, NotificationOption.Silent);
        private static readonly CodeStyleOption<bool> offWithSilent = new CodeStyleOption<bool>(false, NotificationOption.Silent);
        private static readonly CodeStyleOption<bool> onWithInfo = new CodeStyleOption<bool>(true, NotificationOption.Suggestion);
        private static readonly CodeStyleOption<bool> offWithInfo = new CodeStyleOption<bool>(false, NotificationOption.Suggestion);
        private static readonly CodeStyleOption<bool> onWithWarning = new CodeStyleOption<bool>(true, NotificationOption.Warning);
        private static readonly CodeStyleOption<bool> offWithWarning = new CodeStyleOption<bool>(false, NotificationOption.Warning);
        private static readonly CodeStyleOption<bool> offWithError = new CodeStyleOption<bool>(false, NotificationOption.Error);
        private static readonly CodeStyleOption<bool> onWithError = new CodeStyleOption<bool>(true, NotificationOption.Error);

        public static IDictionary<OptionKey, object> PreferExplicitTypeWithError(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithError),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithError),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithError));

        public static IDictionary<OptionKey, object> PreferImplicitTypeWithError(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithError),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithError),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithError));

        public static IDictionary<OptionKey, object> PreferExplicitTypeWithWarning(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithWarning),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithWarning),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithWarning));

        public static IDictionary<OptionKey, object> PreferImplicitTypeWithWarning(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithWarning),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithWarning),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithWarning));

        public static IDictionary<OptionKey, object> PreferExplicitTypeWithInfo(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithInfo),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithInfo),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo));

        public static IDictionary<OptionKey, object> PreferImplicitTypeWithInfo(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithInfo),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo));

        public static IDictionary<OptionKey, object> PreferExplicitTypeWithSilent(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithSilent),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithSilent),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithSilent));

        public static IDictionary<OptionKey, object> PreferImplicitTypeWithSilent(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithSilent),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithSilent),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithSilent));

        public static IDictionary<OptionKey, object> PreferExplicitTypeWithNone(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithNone),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithNone),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithNone));

        public static IDictionary<OptionKey, object> PreferImplicitTypeWithNone(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithNone),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithNone),
                AbstractCodeActionOrUserDiagnosticTest.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithNone));
    }
}
