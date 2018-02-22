// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo.Presentation
{
    internal partial class QuickInfoPresenter
    {
        private class QuickInfoSource : ForegroundThreadAffinitizedObject, IAsyncQuickInfoSource
        {
            public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
            {
                if (!session.Properties.TryGetProperty<QuickInfoPresenterSession>(s_augmentSessionKey, out var presenterSession))
                {
                    return Task.FromResult<QuickInfoItem>(null);
                }

                session.Properties.RemoveProperty(s_augmentSessionKey);
                //presenterSession.AugmentQuickInfoSession(quickInfoContent, out applicableToSpan);
                return Task.FromResult<QuickInfoItem>(null);
            }

            public void Dispose()
            {
            }
        }
    }
}
