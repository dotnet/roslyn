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
    public abstract class CA1003DiagnosticAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1003";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RuleId,
            FxCopRulesResources.UseGenericEventHandlerInstances,
            FxCopRulesResources.UseGenericEventHandlerInstances,
            FxCopDiagnosticCategory.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            helpLink: "http://msdn.microsoft.com/library/ms182178.aspx",
            customTags: DiagnosticCustomTags.Microsoft);

        protected abstract AnalyzerBase GetAnalyzer(
            Compilation compilation,
            INamedTypeSymbol eventHandler,
            INamedTypeSymbol genericEventHandler,
            INamedTypeSymbol eventArgs,
            INamedTypeSymbol comSourceInterfacesAttribute);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCompilationStartAction(
                (context) =>
                {
                    var eventHandler = WellKnownTypes.EventHandler(context.Compilation);
                    if (eventHandler == null)
                    {
                        return;
                    }

                    var genericEventHandler = WellKnownTypes.GenericEventHandler(context.Compilation);
                    if (genericEventHandler == null)
                    {
                        return;
                    }

                    var eventArgs = WellKnownTypes.EventArgs(context.Compilation);
                    if (eventArgs == null)
                    {
                        return;
                    }

                    var comSourceInterfacesAttribute = WellKnownTypes.ComSourceInterfaceAttribute(context.Compilation);
                    if (comSourceInterfacesAttribute == null)
                    {
                        return;
                    }

                    context.RegisterSymbolAction(GetAnalyzer(context.Compilation, eventHandler, genericEventHandler, eventArgs, comSourceInterfacesAttribute).AnalyzeSymbol, SymbolKind.Event);
                });
        }

        protected abstract class AnalyzerBase
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

            public void AnalyzeSymbol(SymbolAnalysisContext context)
            {
                var eventSymbol = (IEventSymbol)context.Symbol;
                if (eventSymbol != null)
                {
                    var eventType = eventSymbol.Type as INamedTypeSymbol;
                    if (eventType != null &&
                        eventSymbol.GetResultantVisibility() == SymbolVisibility.Public &&
                        !eventSymbol.IsOverride &&
                        !HasComSourceInterfacesAttribute(eventSymbol.ContainingType) &&
                        IsViolatingEventHandler(eventType))
                    {
                        context.ReportDiagnostic(eventSymbol.CreateDiagnostic(Rule));
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
