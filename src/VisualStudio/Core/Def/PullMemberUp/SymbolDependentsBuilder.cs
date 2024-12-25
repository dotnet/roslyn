// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp;

internal static class SymbolDependentsBuilder
{
    public static ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> FindMemberToDependentsMap(
        ImmutableArray<ISymbol> membersInType,
        Project project,
        CancellationToken cancellationToken)
    {
        return membersInType.ToImmutableDictionary(
            member => member,
            member => Task.Run(() =>
            {
                var builder = new SymbolWalker(membersInType, project, member, cancellationToken);
                return builder.FindMemberDependentsAsync();
            }, cancellationToken));
    }

    private class SymbolWalker : OperationWalker
    {
        private readonly ImmutableHashSet<ISymbol> _membersInType;
        private readonly Project _project;
        private readonly ISymbolDeclarationService _declarationService;
        private readonly HashSet<ISymbol> _dependents = [];
        private readonly ISymbol _member;
        private readonly CancellationToken _cancellationToken;

        public SymbolWalker(
            ImmutableArray<ISymbol> membersInType,
            Project project,
            ISymbol member,
            CancellationToken cancellationToken)
        {
            _project = project;
            _declarationService = project.Services.GetRequiredService<ISymbolDeclarationService>();
            _membersInType = membersInType.ToImmutableHashSet();
            _member = member;
            _cancellationToken = cancellationToken;
        }

        public async Task<ImmutableArray<ISymbol>> FindMemberDependentsAsync()
        {
            var tasks = _declarationService.GetDeclarations(_member).Select(@ref => @ref.GetSyntaxAsync(_cancellationToken));
            var syntaxes = await Task.WhenAll(tasks).ConfigureAwait(false);
            var compilation = await _project.GetCompilationAsync(_cancellationToken).ConfigureAwait(false);
            foreach (var syntax in syntaxes)
            {
                Visit(compilation.GetSemanticModel(syntax.SyntaxTree).GetOperation(syntax, _cancellationToken));
            }

            return _dependents.ToImmutableArray();
        }

        public override void Visit(IOperation operation)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (operation is IMemberReferenceOperation memberReferenceOp &&
                _membersInType.Contains(memberReferenceOp.Member))
            {
                _dependents.Add(memberReferenceOp.Member);
            }

            // This check is added for checking method invoked in the member
            // It is separated since IInvocationOperation is not subtype of IMemberReferenceOperation
            // issue for this https://github.com/dotnet/roslyn/issues/26206#issuecomment-382105829
            if (operation is IInvocationOperation methodReferenceOp &&
                _membersInType.Contains(methodReferenceOp.TargetMethod))
            {
                _dependents.Add(methodReferenceOp.TargetMethod);
            }

            base.Visit(operation);
        }
    }
}
