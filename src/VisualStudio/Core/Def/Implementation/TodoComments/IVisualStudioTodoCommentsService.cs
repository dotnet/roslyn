// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;

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
    }
}
