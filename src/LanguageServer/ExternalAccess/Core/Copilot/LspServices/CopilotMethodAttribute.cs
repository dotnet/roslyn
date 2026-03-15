// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Copilot;

/// <summary>
/// An attribute which identifies the method which a Copilot request handler like
/// <see cref="AbstractCopilotLspServiceDocumentRequestHandler{TRequest, TResponse}"/> implements.
/// </summary>
internal sealed class CopilotMethodAttribute(string method) : MethodAttribute(method);
