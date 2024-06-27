// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues;

internal abstract partial class AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer : AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer
{
    private sealed partial class SymbolStartAnalyzer(
        AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer compilationAnalyzer,
        INamedTypeSymbol? eventArgsType,
        ImmutableHashSet<INamedTypeSymbol> attributeSetForMethodsToIgnore,
        DeserializationConstructorCheck deserializationConstructorCheck,
        INamedTypeSymbol? iCustomMarshaler,
        SymbolStartAnalysisContext symbolStartAnalysisContext)
    {
        private readonly AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer _compilationAnalyzer = compilationAnalyzer;

        private readonly INamedTypeSymbol? _eventArgsType = eventArgsType;
        private readonly ImmutableHashSet<INamedTypeSymbol> _attributeSetForMethodsToIgnore = attributeSetForMethodsToIgnore;
        private readonly DeserializationConstructorCheck _deserializationConstructorCheck = deserializationConstructorCheck;
        private readonly ConcurrentDictionary<IMethodSymbol, bool> _methodsUsedAsDelegates = [];
        private readonly INamedTypeSymbol? _iCustomMarshaler = iCustomMarshaler;
        private readonly SymbolStartAnalysisContext _symbolStartAnalysisContext = symbolStartAnalysisContext;

        /// <summary>
        /// Map from unused parameters to a boolean value indicating if the parameter has a read reference or not.
        /// For example, a parameter whose initial value is overwritten before any reads
        /// is an unused parameter with read reference(s).
        /// </summary>
        private readonly ConcurrentDictionary<IParameterSymbol, bool> _unusedParameters = [];

        public static void CreateAndRegisterActions(
            CompilationStartAnalysisContext context,
            AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer analyzer)
        {
            var attributeSetForMethodsToIgnore = ImmutableHashSet.CreateRange(GetAttributesForMethodsToIgnore(context.Compilation).WhereNotNull());
            var eventsArgType = context.Compilation.EventArgsType();
            var deserializationConstructorCheck = new DeserializationConstructorCheck(context.Compilation);
            var iCustomMarshaler = context.Compilation.GetTypeByMetadataName(typeof(ICustomMarshaler).FullName!);

            context.RegisterSymbolStartAction(symbolStartContext =>
            {
                if (HasSyntaxErrors((INamedTypeSymbol)symbolStartContext.Symbol, symbolStartContext.CancellationToken))
                {
                    // Bail out on syntax errors.
                    return;
                }

                // Create a new SymbolStartAnalyzer instance for every named type symbol
                // to ensure there is no shared state (such as identified unused parameters within the type),
                // as that would lead to duplicate diagnostics being reported from symbol end action callbacks
                // for unrelated named types.
                var symbolAnalyzer = new SymbolStartAnalyzer(analyzer, eventsArgType, attributeSetForMethodsToIgnore,
                    deserializationConstructorCheck, iCustomMarshaler, symbolStartContext);
                symbolAnalyzer.OnSymbolStart(symbolStartContext);
            }, SymbolKind.NamedType);

            return;

            // Local functions
            static bool HasSyntaxErrors(INamedTypeSymbol namedTypeSymbol, CancellationToken cancellationToken)
            {
                foreach (var syntaxRef in namedTypeSymbol.DeclaringSyntaxReferences)
                {
                    var syntax = syntaxRef.GetSyntax(cancellationToken);
                    if (syntax.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private void OnSymbolStart(SymbolStartAnalysisContext context)
        {
            context.RegisterOperationBlockStartAction(OnOperationBlock);
            context.RegisterSymbolEndAction(OnSymbolEnd);
        }

        private void OnOperationBlock(OperationBlockStartAnalysisContext context)
        {
            context.RegisterOperationAction(OnMethodReference, OperationKind.MethodReference);
            BlockAnalyzer.Analyze(context, this);
        }

        private void OnMethodReference(OperationAnalysisContext context)
        {
            var methodBinding = (IMethodReferenceOperation)context.Operation;
            _methodsUsedAsDelegates.GetOrAdd(methodBinding.Method.OriginalDefinition, true);
        }

        private void OnSymbolEnd(SymbolAnalysisContext context)
        {
            var interpolatedStringHandlerAttribute = context.Compilation.InterpolatedStringHandlerAttributeType();

            foreach (var (parameter, hasReference) in _unusedParameters)
            {
                ReportUnusedParameterDiagnostic(parameter, hasReference, context.ReportDiagnostic, context.Options, interpolatedStringHandlerAttribute, context.CancellationToken);
            }
        }

        private void ReportUnusedParameterDiagnostic(
            IParameterSymbol parameter,
            bool hasReference,
            Action<Diagnostic> reportDiagnostic,
            AnalyzerOptions analyzerOptions,
            INamedTypeSymbol? interpolatedStringHandlerAttributeType,
            CancellationToken cancellationToken)
        {
            if (!IsUnusedParameterCandidate(parameter, interpolatedStringHandlerAttributeType, cancellationToken))
            {
                return;
            }

            var location = parameter.Locations[0];
            var option = analyzerOptions.GetAnalyzerOptions(location.SourceTree!).UnusedParameters;
            if (option.Notification.Severity == ReportDiagnostic.Suppress ||
                !ShouldReportUnusedParameters(parameter.ContainingSymbol, option.Value, option.Notification.Severity))
            {
                return;
            }

            var message = GetMessageForUnusedParameterDiagnostic(
                parameter.Name,
                hasReference,
                isPublicApiParameter: parameter.ContainingSymbol.HasPublicResultantVisibility(),
                isLocalFunctionParameter: parameter.ContainingSymbol.IsLocalFunction());

            var diagnostic = DiagnosticHelper.CreateWithMessage(s_unusedParameterRule, location,
                option.Notification, analyzerOptions, additionalLocations: null, properties: null, message);
            reportDiagnostic(diagnostic);
        }

        private static LocalizableString GetMessageForUnusedParameterDiagnostic(
            string parameterName,
            bool hasReference,
            bool isPublicApiParameter,
            bool isLocalFunctionParameter)
        {
            LocalizableString messageFormat;
            if (isPublicApiParameter &&
                !isLocalFunctionParameter)
            {
                messageFormat = hasReference
                    ? AnalyzersResources.Parameter_0_can_be_removed_if_it_is_not_part_of_a_shipped_public_API_its_initial_value_is_never_used
                    : AnalyzersResources.Remove_unused_parameter_0_if_it_is_not_part_of_a_shipped_public_API;
            }
            else if (hasReference)
            {
                messageFormat = AnalyzersResources.Parameter_0_can_be_removed_its_initial_value_is_never_used;
            }
            else
            {
                messageFormat = s_unusedParameterRule.MessageFormat;
            }

            return new DiagnosticHelper.LocalizableStringWithArguments(messageFormat, parameterName);
        }

        private static IEnumerable<INamedTypeSymbol?> GetAttributesForMethodsToIgnore(Compilation compilation)
        {
            // Ignore conditional methods (One conditional will often call another conditional method as its only use of a parameter)
            yield return compilation.ConditionalAttribute();

            // Ignore methods with special serialization attributes (All serialization methods need to take 'StreamingContext')
            yield return compilation.OnDeserializingAttribute();
            yield return compilation.OnDeserializedAttribute();
            yield return compilation.OnSerializingAttribute();
            yield return compilation.OnSerializedAttribute();

            // Don't flag obsolete methods.
            yield return compilation.ObsoleteAttribute();

            // Don't flag MEF import constructors with ImportingConstructor attribute.
            yield return compilation.SystemCompositionImportingConstructorAttribute();
            yield return compilation.SystemComponentModelCompositionImportingConstructorAttribute();
        }

        private bool IsUnusedParameterCandidate(IParameterSymbol parameter, INamedTypeSymbol? interpolatedStringHandlerAttributeType, CancellationToken cancellationToken)
        {
            // Ignore certain special parameters/methods.
            // Note that "method.ExplicitOrImplicitInterfaceImplementations" check below is not a complete check,
            // as identifying this correctly requires analyzing referencing projects, which is not
            // supported for analyzers. We believe this is still a good enough check for most cases so 
            // we don't have to bail out on reporting unused parameters for all public methods.

            if (parameter.IsImplicitlyDeclared ||
                parameter.Name == DiscardVariableName ||
                parameter.ContainingSymbol is not IMethodSymbol method ||
                method.IsImplicitlyDeclared ||
                method.IsExtern ||
                method.IsAbstract ||
                method.IsVirtual ||
                method.IsOverride ||
                method.PartialImplementationPart != null ||
                method.PartialDefinitionPart != null ||
                !method.ExplicitOrImplicitInterfaceImplementations().IsEmpty ||
                method.IsAccessor() ||
                method.IsAnonymousFunction() ||
                _compilationAnalyzer.MethodHasHandlesClause(method) ||
                _deserializationConstructorCheck.IsDeserializationConstructor(method))
            {
                return false;
            }

            // Ignore parameters of type primary constructors since they map to public properties
            if (parameter.IsPrimaryConstructor(cancellationToken))
            {
                return false;
            }

            // Ignore event handler methods "Handler(object, MyEventArgs)"
            // as event handlers are required to match this signature
            // regardless of whether or not the parameters are used.
            if (_eventArgsType != null &&
                method.Parameters is [{ Type.SpecialType: SpecialType.System_Object }, var secondParam] &&
                secondParam.Type.InheritsFromOrEquals(_eventArgsType))
            {
                return false;
            }

            // Ignore flagging parameters for methods with certain well-known attributes,
            // which are known to have unused parameters in real world code.
            if (method.GetAttributes().Any(static (a, self) => a.AttributeClass is { } attributeClass && self._attributeSetForMethodsToIgnore.Contains(attributeClass), this))
            {
                return false;
            }

            // Methods used as delegates likely need to have unused parameters for signature compat.
            if (_methodsUsedAsDelegates.ContainsKey(method))
            {
                return false;
            }

            // Ignore special parameter names for methods that need a specific signature.
            // For example, methods used as a delegate in a different type or project.
            // This also serves as a convenient way to suppress instances of unused parameter diagnostic
            // without disabling the diagnostic completely.
            // We ignore parameter names that start with an underscore and are optionally followed by an integer,
            // such as '_', '_1', '_2', etc.
            if (parameter.IsSymbolWithSpecialDiscardName())
            {
                return false;
            }

            var methodSyntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken);
            if (_compilationAnalyzer.ReturnsThrow(methodSyntax))
            {
                return false;
            }

            if (method.ContainingType is { } containingType)
            {
                // Don't report on valid GetInstance method of ICustomMarshaler.
                // See https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.icustommarshaler#implementing-the-getinstance-method
                if (method is { MetadataName: "GetInstance", IsStatic: true, Parameters: [{ Type.SpecialType: SpecialType.System_String }] } &&
                    containingType.AllInterfaces.Any((@interface, marshaler) => @interface.Equals(marshaler), _iCustomMarshaler))
                {
                    return false;
                }

                // 2 first `int` parameters of an interpolated string handler
                // constructor are mandatory. Therefore, do not report them as unused
                if (parameter is { Type.SpecialType: SpecialType.System_Int32 } &&
                    method is { Parameters: [var firstParameter, ..], MethodKind: MethodKind.Constructor } &&
                    (firstParameter == parameter || (method.Parameters.Length > 1 && method.Parameters[1] == parameter)) &&
                    containingType.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, interpolatedStringHandlerAttributeType)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
