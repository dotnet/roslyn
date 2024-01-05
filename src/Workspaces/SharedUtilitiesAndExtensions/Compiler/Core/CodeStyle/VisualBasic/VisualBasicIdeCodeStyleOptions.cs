// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.VisualBasic.CodeStyle;

[DataContract]
internal sealed record class VisualBasicIdeCodeStyleOptions : IdeCodeStyleOptions, IEquatable<VisualBasicIdeCodeStyleOptions>
{
    private static readonly CodeStyleOption2<UnusedValuePreference> s_unusedLocalVariableWithSilentEnforcement =
        new(UnusedValuePreference.UnusedLocalVariable, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<UnusedValuePreference> s_unusedLocalVariableWithSuggestionEnforcement =
        new(UnusedValuePreference.UnusedLocalVariable, NotificationOption2.Suggestion);

    private static readonly CodeStyleOption2<string> s_defaultModifierOrder =
        new("Partial,Default,Private,Protected,Public,Friend,NotOverridable,Overridable,MustOverride," +
            "Overloads,Overrides,MustInherit,NotInheritable,Static,Shared,Shadows,ReadOnly,WriteOnly," +
            "Dim,Const,WithEvents,Widening,Narrowing,Custom,Async,Iterator", NotificationOption2.Silent);

    public static readonly VisualBasicIdeCodeStyleOptions Default = new();

    [DataMember] public CodeStyleOption2<string> PreferredModifierOrder { get; init; } = s_defaultModifierOrder;
    [DataMember] public CodeStyleOption2<bool> PreferIsNotExpression { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferSimplifiedObjectCreation { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<UnusedValuePreference> UnusedValueExpressionStatement { get; init; } = s_unusedLocalVariableWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<UnusedValuePreference> UnusedValueAssignment { get; init; } = s_unusedLocalVariableWithSuggestionEnforcement;

    public VisualBasicIdeCodeStyleOptions()
        : base()
    {
    }

    internal VisualBasicIdeCodeStyleOptions(IOptionsReader options, VisualBasicIdeCodeStyleOptions? fallbackOptions)
        : base(options, fallbackOptions ??= Default, LanguageNames.VisualBasic)
    {
        PreferredModifierOrder = options.GetOption(VisualBasicCodeStyleOptions.PreferredModifierOrder, fallbackOptions.PreferredModifierOrder);
        PreferIsNotExpression = options.GetOption(VisualBasicCodeStyleOptions.PreferIsNotExpression, fallbackOptions.PreferIsNotExpression);
        PreferSimplifiedObjectCreation = options.GetOption(VisualBasicCodeStyleOptions.PreferSimplifiedObjectCreation, fallbackOptions.PreferSimplifiedObjectCreation);
        UnusedValueExpressionStatement = options.GetOption(VisualBasicCodeStyleOptions.UnusedValueExpressionStatement, fallbackOptions.UnusedValueExpressionStatement);
        UnusedValueAssignment = options.GetOption(VisualBasicCodeStyleOptions.UnusedValueAssignment, fallbackOptions.UnusedValueAssignment);
    }
}
