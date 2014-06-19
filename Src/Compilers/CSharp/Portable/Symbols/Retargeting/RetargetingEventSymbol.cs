// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly RetargetingModuleSymbol retargetingModule;

        /// <summary>
        /// The underlying EventSymbol, cannot be another RetargetingEventSymbol.
        /// </summary>
        private readonly EventSymbol underlyingEvent;

        //we want to compute this lazily since it may be expensive for the underlying symbol
        private ImmutableArray<EventSymbol> lazyExplicitInterfaceImplementations;

        private DiagnosticInfo lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        public RetargetingEventSymbol(RetargetingModuleSymbol retargetingModule, EventSymbol underlyingEvent)
        {
            Debug.Assert((object)retargetingModule != null);
            Debug.Assert((object)underlyingEvent != null);
            Debug.Assert(!(underlyingEvent is RetargetingEventSymbol));

            this.retargetingModule = retargetingModule;
            this.underlyingEvent = underlyingEvent;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return retargetingModule.RetargetingTranslator;
            }
        }

        public EventSymbol UnderlyingEvent
        {
            get
            {
                return underlyingEvent;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return underlyingEvent.IsImplicitlyDeclared;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return underlyingEvent.HasSpecialName;
            }
        }

        public override TypeSymbol Type
        {
            get
            {
                return this.RetargetingTranslator.Retarget(this.underlyingEvent.Type, RetargetOptions.RetargetPrimitiveTypesByTypeCode);
            }
        }

        public override MethodSymbol AddMethod
        {
            get
            {
                return (object)underlyingEvent.AddMethod == null
                    ? null
                    : this.RetargetingTranslator.Retarget(underlyingEvent.AddMethod);
            }
        }

        public override MethodSymbol RemoveMethod
        {
            get
            {
                return (object)underlyingEvent.RemoveMethod == null
                    ? null
                    : this.RetargetingTranslator.Retarget(underlyingEvent.RemoveMethod);
            }
        }

        internal override FieldSymbol AssociatedField
        {
            get
            {
                return (object)underlyingEvent.AssociatedField == null
                    ? null
                    : this.RetargetingTranslator.Retarget(underlyingEvent.AssociatedField);
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return this.underlyingEvent.IsExplicitInterfaceImplementation; }
        }

        public override ImmutableArray<EventSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (lazyExplicitInterfaceImplementations.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(
                        ref lazyExplicitInterfaceImplementations,
                        this.RetargetExplicitInterfaceImplementations(),
                        default(ImmutableArray<EventSymbol>));
                }
                return lazyExplicitInterfaceImplementations;
            }
        }

        private ImmutableArray<EventSymbol> RetargetExplicitInterfaceImplementations()
        {
            var impls = this.underlyingEvent.ExplicitInterfaceImplementations;

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
                return this.RetargetingTranslator.Retarget(this.underlyingEvent.ContainingSymbol);
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return this.retargetingModule.ContainingAssembly;
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return this.retargetingModule;
            }
        }

        public override string Name
        {
            get
            {
                return this.underlyingEvent.Name;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.underlyingEvent.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return this.underlyingEvent.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return this.underlyingEvent.DeclaringSyntaxReferences;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return this.underlyingEvent.DeclaredAccessibility;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return underlyingEvent.IsStatic;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return underlyingEvent.IsVirtual;
            }
        }

        public override bool IsOverride
        {
            get
            {
                return underlyingEvent.IsOverride;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return underlyingEvent.IsAbstract;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return underlyingEvent.IsSealed;
            }
        }

        public override bool IsExtern
        {
            get
            {
                return underlyingEvent.IsExtern;
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return underlyingEvent.ObsoleteAttributeData;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.underlyingEvent.GetAttributes();
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            return this.RetargetingTranslator.RetargetAttributes(this.underlyingEvent.GetCustomAttributesToEmit(compilationState));
        }

        internal override bool MustCallMethodsDirectly
        {
            get
            {
                return underlyingEvent.MustCallMethodsDirectly;
            }
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                DiagnosticInfo result = null;
                CalculateUseSiteDiagnostic(ref result);
                lazyUseSiteDiagnostic = result;
            }

            return lazyUseSiteDiagnostic;
        }

        public override bool IsWindowsRuntimeEvent
        {
            get
            {
                return underlyingEvent.IsWindowsRuntimeEvent;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return underlyingEvent.HasRuntimeSpecialName;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}