// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            return _context.OnDefinitionFoundAsync(definition.RoslynDefinitionItem);
        }

        public Task OnReferenceFoundAsync(FSharp.FindUsages.FSharpSourceReferenceItem reference)
        {
            return _context.OnReferenceFoundAsync(reference.RoslynSourceReferenceItem);
        }

        public Task ReportMessageAsync(string message)
        {
            return _context.ReportMessageAsync(message);
        }

        public Task ReportProgressAsync(int current, int maximum)
        {
            return _context.ReportProgressAsync(current, maximum);
        }

        public Task SetSearchTitleAsync(string title)
        {
            return _context.SetSearchTitleAsync(title);
        }
    }
}
