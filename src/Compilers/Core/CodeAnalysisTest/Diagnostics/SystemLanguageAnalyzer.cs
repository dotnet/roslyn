// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics.SystemLanguage
{
    // These analyzers are not yet intended for any actual use. They exist solely to test IOperation support.

    /// <summary>Analyzer used to identify local variables that could be declared Const.</summary>
    public class LocalCoundBeConstAnalyzer : DiagnosticAnalyzer
    {
        private const string SystemCategory = "System";

        public static readonly DiagnosticDescriptor LocalCouldBeConstDescriptor = new DiagnosticDescriptor(
            "LocalCouldBeReadOnly",
            "Local Could Be Const",
            "Local variable is never modified and so could be const.",
            SystemCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>Gets the set of supported diagnostic descriptors from this analyzer.</summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(LocalCouldBeConstDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(
                 (compilationContext) =>
                 {
                     compilationContext.RegisterOperationBlockStartAction(
                         (operationBlockContext) =>
                         {
                             IMethodSymbol containingMethod = operationBlockContext.OwningSymbol as IMethodSymbol;

                             if (containingMethod != null)
                             {
                                 HashSet<ILocalSymbol> mightBecomeConstLocals = new HashSet<ILocalSymbol>();
                                 HashSet<ILocalSymbol> assignedToLocals = new HashSet<ILocalSymbol>();

                                 operationBlockContext.RegisterOperationAction(
                                    (operationContext) =>
                                    {
                                        IAssignmentExpression assignment = (IAssignmentExpression)operationContext.Operation;
                                        AssignTo(assignment.Target, assignedToLocals, mightBecomeConstLocals);
                                    },
                                    OperationKind.AssignmentExpression,
                                    OperationKind.CompoundAssignmentExpression);

                                 operationBlockContext.RegisterOperationAction(
                                     (operationContext) =>
                                     {
                                         IInvocationExpression invocation = (IInvocationExpression)operationContext.Operation;
                                         foreach (IArgument argument in invocation.ArgumentsInParameterOrder)
                                         {
                                             if (argument.Parameter.RefKind == RefKind.Out || argument.Parameter.RefKind == RefKind.Ref)
                                             {
                                                 AssignTo(argument.Value, assignedToLocals, mightBecomeConstLocals);
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
                                             if (!local.IsConst)
                                             {
                                                 mightBecomeConstLocals.Add(local);
                                             }
                                         }
                                     },
                                     OperationKind.VariableDeclarationStatement);

                                 operationBlockContext.RegisterOperationBlockEndAction(
                                     (operationBlockEndContext) =>
                                     {
                                         foreach (ILocalSymbol couldBeConstLocal in mightBecomeConstLocals)
                                         {
                                             Report(operationBlockEndContext, couldBeConstLocal, LocalCouldBeConstDescriptor);
                                         }
                                     });
                             }
                         });
                 });
        }

        static void AssignTo(IExpression target, HashSet<ILocalSymbol> assignedToLocals, HashSet<ILocalSymbol> mightBecomeConstLocals)
        {
            if (target.Kind == OperationKind.LocalReferenceExpression)
            {
                ILocalSymbol targetLocal = ((ILocalReferenceExpression)target).Local;

                assignedToLocals.Add(targetLocal);
                mightBecomeConstLocals.Remove(targetLocal);
            }
        }

        void Report(OperationBlockAnalysisContext context, ILocalSymbol local, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, local.Locations.FirstOrDefault()));
        }
    }

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
                             IMethodSymbol containingMethod = operationBlockContext.OwningSymbol as IMethodSymbol;

                             if (containingMethod != null)
                             {
                                 bool inConstructor = containingMethod.MethodKind == MethodKind.Constructor;
                                 ITypeSymbol staticConstructorType = containingMethod.MethodKind == MethodKind.StaticConstructor ? containingMethod.ContainingType : null;
                                 
                                 operationBlockContext.RegisterOperationAction(
                                    (operationContext) =>
                                    {
                                        IAssignmentExpression assignment = (IAssignmentExpression)operationContext.Operation;
                                        AssignTo(assignment.Target, inConstructor, staticConstructorType, assignedToFields, mightBecomeReadOnlyFields);
                                    },
                                    OperationKind.AssignmentExpression,
                                    OperationKind.CompoundAssignmentExpression);

                                 operationBlockContext.RegisterOperationAction(
                                     (operationContext) =>
                                     {
                                         IInvocationExpression invocation = (IInvocationExpression)operationContext.Operation;
                                         foreach (IArgument argument in invocation.ArgumentsInParameterOrder)
                                         {
                                             if (argument.Parameter.RefKind == RefKind.Out || argument.Parameter.RefKind == RefKind.Ref)
                                             {
                                                 AssignTo(argument.Value, inConstructor, staticConstructorType, assignedToFields, mightBecomeReadOnlyFields);
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

        static void AssignTo(IExpression target, bool inConstructor, ITypeSymbol staticConstructorType, HashSet<IFieldSymbol> assignedToFields, HashSet<IFieldSymbol> mightBecomeReadOnlyFields)
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

                if (staticConstructorType != null && targetField.IsStatic && targetField.ContainingType == staticConstructorType)
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