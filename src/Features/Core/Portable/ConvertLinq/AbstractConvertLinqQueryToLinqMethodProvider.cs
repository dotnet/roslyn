// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
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

            protected ImmutableArray<IAnonymousFunctionOperation> FindAnonymousFunctionsFromParentInvocationOperation(SyntaxNode node)
            {
                var operation = FindParentInvocationOperation(node);

                if (operation != null)
                {
                    return ImmutableArray
                        .CreateRange(operation.Arguments.Where(a => a.Value.Kind == OperationKind.DelegateCreation)
                        .Select(argumentOperation => (argumentOperation.Value as IDelegateCreationOperation).Target as IAnonymousFunctionOperation));
                }
                else
                {
                    return ImmutableArray.Create<IAnonymousFunctionOperation>();
                }
            }

            protected IAnonymousFunctionOperation FindParentAnonymousFunction(SyntaxNode node)
            {
                var operation = GetOperation(node);
                while (operation?.Parent != null)
                {
                    operation = operation.Parent;
                    if (operation?.Kind == OperationKind.AnonymousFunction)
                    {
                        return operation as IAnonymousFunctionOperation;
                    }
                }

                return null;
            }

            private IInvocationOperation FindParentInvocationOperation(SyntaxNode node)
            {
                var operation = GetOperation(node)?.Parent;
                while (operation?.Kind != OperationKind.Invocation)
                {
                    operation = operation?.Parent;
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
