// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.SignatureHelp;

[DataContract]
internal readonly record struct SignatureHelpOptions
{
    [DataMember] public bool HideAdvancedMembers { get; init; } = CompletionOptions.Default.HideAdvancedMembers;

    public SignatureHelpOptions()
    {
    }

    public static readonly SignatureHelpOptions Default = new();
}
