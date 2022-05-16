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

    [DataMember(Order = BaseMemberCount + 0)] public readonly CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedMethods;
    [DataMember(Order = BaseMemberCount + 1)] public readonly CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedAccessors;
    [DataMember(Order = BaseMemberCount + 2)] public readonly CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedProperties;
    [DataMember(Order = BaseMemberCount + 3)] public readonly CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedIndexers;
    [DataMember(Order = BaseMemberCount + 4)] public readonly CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedConstructors;
    [DataMember(Order = BaseMemberCount + 5)] public readonly CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedOperators;
    [DataMember(Order = BaseMemberCount + 6)] public readonly CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedLocalFunctions;
    [DataMember(Order = BaseMemberCount + 7)] public readonly CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedLambdas;
    [DataMember(Order = BaseMemberCount + 8)] public readonly CodeStyleOption2<bool> PreferStaticLocalFunction;
    [DataMember(Order = BaseMemberCount + 9)] public readonly CodeStyleOption2<NamespaceDeclarationPreference> NamespaceDeclarations;

    public CSharpCodeGenerationOptions(
        CodeStyleOption2<ExpressionBodyPreference>? preferExpressionBodiedMethods = null,
        CodeStyleOption2<ExpressionBodyPreference>? preferExpressionBodiedAccessors = null,
        CodeStyleOption2<ExpressionBodyPreference>? preferExpressionBodiedProperties = null,
        CodeStyleOption2<ExpressionBodyPreference>? preferExpressionBodiedIndexers = null,
        CodeStyleOption2<ExpressionBodyPreference>? preferExpressionBodiedConstructors = null,
        CodeStyleOption2<ExpressionBodyPreference>? preferExpressionBodiedOperators = null,
        CodeStyleOption2<ExpressionBodyPreference>? preferExpressionBodiedLocalFunctions = null,
        CodeStyleOption2<ExpressionBodyPreference>? preferExpressionBodiedLambdas = null,
        CodeStyleOption2<bool>? preferStaticLocalFunction = null,
        CodeStyleOption2<NamespaceDeclarationPreference>? namespaceDeclarations = null)
    {
        PreferExpressionBodiedMethods = preferExpressionBodiedMethods ?? s_neverWithSilentEnforcement;
        PreferExpressionBodiedAccessors = preferExpressionBodiedAccessors ?? s_whenPossibleWithSilentEnforcement;
        PreferExpressionBodiedProperties = preferExpressionBodiedProperties ?? s_whenPossibleWithSilentEnforcement;
        PreferExpressionBodiedIndexers = preferExpressionBodiedIndexers ?? s_whenPossibleWithSilentEnforcement;
        PreferExpressionBodiedConstructors = preferExpressionBodiedConstructors ?? s_neverWithSilentEnforcement;
        PreferExpressionBodiedOperators = preferExpressionBodiedOperators ?? s_neverWithSilentEnforcement;
        PreferExpressionBodiedLocalFunctions = preferExpressionBodiedLocalFunctions ?? s_neverWithSilentEnforcement;
        PreferExpressionBodiedLambdas = preferExpressionBodiedLambdas ?? s_whenPossibleWithSilentEnforcement;
        PreferStaticLocalFunction = preferStaticLocalFunction ?? s_trueWithSuggestionEnforcement;
        NamespaceDeclarations = namespaceDeclarations ?? s_blockedScopedWithSilentEnforcement;
    }

    public static readonly CSharpCodeGenerationOptions Default = new();

    public static CSharpCodeGenerationOptions Create(AnalyzerConfigOptions options, CSharpCodeGenerationOptions? fallbackOptions)
    {
        fallbackOptions ??= Default;

        return new(
            preferExpressionBodiedMethods: options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, fallbackOptions.PreferExpressionBodiedMethods),
            preferExpressionBodiedAccessors: options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, fallbackOptions.PreferExpressionBodiedAccessors),
            preferExpressionBodiedProperties: options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, fallbackOptions.PreferExpressionBodiedProperties),
            preferExpressionBodiedIndexers: options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, fallbackOptions.PreferExpressionBodiedIndexers),
            preferExpressionBodiedConstructors: options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, fallbackOptions.PreferExpressionBodiedConstructors),
            preferExpressionBodiedOperators: options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferExpressionBodiedOperators, fallbackOptions.PreferExpressionBodiedOperators),
            preferExpressionBodiedLocalFunctions: options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, fallbackOptions.PreferExpressionBodiedLocalFunctions),
            preferExpressionBodiedLambdas: options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, fallbackOptions.PreferExpressionBodiedLambdas),
            preferStaticLocalFunction: options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferStaticLocalFunction, fallbackOptions.PreferStaticLocalFunction),
            namespaceDeclarations: options.GetEditorConfigOption(CSharpCodeStyleOptions.NamespaceDeclarations, fallbackOptions.NamespaceDeclarations));
    }

    public override CodeGenerationContextInfo GetInfo(CodeGenerationContext context, ParseOptions parseOptions)
        => new CSharpCodeGenerationContextInfo(context, this, ((CSharpParseOptions)parseOptions).LanguageVersion);
}
