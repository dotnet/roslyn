// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Diagnostics;

public class DiagnosticRegistrationTests : AbstractLanguageServerProtocolTests
{
    public DiagnosticRegistrationTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestPublicDiagnosticSourcesAreRegisteredWhenSupported(bool mutatingLspWorkspace)
    {
        var clientCapabilities = new ClientCapabilities
        {
            TextDocument = new TextDocumentClientCapabilities
            {
                Diagnostic = new DiagnosticSetting
                {
                    DynamicRegistration = true,
                }
            }
        };
        var clientCallbackTarget = new ClientCallbackTarget();
        var initializationOptions = new InitializationOptions()
        {
            CallInitialized = true,
            ClientCapabilities = clientCapabilities,
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            ClientTarget = clientCallbackTarget,
        };

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, initializationOptions);

        var registrations = clientCallbackTarget.GetRegistrations();

        // Get all registrations for diagnostics (note that workspace registrations are registered against document method name).
        var diagnosticRegistrations = registrations
            .Where(r => r.Method == Methods.TextDocumentDiagnosticName)
            .Select(r => JsonSerializer.Deserialize<DiagnosticRegistrationOptions>((JsonElement)r.RegisterOptions!, ProtocolConversions.LspJsonSerializerOptions)!);

        Assert.NotEmpty(diagnosticRegistrations);

        string[] documentSources = [
            PullDiagnosticCategories.DocumentCompilerSyntax,
            PullDiagnosticCategories.DocumentCompilerSemantic,
            PullDiagnosticCategories.DocumentAnalyzerSyntax,
            PullDiagnosticCategories.DocumentAnalyzerSemantic,
            PublicDocumentNonLocalDiagnosticSourceProvider.NonLocal
        ];

        string[] documentAndWorkspaceSources = [
            PullDiagnosticCategories.EditAndContinue,
            PullDiagnosticCategories.WorkspaceDocumentsAndProject
        ];

        // Verify document only sources are present (and do not set the workspace diagnostic option).
        foreach (var documentSource in documentSources)
        {
            var options = Assert.Single(diagnosticRegistrations, (r) => r.Identifier == documentSource);
            Assert.False(options.WorkspaceDiagnostics);
            Assert.True(options.InterFileDependencies);
        }

        // Verify workspace sources are present (and do set the workspace diagnostic option).
        foreach (var workspaceSource in documentAndWorkspaceSources)
        {
            var options = Assert.Single(diagnosticRegistrations, (r) => r.Identifier == workspaceSource);
            Assert.True(options.WorkspaceDiagnostics);
            Assert.True(options.InterFileDependencies);
            Assert.True(options.WorkDoneProgress);
        }

        // Verify task diagnostics are not present.
        Assert.DoesNotContain(diagnosticRegistrations, (r) => r.Identifier == PullDiagnosticCategories.Task);
    }

    /// <summary>
    /// Implements a client side callback target for client/registerCapability to inspect what was registered.
    /// </summary>
    private class ClientCallbackTarget()
    {
        private readonly List<Registration> _registrations = new();

        [JsonRpcMethod(Methods.ClientRegisterCapabilityName, UseSingleObjectParameterDeserialization = true)]
        public void ClientRegisterCapability(RegistrationParams registrationParams, CancellationToken _)
        {
            _registrations.AddRange(registrationParams.Registrations);
        }

        /// <summary>
        /// This is safe to call after 'initialized' has completed because capabilties are dynamically registered in the
        /// implementation of the initialized request.  Additionally, client/registerCapability is a request (not a notification) 
        /// which means the server will wait for the client to finish handling it before the server returns from 'initialized'.
        /// </summary>
        public ImmutableArray<Registration> GetRegistrations()
        {
            return _registrations.ToImmutableArray();
        }
    }
}
