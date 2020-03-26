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
        /// See <see cref="Renamer.RenameDocumentFoldersAsync(Document, System.Collections.Generic.IReadOnlyList{string}, Options.OptionSet, System.Threading.CancellationToken)" />
        /// and <see cref="Renamer.RenameDocumentNameAsync(Document, string, Options.OptionSet, System.Threading.CancellationToken)" />
        /// </summary>
        public abstract class RenameDocumentAction
        {
            public ImmutableArray<string> Errors { get; }
            internal abstract Task<Solution> GetModifiedSolutionAsync(Solution solution, CancellationToken cancellationToken);
            public abstract string GetDescription(CultureInfo? culture = null);

            protected DocumentId DocumentId { get; }
            protected OptionSet OptionSet { get; }

            internal RenameDocumentAction(DocumentId documentId, OptionSet optionSet, ImmutableArray<string> errors)
            {
                DocumentId = documentId;
                OptionSet = optionSet;
                Errors = errors;
            }
        }

    }
}
