// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics.SystemLanguage
{
    /// <summary>Analyzer used to identify local variables that could be declared Const.</summary>
    public class LocalCouldBeConstAnalyzer : DiagnosticAnalyzer
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
            context.RegisterOperationBlockStartAction(
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
                                        AssignTo(argument.Value, assignedToLocals, mightBecomeConstLocals);
                                    }
                                }
                            },
                            OperationKind.InvocationExpression);

                        operationBlockContext.RegisterOperationAction(
                            (operationContext) =>
                            {
                                IVariableDeclarationStatement declaration = (IVariableDeclarationStatement)operationContext.Operation;
                                foreach (IVariableDeclaration variable in declaration.Variables)
                                {
                                    ILocalSymbol local = variable.Variable;
                                    if (!local.IsConst && !assignedToLocals.Contains(local))
                                    {
                                        var localType = local.Type;
                                        if ((!localType.IsReferenceType || localType.SpecialType == SpecialType.System_String) && localType.SpecialType != SpecialType.None)
                                        {
                                            if (variable.InitialValue != null && variable.InitialValue.ConstantValue.HasValue)
                                            {
                                                mightBecomeConstLocals.Add(local);
                                            }
                                        }
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
        }

        private static void AssignTo(IExpression target, HashSet<ILocalSymbol> assignedToLocals, HashSet<ILocalSymbol> mightBecomeConstLocals)
        {
            if (target.Kind == OperationKind.LocalReferenceExpression)
            {
                ILocalSymbol targetLocal = ((ILocalReferenceExpression)target).Local;

                assignedToLocals.Add(targetLocal);
                mightBecomeConstLocals.Remove(targetLocal);
            }
            else if (target.Kind == OperationKind.FieldReferenceExpression)
            {
                IFieldReferenceExpression fieldReference = (IFieldReferenceExpression)target;
                if (fieldReference.Instance != null && fieldReference.Instance.Type.IsValueType)
                {
                    AssignTo(fieldReference.Instance, assignedToLocals, mightBecomeConstLocals);
                }
            }
        }

        private void Report(OperationBlockAnalysisContext context, ILocalSymbol local, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, local.Locations.FirstOrDefault()));
        }
    }
}