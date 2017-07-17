﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IBraceMatchingService
    {
        Task<BraceMatchingResult?> GetMatchingBracesAsync(Document document, int position, CancellationToken cancellationToken = default);
    }

    internal struct BraceMatchingResult
    {
        public TextSpan LeftSpan { get; }
        public TextSpan RightSpan { get; }

        public BraceMatchingResult(TextSpan leftSpan, TextSpan rightSpan)
            : this()
        {
            this.LeftSpan = leftSpan;
            this.RightSpan = rightSpan;
        }
    }
}
