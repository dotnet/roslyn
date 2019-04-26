// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Roslyn.Test.Utilities;
using Xunit;
using RoslynHandlers = Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Roslyn.VisualStudio.CSharp.UnitTests.LiveShare
{
    public abstract class AbstractLiveShareRequestHandlerTests : AbstractLanguageServerProtocolTests
    {
        private class MockHostProtocolConverter : IHostProtocolConverter
        {
            public Uri FromProtocolUri(Uri uri)
            {
                throw new NotImplementedException();
            }

            public bool IsContainedInRootFolders(Uri uriToCheck)
            {
                return true;
            }

            public Task RegisterExternalFilesAsync(Uri[] filePaths)
            {
                return Task.CompletedTask;
            }

            public Uri ToProtocolUri(Uri uri)
            {
                return uri;
            }

            public bool TryGetExternalUris(string exernalUri, out Uri uri)
            {
                throw new NotImplementedException();
            }
        }

        protected override ExportProvider GetExportProvider()
        {
            // Get all the liveshare request handlers in this assembly.
            var liveShareRequestHelperTypes = DesktopTestHelpers.GetAllTypesImplementingGivenInterface(
                    typeof(LoadHandler).Assembly, typeof(ILspRequestHandler));
            // Get all of the roslyn request helpers in M.CA.LanguageServer
            var roslynRequestHelperTypes = DesktopTestHelpers.GetAllTypesImplementingGivenInterface(
                    typeof(RoslynHandlers.IRequestHandler).Assembly, typeof(RoslynHandlers.IRequestHandler));
            var exportProviderFactory = ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic
                .WithPart(typeof(MockDocumentNavigationServiceFactory))
                .WithParts(liveShareRequestHelperTypes)
                .WithParts(roslynRequestHelperTypes));
            return exportProviderFactory.CreateExportProvider();
        }

        protected static async Task<ResponseType> TestHandleAsync<RequestType, ResponseType>(Solution solution, RequestType request)
        {
            var requestContext = new RequestContext<Solution>(solution, new MockHostProtocolConverter(), new ClientCapabilities());
            return await GetHandler<RequestType, ResponseType>(solution).HandleAsync(request, requestContext, CancellationToken.None);
        }

        protected static ILspRequestHandler<RequestType, ResponseType, Solution> GetHandler<RequestType, ResponseType>(Solution solution)
        {
            var workspace = (TestWorkspace)solution.Workspace;
            var handlers = workspace.ExportProvider.GetExportedValues<ILspRequestHandler>(LiveShareConstants.RoslynContractName);
            return (ILspRequestHandler<RequestType, ResponseType, Solution>)handlers.Single(handler => handler is ILspRequestHandler<RequestType, ResponseType, Solution>);
        }
    }
}
