// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Classification
{
    internal struct ClassifiedText
    {
        public string ClassificationType { get; }
        public string Text { get; }

        public ClassifiedText(string classificationType, string text)
        {
            ClassificationType = classificationType;
            Text = text;
        }
    }
}
