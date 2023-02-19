// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
                if (context.Symbol is not INamedTypeSymbol
                    {
                        TypeKind: TypeKind.Struct,
                        IsReadOnly: false,
                        DeclaringSyntaxReferences: [var reference, ..],
                    } structType)
                {
                    return;
                }

                var cancellationToken = context.CancellationToken;
                var declaration = reference.GetSyntax(cancellationToken);
                var options = context.GetCSharpAnalyzerOptions(declaration.SyntaxTree);
                var option = options.PreferReadOnlyStructMember;
                if (!option.Value)
                    return;

                context.RegisterOperationBlockAction(
                    context => AnalyzeBlock(context, option.Notification.Severity));
            }, SymbolKind.NamedType);
        });

    private void AnalyzeBlock(
        OperationBlockAnalysisContext context,
        ReportDiagnostic severity)
    {
        var cancellationToken = context.CancellationToken;

        // if it's not a method, or it's already readonly, nothing to do.
        if (context.OwningSymbol is not IMethodSymbol
            {
                MethodKind: MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation or MethodKind.PropertyGet or MethodKind.PropertySet,
                IsReadOnly: false,
                IsStatic: false,
                IsImplicitlyDeclared: false,
                DeclaringSyntaxReferences.Length: > 0,
            } owningMethod)
        {
            return;
        }

        // No need to update an accessor if the containing property is already marked readonly.  Note, we have to check
        // the actual modifier, not the IPropertySymbol.IsReadOnly property as that property only tells us if the
        // property is getter-only.  It doesn't verify that the actual getter is non-mutating.
        if (owningMethod.IsPropertyAccessor())
        {
            if (owningMethod.AssociatedSymbol is not IPropertySymbol { DeclaringSyntaxReferences: [var reference, ..] })
                return;

            if (reference.GetSyntax(cancellationToken) is not BasePropertyDeclarationSyntax propertyDeclaration)
                return;

            if (propertyDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
                return;
        }

        foreach (var operationBlock in context.OperationBlocks)
        {
            var semanticModel = operationBlock.SemanticModel;
            Contract.ThrowIfNull(semanticModel);
            foreach (var instanceOperation in operationBlock.DescendantsAndSelf().OfType<IInstanceReferenceOperation>())
            {
                // if we're writing to `this` (e.g. `ref this` or `this = ...` then can't make this `readonly`.
                if (!instanceOperation.IsImplicit &&
                    CSharpSemanticFacts.Instance.IsWrittenTo(semanticModel, instanceOperation.Syntax, cancellationToken))
                {
                    return;
                }

                // Now walk up the instance-operation and see if any operation actually or potentially mutates this value.
                for (var operation = instanceOperation.Parent; operation != null; operation = operation.Parent)
                {
                    if (operation is IFieldReferenceOperation { Instance.Type.TypeKind: TypeKind.Struct, Field.IsReadOnly: false } fieldReference)
                    {
                        // If we're writing to a field off of 'this'.  Can't make this `readonly`.
                        if (CSharpSemanticFacts.Instance.IsWrittenTo(semanticModel, fieldReference.Syntax, cancellationToken))
                            return;

                        // otherwise, keeping walking upwards to make sure subsequent accesses off this field are ok.
                        continue;
                    }

                    if (operation is IPropertyReferenceOperation { Instance.Type.TypeKind: TypeKind.Struct } propertyReference)
                    {
                        // If we're writing to a prop off of 'this'.  Can't make this `readonly`.
                        if (CSharpSemanticFacts.Instance.IsWrittenTo(semanticModel, propertyReference.Syntax, cancellationToken))
                            return;

                        // If we're reading, that's only ok if we know the get-accessor exists and it is itself readonly.
                        // At that point we can stop looking upwards as properties must return values that would not mutate
                        // this.
                        if (propertyReference.Property.GetMethod is not null &&
                            propertyReference.Property.GetMethod.IsReadOnly)
                        {
                            break;
                        }

                        // otherwise, we may mutate this instance and cannot make this method we're in readonly.
                        return;
                    }

                    if (operation is IEventReferenceOperation { Instance.Type.TypeKind: TypeKind.Struct, Parent: IEventAssignmentOperation })
                    {
                        // += or -= on an event off of a struct will cause a copy if we become readonly.
                        return;
                    }

                    // See if we're accessing or invoking a method.
                    var methodReference = operation switch
                    {
                        IMethodReferenceOperation { Instance.Type.TypeKind: TypeKind.Struct } methodRefOperation => methodRefOperation.Method,
                        IInvocationOperation { Instance.Type.TypeKind: TypeKind.Struct } invocationOperation => invocationOperation.TargetMethod,
                        _ => null,
                    };

                    if (methodReference != null)
                    {
                        // Calling a readonly method off of a struct is fine since we know it can't mutate.
                        if (methodReference.IsReadOnly)
                            break;

                        // If we're referencing the method we're in (Which isn't readonly yet) we don't want to mark us as
                        // not-readonly. a recursive call shouldn't impact the final result.
                        if (methodReference.Equals(owningMethod))
                            break;

                        // Any methods from System.Object or System.ValueType called on this `this` don't stop this from being readonly.
                        if (methodReference.ContainingType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType)
                            break;

                        // Any other non-readonly method usage on this means we can't be readonly.
                        return;
                    }

                    // Wasn't a method off a struct.  This chain off this instance expression seems fine.
                    break;
                }
            }
        }

        var declaration = owningMethod.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
        if (declaration is ArrowExpressionClauseSyntax)
            declaration = declaration.GetRequiredParent();

        var nameToken = declaration switch
        {
            MethodDeclarationSyntax methodDeclaration => methodDeclaration.Identifier,
            AccessorDeclarationSyntax accessorDeclaration => accessorDeclaration.Keyword,
            PropertyDeclarationSyntax propertyDeclaration => propertyDeclaration.Identifier,
            IndexerDeclarationSyntax indexerDeclaration => indexerDeclaration.ThisKeyword,
            _ => (SyntaxToken?)null
        };

        if (nameToken is null)
            return;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            nameToken.Value.GetLocation(),
            severity,
            additionalLocations: ImmutableArray.Create(declaration.GetLocation()),
            properties: null));
    }
}
