// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UserFacingStrings;

/// <summary>
/// Represents a string literal pending AI analysis with both basic and enhanced context.
/// </summary>
internal readonly struct PendingStringAnalysis
{
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
    }

    /// <summary>
    /// Gets the cache key using basic context for stable caching.
    /// </summary>
    public StringCacheKey CacheKey => new(StringValue, BasicContext);
}
