// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.UseExplicitOrImplicitType
{
    public abstract class AbstractUseTypeRefactoringTests : AbstractCSharpCodeActionTest
    {
        private readonly CodeStyleOption<bool> onWithNone = new CodeStyleOption<bool>(true, NotificationOption.None);
        private readonly CodeStyleOption<bool> offWithNone = new CodeStyleOption<bool>(false, NotificationOption.None);
        private readonly CodeStyleOption<bool> onWithSilent = new CodeStyleOption<bool>(true, NotificationOption.Silent);
        private readonly CodeStyleOption<bool> offWithSilent = new CodeStyleOption<bool>(false, NotificationOption.Silent);
        private readonly CodeStyleOption<bool> onWithInfo = new CodeStyleOption<bool>(true, NotificationOption.Suggestion);
        private readonly CodeStyleOption<bool> offWithInfo = new CodeStyleOption<bool>(false, NotificationOption.Suggestion);
        private readonly CodeStyleOption<bool> onWithWarning = new CodeStyleOption<bool>(true, NotificationOption.Warning);
        private readonly CodeStyleOption<bool> offWithWarning = new CodeStyleOption<bool>(false, NotificationOption.Warning);
        private readonly CodeStyleOption<bool> offWithError = new CodeStyleOption<bool>(false, NotificationOption.Error);
        private readonly CodeStyleOption<bool> onWithError = new CodeStyleOption<bool>(true, NotificationOption.Error);

        protected IDictionary<OptionKey, object> PreferExplicitTypeWithError() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithError),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithError),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithError));

        protected IDictionary<OptionKey, object> PreferImplicitTypeWithError() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithError),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithError),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithError));

        protected IDictionary<OptionKey, object> PreferExplicitTypeWithWarning() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithWarning),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithWarning),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithWarning));

        protected IDictionary<OptionKey, object> PreferImplicitTypeWithWarning() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithWarning),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithWarning),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithWarning));

        protected IDictionary<OptionKey, object> PreferExplicitTypeWithInfo() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo));

        protected IDictionary<OptionKey, object> PreferImplicitTypeWithInfo() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo));

        protected IDictionary<OptionKey, object> PreferExplicitTypeWithSilent() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithSilent),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithSilent),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithSilent));

        protected IDictionary<OptionKey, object> PreferImplicitTypeWithSilent() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithSilent),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithSilent),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithSilent));

        protected IDictionary<OptionKey, object> PreferExplicitTypeWithNone() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithNone),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithNone),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithNone));

        protected IDictionary<OptionKey, object> PreferImplicitTypeWithNone() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithNone),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithNone),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithNone));
    }
}
