// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[DataContract]
internal sealed class SourceGeneratedDocumentText
{
    [DataMember(Name = "text")]
    public required string? Text { get; init; }
}
