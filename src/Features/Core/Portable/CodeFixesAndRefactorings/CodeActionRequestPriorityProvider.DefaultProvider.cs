// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal abstract partial class CodeActionRequestPriorityProvider
    {
        private sealed class DefaultProvider : CodeActionRequestPriorityProvider
        {
            public static readonly DefaultProvider Instance = new();

            private DefaultProvider()
                : base(CodeActionRequestPriority.None)
            {
            }

            protected override bool IsDeprioritizedAnalyzerWithLowPriority(DiagnosticAnalyzer analyzer)
                => false;

            public override void AddDeprioritizedAnalyzerWithLowPriority(DiagnosticAnalyzer analyzer)
                => throw ExceptionUtilities.Unreachable();

            public override CodeActionRequestPriorityProvider With(CodeActionRequestPriority priority)
                => throw ExceptionUtilities.Unreachable();
        }
    }
}
