// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.VisualBasic.CodeStyle;

internal sealed class VisualBasicCodeStyleOptions
{
    private static readonly ImmutableArray<IOption2>.Builder s_allOptionsBuilder = ImmutableArray.CreateBuilder<IOption2>();

    private static Option2<CodeStyleOption2<T>> CreateOption<T>(
        OptionGroup group,
        string name,
        CodeStyleOption2<T> defaultValue,
        Func<CodeStyleOption2<T>, EditorConfigValueSerializer<CodeStyleOption2<T>>>? serializerFactory = null)
    {
        return s_allOptionsBuilder.CreateEditorConfigOption(name, defaultValue, group, LanguageNames.VisualBasic, serializerFactory);
    }

    public static readonly Option2<CodeStyleOption2<string>> PreferredModifierOrder = CreateOption(
        CodeStyleOptionGroups.Modifier,
        "visual_basic_preferred_modifier_order",
        defaultValue: new CodeStyleOption2<string>(
            "Partial,Default,Private,Protected,Public,Friend,NotOverridable,Overridable,MustOverride," +
            "Overloads,Overrides,MustInherit,NotInheritable,Static,Shared,Shadows,ReadOnly,WriteOnly," +
            "Dim,Const,WithEvents,Widening,Narrowing,Custom,Async,Iterator", NotificationOption2.Silent));

    public static readonly Option2<CodeStyleOption2<bool>> PreferIsNotExpression = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "visual_basic_style_prefer_isnot_expression",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferSimplifiedObjectCreation = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "visual_basic_style_prefer_simplified_object_creation",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<UnusedValuePreference>> UnusedValueExpressionStatement = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "visual_basic_style_unused_value_expression_statement_preference",
        defaultValue: new CodeStyleOption2<UnusedValuePreference>(UnusedValuePreference.UnusedLocalVariable, NotificationOption2.Silent),
        CodeStyleHelpers.GetUnusedValuePreferenceSerializer);

    public static readonly Option2<CodeStyleOption2<UnusedValuePreference>> UnusedValueAssignment = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "visual_basic_style_unused_value_assignment_preference",
        defaultValue: new CodeStyleOption2<UnusedValuePreference>(UnusedValuePreference.UnusedLocalVariable, NotificationOption2.Suggestion),
        CodeStyleHelpers.GetUnusedValuePreferenceSerializer);

    public static ImmutableArray<IOption2> EditorConfigOptions => s_allOptionsBuilder.ToImmutable();
}
