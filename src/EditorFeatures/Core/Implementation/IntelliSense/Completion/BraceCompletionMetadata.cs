// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal class BraceCompletionMetadata
    {
        public IEnumerable<char> OpeningBraces { get; }
        public IEnumerable<char> ClosingBraces { get; }
        public IEnumerable<string> ContentTypes { get; }

        public BraceCompletionMetadata(IReadOnlyDictionary<string, object> data)
        {
            OpeningBraces = data.GetEnumerableMetadata<char>(nameof(OpeningBraces));
            ClosingBraces = data.GetEnumerableMetadata<char>(nameof(ClosingBraces));
            ContentTypes = data.GetEnumerableMetadata<string>(nameof(ContentTypes));
        }
    }
}
