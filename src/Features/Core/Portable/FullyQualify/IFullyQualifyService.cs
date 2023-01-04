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
internal readonly struct FullyQualifyFixData
{
    [DataMember(Order = 0)]
    public readonly string Name;

    [DataMember(Order = 1)]
    public readonly ImmutableArray<FullyQualifyIndividualFixData> IndividualFixData;

    public FullyQualifyFixData(string name, ImmutableArray<FullyQualifyIndividualFixData> individualFixData)
    {
        Name = name;
        IndividualFixData = individualFixData;
    }
}

[DataContract]
internal readonly struct FullyQualifyIndividualFixData
{
    [DataMember(Order = 0)]
    public readonly string Title;
    [DataMember(Order = 1)]
    public readonly ImmutableArray<TextChange> TextChanges;

    public FullyQualifyIndividualFixData(string title, ImmutableArray<TextChange> textChanges)
    {
        Title = title;
        TextChanges = textChanges;
    }
}

internal interface IFullyQualifyService : ILanguageService
{
    Task<FullyQualifyFixData?> GetFixDataAsync(Document document, TextSpan span, bool hideAdvancedMembers, CancellationToken cancellationToken);
}
