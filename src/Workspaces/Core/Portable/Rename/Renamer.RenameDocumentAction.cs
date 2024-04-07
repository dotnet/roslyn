// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Rename;

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
        private readonly ImmutableArray<ErrorResource> _errorStringKeys;

        internal RenameDocumentAction(ImmutableArray<ErrorResource> errors)
        {
            _errorStringKeys = errors;
        }

        /// <summary>
        /// Get any errors that have been noted for this action before it is applied.
        /// Can be used to present to a user.
        /// </summary>
        public ImmutableArray<string> GetErrors(CultureInfo? culture = null)
            => _errorStringKeys.SelectAsArray(s => string.Format(WorkspacesResources.ResourceManager.GetString(s.FormatString, culture ?? WorkspacesResources.Culture)!, s.Arguments));

        /// <summary>
        /// Gets the description of the action. Can be used to present to a user to describe
        /// what extra actions will be taken.
        /// </summary>
        public abstract string GetDescription(CultureInfo? culture = null);

        internal abstract Task<Solution> GetModifiedSolutionAsync(Document document, DocumentRenameOptions options, CancellationToken cancellationToken);

        internal readonly struct ErrorResource(string formatString, object[] arguments)
        {
            public string FormatString { get; } = formatString;
            public object[] Arguments { get; } = arguments;
        }
    }
}
