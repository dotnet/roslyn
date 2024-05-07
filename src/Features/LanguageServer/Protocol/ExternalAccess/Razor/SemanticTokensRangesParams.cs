// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;

[DataContract]
internal class SemanticTokensRangesParams : SemanticTokensParams
{
    [DataMember(Name = "ranges")]
    public required Range[] Ranges { get; set; }
}
