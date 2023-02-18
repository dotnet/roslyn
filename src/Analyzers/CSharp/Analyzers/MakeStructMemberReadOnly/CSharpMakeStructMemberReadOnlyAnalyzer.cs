// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MakeStructMemberReadOnly;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpMakeStructMemberReadOnlyDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public CSharpMakeStructMemberReadOnlyDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.MakeStructMemberReadOnlyDiagnosticId,
               EnforceOnBuildValues.MakeStructMemberReadOnly,
               CSharpCodeStyleOptions.PreferReadOnlyStructMember,
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Make_member_readonly), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Member_can_be_made_readonly), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;
            if (compilation.LanguageVersion() < LanguageVersion.CSharp8)
                return;

            context.RegisterSymbolStartAction(context =>
            {
                // Only run on non-readonly structs.  If the struct is already readonly, no need to make the members readonly.
                if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Struct, IsReadOnly: false } structType)
                    return;

                using var _ = ArrayBuilder<IMethodSymbol>.GetInstance(out var methodsToMakeReadOnly);
                context.RegisterOperationBlockAction(context => AnalyzeBlock(context, structType.OriginalDefinition, methodsToMakeReadOnly));
                context.RegisterSymbolEndAction(context => ReportDiagnostics(context, methodsToMakeReadOnly));
            }, SymbolKind.NamedType);
        });

    private static void AnalyzeBlock(
        OperationBlockAnalysisContext context,
        INamedTypeSymbol structType,
        ArrayBuilder<IMethodSymbol> methodsToMakeReadOnly)
    {
        var cancellationToken = context.CancellationToken;

        // if it's not a method, or it's already readonly, nothing to do.
        if (context.OwningSymbol is not IMethodSymbol
            {
                MethodKind: MethodKind.Ordinary or MethodKind.PropertyGet or MethodKind.PropertySet,
                IsReadOnly: false,
                IsImplicitlyDeclared: false,
            } owningMethod)
        {
            return;
        }

        foreach (var operationBlock in context.OperationBlocks)
        {
            var semanticModel = operationBlock.SemanticModel;
            Contract.ThrowIfNull(semanticModel);
            foreach (var operation in operationBlock.DescendantsAndSelf())
            {
                // if we're writing to `this` (e.g. `ref this` or `this = ...` then can't make this readonly.
                if (operation is IInstanceReferenceOperation { IsImplicit: false } instanceOperation &&
                    CSharpSemanticFacts.Instance.IsWrittenTo(semanticModel, instanceOperation.Syntax, cancellationToken))
                {
                    return;
                }

                // We're writing to a field in the containing type.  Can't make this readonly.
                if (operation is IMemberReferenceOperation
                    {
                        Kind: OperationKind.FieldReference or OperationKind.PropertyReference,
                        Instance: IInstanceReferenceOperation,
                    } fieldOrPropReference &&
                    structType.Equals(fieldOrPropReference.Member.ContainingType) &&
                    CSharpSemanticFacts.Instance.IsWrittenTo(semanticModel, fieldOrPropReference.Syntax, cancellationToken))
                {
                    return;
                }

                if (operation is IMethodReferenceOperation
                    {
                        Instance: IInstanceReferenceOperation,
                        Method.IsReadOnly: false,
                        Method: var methodReference,
                    } &&
                    structType.Equals(methodReference.ContainingType))
                {
                    // If we're referencing this method (Which isn't readonly yet) we don't want to mark us as not-readonly.
                    // a recursive call shouldn't impact the final result.
                    if (methodReference.Equals(owningMethod))
                        continue;

                    // Any methods from System.Object or System.ValueType called on this `this` don't stop this from being readonly.
                    if (methodReference.ContainingType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType)
                        continue;

                    // Any other non-readonly method usage on this means we can't be readonly.
                    return;
                }
            }
        }

        methodsToMakeReadOnly.Add(owningMethod);
    }

    private void ReportDiagnostics(
        SymbolAnalysisContext context,
        ArrayBuilder<IMethodSymbol> methodsToMakeReadOnly)
    {
        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            typeDeclaration.Identifier.GetLocation(),
            option.Notification.Severity,
            additionalLocations: ImmutableArray.Create(typeDeclaration.GetLocation()),
            properties: null));

    }
}
