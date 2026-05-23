// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Remote;

[DataContract]
internal sealed record RemoteExcerptResult(
    [property: DataMember(Order = 0)] DocumentId RazorDocumentId,
    [property: DataMember(Order = 1)] TextSpan RazorDocumentSpan,
    [property: DataMember(Order = 2)] TextSpan ExcerptSpan,
    [property: DataMember(Order = 3)] ImmutableArray<ClassifiedSpan> ClassifiedSpans,
    [property: DataMember(Order = 4)] TextSpan Span);
