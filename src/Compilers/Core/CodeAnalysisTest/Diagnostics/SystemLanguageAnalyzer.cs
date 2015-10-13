// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics.SystemLanguage
{
    // These analyzers are not yet intended for any actual use. They exist solely to test IOperation support.

    /// <summary>Analyzer used to test for loop IOperations.</summary>
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
                             IMethodSymbol containingMethod = operationBlockContext.OwningSymbol as IMethodSymbol;

                             if (containingMethod != null)
                             {
                                 bool inConstructor = containingMethod.MethodKind == MethodKind.Constructor;
                                 ITypeSymbol inStaticConstructor = containingMethod.MethodKind == MethodKind.StaticConstructor ? containingMethod.ContainingType : null;

                                 operationBlockContext.RegisterOperationAction(
                                    (operationContext) =>
                                    {
                                        IAssignmentExpression assignment = (IAssignmentExpression)operationContext.Operation;
                                        AssignTo(assignment.Target, inConstructor, inStaticConstructor, assignedToFields, mightBecomeReadOnlyFields);
                                    },
                                    OperationKind.AssignmentExpression,
                                    OperationKind.CompoundAssignmentExpression);

                                 compilationContext.RegisterOperationAction(
                                     (operationContext) =>
                                     {
                                         IInvocationExpression invocation = (IInvocationExpression)operationContext.Operation;
                                         foreach (IArgument argument in invocation.ArgumentsInParameterOrder)
                                         {
                                             if (argument.Parameter.RefKind == RefKind.Out || argument.Parameter.RefKind == RefKind.Ref)
                                             {
                                                 AssignTo(argument.Value, inConstructor, inStaticConstructor, assignedToFields, mightBecomeReadOnlyFields);
                                             }
                                         }
                                     },
                                     OperationKind.InvocationExpression);
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

        static void AssignTo(IExpression target, bool inConstructor, ITypeSymbol inStaticConstructor, HashSet<IFieldSymbol> assignedToFields, HashSet<IFieldSymbol> mightBecomeReadOnlyFields)
        {
            if (target.Kind == OperationKind.FieldReferenceExpression)
            {
                IFieldReferenceExpression fieldReference = (IFieldReferenceExpression)target;
                if (inConstructor && fieldReference.Instance != null)
                {
                    switch (fieldReference.Instance.Kind)
                    {
                        case OperationKind.InstanceReferenceExpression:
                        case OperationKind.BaseClassInstanceReferenceExpression:
                        case OperationKind.ClassInstanceReferenceExpression:
                            return;
                    }
                }

                IFieldSymbol targetField = fieldReference.Field;

                if (inStaticConstructor != null && targetField.IsStatic && targetField.ContainingType == inStaticConstructor)
                {
                    return;
                }
              
                assignedToFields.Add(targetField);
                mightBecomeReadOnlyFields.Remove(targetField);
            }
        }

        void Report(CompilationAnalysisContext context, IFieldSymbol field, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, field.Locations.FirstOrDefault()));
        }
    }
}