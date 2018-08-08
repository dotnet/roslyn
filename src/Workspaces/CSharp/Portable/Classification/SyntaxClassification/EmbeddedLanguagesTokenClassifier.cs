// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal class EmbeddedLanguagesTokenClassifier : AbstractEmbeddedLanguagesTokenClassifier
    {
        public override ImmutableArray<int> SyntaxTokenKinds { get; } =
            ImmutableArray.Create((int)SyntaxKind.StringLiteralToken, (int)SyntaxKind.InterpolatedStringTextToken);

        public EmbeddedLanguagesTokenClassifier() 
            : base(CSharpEmbeddedLanguagesProvider.Instance)
        {
        }
    }
}
