// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

[UseExportProvider]
public sealed class PartialResultProgressTests : AbstractLanguageServerProtocolTests
{
    private const string NumericToken = "42";
    private const string StringToken = "partial-result-token";

    public PartialResultProgressTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    protected override TestComposition Composition => base.Composition.AddParts(typeof(TestPartialResultHandler));

    [Theory, CombinatorialData]
    public async Task PartialResultsAreReportedWithNamedArguments(bool mutatingLspWorkspace, bool useStringToken)
    {
        var progressTarget = new ProgressCapturingTarget();
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            ClientTarget = progressTarget,
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
        });

        var token = useStringToken ? $"\"{StringToken}\"" : NumericToken;
        using var request = JsonDocument.Parse($$"""
            {
                "{{Methods.PartialResultTokenName}}": {{token}}
            }
            """);

        await testLspServer.ExecutePreSerializedRequestAsync(TestPartialResultHandler.MethodName, request);

        // Intentionally asserts on the raw JSON - deserializing it (or passing an IProgress<T> from the
        // client) would let streamjsonrpc accept either named or positional arguments and hide the shape
        // the spec requires.
        var progressParams = await progressTarget.WaitForProgressAsync();
        Assert.Equal(JsonValueKind.Object, progressParams.ValueKind);

        var reportedToken = progressParams.GetProperty("token");
        if (useStringToken)
        {
            Assert.Equal(StringToken, reportedToken.GetString());
        }
        else
        {
            Assert.Equal(int.Parse(NumericToken), reportedToken.GetInt32());
        }

        Assert.Equal(
            TestPartialResultHandler.ReportedValue,
            progressParams.GetProperty("value").EnumerateArray().Single().GetString());
    }

    /// <summary>
    /// Captures the raw <c>$/progress</c> parameters rather than deserializing them, so that the test can
    /// assert on the shape streamjsonrpc actually put on the wire.
    /// </summary>
    private sealed class ProgressCapturingTarget
    {
        private readonly TaskCompletionSource<JsonElement> _progressSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        [JsonRpcMethod(Methods.ProgressNotificationName, UseSingleObjectParameterDeserialization = true)]
        public Task HandleProgressAsync(JsonElement progressParams, CancellationToken _)
        {
            // Clone as streamjsonrpc recycles the underlying document once the notification is handled.
            _progressSource.TrySetResult(progressParams.Clone());
            return Task.CompletedTask;
        }

        public Task<JsonElement> WaitForProgressAsync() => _progressSource.Task;
    }

    internal sealed class TestPartialResultParams : IPartialResultParams<string[]>
    {
        [JsonPropertyName(Methods.PartialResultTokenName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IProgress<string[]>? PartialResultToken { get; set; }
    }

    [ExportCSharpVisualBasicStatelessLspService(typeof(TestPartialResultHandler)), PartNotDiscoverable, Shared]
    [Method(MethodName)]
    internal sealed class TestPartialResultHandler : ILspServiceRequestHandler<TestPartialResultParams, string[]>
    {
        public const string MethodName = nameof(TestPartialResultHandler);
        public const string ReportedValue = "partial result";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestPartialResultHandler()
        {
        }

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => false;

        public Task<string[]> HandleRequestAsync(TestPartialResultParams request, RequestContext context, CancellationToken cancellationToken)
        {
            request.PartialResultToken?.Report([ReportedValue]);
            return Task.FromResult<string[]>([]);
        }
    }
}
