// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes.FullyQualify;

[DataContract]
internal readonly struct FullyQualifyFixData(string name, ImmutableArray<FullyQualifyIndividualFixData> individualFixData)
{
    [DataMember(Order = 0)]
    public readonly string Name = name;

    [DataMember(Order = 1)]
    public readonly ImmutableArray<FullyQualifyIndividualFixData> IndividualFixData = individualFixData;
}

[DataContract]
internal readonly struct FullyQualifyIndividualFixData(string title, ImmutableArray<TextChange> textChanges)
{
    [DataMember(Order = 0)]
    public readonly string Title = title;
    [DataMember(Order = 1)]
    public readonly ImmutableArray<TextChange> TextChanges = textChanges;
}

internal interface IFullyQualifyService : ILanguageService
{
    Task<FullyQualifyFixData?> GetFixDataAsync(Document document, TextSpan span, CancellationToken cancellationToken);
}
