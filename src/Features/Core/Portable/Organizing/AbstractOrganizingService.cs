// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Organizing.Organizers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Organizing;

internal abstract class AbstractOrganizingService : IOrganizingService
{
    private readonly IEnumerable<ISyntaxOrganizer> _organizers;
    protected AbstractOrganizingService(IEnumerable<ISyntaxOrganizer> organizers)
        => _organizers = organizers.ToImmutableArrayOrEmpty();

    public IEnumerable<ISyntaxOrganizer> GetDefaultOrganizers()
        => _organizers;

    protected abstract Task<Document> ProcessAsync(Document document, IEnumerable<ISyntaxOrganizer> organizers, CancellationToken cancellationToken);

    public Task<Document> OrganizeAsync(Document document, IEnumerable<ISyntaxOrganizer> organizers, CancellationToken cancellationToken)
        => ProcessAsync(document, organizers ?? GetDefaultOrganizers(), cancellationToken);

    protected Func<SyntaxNode, IEnumerable<ISyntaxOrganizer>> GetNodeToOrganizers(IEnumerable<ISyntaxOrganizer> organizers)
    {
        var map = new ConcurrentDictionary<Type, IEnumerable<ISyntaxOrganizer>>();
        IEnumerable<ISyntaxOrganizer> getter(Type t1)
        {
            return (from o in organizers
                    where !o.SyntaxNodeTypes.Any() ||
                          o.SyntaxNodeTypes.Any(t2 => t1 == t2 || t1.GetTypeInfo().IsSubclassOf(t2))
                    select o).Distinct();
        }

        return n => map.GetOrAdd(n.GetType(), getter);
    }
}
