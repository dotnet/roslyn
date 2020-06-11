// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Host
{
    internal static class Extensions
    {
        public static bool CanApplyChange([NotNullWhen(returnValue: true)] this TextDocument? document)
            => document?.State.CanApplyChange() ?? false;

        public static bool CanApplyChange([NotNullWhen(returnValue: true)] this TextDocumentState? document)
            => document?.Services.GetService<IDocumentOperationService>().CanApplyChange ?? false;

        public static bool SupportsDiagnostics([NotNullWhen(returnValue: true)] this TextDocument? document)
            => document?.State.SupportsDiagnostics() ?? false;

        public static bool SupportsDiagnostics([NotNullWhen(returnValue: true)] this TextDocumentState? document)
            => document?.Services.GetService<IDocumentOperationService>().SupportDiagnostics ?? false;
    }
}
