// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
