// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

[DataContract]
internal sealed record class CSharpCodeGenerationOptions : CodeGenerationOptions
{
    private static readonly CodeStyleOption2<ExpressionBodyPreference> s_neverWithSilentEnforcement =
        new(ExpressionBodyPreference.Never, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<ExpressionBodyPreference> s_whenPossibleWithSilentEnforcement =
        new(ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<NamespaceDeclarationPreference> s_blockedScopedWithSilentEnforcement =
        new(NamespaceDeclarationPreference.BlockScoped, NotificationOption2.Silent);

    public static readonly CSharpCodeGenerationOptions Default = new();

    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedMethods { get; init; } = s_neverWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedAccessors { get; init; } = s_whenPossibleWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedProperties { get; init; } = s_whenPossibleWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedIndexers { get; init; } = s_whenPossibleWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedConstructors { get; init; } = s_neverWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedOperators { get; init; } = s_neverWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedLocalFunctions { get; init; } = s_neverWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedLambdas { get; init; } = s_whenPossibleWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferStaticLocalFunction { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<NamespaceDeclarationPreference> NamespaceDeclarations { get; init; } = s_blockedScopedWithSilentEnforcement;

    public CSharpCodeGenerationOptions()
    {
    }

    internal CSharpCodeGenerationOptions(IOptionsReader options)
        : base(options, LanguageNames.CSharp)
    {
        PreferExpressionBodiedMethods = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods);
        PreferExpressionBodiedAccessors = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors);
        PreferExpressionBodiedProperties = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties);
        PreferExpressionBodiedIndexers = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers);
        PreferExpressionBodiedConstructors = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors);
        PreferExpressionBodiedOperators = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedOperators);
        PreferExpressionBodiedLocalFunctions = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions);
        PreferExpressionBodiedLambdas = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas);
        PreferStaticLocalFunction = options.GetOption(CSharpCodeStyleOptions.PreferStaticLocalFunction);
        NamespaceDeclarations = options.GetOption(CSharpCodeStyleOptions.NamespaceDeclarations);
    }
}
