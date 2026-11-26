// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SelectedMembers;

[ExportLanguageService(typeof(ISelectedMembersService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpSelectedMembersService() : ISelectedMembersService
{
    public Task<ImmutableArray<SyntaxNode>> GetSelectedMembersAsync(
        SyntaxTree tree, TextSpan span, bool allowPartialSelection, CancellationToken cancellationToken)
        => CSharpSelectedMembers.Instance.GetSelectedMembersAsync(tree, span, allowPartialSelection, cancellationToken);
}
