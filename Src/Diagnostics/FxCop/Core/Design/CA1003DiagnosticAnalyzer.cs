// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    public abstract class CA1003DiagnosticAnalyzer : ICompilationNestedAnalyzerFactory
    {
        internal const string RuleId = "CA1003";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RuleId,
            FxCopRulesResources.UseGenericEventHandlerInstances,
            FxCopRulesResources.UseGenericEventHandlerInstances,
            FxCopDiagnosticCategory.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLink: "http://msdn.microsoft.com/library/ms182178.aspx",
            customTags: DiagnosticCustomTags.Microsoft);

        protected abstract AnalyzerBase GetAnalyzer(
            Compilation compilation,
            INamedTypeSymbol eventHandler,
            INamedTypeSymbol genericEventHandler,
            INamedTypeSymbol eventArgs,
            INamedTypeSymbol comSourceInterfacesAttribute);

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public IDiagnosticAnalyzer CreateAnalyzerWithinCompilation(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            var eventHandler = WellKnownTypes.EventHandler(compilation);
            if (eventHandler == null)
            {
                return null;
            }

            var genericEventHandler = WellKnownTypes.GenericEventHandler(compilation);
            if (genericEventHandler == null)
            {
                return null;
            }

            var eventArgs = WellKnownTypes.EventArgs(compilation);
            if (eventArgs == null)
            {
                return null;
            }

            var comSourceInterfacesAttribute = WellKnownTypes.ComSourceInterfaceAttribute(compilation);
            if (comSourceInterfacesAttribute == null)
            {
                return null;
            }

            return GetAnalyzer(compilation, eventHandler, genericEventHandler, eventArgs, comSourceInterfacesAttribute);
        }

        protected abstract class AnalyzerBase : ISymbolAnalyzer
        {
            private Compilation compilation;
            private INamedTypeSymbol eventHandler;
            private INamedTypeSymbol genericEventHandler;
            private INamedTypeSymbol eventArgs;
            private INamedTypeSymbol comSourceInterfacesAttribute;

            public AnalyzerBase(
                Compilation compilation,
                INamedTypeSymbol eventHandler,
                INamedTypeSymbol genericEventHandler,
                INamedTypeSymbol eventArgs,
                INamedTypeSymbol comSourceInterfacesAttribute)
            {
                this.compilation = compilation;
                this.eventHandler = eventHandler;
                this.genericEventHandler = genericEventHandler;
                this.eventArgs = eventArgs;
                this.comSourceInterfacesAttribute = comSourceInterfacesAttribute;
            }

            public ImmutableArray<SymbolKind> SymbolKindsOfInterest
            {
                get
                {
                    return ImmutableArray.Create(SymbolKind.Event);
                }
            }

            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(Rule);
                }
            }

            public void AnalyzeSymbol(ISymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                var eventSymbol = (IEventSymbol)symbol;
                if (eventSymbol != null)
                {
                    var eventType = eventSymbol.Type as INamedTypeSymbol;
                    if (eventType != null &&
                        eventSymbol.GetResultantVisibility() == SymbolVisibility.Public &&
                        !eventSymbol.IsOverride &&
                        !HasComSourceInterfacesAttribute(eventSymbol.ContainingType) &&
                        IsViolatingEventHandler(eventType))
                    {
                        addDiagnostic(eventSymbol.CreateDiagnostic(Rule));
                    }
                }
            }

            protected abstract bool IsViolatingEventHandler(INamedTypeSymbol type);

            protected abstract bool IsAssignableTo(Compilation compilation, ITypeSymbol fromSymbol, ITypeSymbol toSymbol);

            protected bool IsValidLibraryEventHandlerInstance(INamedTypeSymbol type)
            {
                if (type == this.eventHandler)
                {
                    return true;
                }

                if (IsGenericEventHandlerInstance(type) &&
                    IsEventArgs(type.TypeArguments[0]))
                {
                    return true;
                }

                return false;
            }

            protected bool IsGenericEventHandlerInstance(INamedTypeSymbol type)
            {
                return type.OriginalDefinition == this.genericEventHandler &&
                    type.TypeArguments.Length == 1;
            }

            protected bool IsEventArgs(ITypeSymbol type)
            {
                if (IsAssignableTo(this.compilation, type, this.eventArgs))
                {
                    return true;
                }

                if (type.IsValueType)
                {
                    return type.Name.EndsWith("EventArgs", StringComparison.Ordinal);
                }

                return false;
            }

            private bool HasComSourceInterfacesAttribute(INamedTypeSymbol symbol)
            {
                return symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass == this.comSourceInterfacesAttribute) != null;
            }
        }
    }
}
