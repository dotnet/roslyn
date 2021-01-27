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
    /// <summary>Analyzer used to identify fields that could be declared ReadOnly.</summary>
    public class FieldCouldBeReadOnlyAnalyzer : DiagnosticAnalyzer
    {
        private const string SystemCategory = "System";

        public static readonly DiagnosticDescriptor FieldCouldBeReadOnlyDescriptor = new DiagnosticDescriptor(
            "FieldCouldBeReadOnly",
            "Field Could Be ReadOnly",
            "Field is never modified and so could be readonly or const.",
            SystemCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>Gets the set of supported diagnostic descriptors from this analyzer.</summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(FieldCouldBeReadOnlyDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(
                 (compilationContext) =>
                 {
                     HashSet<IFieldSymbol> assignedToFields = new HashSet<IFieldSymbol>();
                     HashSet<IFieldSymbol> mightBecomeReadOnlyFields = new HashSet<IFieldSymbol>();

                     compilationContext.RegisterOperationBlockStartAction(
                         (operationBlockContext) =>
                         {

                             if (operationBlockContext.OwningSymbol is IMethodSymbol containingMethod)
                             {
                                 bool inConstructor = containingMethod.MethodKind == MethodKind.Constructor;
                                 ITypeSymbol staticConstructorType = containingMethod.MethodKind == MethodKind.StaticConstructor ? containingMethod.ContainingType : null;

                                 operationBlockContext.RegisterOperationAction(
                                    (operationContext) =>
                                    {
                                        if (operationContext.Operation is IAssignmentOperation assignment)
                                        {
                                            AssignTo(assignment.Target, inConstructor, staticConstructorType, assignedToFields, mightBecomeReadOnlyFields);
                                        }
                                        else if (operationContext.Operation is IIncrementOrDecrementOperation increment)
                                        {
                                            AssignTo(increment.Target, inConstructor, staticConstructorType, assignedToFields, mightBecomeReadOnlyFields);
                                        }
                                        else
                                        {
                                            throw TestExceptionUtilities.UnexpectedValue(operationContext.Operation);
                                        }
                                    },
                                    OperationKind.SimpleAssignment,
                                    OperationKind.CompoundAssignment,
                                    OperationKind.Increment,
                                    OperationKind.Decrement);

                                 operationBlockContext.RegisterOperationAction(
                                     (operationContext) =>
                                     {
                                         IInvocationOperation invocation = (IInvocationOperation)operationContext.Operation;
                                         foreach (IArgumentOperation argument in invocation.Arguments)
                                         {
                                             if (argument.Parameter.RefKind == RefKind.Out || argument.Parameter.RefKind == RefKind.Ref)
                                             {
                                                 AssignTo(argument.Value, inConstructor, staticConstructorType, assignedToFields, mightBecomeReadOnlyFields);
                                             }
                                         }
                                     },
                                     OperationKind.Invocation);
                             }
                         });

                     compilationContext.RegisterSymbolAction(
                         (symbolContext) =>
                         {
                             IFieldSymbol field = (IFieldSymbol)symbolContext.Symbol;
                             if (!field.IsConst && !field.IsReadOnly && !assignedToFields.Contains(field))
                             {
                                 mightBecomeReadOnlyFields.Add(field);
                             }
                         },
                         SymbolKind.Field
                         );

                     compilationContext.RegisterCompilationEndAction(
                         (compilationEndContext) =>
                         {
                             foreach (IFieldSymbol couldBeReadOnlyField in mightBecomeReadOnlyFields)
                             {
                                 Report(compilationEndContext, couldBeReadOnlyField, FieldCouldBeReadOnlyDescriptor);
                             }
                         });
                 });
        }

        private static void AssignTo(IOperation target, bool inConstructor, ITypeSymbol staticConstructorType, HashSet<IFieldSymbol> assignedToFields, HashSet<IFieldSymbol> mightBecomeReadOnlyFields)
        {
            if (target.Kind == OperationKind.FieldReference)
            {
                IFieldReferenceOperation fieldReference = (IFieldReferenceOperation)target;
                if (inConstructor && fieldReference.Instance != null)
                {
                    switch (fieldReference.Instance.Kind)
                    {
                        case OperationKind.InstanceReference:
                            return;
                    }
                }

                IFieldSymbol targetField = fieldReference.Field;

                if (staticConstructorType != null && targetField.IsStatic && targetField.ContainingType == staticConstructorType)
                {
                    return;
                }

                assignedToFields.Add(targetField);
                mightBecomeReadOnlyFields.Remove(targetField);

                if (fieldReference.Instance != null && fieldReference.Instance.Type.IsValueType)
                {
                    AssignTo(fieldReference.Instance, inConstructor, staticConstructorType, assignedToFields, mightBecomeReadOnlyFields);
                }
            }
        }

        private void Report(CompilationAnalysisContext context, IFieldSymbol field, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, field.Locations.FirstOrDefault()));
        }
    }
}
