// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddMissingImports
{
    internal interface IAddMissingImportsFeatureService : ILanguageService
    {
        Task<Solution> AddMissingImportsAsync(Solution solution, CancellationToken cancellationToken);
        Task<Project> AddMissingImportsAsync(Project project, CancellationToken cancellationToken);
        Task<Project> AddMissingImportsAsync(Document document, CancellationToken cancellationToken);
        Task<Project> AddMissingImportsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);
        Task<bool> IsMissingImportsAsync(Document document, CancellationToken cancellationToken);
        Task<bool> IsMissingImportsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);
    }
}
