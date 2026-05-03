// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer;

internal enum WellKnownLspServerKinds
{
    /// <summary>
    /// Roslyn LSP server for razor c# requests.
    /// </summary>
    RazorLspServer,

    /// <summary>
    /// Roslyn LSP server for liveshare guests.
    /// </summary>
    LiveShareLspServer,

    /// <summary>
    /// Roslyn LSP server always activated in VS.
    /// </summary>
    AlwaysActiveVSLspServer,

    /// <summary>
    /// Roslyn LSP server for non-VS use cases.
    /// </summary>
    CSharpVisualBasicLspServer,

    /// <summary>
    /// LSP server for TypeScript
    /// </summary>
    RoslynTypeScriptLspServer,

    /// <summary>
    /// Flag representing any LSP server - used by <see cref="ExportLspServiceFactoryAttribute"/>
    /// to specify that something applies to any LSP server.
    /// </summary>
    Any,
}

internal static class WellKnownLspServerExtensions
{
    public static string ToUserVisibleString(this WellKnownLspServerKinds server)
    {
        return server switch
        {
            WellKnownLspServerKinds.RazorLspServer => "Razor C# Language Server Client",
            WellKnownLspServerKinds.LiveShareLspServer => "Live Share C#/Visual Basic Language Server Client",
            WellKnownLspServerKinds.AlwaysActiveVSLspServer => "Roslyn Language Server Client",
            WellKnownLspServerKinds.CSharpVisualBasicLspServer => "Roslyn Language Server Client",

            // When updating the string of Name, please make sure to update the same string in Microsoft.VisualStudio.LanguageServer.Client.ExperimentalSnippetSupport.AllowList
            WellKnownLspServerKinds.RoslynTypeScriptLspServer => "Roslyn TypeScript Language Server Client",
            _ => throw ExceptionUtilities.UnexpectedValue(server),
        };
    }

    public static string ToTelemetryString(this WellKnownLspServerKinds server)
    {
        return server switch
        {
            // Telemetry was previously reported as RazorInProcLanguageClient.GetType().Name
            WellKnownLspServerKinds.RazorLspServer => "RazorInProcLanguageClient",

            // Telemtry was previously reported as LiveShareInProcLanguageClient.GetType().Name
            WellKnownLspServerKinds.LiveShareLspServer => "LiveShareInProcLanguageClient",

            // Telemtry was previously reported as AlwaysActivateInProcLanguageClient.GetType().Name
            WellKnownLspServerKinds.AlwaysActiveVSLspServer => "AlwaysActivateInProcLanguageClient",

            // Telemetry was previously reported as CSharpVisualBasicLanguageServerFactory.GetType().Name
            WellKnownLspServerKinds.CSharpVisualBasicLspServer => "CSharpVisualBasicLanguageServerFactory",

            WellKnownLspServerKinds.RoslynTypeScriptLspServer => "RoslynTypeScriptLspServer",
            _ => throw ExceptionUtilities.UnexpectedValue(server),
        };
    }

    public static string GetContractName(this WellKnownLspServerKinds server)
    {
        return server switch
        {
            WellKnownLspServerKinds.RazorLspServer => ProtocolConstants.RoslynLspLanguagesContract,
            WellKnownLspServerKinds.LiveShareLspServer => ProtocolConstants.RoslynLspLanguagesContract,
            WellKnownLspServerKinds.AlwaysActiveVSLspServer => ProtocolConstants.RoslynLspLanguagesContract,
            WellKnownLspServerKinds.CSharpVisualBasicLspServer => ProtocolConstants.RoslynLspLanguagesContract,
            WellKnownLspServerKinds.RoslynTypeScriptLspServer => ProtocolConstants.TypeScriptLanguageContract,
            _ => throw ExceptionUtilities.UnexpectedValue(server),
        };
    }
}
