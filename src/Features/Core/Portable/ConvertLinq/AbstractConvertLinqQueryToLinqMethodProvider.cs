// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.ConvertLinq
{
    internal abstract class AbstractConvertLinqQueryToLinqMethodProvider : AbstractConvertLinqProvider
    {
        protected abstract class Analyzer<TSource, TDestination> : AnalyzerBase<TSource, TDestination>
            where TSource : SyntaxNode
            where TDestination : SyntaxNode
        {
            public Analyzer(SemanticModel semanticModel, CancellationToken cancellationToken) : base(semanticModel, cancellationToken) { }

            protected static IInvocationOperation FindParentInvocationOperation(IOperation operation)
            {
                operation = operation.Parent;
                while (operation.Kind != OperationKind.Invocation)
                {
                    operation = operation.Parent;
                    if (operation is null)
                    {
                        return null;
                    }
                }

                return operation as IInvocationOperation;
            }

            protected IOperation GetOperation(SyntaxNode node)
            {
                return _semanticModel.GetOperation(node, _cancellationToken);
            }

            protected ImmutableArray<string> GetIdentifierNames(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            {
                var operation = GetOperation(node);
                if (operation == null)
                {
                    return default;
                }

                var builder = ImmutableArray.CreateBuilder<string>();

                while (operation is IPropertyReferenceOperation)
                {
                    var propertyReference = operation as IPropertyReferenceOperation;
                    builder.Add(propertyReference.Member.Name);
                    operation = propertyReference.Instance;
                }

                var parameterReference = operation as IParameterReferenceOperation;
                if (parameterReference != null)
                {
                    builder.Add(parameterReference.Parameter.Name);
                }

                builder.Reverse();
                return builder.ToImmutable();
            }
        }
    }
}
