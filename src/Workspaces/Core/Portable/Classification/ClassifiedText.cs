// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Classification
{
    internal readonly struct ClassifiedText(string classificationType, string text)
    {
        public string ClassificationType { get; } = classificationType;
        public string Text { get; } = text;
    }
}
