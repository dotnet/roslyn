// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Preserves compatibility for missing generated contracts while reporting each reflection fallback.
/// Tests and their child processes require generated metadata instead.
/// </summary>
internal sealed class ReflectionFallbackJsonTypeInfoResolver(
    bool? requireGeneratedMetadata = null,
    Action<Type>? reportFallback = null) : IJsonTypeInfoResolver
{
    internal const string RequireGeneratedMetadataEnvironmentVariable = "ROSLYN_REQUIRE_GENERATED_JSON_METADATA";

    internal static ReflectionFallbackJsonTypeInfoResolver Instance { get; } = new();

    private static readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> s_generatedOptions = new();
    private readonly Lazy<DefaultJsonTypeInfoResolver> _reflectionResolver = new(static () => new());
    private readonly ConcurrentDictionary<Type, byte> _reportedTypes = new();
    private readonly Action<Type> _reportFallback = reportFallback ?? ReportFallback;

    internal bool IsReflectionResolverCreated => _reflectionResolver.IsValueCreated;

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        if (requireGeneratedMetadata ??
            Environment.GetEnvironmentVariable(RequireGeneratedMetadataEnvironmentVariable) == "1")
        {
            throw CreateMissingMetadataException(type);
        }

        if (_reportedTypes.TryAdd(type, 0))
            _reportFallback(type);

        return _reflectionResolver.Value.GetTypeInfo(type, options);
    }

    internal static bool HasMetadataWithoutFallback(Type type, JsonSerializerOptions options)
    {
        // Cache the generated-only probe without rebuilding metadata or invoking the fallback on each dictionary write.
        var generatedOptions = s_generatedOptions.GetValue(options, static originalOptions =>
        {
            var copy = new JsonSerializerOptions(originalOptions);
            for (var i = copy.TypeInfoResolverChain.Count - 1; i >= 0; i--)
            {
                if (copy.TypeInfoResolverChain[i] is ReflectionFallbackJsonTypeInfoResolver)
                    copy.TypeInfoResolverChain.RemoveAt(i);
            }

            copy.MakeReadOnly();
            return copy;
        });
        return generatedOptions.TryGetTypeInfo(type, out _);
    }

    private static void ReportFallback(Type type)
        => FatalError.ReportNonFatalError(CreateMissingMetadataException(type), ErrorSeverity.Diagnostic);

    private static NotSupportedException CreateMissingMetadataException(Type type)
        => new($"Generated Roslyn/Razor JSON metadata is missing for '{type}'. Register the contract in its owning JsonSerializerContext and ensure the host composes its resolver.");
}
