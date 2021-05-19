// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// Base context presented to a <see cref="QuickInfoProvider"/> or a <see cref="InternalQuickInfoProvider"/> when providing quick info.
    /// </summary>
    internal abstract class AbstractQuickInfoContext
    {
        /// <summary>
        /// The caret position where quick info was requested from.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// Host language services for the workspace.
        /// </summary>
        public HostLanguageServices LanguageServices { get; }

        /// <summary>
        /// Workspace.
        /// </summary>
        public Workspace Workspace => LanguageServices.WorkspaceServices.Workspace;

        /// <summary>
        /// The cancellation token to use for this operation.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        protected AbstractQuickInfoContext(
            int position,
            HostLanguageServices languageServices,
            CancellationToken cancellationToken)
        {
            Position = position;
            LanguageServices = languageServices ?? throw new ArgumentNullException(nameof(languageServices));
            CancellationToken = cancellationToken;
        }
    }
}
