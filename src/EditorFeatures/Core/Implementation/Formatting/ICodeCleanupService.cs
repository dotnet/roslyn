// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting
{
    interface ICodeCleanupService
    {
        Document CleanupDocument(Document document, CancellationToken cancellationToken);
    }
}
