// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    internal interface IOutliningService : ILanguageService
    {
        Task<IList<OutliningSpan>> GetOutliningSpansAsync(Document document, CancellationToken cancellationToken);
    }

    internal interface ISynchronousOutliningService : IOutliningService
    {
        IList<OutliningSpan> GetOutliningSpans(Document document, CancellationToken cancellationToken);
    }
}
