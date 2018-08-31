// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    internal struct EmbeddedBraceMatchingResult
    {
        public TextSpan LeftSpan { get; }
        public TextSpan RightSpan { get; }

        public EmbeddedBraceMatchingResult(TextSpan leftSpan, TextSpan rightSpan)
            : this()
        {
            this.LeftSpan = leftSpan;
            this.RightSpan = rightSpan;
        }
    }
}
