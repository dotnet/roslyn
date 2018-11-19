// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract class TriviaDataWithList : TriviaData
    {
        public TriviaDataWithList(AnalyzerConfigOptions optionSet)
            : base(optionSet)
        {
        }

        public abstract List<SyntaxTrivia> GetTriviaList(CancellationToken cancellationToken);
    }
}
