// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithError),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithError),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithError));

        public static IDictionary<OptionKey, object> PreferImplicitTypeWithError(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithError),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithError),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithError));

        public static IDictionary<OptionKey, object> PreferExplicitTypeWithWarning(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithWarning),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithWarning),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithWarning));

        public static IDictionary<OptionKey, object> PreferImplicitTypeWithWarning(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithWarning),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithWarning),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithWarning));

        public static IDictionary<OptionKey, object> PreferExplicitTypeWithInfo(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithInfo),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithInfo),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo));

        public static IDictionary<OptionKey, object> PreferImplicitTypeWithInfo(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithInfo),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo));

        public static IDictionary<OptionKey, object> PreferExplicitTypeWithSilent(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithSilent),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithSilent),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithSilent));

        public static IDictionary<OptionKey, object> PreferImplicitTypeWithSilent(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithSilent),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithSilent),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithSilent));

        public static IDictionary<OptionKey, object> PreferExplicitTypeWithNone(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithNone),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithNone),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithNone));

        public static IDictionary<OptionKey, object> PreferImplicitTypeWithNone(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithNone),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithNone),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithNone));
    }
}
