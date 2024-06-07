// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Lightup;

namespace Microsoft.CodeAnalysis.CodeFixes;

internal static class FixAllContextExtensions2
{
    private static readonly Func<FixAllContext, object?> s_progress
        = LightupHelpers.CreatePropertyAccessor<FixAllContext, object?>(typeof(FixAllContext), nameof(Progress), defaultValue: null);
    private static readonly Action<object, object?> s_report;

    static FixAllContextExtensions2()
    {
        var progressType = CodeAnalysisProgressWrapper.WrappedType is null ? null : typeof(IProgress<>).MakeGenericType(CodeAnalysisProgressWrapper.WrappedType);
        s_report = LightupHelpers.CreateActionAccessor<object, object?>(progressType, nameof(ProgressImpl.Report), CodeAnalysisProgressWrapper.WrappedType);
    }

    public static IProgress<CodeAnalysisProgressWrapper> Progress(this FixAllContext context)
    {
        var progressInstance = s_progress(context);
        if (progressInstance is null)
            return CodeAnalysisProgressWrapper.None;

        return new ProgressImpl(progressInstance);
    }

    private sealed class ProgressImpl : IProgress<CodeAnalysisProgressWrapper>
    {
        private readonly object _progressInstance;

        public ProgressImpl(object progressInstance)
        {
            _progressInstance = progressInstance;
        }

        public void Report(CodeAnalysisProgressWrapper value)
        {
            s_report(_progressInstance, value.Instance);
        }
    }
}
