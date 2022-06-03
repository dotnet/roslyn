// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.Xaml.Features.InlineRename
{
    internal interface IXamlRenameInfoService
    {
        Task<IXamlRenameInfo> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken);
    }
}
