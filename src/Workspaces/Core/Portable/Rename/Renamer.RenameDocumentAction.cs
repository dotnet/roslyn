// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Rename
{
    public static partial class Renamer
    {
        /// <summary>
        /// Individual action from RenameDocument APIs in <see cref="Renamer"/>. Represents
        /// changes that will be done to one or more document contents to help facilitate
        /// a smooth experience while moving documents around.
        /// 
        /// See <see cref="RenameDocumentActionSet" /> on use case and how to apply them to a solution.
        /// </summary>
        public abstract class RenameDocumentAction
        {
            internal RenameDocumentAction()
            {
            }

            /// <summary>
            /// Get any errors that have been noted for this action before it is applied.
            /// Can be used to present to a user.
            /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Public API")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Public API")]
            public ImmutableArray<string> GetErrors(CultureInfo? culture = null) => ImmutableArray<string>.Empty;

            /// <summary>
            /// Gets the description of the action. Can be used to present to a user to describe
            /// what extra actions will be taken.
            /// </summary>
            public abstract string GetDescription(CultureInfo? culture = null);
        }
    }
}
