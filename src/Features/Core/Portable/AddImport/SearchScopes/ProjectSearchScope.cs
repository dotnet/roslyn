﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        private abstract class ProjectSearchScope : SearchScope
        {
            protected readonly Project _project;

            public ProjectSearchScope(
                AbstractAddImportFeatureService<TSimpleNameSyntax> provider,
                Project project,
                bool exact,
                CancellationToken cancellationToken)
                : base(provider, exact, cancellationToken)
            {
                _project = project;
            }

            public override SymbolReference CreateReference<T>(SymbolResult<T> symbol)
            {
                return new ProjectSymbolReference(
                    provider, symbol.WithSymbol<INamespaceOrTypeSymbol>(symbol.Symbol), _project);
            }
        }
    }
}
