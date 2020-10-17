﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.ConvertTypeOfToNameOf
{
    internal abstract class AbstractConvertTypeOfToNameOfDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        protected AbstractConvertTypeOfToNameOfDiagnosticAnalyzer(LocalizableString title, string language)
            : base(diagnosticId: IDEDiagnosticIds.ConvertTypeOfToNameOfDiagnosticId,
                  option: null,
                  language: language,
                  title: title)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected abstract bool IsValidTypeofAction(OperationAnalysisContext context);

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterOperationAction(AnalyzeAction, OperationKind.TypeOf);
        }

        protected void AnalyzeAction(OperationAnalysisContext context)
        {
            if (!IsValidTypeofAction(context) || !IsValidOperation(context.Operation))
            {
                return;
            }

            var node = context.Operation.Syntax;
            var parent = node.Parent;
            // If the parent node is null then it cannot be a member access, so do not report a diagnostic
            if (parent is null)
            {
                return;
            }
            var location = parent.GetLocation();
            var options = context.Compilation.Options;
            context.ReportDiagnostic(
                DiagnosticHelper.Create(Descriptor,
                                        location,
                                        Descriptor.GetEffectiveSeverity(options),
                                        additionalLocations: null,
                                        properties: null));

        }

        private static bool IsValidOperation(IOperation operation)
        {
            // Cast to a typeof operation & check parent is a property reference and member access
            var typeofOperation = (ITypeOfOperation)operation;
            if (!(operation.Parent is IPropertyReferenceOperation))
            {
                return false;
            }

            // Check Parent is a .Name access
            var operationParent = (IPropertyReferenceOperation)operation.Parent;
            var parentProperty = operationParent.Property.Name;
            if (parentProperty != nameof(System.Type.Name) && parentProperty != "Name")
            {
                return false;
            }

            // If it's a generic type, do not offer the fix because nameof(T) and typeof(T).Name are not 
            // semantically equivalent, the resulting string is formatted differently, where typeof(T).Name
            // return "T`1" and nameof just returns "T"
            if (typeofOperation.TypeOperand is IErrorTypeSymbol)
            {
                return false;
            }
            return typeofOperation.TypeOperand is INamedTypeSymbol namedType && !namedType.IsGenericType;
        }
    }
}
