// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Diagnostics;
using System;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DeconstructionVariablePendingInference
    {
        protected override ErrorCode InferenceFailedError => ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable;
    }

    internal partial class OutVariablePendingInference
    {
        protected override ErrorCode InferenceFailedError => ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedOutVariable;
    }

    internal partial class VariablePendingInference : BoundExpression
    {
        internal BoundExpression SetInferredTypeWithAnnotations(TypeWithAnnotations type, BindingDiagnosticBag? diagnosticsOpt)
        {
            Debug.Assert(type.HasType);

            return SetInferredTypeWithAnnotations(type, null, diagnosticsOpt);
        }

        internal BoundExpression SetInferredTypeWithAnnotations(TypeWithAnnotations type, Binder? binderOpt, BindingDiagnosticBag? diagnosticsOpt)
        {
            Debug.Assert(binderOpt != null || type.HasType);
            Debug.Assert(this.Syntax.Kind() == SyntaxKind.SingleVariableDesignation ||
                (this.Syntax.Kind() == SyntaxKind.DeclarationExpression &&
                    ((DeclarationExpressionSyntax)this.Syntax).Designation.Kind() == SyntaxKind.SingleVariableDesignation));

            bool inferenceFailed = !type.HasType;

            if (inferenceFailed)
            {
                type = TypeWithAnnotations.Create(binderOpt!.CreateErrorType("var"));
            }

            switch (this.VariableSymbol.Kind)
            {
                case SymbolKind.Local:
                    var localSymbol = (SourceLocalSymbol)this.VariableSymbol;

                    if (diagnosticsOpt?.DiagnosticBag != null)
                    {
                        if (inferenceFailed)
                        {
                            ReportInferenceFailure(diagnosticsOpt);
                        }
                        else
                        {
                            SyntaxNode typeOrDesignationSyntax = this.Syntax.Kind() == SyntaxKind.DeclarationExpression ?
                                ((DeclarationExpressionSyntax)this.Syntax).Type :
                                this.Syntax;

                            Binder.CheckRestrictedTypeInAsyncMethod(localSymbol.ContainingSymbol, type.Type, diagnosticsOpt, typeOrDesignationSyntax);

                            if (localSymbol.Scope == ScopedKind.ScopedValue && !type.Type.IsErrorTypeOrRefLikeType())
                            {
                                diagnosticsOpt.Add(ErrorCode.ERR_ScopedRefAndRefStructOnly,
                                                   (typeOrDesignationSyntax is TypeSyntax typeSyntax ? typeSyntax.SkipScoped(out _).SkipRef() : typeOrDesignationSyntax).Location);
                            }
                        }
                    }

                    localSymbol.SetTypeWithAnnotations(type);
                    return new BoundLocal(this.Syntax, localSymbol, BoundLocalDeclarationKind.WithInferredType, constantValueOpt: null, isNullableUnknown: false, type: type.Type, hasErrors: this.HasErrors || inferenceFailed).WithWasConverted();

                case SymbolKind.Field:
                    var fieldSymbol = (GlobalExpressionVariable)this.VariableSymbol;
                    var inferenceDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies:
#if DEBUG
                                                                                                                         true
#else
                                                                                                                         false
#endif
                                                                        );

                    if (inferenceFailed)
                    {
                        ReportInferenceFailure(inferenceDiagnostics);
                    }

                    type = fieldSymbol.SetTypeWithAnnotations(type, inferenceDiagnostics);
#if DEBUG
                    Debug.Assert(inferenceDiagnostics.DependenciesBag is object);
                    Debug.Assert(inferenceDiagnostics.DependenciesBag.Count == 0);
#endif
                    inferenceDiagnostics.Free();

                    return new BoundFieldAccess(this.Syntax,
                                                this.ReceiverOpt,
                                                fieldSymbol,
                                                null,
                                                LookupResultKind.Viable,
                                                isDeclaration: true,
                                                type: type.Type,
                                                hasErrors: this.HasErrors || inferenceFailed);

                default:
                    throw ExceptionUtilities.UnexpectedValue(this.VariableSymbol.Kind);
            }
        }

        internal BoundExpression FailInference(Binder binder, BindingDiagnosticBag? diagnosticsOpt)
        {
            return this.SetInferredTypeWithAnnotations(default, binder, diagnosticsOpt);
        }

        private void ReportInferenceFailure(BindingDiagnosticBag diagnostics)
        {
            SingleVariableDesignationSyntax designation;
            switch (this.Syntax.Kind())
            {
                case SyntaxKind.DeclarationExpression:
                    designation = (SingleVariableDesignationSyntax)((DeclarationExpressionSyntax)this.Syntax).Designation;
                    break;
                case SyntaxKind.SingleVariableDesignation:
                    designation = (SingleVariableDesignationSyntax)this.Syntax;
                    break;
                default:
                    throw ExceptionUtilities.Unreachable();
            }

            Binder.Error(
                diagnostics, this.InferenceFailedError, designation.Identifier,
                designation.Identifier.ValueText);
        }

        protected abstract ErrorCode InferenceFailedError { get; }
    }
}
