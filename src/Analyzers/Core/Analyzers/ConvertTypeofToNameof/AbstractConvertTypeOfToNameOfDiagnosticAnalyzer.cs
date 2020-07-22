// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.ConvertTypeOfToNameOf
{
    internal abstract class AbstractConvertTypeOfToNameOfDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        protected AbstractConvertTypeOfToNameOfDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.ConvertTypeOfToNameOfDiagnosticId, option: null, new LocalizableResourceString(
                       nameof(AnalyzersResources.Convert_gettype_to_nameof), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
            var csharpMessage = new LocalizableResourceString(nameof(AnalyzersResources.Convert_typeof_to_nameof), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
            CSharpDescriptor = CreateDescriptorWithId(DescriptorId, csharpMessage, csharpMessage);
        }
        internal DiagnosticDescriptor VBDescriptor => Descriptor;

        internal DiagnosticDescriptor CSharpDescriptor { get; }

        protected abstract bool IsValidTypeofAction(OperationAnalysisContext context);

        protected abstract Diagnostic LanguageReportDiagnostic(Location location, DiagnosticDescriptor cSharpDescriptor, DiagnosticDescriptor visualBasicDescriptor, CompilationOptions options);

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

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
            context.ReportDiagnostic(LanguageReportDiagnostic(location, CSharpDescriptor, VBDescriptor, context.Compilation.Options));

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
