// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.RazorCompiler;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Remote
{
    [DataContract]
    internal class HostOutputRequest
    {
        [DataMember(Name = "textDocument")]
        internal required TextDocumentIdentifier TextDocument { get; set; }

        [DataMember(Name = "generatorName")]
        internal required string GeneratorName { get; set; }

        [DataMember(Name = "requestedOutput")]
        internal required string RequestedOutput { get; set; }
    }

    [DataContract]
    internal class HostOutputResponse
    {
        [DataMember(Name = "outputs")]
        internal string? Output { get; set; }
    }

    [Method(MethodName)]
    [ExportMetadata("Extensions", new string[] { "cshtml", "razor" })]
    [ExportCSharpVisualBasicStatelessLspServiceAttribute(typeof(RazorHostOutputHandler)), Shared]
    internal class RazorHostOutputHandler : ILspServiceRequestHandler<HostOutputRequest, HostOutputResponse>
    {
        internal const string MethodName = "razor/hostOutput";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RazorHostOutputHandler()
        {
        }

        public bool MutatesSolutionState { get; } = false;

        public bool RequiresLSPSolution { get; } = true;

        public async Task<HostOutputResponse> HandleRequestAsync(HostOutputRequest request, RequestContext context, CancellationToken cancellationToken)
        {
            string? output = null;

            var project = context.Solution?.GetAdditionalDocument(request.TextDocument)?.Project;
            if (project is not null)
            {
                output = await GetHostOutputAsync(project, request.GeneratorName, request.RequestedOutput, cancellationToken).ConfigureAwait(false);
            }
            return new HostOutputResponse() { Output = output };
        }

        public static async Task<string?> GetHostOutputAsync(Project project, string generatorName, string requestedOutput, CancellationToken cancellationToken)
        {
            var runResult = await project.GetSourceGeneratorRunResultAsync(cancellationToken).ConfigureAwait(false);
            if (runResult is not null)
            {
                var generatorResult = runResult.Results.FirstOrDefault(r => r.Generator.GetGeneratorType().FullName == generatorName);
                var hostOutputs = generatorResult.GetHostOutputs();

                foreach (var (key, value) in hostOutputs)
                {
                    if (key == requestedOutput)
                    {
                        return value;
                    }
                }
            }

            return null;
        }
    }
}
