// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ConvertNameOf
{
    /// <summary>
    /// Finds code like typeof(someType).Name and determines whether it can be changed to nameof(someType), if yes then it offers a diagnostic
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpConvertNameOfDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpConvertNameOfDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.ConvertNameOfDiagnosticId,
                   CSharpCodeStyleOptions.PreferBraces, //TODO: Update code style options
                   LanguageNames.CSharp,
                   new LocalizableResourceString(
                       nameof(CSharpAnalyzersResources.Convert_type_name_to_nameof), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterOperationAction(AnalyzeTypeOfAction, OperationKind.TypeOf);
        }

        private void AnalyzeTypeOfAction(OperationAnalysisContext typeofOp)
        {
            //var options = syntaxContext.Options;
            var syntaxTree = typeofOp.Operation.Syntax.SyntaxTree;
            //var cancellationToken = syntaxContext.CancellationToken;
            var node = typeofOp.Operation.Syntax;

            // Verify it's a typeof expression

            // nameof was added in CSharp 6.0, so don't offer it for any languages before that time
            if (((CSharpParseOptions)syntaxTree.Options).LanguageVersion < LanguageVersion.CSharp6)
            {
                return;
            }

            // TODO: Check for compiler errors on the declaration

            var parent = node.Parent;

            // We know that it is a typeof() instance, but we only want to offer the fix if it is a .Name access
            if (!(node.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression) && parent.IsNameMemberAccess()))
            {
                return;
            }

            // Analyze the argument to determine if argument is generic
            if (ArgumentIsGeneric(typeofOp.Operation))
            {
                return;
            }

            // Analyze the argument to determine if argument is primitive
            if (ArgumentIsPrimitive(typeofOp.Operation))
            {
                return;
            }


            // Current case can be effectively changed to a nameof instance so report a diagnostic
            var location = Location.Create(syntaxTree, parent.Span);
            var additionalLocations = ImmutableArray.Create(node.GetLocation());

            typeofOp.ReportDiagnostic(DiagnosticHelper.Create(
                     Descriptor,
                     location,
                     ReportDiagnostic.Hidden,
                     additionalLocations,
                     properties: null));
        }
        // TODO: Overwrite GetAnalyzerCategory
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        private static bool ArgumentIsGeneric(IOperation op)
        {
            // TODO: verify it is a typeof operation

            // Cast it to a ITypeOfOperation
            var tOp = (ITypeOfOperation)op;

            if (((INamedTypeSymbol)(tOp).TypeOperand).IsGenericType)
            {
                return true;
            }
            return false;
        }

        private static bool ArgumentIsPrimitive(IOperation op)
        {
            // TODO: verify it is a typeof operation
            //kind is named type for string

            // Cast it to a ITypeOfOperation
            var tOp = (ITypeOfOperation)op;
            var child = op.Syntax.ChildNodes().ElementAt(0);

            if (child.IsKind(SyntaxKind.PredefinedType))
            {
                return true;
            }
            return false;
        }

    }
}
