// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Host
{
    internal static class Extensions
    {
        public static bool CanApplyChange([NotNullWhen(returnValue: true)] this TextDocument? document)
        {
            return document?.State.CanApplyChange() ?? false;
        }

        public static bool CanApplyChange([NotNullWhen(returnValue: true)] this TextDocumentState? document)
        {
            return document?.Services.GetService<IDocumentOperationService>().CanApplyChange ?? false;
        }

        public static bool SupportsDiagnostics([NotNullWhen(returnValue: true)] this TextDocument? document)
        {
            return document?.State.SupportsDiagnostics() ?? false;
        }

        public static bool SupportsDiagnostics([NotNullWhen(returnValue: true)] this TextDocumentState? document)
        {
            return document?.Services.GetService<IDocumentOperationService>().SupportDiagnostics ?? false;
        }
    }
}
