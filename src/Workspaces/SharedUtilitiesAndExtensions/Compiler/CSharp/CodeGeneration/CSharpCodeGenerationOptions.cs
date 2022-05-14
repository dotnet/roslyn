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
internal sealed class CSharpCodeGenerationOptions : CodeGenerationOptions, IEquatable<CSharpCodeGenerationOptions>
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

    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedMethods { get; init; } = s_neverWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedAccessors { get; init; } = s_whenPossibleWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedProperties { get; init; } = s_whenPossibleWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedIndexers { get; init; } = s_whenPossibleWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedConstructors { get; init; } = s_neverWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedOperators { get; init; } = s_neverWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedLocalFunctions { get; init; } = s_neverWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<ExpressionBodyPreference> PreferExpressionBodiedLambdas { get; init; } = s_whenPossibleWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferStaticLocalFunction { get; init; } = s_trueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<NamespaceDeclarationPreference> NamespaceDeclarations { get; init; } = s_blockedScopedWithSilentEnforcement;

    public override bool Equals(object? obj)
        => Equals(obj as CSharpCodeGenerationOptions);

    public bool Equals(CSharpCodeGenerationOptions? other)
        => other is not null &&
           Common.Equals(other.Common) &&
           PreferExpressionBodiedMethods.Equals(other.PreferExpressionBodiedMethods) &&
           PreferExpressionBodiedAccessors.Equals(other.PreferExpressionBodiedAccessors) &&
           PreferExpressionBodiedProperties.Equals(other.PreferExpressionBodiedProperties) &&
           PreferExpressionBodiedIndexers.Equals(other.PreferExpressionBodiedIndexers) &&
           PreferExpressionBodiedConstructors.Equals(other.PreferExpressionBodiedConstructors) &&
           PreferExpressionBodiedOperators.Equals(other.PreferExpressionBodiedOperators) &&
           PreferExpressionBodiedLocalFunctions.Equals(other.PreferExpressionBodiedLocalFunctions) &&
           PreferExpressionBodiedLambdas.Equals(other.PreferExpressionBodiedLambdas) &&
           PreferStaticLocalFunction.Equals(other.PreferStaticLocalFunction) &&
           NamespaceDeclarations.Equals(other.NamespaceDeclarations);

    public override int GetHashCode()
        => Hash.Combine(Common,
           Hash.Combine(PreferExpressionBodiedMethods,
           Hash.Combine(PreferExpressionBodiedAccessors,
           Hash.Combine(PreferExpressionBodiedProperties,
           Hash.Combine(PreferExpressionBodiedIndexers,
           Hash.Combine(PreferExpressionBodiedConstructors,
           Hash.Combine(PreferExpressionBodiedOperators,
           Hash.Combine(PreferExpressionBodiedLocalFunctions,
           Hash.Combine(PreferExpressionBodiedLambdas,
           Hash.Combine(PreferStaticLocalFunction,
           Hash.Combine(NamespaceDeclarations, 0)))))))))));

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
