// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal abstract class QuickInfoService : ILanguageService
    {
        public abstract Task<QuickInfoData> GetQuickInfoAsync(Document document, int position, CancellationToken cancellationToken);
    }
}