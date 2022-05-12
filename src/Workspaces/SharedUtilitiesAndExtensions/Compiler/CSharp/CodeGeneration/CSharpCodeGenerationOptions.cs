// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

[DataContract]
internal sealed class CSharpCodeGenerationOptions : CodeGenerationOptions
{
    private static readonly CodeStyleOption2<ExpressionBodyPreference> s_neverWithSilentEnforcement =
        new(ExpressionBodyPreference.Never, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<ExpressionBodyPreference> s_whenPossibleWithSilentEnforcement =
        new(ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<NamespaceDeclarationPreference> s_blockedScopedWithSilentEnforcement =
        new(NamespaceDeclarationPreference.BlockScoped, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<bool> s_trueWithSuggestionEnforcement =
        new(value: true, notification: NotificationOption2.Suggestion);

    public static readonly CSharpCodeGenerationOptions Default = new();

    [DataMember(Order = BaseMemberCount + 0)] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedMethods { get; init; } = s_neverWithSilentEnforcement;
    [DataMember(Order = BaseMemberCount + 1)] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedAccessors { get; init; } = s_whenPossibleWithSilentEnforcement;
    [DataMember(Order = BaseMemberCount + 2)] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedProperties { get; init; } = s_whenPossibleWithSilentEnforcement;
    [DataMember(Order = BaseMemberCount + 3)] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedIndexers { get; init; } = s_whenPossibleWithSilentEnforcement;
    [DataMember(Order = BaseMemberCount + 4)] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedConstructors { get; init; } = s_neverWithSilentEnforcement;
    [DataMember(Order = BaseMemberCount + 5)] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedOperators { get; init; } = s_neverWithSilentEnforcement;
    [DataMember(Order = BaseMemberCount + 6)] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedLocalFunctions { get; init; } = s_neverWithSilentEnforcement;
    [DataMember(Order = BaseMemberCount + 7)] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedLambdas { get; init; } = s_whenPossibleWithSilentEnforcement;
    [DataMember(Order = BaseMemberCount + 8)] public CodeStyleOption2<bool> PreferStaticLocalFunction { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember(Order = BaseMemberCount + 9)] public CodeStyleOption2<NamespaceDeclarationPreference> NamespaceDeclarations { get; init; } = s_blockedScopedWithSilentEnforcement;

#if !CODE_STYLE
    public override CodeGenerationContextInfo GetInfo(CodeGenerationContext context, ParseOptions parseOptions)
        => new CSharpCodeGenerationContextInfo(context, this, ((CSharpParseOptions)parseOptions).LanguageVersion);
#endif
}

internal static class CSharpCodeGenerationOptionsProviders
{
    public static CSharpCodeGenerationOptions GetCSharpCodeGenerationOptions(this AnalyzerConfigOptions options, CSharpCodeGenerationOptions? fallbackOptions)
    {
        fallbackOptions ??= CSharpCodeGenerationOptions.Default;

        return new()
        {
            Common = options.GetCommonCodeGenerationOptions(fallbackOptions.Common),
            PreferExpressionBodiedMethods = options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, fallbackOptions.PreferExpressionBodiedMethods),
            PreferExpressionBodiedAccessors = options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, fallbackOptions.PreferExpressionBodiedAccessors),
            PreferExpressionBodiedProperties = options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, fallbackOptions.PreferExpressionBodiedProperties),
            PreferExpressionBodiedIndexers = options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, fallbackOptions.PreferExpressionBodiedIndexers),
            PreferExpressionBodiedConstructors = options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, fallbackOptions.PreferExpressionBodiedConstructors),
            PreferExpressionBodiedOperators = options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferExpressionBodiedOperators, fallbackOptions.PreferExpressionBodiedOperators),
            PreferExpressionBodiedLocalFunctions = options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, fallbackOptions.PreferExpressionBodiedLocalFunctions),
            PreferExpressionBodiedLambdas = options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, fallbackOptions.PreferExpressionBodiedLambdas),
            PreferStaticLocalFunction = options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferStaticLocalFunction, fallbackOptions.PreferStaticLocalFunction),
            NamespaceDeclarations = options.GetEditorConfigOption(CSharpCodeStyleOptions.NamespaceDeclarations, fallbackOptions.NamespaceDeclarations)
        };
    }
}
