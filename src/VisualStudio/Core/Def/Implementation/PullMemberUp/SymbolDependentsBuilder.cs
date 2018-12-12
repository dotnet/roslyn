// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
{
    internal class SymbolDependentsBuilder : OperationWalker
    {
        private readonly HashSet<ISymbol> _dependents;

        private readonly ImmutableHashSet<ISymbol> _membersInType;

        internal static ImmutableDictionary<ISymbol, AsyncLazy<ImmutableArray<ISymbol>>> CreateDependentsMap(
            Document document,
            ImmutableArray<ISymbol> membersInType)
        {
            return membersInType.ToImmutableDictionary(
                member => member,
                member => new AsyncLazy<ImmutableArray<ISymbol>>(
                    cancellationToken => FindMemberDependentsAsync(member, document, membersInType, cancellationToken),
                    cacheResult: true));
        }

        private async static Task<ImmutableArray<ISymbol>> FindMemberDependentsAsync(
            ISymbol member,
            Document contextDocument,
            ImmutableArray<ISymbol> membersInType,
            CancellationToken cancellationToken)
        {
            var tasks = member.DeclaringSyntaxReferences.Select(@ref => @ref.GetSyntaxAsync(cancellationToken));
            var syntaxes = await Task.WhenAll(tasks).ConfigureAwait(false);
            var compilation = await contextDocument.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var builder = new SymbolDependentsBuilder(membersInType.ToImmutableHashSet());
            var operations = syntaxes.Select(syntax => compilation.GetSemanticModel(syntax.SyntaxTree).GetOperation(syntax, cancellationToken));

            foreach (var operation in operations)
            {
                builder.Visit(operation);
            }

            return builder._dependents.ToImmutableArray();
        }

        private SymbolDependentsBuilder(ImmutableHashSet<ISymbol> membersInType)
        {
            _membersInType = membersInType;
            _dependents = new HashSet<ISymbol>();
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
