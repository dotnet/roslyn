// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.Classification.Classifiers;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    internal static class SyntaxClassifier
    {
        public static readonly IEnumerable<ISyntaxClassifier> DefaultSyntaxClassifiers =
            ImmutableArray.Create<ISyntaxClassifier>(
                new NameSyntaxClassifier(),
                new SyntaxTokenClassifier(),
                new UsingDirectiveSyntaxClassifier());
    }
}
