// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Indentation
{
    internal interface ISmartTokenFormatter
    {
        Task<IList<TextChange>> FormatTokenAsync(Workspace workspace, SyntaxToken token, CancellationToken cancellationToken);
    }
}
