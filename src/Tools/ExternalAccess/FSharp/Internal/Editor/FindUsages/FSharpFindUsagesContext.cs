// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor.FindUsages
{
    internal class FSharpFindUsagesContext : IFSharpFindUsagesContext
    {
        private readonly IFindUsagesContext _context;

        public FSharpFindUsagesContext(IFindUsagesContext context)
        {
            _context = context;
        }

        public CancellationToken CancellationToken => _context.CancellationToken;

        public Task OnDefinitionFoundAsync(FSharp.FindUsages.FSharpDefinitionItem definition)
        {
            return _context.OnDefinitionFoundAsync(definition.RoslynDefinitionItem).AsTask();
        }

        public Task OnReferenceFoundAsync(FSharp.FindUsages.FSharpSourceReferenceItem reference)
        {
            return _context.OnReferenceFoundAsync(reference.RoslynSourceReferenceItem).AsTask();
        }

        public Task ReportMessageAsync(string message)
        {
            return _context.ReportMessageAsync(message).AsTask();
        }

        public Task ReportProgressAsync(int current, int maximum)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return _context.ReportProgressAsync(current, maximum).AsTask();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public Task SetSearchTitleAsync(string title)
        {
            return _context.SetSearchTitleAsync(title).AsTask();
        }
    }
}
