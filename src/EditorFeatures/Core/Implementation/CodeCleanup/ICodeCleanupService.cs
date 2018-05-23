// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CodeCleanup
{
    internal interface ICodeCleanupService
    {
        Task<IEnumerable<TextChange>> GetChangesForCleanupDocument(Document document, CancellationToken cancellationToken);
    }
}
