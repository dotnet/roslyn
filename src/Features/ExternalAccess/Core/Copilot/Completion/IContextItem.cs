// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Copilot.Completion;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Completion;
#endif

[JsonDerivedType(typeof(CodeSnippetItem))]
[JsonDerivedType(typeof(TraitItem))]
internal interface IContextItem
{
}
