// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo.Presentation
{
    internal partial class QuickInfoPresenter
    {
        private class QuickInfoSource : ForegroundThreadAffinitizedObject, IAsyncQuickInfoSource
        {
            public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
            {
                //throw new System.NotImplementedException();
                return Task.FromResult<QuickInfoItem>(null);
            }

            public void Dispose()
            {
            }
        }
    }
}
