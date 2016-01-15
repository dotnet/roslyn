// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    internal sealed class RetargetingEventSymbol : EventSymbol
    {
        /// <summary>
        /// Owning RetargetingModuleSymbol.
        /// </summary>
        private readonly RetargetingModuleSymbol _retargetingModule;

        /// <summary>
        /// The underlying EventSymbol, cannot be another RetargetingEventSymbol.
        /// </summary>
        private readonly EventSymbol _underlyingEvent;

        //we want to compute this lazily since it may be expensive for the underlying symbol
        private ImmutableArray<EventSymbol> _lazyExplicitInterfaceImplementations;

        private DiagnosticInfo _lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        public RetargetingEventSymbol(RetargetingModuleSymbol retargetingModule, EventSymbol underlyingEvent)
        {
            Debug.Assert((object)retargetingModule != null);
            Debug.Assert((object)underlyingEvent != null);
            Debug.Assert(!(underlyingEvent is RetargetingEventSymbol));

            _retargetingModule = retargetingModule;
            _underlyingEvent = underlyingEvent;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return _retargetingModule.RetargetingTranslator;
            }
        }

        public EventSymbol UnderlyingEvent
        {
            get
            {
                return _underlyingEvent;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return _underlyingEvent.IsImplicitlyDeclared;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return _underlyingEvent.HasSpecialName;
            }
        }

        public override TypeSymbolWithAnnotations Type
        {
            get
            {
                return this.RetargetingTranslator.Retarget(_underlyingEvent.Type, RetargetOptions.RetargetPrimitiveTypesByTypeCode);
            }
        }

        public override MethodSymbol AddMethod
        {
            get
            {
                return (object)_underlyingEvent.AddMethod == null
                    ? null
                    : this.RetargetingTranslator.Retarget(_underlyingEvent.AddMethod);
            }
        }

        public override MethodSymbol RemoveMethod
        {
            get
            {
                return (object)_underlyingEvent.RemoveMethod == null
                    ? null
                    : this.RetargetingTranslator.Retarget(_underlyingEvent.RemoveMethod);
            }
        }

        internal override FieldSymbol AssociatedField
        {
            get
            {
                return (object)_underlyingEvent.AssociatedField == null
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

        public override string Name
        {
            get
            {
                return _underlyingEvent.Name;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _underlyingEvent.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _underlyingEvent.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _underlyingEvent.DeclaringSyntaxReferences;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return _underlyingEvent.DeclaredAccessibility;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return _underlyingEvent.IsStatic;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return _underlyingEvent.IsVirtual;
            }
        }

        public override bool IsOverride
        {
            get
            {
                return _underlyingEvent.IsOverride;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return _underlyingEvent.IsAbstract;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return _underlyingEvent.IsSealed;
            }
        }

        public override bool IsExtern
        {
            get
            {
                return _underlyingEvent.IsExtern;
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return _underlyingEvent.ObsoleteAttributeData;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _underlyingEvent.GetAttributes();
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            return this.RetargetingTranslator.RetargetAttributes(_underlyingEvent.GetCustomAttributesToEmit(compilationState));
        }

        internal override bool MustCallMethodsDirectly
        {
            get
            {
                return _underlyingEvent.MustCallMethodsDirectly;
            }
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(_lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                DiagnosticInfo result = null;
                CalculateUseSiteDiagnostic(ref result);
                _lazyUseSiteDiagnostic = result;
            }

            return _lazyUseSiteDiagnostic;
        }

        public override bool IsWindowsRuntimeEvent
        {
            get
            {
                return _underlyingEvent.IsWindowsRuntimeEvent;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return _underlyingEvent.HasRuntimeSpecialName;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

        internal override bool NullableOptOut
        {
            get
            {
                return _underlyingEvent.NullableOptOut;
            }
        }
    }
}
