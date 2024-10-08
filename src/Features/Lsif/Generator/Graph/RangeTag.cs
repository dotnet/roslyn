// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    internal abstract class RangeTag(string type, string text)
    {
        public string Type { get; } = type;
        public string Text { get; } = text;
    }
}
