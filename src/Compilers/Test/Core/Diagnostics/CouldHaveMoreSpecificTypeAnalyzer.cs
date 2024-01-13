// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>Analyzer used to identify local variables and fields that could be declared with more specific types.</summary>
    public class SymbolCouldHaveMoreSpecificTypeAnalyzer : DiagnosticAnalyzer
    {
        private const string SystemCategory = "System";

        public static readonly DiagnosticDescriptor LocalCouldHaveMoreSpecificTypeDescriptor = new DiagnosticDescriptor(
            "LocalCouldHaveMoreSpecificType",
            "Local Could Have More Specific Type",
            "Local variable {0} could be declared with more specific type {1}.",
            SystemCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FieldCouldHaveMoreSpecificTypeDescriptor = new DiagnosticDescriptor(
           "FieldCouldHaveMoreSpecificType",
           "Field Could Have More Specific Type",
           "Field {0} could be declared with more specific type {1}.",
           SystemCategory,
           DiagnosticSeverity.Warning,
           isEnabledByDefault: true);

        /// <summary>Gets the set of supported diagnostic descriptors from this analyzer.</summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(LocalCouldHaveMoreSpecificTypeDescriptor, FieldCouldHaveMoreSpecificTypeDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(
                (compilationContext) =>
                {
                    Dictionary<IFieldSymbol, HashSet<INamedTypeSymbol>> fieldsSourceTypes = new Dictionary<IFieldSymbol, HashSet<INamedTypeSymbol>>();

                    compilationContext.RegisterOperationBlockStartAction(
                        (operationBlockContext) =>
                        {
                            if (operationBlockContext.OwningSymbol is IMethodSymbol containingMethod)
                            {
                                Dictionary<ILocalSymbol, HashSet<INamedTypeSymbol>> localsSourceTypes = new Dictionary<ILocalSymbol, HashSet<INamedTypeSymbol>>();

                                // Track explicit assignments.
                                operationBlockContext.RegisterOperationAction(
                                   (operationContext) =>
                                   {
                                       if (operationContext.Operation is IAssignmentOperation assignment)
                                       {
                                           AssignTo(assignment.Target, localsSourceTypes, fieldsSourceTypes, assignment.Value);
                                       }
                                       else if (operationContext.Operation is IIncrementOrDecrementOperation increment)
                                       {
                                           SyntaxNode syntax = increment.Syntax;
                                           ITypeSymbol type = increment.Type;
                                           var constantValue = ConstantValue.Create(1);
                                           bool isImplicit = increment.IsImplicit;
                                           var value = new LiteralOperation(increment.SemanticModel, syntax, type, constantValue, isImplicit);

                                           AssignTo(increment.Target, localsSourceTypes, fieldsSourceTypes, value);
                                       }
                                       else
                                       {
                                           throw TestExceptionUtilities.UnexpectedValue(operationContext.Operation);
                                       }
                                   },
                                   OperationKind.SimpleAssignment,
                                   OperationKind.CompoundAssignment,
                                   OperationKind.Increment);

                                // Track arguments that match out or ref parameters.
                                operationBlockContext.RegisterOperationAction(
                                    (operationContext) =>
                                    {
                                        IInvocationOperation invocation = (IInvocationOperation)operationContext.Operation;
                                        foreach (IArgumentOperation argument in invocation.Arguments)
                                        {
                                            if (argument.Parameter.RefKind == RefKind.Out || argument.Parameter.RefKind == RefKind.Ref)
                                            {
                                                AssignTo(argument.Value, localsSourceTypes, fieldsSourceTypes, argument.Parameter.Type);
                                            }
                                        }
                                    },
                                    OperationKind.Invocation);

                                // Track local variable initializations.
                                operationBlockContext.RegisterOperationAction(
                                    (operationContext) =>
                                    {
                                        IVariableInitializerOperation initializer = (IVariableInitializerOperation)operationContext.Operation;
                                        // If the parent is a single variable declaration, just process that one variable. If it's a multi variable
                                        // declaration, process all variables being assigned
                                        if (initializer.Parent is IVariableDeclaratorOperation singleVariableDeclaration)
                                        {
                                            ILocalSymbol local = singleVariableDeclaration.Symbol;
                                            AssignTo(local, local.Type, localsSourceTypes, initializer.Value);
                                        }
                                        else if (initializer.Parent is IVariableDeclarationOperation multiVariableDeclaration)
                                        {
                                            foreach (ILocalSymbol local in multiVariableDeclaration.GetDeclaredVariables())
                                            {
                                                AssignTo(local, local.Type, localsSourceTypes, initializer.Value);
                                            }
                                        }
                                    },
                                    OperationKind.VariableInitializer);

                                // Report locals that could have more specific types.
                                operationBlockContext.RegisterOperationBlockEndAction(
                                    (operationBlockEndContext) =>
                                    {
                                        foreach (ILocalSymbol local in localsSourceTypes.Keys)
                                        {
                                            if (HasMoreSpecificSourceType(local, local.Type, localsSourceTypes, out var mostSpecificSourceType))
                                            {
                                                Report(operationBlockEndContext, local, mostSpecificSourceType, LocalCouldHaveMoreSpecificTypeDescriptor);
                                            }
                                        }
                                    });
                            }
                        });

                    // Track field initializations.
                    compilationContext.RegisterOperationAction(
                        (operationContext) =>
                        {
                            IFieldInitializerOperation initializer = (IFieldInitializerOperation)operationContext.Operation;
                            foreach (IFieldSymbol initializedField in initializer.InitializedFields)
                            {
                                AssignTo(initializedField, initializedField.Type, fieldsSourceTypes, initializer.Value);
                            }
                        },
                        OperationKind.FieldInitializer);

                    // Report fields that could have more specific types.
                    compilationContext.RegisterCompilationEndAction(
                        (compilationEndContext) =>
                        {
                            foreach (IFieldSymbol field in fieldsSourceTypes.Keys)
                            {
                                if (HasMoreSpecificSourceType(field, field.Type, fieldsSourceTypes, out var mostSpecificSourceType))
                                {
                                    Report(compilationEndContext, field, mostSpecificSourceType, FieldCouldHaveMoreSpecificTypeDescriptor);
                                }
                            }
                        });
                });
        }

        private static bool HasMoreSpecificSourceType<SymbolType>(SymbolType symbol, ITypeSymbol symbolType, Dictionary<SymbolType, HashSet<INamedTypeSymbol>> symbolsSourceTypes, out INamedTypeSymbol commonSourceType)
        {
            if (symbolsSourceTypes.TryGetValue(symbol, out var sourceTypes))
            {
                commonSourceType = CommonType(sourceTypes);
                if (commonSourceType != null && DerivesFrom(commonSourceType, (INamedTypeSymbol)symbolType))
                {
                    return true;
                }
            }

            commonSourceType = null;
            return false;
        }

        private static INamedTypeSymbol CommonType(IEnumerable<INamedTypeSymbol> types)
        {
            foreach (INamedTypeSymbol type in types)
            {
                bool success = true;
                foreach (INamedTypeSymbol testType in types)
                {
                    if (type != testType)
                    {
                        if (!DerivesFrom(testType, type))
                        {
                            success = false;
                            break;
                        }
                    }
                }

                if (success)
                {
                    return type;
                }
            }

            return null;
        }

        private static bool DerivesFrom(INamedTypeSymbol derivedType, INamedTypeSymbol baseType)
        {
            if (derivedType.TypeKind == TypeKind.Class || derivedType.TypeKind == TypeKind.Structure)
            {
                INamedTypeSymbol derivedBaseType = derivedType.BaseType;
                return derivedBaseType != null && (derivedBaseType.Equals(baseType) || DerivesFrom(derivedBaseType, baseType));
            }

            else if (derivedType.TypeKind == TypeKind.Interface)
            {
                if (derivedType.Interfaces.Contains(baseType))
                {
                    return true;
                }

                foreach (INamedTypeSymbol baseInterface in derivedType.Interfaces)
                {
                    if (DerivesFrom(baseInterface, baseType))
                    {
                        return true;
                    }
                }

                return baseType.TypeKind == TypeKind.Class && baseType.SpecialType == SpecialType.System_Object;
            }

            return false;
        }

        private static void AssignTo(IOperation target, Dictionary<ILocalSymbol, HashSet<INamedTypeSymbol>> localsSourceTypes, Dictionary<IFieldSymbol, HashSet<INamedTypeSymbol>> fieldsSourceTypes, IOperation sourceValue)
        {
            AssignTo(target, localsSourceTypes, fieldsSourceTypes, OriginalType(sourceValue));
        }

        private static void AssignTo(IOperation target, Dictionary<ILocalSymbol, HashSet<INamedTypeSymbol>> localsSourceTypes, Dictionary<IFieldSymbol, HashSet<INamedTypeSymbol>> fieldsSourceTypes, ITypeSymbol sourceType)
        {
            OperationKind targetKind = target.Kind;
            if (targetKind == OperationKind.LocalReference)
            {
                ILocalSymbol targetLocal = ((ILocalReferenceOperation)target).Local;
                AssignTo(targetLocal, targetLocal.Type, localsSourceTypes, sourceType);
            }
            else if (targetKind == OperationKind.FieldReference)
            {
                IFieldSymbol targetField = ((IFieldReferenceOperation)target).Field;
                AssignTo(targetField, targetField.Type, fieldsSourceTypes, sourceType);
            }
        }

        private static void AssignTo<SymbolType>(SymbolType target, ITypeSymbol targetType, Dictionary<SymbolType, HashSet<INamedTypeSymbol>> sourceTypes, IOperation sourceValue)
        {
            AssignTo(target, targetType, sourceTypes, OriginalType(sourceValue));
        }

        private static void AssignTo<SymbolType>(SymbolType target, ITypeSymbol targetType, Dictionary<SymbolType, HashSet<INamedTypeSymbol>> sourceTypes, ITypeSymbol sourceType)
        {
            if (sourceType != null && targetType != null)
            {
                TypeKind targetTypeKind = targetType.TypeKind;
                TypeKind sourceTypeKind = sourceType.TypeKind;

                // Don't suggest using an interface type instead of a class type, or vice versa.
                if ((targetTypeKind == sourceTypeKind && (targetTypeKind == TypeKind.Class || targetTypeKind == TypeKind.Interface)) ||
                    (targetTypeKind == TypeKind.Class && (sourceTypeKind == TypeKind.Structure || sourceTypeKind == TypeKind.Interface) && targetType.SpecialType == SpecialType.System_Object))
                {
                    if (!sourceTypes.TryGetValue(target, out var symbolSourceTypes))
                    {
                        symbolSourceTypes = new HashSet<INamedTypeSymbol>();
                        sourceTypes[target] = symbolSourceTypes;
                    }

                    symbolSourceTypes.Add((INamedTypeSymbol)sourceType);
                }
            }
        }

        private static ITypeSymbol OriginalType(IOperation value)
        {
            if (value.Kind == OperationKind.Conversion)
            {
                IConversionOperation conversion = (IConversionOperation)value;
                if (conversion.IsImplicit)
                {
                    return conversion.Operand.Type;
                }
            }

            return value.Type;
        }

        private void Report(OperationBlockAnalysisContext context, ILocalSymbol local, ITypeSymbol moreSpecificType, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, local.Locations.FirstOrDefault(), local, moreSpecificType));
        }

        private void Report(CompilationAnalysisContext context, IFieldSymbol field, ITypeSymbol moreSpecificType, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, field.Locations.FirstOrDefault(), field, moreSpecificType));
        }
    }
}
