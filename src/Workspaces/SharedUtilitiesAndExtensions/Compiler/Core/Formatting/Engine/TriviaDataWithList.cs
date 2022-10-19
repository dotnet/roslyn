// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract class TriviaDataWithList : TriviaData
    {
        public TriviaDataWithList(SyntaxFormattingOptions options, string language)
            : base(options, language)
        {
        }

        public abstract SyntaxTriviaList GetTriviaList(CancellationToken cancellationToken);
    }
}
