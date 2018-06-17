// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Organizing.Organizers
{
    internal abstract class AbstractSyntaxNodeOrganizer<TSyntaxNode> : ISyntaxOrganizer
        where TSyntaxNode : SyntaxNode
    {
        public IEnumerable<Type> SyntaxNodeTypes
        {
            get { return SpecializedCollections.SingletonEnumerable(typeof(TSyntaxNode)); }
        }

        public SyntaxNode OrganizeNode(SemanticModel semanticModel, OptionSet optionSet, SyntaxNode node, CancellationToken cancellationToken)
        {
            return Organize((TSyntaxNode)node, optionSet, cancellationToken);
        }

        protected abstract TSyntaxNode Organize(TSyntaxNode node, OptionSet optionSet, CancellationToken cancellationToken);
    }
}
