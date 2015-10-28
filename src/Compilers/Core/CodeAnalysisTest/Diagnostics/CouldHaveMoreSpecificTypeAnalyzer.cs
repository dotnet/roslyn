// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics.SystemLanguage
{
    /// <summary>Analyzer used to identify local variables and fields that could be declared with more specific types.</summary>
    public class CouldHaveMoreSpecificTypeAnalyzer : DiagnosticAnalyzer
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

                    compilationContext.RegisterSymbolAction(
                        (symbolContext) =>
                        {
                            IFieldSymbol field = (IFieldSymbol)symbolContext.Symbol;

                            // Need to get access to the field's initializer, or at least the type of the initializer.
                        },
                        SymbolKind.Field);

                    compilationContext.RegisterOperationBlockStartAction(
                        (operationBlockContext) =>
                        {
                            IMethodSymbol containingMethod = operationBlockContext.OwningSymbol as IMethodSymbol;

                            if (containingMethod != null)
                            {
                                Dictionary<ILocalSymbol, HashSet<INamedTypeSymbol>> localsSourceTypes = new Dictionary<ILocalSymbol, HashSet<INamedTypeSymbol>>();

                                operationBlockContext.RegisterOperationAction(
                                   (operationContext) =>
                                   {
                                       IAssignmentExpression assignment = (IAssignmentExpression)operationContext.Operation;
                                       AssignTo(assignment.Target, localsSourceTypes, fieldsSourceTypes, assignment.Value);
                                   },
                                   OperationKind.AssignmentExpression,
                                   OperationKind.CompoundAssignmentExpression,
                                   OperationKind.IncrementExpression);

                                operationBlockContext.RegisterOperationAction(
                                    (operationContext) =>
                                    {
                                        IInvocationExpression invocation = (IInvocationExpression)operationContext.Operation;
                                        foreach (IArgument argument in invocation.ArgumentsInParameterOrder)
                                        {
                                            if (argument.Parameter.RefKind == RefKind.Out || argument.Parameter.RefKind == RefKind.Ref)
                                            {
                                                AssignTo(argument.Value, localsSourceTypes, fieldsSourceTypes, argument.Parameter.Type);
                                            }
                                        }
                                    },
                                    OperationKind.InvocationExpression);

                                operationBlockContext.RegisterOperationAction(
                                    (operationContext) =>
                                    {
                                        IVariableDeclarationStatement declaration = (IVariableDeclarationStatement)operationContext.Operation;
                                        foreach (IVariable variable in declaration.Variables)
                                        {
                                            ILocalSymbol local = variable.Variable;
                                            if (variable.InitialValue != null)
                                            {
                                                AssignTo(local, local.Type, localsSourceTypes, variable.InitialValue);
                                            }
                                        }
                                    },
                                    OperationKind.VariableDeclarationStatement);

                                operationBlockContext.RegisterOperationBlockEndAction(
                                    (operationBlockEndContext) =>
                                    {
                                        foreach (ILocalSymbol local in localsSourceTypes.Keys)
                                        {
                                            INamedTypeSymbol mostSpecificSourceType;
                                            if (HasMoreSpecificSourceType(local, local.Type, localsSourceTypes, out mostSpecificSourceType))
                                            {
                                                Report(operationBlockEndContext, local, mostSpecificSourceType, LocalCouldHaveMoreSpecificTypeDescriptor);
                                            }
                                        }
                                    });
                            }
                        });

                    compilationContext.RegisterCompilationEndAction(
                        (compilationEndContext) =>
                        {
                            foreach (IFieldSymbol field in fieldsSourceTypes.Keys)
                            {
                                INamedTypeSymbol mostSpecificSourceType;
                                if (HasMoreSpecificSourceType(field, field.Type, fieldsSourceTypes, out mostSpecificSourceType))
                                {
                                    Report(compilationEndContext, field, mostSpecificSourceType, FieldCouldHaveMoreSpecificTypeDescriptor);
                                }
                            }
                        });
                });
        }

        static bool HasMoreSpecificSourceType<SymbolType>(SymbolType symbol, ITypeSymbol symbolType, Dictionary<SymbolType, HashSet<INamedTypeSymbol>> symbolsSourceTypes, out INamedTypeSymbol mostSpecificSourceType)
        {
            HashSet<INamedTypeSymbol> sourceTypes;
            if (symbolsSourceTypes.TryGetValue(symbol, out sourceTypes))
            {
                mostSpecificSourceType = MostSpecificType(sourceTypes);
                if (mostSpecificSourceType != null && DerivesFrom(mostSpecificSourceType, (INamedTypeSymbol)symbolType))
                {
                    return true;
                }
            }

            mostSpecificSourceType = null;
            return false;
        }

        static INamedTypeSymbol MostSpecificType(IEnumerable<INamedTypeSymbol> types)
        {
            foreach (INamedTypeSymbol type in types)
            {
                bool success = true;
                foreach (INamedTypeSymbol testType in types)
                {
                    if (type != testType)
                    {
                        if (!DerivesFrom(type, testType))
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

        static bool DerivesFrom(INamedTypeSymbol derivedType, INamedTypeSymbol baseType)
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
            }

            return false;
        }

        static void AssignTo(IExpression target, Dictionary<ILocalSymbol, HashSet<INamedTypeSymbol>> localsSourceTypes, Dictionary<IFieldSymbol, HashSet<INamedTypeSymbol>> fieldsSourceTypes, IExpression sourceValue)
        {
            AssignTo(target, localsSourceTypes, fieldsSourceTypes, OriginalType(sourceValue));
        }

        static void AssignTo(IExpression target, Dictionary<ILocalSymbol, HashSet<INamedTypeSymbol>> localsSourceTypes, Dictionary<IFieldSymbol, HashSet<INamedTypeSymbol>> fieldsSourceTypes, ITypeSymbol sourceType)
        {
            OperationKind targetKind = target.Kind;
            if (targetKind == OperationKind.LocalReferenceExpression)
            {
                ILocalSymbol targetLocal = ((ILocalReferenceExpression)target).Local;
                AssignTo(targetLocal, targetLocal.Type, localsSourceTypes, sourceType);
            }
            else if (targetKind == OperationKind.FieldReferenceExpression)
            {
                IFieldSymbol targetField = ((IFieldReferenceExpression)target).Field;
                AssignTo(targetField, targetField.Type, fieldsSourceTypes, sourceType);
            }
        }

        static void AssignTo<SymbolType>(SymbolType target, ITypeSymbol targetType, Dictionary<SymbolType, HashSet<INamedTypeSymbol>> sourceTypes, IExpression sourceValue)
        {
            AssignTo(target, targetType, sourceTypes, OriginalType(sourceValue));
        }

        static void AssignTo<SymbolType>(SymbolType target, ITypeSymbol targetType, Dictionary<SymbolType, HashSet<INamedTypeSymbol>> sourceTypes, ITypeSymbol sourceType)
        {
            HashSet<INamedTypeSymbol> symbolSourceTypes;
            TypeKind targetTypeKind = targetType.TypeKind;
            TypeKind sourceTypeKind = sourceType.TypeKind;

            // Don't suggest using an interface type instead of a class type, or vice versa.
            if ((targetTypeKind == sourceTypeKind && (targetTypeKind == TypeKind.Class || targetTypeKind == TypeKind.Interface)) ||
                (targetTypeKind == TypeKind.Class && sourceTypeKind == TypeKind.Structure && targetType.SpecialType == SpecialType.System_Object))
            {
                if (!sourceTypes.TryGetValue(target, out symbolSourceTypes))
                {
                    symbolSourceTypes = new HashSet<INamedTypeSymbol>();
                    sourceTypes[target] = symbolSourceTypes;
                }

                symbolSourceTypes.Add((INamedTypeSymbol)sourceType);
            }
        }

        static ITypeSymbol OriginalType (IExpression value)
        {
            if (value.Kind == OperationKind.ConversionExpression)
            {
                IConversionExpression conversion = (IConversionExpression)value;
                if (!conversion.IsExplicit)
                {
                    return conversion.Operand.ResultType;
                }
            }

            return value.ResultType;
        }

        void Report(OperationBlockAnalysisContext context, ILocalSymbol local, ITypeSymbol moreSpecificType, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, local.Locations.FirstOrDefault(), local, moreSpecificType));
        }

        void Report(CompilationAnalysisContext context, IFieldSymbol field, ITypeSymbol moreSpecificType, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, field.Locations.FirstOrDefault(), field, moreSpecificType));
        }
    }
}