// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.PdbSourceDocument;

/// <summary>
/// Logs messages when navigating to external sources (eg. SourceLink, embedded) so that users can
/// troubleshoot issues that might prevent it working (authentication, checksum errors, etc.)
/// </summary>
internal interface IPdbSourceDocumentLogger
{
    void Clear();
    void Log(string message);
}

internal static class PdbSourceDocumentLoggerExtensions
{
    public static void Log(this IPdbSourceDocumentLogger logger, string message, object arg0)
        => logger.Log(string.Format(message, arg0));

    public static void Log(this IPdbSourceDocumentLogger logger, string message, object arg0, object arg1)
        => logger.Log(string.Format(message, arg0, arg1));
}
