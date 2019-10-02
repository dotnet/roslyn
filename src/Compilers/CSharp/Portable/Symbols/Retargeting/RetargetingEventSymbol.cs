// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    internal sealed class RetargetingEventSymbol : WrappedEventSymbol
    {
        /// <summary>
        /// Owning RetargetingModuleSymbol.
        /// </summary>
        private readonly RetargetingModuleSymbol _retargetingModule;

        //we want to compute this lazily since it may be expensive for the underlying symbol
        private ImmutableArray<EventSymbol> _lazyExplicitInterfaceImplementations;

        private DiagnosticInfo? _lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        public RetargetingEventSymbol(RetargetingModuleSymbol retargetingModule, EventSymbol underlyingEvent)
            : base(underlyingEvent)
        {
            RoslynDebug.Assert((object)retargetingModule != null);
            Debug.Assert(!(underlyingEvent is RetargetingEventSymbol));

            _retargetingModule = retargetingModule;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return _retargetingModule.RetargetingTranslator;
            }
        }

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get
            {
                return this.RetargetingTranslator.Retarget(_underlyingEvent.TypeWithAnnotations, RetargetOptions.RetargetPrimitiveTypesByTypeCode);
            }
        }

        public override MethodSymbol? AddMethod
        {
            get
            {
                return (object?)_underlyingEvent.AddMethod == null
                    ? null
                    : this.RetargetingTranslator.Retarget(_underlyingEvent.AddMethod);
            }
        }

        public override MethodSymbol? RemoveMethod
        {
            get
            {
                return (object?)_underlyingEvent.RemoveMethod == null
                    ? null
                    : this.RetargetingTranslator.Retarget(_underlyingEvent.RemoveMethod);
            }
        }

        internal override FieldSymbol? AssociatedField
        {
            get
            {
                return (object?)_underlyingEvent.AssociatedField == null
                    ? null
                    : this.RetargetingTranslator.Retarget(_underlyingEvent.AssociatedField);
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return _underlyingEvent.IsExplicitInterfaceImplementation; }
        }

        public override ImmutableArray<EventSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (_lazyExplicitInterfaceImplementations.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(
                        ref _lazyExplicitInterfaceImplementations,
                        this.RetargetExplicitInterfaceImplementations(),
                        default(ImmutableArray<EventSymbol>));
                }
                return _lazyExplicitInterfaceImplementations;
            }
        }

        private ImmutableArray<EventSymbol> RetargetExplicitInterfaceImplementations()
        {
            var impls = _underlyingEvent.ExplicitInterfaceImplementations;

            if (impls.IsEmpty)
            {
                return impls;
            }

            // CONSIDER: we could skip the builder until the first time we see a different method after retargeting

            var builder = ArrayBuilder<EventSymbol>.GetInstance();

            for (int i = 0; i < impls.Length; i++)
            {
                var retargeted = this.RetargetingTranslator.Retarget(impls[i]);
                if ((object)retargeted != null)
                {
                    builder.Add(retargeted);
                }
            }

            return builder.ToImmutableAndFree();
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingTranslator.Retarget(_underlyingEvent.ContainingSymbol);
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return _retargetingModule.ContainingAssembly;
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return _retargetingModule;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _underlyingEvent.GetAttributes();
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return this.RetargetingTranslator.RetargetAttributes(_underlyingEvent.GetCustomAttributesToEmit(moduleBuilder));
        }

        internal override bool MustCallMethodsDirectly
        {
            get
            {
                return _underlyingEvent.MustCallMethodsDirectly;
            }
        }

        internal override DiagnosticInfo? GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(_lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                DiagnosticInfo? result = null;
                CalculateUseSiteDiagnostic(ref result);
                _lazyUseSiteDiagnostic = result;
            }

            return _lazyUseSiteDiagnostic;
        }

        internal sealed override CSharpCompilation? DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}
