// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Newtonsoft.Json.Linq;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.UnitTests;

public abstract class AbstractLiveShareRequestHandlerTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    private static readonly TestComposition s_composition = LiveShareTestCompositions.Features
        .AddParts(typeof(MockDocumentNavigationService))
        .AddParts(typeof(TestWorkspaceRegistrationService))
        .AddParts(typeof(TestWorkspaceConfigurationService));

    private sealed class MockHostProtocolConverter : IHostProtocolConverter
    {
        private readonly Func<Uri, Uri> _uriConversionFunction;

        public MockHostProtocolConverter() => _uriConversionFunction = uri => uri;

        public MockHostProtocolConverter(Func<Uri, Uri> uriConversionFunction) => _uriConversionFunction = uriConversionFunction;

        public Uri FromProtocolUri(Uri uri) => _uriConversionFunction(uri);

        public bool IsContainedInRootFolders(Uri uriToCheck) => true;

        public bool IsKnownWorkspaceFile(Uri uriToCheck) => throw new NotImplementedException();

        public Task RegisterExternalFilesAsync(Uri[] filePaths) => Task.CompletedTask;

        public Uri ToProtocolUri(Uri uri) => uri;

        public bool TryGetExternalUris(string exernalUri, out Uri uri) => throw new NotImplementedException();
    }

    protected override TestComposition Composition => s_composition;

    protected static async Task<ResponseType> TestHandleAsync<RequestType, ResponseType>(Solution solution, RequestType request, string methodName)
    {
        var requestContext = new RequestContext<Solution>(solution, new MockHostProtocolConverter(), JObject.FromObject(new ClientCapabilities()));
        return await GetHandler<RequestType, ResponseType>(solution, methodName).HandleAsync(request, requestContext, CancellationToken.None);
    }

    protected static async Task<ResponseType> TestHandleAsync<RequestType, ResponseType>(Solution solution, RequestType request, string methodName, Func<Uri, Uri> uriMappingFunc)
    {
        var requestContext = new RequestContext<Solution>(solution, new MockHostProtocolConverter(uriMappingFunc), JObject.FromObject(new ClientCapabilities()));
        return await GetHandler<RequestType, ResponseType>(solution, methodName).HandleAsync(request, requestContext, CancellationToken.None);
    }

    protected static ILspRequestHandler<RequestType, ResponseType, Solution> GetHandler<RequestType, ResponseType>(Solution solution, string methodName)
    {
        var workspace = (LspTestWorkspace)solution.Workspace;
        var handlers = workspace.ExportProvider.GetExportedValues<ILspRequestHandler>(LiveShareConstants.RoslynContractName);
        return (ILspRequestHandler<RequestType, ResponseType, Solution>)handlers.Single(handler => handler is ILspRequestHandler<RequestType, ResponseType, Solution> && IsMatchingMethod(handler, methodName));

        // Since request handlers can have the same input and output types (especially with object), we need to also
        // check that the LSP method the handler is exported for matches the one we're requesting.
        static bool IsMatchingMethod(ILspRequestHandler handler, string methodName)
        {
            var attribute = (ExportLspRequestHandlerAttribute)Attribute.GetCustomAttribute(handler.GetType(), typeof(ExportLspRequestHandlerAttribute));
            return attribute?.MethodName == methodName;
        }
    }
}
