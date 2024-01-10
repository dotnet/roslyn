// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;

#if CODE_STYLE
using AbstractCodeActionOrUserDiagnosticTest = Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor;
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

#if false

        public static OptionsCollection PreferExplicitTypeWithError(this AbstractCodeActionOrUserDiagnosticTest test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, offWithError },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithError },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithError },
            };

        public static OptionsCollection PreferImplicitTypeWithError(this AbstractCodeActionOrUserDiagnosticTest test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, onWithError },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithError },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithError },
            };

        public static OptionsCollection PreferExplicitTypeWithWarning(this AbstractCodeActionOrUserDiagnosticTest test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, offWithWarning },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithWarning },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithWarning },
            };

        public static OptionsCollection PreferImplicitTypeWithWarning(this AbstractCodeActionOrUserDiagnosticTest test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, onWithWarning },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithWarning },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithWarning },
            };

        public static OptionsCollection PreferExplicitTypeWithInfo(this AbstractCodeActionOrUserDiagnosticTest test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, offWithInfo },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithInfo },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo },
            };

        public static OptionsCollection PreferImplicitTypeWithInfo(this AbstractCodeActionOrUserDiagnosticTest test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, onWithInfo },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo },
            };

        public static OptionsCollection PreferExplicitTypeWithSilent(this AbstractCodeActionOrUserDiagnosticTest test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, offWithSilent },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithSilent },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithSilent },
            };

        public static OptionsCollection PreferImplicitTypeWithSilent(this AbstractCodeActionOrUserDiagnosticTest test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, onWithSilent },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithSilent },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithSilent },
            };

        public static OptionsCollection PreferExplicitTypeWithNone(this AbstractCodeActionOrUserDiagnosticTest test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, offWithNone },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithNone },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithNone },
            };

        public static OptionsCollection PreferImplicitTypeWithNone(this AbstractCodeActionOrUserDiagnosticTest test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, onWithNone },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithNone },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithNone },
            };

#endif

        public static OptionsCollection PreferExplicitTypeWithError(this AbstractCodeActionOrUserDiagnosticTest_NoEditor test)
            => new OptionsCollection(test.GetLanguage())
            {
                        { CSharpCodeStyleOptions.VarElsewhere, offWithError },
                        { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithError },
                        { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithError },
            };

        public static OptionsCollection PreferImplicitTypeWithError(this AbstractCodeActionOrUserDiagnosticTest_NoEditor test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, onWithError },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithError },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithError },
            };

        public static OptionsCollection PreferExplicitTypeWithWarning(this AbstractCodeActionOrUserDiagnosticTest_NoEditor test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, offWithWarning },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithWarning },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithWarning },
            };

        public static OptionsCollection PreferImplicitTypeWithWarning(this AbstractCodeActionOrUserDiagnosticTest_NoEditor test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, onWithWarning },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithWarning },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithWarning },
            };

        public static OptionsCollection PreferExplicitTypeWithInfo(this AbstractCodeActionOrUserDiagnosticTest_NoEditor test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, offWithInfo },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithInfo },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo },
            };

        public static OptionsCollection PreferImplicitTypeWithInfo(this AbstractCodeActionOrUserDiagnosticTest_NoEditor test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, onWithInfo },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo },
            };

        public static OptionsCollection PreferExplicitTypeWithSilent(this AbstractCodeActionOrUserDiagnosticTest_NoEditor test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, offWithSilent },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithSilent },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithSilent },
            };

        public static OptionsCollection PreferImplicitTypeWithSilent(this AbstractCodeActionOrUserDiagnosticTest_NoEditor test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, onWithSilent },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithSilent },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithSilent },
            };

        public static OptionsCollection PreferExplicitTypeWithNone(this AbstractCodeActionOrUserDiagnosticTest_NoEditor test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, offWithNone },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithNone },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithNone },
            };

        public static OptionsCollection PreferImplicitTypeWithNone(this AbstractCodeActionOrUserDiagnosticTest_NoEditor test)
            => new OptionsCollection(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, onWithNone },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithNone },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithNone },
            };
    }
}
