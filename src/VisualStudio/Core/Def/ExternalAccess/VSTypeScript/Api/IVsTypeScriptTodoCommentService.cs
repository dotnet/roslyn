// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.TodoComments;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.VSTypeScript.Api
{
    internal interface IVsTypeScriptTodoCommentService
    {
        /// <summary>
        /// Legacy entry-point to allow existing in-process TypeScript language service to report todo comments.
        /// TypeScript is responsible for determining when to compute todo comments (for example, on <see
        /// cref="Workspace.WorkspaceChanged"/>).  This can be called on any thread.
        /// </summary>
        Task ReportTodoCommentsAsync(Document document, ImmutableArray<TodoComment> todoComments, CancellationToken cancellationToken);
    }
}
