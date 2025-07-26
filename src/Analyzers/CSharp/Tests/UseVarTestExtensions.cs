// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions;

internal static class UseVarTestExtensions
{
    private static readonly CodeStyleOption2<bool> onWithNone = new(true, NotificationOption2.None);
    private static readonly CodeStyleOption2<bool> offWithNone = new(false, NotificationOption2.None);
    private static readonly CodeStyleOption2<bool> onWithSilent = new(true, NotificationOption2.Silent);
    private static readonly CodeStyleOption2<bool> offWithSilent = new(false, NotificationOption2.Silent);
    private static readonly CodeStyleOption2<bool> onWithInfo = new(true, NotificationOption2.Suggestion);
    private static readonly CodeStyleOption2<bool> offWithInfo = new(false, NotificationOption2.Suggestion);
    private static readonly CodeStyleOption2<bool> onWithWarning = new(true, NotificationOption2.Warning);
    private static readonly CodeStyleOption2<bool> offWithWarning = new(false, NotificationOption2.Warning);
    private static readonly CodeStyleOption2<bool> offWithError = new(false, NotificationOption2.Error);
    private static readonly CodeStyleOption2<bool> onWithError = new(true, NotificationOption2.Error);

    extension(AbstractCodeActionOrUserDiagnosticTest_NoEditor<TestHostDocument, TestHostProject, TestHostSolution, TestWorkspace> test)
    {
        public OptionsCollection PreferExplicitTypeWithError()
            => new(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, offWithError },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithError },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithError },
            };

        public OptionsCollection PreferImplicitTypeWithError()
            => new(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, onWithError },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithError },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithError },
            };

        public OptionsCollection PreferExplicitTypeWithWarning()
            => new(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, offWithWarning },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithWarning },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithWarning },
            };

        public OptionsCollection PreferImplicitTypeWithWarning()
            => new(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, onWithWarning },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithWarning },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithWarning },
            };

        public OptionsCollection PreferExplicitTypeWithInfo()
            => new(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, offWithInfo },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithInfo },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo },
            };

        public OptionsCollection PreferImplicitTypeWithInfo()
            => new(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, onWithInfo },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo },
            };

        public OptionsCollection PreferExplicitTypeWithSilent()
            => new(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, offWithSilent },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithSilent },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithSilent },
            };

        public OptionsCollection PreferImplicitTypeWithSilent()
            => new(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, onWithSilent },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithSilent },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithSilent },
            };

        public OptionsCollection PreferExplicitTypeWithNone()
            => new(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, offWithNone },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithNone },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithNone },
            };

        public OptionsCollection PreferImplicitTypeWithNone()
            => new(test.GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, onWithNone },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithNone },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithNone },
            };
    }
}
