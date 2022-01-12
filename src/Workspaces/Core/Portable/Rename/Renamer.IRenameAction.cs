// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// This interface acts as a layer of indirection from <see cref="Renamer.RenameDocumentAction" /> to
        /// allow implementation details to exist in different layers (including Code Style) without changing
        /// the public API surface.
        /// </summary>
        /// <remarks>
        /// Originally <see cref="Renamer.RenameDocumentAction" /> was an abstract class with no public way to 
        /// inherit. To keep the public API surface from breaking and still correctly represent the intent, that no
        /// external implementation of <see cref="Renamer.RenameDocumentAction" /> is possible, the class is made to
        /// be sealed and this interface was added as the adaptor between internal implementations and the public API 
        /// surface. 
        /// </remarks>
        internal interface IRenameAction
        {
            string GetDescription(CultureInfo? culture);
            Task<Solution> GetModifiedSolutionAsync(Document document, OptionSet optionSet, CancellationToken cancellationToken);
            ImmutableArray<string> GetErrors(CultureInfo? culture);
        }
    }
}
