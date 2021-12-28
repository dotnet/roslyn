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
        internal interface IRenameAction
        {
            string GetDescription(CultureInfo? culture);
            Task<Solution> GetModifiedSolutionAsync(Document document, OptionSet optionSet, CancellationToken cancellationToken);
            ImmutableArray<string> GetErrors(CultureInfo? culture);
        }
    }
}
