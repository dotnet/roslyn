// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    // Note: instances of this object are pooled
    internal sealed class AnalyzedArguments
    {
        public readonly ArrayBuilder<BoundExpression> Arguments;
        public readonly ArrayBuilder<IdentifierNameSyntax> Names;
        public readonly ArrayBuilder<RefKind> RefKinds;
        public bool IsExtensionMethodInvocation;
        private ThreeState _lazyHasDynamicArgument;

        internal AnalyzedArguments()
        {
            this.Arguments = new ArrayBuilder<BoundExpression>(32);
            this.Names = new ArrayBuilder<IdentifierNameSyntax>(32);
            this.RefKinds = new ArrayBuilder<RefKind>(32);
        }

        public void Clear()
        {
            this.Arguments.Clear();
            this.Names.Clear();
            this.RefKinds.Clear();
            this.IsExtensionMethodInvocation = false;
            _lazyHasDynamicArgument = ThreeState.Unknown;
        }

        public BoundExpression Argument(int i)
        {
            return Arguments[i];
        }

        public string Name(int i)
        {
            if (Names.Count == 0)
            {
                return null;
            }

            IdentifierNameSyntax syntax = Names[i];
            return syntax == null ? null : syntax.Identifier.ValueText;
        }

        public ImmutableArray<string> GetNames()
        {
            int count = this.Names.Count;

            if (count == 0)
            {
                return default;
            }

            var builder = ArrayBuilder<string>.GetInstance(this.Names.Count);
            for (int i = 0; i < this.Names.Count; ++i)
            {
                builder.Add(Name(i));
            }

            return builder.ToImmutableAndFree();
        }

        public RefKind RefKind(int i)
        {
            return RefKinds.Count > 0 ? RefKinds[i] : Microsoft.CodeAnalysis.RefKind.None;
        }

        public bool IsExtensionMethodThisArgument(int i)
        {
            return (i == 0) && this.IsExtensionMethodInvocation;
        }

        public bool HasDynamicArgument
        {
            get
            {
                if (_lazyHasDynamicArgument.HasValue())
                {
                    return _lazyHasDynamicArgument.Value();
                }

                bool hasRefKinds = RefKinds.Count > 0;
                for (int i = 0; i < Arguments.Count; i++)
                {
                    var argument = Arguments[i];

                    // By-ref dynamic arguments don't make the invocation dynamic.
                    if ((object)argument.Type != null && argument.Type.IsDynamic() && (!hasRefKinds || RefKinds[i] == Microsoft.CodeAnalysis.RefKind.None))
                    {
                        _lazyHasDynamicArgument = ThreeState.True;
                        return true;
                    }
                }

                _lazyHasDynamicArgument = ThreeState.False;
                return false;
            }
        }

        public bool HasErrors
        {
            get
            {
                foreach (var argument in this.Arguments)
                {
                    if (argument.HasAnyErrors)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        #region "Poolable"

        public static AnalyzedArguments GetInstance()
        {
            return Pool.Allocate();
        }

        public static AnalyzedArguments GetInstance(AnalyzedArguments original)
        {
            var instance = GetInstance();
            instance.Arguments.AddRange(original.Arguments);
            instance.Names.AddRange(original.Names);
            instance.RefKinds.AddRange(original.RefKinds);
            instance.IsExtensionMethodInvocation = original.IsExtensionMethodInvocation;
            instance._lazyHasDynamicArgument = original._lazyHasDynamicArgument;
            return instance;
        }

        public void Free()
        {
            this.Clear();
            Pool.Free(this);
        }

        //2) Expose the pool or the way to create a pool or the way to get an instance.
        //       for now we will expose both and figure which way works better
        public static readonly ObjectPool<AnalyzedArguments> Pool = CreatePool();

        private static ObjectPool<AnalyzedArguments> CreatePool()
        {
            ObjectPool<AnalyzedArguments> pool = null;
            pool = new ObjectPool<AnalyzedArguments>(() => new AnalyzedArguments(), 10);
            return pool;
        }

        #endregion
    }
}
