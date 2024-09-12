// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess
{
    internal interface ITextViewWindowInProcess
    {
        TestServices TestServices { get; }

        Task<IWpfTextView> GetActiveTextViewAsync(CancellationToken cancellationToken);

        Task<ITextBuffer?> GetBufferContainingCaretAsync(IWpfTextView view, CancellationToken cancellationToken);
    }
}
