// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator
{
    internal partial class WpfBackgroundWorkIndicatorFactory
    {
        private class BackgroundWorkIndicatorScope : IUIThreadOperationScope
        {
            private readonly BackgroundWorkOperationScope _backgroundScope;
            private readonly BackgroundWorkIndicatorContext _backgroundWorkIndicatorContext;

            public BackgroundWorkIndicatorScope(
                BackgroundWorkOperationScope backgroundScope,
                string description,
                BackgroundWorkIndicatorContext backgroundWorkIndicatorContext)
            {
                _backgroundScope = backgroundScope;
                _description = description;
                _backgroundWorkIndicatorContext = backgroundWorkIndicatorContext;
            }

            public bool AllowCancellation { get; set; }

            private string _description;
            public string Description
            {
                get => _description;
                set
                {
                    _backgroundScope.Description = value;
                    _description = value;
                }
            }

            public IUIThreadOperationContext Context => _backgroundWorkIndicatorContext;

            public IProgress<ProgressInfo> Progress { get; } = new Progress<ProgressInfo>();

            public void Dispose()
            {
                _backgroundWorkIndicatorContext.RemoveScope(this);
                _backgroundScope.Dispose();
            }
        }
    }
}
