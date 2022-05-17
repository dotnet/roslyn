﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementType;

[DataContract]
internal readonly record struct ImplementTypeOptions
{
    [DataMember] public ImplementTypeInsertionBehavior InsertionBehavior { get; init; } = ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind;
    [DataMember] public ImplementTypePropertyGenerationBehavior PropertyGenerationBehavior { get; init; } = ImplementTypePropertyGenerationBehavior.PreferThrowingProperties;

    public ImplementTypeOptions()
    {
    }

    public static readonly ImplementTypeOptions Default = new();
}

internal readonly record struct ImplementTypeGenerationOptions(
    ImplementTypeOptions ImplementTypeOptions,
    CleanCodeGenerationOptionsProvider FallbackOptions);
