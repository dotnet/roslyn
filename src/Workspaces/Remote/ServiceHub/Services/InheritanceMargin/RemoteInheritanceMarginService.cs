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

        public ValueTask<ImmutableArray<InheritanceMarginItem>> GetGlobalImportItemsAsync(
            Checksum solutionChecksum,
            DocumentId documentId,
            TextSpan spanToSearch,
            bool frozenPartialSemantics,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, solution =>
            {
                var document = solution.GetRequiredDocument(documentId);
                var service = (AbstractInheritanceMarginService)document.GetRequiredLanguageService<IInheritanceMarginService>();

                return service.GetGlobalImportItemsAsync(document, spanToSearch, frozenPartialSemantics, cancellationToken);
            }, cancellationToken);
        }

        public ValueTask<ImmutableArray<InheritanceMarginItem>> GetSymbolItemsAsync(
            Checksum solutionChecksum,
            ProjectId projectId,
            DocumentId? documentId,
            ImmutableArray<(SymbolKey symbolKey, int lineNumber)> symbolKeyAndLineNumbers,
            bool frozenPartialSemantics,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, solution =>
            {
                var project = solution.GetRequiredProject(projectId);
                var document = solution.GetDocument(documentId);

                return AbstractInheritanceMarginService.GetSymbolItemsAsync(project, document, symbolKeyAndLineNumbers, frozenPartialSemantics, cancellationToken);
            }, cancellationToken);
        }
    }
}
