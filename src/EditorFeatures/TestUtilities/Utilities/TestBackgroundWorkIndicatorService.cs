// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities.Utilities
{
    internal class TestBackgroundWorkIndicatorService : IBackgroundWorkIndicatorService
    {
        public IBackgroundWorkIndicator Create(ITextView textView, SnapshotSpan applicableToSpan, string description, BackgroundWorkIndicatorOptions? options = null)
        {
            return new TestBackgroundWorkIndicator(textView, applicableToSpan, description, options);
        }
    }

    internal class TestBackgroundWorkIndicator : IBackgroundWorkIndicator
    {
        private readonly ITextView _textView;
        private readonly SnapshotSpan _applicableToSpan;
        private readonly string _description;
        private readonly BackgroundWorkIndicatorOptions? _options;

        public TestBackgroundWorkIndicator(ITextView textView, SnapshotSpan applicableToSpan, string description, BackgroundWorkIndicatorOptions? options)
        {
            _textView = textView;
            _applicableToSpan = applicableToSpan;
            _description = description;
            _options = options;
        }

        public CancellationToken CancellationToken { get; } = new();

        public BackgroundWorkOperationScope AddScope(string description)
        {
            return new BackgroundWorkOperationScope();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IDisposable SuppressAutoCancel()
        {
            throw new NotImplementedException();
        }
    }
}
