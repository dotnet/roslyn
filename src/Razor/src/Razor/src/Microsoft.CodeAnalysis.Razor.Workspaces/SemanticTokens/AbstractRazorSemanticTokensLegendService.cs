// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.SemanticTokens;

internal abstract class AbstractRazorSemanticTokensLegendService(IClientCapabilitiesService clientCapabilitiesService) : ISemanticTokensLegendService
{
    private static readonly SemanticTokenModifiers s_modifiers = ConstructTokenModifiers();

    // DI calls this constructor to build the service container, but we can't access clientCapabilitiesService
    // until the language server has received the Initialize message, so we have to do this lazily.
    private readonly Lazy<SemanticTokenTypes> _typesLazy = new(() => ConstructTokenTypes(clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions));

    public SemanticTokenTypes TokenTypes => _typesLazy.Value;
    public SemanticTokenModifiers TokenModifiers { get; } = s_modifiers;

    private static SemanticTokenTypes ConstructTokenTypes(bool supportsVsExtensions)
    {
        using var _ = ArrayBuilderPool<string>.GetPooledObject(out var builder);

        builder.AddRange(RazorSemanticTokensAccessor.GetTokenTypes(supportsVsExtensions));

        foreach (var razorTokenType in GetStaticFieldValues(typeof(SemanticTokenTypes)))
        {
            builder.Add(razorTokenType);
        }

        return new SemanticTokenTypes(builder.ToArray());
    }

    private static SemanticTokenModifiers ConstructTokenModifiers()
    {
        using var _ = ArrayBuilderPool<string>.GetPooledObject(out var builder);

        builder.AddRange(RazorSemanticTokensAccessor.GetTokenModifiers());

        foreach (var razorModifier in GetStaticFieldValues(typeof(SemanticTokenModifiers)))
        {
            builder.Add(razorModifier);
        }

        return new SemanticTokenModifiers(builder.ToArray());
    }

    private static ImmutableArray<string> GetStaticFieldValues(Type type)
    {
        using var _ = ArrayBuilderPool<string>.GetPooledObject(out var builder);

        foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (field.GetValue(null) is string value)
            {
                builder.Add(value);
            }
        }

        return builder.ToImmutable();
    }
}
