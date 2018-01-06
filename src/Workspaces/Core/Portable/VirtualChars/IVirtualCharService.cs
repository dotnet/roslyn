// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.VirtualChars
{
    /// <summary>
    /// Helper service that takes the raw text of a string token and produces the individual
    /// characters that raw string token represents (i.e. with escapes collapsed).  The difference
    /// between this and the result from token.ValueText is that for each collapsed character returned
    /// the original span of text in the original token can be found.  i.e. if you had the following
    /// in C#:
    /// 
    /// "G\u006fo"
    /// 
    /// Then you'd get back:
    /// 
    /// 'G' -> [0, 1)
    /// 'o' -> [1, 7)
    /// 'o' -> [7, 1)
    /// 
    /// This allows for regex processing that can refer back to the users' original code instead of
    /// the escaped value we're processing.
    /// 
    /// </summary>
    internal interface IVirtualCharService : ILanguageService
    {
        ImmutableArray<VirtualChar> TryConvertToVirtualChars(SyntaxToken token);
    }
}
