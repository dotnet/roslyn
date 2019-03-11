// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

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

        /// <summary>
        /// This method always returns true. The definition is retained until IVT users migrate to the adapter
        /// assemblies.
        /// </summary>
        [Obsolete("This method always returns true.")]
        public static bool SupportsDiagnostics(this TextDocument document)
        {
            return true;
        }

        /// <summary>
        /// This method always returns true. The definition is retained until IVT users migrate to the adapter
        /// assemblies.
        /// </summary>
        [Obsolete("This method always returns true.")]
        public static bool SupportsDiagnostics(this TextDocumentState document)
        {
            return true;
        }
    }
}
