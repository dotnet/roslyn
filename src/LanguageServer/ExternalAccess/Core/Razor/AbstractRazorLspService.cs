// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer;

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Razor.Features;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
#endif

internal abstract class AbstractRazorLspService : ILspService
{
}
