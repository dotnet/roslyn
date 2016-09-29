// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Xaml.Features;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Xaml.CodeFixes.AddMissingNamespace
{
    internal interface IXamlAddMissingNamespaceService
    {
        Task<CodeAction> CreateMissingNamespaceFixAsync(Document document, TextSpan span, CancellationToken cancellationToken);
    }
}
