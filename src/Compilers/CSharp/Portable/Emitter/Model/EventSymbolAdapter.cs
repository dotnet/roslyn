// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class
#if DEBUG
        EventSymbolAdapter : SymbolAdapter,
#else
        EventSymbol :
#endif 
        Cci.IEventDefinition
    {
        bool Cci.IDefinition.IsEncDeleted
            => false;

        #region IEventDefinition Members

        IEnumerable<Cci.IMethodReference> Cci.IEventDefinition.GetAccessors(EmitContext context)
        {
            CheckDefinitionInvariant();

            var addMethod = AdaptedEventSymbol.AddMethod?.GetCciAdapter();
            RoslynDebug.Assert((object?)addMethod != null);
            if (addMethod.ShouldInclude(context))
            {
                yield return addMethod;
            }

            var removeMethod = AdaptedEventSymbol.RemoveMethod?.GetCciAdapter();
            RoslynDebug.Assert((object?)removeMethod != null);
            if (removeMethod.ShouldInclude(context))
            {
                yield return removeMethod;
            }
        }

        Cci.IMethodReference Cci.IEventDefinition.Adder
        {
            get
            {
                CheckDefinitionInvariant();
                var addMethod = AdaptedEventSymbol.AddMethod?.GetCciAdapter();
                RoslynDebug.Assert((object?)addMethod != null);
                return addMethod;
            }
        }

        Cci.IMethodReference Cci.IEventDefinition.Remover
        {
            get
            {
                CheckDefinitionInvariant();
                var removeMethod = AdaptedEventSymbol.RemoveMethod?.GetCciAdapter();
                RoslynDebug.Assert((object?)removeMethod != null);
                return removeMethod;
            }
        }

        bool Cci.IEventDefinition.IsRuntimeSpecial
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedEventSymbol.HasRuntimeSpecialName;
            }
        }

        bool Cci.IEventDefinition.IsSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedEventSymbol.HasSpecialName;
            }
        }

        Cci.IMethodReference? Cci.IEventDefinition.Caller
        {
            get
            {
                CheckDefinitionInvariant();
                return null; // C# doesn't use the raise/fire accessor
            }
        }

        Cci.ITypeReference Cci.IEventDefinition.GetType(EmitContext context)
        {
            return ((PEModuleBuilder)context.Module).Translate(AdaptedEventSymbol.Type, syntaxNodeOpt: (CSharpSyntaxNode?)context.SyntaxNode, diagnostics: context.Diagnostics);
        }

        #endregion

        #region ITypeDefinitionMember Members

        Cci.ITypeDefinition Cci.ITypeDefinitionMember.ContainingTypeDefinition
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedEventSymbol.ContainingType.GetCciAdapter();
            }
        }

        Cci.TypeMemberVisibility Cci.ITypeDefinitionMember.Visibility
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedEventSymbol.MetadataVisibility;
            }
        }

        #endregion

        #region ITypeMemberReference Members

        Cci.ITypeReference Cci.ITypeMemberReference.GetContainingType(EmitContext context)
        {
            CheckDefinitionInvariant();
            return AdaptedEventSymbol.ContainingType.GetCciAdapter();
        }

        #endregion

        #region IReference Members

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            CheckDefinitionInvariant();
            visitor.Visit((Cci.IEventDefinition)this);
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            CheckDefinitionInvariant();
            return this;
        }

        #endregion

        #region INamedEntity Members

        string Cci.INamedEntity.Name
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedEventSymbol.MetadataName;
            }
        }

        #endregion
    }

    internal partial class EventSymbol
    {
#if DEBUG
        private EventSymbolAdapter? _lazyAdapter;

        protected sealed override SymbolAdapter GetCciAdapterImpl() => GetCciAdapter();

        internal new EventSymbolAdapter GetCciAdapter()
        {
            if (_lazyAdapter is null)
            {
                return InterlockedOperations.Initialize(ref _lazyAdapter, new EventSymbolAdapter(this));
            }

            return _lazyAdapter;
        }
#else
        internal EventSymbol AdaptedEventSymbol => this;

        internal new EventSymbol GetCciAdapter()
        {
            return this;
        }
#endif

        internal virtual bool HasRuntimeSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return false;
            }
        }
    }

#if DEBUG
    internal partial class EventSymbolAdapter
    {
        internal EventSymbolAdapter(EventSymbol underlyingEventSymbol)
        {
            AdaptedEventSymbol = underlyingEventSymbol;
        }

        internal sealed override Symbol AdaptedSymbol => AdaptedEventSymbol;
        internal EventSymbol AdaptedEventSymbol { get; }
    }
#endif
}
