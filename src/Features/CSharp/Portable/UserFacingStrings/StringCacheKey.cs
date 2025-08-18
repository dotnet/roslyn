// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.UserFacingStrings;

/// <summary>
/// Represents a cache key for user-facing string analysis results.
/// Combines string content with basic context to create a stable cache identifier.
/// </summary>
internal readonly struct StringCacheKey : IEquatable<StringCacheKey>
{
    public string StringValue { get; }
    public string ContextType { get; }

    public StringCacheKey(string stringValue, string contextType)
    {
        StringValue = stringValue ?? string.Empty;
        ContextType = contextType ?? string.Empty;
    }

    public bool Equals(StringCacheKey other)
    {
        return StringValue == other.StringValue && ContextType == other.ContextType;
    }

    public override bool Equals(object? obj)
    {
        return obj is StringCacheKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (StringValue.GetHashCode() * 397) ^ ContextType.GetHashCode();
        }
    }

    public static bool operator ==(StringCacheKey left, StringCacheKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StringCacheKey left, StringCacheKey right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"StringCacheKey(Value: {StringValue}, Context: {ContextType})";
    }
}
