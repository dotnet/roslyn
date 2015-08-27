// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Diagnostics.Log
{
    internal static class DiagnosticLogger
    {
        private const string From = "From";
        private const string Id = "Id";
        private const string HasDescription = "HasDescription";
        private const string Uri = "Uri";

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
