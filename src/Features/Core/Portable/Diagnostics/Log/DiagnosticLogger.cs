// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Diagnostics.Log
{
    internal static class DiagnosticLogger
    {
        private const string From = nameof(From);
        private const string Id = nameof(Id);
        private const string HasDescription = nameof(HasDescription);
        private const string Uri = nameof(Uri);

        public static void LogHyperlink(
            string from,
            string id,
            bool description,
            bool telemetry,
            string uri)
        {
            Logger.Log(FunctionId.Diagnostics_HyperLink, KeyValueLogMessage.Create(m =>
            {
                m[From] = from;
                m[Id] = telemetry ? id : id.GetHashCode().ToString();
                m[HasDescription] = description;
                m[Uri] = telemetry ? uri : uri.GetHashCode().ToString();
            }));
        }
    }
}
