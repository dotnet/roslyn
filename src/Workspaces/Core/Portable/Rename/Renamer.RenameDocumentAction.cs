// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        /// Individual action from RenameDocument APIs in <see cref="Renamer"/>.
        /// 
        /// See <see cref="RenameDocumentActionSet" />
        /// </summary>
        public abstract class RenameDocumentAction
        {
            private readonly ImmutableArray<ErrorResource> _errorStringKeys;

            public ImmutableArray<string> GetErrors(CultureInfo? culture = null)
                => _errorStringKeys.SelectAsArray(s => string.Format(WorkspacesResources.ResourceManager.GetString(s.FormatString, culture ?? WorkspacesResources.Culture)!, s.Arguments));

            public abstract string GetDescription(CultureInfo? culture = null);
            internal abstract Task<Solution> GetModifiedSolutionAsync(Document document, OptionSet optionSet, CancellationToken cancellationToken);

            internal RenameDocumentAction(ImmutableArray<ErrorResource> errors)
            {
                _errorStringKeys = errors;
            }

            internal readonly struct ErrorResource
            {
                public string FormatString { get; }
                public object?[] Arguments { get; }

                public ErrorResource(string formatString, object?[] arguments)
                {
                    FormatString = formatString;
                    Arguments = arguments;
                }
            }
        }
    }
}
