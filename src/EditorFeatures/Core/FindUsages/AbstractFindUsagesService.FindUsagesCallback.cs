// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal abstract partial class AbstractFindUsagesService
    {
        private class FindUsagesCallback
        {
            private readonly Workspace _workspace;
            private readonly IFindUsagesContext _context;

            public FindUsagesCallback(Workspace workspace, IFindUsagesContext context)
            {
                _workspace = workspace;
                _context = context;
            }

            public Task ReportMessageAsync(string message)
                => _context.ReportMessageAsync(message);

            public Task SetSearchTitleAsync(string title)
                => _context.SetSearchTitleAsync(title);

            public Task ReportProgressAsync(int current, int maximum)
                => _context.ReportProgressAsync(current, maximum);

            public Task OnDefinitionFoundAsync(SerializableDefinitionItem definition)
                => _context.OnDefinitionFoundAsync(definition.Rehydrate(_workspace));

            public Task OnReferenceFoundAsync(SerializableSourceReferenceItem reference)
                => _context.OnReferenceFoundAsync(reference.Rehydrate(_workspace));
        }
    }
}