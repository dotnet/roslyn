// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Optional interface that can be implemented by <see cref="ILspServices"/> implementations
/// to provide faster access to <see cref="IMethodHandler"/>s.
/// </summary>
internal interface IMethodHandlerProvider
{
    ImmutableArray<(IMethodHandler? Instance, TypeRef HandlerTypeRef, ImmutableArray<MethodHandlerDetails> HandlerDetails)> GetMethodHandlers();
}
