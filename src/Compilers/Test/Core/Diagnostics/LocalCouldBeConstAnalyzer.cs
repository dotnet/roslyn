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

                    if (operationBlockContext.OwningSymbol is IMethodSymbol containingMethod)
                    {
                        HashSet<ILocalSymbol> mightBecomeConstLocals = new HashSet<ILocalSymbol>();
                        HashSet<ILocalSymbol> assignedToLocals = new HashSet<ILocalSymbol>();

                        operationBlockContext.RegisterOperationAction(
                           (operationContext) =>
                           {
                               if (operationContext.Operation is IAssignmentOperation assignment)
                               {
                                   AssignTo(assignment.Target, assignedToLocals, mightBecomeConstLocals);
                               }
                               else if (operationContext.Operation is IIncrementOrDecrementOperation increment)
                               {
                                   AssignTo(increment.Target, assignedToLocals, mightBecomeConstLocals);
                               }
                               else
                               {
                                   throw TestExceptionUtilities.UnexpectedValue(operationContext.Operation);
                               }
                           },
                           OperationKind.SimpleAssignment,
                           OperationKind.CompoundAssignment,
                           OperationKind.Increment);

                        operationBlockContext.RegisterOperationAction(
                            (operationContext) =>
                            {
                                IInvocationOperation invocation = (IInvocationOperation)operationContext.Operation;
                                foreach (IArgumentOperation argument in invocation.Arguments)
                                {
                                    if (argument.Parameter.RefKind == RefKind.Out || argument.Parameter.RefKind == RefKind.Ref)
                                    {
                                        AssignTo(argument.Value, assignedToLocals, mightBecomeConstLocals);
                                    }
                                }
                            },
                            OperationKind.Invocation);

                        operationBlockContext.RegisterOperationAction(
                            (operationContext) =>
                            {
                                IVariableDeclarationGroupOperation declaration = (IVariableDeclarationGroupOperation)operationContext.Operation;
                                foreach (IVariableDeclaratorOperation variable in declaration.Declarations.SelectMany(decl => decl.Declarators))
                                {
                                    ILocalSymbol local = variable.Symbol;
                                    if (!local.IsConst && !assignedToLocals.Contains(local))
                                    {
                                        var localType = local.Type;
                                        if ((!localType.IsReferenceType || localType.SpecialType == SpecialType.System_String) && localType.SpecialType != SpecialType.None)
                                        {
                                            IVariableInitializerOperation initializer = variable.GetVariableInitializer();
                                            if (initializer != null && initializer.Value.ConstantValue.HasValue)
                                            {
                                                mightBecomeConstLocals.Add(local);
                                            }
                                        }
                                    }
                                }
                            },
                            OperationKind.VariableDeclarationGroup);

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

        private static void AssignTo(IOperation target, HashSet<ILocalSymbol> assignedToLocals, HashSet<ILocalSymbol> mightBecomeConstLocals)
        {
            if (target.Kind == OperationKind.LocalReference)
            {
                ILocalSymbol targetLocal = ((ILocalReferenceOperation)target).Local;

                assignedToLocals.Add(targetLocal);
                mightBecomeConstLocals.Remove(targetLocal);
            }
            else if (target.Kind == OperationKind.FieldReference)
            {
                IFieldReferenceOperation fieldReference = (IFieldReferenceOperation)target;
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
