// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.TodoComments;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TodoComments
{
    /// <summary>
    /// In process service responsible for listening to OOP todo comment notifications.
    /// </summary>
    internal interface IVisualStudioTodoCommentsService
    {
        /// <summary>
        /// Called by a host to let this service know that it should start background
        /// analysis of the workspace to find todo comments
        /// </summary>
        void Start(CancellationToken cancellationToken);

        /// <summary>
        /// Legacy entry-point to allow existing in-process languages to report todo comments.  These languages are
        /// responsible for determining when to compute todo comments (for example, on <see
        /// cref="Workspace.WorkspaceChanged"/>).  This can be called on any thread.
        /// </summary>
        Task ReportTodoCommentsAsync(Document document, ImmutableArray<TodoComment> todoComments, CancellationToken cancellationToken);
    }
}
