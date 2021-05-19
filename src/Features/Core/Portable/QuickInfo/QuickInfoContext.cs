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
    /// The context presented to a <see cref="QuickInfoProvider"/> when providing quick info.
    /// </summary>
    internal sealed class QuickInfoContext : AbstractQuickInfoContext
    {
        /// <summary>
        /// The document that quick info was requested within.
        /// </summary>
        public Document Document { get; }

        public QuickInfoContext(
            Document document,
            int position,
            CancellationToken cancellationToken)
            : base(position, document.Project.LanguageServices, cancellationToken)
        {
            Document = document;
        }
    }
}
