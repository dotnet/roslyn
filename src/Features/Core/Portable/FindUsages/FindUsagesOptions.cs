// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.FindUsages;

[DataContract]
internal readonly record struct FindUsagesOptions
{
    [DataMember] public ClassificationOptions ClassificationOptions { get; init; } = ClassificationOptions.Default;

    public FindUsagesOptions()
    {
    }

    public static readonly FindUsagesOptions Default = new();
}

