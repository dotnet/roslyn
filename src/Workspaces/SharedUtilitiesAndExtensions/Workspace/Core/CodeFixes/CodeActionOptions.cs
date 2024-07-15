// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.CodeActions;

/// <summary>
/// Options available to code fixes that are supplied by the IDE (i.e. not stored in editorconfig).
/// </summary>
[DataContract]
internal sealed record class CodeActionOptions
{
    public static readonly CodeActionOptions Default = new();
    public static readonly CodeActionOptionsProvider DefaultProvider = Default.CreateProvider();

#if !CODE_STYLE
    [DataMember] public SymbolSearchOptions SearchOptions { get; init; } = SymbolSearchOptions.Default;
    [DataMember] public ImplementTypeOptions ImplementTypeOptions { get; init; } = ImplementTypeOptions.Default;
#endif
    public CodeActionOptionsProvider CreateProvider()
        => new DelegatingCodeActionOptionsProvider(_ => this);
}

internal interface CodeActionOptionsProvider
{
    CodeActionOptions GetOptions(LanguageServices languageService);
}

internal abstract class AbstractCodeActionOptionsProvider : CodeActionOptionsProvider
{
    public abstract CodeActionOptions GetOptions(LanguageServices languageServices);
}

internal sealed class DelegatingCodeActionOptionsProvider(Func<LanguageServices, CodeActionOptions> @delegate) : AbstractCodeActionOptionsProvider
{
    public override CodeActionOptions GetOptions(LanguageServices languageService)
        => @delegate(languageService);
}

internal static class CodeActionOptionsProviders
{
    internal static CodeActionOptionsProvider GetOptionsProvider(this CodeFixContext context)
#if CODE_STYLE
        => CodeActionOptions.DefaultProvider;
#else
        => context.Options;
#endif

#if CODE_STYLE
    internal static CodeActionOptionsProvider GetOptionsProvider(this FixAllContext _)
        => CodeActionOptions.DefaultProvider;
#else
    internal static CodeActionOptionsProvider GetOptionsProvider(this IFixAllContext context)
        => context.State.CodeActionOptionsProvider;
#endif
}
