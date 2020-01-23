// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract class TriviaDataWithList : TriviaData
    {
        public TriviaDataWithList(OptionSet optionSet, string language)
            : base(optionSet, language)
        {
        }

        public abstract List<SyntaxTrivia> GetTriviaList(CancellationToken cancellationToken);
    }
}
