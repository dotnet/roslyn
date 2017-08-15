﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface ILineSeparatorService : ILanguageService
    {
        Task<IEnumerable<TextSpan>> GetLineSeparatorsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken = default);
    }
}
