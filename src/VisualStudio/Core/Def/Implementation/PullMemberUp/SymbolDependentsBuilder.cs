// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
{
    internal static class SymbolDependentsBuilder
    {
        internal static ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> FindMemberToDependentsMap(
            Document document,
            ImmutableArray<ISymbol> membersInType,
            CancellationToken cancellationToken)
        {
            return membersInType.ToImmutableDictionary(
                member => member,
                member =>
                {
                    var builder = new SymbolWalker(document, membersInType, member);
                    return Task.Run(() => builder.FindMemberDependentsAsync(cancellationToken), cancellationToken);
                });
        }

        private class SymbolWalker : OperationWalker
        {
            private readonly ImmutableHashSet<ISymbol> _membersInType;
            private readonly Document _document;
            private readonly HashSet<ISymbol> _dependents;
            private readonly ISymbol _member;

            internal SymbolWalker(
                Document document,
                ImmutableArray<ISymbol> membersInType,
                ISymbol member)
            {
                _document = document;
                _membersInType = membersInType.ToImmutableHashSet();
                _dependents = new HashSet<ISymbol>();
                _member = member;
            }

            internal async Task<ImmutableArray<ISymbol>> FindMemberDependentsAsync(CancellationToken cancellationToken)
            {
                var tasks = _member.DeclaringSyntaxReferences.Select(@ref => @ref.GetSyntaxAsync(cancellationToken));
                var syntaxes = await Task.WhenAll(tasks).ConfigureAwait(false);
                var compilation = await _document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                foreach (var syntax in syntaxes)
                {
                    Visit(compilation.GetSemanticModel(syntax.SyntaxTree).GetOperation(syntax, cancellationToken));
                }

                return _dependents.ToImmutableArray();
            }

            public override void Visit(IOperation operation)
            {
                if (operation is IMemberReferenceOperation memberReferenceOp &&
                    _membersInType.Contains(memberReferenceOp.Member))
                {
                    _dependents.Add(memberReferenceOp.Member);
                }

                // This check is added for checking method invoked in the member
                // It is seperated since IInvocationOperation is not subtype of IMemberReferenceOperation
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
}
