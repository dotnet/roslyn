// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.Language;

internal partial class DefaultRazorTagHelperContextDiscoveryPhase
{
    private static readonly ObjectPool<TagHelperDirectiveVisitor> s_tagHelperDirectiveVisitorPool = DefaultPool.Create<TagHelperDirectiveVisitor>();
    private static readonly ObjectPool<ComponentDirectiveVisitor> s_componentDirectiveVisitorPool = DefaultPool.Create<ComponentDirectiveVisitor>();

    internal readonly ref struct PooledDirectiveVisitor(DirectiveVisitor visitor, bool isComponentDirectiveVisitor)
    {
        public void Dispose()
        {
            if (isComponentDirectiveVisitor)
            {
                s_componentDirectiveVisitorPool.Return((ComponentDirectiveVisitor)visitor);
            }
            else
            {
                s_tagHelperDirectiveVisitorPool.Return((TagHelperDirectiveVisitor)visitor);
            }
        }
    }

    internal static PooledDirectiveVisitor GetPooledVisitor(
        RazorCodeDocument codeDocument,
        TagHelperCollection tagHelpers,
        CancellationToken cancellationToken,
        out DirectiveVisitor visitor)
    {
        var useComponentDirectiveVisitor = codeDocument.ParserOptions.AllowComponentFileKind &&
                                           codeDocument.FileKind.IsComponent();

        var filePath = codeDocument.Source.FilePath;

        if (useComponentDirectiveVisitor)
        {
            var componentDirectiveVisitor = s_componentDirectiveVisitorPool.Get();

            codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var currentNamespace);
            componentDirectiveVisitor.Initialize(tagHelpers, filePath, currentNamespace, cancellationToken);

            visitor = componentDirectiveVisitor;
        }
        else
        {
            var tagHelperDirectiveVisitor = s_tagHelperDirectiveVisitorPool.Get();

            tagHelperDirectiveVisitor.Initialize(tagHelpers, filePath, cancellationToken);

            visitor = tagHelperDirectiveVisitor;
        }

        return new(visitor, useComponentDirectiveVisitor);
    }
}
