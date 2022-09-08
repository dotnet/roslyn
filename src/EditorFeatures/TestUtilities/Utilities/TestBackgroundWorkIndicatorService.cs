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
using Moq;

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
#pragma warning disable IDE0052 // Remove unread private members
        private readonly ITextView _textView;
        private readonly SnapshotSpan _applicableToSpan;
        private readonly string _description;
        private readonly BackgroundWorkIndicatorOptions? _options;
#pragma warning restore IDE0052 // Remove unread private members

        private AutoCancelSuppresor? _autoCancelSuppressor;

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
            return Mock.Of<BackgroundWorkOperationScope>();
        }

        public void Dispose()
        {
        }

        public IDisposable SuppressAutoCancel()
        {
            if (_autoCancelSuppressor is not null)
            {
                return _autoCancelSuppressor;
            }

            _autoCancelSuppressor = new(this);
            return _autoCancelSuppressor;
        }

        private class AutoCancelSuppresor : IDisposable
        {
            private readonly TestBackgroundWorkIndicator _testBackgroundWorkIndicator;

            public AutoCancelSuppresor(TestBackgroundWorkIndicator testBackgroundWorkIndicator)
            {
                _testBackgroundWorkIndicator = testBackgroundWorkIndicator;
            }

            public void Dispose()
            {
                if (_testBackgroundWorkIndicator._autoCancelSuppressor == this)
                {
                    _testBackgroundWorkIndicator._autoCancelSuppressor = null;
                }
            }
        }
    }
}
