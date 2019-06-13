// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
                IInvocationOperation operation = FindParentInvocationOperation(node);

                if (operation != null)
                {
                    IAnonymousFunctionOperation GetAnonymousFunctionOperation(IArgumentOperation argument)
                    {
                        switch (argument.Value.Kind)
                        {
                            case OperationKind.DelegateCreation:
                                return GetAnonymousFunctionOperationOpt(argument.Value);
                            case OperationKind.Conversion:
                                return GetAnonymousFunctionOperationOpt(((IConversionOperation)argument.Value).Operand);
                            default:
                                return null;
                        }
                    }

                    return operation.Arguments
                        .SelectAsArray(argument => GetAnonymousFunctionOperation(argument))
                        .WhereAsArray(operation => operation != null);
                }
                else
                {
                    return ImmutableArray<IAnonymousFunctionOperation>.Empty;
                }
            }

            private static IAnonymousFunctionOperation GetAnonymousFunctionOperationOpt(IOperation operation)
            {
                if (operation is IDelegateCreationOperation delegateCreationOperation)
                {
                    return delegateCreationOperation.Target as IAnonymousFunctionOperation;
                }

                return null;
            }

            protected IAnonymousFunctionOperation FindParentAnonymousFunction(SyntaxNode node)
            {
                IOperation operation = GetOperation(node);
                while (operation?.Parent != null)
                {
                    operation = operation.Parent;
                    if (operation?.Kind == OperationKind.AnonymousFunction)
                    {
                        return (IAnonymousFunctionOperation)operation;
                    }
                }

                return null;
            }

            private IInvocationOperation FindParentInvocationOperation(SyntaxNode node)
            {
                IOperation operation = GetOperation(node)?.Parent;
                while (operation?.Kind != OperationKind.Invocation)
                {
                    operation = operation.Parent;
                    if (operation is null)
                    {
                        return null;
                    }
                }

                return (IInvocationOperation)operation;
            }

            protected IOperation GetOperation(SyntaxNode node)
                => _semanticModel.GetOperation(node, _cancellationToken);

            protected ImmutableArray<string> GetIdentifierNames(SyntaxNode node)
            {
                IOperation operation = GetOperation(node);
                if (operation == null)
                {
                    return default;
                }

                var builder = ImmutableArray.CreateBuilder<string>();

                while (operation is IPropertyReferenceOperation propertyReference)
                {
                    builder.Add(propertyReference.Member.Name);
                    operation = propertyReference.Instance;
                }

                if (operation is IParameterReferenceOperation parameterReference)
                {
                    builder.Add(parameterReference.Parameter.Name);
                }

                builder.Reverse();
                return builder.ToImmutable();
            }
        }
    }
}
