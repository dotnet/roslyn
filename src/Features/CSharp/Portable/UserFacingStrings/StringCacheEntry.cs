// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.UserFacingStrings;

namespace Microsoft.CodeAnalysis.CSharp.UserFacingStrings;

/// <summary>
/// Represents a cached analysis result for a user-facing string.
/// Contains the cache key, analysis result, and expiration information.
/// </summary>
internal readonly struct StringCacheEntry : IEquatable<StringCacheEntry>
{
    private static readonly TimeSpan DefaultCacheLifetime = TimeSpan.FromMinutes(30);

    public StringCacheKey CacheKey { get; }
    public UserFacingStringAnalysis Analysis { get; }
    public DateTime CreatedAt { get; }
    public DateTime ExpiresAt { get; }

    /// <summary>
    /// Gets whether this cache entry has expired based on its expiration time.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Gets the string value from the cache key for convenience.
    /// </summary>
    public string StringValue => CacheKey.StringValue;

    /// <summary>
    /// Gets the basic context from the cache key for convenience.
    /// </summary>
    public string BasicContext => CacheKey.ContextType;

    public StringCacheEntry(string stringValue, string contextType, UserFacingStringAnalysis analysis)
        : this(stringValue, contextType, analysis, DefaultCacheLifetime)
    {
    }

    public StringCacheEntry(string stringValue, string contextType, UserFacingStringAnalysis analysis, TimeSpan cacheLifetime)
    {
        CacheKey = new StringCacheKey(stringValue, contextType);
        Analysis = analysis;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.Add(cacheLifetime);
    }

    public bool Equals(StringCacheEntry other)
    {
        return CacheKey.Equals(other.CacheKey) && CreatedAt == other.CreatedAt;
    }

    public override bool Equals(object? obj)
    {
        return obj is StringCacheEntry other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (CacheKey.GetHashCode() * 397) ^ CreatedAt.GetHashCode();
        }
    }

    public static bool operator ==(StringCacheEntry left, StringCacheEntry right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StringCacheEntry left, StringCacheEntry right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"StringCacheEntry(Key: {CacheKey}, CreatedAt: {CreatedAt}, IsExpired: {IsExpired})";
    }
}
