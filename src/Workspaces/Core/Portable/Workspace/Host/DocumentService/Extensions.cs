// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Host;

internal static class Extensions
{
    private const string RazorCSharpLspClientName = "RazorCSharp";

    extension([NotNullWhen(true)] TextDocument? document)
    {
        public bool CanApplyChange()
        => document?.State.CanApplyChange() ?? false;

        public bool SupportsDiagnostics()
            => document?.State.SupportsDiagnostics() ?? false;
    }

    extension([NotNullWhen(true)] TextDocumentState? document)
    {
        public bool CanApplyChange()
        => document?.DocumentServiceProvider.GetService<IDocumentOperationService>()?.CanApplyChange ?? false;

        public bool SupportsDiagnostics()
            => document?.DocumentServiceProvider.GetService<IDocumentOperationService>()?.SupportDiagnostics ?? false;
    }

    extension(TextDocument document)
    {
        public bool IsRazorDocument()
        => IsRazorDocument(document.State);
    }

    extension(TextDocumentState documentState)
    {
        public bool IsRazorDocument()
        => documentState.DocumentServiceProvider.GetService<DocumentPropertiesService>()?.DiagnosticsLspClientName == RazorCSharpLspClientName;
    }
}
