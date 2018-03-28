// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.Xaml.Features.InlineRename
{
    internal interface IXamlRenameInfoService
    {
        Task<IXamlRenameInfo> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken);
    }
}
