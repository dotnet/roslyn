// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Newtonsoft.Json;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests
{
    [UseExportProvider]
    public class HandlerTests : AbstractLanguageServerProtocolTests
    {
        public HandlerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        protected override TestComposition Composition => base.Composition.AddParts(
            typeof(DocumentHandler),
            typeof(RequestHandlerWithNoParams),
            typeof(NotificationHandler),
            typeof(NotificationWithoutParamsHandler),
            typeof(LanguageSpecificHandler),
            typeof(LanguageSpecificHandlerWithDifferentParams));

        [Theory, CombinatorialData]
        public async Task CanExecuteRequestHandler(bool mutatingLspWorkspace)
        {
            await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

            var request = new TestRequestTypeOne(new TextDocumentIdentifier
            {
                Uri = ProtocolConversions.CreateAbsoluteUri(@"C:\test.cs")
            });
            var response = await server.ExecuteRequestAsync<TestRequestTypeOne, string>(DocumentHandler.MethodName, request, CancellationToken.None);
            Assert.Equal(typeof(DocumentHandler).Name, response);
        }

        [Theory, CombinatorialData]
        public async Task CanExecuteRequestHandlerWithNoParams(bool mutatingLspWorkspace)
        {
            await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

            var response = await server.ExecuteRequest0Async<string>(RequestHandlerWithNoParams.MethodName, CancellationToken.None);
            Assert.Equal(typeof(RequestHandlerWithNoParams).Name, response);
        }

        [Theory, CombinatorialData]
        public async Task CanExecuteNotificationHandler(bool mutatingLspWorkspace)
        {
            await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

            var request = new TestRequestTypeOne(new TextDocumentIdentifier
            {
                Uri = ProtocolConversions.CreateAbsoluteUri(@"C:\test.cs")
            });
            await server.ExecuteNotificationAsync(NotificationHandler.MethodName, request);
            var response = await NotificationHandler.ResultSource.Task;
            Assert.Equal(typeof(NotificationHandler).Name, response);
        }

        [Theory, CombinatorialData]
        public async Task CanExecuteNotificationHandlerWithNoParams(bool mutatingLspWorkspace)
        {
            await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

            await server.ExecuteNotification0Async(NotificationWithoutParamsHandler.MethodName);
            var response = await NotificationWithoutParamsHandler.ResultSource.Task;
            Assert.Equal(typeof(NotificationWithoutParamsHandler).Name, response);
        }

        [Theory, CombinatorialData]
        public async Task CanExecuteLanguageSpecificHandler(bool mutatingLspWorkspace)
        {
            await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

            var request = new TestRequestTypeOne(new TextDocumentIdentifier
            {
                Uri = ProtocolConversions.CreateAbsoluteUri(@"C:\test.fs")
            });
            var response = await server.ExecuteRequestAsync<TestRequestTypeOne, string>(DocumentHandler.MethodName, request, CancellationToken.None);
            Assert.Equal(typeof(LanguageSpecificHandler).Name, response);
        }

        [Theory, CombinatorialData]
        public async Task CanExecuteLanguageSpecificHandlerWithDifferentRequestTypes(bool mutatingLspWorkspace)
        {
            await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

            var request = new TestRequestTypeTwo(new TextDocumentIdentifier
            {
                Uri = ProtocolConversions.CreateAbsoluteUri(@"C:\test.vb")
            });
            var response = await server.ExecuteRequestAsync<TestRequestTypeTwo, string>(DocumentHandler.MethodName, request, CancellationToken.None);
            Assert.Equal(typeof(LanguageSpecificHandlerWithDifferentParams).Name, response);
        }

        [Theory, CombinatorialData]
        public async Task ThrowsOnInvalidLanguageSpecificHandler(bool mutatingLspWorkspace)
        {
            // Arrange
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await CreateTestLspServerAsync("", mutatingLspWorkspace, extraExportedTypes: [typeof(DuplicateLanguageSpecificHandler)]));
        }

        [Theory, CombinatorialData]
        public async Task ThrowsIfDeserializationFails(bool mutatingLspWorkspace)
        {
            await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

            var request = new TestRequestTypeThree("value");
            await Assert.ThrowsAsync<StreamJsonRpc.RemoteInvocationException>(async () => await server.ExecuteRequestAsync<TestRequestTypeThree, string>(DocumentHandler.MethodName, request, CancellationToken.None));
        }

        [DataContract]
        internal record TestRequestTypeOne([property: DataMember(Name = "textDocument"), JsonProperty(Required = Required.Always)] TextDocumentIdentifier TextDocumentIdentifier);

        [DataContract]
        internal record TestRequestTypeTwo([property: DataMember(Name = "textDocument"), JsonProperty(Required = Required.Always)] TextDocumentIdentifier TextDocumentIdentifier);

        [DataContract]
        internal record TestRequestTypeThree([property: DataMember(Name = "someValue")] string SomeValue);

        [ExportCSharpVisualBasicStatelessLspService(typeof(DocumentHandler)), PartNotDiscoverable, Shared]
        [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
        internal class DocumentHandler : ILspServiceDocumentRequestHandler<TestRequestTypeOne, string>
        {
            public const string MethodName = nameof(DocumentHandler);

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public DocumentHandler()
            {
            }

            public bool MutatesSolutionState => true;
            public bool RequiresLSPSolution => true;

            public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequestTypeOne request)
            {
                return request.TextDocumentIdentifier;
            }

            public Task<string> HandleRequestAsync(TestRequestTypeOne request, RequestContext context, CancellationToken cancellationToken)
            {
                return Task.FromResult(this.GetType().Name);
            }
        }

        [ExportCSharpVisualBasicStatelessLspService(typeof(RequestHandlerWithNoParams)), PartNotDiscoverable, Shared]
        [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
        internal class RequestHandlerWithNoParams : ILspServiceRequestHandler<string>
        {
            public const string MethodName = nameof(RequestHandlerWithNoParams);

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public RequestHandlerWithNoParams()
            {
            }

            public bool MutatesSolutionState => true;
            public bool RequiresLSPSolution => true;

            public Task<string> HandleRequestAsync(RequestContext context, CancellationToken cancellationToken)
            {
                return Task.FromResult(this.GetType().Name);
            }
        }

        [ExportCSharpVisualBasicStatelessLspService(typeof(NotificationHandler)), PartNotDiscoverable, Shared]
        [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
        internal class NotificationHandler : ILspServiceNotificationHandler<TestRequestTypeOne>
        {
            public const string MethodName = nameof(NotificationHandler);
            public static readonly TaskCompletionSource<string> ResultSource = new();

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public NotificationHandler()
            {
            }

            public bool MutatesSolutionState => true;
            public bool RequiresLSPSolution => true;

            public Task HandleNotificationAsync(TestRequestTypeOne request, RequestContext context, CancellationToken cancellationToken)
            {
                ResultSource.SetResult(this.GetType().Name);
                return ResultSource.Task;
            }
        }

        [ExportCSharpVisualBasicStatelessLspService(typeof(NotificationWithoutParamsHandler)), PartNotDiscoverable, Shared]
        [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
        internal class NotificationWithoutParamsHandler : ILspServiceNotificationHandler
        {
            public const string MethodName = nameof(NotificationWithoutParamsHandler);
            public static readonly TaskCompletionSource<string> ResultSource = new();

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public NotificationWithoutParamsHandler()
            {
            }

            public bool MutatesSolutionState => true;
            public bool RequiresLSPSolution => true;

            public Task HandleNotificationAsync(RequestContext context, CancellationToken cancellationToken)
            {
                ResultSource.SetResult(this.GetType().Name);
                return ResultSource.Task;
            }
        }

        /// <summary>
        /// Defines a language specific handler with the same method as <see cref="DocumentHandler"/>
        /// </summary>
        [ExportCSharpVisualBasicStatelessLspService(typeof(LanguageSpecificHandler)), PartNotDiscoverable, Shared]
        [LanguageServerEndpoint(DocumentHandler.MethodName, LanguageNames.FSharp)]
        internal class LanguageSpecificHandler : ILspServiceDocumentRequestHandler<TestRequestTypeOne, string>
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public LanguageSpecificHandler()
            {
            }

            public bool MutatesSolutionState => true;
            public bool RequiresLSPSolution => true;

            public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequestTypeOne request)
            {
                return request.TextDocumentIdentifier;
            }

            public Task<string> HandleRequestAsync(TestRequestTypeOne request, RequestContext context, CancellationToken cancellationToken)
            {
                return Task.FromResult(this.GetType().Name);
            }
        }

        /// <summary>
        /// Defines a language specific handler with the same method as <see cref="DocumentHandler"/>
        /// but using different request and response types.
        /// </summary>
        [ExportCSharpVisualBasicStatelessLspService(typeof(LanguageSpecificHandlerWithDifferentParams)), PartNotDiscoverable, Shared]
        [LanguageServerEndpoint(DocumentHandler.MethodName, LanguageNames.VisualBasic)]
        internal class LanguageSpecificHandlerWithDifferentParams : ILspServiceDocumentRequestHandler<TestRequestTypeTwo, string>
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public LanguageSpecificHandlerWithDifferentParams()
            {
            }

            public bool MutatesSolutionState => true;
            public bool RequiresLSPSolution => true;

            public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequestTypeTwo request)
            {
                return request.TextDocumentIdentifier;
            }

            public Task<string> HandleRequestAsync(TestRequestTypeTwo request, RequestContext context, CancellationToken cancellationToken)
            {
                return Task.FromResult(this.GetType().Name);
            }
        }

        /// <summary>
        /// Defines a language specific handler with the same method and language as <see cref="LanguageSpecificHandler"/>
        /// but with different params (an error)
        /// </summary>
        [ExportCSharpVisualBasicStatelessLspService(typeof(DuplicateLanguageSpecificHandler)), PartNotDiscoverable, Shared]
        [LanguageServerEndpoint(DocumentHandler.MethodName, LanguageNames.FSharp)]
        internal class DuplicateLanguageSpecificHandler : ILspServiceRequestHandler<string>
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public DuplicateLanguageSpecificHandler()
            {
            }

            public bool MutatesSolutionState => true;
            public bool RequiresLSPSolution => true;

            public Task<string> HandleRequestAsync(RequestContext context, CancellationToken cancellationToken)
            {
                return Task.FromResult(this.GetType().Name);
            }
        }
    }
}
