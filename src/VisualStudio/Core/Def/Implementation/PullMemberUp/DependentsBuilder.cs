// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
{
    internal class DependentsBuilder : OperationWalker
    {
        private readonly HashSet<ISymbol> _dependents;

        private readonly ImmutableHashSet<ISymbol> _membersInType;

        internal static IEnumerable<ISymbol> Build(
            SemanticModel semanticModel,
            ISymbol symbol,
            ImmutableHashSet<ISymbol> membersInType)
        {
            var builder = new DependentsBuilder(membersInType);

            var operatons = symbol.DeclaringSyntaxReferences.Select(@ref => semanticModel.GetOperation(@ref.GetSyntax()));
            foreach (var operation in operatons)
            {
                builder.Visit(operation);
            }

            return builder._dependents;
        }

        private DependentsBuilder(ImmutableHashSet<ISymbol> membersInType)
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
