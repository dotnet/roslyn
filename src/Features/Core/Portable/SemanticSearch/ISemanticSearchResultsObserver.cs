// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal interface ISemanticSearchResultsCommonObserver
{
    ValueTask OnUserCodeExceptionAsync(UserCodeExceptionInfo exception, CancellationToken cancellationToken);
    ValueTask AddItemsAsync(int itemCount, CancellationToken cancellationToken);
    ValueTask ItemsCompletedAsync(int itemCount, CancellationToken cancellationToken);
}

internal interface ISemanticSearchResultsObserver : ISemanticSearchResultsCommonObserver
{
    ValueTask OnSymbolFoundAsync(Solution solution, ISymbol symbol, CancellationToken cancellationToken);
}

internal interface ISemanticSearchResultsDefinitionObserver : ISemanticSearchResultsCommonObserver
{
    ValueTask<ClassificationOptions> GetClassificationOptionsAsync(LanguageServices language, CancellationToken cancellationToken);
    ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken);
}

[DataContract]
internal readonly record struct UserCodeExceptionInfo(
    [property: DataMember(Order = 0)] string ProjectDisplayName,
    [property: DataMember(Order = 1)] string Message,
    [property: DataMember(Order = 2)] ImmutableArray<TaggedText> TypeName,
    [property: DataMember(Order = 3)] ImmutableArray<TaggedText> StackTrace,
    [property: DataMember(Order = 4)] TextSpan Span);
