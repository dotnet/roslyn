// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace System.Runtime.Analyzers
{
    /// <summary>
    /// CA1003: Use generic event handler instances
    /// 
    /// Recommends that event handlers use <see cref="System.EventHandler{TEventArgs}"/>
    /// </summary>
    public abstract class UseGenericEventHandler : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1003";
        private static LocalizableString s_localizableMessageAndTitle = new LocalizableResourceString(nameof(SystemRuntimeAnalyzersResources.UseGenericEventHandlerInstances), SystemRuntimeAnalyzersResources.ResourceManager, typeof(SystemRuntimeAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RuleId,
            s_localizableMessageAndTitle,
            s_localizableMessageAndTitle,
            DiagnosticCategory.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            helpLinkUri: "http://msdn.microsoft.com/library/ms182178.aspx",
            customTags: WellKnownDiagnosticTags.Telemetry);

        protected abstract AnalyzerBase GetAnalyzer(
            Compilation compilation,
            INamedTypeSymbol eventHandler,
            INamedTypeSymbol genericEventHandler,
            INamedTypeSymbol eventArgs,
            INamedTypeSymbol comSourceInterfacesAttribute);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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
            private Compilation _compilation;
            private INamedTypeSymbol _eventHandler;
            private INamedTypeSymbol _genericEventHandler;
            private INamedTypeSymbol _eventArgs;
            private INamedTypeSymbol _comSourceInterfacesAttribute;

            public AnalyzerBase(
                Compilation compilation,
                INamedTypeSymbol eventHandler,
                INamedTypeSymbol genericEventHandler,
                INamedTypeSymbol eventArgs,
                INamedTypeSymbol comSourceInterfacesAttribute)
            {
                _compilation = compilation;
                _eventHandler = eventHandler;
                _genericEventHandler = genericEventHandler;
                _eventArgs = eventArgs;
                _comSourceInterfacesAttribute = comSourceInterfacesAttribute;
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
                if (type == _eventHandler)
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
                return type.OriginalDefinition == _genericEventHandler &&
                    type.TypeArguments.Length == 1;
            }

            protected bool IsEventArgs(ITypeSymbol type)
            {
                if (IsAssignableTo(_compilation, type, _eventArgs))
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
                return symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass == _comSourceInterfacesAttribute) != null;
            }
        }
    }
}
