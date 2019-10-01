// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Mime;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseSystemHashCode
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal class UseSystemHashCodeDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public UseSystemHashCodeDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseSystemHashCode,
                   CodeStyleOptions.PreferSystemHashCode,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_System_HashCode), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(c =>
            {
                // var hashCodeType = c.Compilation.GetTypeByMetadataName("System.HashCode");
                var objectType = c.Compilation.GetSpecialType(SpecialType.System_Object);
                var objectGetHashCode = objectType?.GetMembers(nameof(GetHashCode)).FirstOrDefault() as IMethodSymbol;
                var equalityComparerTypeOpt = c.Compilation.GetTypeByMetadataName(typeof(EqualityComparer<>).FullName);

                if (// hashCodeType != null &&
                    objectGetHashCode != null)
                {
                    c.RegisterOperationBlockAction(c2 =>
                        AnalyzeOperationBlock(c2, objectGetHashCode, equalityComparerTypeOpt));
                }
            });
        }

        private void AnalyzeOperationBlock(
            OperationBlockAnalysisContext context, IMethodSymbol objectGetHashCode, INamedTypeSymbol equalityComparerTypeOpt)
        {
            if (!(context.OwningSymbol is IMethodSymbol method))
            {
                return;
            }

            if (method.Name != nameof(GetHashCode))
            {
                return;
            }

            if (!method.IsOverride)
            {
                return;
            }

            if (method.Locations.Length != 1 || method.DeclaringSyntaxReferences.Length != 1)
            {
                return;
            }

            var location = method.Locations[0];
            if (!location.IsInSource)
            {
                return;
            }

            if (context.OperationBlocks.Length != 1)
            {
                return;
            }

            var operation = context.OperationBlocks[0];
            if (!(operation is IBlockOperation blockOperation))
            {
                return;
            }

            if (!Analyzer.OverridesSystemObject(objectGetHashCode, method))
            {
                return;
            }

            var cancellationToken = context.CancellationToken;

            var optionSet = context.Options.GetDocumentOptionSetAsync(location.SourceTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CodeStyleOptions.PreferSystemHashCode, operation.Language);
            if (!option.Value)
            {
                return;
            }

            var analyzer = new Analyzer(method, objectGetHashCode, equalityComparerTypeOpt);
            var hashedMembers = analyzer.GetHashedMembers(blockOperation);

            if (!hashedMembers.IsDefaultOrEmpty)
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    this.Descriptor, location, option.Notification.Severity,
                    new[] { operation.Syntax.GetLocation() }, ImmutableDictionary<string, string>.Empty));
            }
        }
    }
}
