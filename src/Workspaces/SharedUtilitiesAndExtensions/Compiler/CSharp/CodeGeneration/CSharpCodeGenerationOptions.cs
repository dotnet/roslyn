// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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

    internal CSharpCodeGenerationOptions(IOptionsReader options, CSharpCodeGenerationOptions? fallbackOptions)
        : base(options, fallbackOptions ??= Default, LanguageNames.CSharp)
    {
        PreferExpressionBodiedMethods = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, fallbackOptions.PreferExpressionBodiedMethods);
        PreferExpressionBodiedAccessors = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, fallbackOptions.PreferExpressionBodiedAccessors);
        PreferExpressionBodiedProperties = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, fallbackOptions.PreferExpressionBodiedProperties);
        PreferExpressionBodiedIndexers = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, fallbackOptions.PreferExpressionBodiedIndexers);
        PreferExpressionBodiedConstructors = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, fallbackOptions.PreferExpressionBodiedConstructors);
        PreferExpressionBodiedOperators = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedOperators, fallbackOptions.PreferExpressionBodiedOperators);
        PreferExpressionBodiedLocalFunctions = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, fallbackOptions.PreferExpressionBodiedLocalFunctions);
        PreferExpressionBodiedLambdas = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, fallbackOptions.PreferExpressionBodiedLambdas);
        PreferStaticLocalFunction = options.GetOption(CSharpCodeStyleOptions.PreferStaticLocalFunction, fallbackOptions.PreferStaticLocalFunction);
        NamespaceDeclarations = options.GetOption(CSharpCodeStyleOptions.NamespaceDeclarations, fallbackOptions.NamespaceDeclarations);
    }
}
