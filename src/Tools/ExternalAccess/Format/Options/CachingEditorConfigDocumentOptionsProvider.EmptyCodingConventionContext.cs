// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.ExternalAccess.Format.Options
{
    internal sealed partial class CachingEditorConfigDocumentOptionsProvider
    {
        private class EmptyCodingConventionContext : ICodingConventionContext
        {
            public static ICodingConventionContext Instance { get; } = new EmptyCodingConventionContext();

            public ICodingConventionsSnapshot CurrentConventions { get; } = null;

            event CodingConventionsChangedAsyncEventHandler ICodingConventionContext.CodingConventionsChangedAsync
            {
                add { }
                remove { }
            }

            public void Dispose() { }

            public Task WriteConventionValueAsync(string conventionName, string conventionValue, CancellationToken cancellationToken)
                => Task.CompletedTask;
        }
    }
}
