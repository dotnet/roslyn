// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Provides the intermediate document data passed from Handler to ResolveHandler.
/// </summary>
/// <param name="DocumentId">the text document id obtained from the <see cref="DocumentCache"/>.</param>
internal record DocumentIdResolveData(long DocumentId);
