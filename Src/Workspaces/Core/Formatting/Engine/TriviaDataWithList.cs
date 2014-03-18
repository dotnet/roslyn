// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract class TriviaDataWithList<T> : TriviaData
    {
        public TriviaDataWithList(OptionSet optionSet, string language)
            : base(optionSet, language)
        {
        }

        public abstract List<T> GetTriviaList(CancellationToken cancellationToken);
    }
}