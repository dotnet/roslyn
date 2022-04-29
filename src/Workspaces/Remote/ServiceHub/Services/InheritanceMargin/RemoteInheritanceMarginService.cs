// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteInheritanceMarginService : BrokeredServiceBase, IRemoteInheritanceMarginService
    {
        internal sealed class Factory : FactoryBase<IRemoteInheritanceMarginService>
        {
            protected override IRemoteInheritanceMarginService CreateService(in ServiceConstructionArguments arguments)
            {
                return new RemoteInheritanceMarginService(arguments);
            }
        }

        public RemoteInheritanceMarginService(in ServiceConstructionArguments arguments) : base(in arguments)
        {
        }

        public ValueTask<ImmutableArray<InheritanceMarginItem>> GetInheritanceMarginItemsAsync(
            Checksum solutionChecksum,
            ProjectId projectId,
            DocumentId? documentIdForGlobalImports,
            TextSpan spanToSearch,
            ImmutableArray<(SymbolKey symbolKey, int lineNumber)> symbolKeyAndLineNumbers,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, solution =>
            {
                var project = solution.GetRequiredProject(projectId);
                var service = (AbstractInheritanceMarginService)project.GetRequiredLanguageService<IInheritanceMarginService>();
                var documentForGlobaImports = solution.GetDocument(documentIdForGlobalImports);

                return service.GetInheritanceMemberItemAsync(project, documentForGlobaImports, spanToSearch, symbolKeyAndLineNumbers, cancellationToken);
            }, cancellationToken);
        }
    }
}
