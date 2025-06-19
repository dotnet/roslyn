// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceExtensionImplementationMethodSymbol : RewrittenMethodSymbol // Tracked by https://github.com/dotnet/roslyn/issues/76130 : Do we need to implement ISynthesizedMethodBodyImplementationSymbol?
    {
        private string? lazyDocComment;
        private StrongBox<byte?>? lazyNullableContext;

        public SourceExtensionImplementationMethodSymbol(MethodSymbol sourceMethod)
            : base(sourceMethod, TypeMap.Empty, sourceMethod.ContainingType.TypeParameters.Concat(sourceMethod.TypeParameters))
        {
            Debug.Assert(sourceMethod.GetIsNewExtensionMember());
            Debug.Assert(sourceMethod.IsStatic || sourceMethod.ContainingType.ExtensionParameter is not null);
            Debug.Assert(!sourceMethod.IsExtern);
            Debug.Assert(!sourceMethod.IsExternal);

            // Tracked by https://github.com/dotnet/roslyn/issues/78963 : Are we creating type parameters with the right emit behavior? Attributes, etc.
            //            Also, they should be IsImplicitlyDeclared
        }

        public override int Arity => TypeParameters.Length;

        public override bool IsGenericMethod => Arity != 0;

        public override MethodKind MethodKind => MethodKind.Ordinary;
        public override bool IsImplicitlyDeclared => true;

        internal override bool HasSpecialName => _originalMethod.HasSpecialNameAttribute;

        internal override int ParameterCount
        {
            get
            {
                return _originalMethod.ParameterCount + (_originalMethod.IsStatic ? 0 : 1);
            }
        }

        public sealed override bool IsExtensionMethod => !_originalMethod.IsStatic && _originalMethod.MethodKind is MethodKind.Ordinary;
        public sealed override bool IsVirtual => false;

        public sealed override bool IsOverride => false;
        public sealed override bool IsAbstract => false;
        public sealed override bool IsSealed => false;

        internal sealed override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None) => false;
        internal sealed override bool IsMetadataFinal => false;
        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal sealed override bool IsAccessCheckedOnOverride => false;

        public sealed override bool IsExtern => false;
        public sealed override DllImportData? GetDllImportData() => null;
        internal sealed override bool IsExternal => false;

        internal sealed override bool IsDeclaredReadOnly => false;

        public sealed override Symbol ContainingSymbol => _originalMethod.ContainingType.ContainingSymbol;

        internal sealed override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            // Copy ORPA from the property onto the implementation accessors
            if (_originalMethod is SourcePropertyAccessorSymbol { AssociatedSymbol: SourcePropertySymbolBase extensionProperty })
            {
                foreach (CSharpAttributeData attr in extensionProperty.GetAttributes())
                {
                    if (attr.IsTargetAttribute(AttributeDescription.OverloadResolutionPriorityAttribute))
                    {
                        AddSynthesizedAttribute(ref attributes, attr);
                    }
                }
            }

            SourceMethodSymbol.AddSynthesizedAttributes(this, moduleBuilder, ref attributes);
        }

        internal override void AddSynthesizedReturnTypeAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            if (_originalMethod is SourcePropertyAccessorSymbol accessor)
            {
                accessor.AddSynthesizedReturnTypeFlowAnalysisAttributes(ref attributes);
            }

            base.AddSynthesizedReturnTypeAttributes(moduleBuilder, ref attributes);
        }

        internal override byte? GetLocalNullableContextValue()
        {
            if (lazyNullableContext is null)
            {
                byte? nullableContext = SourceMemberMethodSymbol.ComputeNullableContextValue(this);
                Interlocked.CompareExchange(ref lazyNullableContext, new StrongBox<byte?>(nullableContext), comparand: null);
            }

            return lazyNullableContext.Value;
        }

        public override bool IsStatic => true;
        public override bool RequiresInstanceReceiver => false;

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                return (_originalMethod.CallingConvention & (~Cci.CallingConvention.HasThis)) |
                       (Arity != 0 ? Cci.CallingConvention.Generic : 0);
            }
        }

        public override Symbol? AssociatedSymbol => null;

        protected override ImmutableArray<ParameterSymbol> MakeParameters()
        {
            var sourceParameters = _originalMethod.Parameters;
            var parameters = ArrayBuilder<ParameterSymbol>.GetInstance(ParameterCount);

            if (!_originalMethod.IsStatic)
            {
                // Tracked by https://github.com/dotnet/roslyn/issues/78962 : Need to confirm if this rewrite going to break LocalStateTracingInstrumenter
                //            Specifically BoundParameterId, etc.   
                parameters.Add(new ExtensionMetadataMethodParameterSymbol(this, ((SourceNamedTypeSymbol)_originalMethod.ContainingType).ExtensionParameter!));
            }

            foreach (var parameter in sourceParameters)
            {
                parameters.Add(new ExtensionMetadataMethodParameterSymbol(this, parameter));
            }

            Debug.Assert(parameters.Count == ParameterCount);
            return parameters.ToImmutableAndFree();
        }

        internal override bool TryGetThisParameter(out ParameterSymbol? thisParameter)
        {
            thisParameter = null;
            return true;
        }

        internal override int TryGetOverloadResolutionPriority()
        {
            if (UnderlyingMethod is SourcePropertyAccessorSymbol { AssociatedSymbol: SourcePropertySymbol property })
            {
                return property.TryGetOverloadResolutionPriority();
            }

            return UnderlyingMethod.TryGetOverloadResolutionPriority();
        }

        public override string GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
        {
            // Neither the culture nor the expandIncludes affect the XML for extension implementation methods.
            string result = SourceDocumentationCommentUtils.GetAndCacheDocumentationComment(this, expandIncludes: false, ref lazyDocComment);

#if DEBUG
            string? ignored = null;
            string withIncludes = SourceDocumentationCommentUtils.GetAndCacheDocumentationComment(this, expandIncludes: true, lazyXmlText: ref ignored);
            Debug.Assert(string.Equals(result, withIncludes, System.StringComparison.Ordinal));
#endif

            return result;
        }

        private sealed class ExtensionMetadataMethodParameterSymbol : RewrittenMethodParameterSymbol
        {
            public ExtensionMetadataMethodParameterSymbol(SourceExtensionImplementationMethodSymbol containingMethod, ParameterSymbol sourceParameter) :
                base(containingMethod, sourceParameter)
            {
            }

            public override bool IsImplicitlyDeclared => true;

            public override int Ordinal
            {
                get
                {
                    if (this._underlyingParameter.ContainingSymbol is NamedTypeSymbol)
                    {
                        return 0;
                    }

                    var ordinal = this._underlyingParameter.Ordinal;

                    if (this._underlyingParameter.ContainingSymbol.IsStatic)
                    {
                        return ordinal;
                    }

                    return ordinal + 1;
                }
            }

            internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
            {
                if (_underlyingParameter is SynthesizedAccessorValueParameterSymbol valueParameter)
                {
                    valueParameter.AddSynthesizedFlowAnalysisAttributes(ref attributes);
                }

                // Synthesized nullability attributes are context-dependent, so we intentionally do not call base.AddSynthesizedAttributes here
                // as that would delegate to underlying parameter symbol
                SourceParameterSymbolBase.AddSynthesizedAttributes(this, moduleBuilder, ref attributes);
            }
        }
    }
}
