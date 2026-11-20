// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Completion;

[JsonDerivedType(typeof(CodeSnippetItem))]
[JsonDerivedType(typeof(TraitItem))]
internal interface IContextItem
{
}
