// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EncapsulateField;

internal interface IEncapsulateFieldService : ILanguageService
{
    Task<ImmutableArray<CodeAction>> GetEncapsulateFieldCodeActionsAsync(Document document, TextSpan span, CancellationToken cancellationToken);

    Task<Solution> EncapsulateFieldsAsync(Document document, ImmutableArray<IFieldSymbol> fields, bool updateReferences, CancellationToken cancellationToken);
    Task<EncapsulateFieldResult> EncapsulateFieldsInSpanAsync(Document document, TextSpan span, bool useDefaultBehavior, CancellationToken cancellationToken);
}
