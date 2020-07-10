// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertTypeofToNameof
{
    /// <summary>
    /// Finds code like typeof(someType).Name and determines whether it can be changed to nameof(someType), if yes then it offers a diagnostic
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpConvertTypeofToNameofDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpConvertTypeofToNameofDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.ConvertTypeOfToNameOfDiagnosticId,
                   option: null,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(
                       nameof(CSharpAnalyzersResources.Convert_typeof_to_nameof), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterOperationAction(AnalyzeTypeOfAction, OperationKind.TypeOf);
        }

        private void AnalyzeTypeOfAction(OperationAnalysisContext context)
        {
            var syntaxTree = context.Operation.Syntax.SyntaxTree;
            var node = context.Operation.Syntax;

            // Make sure that the syntax that we're looking at is actually a typeof expression
            if (!(node is TypeOfExpressionSyntax))
            {
                return;
            }

            // nameof was added in CSharp 6.0, so don't offer it for any languages before that time
            if (((CSharpParseOptions)syntaxTree.Options).LanguageVersion < LanguageVersion.CSharp6)
            {
                return;
            }

            // Make sure the parent syntax is a member access expression otherwise the syntax is not the
            // kind of expression that we want to analyze
            if (!(node.Parent is MemberAccessExpressionSyntax))
            {
                return;
            }

            // Check if the operation is one that we want to offer the fix for
            if (!IsValidOperation(context.Operation))
            {
                return;
            }

            var parent = node.Parent;
            // If the parent node is null then it cannot be a member access, so do not report a diagnostic
            if (parent is null)
            {
                return;
            }

            // Current case can be effectively changed to a nameof instance so report a diagnostic
            var location = parent.GetLocation();
            context.ReportDiagnostic(DiagnosticHelper.Create(Descriptor, location, ReportDiagnostic.Hidden, additionalLocations: null,
                properties: null, messageArgs: null));
        }
        // Overwrite GetAnalyzerCategory
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

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
            if (operationParent.Property.Name != nameof(System.Type.Name))
            {
                return false;
            }

            // If it's a generic type, do not offer the fix because nameof(T) and typeof(T).name are not 
            // semantically equivalent, typeof().Name includes information about the actual type used 
            // by the generic while nameof loses this information during the standard identifier transformation
            if (!(typeofOperation.TypeOperand is INamedTypeSymbol namedType) || namedType.IsGenericType)
            {
                return false;
            }
            return true;
        }
    }
}
