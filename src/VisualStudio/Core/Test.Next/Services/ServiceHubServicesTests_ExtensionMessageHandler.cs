// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.Threading;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote;

public sealed partial class ServiceHubServicesTests
{
    [Fact]
    public async Task TestExtensionMessageHandlerService_MultipleRegistrationThrows()
    {
        using var workspace = CreateWorkspace();

        var extensionMessageHandlerService = workspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        // Don't trap the error here.  We want to validate that this crosses the ServiceHub boundary and is reported as
        // a real roslyn/gladstone error.
        string errorMessage = null;
        var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => errorMessage = message;

        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Null(errorMessage);

        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Contains(nameof(InvalidOperationException), errorMessage);
    }

    //[PartNotDiscoverable]
    //[ExportWorkspaceService(typeof(IWorkspaceConfigurationService), ServiceLayer.Test), Shared]
    //[method: ImportingConstructor]
    //[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    //private sealed class TestExtensionMessageHandlerService() : IExtensionMessageHandlerService
    //{
    //    public ValueTask RegisterExtensionAsync(string assemblyFilePath, CancellationToken cancellationToken)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public ValueTask UnregisterExtensionAsync(string assemblyFilePath, CancellationToken cancellationToken)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public ValueTask<GetExtensionMessageNamesResponse> GetExtensionMessageNamesAsync(string assemblyFilePath, CancellationToken cancellationToken)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public ValueTask ResetAsync(CancellationToken cancellationToken)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public ValueTask<string> HandleExtensionWorkspaceMessageAsync(Solution solution, string messageName, string jsonMessage, CancellationToken cancellationToken)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public ValueTask<string> HandleExtensionDocumentMessageAsync(Document documentId, string messageName, string jsonMessage, CancellationToken cancellationToken)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

}
