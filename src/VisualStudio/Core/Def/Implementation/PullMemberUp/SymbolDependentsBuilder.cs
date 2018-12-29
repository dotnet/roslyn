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
    internal class SymbolDependentsBuilder : OperationWalker
    {
        private readonly HashSet<ISymbol> _dependents;
        private readonly ImmutableHashSet<ISymbol> _membersInType;
        private readonly Document _document;

        internal SymbolDependentsBuilder(Document document, ImmutableArray<ISymbol> membersInType)
        {
            _document = document;
            _membersInType = membersInType.ToImmutableHashSet();
            _dependents = new HashSet<ISymbol>();
        }

        public ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> CreateDependentsMap(CancellationToken cancellationToken)
        {
            return _membersInType.ToImmutableDictionary(
                member => member,
                member => Task.Run(() => FindMemberDependentsAsync(_document, member, cancellationToken), cancellationToken));
        }

        private async Task<ImmutableArray<ISymbol>> FindMemberDependentsAsync(
            Document document,
            ISymbol member,
            CancellationToken cancellationToken)
        {
            var tasks = member.DeclaringSyntaxReferences.Select(@ref => @ref.GetSyntaxAsync(cancellationToken));
            var syntaxes = await Task.WhenAll(tasks).ConfigureAwait(false);
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

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

            // This check is added for methodReferenceOperation due to https://github.com/dotnet/roslyn/issues/26206#issuecomment-382105829
            if (operation is IInvocationOperation methodReferenceOp &&
                _membersInType.Contains(methodReferenceOp.TargetMethod))
            {
                _dependents.Add(methodReferenceOp.TargetMethod);
            }

            base.Visit(operation);
        }
    }
}
