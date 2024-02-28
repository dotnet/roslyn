// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateDefaultConstructors;

internal interface IGenerateDefaultConstructorsService : ILanguageService
{
    Task<ImmutableArray<CodeAction>> GenerateDefaultConstructorsAsync(
        Document document, TextSpan textSpan, CodeAndImportGenerationOptionsProvider fallbackOptions, bool forRefactoring, CancellationToken cancellationToken);
}
