// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;

#if CODE_STYLE
using AbstractCodeActionOrUserDiagnosticTest = Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest;
#else
using static Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.AbstractCodeActionOrUserDiagnosticTest;
#endif

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions
{
    internal static class UseVarTestExtensions
    {
        private static readonly CodeStyleOption2<bool> onWithNone = new CodeStyleOption2<bool>(true, NotificationOption2.None);
        private static readonly CodeStyleOption2<bool> offWithNone = new CodeStyleOption2<bool>(false, NotificationOption2.None);
        private static readonly CodeStyleOption2<bool> onWithSilent = new CodeStyleOption2<bool>(true, NotificationOption2.Silent);
        private static readonly CodeStyleOption2<bool> offWithSilent = new CodeStyleOption2<bool>(false, NotificationOption2.Silent);
        private static readonly CodeStyleOption2<bool> onWithInfo = new CodeStyleOption2<bool>(true, NotificationOption2.Suggestion);
        private static readonly CodeStyleOption2<bool> offWithInfo = new CodeStyleOption2<bool>(false, NotificationOption2.Suggestion);
        private static readonly CodeStyleOption2<bool> onWithWarning = new CodeStyleOption2<bool>(true, NotificationOption2.Warning);
        private static readonly CodeStyleOption2<bool> offWithWarning = new CodeStyleOption2<bool>(false, NotificationOption2.Warning);
        private static readonly CodeStyleOption2<bool> offWithError = new CodeStyleOption2<bool>(false, NotificationOption2.Error);
        private static readonly CodeStyleOption2<bool> onWithError = new CodeStyleOption2<bool>(true, NotificationOption2.Error);

        public static IOptionsCollection PreferExplicitTypeWithError(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithError),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithError),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithError));

        public static IOptionsCollection PreferImplicitTypeWithError(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithError),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithError),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithError));

        public static IOptionsCollection PreferExplicitTypeWithWarning(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithWarning),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithWarning),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithWarning));

        public static IOptionsCollection PreferImplicitTypeWithWarning(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithWarning),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithWarning),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithWarning));

        public static IOptionsCollection PreferExplicitTypeWithInfo(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithInfo),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithInfo),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo));

        public static IOptionsCollection PreferImplicitTypeWithInfo(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithInfo),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo));

        public static IOptionsCollection PreferExplicitTypeWithSilent(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithSilent),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithSilent),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithSilent));

        public static IOptionsCollection PreferImplicitTypeWithSilent(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithSilent),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithSilent),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithSilent));

        public static IOptionsCollection PreferExplicitTypeWithNone(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithNone),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithNone),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithNone));

        public static IOptionsCollection PreferImplicitTypeWithNone(this AbstractCodeActionOrUserDiagnosticTest test)
            => AbstractCodeActionOrUserDiagnosticTest.OptionsSet(
                test.SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithNone),
                test.SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithNone),
                test.SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithNone));
    }
}
