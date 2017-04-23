// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal abstract partial class AbstractFindUsagesService
    {
        private class FindUsagesCallback
        {
            private readonly Solution _solution;
            private readonly IFindUsagesContext _context;

            private readonly object _gate = new object();
            private readonly Dictionary<int, DefinitionItem> _definitionIdToItem = new Dictionary<int, DefinitionItem>();

            public FindUsagesCallback(Solution solution, IFindUsagesContext context)
            {
                _solution = solution;
                _context = context;
            }

            public Task ReportMessageAsync(string message)
                => _context.ReportMessageAsync(message);

            public Task SetSearchTitleAsync(string title)
                => _context.SetSearchTitleAsync(title);

            public Task ReportProgressAsync(int current, int maximum)
                => _context.ReportProgressAsync(current, maximum);

            public Task OnDefinitionFoundAsync(SerializableDefinitionItem definition)
            {
                var definitionItem = definition.Rehydrate(_solution);
                lock (_gate)
                {
                    _definitionIdToItem.Add(definition.SerializationId, definitionItem);
                }

                return _context.OnDefinitionFoundAsync(definitionItem);
            }

            public Task OnReferenceFoundAsync(SerializableSourceReferenceItem reference)
            {
                DefinitionItem item;
                lock (_gate)
                {
                    item = _definitionIdToItem[reference.DefinitionId];
                }

                return _context.OnReferenceFoundAsync(reference.Rehydrate(_solution, item));
            }
        }
    }
}