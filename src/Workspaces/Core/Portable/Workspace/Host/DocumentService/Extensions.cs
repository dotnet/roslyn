// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host
{
    internal static class Extensions
    {
        public static bool CanApplyChange(this TextDocument document)
        {
            return document?.State.CanApplyChange() ?? false;
        }

        public static bool CanApplyChange(this TextDocumentState document)
        {
            return document?.Services.GetService<IDocumentOperationService>().CanApplyChange ?? false;
        }

        public static bool SupportsDiagnostics(this TextDocument document)
        {
            return document?.State.SupportsDiagnostics() ?? false;
        }

        public static bool SupportsDiagnostics(this TextDocumentState document)
        {
            return document?.Services.GetService<IDocumentOperationService>().SupportDiagnostics ?? false;
        }
    }
}
