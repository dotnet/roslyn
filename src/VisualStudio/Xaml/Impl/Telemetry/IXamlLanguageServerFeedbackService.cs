// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Telemetry
{
    /// <summary>
    /// Service that collects data for Telemetry in XamlLanguageServer
    /// </summary>
    internal interface IXamlLanguageServerFeedbackService
    {
        /// <summary>
        /// Create a RequestScope of a request of given documentId
        /// </summary>
        IRequestScope CreateRequestScope(DocumentId? documentId, string methodName);
    }
}
