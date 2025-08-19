// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UserFacingStrings;

/// <summary>
/// Cache key for string analysis results. Based on string content and basic context type only.
/// Enhanced context is used for AI prompt but not cache key to maximize cache hits.
/// </summary>
internal readonly struct StringCacheKey : IEquatable<StringCacheKey>
{
    public string StringValue { get; }
    public string BasicContext { get; }

    public StringCacheKey(string stringValue, string basicContext)
    {
        StringValue = stringValue;
        BasicContext = basicContext;
    }

    public bool Equals(StringCacheKey other) =>
        StringValue == other.StringValue &&
        BasicContext == other.BasicContext;

    public override bool Equals(object? obj) => obj is StringCacheKey other && Equals(other);

    public override int GetHashCode() => (StringValue?.GetHashCode() ?? 0) ^ (BasicContext?.GetHashCode() ?? 0);

    public static bool operator ==(StringCacheKey left, StringCacheKey right) => left.Equals(right);
    public static bool operator !=(StringCacheKey left, StringCacheKey right) => !left.Equals(right);
}

/// <summary>
/// Cached analysis result with expiration.
/// </summary>
internal readonly struct CachedAnalysisResult
{
    public UserFacingStringAnalysis Analysis { get; }
    public string OriginalString { get; }
    public DateTime CachedAt { get; }

    public CachedAnalysisResult(UserFacingStringAnalysis analysis, string originalString)
    {
        Analysis = analysis;
        OriginalString = originalString;
        CachedAt = DateTime.UtcNow;
    }

    public bool IsExpired => DateTime.UtcNow - CachedAt > TimeSpan.FromMinutes(30);
}

/// <summary>
/// Per-document string cache entry that includes the string content, basic context, and analysis.
/// </summary>
internal readonly struct StringCacheEntry : IEquatable<StringCacheEntry>
{
    public StringCacheKey CacheKey { get; }
    public string StringValue { get; }
    public string BasicContext { get; }
    public UserFacingStringAnalysis Analysis { get; }
    public DateTime CachedAt { get; }

    public StringCacheEntry(string stringValue, string basicContext, UserFacingStringAnalysis analysis)
    {
        StringValue = stringValue;
        BasicContext = basicContext;
        Analysis = analysis;
        CachedAt = DateTime.UtcNow;
        CacheKey = new StringCacheKey(stringValue, basicContext);
    }

    public bool IsExpired => DateTime.UtcNow - CachedAt > TimeSpan.FromMinutes(30);

    public bool Equals(StringCacheEntry other) => CacheKey.Equals(other.CacheKey);

    public override bool Equals(object? obj) => obj is StringCacheEntry other && Equals(other);

    public override int GetHashCode() => CacheKey.GetHashCode();

    public static bool operator ==(StringCacheEntry left, StringCacheEntry right) => left.Equals(right);
    public static bool operator !=(StringCacheEntry left, StringCacheEntry right) => !left.Equals(right);
}

/// <summary>
/// Request for batch AI analysis of multiple strings.
/// </summary>
internal readonly struct BatchAnalysisRequest
{
    public DocumentId DocumentId { get; }
    public VersionStamp Version { get; }
    public ImmutableArray<PendingStringAnalysis> PendingStrings { get; }
    public DateTime RequestedAt { get; }

    public BatchAnalysisRequest(DocumentId documentId, VersionStamp version, ImmutableArray<PendingStringAnalysis> pendingStrings)
    {
        DocumentId = documentId;
        Version = version;
        PendingStrings = pendingStrings;
        RequestedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// String awaiting AI analysis with enhanced context for the AI prompt.
/// </summary>
internal readonly struct PendingStringAnalysis
{
    public StringCacheKey CacheKey { get; }
    public string StringValue { get; }
    public string BasicContext { get; }
    public string EnhancedContext { get; }
    public TextSpan Location { get; }

    public PendingStringAnalysis(string stringValue, string basicContext, string enhancedContext, TextSpan location)
    {
        StringValue = stringValue;
        BasicContext = basicContext;
        EnhancedContext = enhancedContext;
        Location = location;
        CacheKey = new StringCacheKey(stringValue, basicContext);
    }
}

/// <summary>
/// Request for document analysis.
/// </summary>
internal readonly record struct AnalysisRequest(
    DocumentId DocumentId,
    VersionStamp Version,
    DateTime RequestedAt);

/// <summary>
/// State tracking for analyzed documents.
/// </summary>
internal readonly struct DocumentWatchState
{
    public VersionStamp LastAnalyzedVersion { get; }
    public ImmutableHashSet<StringCacheKey> AnalyzedStrings { get; }
    public DateTime LastAnalyzedAt { get; }

    public DocumentWatchState(VersionStamp version, ImmutableHashSet<StringCacheKey> analyzedStrings)
    {
        LastAnalyzedVersion = version;
        AnalyzedStrings = analyzedStrings;
        LastAnalyzedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Candidate string for analysis.
/// </summary>
internal readonly struct StringAnalysisCandidate
{
    public StringCacheKey CacheKey { get; }
    public string Value { get; }
    public string Context { get; }
    public TextSpan Location { get; }

    public StringAnalysisCandidate(string value, string context, TextSpan location)
    {
        Value = value;
        Context = context;
        Location = location;
        CacheKey = new StringCacheKey(value, context);
    }
}
