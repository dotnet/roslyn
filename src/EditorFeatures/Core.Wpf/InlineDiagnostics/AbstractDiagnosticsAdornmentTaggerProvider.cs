// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal abstract partial class AbstractDiagnosticsAdornmentTaggerProvider<TTag> :
    AbstractDiagnosticsTaggerProvider<TTag>
    where TTag : class, ITag
{
    protected AbstractDiagnosticsAdornmentTaggerProvider(
        IThreadingContext threadingContext,
        IDiagnosticService diagnosticService,
        IDiagnosticAnalyzerService analyzerService,
        IGlobalOptionService globalOptions,
        ITextBufferVisibilityTracker? visibilityTracker,
        IAsynchronousOperationListenerProvider listenerProvider)
        : base(threadingContext, diagnosticService, analyzerService, globalOptions, visibilityTracker, listenerProvider.GetListener(FeatureAttribute.ErrorSquiggles))
    {
    }


}
